using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CavesOfOoo.Editor
{
    /// <summary>Finite isolated first-hour staged viper launcher. Owns its bootstrap seed and
    /// restores the preceding scene setup, input settings, save root and prefs.</summary>
    [InitializeOnLoad]
    public static class FirstHourViperNativeBatch
    {
        private const string Prefix = "FirstHourViper.Native.";
        static FirstHourViperNativeBatch()
        {
            if (SessionState.GetBool(Prefix + "active", false)) Subscribe();
            if (SessionState.GetBool(Prefix + "restoreScenes", false)) AwaitSceneRestore();
        }
        public static void Run() => LaunchCore(true);
        [MenuItem("Caves Of Ooo/Scenarios/World/First-Hour Staged Viper Native Audit")]
        public static void Launch() => LaunchCore(false);
        [MenuItem("Caves Of Ooo/Scenarios/World/First-Hour Staged Viper Native Audit", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void LaunchCore(bool exitEditor)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play before launching the first-hour staged viper audit.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save scene edits before launching; the first-hour staged viper audit will not discard them.");
            var snapshot = EditorSceneManager.GetSceneManagerSetup().Select(s => new SceneRow
                { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray();
            if (snapshot.Any(s => string.IsNullOrEmpty(s.path)))
                throw new InvalidOperationException("Save untitled scenes before launching the first-hour staged viper audit.");
            string token = NativeSaveIsolation.Begin(Prefix, "Density-first-hour staged viper-marker-" + Guid.NewGuid().ToString("N"), exitEditor);
            SessionState.SetString(Prefix + "token", token);
            SessionState.SetString(Prefix + "scenes", JsonUtility.ToJson(new SceneRows { rows = snapshot }));
            SessionState.SetString(Prefix + "oldStartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) ?? "");
            SessionState.SetInt(Prefix + "oldSeed", NativeAuditBootstrapSettings.RequestedSeed);
            SessionState.SetInt(Prefix + "seed", 1);
            SessionState.SetBool(Prefix + "active", true);
            SessionState.SetBool(Prefix + "restoreScenes", false);
            SessionState.SetBool(Prefix + "exitEditor", exitEditor);
            SessionState.SetInt(Prefix + "errors", 0);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 660);
            try
            {
                Subscribe();
                EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");
                EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main/SampleScene.unity");
                EditorApplication.isPlaying = true;
            }
            catch (Exception error) { Debug.LogError("[FirstHourViperNative] Launch failed: " + error); Finish(4); }
        }
        private static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix, SessionState.GetString(Prefix + "token", ""), Finish);
            NativeAuditBootstrapSettings.RequestedSeed = SessionState.GetInt(Prefix + "seed", 0);
            GameBootstrap.OnAfterBootstrap -= Apply; GameBootstrap.OnAfterBootstrap += Apply;
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        }
        private static void Apply(Zone zone, EntityFactory factory, Entity player, TurnManager turns)
        {
            GameBootstrap.OnAfterBootstrap -= Apply;
            new GameObject("First-Hour Staged Viper Native Audit").AddComponent<FirstHourViperNativePlayer>()
                .Initialize(new ScenarioContext(zone, factory, player, turns));
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            var driver = UnityEngine.Object.FindFirstObjectByType<FirstHourViperNativePlayer>();
            if (driver != null && driver.Finished)
            {
                driver.SetUnexpectedErrors(SessionState.GetInt(Prefix + "errors", 0));
                Finish(driver.Failures == 0 ? 0 : 1); return;
            }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0))
            {
                if (driver != null) driver.Abort("Editor watchdog exceeded eleven minutes.");
                else { Debug.LogError("[FirstHourViperNative] Bootstrap did not create the native driver."); Finish(2); }
            }
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
            NativeAuditBootstrapSettings.RequestedSeed = SessionState.GetInt(Prefix + "oldSeed", 0);
            SaveGameService.RegisterRuntime(null, null);
            string oldStart=SessionState.GetString(Prefix+"oldStartScene","");
            EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(oldStart)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(oldStart);
            Debug.Log("[FirstHourViperNative] Native capture exit=" + code);
            if (!SessionState.GetBool(Prefix + "exitEditor", false))
            { SessionState.SetBool(Prefix + "restoreScenes", true); AwaitSceneRestore(); }
            NativeSaveIsolation.Finish(Prefix, SessionState.GetString(Prefix + "token", ""), code);
        }
        private static void AwaitSceneRestore()
        {
            // A one-shot delay can fire during Play teardown and be consumed
            // before the editor is ready. Keep one retry until restoration ends.
            EditorApplication.update -= RestoreScenes;
            EditorApplication.update += RestoreScenes;
            EditorApplication.playModeStateChanged -= OnStopped;
            EditorApplication.playModeStateChanged += OnStopped;
            if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.delayCall += RestoreScenes;
        }
        private static void OnStopped(PlayModeStateChange state)
        { if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RestoreScenes; }
        private static void RestoreScenes()
        {
            if (!SessionState.GetBool(Prefix + "restoreScenes", false))
            { EditorApplication.update -= RestoreScenes; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= RestoreScenes;
            SessionState.SetBool(Prefix + "restoreScenes", false);
            EditorApplication.playModeStateChanged -= OnStopped;
            var rows = JsonUtility.FromJson<SceneRows>(SessionState.GetString(Prefix + "scenes", ""))?.rows;
            if (rows != null && rows.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(rows.Select(s => new SceneSetup
                    { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
        }
        [Serializable] private sealed class SceneRows { public SceneRow[] rows; }
        [Serializable] private sealed class SceneRow { public string path; public bool loaded, active; }
    }
}
