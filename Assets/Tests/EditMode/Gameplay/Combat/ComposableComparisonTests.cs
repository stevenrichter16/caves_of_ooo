using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class ComposableComparisonTests
    {
        EntityFactory factory;
        System.Collections.Generic.Dictionary<string,int> oldReputation;
        [SetUp] public void Setup(){oldReputation=PlayerReputation.GetAll();factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));SkillRegistry.ResetForTests();SkillRegistry.EnsureInitialized();}
        [TearDown] public void Cleanup(){SkillRegistry.ResetForTests();PlayerReputation.Restore(oldReputation);}
        [TestCase(0,false)] [TestCase(1,false)] [TestCase(2,false)] [TestCase(3,false)]
        [TestCase(0,true)] [TestCase(1,true)] [TestCase(2,true)] [TestCase(3,true)]
        public void SixRealPurchasesKeepEqualKitAndManualDoesNotGrantRoot(int profile,bool vault)
        {
            var zone=new Zone("comparison-loadout");
            var a=ComposableBuildComparisonBench.CreatePlayer(factory,zone,(ComposableBuildComparisonBench.Profile)profile,vault);
            Assert.AreEqual(0,a.GetStatValue("SP"));Assert.AreEqual(40,a.GetStatValue("Hitpoints"));
            Assert.AreEqual("Flesh",a.GetPart<MaterialPart>()?.MaterialID);Assert.NotNull(a.GetPart<ThermalPart>());
            Assert.AreEqual(18,a.GetStatValue("Strength"));Assert.AreEqual(18,a.GetStatValue("Agility"));
            Assert.AreEqual("LongSword",BladeDefense.FindLongBlade(a).ParentEntity.BlueprintName);
            Assert.AreEqual(vault,a.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault"));
            Assert.AreEqual(profile==3,a.GetPart<SkillsPart>().HasSkill("AcrobaticsSkill"));
            Assert.AreEqual(profile==2?2:profile==3?0:1,a.GetPart<SkillsPart>().GetRank("LongBlades_Deflect"));
            Assert.AreEqual(2,a.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));
        }
        [TestCase(0,0)] [TestCase(0,1)] [TestCase(0,2)]
        [TestCase(1,0)] [TestCase(1,1)] [TestCase(1,2)]
        [TestCase(2,0)] [TestCase(2,1)] [TestCase(2,2)]
        [TestCase(3,0)] [TestCase(3,1)] [TestCase(3,2)]
        public void TrialIsFiniteDeterministicAndRestoresScheduler(int profile,int encounter)
        {
            var active=TurnManager.Active;var world=TurnManager.World;
            var reputation=PlayerReputation.GetAll();
            var a=ComposableBuildComparisonBench.RunTrial(factory,(ComposableBuildComparisonBench.Profile)profile,(ComposableBuildComparisonBench.Encounter)encounter,17,true);
            var b=ComposableBuildComparisonBench.RunTrial(factory,(ComposableBuildComparisonBench.Profile)profile,(ComposableBuildComparisonBench.Encounter)encounter,17,true);
            Assert.AreEqual(a.Damage,b.Damage);Assert.AreEqual(a.EndHp,b.EndHp);Assert.AreEqual(a.Actions,b.Actions);Assert.AreEqual(a.Outcome,b.Outcome);
            Assert.Greater(a.Actions,0);Assert.LessOrEqual(a.Actions,60);Assert.LessOrEqual(a.Tonics,2);
            Assert.AreSame(active,TurnManager.Active);Assert.AreSame(world,TurnManager.World);
            CollectionAssert.AreEquivalent(reputation,PlayerReputation.GetAll(),"laboratory kills must not change campaign reputation");
        }
        [Test] public void FullRegisteredComparisonRunsEveryDeclaredArm()
        {
            var z=new Zone("caller");var a=MultiCellAbilityConsumerTests.Owner(creature:true);z.AddEntity(a,10,10);
            var previous=TurnManager.Active;var turns=new TurnManager();
            try
            {
                var bench=new ComposableBuildComparisonBench();bench.Apply(new CavesOfOoo.Scenarios.ScenarioContext(z,factory,a,turns));
                Assert.AreEqual(42,bench.Cases);Assert.AreEqual(0,bench.Failures,string.Join("\n",bench.Audit));
                Assert.AreEqual(1344,bench.Trials.Count);Assert.AreSame(turns,TurnManager.Active);
                Assert.Greater(bench.Trials.Sum(r=>r.Parries),0);Assert.Greater(bench.Trials.Sum(r=>r.Counters),0);
                Assert.Greater(bench.Trials.Sum(r=>r.Decisions.TryGetValue("walk-off-ray",out int n)?n:0),0);
                Assert.Greater(bench.Trials.Sum(r=>r.Decisions.TryGetValue("tumble",out int n)?n:0),0,"control capability must be exercised");
            }
            finally{typeof(TurnManager).GetProperty("Active").SetValue(null,previous);}
        }
        [TestCase(false,1)] [TestCase(true,2)]
        public void ExperimentalUncappedChangesOnlyTheIntervalGate(bool uncapped,int issued)
        {
            using(var f=new FiftySecondCombatFixture())
            {
                var a=f.Actor(x:11);var d=f.Actor(new LongBlades_Deflect());FiftySecondCombatFixture.Learn(d,new LongBlades_Riposte());
                var budget=d.GetPart<RiposteAllowancePart>();
                var field=typeof(RiposteAllowancePart).GetField("ComparisonUncapped",BindingFlags.NonPublic|BindingFlags.Instance);
                Assert.NotNull(field);field.SetValue(budget,uncapped);
                var sword=BladeDefense.FindLongBlade(d);sword.PenBonus=3;
                for(int i=0;i<2;i++)CombatSystem.PerformSingleAttack(a,d,BladeDefense.FindLongBlade(a),true,f.Zone,new ComposableRiposteTests.Rolls(0,0));
                Assert.AreEqual(1000-issued,a.GetStatValue("Hitpoints"));Assert.True(budget.Spent);
                var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(d);
                Assert.False((bool)field.GetValue(loaded.GetPart<RiposteAllowancePart>()));Assert.True(loaded.GetPart<RiposteAllowancePart>().Spent);
            }
        }
        [TestCase(3,3)] [TestCase(50,5)]
        public void DamageReceiptReportsCommittedHpLossNotOverkill(int damage,int expected)
        {
            using(var f=new FiftySecondCombatFixture())
            {
                var a=f.Actor();var d=f.Target(hp:5);
                bool old=CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("damage");
                try
                {
                    if(!old)CavesOfOoo.Diagnostics.Diag.SetChannel("damage",true);
                    CombatSystem.ApplyDamage(d,new Damage(damage),a,f.Zone);
                    string payload=CavesOfOoo.Diagnostics.Diag.Snapshot(100).Last(e=>e.Kind=="DamageDealt"&&e.TargetId==d.ID).PayloadJson;
                    StringAssert.Contains("\"ActualDamage\":"+expected,payload);
                }
                finally{if(!old)CavesOfOoo.Diagnostics.Diag.SetChannel("damage",false);}
            }
        }
    }
}
