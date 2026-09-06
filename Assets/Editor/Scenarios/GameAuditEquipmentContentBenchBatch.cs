
using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Isolated finite native launcher. No error exemptions or performance claim.</summary>
    [InitializeOnLoad]
    public static class GameAuditEquipmentContentBenchBatch
    {
        private const string Prefix = "GA03i.NativeBench.";
        static GameAuditEquipmentContentBenchBatch() { if (SessionState.GetBool(Prefix + "active", false)) Subscribe(); }
        public static void Run() => RunCore(true);
        public static void RunPerformanceBefore() => RunCore(true, "before");
        public static void RunPerformanceAfter() => RunCore(true, "after");
        [MenuItem("Caves Of Ooo/Scenarios/Items/Equipment Content Audit")]
        private static void Launch() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) RunCore(false); }
        [MenuItem("Caves Of Ooo/Scenarios/Items/Equipment Content Audit", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;
        private static void RunCore(bool exitEditor, string performanceMode = null)
        {
            string marker = "GA03i-marker-" + Guid.NewGuid().ToString("N");
            string token = NativeSaveIsolation.Begin(Prefix, marker, exitEditor);
            SessionState.SetString(Prefix + "performanceMode", performanceMode ?? "");
            SessionState.SetString(Prefix + "saveToken", token); SessionState.SetBool(Prefix + "active", true); SessionState.SetInt(Prefix + "errors", 0);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 240);
            try { Subscribe(); EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity"); EditorApplication.isPlaying = true; }
            catch (Exception ex) { Debug.LogError("[GameAuditEquipmentContentBench] Launch failed: " + ex); Finish(4); }
        }
        private static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix, SessionState.GetString(Prefix + "saveToken", ""), Finish);
            GameBootstrap.OnAfterBootstrap -= Apply; GameBootstrap.OnAfterBootstrap += Apply;
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        }
        private static void Apply(Zone zone, EntityFactory factory, Entity player, TurnManager turns)
        {
            GameBootstrap.OnAfterBootstrap -= Apply;
            new GameAuditEquipmentContentBench().Apply(new ScenarioContext(zone, factory, player, turns, 67));
            string mode = SessionState.GetString(Prefix + "performanceMode", "");
            if (mode.Length > 0)
            {
                var driver = UnityEngine.Object.FindFirstObjectByType<GameAuditEquipmentContentBenchPlayer>();
                if (driver == null) throw new InvalidOperationException("Missing combat performance driver.");
                driver.ConfigurePerformance(mode);
            }
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            var driver = UnityEngine.Object.FindFirstObjectByType<GameAuditEquipmentContentBenchPlayer>();
            if (driver != null && driver.Finished) { driver.SetUnexpectedErrors(SessionState.GetInt(Prefix + "errors", 0)); Finish(driver.Failures == 0 ? 0 : 1); return; }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0)) { Debug.LogError("[GameAuditEquipmentContentBench] Native audit timed out."); Finish(2); }
        }
        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) SessionState.SetInt(Prefix + "errors", SessionState.GetInt(Prefix + "errors", 0) + 1);
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Prefix + "active", false); GameBootstrap.OnAfterBootstrap -= Apply; EditorApplication.update -= Poll; Application.logMessageReceived -= OnLog;
            SaveGameService.RegisterRuntime(null, null); Debug.Log("[GameAuditEquipmentContentBench] Native capture exit=" + code);
            NativeSaveIsolation.Finish(Prefix, SessionState.GetString(Prefix + "saveToken", ""), code);
        }
    }
}
