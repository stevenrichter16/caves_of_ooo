using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class FirstHourAmbushSaveAdversarialTests
    {
        static int Sleeps(BrainPart brain) => brain.GetGoalsSnapshot().OfType<DormantGoal>().Count();
        [TestCase("sleep")][TestCase("awake")][TestCase("pending")]
        public void RepeatedFullSaveCyclesRetainExactLifecycle(string state)
        {
            using(var f=new FirstHourAmbushSaveTests.Fixture())
            {
                f.Sleep.TakeAction(); if(state!="sleep") f.Sleep.Wake(); if(state=="awake")f.Turn();
                for(int cycle=0;cycle<4;cycle++)
                {
                    f.RoundTrip();Assert.AreEqual(state=="awake"?0:1,Sleeps(f.Brain));
                    if(state=="pending")Assert.True(f.Sleep.Finished());
                    else { f.Turn(); Assert.AreEqual(state=="awake"?0:1,Sleeps(f.Brain)); }
                }
            }
        }
        [Test] public void DeliberatePendingRearmSurvivesSaveRatherThanLegacyInference()
        {
            using(var f=new FirstHourAmbushSaveTests.Fixture())
            {
                f.Sleep.Wake();f.Turn();Assert.AreEqual(0,Sleeps(f.Brain));
                f.Actor.GetPart<AIAmbushPart>().Rearm();f.RoundTrip();Assert.AreEqual(0,Sleeps(f.Brain));
                f.Turn();Assert.AreEqual(1,Sleeps(f.Brain)); f.RoundTrip();f.Turn();Assert.AreEqual(1,Sleeps(f.Brain));
            }
        }
        [Test] public void NewUnsampledGoalDoesNotInventDamageBaseline()
        {
            using(var f=new FirstHourAmbushSaveTests.Fixture())
            { f.Actor.GetStat("Hitpoints").BaseValue--;f.RoundTrip();f.Sleep.TakeAction();Assert.False(f.Sleep.Finished()); }
        }
        [TestCase(true)][TestCase(false)]
        public void DamageAfterRestoredSampleHonorsWakeConfiguration(bool wake)
        {
            using(var f=new FirstHourAmbushSaveTests.Fixture())
            {f.Sleep.WakeOnDamage=wake;f.Sleep.TakeAction();f.RoundTrip();f.Actor.GetStat("Hitpoints").BaseValue--;f.Sleep.TakeAction();Assert.AreEqual(wake,f.Sleep.Finished());}
        }
        [Test] public void HealingBeforeSaveDoesNotWakeAndNewLowerSampleStillDoes()
        {
            using(var f=new FirstHourAmbushSaveTests.Fixture())
            { f.Actor.GetStat("Hitpoints").BaseValue-=2;f.Sleep.TakeAction();f.Actor.GetStat("Hitpoints").BaseValue++;f.RoundTrip();f.Sleep.TakeAction();Assert.False(f.Sleep.Finished());f.Actor.GetStat("Hitpoints").BaseValue--;f.Sleep.TakeAction();Assert.True(f.Sleep.Finished()); }
        }
        [Test] public void GoalConfigurationAgeAndBrainLinksRemainExact()
        {
            using(var f=new FirstHourAmbushSaveTests.Fixture())
            {
                f.Sleep.Age=17;f.Sleep.WakeOnDamage=false;f.Sleep.WakeOnHostileInSight=false;f.Sleep.SleepParticleInterval=23;
                f.RoundTrip();Assert.AreEqual(17,f.Sleep.Age);Assert.False(f.Sleep.WakeOnDamage);Assert.False(f.Sleep.WakeOnHostileInSight);Assert.AreEqual(23,f.Sleep.SleepParticleInterval);
                Assert.Null(f.Sleep.ParentHandler);Assert.AreSame(f.Brain,f.Sleep.ParentBrain);Assert.AreSame(f.Zone,f.Brain.CurrentZone);
            }
        }
        [Test] public void AmbushWithNoBrainCanBeSavedThenArmsExactlyOnceAfterLateAttachment()
        {
            using(var f=new FirstHourAmbushSaveTests.Fixture("Viper",false))
            {
                f.Actor.RemovePart(f.Brain);f.Actor.AddPart(new AIAmbushPart{WakeOnHostileInSight=false,SleepParticleInterval=0});f.RoundTrip();Assert.Null(f.Brain);
                f.Actor.AddPart(new BrainPart{CurrentZone=f.Zone,Rng=new Random(1),Wanders=false,WandersRandomly=false});
                f.Turn();Assert.AreEqual(1,Sleeps(f.Brain));f.RoundTrip();f.Turn();Assert.AreEqual(1,Sleeps(f.Brain));
            }
        }
        [TestCase("AsleepRoundTripRetainsExactlyOneDormantGoalAfterEachTurn",true)]
        [TestCase("FullyAwakeRoundTripNeverSilentlyRearms",false)]
        [TestCase("ExistingAmbusherRetainsItsSingleSavedSleep__AmbushBandit__",true)]
        [TestCase("ExistingAmbusherRetainsItsSingleSavedSleep__SleepingTroll__",true)]
        [TestCase("ExistingAmbusherRetainsItsSingleSavedSleep__MimicChest__",true)]
        public void ActualLegacyBytesPreserveAwakeOrSleepingShapeWithoutRearming(string key,bool sleeping)
        {
            using(var scope=new HotbarSaveFixture(false,false))
            {
                var state=FirstHourAmbushLegacyBytes.Load(key);var actor=state.ZoneManager.ActiveZone.GetAllEntities().Single(e=>e.HasPart<AIAmbushPart>());var brain=actor.GetPart<BrainPart>();
                Assert.AreEqual(sleeping?1:0,Sleeps(brain));
                for(int i=0;i<3;i++){actor.FireEvent(GameEvent.New("TakeTurn"));Assert.AreEqual(sleeping?1:0,Sleeps(brain));}
                var loaded=HotbarSaveFixture.RoundTrip(state);actor=loaded.ZoneManager.ActiveZone.GetAllEntities().Single(e=>e.ID==actor.ID);brain=actor.GetPart<BrainPart>();actor.FireEvent(GameEvent.New("TakeTurn"));Assert.AreEqual(sleeping?1:0,Sleeps(brain));
            }
        }
        [TestCase("PendingExplicitWakeSurvivesBeforeNextStackCleanup")]
        [TestCase("DamageBetweenLastSleepTickAndSaveStillWakesAfterLoad")]
        public void LegacyUnwrittenPendingStateCannotBeRecoveredButCurrentDamageStillWakes(string key)
        {
            using(var scope=new HotbarSaveFixture(false,false))
            {
                var state=FirstHourAmbushLegacyBytes.Load(key);var actor=state.ZoneManager.ActiveZone.GetAllEntities().Single(e=>e.HasPart<AIAmbushPart>());var sleep=actor.GetPart<BrainPart>().FindGoal<DormantGoal>();
                Assert.False(sleep.Finished(),"The baseline never wrote its private pending-wake state.");
                sleep.TakeAction();Assert.False(sleep.Finished(),"Legacy baseline begins at the first current HP sample.");actor.GetStat("Hitpoints").BaseValue--;sleep.TakeAction();Assert.True(sleep.Finished());
            }
        }
        [Test] public void LegacyOrdinaryViperStaysOrdinary()
        {
            using(var scope=new HotbarSaveFixture(false,false))
            {
                var state=FirstHourAmbushLegacyBytes.Load("OrdinaryViperDoesNotGainAnAmbushPartOrGoalOnSaveLoad");var actor=state.ZoneManager.ActiveZone.GetAllEntities().Single(e=>e.BlueprintName=="Viper");
                Assert.False(actor.HasPart<AIAmbushPart>());actor.FireEvent(GameEvent.New("TakeTurn"));Assert.AreEqual(0,Sleeps(actor.GetPart<BrainPart>()));
            }
        }
    }
}
