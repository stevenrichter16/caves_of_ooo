using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Native batch Play launcher. Uses the production bootstrap
    /// event, with an Editor wall-clock deadline independent of game frames.</summary>
    [InitializeOnLoad]
    public static class GameAuditCraftingFlowBenchBatch
    {
        private const string Prefix = "FLOW1.NativeBench.";
        static GameAuditCraftingFlowBenchBatch() { if (SessionState.GetBool(Prefix + "active", false)) Subscribe(); }

        [MenuItem("Caves Of Ooo/Scenarios/Items/Crafting Flow Audit")]
        private static void Launch()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) RunPointer();
        }
        [MenuItem("Caves Of Ooo/Scenarios/Items/Crafting Flow Audit", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;

        public static void Run() { SessionState.SetBool(Prefix+"verify",true);SessionState.SetBool(Prefix+"pointer",false);RunCore(true); }
        public static void RunBaseline() { SessionState.SetBool(Prefix+"verify",false);SessionState.SetBool(Prefix+"pointer",false);RunCore(true); }
        public static void RunPointer() { SessionState.SetBool(Prefix+"verify",true);SessionState.SetBool(Prefix+"pointer",true);RunCore(false); }
        private static void RunCore(bool exitEditor)
        {
            string id = "FLOW1-bench-" + Guid.NewGuid().ToString("N");
            string slot = Path.Combine(Application.persistentDataPath, "Saves", id);
            Directory.CreateDirectory(slot);
            // An isolated marker prevents bootstrap autosaving this synthetic
            // arena. Native N dismisses the menu; this marker is never loaded.
            File.WriteAllBytes(Path.Combine(slot, "Quick.sav.gz"), Array.Empty<byte>());
            SessionState.SetBool(Prefix + "exitEditor", exitEditor);
            SessionState.SetString(Prefix + "slot", slot);
            SessionState.SetBool(Prefix + "hadPref", PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey));
            SessionState.SetString(Prefix + "oldPref", PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));
            PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey, id);
            SessionState.SetBool(Prefix + "active", true);
            // Desktop phases allow 540 seconds; include bootstrap/cleanup margin.
            float timeout = SessionState.GetBool(Prefix + "pointer", false) ? 600 : 360;
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + timeout);
            SessionState.SetInt(Prefix + "errors", 0);
            Subscribe();
            EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");
            EditorApplication.isPlaying = true;
        }

        private static void Subscribe()
        {
            GameBootstrap.OnAfterBootstrap -= Apply; GameBootstrap.OnAfterBootstrap += Apply;
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
            EditorApplication.playModeStateChanged -= OnPlayState; EditorApplication.playModeStateChanged += OnPlayState;
        }
        private static void Apply(Zone zone, EntityFactory factory, Entity player, TurnManager turns)
        {
            GameBootstrap.OnAfterBootstrap -= Apply;
            new GameAuditCraftingFlowBench { VerifyFixes = SessionState.GetBool(Prefix+"verify",true), ObservePointer = SessionState.GetBool(Prefix+"pointer",false) }
                .Apply(new ScenarioContext(zone, factory, player, turns, 63));
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            var driver = UnityEngine.Object.FindFirstObjectByType<GameAuditCraftingFlowBenchPlayer>();
            if (driver != null && driver.Finished)
            {
                Finish(driver.Failures + SessionState.GetInt(Prefix + "errors", 0) == 0 ? 0 : 1);
                return;
            }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0))
            { Debug.LogError("[GameAuditCraftingFlowBench] Native Play timed out before a complete audit and capture."); Finish(2); }
        }
        private static void OnPlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Prefix + "active", false))
                Finish(3); // Manual stop still restores preferences and deletes only the disposable slot.
        }
        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                SessionState.SetInt(Prefix + "errors", SessionState.GetInt(Prefix + "errors", 0) + 1);
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Prefix + "active", false);
            GameBootstrap.OnAfterBootstrap -= Apply; EditorApplication.update -= Poll; Application.logMessageReceived -= OnLog;
            EditorApplication.playModeStateChanged -= OnPlayState;
            SaveGameService.RegisterRuntime(null, null);
            string slot = SessionState.GetString(Prefix + "slot", "");
            if (!string.IsNullOrEmpty(slot) && Directory.Exists(slot)) Directory.Delete(slot, true);
            if (SessionState.GetBool(Prefix + "hadPref", false))
                PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey, SessionState.GetString(Prefix + "oldPref", ""));
            else PlayerPrefs.DeleteKey(SaveGameService.LastGameIDPrefsKey);
            EditorApplication.isPlaying = false;
            Debug.Log("[GameAuditCraftingFlowBench] Native capture exit=" + code);
            if (SessionState.GetBool(Prefix + "exitEditor", false)) EditorApplication.Exit(code);
        }
    }
}
