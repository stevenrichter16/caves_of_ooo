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
    /// <summary>Finite isolated throwing launcher. Owns its bootstrap seed and
    /// restores the preceding scene setup, input settings, save root and prefs.</summary>
    [InitializeOnLoad]
    public static class ThrowingNaturalNativeBatch
    {
        private const string Prefix = "Throwing.Natural.Native.";
        static ThrowingNaturalNativeBatch()
        {
            if (SessionState.GetBool(Prefix + "active", false)) Subscribe();
            if (SessionState.GetBool(Prefix + "restoreScenes", false)) AwaitSceneRestore();
        }
        [MenuItem("Caves Of Ooo/Scenarios/Combat/Throwing Natural Bombardier 29")]
        public static void Launch() => LaunchCore("bombardier",29,false);
        [MenuItem("Caves Of Ooo/Scenarios/Combat/Throwing Natural Bombardier 64")]
        public static void Bombardier64() => LaunchCore("bombardier",64,false);
        [MenuItem("Caves Of Ooo/Scenarios/Combat/Throwing Natural Breaker 1729")]
        public static void Breaker1729() => LaunchCore("breaker",1729,false);
        [MenuItem("Caves Of Ooo/Scenarios/Combat/Throwing Natural Warden 64")]
        public static void Warden64() => LaunchCore("warden",64,false);
        [MenuItem("Caves Of Ooo/Scenarios/Combat/Throwing Natural Duelist 64")]
        public static void Duelist64() => LaunchCore("duelist",64,false);
        // These are held-out follow-ups only after an absent/ambiguous physical
        // opportunity in the initial three, never automatic replacement seeds.
        public static void HeldOut201() => LaunchCore("bombardier",201,false);
        public static void HeldOut202() => LaunchCore("bombardier",202,false);
        [MenuItem("Caves Of Ooo/Scenarios/Combat/Throwing Natural Bombardier 29", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void LaunchCore(string build, int seed, bool exitEditor)
        {
            bool control=build=="warden"||build=="duelist";
            if ((seed==201||seed==202) && !SessionState.GetBool(Prefix+"observedMissingOpportunity",false))
                throw new InvalidOperationException("Held-out follow-up requires a recorded absent initial throwing opportunity.");
            string ledger=control?"controls":"initial";
            int priorActions=SessionState.GetInt(Prefix+ledger+"Actions",0);
            int maximum=control?1200:2000, attempts=SessionState.GetInt(Prefix+ledger+"Attempts",0);
            if(priorActions>=maximum||attempts>=(control?2:5))
                throw new InvalidOperationException("Declared natural cohort budget is exhausted; retain unreached evidence.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play before launching the throwing audit.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save scene edits before launching; the throwing audit will not discard them.");
            var snapshot = EditorSceneManager.GetSceneManagerSetup().Select(s => new SceneRow
                { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray();
            if (snapshot.Any(s => string.IsNullOrEmpty(s.path)))
                throw new InvalidOperationException("Save untitled scenes before launching the throwing audit.");
            string token = NativeSaveIsolation.Begin(Prefix, "Throwing-marker-" + Guid.NewGuid().ToString("N"), exitEditor);
            SessionState.SetString(Prefix + "token", token);
            SessionState.SetString(Prefix + "scenes", JsonUtility.ToJson(new SceneRows { rows = snapshot }));
            SessionState.SetString(Prefix + "oldStartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) ?? "");
            SessionState.SetInt(Prefix + "oldSeed", NativeAuditBootstrapSettings.RequestedSeed);
            SessionState.SetBool(Prefix + "oldBuildChoice", NativeAuditBootstrapSettings.AllowStartingBuildChoice);
            SessionState.SetInt(Prefix + "seed", seed);
            SessionState.SetString(Prefix + "build", build);
            SessionState.SetString(Prefix+"ledger",ledger);
            SessionState.SetInt(Prefix+ledger+"Attempts",attempts+1);
            SessionState.SetInt(Prefix+"actionBudget",Math.Min(700,maximum-priorActions));
            SessionState.SetBool(Prefix+"accounted",false);
            SessionState.SetBool(Prefix + "active", true);
            SessionState.SetBool(Prefix + "restoreScenes", false);
            SessionState.SetBool(Prefix + "exitEditor", exitEditor);
            SessionState.SetInt(Prefix + "errors", 0);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 1200);
            try
            {
                Subscribe();
                EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");
                EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main/SampleScene.unity");
                EditorApplication.isPlaying = true;
            }
            catch (Exception error) { Debug.LogError("[ThrowingNaturalNative] Launch failed: " + error); Finish(4); }
        }
        private static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix, SessionState.GetString(Prefix + "token", ""), Finish);
            NativeAuditBootstrapSettings.RequestedSeed = SessionState.GetInt(Prefix + "seed", 0);
            NativeAuditBootstrapSettings.AllowStartingBuildChoice = true;
            GameBootstrap.OnAfterBootstrap -= Apply; GameBootstrap.OnAfterBootstrap += Apply;
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        }
        private static void Apply(Zone zone, EntityFactory factory, Entity player, TurnManager turns)
        {
            GameBootstrap.OnAfterBootstrap -= Apply;
            new GameObject("Throwing Natural Bombardier 29").AddComponent<SpreadDiscoveryNativePlayer>()
                .InitializeThrowingNatural(new ScenarioContext(zone, factory, player, turns),
                    SessionState.GetString(Prefix + "build", "bombardier"), SessionState.GetInt(Prefix + "seed", 29), SessionState.GetInt(Prefix+"actionBudget",700));
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            var driver = UnityEngine.Object.FindFirstObjectByType<SpreadDiscoveryNativePlayer>();
            if (driver != null && driver.Finished)
            {
                driver.SetUnexpectedErrors(SessionState.GetInt(Prefix + "errors", 0));
                Finish(driver.Failures == 0 ? 0 : 1); return;
            }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0))
            {
                if (driver != null) driver.Abort("Editor watchdog exceeded twenty minutes; actual partial evidence retained.");
                else { Debug.LogError("[ThrowingNaturalNative] Bootstrap did not create the native driver."); Finish(2); }
            }
        }
        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                SessionState.SetInt(Prefix + "errors", SessionState.GetInt(Prefix + "errors", 0) + 1);
        }
        private static void Finish(int code)
        {
            var driver=UnityEngine.Object.FindFirstObjectByType<SpreadDiscoveryNativePlayer>();
            if(!SessionState.GetBool(Prefix+"accounted",false))
            {
                SessionState.SetBool(Prefix+"accounted",true);
                string ledger=SessionState.GetString(Prefix+"ledger","initial");
                SessionState.SetInt(Prefix+ledger+"Actions",SessionState.GetInt(Prefix+ledger+"Actions",0)+(driver?.ThrowingNaturalPaidActions??0));
                if(driver?.ThrowingNaturalOutcome=="no-useful-visible-throw")SessionState.SetBool(Prefix+"observedMissingOpportunity",true);
                Debug.Log("[ThrowingNaturalNative] Cohort="+ledger+" attempts="+SessionState.GetInt(Prefix+ledger+"Attempts",0)
                    +" paidActions="+SessionState.GetInt(Prefix+ledger+"Actions",0)+" report="+driver?.ReportPath);
            }
            SessionState.SetBool(Prefix + "active", false);
            GameBootstrap.OnAfterBootstrap -= Apply; EditorApplication.update -= Poll; Application.logMessageReceived -= OnLog;
            NativeAuditBootstrapSettings.RequestedSeed = SessionState.GetInt(Prefix + "oldSeed", 0);
            NativeAuditBootstrapSettings.AllowStartingBuildChoice = SessionState.GetBool(Prefix + "oldBuildChoice", false);
            SaveGameService.RegisterRuntime(null, null);
            string oldStart=SessionState.GetString(Prefix+"oldStartScene","");
            EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(oldStart)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(oldStart);
            Debug.Log("[ThrowingNaturalNative] Native capture exit=" + code);
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
