using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;

namespace CavesOfOoo.Editor
{
    public static class FellingSiteBenchMenu
    {
        [MenuItem("Caves of Ooo/Scenarios/World/Felling-Site Exposure Audit")]
        public static void Launch() => ScenarioRunner.Launch<FellingSiteBench>();
    }
}
