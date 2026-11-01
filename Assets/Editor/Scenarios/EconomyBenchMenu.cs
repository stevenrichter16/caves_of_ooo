using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;

namespace CavesOfOoo.Editor
{
    public static class EconomyBenchMenu
    {
        [MenuItem("Caves of Ooo/Scenarios/Economy/Economy Barter Bench")]
        public static void LaunchBench() => ScenarioRunner.Launch<EconomyBarterBench>();

        [MenuItem("Caves of Ooo/Scenarios/Economy/Barter Screen Fixture")]
        public static void LaunchFixture() => ScenarioRunner.Launch<BarterScreenFixture>();
    }
}
