using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public abstract class WorldAudioRiverFixture
    {
        protected GameObject Root;
        protected AudioClip Clip;
        protected WorldAudioPlayer Audio;
        protected WorldAudioSoundscape Scene;
        protected Zone Zone;
        protected Entity Actor;
        float music, ambience, sfx;
        bool pause;
        int sequence;
        [SetUp] public void SetUp()
        {
            music=WorldAudioSettings.MusicVolume;ambience=WorldAudioSettings.AmbienceVolume;sfx=WorldAudioSettings.SfxVolume;pause=AudioListener.pause;
            WorldAudioSettings.MusicVolume=WorldAudioSettings.AmbienceVolume=WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            Root=new GameObject("river audio fixture");Clip=AudioClip.Create("water detail",48000,1,48000,false);
            var library=new WorldAudioLibrary();library.AddDetail("water-lap",Clip,.3f);library.AddDetail("water-drop",Clip,.3f);
            Audio=new WorldAudioPlayer(Root.transform,library);Scene=new WorldAudioSoundscape(Audio);
            Zone=new Zone("Overworld.10.10.0");Actor=WorldAudioAdversarialTests.Actor("river listener");
            Assert.True(Zone.AddEntity(Actor,10,10));foreach(var cell in Zone.Cells)cell.Explored=cell.IsVisible=true;
        }
        [TearDown] public void TearDown()
        {
            Audio.Dispose();UnityEngine.Object.DestroyImmediate(Root);UnityEngine.Object.DestroyImmediate(Clip);
            WorldAudioSettings.MusicVolume=music;WorldAudioSettings.AmbienceVolume=ambience;WorldAudioSettings.SfxVolume=sfx;AudioListener.pause=pause;
        }
        protected Entity Pool(int x=13,int y=10,string flow="FlowsEast",string liquid="water",string blueprint="test river")
        {
            var owner=new Entity{ID="river-"+sequence++,BlueprintName=blueprint};owner.AddPart(new PhysicsPart());owner.AddPart(new RenderPart());
            owner.AddPart(new LiquidPoolPart{LiquidId=liquid,Volume=20});if(flow!=null)owner.SetTag(flow);
            Assert.True(Zone.AddEntity(owner,x,y));return owner;
        }
        protected void Observe(BiomeType biome=BiomeType.Sodden,int depth=0,DayBand band=DayBand.Dawn,bool visible=true)
            =>Scene.Observe(Zone,Actor,biome,depth,band,visible);
        protected void StillNear()=>Pool(9,10,null);
        protected void Tick()=>Scene.Tick(.1f,true,false);
    }

    public sealed class WorldAudioRiverTests : WorldAudioRiverFixture
    {
        [TestCase("FlowsEast","test river")][TestCase("FlowsSouth","test river")]
        [TestCase(null,"SprayPool")][TestCase(null,"HelmwoodWaterPassage")]
        public void NearerStandingWaterCannotMaskAnActualFlowWitness(string tag,string blueprint)
        {
            Pool(flow:tag,blueprint:blueprint);Observe();Assert.AreEqual("water",Scene.AmbienceId,"positive flow control");
            StillNear();Observe();Assert.AreEqual("water",Scene.AmbienceId,"nearest detail owner must not replace bed authority");
            Assert.AreEqual(1,Scene.SourceCount,"retained detail categories remain bounded; flow is an independent witness");
        }
        [TestCase(13,10,9,10)][TestCase(7,10,11,10)][TestCase(10,13,10,9)][TestCase(10,7,10,11)]
        public void BedIsIndependentOfCellScanOrder(int fx,int fy,int sx,int sy)
        {Pool(fx,fy);Pool(sx,sy,null);Observe();Assert.AreEqual("water",Scene.AmbienceId);}
        [Test] public void NearestStandingPoolStillOwnsTheLocalDetail()
        {
            Pool();StillNear();Observe();Scene.Tick(10,true,false);
            Assert.AreEqual("water-lap",Audio.LastDetailId);Assert.AreEqual(1,Scene.MaterialPhrases);
            var voice=Audio.Root.GetComponentsInChildren<AudioSource>().Single(s=>s.priority==200&&s.clip==Clip);
            Assert.Less(voice.panStereo,0,"closer western still pool owns detail; eastern river owns bed");
            Assert.AreEqual("water",Scene.AmbienceId);
        }
        [Test] public void AddingPuddleDuringExistingObservationRetainsFlowOnRefresh()
        {
            Pool();Observe();int before=Scene.ScanCount;StillNear();Tick();Assert.AreEqual(before,Scene.ScanCount);
            Observe();Assert.AreEqual(before+1,Scene.ScanCount);Assert.AreEqual("water",Scene.AmbienceId);
        }
        [Test] public void NoFlowMarkerMeansOnlyLocalWaterDetail()
        {Pool(flow:null);StillNear();Observe();Assert.AreEqual("marsh",Scene.AmbienceId);Scene.Tick(10,true,false);Assert.AreEqual("water-lap",Audio.LastDetailId);}
        [Test] public void IdleKnownFlowValidationDoesNotRescanOrAllocateSources()
        {
            Pool();StillNear();Observe();int scans=Scene.ScanCount;
            for(int i=0;i<1000;i++){Tick();Audio.Update(.1f);}
            Assert.AreEqual("water",Scene.AmbienceId);Assert.AreEqual(scans,Scene.ScanCount);
            Assert.LessOrEqual(Scene.LastVisitedOwners,512);Assert.AreEqual(16,Audio.AllocatedSources);
        }
    }
}
