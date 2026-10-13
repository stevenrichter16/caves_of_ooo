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
        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 01 World Map Music")]
        public static void LaunchAudioPolish01()=>LaunchAudioPolish(1);
        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 02 Music Continuation")]
        public static void LaunchAudioPolish02()=>LaunchAudioPolish(2);
        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 03 River and Puddle")]
        public static void LaunchAudioPolish03()=>LaunchAudioPolish(3);
        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 04 Frozen Water")]
        public static void LaunchAudioPolish04()=>LaunchAudioPolish(4);
        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 05 Enclosing Rooms")]
        public static void LaunchAudioPolish05()=>LaunchAudioPolish(5);
        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 06 Animal Rest")]
        public static void LaunchAudioPolish06()=>LaunchAudioPolish(6);
        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 07 Phrase Tails")]
        public static void LaunchAudioPolish07()=>LaunchAudioPolish(7);
        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 08 Dry Brush")]
        public static void LaunchAudioPolish08()=>LaunchAudioPolish(8);
        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 09 Dry Footing")]
        public static void LaunchAudioPolish09()=>LaunchAudioPolish(9);
        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 10 Water Vessels")]
        public static void LaunchAudioPolish10()=>LaunchAudioPolish(10);

        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 11 Cooking")]
        public static void LaunchAudioPolish11()=>LaunchAudioPolish(11);

        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 12 Brewing")]
        public static void LaunchAudioPolish12()=>LaunchAudioPolish(12);

        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 13 Material Handling")]
        public static void LaunchAudioPolish13()=>LaunchAudioPolish(13);

        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 14 Mineral Harvest")]
        public static void LaunchAudioPolish14()=>LaunchAudioPolish(14);

        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 15 Physical Reading")]
        public static void LaunchAudioPolish15()=>LaunchAudioPolish(15);

        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 16 Regions")]
        public static void LaunchAudioPolish16()=>LaunchAudioPolish(16);

        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 17 Score Sequence")]
        public static void LaunchAudioPolish17()=>LaunchAudioPolish(17);

        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 18 Quiet Intermissions")]
        public static void LaunchAudioPolish18()=>LaunchAudioPolish(18);

        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 19 Morrowfast Score")]
        public static void LaunchAudioPolish19()=>LaunchAudioPolish(19);

        [MenuItem("Caves Of Ooo/Scenarios/World/Audio Polish 20 Smooth Audio Levels")]
        public static void LaunchAudioPolish20()=>LaunchAudioPolish(20);
        public static void LaunchAudioPolish(int iteration)=>LaunchCore(false,audioPolishIteration:iteration);
        [MenuItem("Caves Of Ooo/Scenarios/World/Reference Glade Performance Profile")]
        public static void LaunchProfile() => LaunchCore(false,true);
        [MenuItem("Caves Of Ooo/Scenarios/World/World Audio Audit")]
        public static void LaunchWorldAudio() => LaunchCore(false,worldAudioAudit:true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Steam Deck Graphics Audit")]
        public static void LaunchGraphics() => LaunchCore(false, graphicsAudit: true);
        [MenuItem("Caves Of Ooo/Scenarios/UI/Full Screen Skills Audit")]
        public static void LaunchSkills()=>LaunchSkillsAt(800);
        [MenuItem("Caves Of Ooo/Scenarios/UI/Full Screen Skills Audit 720p")]
        public static void LaunchSkills720()=>LaunchSkillsAt(720);
        public static void LaunchSkillsAt(int height)=>LaunchCore(false,skillsAudit:true,hudWidth:1280,hudHeight:height);
        [MenuItem("Caves Of Ooo/Scenarios/World/Steam Deck Qud Controller Audit")]
        public static void LaunchController() => LaunchCore(false, controllerAudit: true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Companion Orders and Quest Journal Audit")]
        public static void LaunchSystemDepth() => LaunchCore(false, systemDepthAudit: true);
        [MenuItem("Caves Of Ooo/Scenarios/World/System Depth II Connections Audit")]
        public static void LaunchSystemDepthII() => LaunchCore(false, systemDepthIIAudit: true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Reference Glade Combat Audio Audit")]
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
        [MenuItem("Caves Of Ooo/Scenarios/World/Committed Work Audio Audit")]
        public static void LaunchRepairWorkAudio() => LaunchCore(false,repairCultivation:true,repairWorkAudio:true);
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
        [MenuItem("Caves Of Ooo/Scenarios/World/Reference Glade Combat Audio Audit", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;

        // HUD audits must start only after test/native teardown has completed.
        // Read the installed test framework's actual active-job state; do not
        // infer cleanup completion merely from a returned XML/test result.
        private static void RequireHudAuditIdle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating
                || SessionState.GetBool(Prefix + "active", false) || SessionState.GetBool(Prefix + "restoreScenes", false)
                || SessionState.GetBool("CavesOfOoo.NativeSaveIsolation.active", false))
                throw new InvalidOperationException("Wait for editor, test and native cleanup before a HUD audit.");
            var api = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi", false)).FirstOrDefault(t => t != null);
            var active = api?.GetMethod("IsRunActive", BindingFlags.Static | BindingFlags.NonPublic);
            if (active == null || !(active.Invoke(null, null) is bool running) || running)
                throw new InvalidOperationException("HUD audit requires an available, idle native test runner; wait for test cleanup.");
        }

        [MenuItem("Caves Of Ooo/Scenarios/World/HUD Baseline Profile")]
        public static void LaunchHudBaselineProfile() { RequireHudAuditIdle(); LaunchCore(false, hudProfile:"before"); }
        [MenuItem("Caves Of Ooo/Scenarios/World/HUD After Profile")]
        public static void LaunchHudAfterProfile() { RequireHudAuditIdle(); LaunchCore(false, hudProfile:"after"); }

        [MenuItem("Caves Of Ooo/Scenarios/World/HUD Polish Audit 1080p")]
        public static void LaunchHudPolish() { RequireHudAuditIdle(); LaunchCore(false,hudPolish:true); }
        [MenuItem("Caves Of Ooo/Scenarios/World/HUD Polish Audit 800p")]
        public static void LaunchHudPolish800() { RequireHudAuditIdle(); LaunchCore(false,hudPolish:true,hudWidth:1280,hudHeight:800); }
        [MenuItem("Caves Of Ooo/Scenarios/World/HUD Polish Audit 720p")]
        public static void LaunchHudPolish720() { RequireHudAuditIdle(); LaunchCore(false,hudPolish:true,hudWidth:1280,hudHeight:720); }

        private static void LaunchCore(bool exitEditor,bool profile=false,bool combat=false,bool biome=false,bool specialists=false,bool residents=false,bool curation=false,bool curationQuarantine=false,bool repairCultivation=false,bool biomeCrops=false,bool biomeCaveCrops=false,bool curationAnnex=false,bool graphicsAudit=false,bool controllerAudit=false,bool systemDepthAudit=false,bool systemDepthIIAudit=false,bool worldAudioAudit=false,bool repairWorkAudio=false,int audioPolishIteration=0,string hudProfile=null,bool hudPolish=false,int hudWidth=1920,int hudHeight=1080,bool skillsAudit=false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play before launching the completion audit.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save scene edits before launching; the completion audit will not discard them.");
            if (hudPolish && (!string.IsNullOrEmpty(hudProfile) || !((hudWidth==1920 && hudHeight==1080) || (hudWidth==1280 && (hudHeight==800 || hudHeight==720)))))
                throw new InvalidOperationException("HUD functional audit requires one approved viewport and a separate profile run.");
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
            SessionState.SetBool(Prefix+"skillsAudit",skillsAudit);
            SessionState.SetBool(Prefix+"hudPolish",hudPolish);
            SessionState.SetString(Prefix+"hudProfile",hudProfile ?? "");
            SessionState.SetInt(Prefix+"audioPolishIteration",audioPolishIteration);
            SessionState.SetBool(Prefix+"worldAudioAudit",worldAudioAudit);
            SessionState.SetBool(Prefix+"systemDepthIIAudit",systemDepthIIAudit);
            SessionState.SetBool(Prefix+"systemDepthAudit",systemDepthAudit);
            SessionState.SetBool(Prefix+"controllerAudit",controllerAudit);
            SessionState.SetBool(Prefix+"graphicsAudit",graphicsAudit);
            SessionState.SetBool(Prefix+"profile",profile);
            SessionState.SetBool(Prefix+"combat",combat);
            SessionState.SetBool(Prefix+"biome",biome);
            SessionState.SetBool(Prefix+"specialists",specialists);
            SessionState.SetBool(Prefix+"residents",residents);
            SessionState.SetBool(Prefix+"curation",curation);
            SessionState.SetBool(Prefix+"curationQuarantine",curationQuarantine);
            SessionState.SetBool(Prefix+"curationAnnex",curationAnnex);
            SessionState.SetBool(Prefix+"repairCultivation",repairCultivation);
            SessionState.SetBool(Prefix+"repairWorkAudio",repairWorkAudio);
            SessionState.SetBool(Prefix+"biomeCrops",biomeCrops);
            SessionState.SetBool(Prefix+"biomeCaveCrops",biomeCaveCrops);
            SessionState.SetBool(Prefix + "active", true);
            SessionState.SetBool(Prefix + "restoreScenes", false);
            SessionState.SetBool(Prefix + "exitEditor", exitEditor);
            SessionState.SetInt(Prefix + "errors", 0);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + (audioPolishIteration==17||audioPolishIteration==18?960:270));
            try
            {
                ConfigureNativeView(hudPolish || skillsAudit ? hudWidth : 1920, hudPolish || skillsAudit ? hudHeight : 1080);
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
            if (SessionState.GetBool(Prefix+"skillsAudit",false)) runner.ConfigureSkillsAudit();
            if (SessionState.GetBool(Prefix+"hudPolish",false)) runner.ConfigureHudPolish();
            string hudProfile=SessionState.GetString(Prefix+"hudProfile", "");
            if(!string.IsNullOrEmpty(hudProfile))runner.ConfigureHudProfile(hudProfile);
            int polish=SessionState.GetInt(Prefix+"audioPolishIteration",0);
            if(polish>0)runner.ConfigureAudioPolish(polish);
            if (SessionState.GetBool(Prefix+"worldAudioAudit",false)) runner.ConfigureWorldAudioAudit();
            if (SessionState.GetBool(Prefix+"repairWorkAudio",false)) runner.ConfigureRepairWorkAudio();
            if (SessionState.GetBool(Prefix+"systemDepthIIAudit",false)) runner.ConfigureSystemDepthIIAudit();
            if (SessionState.GetBool(Prefix+"systemDepthAudit",false)) runner.ConfigureSystemDepthAudit();
            if (SessionState.GetBool(Prefix+"controllerAudit",false)) runner.ConfigureControllerAudit();
            if (SessionState.GetBool(Prefix+"graphicsAudit",false)) runner.ConfigureGraphicsAudit();
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
        private static void ConfigureNativeView(int width,int height)
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
                if(w==width&&h==height){chosen=i;break;}
            }
            if(chosen<0)
            {
                var sizeType=EditorType("UnityEditor.GameViewSize");var enumType=EditorType("UnityEditor.GameViewSizeType");
                var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.Parse(enumType,"FixedResolution"),width,height,"Native world audit "+width+"x"+height},null);
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
