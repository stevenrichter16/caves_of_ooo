using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditConsumableBenchTests
    {
        private static IScenario Bench()
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a =>
                a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditConsumablesBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Native consumable audit must be launchable as a scenario.");
            return (IScenario)Activator.CreateInstance(type);
        }
        [Test]
        public void Arena_StagesOneAdjacentRealTonicAndEmptyPack()
        {
            using var harness = new ScenarioTestHarness();
            var ctx = harness.CreateContext(playerBlueprint: "Player");
            ctx.PlayerEntity.GetPart<InventoryPart>().AddObject(harness.Factory.CreateEntity("Dagger"));
            var bench = Bench(); bench.Apply(ctx);
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
            var tonic = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "HealingTonic");
            Assert.AreEqual((21, 12), ctx.Zone.GetEntityPosition(tonic));
            Assert.AreEqual(1, tonic.GetPart<StackerPart>().StackCount);
            Assert.IsEmpty(ctx.PlayerEntity.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(10, ctx.PlayerEntity.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, (int)bench.GetType().GetProperty("Cases").GetValue(bench),
                "Staging alone must never claim a successful native input audit.");
        }
        [TestCase(false)] [TestCase(true)]
        public void NativeCase_EmitsRecordAndRestoresPriorChannelPreference(bool enabled)
        {
            Diag.ResetAll();
            try
            {
                Diag.SetChannel("scenario", enabled);
                var bench = Bench();
                bench.GetType().GetMethod("Check").Invoke(bench, new object[] { "probe", true });
                Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "ConsumableNativeAudit" }).Count);
                Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario"));
            }
            finally { Diag.ResetAll(); }
        }

        [TestCase("HealingTonic")] [TestCase("StoneFloor")]
        public void MissingRequiredContent_RejectsBeforeClearingArena(string missing)
        {
            using var harness = new ScenarioTestHarness();
            var ctx = harness.CreateContext(playerBlueprint: "Player");
            harness.Factory.Blueprints.Remove(missing);
            var bench = Bench();
            Assert.Throws<InvalidOperationException>(() => bench.Apply(ctx));
            Assert.AreEqual((40, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
        }
    }
}
