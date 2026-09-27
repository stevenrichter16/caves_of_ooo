using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public class FirstHourAmbushSaveTests
    {
        internal sealed class Fixture:IDisposable
        {
            readonly HotbarSaveFixture scope=new HotbarSaveFixture(false,false);
            public Entity Actor,Player;public OverworldZoneManager Manager;public Zone Zone;
            public Fixture(string blueprint="Viper",bool addAmbush=true)
            {
                var factory=new EntityFactory();factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
                Manager=OverworldZoneManager.CreateDetached(factory,64);Zone=new Zone("Overworld.0.0.0");Manager.SetActiveZone(Zone);
                Player=new Entity{ID="ambush-save-player",BlueprintName="Player"};Player.Tags["Player"]="true";Player.AddPart(new PhysicsPart());
                Player.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=40,Max=40,Owner=Player};Zone.AddEntity(Player,2,2);
                Actor=factory.CreateEntity(blueprint);Assert.NotNull(Actor);Assert.True(Zone.AddEntity(Actor,20,10));
                var brain=Actor.GetPart<BrainPart>();brain.CurrentZone=Zone;brain.Rng=new System.Random(1);brain.Wanders=false;brain.WandersRandomly=false;
                if(addAmbush&&!Actor.HasPart<AIAmbushPart>())Actor.AddPart(new AIAmbushPart{WakeOnDamage=true,WakeOnHostileInSight=false,SleepParticleInterval=0});
                foreach(var goal in brain.GetGoalsSnapshot().OfType<DormantGoal>()){goal.WakeOnHostileInSight=false;goal.SleepParticleInterval=0;}
            }
            public BrainPart Brain=>Actor.GetPart<BrainPart>();
            public DormantGoal Sleep=>Brain.FindGoal<DormantGoal>();
            public void Turn()=>Actor.FireEvent(GameEvent.New("TakeTurn"));
            public void RoundTrip()
            {
                var id=Actor.ID;var old=Actor;var state=GameSessionState.Capture("e2-private","e2-private",Manager,new TurnManager(),Player,0);
                var capture=Environment.GetEnvironmentVariable("COO_E2_LEGACY_DIR");
                if(!string.IsNullOrEmpty(capture)) { System.IO.Directory.CreateDirectory(capture); using(var memory=new System.IO.MemoryStream()){state.Save(new SaveWriter(memory));System.IO.File.WriteAllBytes(System.IO.Path.Combine(capture,System.Text.RegularExpressions.Regex.Replace(TestContext.CurrentContext.Test.Name,"[^a-zA-Z0-9]","_")+".bin"),memory.ToArray());} }
                var loaded=HotbarSaveFixture.RoundTrip(state);Manager=loaded.ZoneManager;Zone=Manager.ActiveZone;Player=loaded.Player;Actor=Zone.GetAllEntities().Single(e=>e.ID==id);
                Assert.AreNotSame(old,Actor); if(Brain!=null) { Assert.AreSame(Brain, Sleep?.ParentBrain??Brain); Assert.AreSame(Zone,Brain.CurrentZone); }
            }
            public void Dispose()=>scope.Dispose();
        }
        [Test] public void AsleepRoundTripRetainsExactlyOneDormantGoalAfterEachTurn()
        {
            using(var f=new Fixture())
            {
                f.Turn();Assert.AreEqual(1,f.Brain.GetGoalsSnapshot().OfType<DormantGoal>().Count());f.RoundTrip();
                Assert.AreEqual(1,f.Brain.GetGoalsSnapshot().OfType<DormantGoal>().Count());
                for(int i=0;i<3;i++){f.Turn();Assert.AreEqual(1,f.Brain.GetGoalsSnapshot().OfType<DormantGoal>().Count());}
            }
        }
        [Test] public void FullyAwakeRoundTripNeverSilentlyRearms()
        {
            using(var f=new Fixture()){f.Sleep.Wake();f.Turn();Assert.False(f.Brain.HasGoal<DormantGoal>());f.RoundTrip();for(int i=0;i<3;i++){f.Turn();Assert.False(f.Brain.HasGoal<DormantGoal>());}}
        }
        [Test] public void PendingExplicitWakeSurvivesBeforeNextStackCleanup()
        {
            using(var f=new Fixture()){f.Sleep.Wake();Assert.True(f.Sleep.Finished());f.RoundTrip();Assert.True(f.Sleep.Finished());f.Turn();Assert.False(f.Brain.HasGoal<DormantGoal>());}
        }
        [Test] public void DamageBetweenLastSleepTickAndSaveStillWakesAfterLoad()
        {
            using(var f=new Fixture()){f.Sleep.TakeAction();f.Actor.GetStat("Hitpoints").BaseValue-=1;f.RoundTrip();f.Sleep.TakeAction();Assert.True(f.Sleep.Finished());}
        }
        [Test] public void NoDamageDoesNotCreatePendingWakeOnLoad()
        {
            using(var f=new Fixture()){f.Sleep.TakeAction();f.RoundTrip();f.Sleep.TakeAction();Assert.False(f.Sleep.Finished());}
        }
        [Test] public void OrdinaryViperDoesNotGainAnAmbushPartOrGoalOnSaveLoad()
        {
            using(var f=new Fixture("Viper",false)){Assert.False(f.Actor.HasPart<AIAmbushPart>());f.RoundTrip();f.Turn();Assert.False(f.Actor.HasPart<AIAmbushPart>());Assert.False(f.Brain.HasGoal<DormantGoal>());}
        }
        [TestCase("AmbushBandit")][TestCase("SleepingTroll")][TestCase("MimicChest")]
        public void ExistingAmbusherRetainsItsSingleSavedSleep(string blueprint)
        {
            using(var f=new Fixture(blueprint,false))
            {Assert.NotNull(f.Sleep);bool onDamage=f.Sleep.WakeOnDamage;f.RoundTrip();Assert.AreEqual(onDamage,f.Sleep.WakeOnDamage);f.Turn();Assert.AreEqual(1,f.Brain.GetGoalsSnapshot().OfType<DormantGoal>().Count());}
        }
    }
}
