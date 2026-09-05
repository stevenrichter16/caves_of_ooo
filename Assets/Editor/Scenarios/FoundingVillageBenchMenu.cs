using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;

namespace CavesOfOoo.Editor
{
    public static class FoundingVillageBenchMenu
    {
        [MenuItem("Caves of Ooo/Scenarios/World/Olderdeep Founding Encounter")]
        public static void Launch() => ScenarioRunner.Launch<FoundingVillageBench>();
    }
}
