using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckFogUploadTests
    {
        internal static int Uploads(NativeZone3DRenderSurface s)
        {
            var p=typeof(NativeZone3DRenderSurface).GetProperty("FogUploadCount",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            Assert.NotNull(p,"Observe actual texture submissions, not render refresh requests.");
            return (int)p.GetValue(s);
        }
        internal static void Exact(NativeZone3DRenderSurface s,Zone z,LightMap light,bool reveal)
        {
            var actual=s.FogTexture.GetPixels32();
            for(int y=0;y<Zone.Height;y++) for(int x=0;x<Zone.Width;x++)
                Assert.AreEqual((Color32)(z==null?Color.clear:Village3DVisibility.SampleCell(z.GetCell(x,y),light,reveal)),actual[(Zone.Height-1-y)*Zone.Width+x],"cell "+x+","+y);
        }
        [Test] public void UnchangedFogPublishesOnceAndChangedCellPublishesImmediately()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var s=f.Get<NativeZone3DRenderSurface>("ActiveSurface");s.UpdateFog(f.Zone,f.Light,false);int n=Uploads(s);
                for(int i=0;i<20;i++)s.UpdateFog(f.Zone,f.Light,false);
                Assert.AreEqual(n,Uploads(s));Exact(s,f.Zone,f.Light,false);
                var c=f.Zone.GetCell(20,10);c.Explored=true;c.IsVisible=!c.IsVisible;
                s.UpdateFog(f.Zone,f.Light,false);Assert.AreEqual(n+1,Uploads(s));Exact(s,f.Zone,f.Light,false);
                s.UpdateFog(f.Zone,f.Light,false);Assert.AreEqual(n+1,Uploads(s));
            }
        }
        [TestCase("reveal")][TestCase("remembered")][TestCase("unexplored")][TestCase("ambient")][TestCase("tint")][TestCase("null")][TestCase("replacement")]
        public void DirectStateChangesKeepEverySubmittedPixelCurrent(string change)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var s=f.Get<NativeZone3DRenderSurface>("ActiveSurface");var z=f.Zone;var light=f.Light;bool reveal=false;
                var c=z.GetCell(20,10);c.Explored=true;c.IsVisible=true;s.UpdateFog(z,light,false);int n=Uploads(s);
                if(change=="reveal"){c.Explored=false;c.IsVisible=false;s.UpdateFog(z,light,false);n=Uploads(s);reveal=true;}
                if(change=="remembered")c.IsVisible=false;
                if(change=="unexplored")c.Explored=false;
                if(change=="ambient"){z.AmbientLevel=.97f;light.Compute(z);}
                if(change=="tint"){z.AmbientTint=Color.magenta;light.Compute(z);}
                if(change=="null")z=null;
                if(change=="replacement")z=new Zone(z.ZoneID);
                s.UpdateFog(z,light,reveal);Assert.AreEqual(n+1,Uploads(s));Exact(s,z,light,reveal);
                s.UpdateFog(z,light,reveal);Assert.AreEqual(n+1,Uploads(s));
            }
        }
        [Test] public void ExternalTextureEditIsRepairedWithoutRelyingOnWorldRevision()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var s=f.Get<NativeZone3DRenderSurface>("ActiveSurface");s.UpdateFog(f.Zone,f.Light,false);int n=Uploads(s);
                s.FogTexture.SetPixel(0,0,Color.magenta);s.FogTexture.Apply(false,false);
                s.UpdateFog(f.Zone,f.Light,false);Assert.AreEqual(n+1,Uploads(s));Exact(s,f.Zone,f.Light,false);
            }
        }
        [Test] public void NullFogDoesNotRepeatedlyUploadClearedTexture()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var s=f.Get<NativeZone3DRenderSurface>("ActiveSurface");s.UpdateFog(null,null,false);int n=Uploads(s);
                s.UpdateFog(null,null,true);Assert.AreEqual(n,Uploads(s));Exact(s,null,null,true);
            }
        }
    }
}
