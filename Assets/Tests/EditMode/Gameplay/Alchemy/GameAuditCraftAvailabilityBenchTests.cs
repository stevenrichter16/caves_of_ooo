using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditCraftAvailabilityBenchTests
    {
        private TurnManager _previousTurns;
        [SetUp] public void Capture() { _previousTurns = TurnManager.Active; }
        [TearDown] public void Cleanup()
        {
            typeof(TurnManager).GetProperty("Active", BindingFlags.Public | BindingFlags.Static).SetMethod.Invoke(null, new object[] { _previousTurns });
            Diag.ResetAll();
        }
        private static IScenario Bench()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditCraftAvailabilityBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Craft availability audit must be launchable."); return (IScenario)Activator.CreateInstance(type);
        }
        [Test] public void ArenaHasActualStaleMixAndLaterPositiveIngredient()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
            var still = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "AlchemyStill"); Assert.AreEqual((21, 12), ctx.Zone.GetEntityPosition(still));
            var inventory = ctx.PlayerEntity.GetPart<InventoryPart>(); Assert.AreEqual(5, inventory.Objects.Count);
            var salt = inventory.Objects.Where(e => e.BlueprintName == "PaleSalt").ToArray(); Assert.AreEqual(2, salt.Length); Assert.AreEqual(0, salt[0].GetPart<StackerPart>().StackCount); Assert.AreEqual(2, salt[1].GetPart<StackerPart>().StackCount);
            var picks = CraftingMarkPart.CollectMarked(ctx.PlayerEntity).Reagents; Assert.AreEqual(2, picks.Count);
            Assert.AreEqual(0, picks.Single(e => e.BlueprintName == "SparkRoot").GetPart<StackerPart>().StackCount); Assert.IsFalse(BrewingService.PreviewBrew(picks).IsValid);
            Assert.IsFalse(ctx.Zone.GetAllEntities().Any(e => e != ctx.PlayerEntity && e.HasPart<BrainPart>()));
        }
        [TestCase("Dagger")] [TestCase("GlimmerBrine")] [TestCase("SparkRoot")]
        [TestCase("PaleSalt")] [TestCase("AlchemyStill")] [TestCase("StoneFloor")]
        public void MissingContentRefusesBeforeMutatingPlayerAndZone(string missing)
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player");
            var entities = ctx.Zone.GetAllEntities().ToArray(); h.Factory.Blueprints.Remove(missing);
            Assert.Throws<InvalidOperationException>(() => Bench().Apply(ctx));
            CollectionAssert.AreEquivalent(entities, ctx.Zone.GetAllEntities()); Assert.AreEqual((40, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
        }
        [TestCase(false)] [TestCase(true)] public void AuditPreservesDiagnosticPreferences(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario", enabled); var bench = Bench();
            bench.GetType().GetMethod("Check").Invoke(bench, new object[] { "probe", true });
            Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario")); Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "CraftAvailabilityNativeAudit" }).Count);
        }
    }
}
