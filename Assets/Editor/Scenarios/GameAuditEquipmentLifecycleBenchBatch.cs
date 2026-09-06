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
    public static class GameAuditEquipmentLifecycleBenchBatch
    {
        private const string Prefix = "GA03f.NativeBench.";
        static GameAuditEquipmentLifecycleBenchBatch() { if (SessionState.GetBool(Prefix + "active", false)) Subscribe(); }
        public static void Run() => RunCore(true);
        [MenuItem("Caves Of Ooo/Scenarios/Items/Equipment Lifecycle Audit")]
        private static void Launch() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) RunCore(false); }
        [MenuItem("Caves Of Ooo/Scenarios/Items/Equipment Lifecycle Audit", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;
        private static void RunCore(bool exitEditor)
        {
            string marker = "GA03f-marker-" + Guid.NewGuid().ToString("N");
            string token = NativeSaveIsolation.Begin(Prefix, marker, exitEditor);
            SessionState.SetString(Prefix + "saveToken", token); SessionState.SetBool(Prefix + "active", true); SessionState.SetInt(Prefix + "errors", 0);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 180);
            try { Subscribe(); EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity"); EditorApplication.isPlaying = true; }
            catch (Exception ex) { Debug.LogError("[GameAuditEquipmentLifecycleBench] Launch failed: " + ex); Finish(4); }
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
            new GameAuditEquipmentLifecycleBench().Apply(new ScenarioContext(zone, factory, player, turns, 67));
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            var driver = UnityEngine.Object.FindFirstObjectByType<GameAuditEquipmentLifecycleBenchPlayer>();
            if (driver != null && driver.Finished) { driver.SetUnexpectedErrors(SessionState.GetInt(Prefix + "errors", 0)); Finish(driver.Failures == 0 ? 0 : 1); return; }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0)) { Debug.LogError("[GameAuditEquipmentLifecycleBench] Native audit timed out."); Finish(2); }
        }
        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) SessionState.SetInt(Prefix + "errors", SessionState.GetInt(Prefix + "errors", 0) + 1);
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Prefix + "active", false); GameBootstrap.OnAfterBootstrap -= Apply; EditorApplication.update -= Poll; Application.logMessageReceived -= OnLog;
            SaveGameService.RegisterRuntime(null, null); Debug.Log("[GameAuditEquipmentLifecycleBench] Native capture exit=" + code);
            NativeSaveIsolation.Finish(Prefix, SessionState.GetString(Prefix + "saveToken", ""), code);
        }
    }
}
