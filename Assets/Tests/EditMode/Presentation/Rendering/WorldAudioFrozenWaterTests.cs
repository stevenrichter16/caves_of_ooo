using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public abstract class WorldAudioFrozenWaterFixture : NaturalLiquidFreezeReviewFixture
    {
        protected WorldAudioPlayer Audio;protected WorldAudioSoundscape Scene;
        GameObject root;AudioClip clip;float ambience;bool paused;
        [SetUp] public void SetUpAudio()
        {
            ambience=WorldAudioSettings.AmbienceVolume;paused=AudioListener.pause;WorldAudioSettings.AmbienceVolume=1;AudioListener.pause=false;
            root=new GameObject("frozen water audio");clip=AudioClip.Create("wet sound",24000,1,48000,false);
            var library=new WorldAudioLibrary();library.AddDetail("water-lap",clip,.3f);library.AddDetail("water-drop",clip,.3f);
            Audio=new WorldAudioPlayer(root.transform,library);Scene=new WorldAudioSoundscape(Audio);
        }
        [TearDown] public void TearDownAudio()
        {Audio.Dispose();Object.DestroyImmediate(root);Object.DestroyImmediate(clip);WorldAudioSettings.AmbienceVolume=ambience;AudioListener.pause=paused;}
        protected Entity WetPool(string blueprint)
        {var pool=Place(blueprint);pool.GetPart<TileStateSourcePart>()?.Seed(Zone,11,10);return pool;}
        protected void Observe(int depth=0)=>Scene.Observe(Zone,Actor,BiomeType.Sodden,depth,DayBand.Dawn,true);
        protected void Start(Entity pool,int depth=0)
        {Observe(depth);Scene.Tick(10,true,false);Assert.AreEqual(1,Scene.MaterialPhrases);Assert.AreEqual(depth>0?"water-drop":"water-lap",Audio.LastDetailId);}
    }
    public sealed class WorldAudioFrozenWaterTests : WorldAudioFrozenWaterFixture
    {
        [TestCase("WaterPuddle")][TestCase("BrinePool")][TestCase("MirePool")]
        public void ActualPaidFrostLichenSilencesRemainingPhysicalPool(string blueprint)
        {
            var pool=WetPool(blueprint);Start(pool);int before=pool.GetPart<LiquidPoolPart>().Volume;
            var item=Carry("FrostLichen");Assert.True(Act(item,Freeze(item)));Assert.True(Zone.TileState.HasCoating(11,10,"ice"));
            Assert.AreEqual(before,pool.GetPart<LiquidPoolPart>().Volume);Assert.False(Pack.Objects.Contains(item));
            int scans=Scene.ScanCount,phrases=Scene.MaterialPhrases;Scene.Tick(.01f,true,false);Assert.AreEqual(0,Audio.ActiveDetailSources);
            for(int i=0;i<40;i++)Scene.Tick(1,true,false);Assert.AreEqual(phrases,Scene.MaterialPhrases);Assert.AreEqual(scans,Scene.ScanCount);
        }
        [TestCase("WaterPuddle")][TestCase("BrinePool")][TestCase("MirePool")]
        public void ThawedCurrentSourceCanBeObservedAgain(string blueprint)
        {
            var pool=WetPool(blueprint);Zone.TileState.WriteCoating(11,10,"ice",4);Observe();Assert.AreEqual(0,Scene.SourceCount);
            Assert.True(Zone.TileState.RemoveCoating(11,10,"ice"));Observe();Assert.AreEqual(1,Scene.SourceCount);Scene.Tick(10,true,false);Assert.AreEqual(1,Scene.MaterialPhrases);
        }
        [TestCase("WaterPuddle")][TestCase("BrinePool")][TestCase("MirePool")]
        public void ExplicitFrozenEffectUsesTheSameCurrentPhase(string blueprint)
        {
            var pool=WetPool(blueprint);Observe();Assert.AreEqual(1,Scene.SourceCount);
            Assert.True(pool.ApplyEffect(new FrozenEffect(1),Actor,Zone));Scene.Tick(.01f,true,false);
            Assert.False(LiquidSourcePhase.CanDrawWater(Zone,pool));Assert.AreEqual(0,Scene.SourceCount);
            pool.GetEffect<FrozenEffect>().Cold=0;Observe();Assert.True(LiquidSourcePhase.CanDrawWater(Zone,pool));Assert.AreEqual(1,Scene.SourceCount);
        }
        [Test] public void GroundIceRetiresActualFlowBedWithoutRescan()
        {
            var pool=WetPool("WaterPuddle");pool.SetTag("FlowsEast");Observe();Assert.AreEqual("water",Scene.AmbienceId);
            int scans=Scene.ScanCount;Zone.TileState.WriteCoating(11,10,"ice",4);Scene.Tick(.01f,true,false);
            Assert.AreEqual("marsh",Scene.AmbienceId);Assert.AreEqual(0,Scene.SourceCount);Assert.AreEqual(scans,Scene.ScanCount);
        }
        [Test] public void UndergroundDripAlsoStopsWhenItsPoolFreezes()
        {var pool=WetPool("WaterPuddle");Start(pool,1);Zone.TileState.WriteCoating(11,10,"ice",4);Scene.Tick(.01f,true,false);Assert.AreEqual(0,Audio.ActiveDetailSources);Assert.AreEqual("cave",Scene.AmbienceId);}
        [TestCase("BrinePool")][TestCase("MirePool")]
        public void ActualColdTemperatureUsesExistingDrawPhaseAuthority(string blueprint)
        {
            var pool=WetPool(blueprint);var heat=pool.GetPart<ThermalPart>();Assert.NotNull(heat);Observe();Assert.AreEqual(1,Scene.SourceCount);
            heat.Temperature=heat.FreezeTemperature;Scene.Tick(.01f,true,false);Assert.False(LiquidSourcePhase.CanDrawWater(Zone,pool));Assert.AreEqual(0,Scene.SourceCount);
            heat.Temperature=heat.FreezeTemperature+1;Observe();Assert.True(LiquidSourcePhase.CanDrawWater(Zone,pool));Assert.AreEqual(1,Scene.SourceCount);
        }
    }
}
