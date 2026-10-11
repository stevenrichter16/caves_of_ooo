using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public abstract class WorldAudioAnimalRestFixture
    {
        protected Zone Zone;protected Entity Actor;protected WorldAudioPlayer Audio;protected WorldAudioSoundscape Scene;
        GameObject root;AudioClip clip;float volume;bool paused;int sequence;
        [SetUp] public void Setup()
        {
            volume=WorldAudioSettings.AmbienceVolume;paused=AudioListener.pause;WorldAudioSettings.AmbienceVolume=1;AudioListener.pause=false;
            root=new GameObject("animal rest audio");clip=AudioClip.Create("animal call",48000,1,48000,false);var library=new WorldAudioLibrary();
            foreach(var id in new[]{"frog","dog","bird"})library.AddDetail(id,clip,.3f);
            Audio=new WorldAudioPlayer(root.transform,library);Scene=new WorldAudioSoundscape(Audio);
            Zone=new Zone("Overworld.10.10.0");Actor=WorldAudioAdversarialTests.Actor("rest listener");Assert.True(Zone.AddEntity(Actor,10,10));
            foreach(var c in Zone.Cells)c.IsVisible=c.Explored=true;
        }
        [TearDown] public void Cleanup(){Audio.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);WorldAudioSettings.AmbienceVolume=volume;AudioListener.pause=paused;}
        protected Entity Animal(string blueprint="Bandfrog",int x=11)
        {
            var e=new Entity{ID="vocal-"+sequence++,BlueprintName=blueprint};e.SetTag("Creature");e.AddPart(new PhysicsPart());e.AddPart(new RenderPart());e.AddPart(new BrainPart());
            e.Statistics["Hitpoints"]=new Stat{Owner=e,Name="Hitpoints",BaseValue=20,Max=20};Assert.True(Zone.AddEntity(e,x,10));return e;
        }
        protected void Observe()=>Scene.Observe(Zone,Actor,BiomeType.Sodden,0,DayBand.Dawn,true);
        protected Effect Rest(string kind)=>kind=="sleep"?(Effect)new AsleepByGasEffect(5):kind=="hibernate"?new HibernatingEffect(5):new FrozenEffect(.5f);
        protected void Start(string expected="frog"){Audio.Update(2);Observe();Scene.Tick(30,true,false);Assert.AreEqual(expected,Audio.LastDetailId);Assert.AreEqual(1,Audio.ActiveDetailSources);}
    }
    public sealed class WorldAudioAnimalRestTests : WorldAudioAnimalRestFixture
    {
        [TestCase("Bandfrog","frog")][TestCase("Reedfrog","frog")][TestCase("GinFrog","frog")]
        [TestCase("MawToad","frog")][TestCase("HelmwoodFrog","frog")][TestCase("PetDog","dog")][TestCase("Magpie","bird")]
        public void EachApprovedAnimalStopsVoluntaryVoiceWhileFrozen(string blueprint,string cue)
        {
            var owner=Animal(blueprint);Start(cue);Assert.True(owner.ApplyEffect(new FrozenEffect(.5f)));int scans=Scene.ScanCount,accepted=Audio.AcceptedDetailCount;
            Scene.Tick(.01f,true,false);Assert.AreEqual(0,Audio.ActiveDetailSources);Assert.AreEqual(0,Scene.SourceCount);
            for(int i=0;i<60;i++)Scene.Tick(1,true,false);Assert.AreEqual(accepted,Audio.AcceptedDetailCount);Assert.AreEqual(scans,Scene.ScanCount);
        }
        [TestCase("sleep")][TestCase("hibernate")][TestCase("freeze")]
        public void ActualRestEffectBlocksDiscoveryAndRealRemovalRearmsOnObservation(string kind)
        {
            var owner=Animal();var effect=Rest(kind);Assert.True(owner.ApplyEffect(effect));Observe();Assert.AreEqual(0,Scene.SourceCount);
            owner.GetPart<StatusEffectsPart>().RemoveEffect(effect);Start();Assert.AreEqual(1,Scene.SourceCount);
        }
        [Test] public void NoMovementRootDoesNotMakeTheAnimalUnconscious()
        {var owner=Animal();Assert.True(owner.ApplyEffect(new RootedEffect(4)));Start();Assert.False(owner.GetEffect<RootedEffect>().AllowMovement(owner));Assert.True(owner.GetEffect<RootedEffect>().AllowAction(owner));}
        [Test] public void RealCalmGoalAndPassiveBrainRemainVocal()
        {var owner=Animal();owner.GetPart<BrainPart>().Passive=true;owner.GetPart<BrainPart>().PushGoal(new NoFightGoal(50,wander:false){BreakOnDamage=true});Start();}
        [Test] public void OrdinaryDurationExpiryCanBeObservedAsAwakeAgain()
        {
            var owner=Animal();var effect=new AsleepByGasEffect(1);Assert.True(owner.ApplyEffect(effect));Observe();Assert.AreEqual(0,Scene.SourceCount);
            effect.OnTurnEnd(owner);Assert.AreEqual(0,effect.Duration);Start();
        }
    }
}
