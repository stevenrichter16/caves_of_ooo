using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;

namespace CavesOfOoo.Editor
{
    internal static class MeteredHydrationScenarioMenu
    {
        [MenuItem("Caves Of Ooo/Scenarios/Home/Finite Water Trial (isolated)", priority = 408)]
        static void Launch()
        {
            if (EditorApplication.isPlaying || CavesOfOoo.Core.SaveGameService.SaveRootOverride != null)
                throw new System.InvalidOperationException("Stop the current owned session before starting a water trial.");
            NativeSaveIsolation.Begin("Codex.MeteredHydration.", "MeteredHydrationStart", false);
            ScenarioRunner.LaunchWild<MeteredHydrationShowcase>();
        }
    }
}
