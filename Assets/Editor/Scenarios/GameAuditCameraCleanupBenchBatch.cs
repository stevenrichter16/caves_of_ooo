using System;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CavesOfOoo.Editor
{
    /// <summary>Standalone native lifecycle audit in an unsaved empty scene. No game/save binding.</summary>
    [InitializeOnLoad]
    public static class GameAuditCameraCleanupBenchBatch
    {
        private const string Prefix = "GA03a.NativeBench.";
        static GameAuditCameraCleanupBenchBatch() { if (SessionState.GetBool(Prefix + "active", false)) Subscribe(); }
        [MenuItem("Caves Of Ooo/Scenarios/Rendering/Camera Cleanup Audit")]
        private static void Launch() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) RunCore(false); }
        [MenuItem("Caves Of Ooo/Scenarios/Rendering/Camera Cleanup Audit", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;
        public static void Run() => RunCore(true);
        private static void RunCore(bool exitEditor)
        {
            SessionState.SetBool(Prefix + "exit", exitEditor); SessionState.SetBool(Prefix + "active", true); SessionState.SetInt(Prefix + "errors", 0);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 180);
            Subscribe(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); EditorApplication.isPlaying = true;
        }
        private static void Subscribe()
        {
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged -= OnState; EditorApplication.playModeStateChanged += OnState;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        }
        private static void OnState(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) new GameObject("Camera Cleanup Native Audit").AddComponent<GameAuditCameraCleanupBenchPlayer>();
            if (state == PlayModeStateChange.EnteredEditMode) Finish(3);
        }
        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert || message.Contains("[SpellFX] Cancelled presentation"))
                SessionState.SetInt(Prefix + "errors", SessionState.GetInt(Prefix + "errors", 0) + 1);
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            var driver = UnityEngine.Object.FindFirstObjectByType<GameAuditCameraCleanupBenchPlayer>();
            if (driver != null && driver.Finished) { Finish(driver.Failures + SessionState.GetInt(Prefix + "errors", 0) == 0 ? 0 : 1); return; }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0)) { Debug.LogError("Camera cleanup native audit timed out."); Finish(2); }
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Prefix + "active", false); EditorApplication.update -= Poll; EditorApplication.playModeStateChanged -= OnState; Application.logMessageReceived -= OnLog;
            EditorApplication.isPlaying = false; Debug.Log("[CameraCleanupAudit] Native capture exit=" + code);
            if (SessionState.GetBool(Prefix + "exit", false)) EditorApplication.Exit(code);
        }
    }
}
