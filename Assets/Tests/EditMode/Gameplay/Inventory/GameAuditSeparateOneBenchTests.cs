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
    public class GameAuditSeparateOneBenchTests
    {
        private TurnManager _previousTurns;
        [SetUp] public void CaptureGlobals() { _previousTurns = TurnManager.Active; }
        private static IScenario Bench()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditSeparateOneBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Separate one audit must be launchable."); return (IScenario)Activator.CreateInstance(type);
        }
        [TearDown] public void Cleanup()
        {
            TinkerRecipeRegistry.ResetForTests(); Diag.ResetAll();
            typeof(TurnManager).GetProperty("Active", BindingFlags.Public | BindingFlags.Static).SetMethod.Invoke(null, new object[] { _previousTurns });
        }
        [Test] public void ArenaStartsWithThreeUnmodifiedDaggersEmptyEquipmentAndPaidSharpBits()
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>(); var source = inv.Objects.Single();
            Assert.AreEqual("Dagger", source.BlueprintName); Assert.AreEqual(3, source.GetPart<StackerPart>().StackCount);
            Assert.IsFalse(source.HasTag("ModSharp")); Assert.IsFalse(inv.GetAllEquipped().Any());
            var bits = ctx.PlayerEntity.GetPart<BitLockerPart>(); Assert.AreEqual(1, bits.GetBitCount('B')); Assert.AreEqual(1, bits.GetBitCount('C'));
            Assert.IsTrue(bits.GetKnownRecipes().Contains("mod_sharp_melee"));
        }
        [Test] public void ArenaHasUsableGroundAndPlayerPosition()
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
            Assert.IsTrue(ctx.Zone.GetCell(20, 12).Objects.Any(e => e.BlueprintName == "StoneFloor"));
        }
        [TestCase("Dagger")] [TestCase("StoneFloor")]
        public void MissingContentRefusesBeforeChangingInventoryOrPosition(string missing)
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player");
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>(); var item = harness.Factory.CreateEntity("Torch"); Assert.IsTrue(inv.AddObject(item)); var contents = inv.Objects.ToArray();
            harness.Factory.Blueprints.Remove(missing); Assert.Throws<InvalidOperationException>(() => Bench().Apply(ctx));
            CollectionAssert.AreEqual(contents, inv.Objects); Assert.AreEqual((40, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
        }
        [TestCase(false)] [TestCase(true)] public void AuditPreservesDiagnosticPreferences(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario", enabled); var bench = Bench();
            bench.GetType().GetMethod("Check").Invoke(bench, new object[] { "probe", true });
            Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario"));
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "SeparateOneNativeAudit" }).Count);
        }
    }
}
