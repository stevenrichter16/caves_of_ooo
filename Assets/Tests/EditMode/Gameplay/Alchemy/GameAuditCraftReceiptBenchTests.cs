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
    public class GameAuditCraftReceiptBenchTests
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
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditCraftReceiptBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Craft receipt audit must be launchable."); return (IScenario)Activator.CreateInstance(type);
        }
        [Test] public void ArenaHasRealRepeatedCraftsAndCapacityRetry()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>(); Assert.AreEqual(2, inv.Objects.Count); Assert.AreEqual(16, inv.MaxWeight); Assert.AreEqual(6, inv.GetCarriedWeight()); Assert.IsEmpty(inv.EquippedItems);
            var dagger = inv.Objects.Single(e => e.BlueprintName == "Dagger"); var brine = inv.Objects.Single(e => e.BlueprintName == "GlimmerBrine");
            Assert.AreEqual(1, dagger.GetPart<StackerPart>().StackCount); Assert.AreEqual(2, brine.GetPart<StackerPart>().StackCount); Assert.AreSame(ctx.PlayerEntity, dagger.GetPart<PhysicsPart>().InInventory);
            CollectionAssert.AreEqual(new[] { brine }, CraftingMarkPart.CollectMarked(ctx.PlayerEntity).Reagents);
            var bits = ctx.PlayerEntity.GetPart<BitLockerPart>(); Assert.AreEqual(3, bits.GetBitCount('B')); Assert.AreEqual(3, bits.GetBitCount('C')); CollectionAssert.AreEquivalent(new[] { "craft_dagger" }, bits.GetKnownRecipes());
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("craft_dagger", out var recipe)); Assert.AreEqual("BC", recipe.Cost); Assert.AreEqual(1, recipe.NumberMade);
            Assert.IsFalse(ctx.Zone.GetAllEntities().Any(e => e != ctx.PlayerEntity && e.HasPart<BrainPart>()));
        }
        [TestCase("Dagger")] [TestCase("GlimmerBrine")] [TestCase("BrewedTonic")] [TestCase("StoneFloor")]
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
            Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario")); Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "CraftReceiptNativeAudit" }).Count);
        }
    }
}
