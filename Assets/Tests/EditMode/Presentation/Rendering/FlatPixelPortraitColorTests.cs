using System;
using System.IO;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelPortraitColorTests
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        IDisposable drawer;
        Texture2D source, readback;
        RenderTexture target;
        [TearDown] public void Cleanup()
        {
            drawer?.Dispose(); drawer=null;
            if(source!=null)Object.DestroyImmediate(source);
            if(readback!=null)Object.DestroyImmediate(readback);
            if(target!=null){target.Release();Object.DestroyImmediate(target);}
        }

        [TestCase(true)][TestCase(false)]
        public void ActualCrateAtlasUvDrawPreservesNativePaletteWithExactlyOneOutputEncoding(bool srgbTarget)
        {
            Assert.AreEqual(ColorSpace.Linear,QualitySettings.activeColorSpace,"This regression reproduces the actual Linear project.");
            var resource=Resources.Load<Texture2D>("Sprites/Live16x24/opening-cellar"); Assert.NotNull(resource);
            source=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            Assert.True(source.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,"Resources/Sprites/Live16x24/opening-cellar.png"))));
            var atlas=source.GetPixels32();var expected=new Color32[16*24];
            for(int y=0;y<24;y++)for(int x=0;x<16;x++)expected[y*16+x]=atlas[y*source.width+16+x];
            Create();
            var actual=Draw(new ExaminePortrait(resource,new Rect(16f/144,0,16f/144,1),Color.white,false),srgbTarget);
            int opaque=0;
            for(int i=0;i<expected.Length;i++)
            {
                if(expected[i].a==0){Assert.LessOrEqual(actual[i].a,1,"Transparent pixels must remain transparent.");continue;}
                opaque++;
                Color32 wanted=srgbTarget?expected[i]:(Color32)((Color)expected[i]).linear;
                Near(wanted,actual[i],2,"crate source pixel "+i+" (not a doubly encoded or doubled GUI color)");
            }
            Assert.Greater(opaque,80,"The positive comparison must include the actual crate mass.");
        }

        [Test]
        public void TintAndTransparentCounterUseTheSameGpuRouteWithoutChangingBorrowedSource()
        {
            source=new Texture2D(2,1,TextureFormat.RGBA32,false,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            source.SetPixels32(new[]{new Color32(255,255,255,255),new Color32(255,0,255,0)});source.Apply();Create();
            var tint=(Color)new Color32(148,113,61,255);var original=source.GetPixels32();
            var output=Draw(new ExaminePortrait(source,new Rect(0,0,1,1),tint,false),true);
            Near((Color32)tint,output[12*16+3],2,"Procedural white sprite must retain its authored palette tint.");
            Assert.LessOrEqual(output[12*16+12].a,1,"Transparent magenta neighbor must not be drawn.");
            CollectionAssert.AreEqual(original,source.GetPixels32());Assert.AreEqual(FilterMode.Point,source.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp,source.wrapMode);
        }

        [TestCase(false,false,false)][TestCase(false,true,false)]
        [TestCase(true,true,false)][TestCase(true,false,true)]
        public void OnlyFlatNonModelPortraitsChooseTheNewDrawPath(bool flat,bool model,bool expected)
        {
            source=new Texture2D(16,24);var portrait=new ExaminePortrait(source,new Rect(0,0,1,1),Color.white,model);
            var method=typeof(AnnouncementUI).GetMethod("UsesFlatPixelPortraitDraw",Static);
            Assert.NotNull(method,"Actual popup routing must keep models and legacy sprite fallback on their existing draw path.");
            Assert.AreEqual(expected,method.Invoke(null,new object[]{portrait,flat}));
            Assert.False((bool)method.Invoke(null,new object[]{default(ExaminePortrait),flat}));
        }

        [Test]
        public void RepeatedDrawCachesOneOwnedMaterialAndDisposeDoesNotOwnTheAtlas()
        {
            source=new Texture2D(1,1,TextureFormat.RGBA32,false,false);source.SetPixel(0,0,Color.white);source.Apply();Create();
            var portrait=new ExaminePortrait(source,new Rect(0,0,1,1),Color.white,false);
            Draw(portrait,true);var owned=Material();Assert.NotNull(owned);
            Draw(portrait,true);Assert.AreSame(owned,Material());drawer.Dispose();drawer.Dispose();
            Assert.True(owned==null);Assert.True(source!=null);Assert.False(InvokeDraw(new Rect(0,0,16,24),portrait));
        }

        [Test]
        public void NullAndInvalidRectsCannotAllocateOrDraw()
        {
            Create();Assert.False(InvokeDraw(new Rect(0,0,16,24),default));Assert.IsNull(Material());
            source=new Texture2D(1,1);var portrait=new ExaminePortrait(source,new Rect(0,0,1,1),Color.white,false);
            Assert.False(InvokeDraw(new Rect(0,0,0,24),portrait));Assert.False(InvokeDraw(new Rect(float.NaN,0,16,24),portrait));
            Assert.IsNull(Material());
        }

        Color32[] Draw(ExaminePortrait portrait,bool srgbTarget)
        {
            if(target!=null){target.Release();Object.DestroyImmediate(target);}
            target=new RenderTexture(16,24,0,RenderTextureFormat.ARGB32,srgbTarget?RenderTextureReadWrite.sRGB:RenderTextureReadWrite.Linear)
                {filterMode=FilterMode.Point,antiAliasing=1};Assert.True(target.Create());
            var previous=RenderTexture.active;bool oldWrite=GL.sRGBWrite;
            GL.PushMatrix();
            try
            {
                RenderTexture.active=target;GL.sRGBWrite=srgbTarget;GL.Clear(false,true,Color.clear);
                GL.LoadPixelMatrix(0,16,24,0);Assert.True(InvokeDraw(new Rect(0,0,16,24),portrait));
                if(readback!=null)Object.DestroyImmediate(readback);
                readback=new Texture2D(16,24,TextureFormat.RGBA32,false,true);
                readback.ReadPixels(new Rect(0,0,16,24),0,0);readback.Apply();return readback.GetPixels32();
            }
            finally{GL.PopMatrix();GL.sRGBWrite=oldWrite;RenderTexture.active=previous;}
        }
        void Create()
        {
            var type=typeof(AnnouncementUI).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelPortraitDrawer");
            Assert.NotNull(type,"Missing native palette-preserving popup draw path.");drawer=(IDisposable)Activator.CreateInstance(type);
        }
        bool InvokeDraw(Rect rect,ExaminePortrait portrait)=>(bool)drawer.GetType().GetMethod("Draw",Instance).Invoke(drawer,new object[]{rect,portrait});
        Material Material()=>(Material)drawer.GetType().GetProperty("OwnedMaterial",Instance).GetValue(drawer);
        static void Near(Color32 expected,Color32 actual,int tolerance,string message)
        {
            Assert.That(Math.Abs(expected.r-actual.r),Is.LessThanOrEqualTo(tolerance),message+" red");
            Assert.That(Math.Abs(expected.g-actual.g),Is.LessThanOrEqualTo(tolerance),message+" green");
            Assert.That(Math.Abs(expected.b-actual.b),Is.LessThanOrEqualTo(tolerance),message+" blue");
            Assert.That(Math.Abs(expected.a-actual.a),Is.LessThanOrEqualTo(tolerance),message+" alpha");
        }
    }
}
