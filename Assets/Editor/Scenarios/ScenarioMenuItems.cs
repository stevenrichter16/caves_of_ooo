using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor.Scenarios
{
    /// <summary>
    /// Centralized menu stubs for every scenario. Adding a new scenario requires
    /// writing the <c>IScenario</c> class AND adding a 2-line entry here. Unity's
    /// <c>[MenuItem]</c> is compile-time and cannot be generated from attributes,
    /// so this manual approach is the simplest path to working menu items.
    ///
    /// Conventions:
    /// - Menu path: "Caves Of Ooo/Scenarios/&lt;Category&gt;/&lt;Name&gt;" (matches existing
    ///   project menu style under "Caves Of Ooo/Diagnostics/...").
    /// - Keyboard shortcuts: append <c>" %#&lt;key&gt;"</c> (Cmd+Shift+key) to the path
    ///   for scenarios you run frequently. Reserve <c>%#r</c> for the Re-run Last
    ///   action (defined below).
    /// - Keep entries grouped by category to match menu structure.
    ///
    /// If this file grows past ~50 entries, consider auto-generating it via a
    /// <c>Tools &gt; Regenerate Scenario Menu</c> command. Not built yet.
    /// </summary>
    internal static class ScenarioMenuItems
    {
        // =========================================================
        // Accelerators — always present
        // =========================================================

        [MenuItem("Caves Of Ooo/Scenarios/↻ Re-run Last Scenario %#r", priority = 10)]
        private static void ReRunLastScenario()
        {
            if (ScenarioRunner.LastLaunched == null)
            {
                Debug.LogWarning("[Scenario] No previously-launched scenario to re-run. Launch one first from the Scenarios menu.");
                return;
            }
            ScenarioRunner.Launch(ScenarioRunner.LastLaunched);
        }

        [MenuItem("Caves Of Ooo/Scenarios/↻ Re-run Last Scenario %#r", validate = true)]
        private static bool ReRunLastScenario_Validate() => ScenarioRunner.LastLaunched != null;

        [MenuItem("Caves Of Ooo/Scenarios/⏹ Stop Play Mode", priority = 11)]
        private static void StopPlayMode()
        {
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
        }

        [MenuItem("Caves Of Ooo/Scenarios/⏹ Stop Play Mode", validate = true)]
        private static bool StopPlayMode_Validate() => EditorApplication.isPlaying;

        // =========================================================
        // Combat Stress
        // =========================================================

        [MenuItem("Caves Of Ooo/Scenarios/Combat Stress/Five Snapjaw Ambush", priority = 100)]
        private static void Launch_FiveSnapjawAmbush()
            => ScenarioRunner.Launch<FiveSnapjawAmbush>();

        [MenuItem("Caves Of Ooo/Scenarios/Combat Stress/Snapjaw Ring Ambush (x8)", priority = 101)]
        private static void Launch_SnapjawRingAmbush()
            => ScenarioRunner.Launch<SnapjawRingAmbush>();

        [MenuItem("Caves Of Ooo/Scenarios/Combat Stress/Personally-hostile Stout Snapjaw", priority = 102)]
        private static void Launch_StoutSnapjaw()
            => ScenarioRunner.Launch<StoutSnapjaw>();

        // =========================================================
        // AI Behavior
        // =========================================================

        [MenuItem("Caves Of Ooo/Scenarios/AI Behavior/Wounded Warden", priority = 200)]
        private static void Launch_WoundedWarden()
            => ScenarioRunner.Launch<WoundedWarden>();

        // =========================================================
        // Content Demo
        // =========================================================

        [MenuItem("Caves Of Ooo/Scenarios/Content Demo/Mimic Surprise", priority = 300)]
        private static void Launch_MimicSurprise()
            => ScenarioRunner.Launch<MimicSurprise>();

        // =========================================================
        // Baseline
        // =========================================================

        [MenuItem("Caves Of Ooo/Scenarios/Baseline/Empty Starting Zone", priority = 400)]
        private static void Launch_EmptyStartingZone()
            => ScenarioRunner.Launch<EmptyStartingZone>();

        // =========================================================
        // Mutations
        // =========================================================

        [MenuItem("Caves Of Ooo/Scenarios/Mutations/Calm Test Setup (M2-ready)", priority = 500)]
        private static void Launch_CalmTestSetup()
            => ScenarioRunner.Launch<CalmTestSetup>();
    }
}
