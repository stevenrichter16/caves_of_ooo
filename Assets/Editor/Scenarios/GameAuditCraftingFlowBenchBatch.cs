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
            string saveToken = NativeSaveIsolation.Begin(Prefix, id, exitEditor);
            SessionState.SetString(Prefix + "saveToken", saveToken);
            SessionState.SetBool(Prefix + "exitEditor", exitEditor);
            SessionState.SetBool(Prefix + "active", true);
            // Desktop phases allow 540 seconds; include bootstrap/cleanup margin.
            float timeout = SessionState.GetBool(Prefix + "pointer", false) ? 600 : 360;
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + timeout);
            SessionState.SetInt(Prefix + "errors", 0);
            try
            {
                Subscribe();
                EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");
                EditorApplication.isPlaying = true;
            }
            catch (Exception ex) { Debug.LogError("[NativeBench] Launch failed: " + ex); Finish(4); }
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
        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                SessionState.SetInt(Prefix + "errors", SessionState.GetInt(Prefix + "errors", 0) + 1);
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Prefix + "active", false);
            GameBootstrap.OnAfterBootstrap -= Apply; EditorApplication.update -= Poll; Application.logMessageReceived -= OnLog;
            SaveGameService.RegisterRuntime(null, null);
            Debug.Log("[GameAuditCraftingFlowBench] Native capture exit=" + code);
            NativeSaveIsolation.Finish(Prefix, SessionState.GetString(Prefix + "saveToken", ""), code);
        }
    }
}
