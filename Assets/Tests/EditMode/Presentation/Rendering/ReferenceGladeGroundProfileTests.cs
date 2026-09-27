using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeGroundProfileTests
    {
        const string Strength = "_GroundMottleStrength";
        static float Mottle(Material material)
        { Assert.True(material.HasProperty(Strength),"Palette shader needs a default-zero scoped ground control.");return material.GetFloat(Strength); }

        [Test] public void OnlyOwnedGladePaletteEnablesGroundMottling()
        {
            var glade=ReferenceGladeVoxelLibrary.Load();Assert.AreEqual(0,Mottle(glade.Material));
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var surface=f.Get<NativeZone3DRenderSurface>("ActiveSurface");
                Assert.That(Mottle(surface.MaterialFor(glade.Material)),Is.InRange(.10f,.30f));
                Assert.AreEqual(0,Mottle(surface.MaterialFor(f.Library.WorldMaterial)));
                Assert.AreEqual(0,Mottle(f.Library.WorldMaterial));Assert.AreEqual(0,Mottle(glade.Material));
            }
            using(var f=new SpawnRing3DIntegrationFixture())
                Assert.AreEqual(0,Mottle(f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(f.Library.WorldMaterial)));
        }
        [Test] public void AuthorityReversalRebuildsTheProfileWithoutChangingNativeOwners()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var glade=ReferenceGladeVoxelLibrary.Load();var at=f.Zone.GetEntityPosition(f.Player);int version=f.Zone.EntityVersion;
                Assert.Greater(Mottle(f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(glade.Material)),0);
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Sodden;f.Refresh();
                Assert.AreEqual(0,Mottle(f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(f.Library.WorldMaterial)));
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Spread;f.Frame();
                Assert.Greater(Mottle(f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(glade.Material)),0);
                Assert.AreEqual(at,f.Zone.GetEntityPosition(f.Player));Assert.AreEqual(version,f.Zone.EntityVersion);
            }
        }
        [Test] public void NativeFogAndIndexedActorPropertiesSurviveProfileRefresh()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var surface=f.Get<NativeZone3DRenderSurface>("ActiveSurface");
                Assert.Greater(Mottle(surface.MaterialFor(ReferenceGladeVoxelLibrary.Load().Material)),0);
                var root=f.View(f.Player);var renderer=root.GetComponentInChildren<Renderer>();
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_GroundReviewSentinel",17);renderer.SetPropertyBlock(block);
                var indexed=new MaterialPropertyBlock();indexed.SetColor("_ReviewTint",Color.magenta);renderer.SetPropertyBlock(indexed,0);
                surface.PrepareModel(root,true);renderer.GetPropertyBlock(block);renderer.GetPropertyBlock(indexed,0);
                Assert.AreEqual(17,block.GetFloat("_GroundReviewSentinel"));Assert.AreEqual(1,block.GetFloat("_Transient"));
                Assert.AreEqual(Color.magenta,indexed.GetColor("_ReviewTint"));Assert.AreEqual(1,indexed.GetFloat("_Transient"));
                var cell=f.Zone.GetEntityCell(f.Player);cell.IsVisible=false;f.Refresh();Assert.False(f.Rendered(f.Player));
                cell.IsVisible=true;f.Refresh();Assert.True(f.Rendered(f.Player));Assert.True(f.Pick(f.Player,out _));
            }
        }
        [TestCase(false)][TestCase(true)] public void ActualGpuFragmentAddsStableBroadVariationOnlyWhenEnabled(bool remembered)
        {
            var plain=Draw(0,0,Vector3.up,false,remembered ? .5f : 1f);
            var textured=Draw(.24f,0,Vector3.up,false,remembered ? .5f : 1f);
            var repeated=Draw(.24f,0,Vector3.up,false,remembered ? .5f : 1f);
            SavePair(plain,textured,remembered);
            Assert.Greater(Mean(plain),.05,"Actual fragment must draw a lit/remembered ground plane, not pass on black pixels.");
            Assert.Less(Spread(plain),.004,"Zero setting retains spatially uniform baseline.");
            Assert.Greater(Spread(textured),.008,"Enabled ground must have visible low-contrast spatial variation on the GPU.");
            Assert.That(Mean(textured)/Mean(plain),Is.InRange(.85,1.15));
            Assert.Less(Difference(textured,repeated),.00001,"Mottling is static, not animated noise.");
        }
        [TestCase("raised")][TestCase("vertical")][TestCase("actor")]
        public void ActualGpuFragmentKeepsRaisedVerticalAndTransientOwnersUnchanged(string control)
        {
            float height=control=="raised" ? .7f : 0;var normal=control=="vertical"?Vector3.right:Vector3.up;bool actor=control=="actor";
            var plain=Draw(0,height,normal,actor,1);var enabled=Draw(.24f,height,normal,actor,1);
            Assert.Greater(Mean(plain),.05,"Control must actually render.");Assert.Less(Difference(plain,enabled),.00001);
        }
        [Test] public void ActualGpuFragmentNeverRevealsHiddenGroundOrRememberedActors()
        {
            Assert.Less(Mean(Draw(.24f,0,Vector3.up,false,0)),.00001);
            Assert.Less(Mean(Draw(.24f,0,Vector3.up,true,.5f)),.00001);
            Assert.Greater(Mean(Draw(.24f,0,Vector3.up,false,.5f)),.05);
        }
        static Color[] Draw(float strength,float height,Vector3 normal,bool transient,float visibility)
        {
            var shader=Shader.Find("Hidden/CavesOfOoo/Tests/ReferenceGladeGroundProbe");Assert.NotNull(shader);Assert.True(shader.isSupported);
            var material=new Material(shader);var fog=new Texture2D(80,25,TextureFormat.RGBA32,false,true);
            var target=new RenderTexture(128,128,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){memorylessMode=RenderTextureMemoryless.None};
            Texture2D readback=null;var previous=RenderTexture.active;
            string[] globals={"unity_SHAr","unity_SHAg","unity_SHAb","unity_SHBr","unity_SHBg","unity_SHBb","unity_SHC","_MainLightColor"};
            var old=globals.Select(Shader.GetGlobalVector).ToArray();
            try
            {
                // Test-only lighting, restored exactly; no scene RenderSettings,
                // cameras, shared materials or native world state are changed.
                for(int i=0;i<globals.Length;i++)Shader.SetGlobalVector(globals[i],i<3?new Vector4(0,0,0,.6f):Vector4.zero);
                fog.SetPixels(Enumerable.Repeat(new Color(1,1,1,visibility),80*25).ToArray());fog.Apply();fog.filterMode=FilterMode.Point;
                material.SetTexture("_FogLight",fog);material.SetTexture("_BaseMap",Texture2D.whiteTexture);material.SetFloat(Strength,strength);
                material.SetFloat("_ProbeHeight",height);material.SetVector("_ProbeNormal",normal);material.SetFloat("_Transient",transient?1:0);
                target.Create();RenderTexture.active=target;GL.Clear(false,true,Color.black);Graphics.Blit(Texture2D.whiteTexture,target,material,0);
                RenderTexture.active=target;readback=new Texture2D(128,128,TextureFormat.RGBA32,false,true);readback.ReadPixels(new Rect(0,0,128,128),0,0);readback.Apply();return readback.GetPixels();
            }
            finally
            {
                for(int i=0;i<globals.Length;i++)Shader.SetGlobalVector(globals[i],old[i]);RenderTexture.active=previous;
                if(readback!=null)Object.DestroyImmediate(readback);target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(fog);Object.DestroyImmediate(material);
            }
        }
        static float Mean(Color[] colors)=>colors.Average(c=>(c.r+c.g+c.b)/3);
        static float Spread(Color[] colors){float mean=Mean(colors);return Mathf.Sqrt(colors.Average(c=>Mathf.Pow((c.r+c.g+c.b)/3-mean,2)));}
        static float Difference(Color[] a,Color[] b)=>a.Zip(b,(x,y)=>Mathf.Abs(x.r-y.r)+Mathf.Abs(x.g-y.g)+Mathf.Abs(x.b-y.b)).Max();
        static void SavePair(Color[] plain,Color[] textured,bool remembered)
        {
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/ReferenceGlade/Art/GroundGpu",Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(path);var texture=new Texture2D(128,128,TextureFormat.RGBA32,false,true);
            try{texture.SetPixels(plain);texture.Apply();File.WriteAllBytes(Path.Combine(path,"zero.png"),texture.EncodeToPNG());texture.SetPixels(textured);texture.Apply();File.WriteAllBytes(Path.Combine(path,"enabled.png"),texture.EncodeToPNG());}
            finally{Object.DestroyImmediate(texture);}
            File.WriteAllText(Path.Combine(path,"metrics.json"),JsonUtility.ToJson(new Metrics{remembered=remembered,zeroMean=Mean(plain),enabledMean=Mean(textured),zeroSpread=Spread(plain),enabledSpread=Spread(textured)},true));
        }
        [Serializable]sealed class Metrics{public bool remembered;public float zeroMean,enabledMean,zeroSpread,enabledSpread;}
    }
}
