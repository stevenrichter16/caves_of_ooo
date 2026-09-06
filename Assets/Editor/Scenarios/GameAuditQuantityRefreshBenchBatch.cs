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
    public static class GameAuditQuantityRefreshBenchBatch
    {
        private const string Prefix = "GA02f.NativeBench.";
        static GameAuditQuantityRefreshBenchBatch() { if (SessionState.GetBool(Prefix + "active", false)) Subscribe(); }

        [MenuItem("Caves Of Ooo/Scenarios/Items/Carried Quantity Audit")]
        private static void Launch()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) RunCore(false);
        }
        [MenuItem("Caves Of Ooo/Scenarios/Items/Carried Quantity Audit", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;

        public static void Run() => RunCore(true);
        private static void RunCore(bool exitEditor)
        {
            string id = "GA02f-bench-" + Guid.NewGuid().ToString("N");
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
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 180);
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
            new GameAuditQuantityRefreshBench()
                .Apply(new ScenarioContext(zone, factory, player, turns, 63));
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            var driver = UnityEngine.Object.FindFirstObjectByType<GameAuditQuantityRefreshBenchPlayer>();
            if (driver != null && driver.Finished)
            {
                Finish(driver.Failures + SessionState.GetInt(Prefix + "errors", 0) == 0 ? 0 : 1);
                return;
            }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0))
            { Debug.LogError("[GameAuditQuantityRefreshBench] Native Play timed out before a complete audit and capture."); Finish(2); }
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
            Debug.Log("[GameAuditQuantityRefreshBench] Native capture exit=" + code);
            if (SessionState.GetBool(Prefix + "exitEditor", false)) EditorApplication.Exit(code);
        }
    }
}
