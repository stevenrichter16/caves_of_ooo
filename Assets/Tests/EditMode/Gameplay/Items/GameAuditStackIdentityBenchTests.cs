using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditStackIdentityBenchTests
    {
        private static IScenario Bench()
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditStackIdentityBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Stack identity audit must be a launchable native scenario."); return (IScenario)Activator.CreateInstance(type);
        }
        private CavesOfOoo.Data.EntityFactory _oldFactory;
        [SetUp] public void Setup() { _oldFactory = AlchemyStillPart.Factory; }
        [TearDown] public void Cleanup() { AlchemyStillPart.Factory = _oldFactory; Diag.ResetAll(); EnhancementFactory.ForceReinitialize(); }
        [Test] public void ArenaStagesActualRecipesAndAnAdjacentStill()
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>(); Assert.AreEqual(150, inv.MaxWeight);
            CollectionAssert.AreEqual(new[] { "GlimmerBrine", "SparkRoot", "MendleafSprig", "EmberFruit", "CandyHeartRoot", "PaleSalt" }, inv.Objects.Select(i => i.BlueprintName));
            CollectionAssert.AreEqual(new[] { 3, 2, 2, 1, 2, 4 }, inv.Objects.Select(i => i.GetPart<StackerPart>().StackCount));
            Assert.AreEqual((21, 12), ctx.Zone.GetEntityPosition(ctx.Zone.GetAllEntities().Single(i => i.BlueprintName == "AlchemyStill")));
            Assert.IsTrue(ctx.PlayerEntity.GetPart<BitLockerPart>().KnowsRecipe("mod_palesalt_infuse"));
            Assert.IsFalse(inv.Objects.Any(i => i.BlueprintName == "BrewedTonic"));
        }
        [Test] public void ArenaLeavesPlainSingletonDaggersForSequentialPickupAndOccupiesBothHands()
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            var daggers = ctx.Zone.GetAllEntities().Where(i => i.BlueprintName == "Dagger").ToArray(); Assert.AreEqual(3, daggers.Length);
            CollectionAssert.AreEquivalent(new[] { (20, 12), (20, 13), (20, 14) }, daggers.Select(ctx.Zone.GetEntityPosition));
            Assert.IsTrue(daggers.All(i => i.GetPart<StackerPart>().StackCount == 1 && i.GetPart<StackerPart>().MaxStack == 99 && !i.Parts.OfType<IItemEnhancement>().Any()));
            var equipped = ctx.PlayerEntity.GetPart<InventoryPart>().GetAllEquipped(); Assert.AreEqual(1, equipped.Count); Assert.AreEqual("Warhammer", equipped[0].BlueprintName);
            Assert.AreEqual(2, ctx.PlayerEntity.GetPart<Body>().GetParts().Count(p => p._Equipped == equipped[0]));
        }
        [TestCase("AlchemyStill")] [TestCase("Dagger")] [TestCase("GlimmerBrine")]
        public void MissingContentRefusesBeforeArenaChanges(string missing)
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player"); harness.Factory.Blueprints.Remove(missing);
            Assert.Throws<InvalidOperationException>(() => Bench().Apply(ctx)); Assert.AreEqual((40, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
        }
        [TestCase(false)] [TestCase(true)] public void ObservationPreservesDiagnosticPreference(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario", enabled); var bench = Bench();
            bench.GetType().GetMethod("Check").Invoke(bench, new object[] { "probe", true });
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "StackIdentityNativeAudit" }).Count);
            Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario"));
        }
    }
}
