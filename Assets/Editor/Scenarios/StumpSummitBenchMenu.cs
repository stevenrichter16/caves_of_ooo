using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;

namespace CavesOfOoo.Editor
{
    public static class StumpSummitBenchMenu
    {
        [MenuItem("Caves of Ooo/Scenarios/World/Stump Summit and Sima Audit")]
        public static void Launch() => ScenarioRunner.Launch<StumpSummitBench>();
    }
}
