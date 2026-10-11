using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public abstract class WorldAudioPhraseTailFixture
    {
        protected Zone Zone;protected Entity Actor;protected WorldAudioPlayer Audio;protected WorldAudioSoundscape Scene;
        GameObject root;AudioClip clip;float volume;bool paused;
        [SetUp] public void Setup()
        {
            volume=WorldAudioSettings.AmbienceVolume;paused=AudioListener.pause;WorldAudioSettings.AmbienceVolume=1;AudioListener.pause=false;
            root=new GameObject("valid phrase tail");clip=AudioClip.Create("two second phrase",96000,1,48000,false);var lib=new WorldAudioLibrary();
            foreach(var id in new[]{"fire","wood-settle","stone-shift","bird","insect","water-lap","water-drop"})lib.AddDetail(id,clip,.3f);
            Audio=new WorldAudioPlayer(root.transform,lib);Scene=new WorldAudioSoundscape(Audio);Zone=new Zone("Overworld.10.10.0");Actor=WorldAudioAdversarialTests.Actor("tail listener");Assert.True(Zone.AddEntity(Actor,10,10));
            foreach(var c in Zone.Cells)c.Explored=c.IsVisible=true;
        }
        [TearDown] public void Cleanup(){Audio.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);WorldAudioSettings.AmbienceVolume=volume;AudioListener.pause=paused;}
        protected Entity Source(string kind)
        {
            var owner=new Entity{ID="tail source",BlueprintName=kind};owner.AddPart(new PhysicsPart());owner.AddPart(new RenderPart());Assert.True(Zone.AddEntity(owner,11,10));
            if(kind=="fire"){owner.AddPart(new CampfirePart{FiniteCooking=false});owner.AddPart(new FuelPart{FuelMass=20});owner.AddPart(new ThermalPart{Temperature=600,FlameTemperature=500});}
            else if(kind=="bird"){owner.BlueprintName="Magpie";owner.SetTag("Creature");owner.AddPart(new BrainPart());owner.Statistics["Hitpoints"]=new Stat{Owner=owner,Name="Hitpoints",BaseValue=10,Max=10};}
            else{owner.SetTag("Wall");owner.AddPart(new MaterialPart{MaterialID=kind=="wood-settle"?"Wood":"Stone"});}
            return owner;
        }
        protected void Observe(DayBand band=DayBand.Dawn,BiomeType biome=BiomeType.Ruins,int depth=0)=>Scene.Observe(Zone,Actor,biome,depth,band,true);
        protected void Start(string kind,DayBand band=DayBand.Dawn)
        {Source(kind);Observe(band);Scene.Tick(10,true,false);Assert.AreEqual(kind,Audio.LastDetailId);Assert.AreEqual(1,Audio.ActiveDetailSources);}
        protected void Habitat(DayBand band)
        {Observe(band,BiomeType.Spread);Scene.Tick(20,true,false);Assert.AreEqual(band==DayBand.Dawn||band==DayBand.Height?"bird":"insect",Audio.LastDetailId);Assert.AreEqual(1,Audio.ActiveDetailSources);}
    }
    public sealed class WorldAudioPhraseTailTests : WorldAudioPhraseTailFixture
    {
        [TestCase("fire")][TestCase("wood-settle")][TestCase("stone-shift")]
        public void StillValidPhysicalPhraseSurvivesDawnToHeight(string kind)
        {
            Start(kind);int count=Audio.AcceptedDetailCount;Observe(DayBand.Height);
            Assert.AreEqual(1,Audio.ActiveDetailSources);Assert.AreEqual(count,Audio.AcceptedDetailCount);
            Audio.Update(.5f);Assert.AreEqual(1,Audio.ActiveDetailSources);Audio.Update(2);Assert.AreEqual(0,Audio.ActiveDetailSources);
        }
        [TestCase(DayBand.Height,DayBand.Dusk)][TestCase(DayBand.Dusk,DayBand.Dark)]
        public void FireDoesNotChangeMeaningAtAnotherClockBoundary(DayBand before,DayBand after)
        {Start("fire",before);Observe(after);Assert.AreEqual(1,Audio.ActiveDetailSources);Assert.AreEqual(1,Audio.AcceptedDetailCount);}
        [Test] public void ActualDayBirdEndsWhenNightMakesItsCallIneligible()
        {Start("bird",DayBand.Height);Observe(DayBand.Dusk);Assert.AreEqual(0,Audio.ActiveDetailSources);Assert.AreEqual(0,Scene.SourceCount);}
        [TestCase(DayBand.Dawn,DayBand.Height)][TestCase(DayBand.Dusk,DayBand.Dark)]
        public void SameHabitatTextureCanFinishAcrossEquivalentBandPolicy(DayBand before,DayBand after)
        {Habitat(before);int count=Audio.AcceptedDetailCount;Observe(after,BiomeType.Spread);Assert.AreEqual(1,Audio.ActiveDetailSources);Assert.AreEqual(count,Audio.AcceptedDetailCount);}
    }
}
