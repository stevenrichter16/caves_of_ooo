using System;
using System.Linq;
using System.Reflection;
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
    /// <summary>Finite isolated completion launcher. Owns its bootstrap seed and
    /// restores the preceding scene setup, input settings, save root and prefs.</summary>
    [InitializeOnLoad]
    public static class ReferenceGladeNativeBatch
    {
        private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        private const string Prefix = "ReferenceGlade.Native.";
        static ReferenceGladeNativeBatch()
        {
            if (SessionState.GetBool(Prefix + "active", false)) Subscribe();
            if (SessionState.GetBool(Prefix + "restoreScenes", false)) AwaitSceneRestore();
        }
        public static void Run() => LaunchCore(true);
        public static void LaunchProfile() => LaunchCore(false,true);
        public static void LaunchCombat() => LaunchCore(false,false,true);
        public static void LaunchBiome() => LaunchCore(false,false,false,true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Spread Specialist Content Audit")]
        public static void LaunchSpecialists() => LaunchCore(false,false,false,false,true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Spread Everyday Residents Audit")]
        public static void LaunchResidents() => LaunchCore(false,false,false,false,false,true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Curation Receiving Public Audit")]
        public static void LaunchCuration() => LaunchCore(false,false,false,false,false,false,true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Curation Receiving Quarantine Audit")]
        public static void LaunchCurationQuarantine() => LaunchCore(false,false,false,false,false,false,true,true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Curation Annex Audit")]
        public static void LaunchCurationAnnex() => LaunchCore(false,curation:true,curationAnnex:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Repair and Cultivation Audit")]
        public static void LaunchRepairCultivation() => LaunchCore(false,repairCultivation:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Biome Crops Audit")]
        public static void LaunchBiomeCrops() => LaunchCore(false,biomeCrops:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Biome Cave Crops Audit")]
        public static void LaunchBiomeCaveCrops() => LaunchCore(false,biomeCrops:true,biomeCaveCrops:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Reference Glade Native Audit")]
        public static void Launch() => LaunchCore(false);
        [MenuItem("Caves Of Ooo/Scenarios/World/Reference Glade Native Audit", true)]
        [MenuItem("Caves Of Ooo/Scenarios/World/Spread Specialist Content Audit", true)]
        [MenuItem("Caves Of Ooo/Scenarios/World/Spread Everyday Residents Audit", true)]
        [MenuItem("Caves Of Ooo/Scenarios/World/Curation Receiving Public Audit", true)]
        [MenuItem("Caves Of Ooo/Scenarios/World/Curation Receiving Quarantine Audit", true)]
        [MenuItem("Caves Of Ooo/Scenarios/World/Curation Annex Audit", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void LaunchCore(bool exitEditor,bool profile=false,bool combat=false,bool biome=false,bool specialists=false,bool residents=false,bool curation=false,bool curationQuarantine=false,bool repairCultivation=false,bool biomeCrops=false,bool biomeCaveCrops=false,bool curationAnnex=false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play before launching the completion audit.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save scene edits before launching; the completion audit will not discard them.");
            var snapshot = EditorSceneManager.GetSceneManagerSetup().Select(s => new SceneRow
                { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray();
            if (snapshot.Any(s => string.IsNullOrEmpty(s.path)))
                throw new InvalidOperationException("Save untitled scenes before launching the completion audit.");
            string token = NativeSaveIsolation.Begin(Prefix, "Reference-glade-marker-" + Guid.NewGuid().ToString("N"), exitEditor);
            SessionState.SetString(Prefix + "token", token);
            SessionState.SetString(Prefix + "scenes", JsonUtility.ToJson(new SceneRows { rows = snapshot }));
            SessionState.SetString(Prefix + "oldStartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) ?? "");
            SessionState.SetInt(Prefix + "oldSeed", NativeAuditBootstrapSettings.RequestedSeed);
            SessionState.SetInt(Prefix + "seed", 64);
            SessionState.SetBool(Prefix+"profile",profile);
            SessionState.SetBool(Prefix+"combat",combat);
            SessionState.SetBool(Prefix+"biome",biome);
            SessionState.SetBool(Prefix+"specialists",specialists);
            SessionState.SetBool(Prefix+"residents",residents);
            SessionState.SetBool(Prefix+"curation",curation);
            SessionState.SetBool(Prefix+"curationQuarantine",curationQuarantine);
            SessionState.SetBool(Prefix+"curationAnnex",curationAnnex);
            SessionState.SetBool(Prefix+"repairCultivation",repairCultivation);
            SessionState.SetBool(Prefix+"biomeCrops",biomeCrops);
            SessionState.SetBool(Prefix+"biomeCaveCrops",biomeCaveCrops);
            SessionState.SetBool(Prefix + "active", true);
            SessionState.SetBool(Prefix + "restoreScenes", false);
            SessionState.SetBool(Prefix + "exitEditor", exitEditor);
            SessionState.SetInt(Prefix + "errors", 0);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 270);
            try
            {
                Configure1080p();
                Subscribe();
                EditorSceneManager.OpenScene("Assets/Scenes/Main/ReferenceGlade.unity");
                EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main/ReferenceGlade.unity");
                EditorApplication.isPlaying = true;
            }
            catch (Exception error) { Debug.LogError("[ReferenceGladeNative] Launch failed: " + error); Finish(4); }
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
            var runner = new GameObject("Reference Glade Native Audit").AddComponent<ReferenceGladeNativePlayer>();
            if (SessionState.GetBool(Prefix+"biomeCaveCrops",false)) runner.ConfigureBotanicalCaveOnly();
            runner.Initialize(new ScenarioContext(zone, factory, player, turns),SessionState.GetBool(Prefix+"profile",false),SessionState.GetBool(Prefix+"combat",false),SessionState.GetBool(Prefix+"biome",false),SessionState.GetBool(Prefix+"specialists",false),SessionState.GetBool(Prefix+"residents",false),SessionState.GetBool(Prefix+"curation",false),SessionState.GetBool(Prefix+"curationQuarantine",false),SessionState.GetBool(Prefix+"repairCultivation",false),SessionState.GetBool(Prefix+"biomeCrops",false),SessionState.GetBool(Prefix+"curationAnnex",false));
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            var driver = UnityEngine.Object.FindFirstObjectByType<ReferenceGladeNativePlayer>();
            if (driver != null && driver.Finished)
            {
                driver.SetUnexpectedErrors(SessionState.GetInt(Prefix + "errors", 0));
                Finish(driver.Failures == 0 ? 0 : 1); return;
            }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0))
            {
                if (driver != null) driver.Abort("Editor watchdog exceeded four and a half minutes.");
                else { Debug.LogError("[ReferenceGladeNative] Bootstrap did not create the native driver."); Finish(2); }
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
            Debug.Log("[ReferenceGladeNative] Native capture exit=" + code);
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
            RestoreView();
            EditorApplication.playModeStateChanged -= OnStopped;
            var rows = JsonUtility.FromJson<SceneRows>(SessionState.GetString(Prefix + "scenes", ""))?.rows;
            if (rows != null && rows.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(rows.Select(s => new SceneSetup
                    { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
            string oldStart=SessionState.GetString(Prefix+"oldStartScene","");
            EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(oldStart)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(oldStart);
        }
        private static Type EditorType(string name)=>typeof(UnityEditor.Editor).Assembly.GetType(name,true);
        private static object Sizes()
        {
            var type=EditorType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(type);
            return singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
        }
        private static object Group()
        {
            var sizes=Sizes();var kind=EditorType("UnityEditor.GameViewSizeGroupType");
            return sizes.GetType().GetMethod("GetGroup",All).Invoke(sizes,new[]{Enum.Parse(kind,"Standalone")});
        }
        private static EditorWindow View()=>EditorWindow.GetWindow(EditorType("UnityEditor.GameView"));
        private static void Configure1080p()
        {
            var viewType=EditorType("UnityEditor.GameView");SessionState.SetBool(Prefix+"hadView",Resources.FindObjectsOfTypeAll(viewType).Length>0);
            var view=View();var selected=viewType.GetProperty("selectedSizeIndex",All);if(selected==null)throw new InvalidOperationException("GameView fixed-size API unavailable.");
            SessionState.SetInt(Prefix+"oldViewIndex",(int)selected.GetValue(view));SessionState.SetInt(Prefix+"addedCustomIndex",-1);
            SessionState.SetBool(Prefix+"viewConfigured",true);var group=Group();var gt=group.GetType();
            int built=(int)gt.GetMethod("GetBuiltinCount",All).Invoke(group,null),custom=(int)gt.GetMethod("GetCustomCount",All).Invoke(group,null),chosen=-1;
            SessionState.SetInt(Prefix+"oldCustomCount",custom);
            for(int i=0;i<built+custom;i++)
            {
                var size=gt.GetMethod("GetGameViewSize",All).Invoke(group,new object[]{i});
                int w=(int)size.GetType().GetProperty("width",All).GetValue(size),h=(int)size.GetType().GetProperty("height",All).GetValue(size);
                if(w==1920&&h==1080){chosen=i;break;}
            }
            if(chosen<0)
            {
                var sizeType=EditorType("UnityEditor.GameViewSize");var enumType=EditorType("UnityEditor.GameViewSizeType");
                var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.Parse(enumType,"FixedResolution"),1920,1080,"Voxel world audit 1920x1080"},null);
                gt.GetMethod("AddCustomSize",All).Invoke(group,new[]{size});SessionState.SetInt(Prefix+"addedCustomIndex",custom);chosen=built+custom;
            }
            selected.SetValue(view,chosen);view.Show();view.Focus();view.Repaint();EditorApplication.QueuePlayerLoopUpdate();
        }
        private static void RestoreView()
        {
            if(!SessionState.GetBool(Prefix+"viewConfigured",false))return;
            var view=View();var type=view.GetType();int added=SessionState.GetInt(Prefix+"addedCustomIndex",-1);
            if(added>=0){var group=Group();group.GetType().GetMethod("RemoveCustomSize",All).Invoke(group,new object[]{added});}
            type.GetProperty("selectedSizeIndex",All).SetValue(view,SessionState.GetInt(Prefix+"oldViewIndex",0));view.Repaint();
            if(!SessionState.GetBool(Prefix+"hadView",true))view.Close();SessionState.SetBool(Prefix+"viewConfigured",false);
        }

        [Serializable] private sealed class SceneRows { public SceneRow[] rows; }
        [Serializable] private sealed class SceneRow { public string path; public bool loaded, active; }
    }
}
