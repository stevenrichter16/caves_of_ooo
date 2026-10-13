using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class ComposableComparisonAdversarialTests
    {
        EntityFactory factory;
        [SetUp] public void Setup(){factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));SkillRegistry.ResetForTests();SkillRegistry.EnsureInitialized();}
        [TearDown] public void Cleanup()=>SkillRegistry.ResetForTests();
        [TestCase(-1)] [TestCase(0)] [TestCase(24)] [TestCase(201)] [TestCase(int.MaxValue)]
        public void Adversarial_InvalidSpeedRefusesBeforeCreatingPlayer(int speed)
        {
            var z=new Zone("bad-speed");Assert.Throws<ArgumentOutOfRangeException>(()=>ComposableBuildComparisonBench.CreatePlayer(factory,z,ComposableBuildComparisonBench.Profile.Counterfighter,false,speed));
            Assert.Zero(z.GetAllEntities().Count);
        }
        [TestCase(-1)] [TestCase(4)] [TestCase(99)]
        public void Adversarial_InvalidProfileCannotSilentlyBecomeControl(int profile)
        {
            var z=new Zone("bad-profile");Assert.Throws<ArgumentOutOfRangeException>(()=>ComposableBuildComparisonBench.CreatePlayer(factory,z,(ComposableBuildComparisonBench.Profile)profile,false));Assert.Zero(z.GetAllEntities().Count);
        }
        [TestCase(0,false)] [TestCase(1,false)] [TestCase(2,false)] [TestCase(3,false)]
        [TestCase(0,true)] [TestCase(1,true)] [TestCase(2,true)] [TestCase(3,true)]
        public void Adversarial_EachBuildRestoresGlobalsAndUsesActualSkills(int profile,bool vault)
        {
            var rep=PlayerReputation.GetAll();var active=TurnManager.Active;var loot=LootDropSystem.Factory;var lootRng=LootDropSystem.Rng;bool capture=Diag.DetailedCaptureEnabled,damage=Diag.IsChannelEnabled("damage"),ai=Diag.IsChannelEnabled("ai");
            var r=ComposableBuildComparisonBench.RunTrial(factory,(ComposableBuildComparisonBench.Profile)profile,ComposableBuildComparisonBench.Encounter.MixedLane,3,vault);
            CollectionAssert.AreEquivalent(rep,PlayerReputation.GetAll());Assert.AreSame(active,TurnManager.Active);Assert.AreSame(loot,LootDropSystem.Factory);Assert.AreSame(lootRng,LootDropSystem.Rng);
            Assert.AreEqual(capture,Diag.DetailedCaptureEnabled);Assert.AreEqual(damage,Diag.IsChannelEnabled("damage"));Assert.AreEqual(ai,Diag.IsChannelEnabled("ai"));
            Assert.Greater(r.Actions,0);Assert.LessOrEqual(r.Actions,60);Assert.LessOrEqual(r.Tonics,2);
            if(profile==1||profile==3)Assert.True(r.Decisions.ContainsKey("jet-blast"));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_UncappedStillRejectsReactiveAncestors(bool extraChild)
        {
            using(var f=new FiftySecondCombatFixture())
            {
                var a=f.Actor(x:11);var d=f.Actor(new LongBlades_Deflect());FiftySecondCombatFixture.Learn(d,new LongBlades_Riposte());
                typeof(RiposteAllowancePart).GetField("ComparisonUncapped",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(d.GetPart<RiposteAllowancePart>(),true);
                var parent=MeleeStrikeProvenance.Action(MeleeStrikeOrigin.ReactiveCounter).Child(MeleeStrikeOrigin.ReactiveCounter);
                if(extraChild)parent=parent.Child(MeleeStrikeOrigin.ExtraAttack);
                var rng=new ComposableRiposteTests.Rolls(0,0);
                CombatSystem.PerformSingleAttack(a,d,BladeDefense.FindLongBlade(a),true,f.Zone,rng,provenance:parent);
                Assert.False(d.GetPart<RiposteAllowancePart>().Spent);Assert.AreEqual(1,rng.Draws);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_ReservedStillRejectsWithEitherCapMode(bool uncapped)
        {
            using(var f=new FiftySecondCombatFixture())
            {
                var a=f.Actor(x:11);var d=f.Actor(new LongBlades_Deflect());FiftySecondCombatFixture.Learn(d,new LongBlades_Riposte());var budget=d.GetPart<RiposteAllowancePart>();
                typeof(RiposteAllowancePart).GetField("ComparisonUncapped",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(budget,uncapped);
                typeof(RiposteAllowancePart).GetField("Reserved",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(budget,true);
                var rng=new ComposableRiposteTests.Rolls(0,0);CombatSystem.PerformSingleAttack(a,d,BladeDefense.FindLongBlade(a),true,f.Zone,rng);
                Assert.False(budget.Spent);Assert.AreEqual(1,rng.Draws);
            }
        }
    }
}
