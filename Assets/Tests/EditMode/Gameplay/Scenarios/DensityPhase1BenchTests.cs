using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>Smoke of the numeric scenario only. Native key routing, bootstrap,
    /// screenshot and save-isolated shutdown require the editor launcher.</summary>
    public class DensityPhase1BenchTests
    {
        private ScenarioTestHarness _harness;
        private EntityFactory _oldContainerFactory;
        [SetUp]
        public void Setup()
        {
            _harness = new ScenarioTestHarness();
            _oldContainerFactory = ContainerPlacementService.Factory;
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Loot/LootTables.json")));
            Diag.ResetAll();
        }
        [TearDown]
        public void Cleanup()
        {
            ContainerPlacementService.Factory = _oldContainerFactory;
            LootTableRegistry.ResetForTests();
            _harness.Dispose();
            Diag.ResetAll();
        }
        private static IScenario NewBench()
        {
            var type = typeof(IScenario).Assembly.GetType("CavesOfOoo.Scenarios.Custom.DensityPhase1Bench");
            Assert.IsNotNull(type, "Density Phase 1 must have a reusable audit scenario");
            return (IScenario)Activator.CreateInstance(type);
        }
        private static T Read<T>(IScenario bench, string name) => (T)bench.GetType().GetProperty(name).GetValue(bench);

        [Test]
        public void Smoke_RealFactoryMatrix_CompletesWithRunStampedMeasurements()
        {
            var bench = NewBench();
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            var playerPosition = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            Assert.DoesNotThrow(() => bench.Apply(ctx));
            Assert.AreEqual(0, Read<int>(bench, "Failures"));
            Assert.GreaterOrEqual(Read<int>(bench, "Cases"), 12);
            string runId = Read<string>(bench, "RunId");
            var records = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "scenario", Kind = "DensityPhase1Case", Limit = 100 }).Records;
            Assert.AreEqual(Read<int>(bench, "Cases"), records.Count);
            Assert.IsTrue(records.All(r => r.PayloadJson.Contains(runId)));
            Assert.AreEqual(playerPosition, ctx.Zone.GetEntityPosition(ctx.PlayerEntity), "numeric smoke does not stage or teleport the player");
        }

        [Test]
        public void Smoke_RerunGetsAFreshMarker_AndDoesNotAccumulateCases()
        {
            var bench = NewBench();
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            bench.Apply(ctx);
            string first = Read<string>(bench, "RunId");
            int count = Read<int>(bench, "Cases");
            bench.Apply(ctx);
            Assert.AreNotEqual(first, Read<string>(bench, "RunId"));
            Assert.AreEqual(count, Read<int>(bench, "Cases"));
            Assert.AreEqual(0, Read<int>(bench, "Failures"));
        }

        [Test]
        public void Smoke_MissingContent_IsALoudFailureRatherThanANeutralPass()
        {
            var bench = NewBench();
            var existing = _harness.CreateContext(playerBlueprint: "Player");
            var ctx = new ScenarioContext(existing.Zone, new EntityFactory(), existing.PlayerEntity, existing.Turns);
            Assert.Throws<InvalidOperationException>(() => bench.Apply(ctx));
            Assert.Greater(Read<int>(bench, "Failures"), 0);
            var failures = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "scenario", Kind = "DensityPhase1Skipped", Limit = 10 }).Records;
            Assert.AreEqual(1, failures.Count);
            StringAssert.Contains(Read<string>(bench, "RunId"), failures[0].PayloadJson);
        }
    }
}
