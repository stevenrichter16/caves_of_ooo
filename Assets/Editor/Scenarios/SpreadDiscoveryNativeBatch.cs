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
    /// <summary>Finite isolated Spread discovery launcher. Owns its bootstrap seed and
    /// restores the preceding scene setup, input settings, save root and prefs.</summary>
    [InitializeOnLoad]
    public static class SpreadDiscoveryNativeBatch
    {
        private const string Prefix = "SpreadDiscovery.Native.";
        static SpreadDiscoveryNativeBatch()
        {
            if (SessionState.GetBool(Prefix + "active", false)) Subscribe();
            if (SessionState.GetBool(Prefix + "restoreScenes", false)) AwaitSceneRestore();
        }
        public static void Run() => LaunchCore(true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Connected Spread Duelist Audit")]
        public static void LaunchConnectedDuelist() => LaunchCore(false,connectedBuild:"duelist");
        [MenuItem("Caves Of Ooo/Scenarios/World/Living Fieldwork Ordinary Audit")]
        public static void LaunchFieldwork() => LaunchCore(false,connectedBuild:"duelist",fieldwork:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Mendleaf Drying Yard Ordinary Audit")]
        public static void LaunchMendleafYard() => LaunchCore(false,connectedBuild:"duelist",mendleafYard:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Timber Trap Jamming Ordinary Audit")]
        public static void LaunchTrapJamming() => LaunchCore(false,connectedBuild:"duelist",trapJamming:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Predator Meat Diversion Ordinary Audit")]
        public static void LaunchPredatorDiversion() => LaunchCore(false,connectedBuild:"duelist",predatorDiversion:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Connected Spread Stormcaller Audit")]
        public static void LaunchConnectedStormcaller() => LaunchCore(false,connectedBuild:"stormcaller");
        [MenuItem("Caves Of Ooo/Scenarios/World/Connected Spread Breaker Targeted Audit")]
        public static void LaunchConnectedBreaker() => LaunchCore(false,seed:1729,connectedBuild:"breaker");
        [MenuItem("Caves Of Ooo/Scenarios/World/Connected Spread Bombardier Targeted Audit")]
        public static void LaunchConnectedBombardier() => LaunchCore(false,seed:29,connectedBuild:"bombardier");
        [MenuItem("Caves Of Ooo/Scenarios/World/Sodden Expedition Ordinary Audit")]
        public static void LaunchSoddenDistrict() => LaunchCore(false,connectedBuild:"duelist",soddenDistrict:true);
        public static void LaunchCards() => LaunchCore(false,false,true);
        public static void LaunchOrdinary() => LaunchCore(false,true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Gleaners District Ordinary Audit")]
        public static void LaunchDistrict() => LaunchCore(false,district:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Gleaners District Mirrored Audit")]
        public static void LaunchDistrictMirrored() => LaunchCore(false,district:true,seed:1729);
        [MenuItem("Caves Of Ooo/Scenarios/World/Gleaners District Confrontation Audit")]
        public static void LaunchDistrictCombat() => LaunchCore(false,district:true,districtCombat:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Gleaners District Preparation Audit")]
        public static void LaunchDistrictPrepared() => LaunchCore(false,district:true,districtPrepared:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Spread Discovery Native Audit")]
        public static void Launch() => LaunchCore(false);
        [MenuItem("Caves Of Ooo/Scenarios/World/Spread Discovery Native Audit", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void LaunchCore(bool exitEditor,bool ordinary=false,bool cards=false,bool district=false,int seed=64,bool districtCombat=false,bool districtPrepared=false,string connectedBuild=null,bool fieldwork=false,bool predatorDiversion=false,bool trapJamming=false,bool soddenDistrict=false,bool mendleafYard=false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play before launching the Spread discovery audit.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save scene edits before launching; the Spread discovery audit will not discard them.");
            var snapshot = EditorSceneManager.GetSceneManagerSetup().Select(s => new SceneRow
                { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray();
            if (snapshot.Any(s => string.IsNullOrEmpty(s.path)))
                throw new InvalidOperationException("Save untitled scenes before launching the Spread discovery audit.");
            string token = NativeSaveIsolation.Begin(Prefix, "Density-Spread discovery-marker-" + Guid.NewGuid().ToString("N"), exitEditor);
            SessionState.SetString(Prefix + "token", token);
            SessionState.SetString(Prefix + "scenes", JsonUtility.ToJson(new SceneRows { rows = snapshot }));
            SessionState.SetString(Prefix + "oldStartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) ?? "");
            SessionState.SetInt(Prefix + "oldSeed", NativeAuditBootstrapSettings.RequestedSeed);
            SessionState.SetBool(Prefix + "oldBuildChoice", NativeAuditBootstrapSettings.AllowStartingBuildChoice);
            SessionState.SetString(Prefix + "connectedBuild", connectedBuild??"");
            SessionState.SetBool(Prefix + "fieldwork", fieldwork);
            SessionState.SetBool(Prefix + "predatorDiversion", predatorDiversion);
            SessionState.SetBool(Prefix + "trapJamming", trapJamming);
            SessionState.SetBool(Prefix + "soddenDistrict", soddenDistrict);
            SessionState.SetBool(Prefix + "mendleafYard", mendleafYard);
            SessionState.SetInt(Prefix + "seed", seed);
            SessionState.SetBool(Prefix + "ordinary", ordinary);
            SessionState.SetBool(Prefix + "cards", cards);
            SessionState.SetBool(Prefix + "district", district);
            SessionState.SetBool(Prefix + "districtCombat", districtCombat);
            SessionState.SetBool(Prefix + "districtPrepared", districtPrepared);
            SessionState.SetBool(Prefix + "active", true);
            SessionState.SetBool(Prefix + "restoreScenes", false);
            SessionState.SetBool(Prefix + "exitEditor", exitEditor);
            SessionState.SetInt(Prefix + "errors", 0);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + (string.IsNullOrEmpty(connectedBuild)?660:1260));
            try
            {
                Subscribe();
                EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");
                EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main/SampleScene.unity");
                EditorApplication.isPlaying = true;
            }
            catch (Exception error) { Debug.LogError("[SpreadDiscoveryNative] Launch failed: " + error); Finish(4); }
        }
        private static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix, SessionState.GetString(Prefix + "token", ""), Finish);
            NativeAuditBootstrapSettings.RequestedSeed = SessionState.GetInt(Prefix + "seed", 0);
            NativeAuditBootstrapSettings.AllowStartingBuildChoice = !string.IsNullOrEmpty(SessionState.GetString(Prefix + "connectedBuild", ""));
            GameBootstrap.OnAfterBootstrap -= Apply; GameBootstrap.OnAfterBootstrap += Apply;
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        }
        private static void Apply(Zone zone, EntityFactory factory, Entity player, TurnManager turns)
        {
            GameBootstrap.OnAfterBootstrap -= Apply;
            var driver = new GameObject("Spread Discovery Native Audit").AddComponent<SpreadDiscoveryNativePlayer>();
            if (SessionState.GetBool(Prefix + "mendleafYard", false))
                driver.InitializeMendleafYard(new ScenarioContext(zone, factory, player, turns));
            else if (SessionState.GetBool(Prefix + "soddenDistrict", false))
                driver.InitializeSoddenDistrict(new ScenarioContext(zone, factory, player, turns));
            else if (SessionState.GetBool(Prefix + "trapJamming", false))
                driver.InitializeTrapJamming(new ScenarioContext(zone, factory, player, turns));
            else if (SessionState.GetBool(Prefix + "predatorDiversion", false))
                driver.InitializePredatorDiversion(new ScenarioContext(zone, factory, player, turns));
            else driver.Initialize(new ScenarioContext(zone, factory, player, turns),SessionState.GetBool(Prefix+"ordinary",false),SessionState.GetBool(Prefix+"cards",false),SessionState.GetBool(Prefix+"district",false),SessionState.GetBool(Prefix+"districtCombat",false),SessionState.GetBool(Prefix+"districtPrepared",false),SessionState.GetString(Prefix+"connectedBuild",""),SessionState.GetBool(Prefix+"fieldwork",false));
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
                if (driver != null) driver.Abort("Editor watchdog exceeded the finite native audit deadline.");
                else { Debug.LogError("[SpreadDiscoveryNative] Bootstrap did not create the native driver."); Finish(2); }
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
            NativeAuditBootstrapSettings.AllowStartingBuildChoice = SessionState.GetBool(Prefix + "oldBuildChoice", false);
            SaveGameService.RegisterRuntime(null, null);
            string oldStart=SessionState.GetString(Prefix+"oldStartScene","");
            EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(oldStart)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(oldStart);
            Debug.Log("[SpreadDiscoveryNative] Native capture exit=" + code);
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
