using System;
using NUnit.Framework;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    public sealed class StumpSummitBenchTests
    {
        [Test]
        public void BenchAuditsBothRetreatAndNestControlsWithANewRunStamp()
        {
            using (var harness = new ScenarioTestHarness())
            {
                var bench = new StumpSummitBench();
                bench.Apply(harness.CreateContext());
                Assert.AreEqual(0, bench.Failures);
                Assert.AreEqual(6, bench.Cases);
                string first = bench.RunId;
                bench.Apply(harness.CreateContext());
                Assert.AreNotEqual(first, bench.RunId);
                Assert.AreEqual(0, bench.Failures);
            }
        }

        [Test]
        public void MissingCoverBlueprintCannotProduceAFalsePassingAudit()
        {
            using (var harness = new ScenarioTestHarness())
            {
                harness.Factory.Blueprints.Remove("TankBrocchinia");
                var bench = new StumpSummitBench();
                Assert.Throws<InvalidOperationException>(() => bench.Apply(harness.CreateContext()));
            }
        }
    }
}
