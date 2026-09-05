using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SealedLibraryBenchTests
    {
        [Test] public void DeterministicScenarioExercisesTheActualSealAndOpenControl()
        {
            var type=typeof(IScenario).Assembly.GetType("CavesOfOoo.Scenarios.Custom.SealedLibraryBench");Assert.IsNotNull(type);
            using(var harness=new ScenarioTestHarness())
            {
                var scenario=(IScenario)System.Activator.CreateInstance(type);scenario.Apply(harness.CreateContext(playerBlueprint:"Player"));
                Assert.AreEqual(0,type.GetProperty("Failures").GetValue(scenario));Assert.GreaterOrEqual((int)type.GetProperty("Cases").GetValue(scenario),6);
            }
        }
    }
}
