using UnityEditor;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
namespace CavesOfOoo.Editor
{
    public static class SealedLibraryBenchMenu
    {
        [MenuItem("Caves of Ooo/Scenarios/World/Sealed Library Access Audit")]
        public static void Run() => ScenarioRunner.Launch<SealedLibraryBench>();
    }
}
