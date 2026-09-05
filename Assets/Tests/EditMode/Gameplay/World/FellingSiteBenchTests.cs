using System;
using NUnit.Framework;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    public sealed class FellingSiteBenchTests
    {
        [Test]
        public void DeterministicBenchAuditsExposureAndControlsAndCanRunAgain()
        {
            using(var harness=new ScenarioTestHarness())
            {
                var ctx=harness.CreateContext(playerBlueprint:"Player");var bench=new FellingSiteBench();bench.Apply(ctx);
                Assert.AreEqual(8,bench.Cases);Assert.AreEqual(0,bench.Failures);string first=bench.RunId;
                bench.Apply(ctx);Assert.AreNotEqual(first,bench.RunId);Assert.AreEqual(8,bench.Cases);Assert.AreEqual(0,bench.Failures);
            }
        }
        [Test]
        public void MissingSeventhCannotYieldAPassingNativeAudit()
        {
            using(var harness=new ScenarioTestHarness())
            {
                harness.Factory.Blueprints.Remove("SeventhPosition");
                Assert.Throws<InvalidOperationException>(()=>new FellingSiteBench().Apply(harness.CreateContext(playerBlueprint:"Player")));
            }
        }
    }
}
