using System;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Native editor only: actual 1080p GameView, isolated ordinary bootstrap,
    /// automatic Play teardown, scene/view restoration and final cleanup receipt.</summary>
    [InitializeOnLoad]
    public static class Village3DNativeAuditBatch
    {
        private const string Prefix="Village3D.NativeAudit.";
        private const int PairedWorldSeed=729490642;
        private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static Village3DNativeAuditBatch()
        {
            // Save isolation can finish before this type initializes after a reload.
            // Its owner-active flag is then false while our scene/view cleanup remains.
            if(SessionState.GetBool(Prefix+"finishing",false))EditorApplication.update+=CompleteWhenStopped;
            else if(SessionState.GetBool(Prefix+"active",false))Subscribe();
        }
        private static void CompleteWhenStopped()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode
                || SessionState.GetBool("CavesOfOoo.NativeSaveIsolation.active",false))return;
            EditorApplication.update-=CompleteWhenStopped;
            AfterNativeCleanup();
        }
        public static void RunBefore()=>RunCore("before",true);
        public static void RunAfter()=>RunCore("after",true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Village 3D Paired Audit Before")]
        public static void LaunchBefore()=>RunCore("before",false);
        [MenuItem("Caves Of Ooo/Scenarios/World/Village 3D Paired Audit After")]
        public static void LaunchAfter()=>RunCore("after",false);
        private static void RunCore(string mode,bool exitEditor)
        {
            if(Application.isBatchMode)throw new InvalidOperationException("Use a native editor without -batchmode; actual GameView screenshots require rendering.");
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit current Play before starting the audit.");
            if(SessionState.GetBool(Prefix+"active",false))throw new InvalidOperationException("Audit already active.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save current scene edits first; audit refuses to discard them.");
            var saved=new Scenes{rows=new SceneRow[setup.Length]};for(int i=0;i<setup.Length;i++)saved.rows[i]=new SceneRow{path=setup[i].path,isLoaded=setup[i].isLoaded,isActive=setup[i].isActive};
            SessionState.SetString(Prefix+"scenes",JsonUtility.ToJson(saved));SessionState.SetString(Prefix+"mode",mode);
            SessionState.SetInt(Prefix+"oldRequestedSeed",NativeAuditBootstrapSettings.RequestedSeed);
            SessionState.SetBool(Prefix+"seedArmed",false);
            SessionState.SetBool(Prefix+"exit",exitEditor);SessionState.SetBool(Prefix+"finishing",false);SessionState.SetInt(Prefix+"code",4);
            SessionState.SetInt(Prefix+"errors",0);SessionState.SetBool(Prefix+"viewConfigured",false);
            try
            {
                Configure1080p();
                string token=NativeSaveIsolation.Begin(Prefix,"Village3D-native-marker-"+Guid.NewGuid().ToString("N"),false);
                SessionState.SetString(Prefix+"token",token);SessionState.SetString(Prefix+"root",SaveGameService.SaveRootOverride);
                SessionState.SetBool(Prefix+"active",true);SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+660);
                Subscribe();EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");EditorApplication.isPlaying=true;
            }
            catch(Exception error)
            {
                Debug.LogError("[Village3DNativeAudit] Launch failed: "+error);
                if(SessionState.GetBool(Prefix+"active",false))Finish(4);else{RestoreSeed();RestoreView();RestoreScenes();throw;}
            }
        }
        private static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix,SessionState.GetString(Prefix+"token",""),Finish);
            // SessionState preserves the original setting across domain reload. Re-arm
            // before scene Start, after re-establishing the same private audit root.
            NativeAuditBootstrapSettings.RequestedSeed=PairedWorldSeed;
            SessionState.SetBool(Prefix+"seedArmed",true);
            GameBootstrap.OnAfterBootstrap-=Apply;GameBootstrap.OnAfterBootstrap+=Apply;
            EditorApplication.update-=Poll;EditorApplication.update+=Poll;
            Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;
            EditorApplication.playModeStateChanged-=OnPlayState;EditorApplication.playModeStateChanged+=OnPlayState;
        }
        private static void Apply(Zone zone,EntityFactory factory,Entity player,TurnManager turns)
        {GameBootstrap.OnAfterBootstrap-=Apply;new GameObject("Village 3D Native Audit").AddComponent<Village3DNativeAudit>().Initialize(SessionState.GetString(Prefix+"mode",""));}
        private static void Poll()
        {
            if(!SessionState.GetBool(Prefix+"active",false)||SessionState.GetBool(Prefix+"finishing",false))return;
            var driver=UnityEngine.Object.FindFirstObjectByType<Village3DNativeAudit>();
            if(driver!=null&&driver.Finished){driver.SetUnexpectedErrors(SessionState.GetInt(Prefix+"errors",0));Finish(driver.Failures==0?0:1);return;}
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Prefix+"deadline",0))
            {driver?.Abort("Native watchdog exceeded eleven minutes.");Finish(2);}
        }
        private static void OnLog(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)SessionState.SetInt(Prefix+"errors",SessionState.GetInt(Prefix+"errors",0)+1);}
        private static void Finish(int code)
        {
            if(SessionState.GetBool(Prefix+"finishing",false))return;
            SessionState.SetBool(Prefix+"finishing",true);SessionState.SetInt(Prefix+"code",code);
            GameBootstrap.OnAfterBootstrap-=Apply;EditorApplication.update-=Poll;Application.logMessageReceived-=OnLog;
            SaveGameService.RegisterRuntime(null,null);Debug.Log("[Village3DNativeAudit] Native capture exit="+code);
            EditorApplication.update-=CompleteWhenStopped;EditorApplication.update+=CompleteWhenStopped;
            NativeSaveIsolation.Finish(Prefix,SessionState.GetString(Prefix+"token",""),code);
            if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.delayCall+=AfterNativeCleanup;
        }
        private static void OnPlayState(PlayModeStateChange state)
        {if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Prefix+"finishing",false)){EditorApplication.delayCall-=AfterNativeCleanup;EditorApplication.delayCall+=AfterNativeCleanup;}}
        private static void AfterNativeCleanup()
        {
            if(!SessionState.GetBool(Prefix+"finishing",false))return;
            if(EditorApplication.isPlayingOrWillChangePlaymode||SessionState.GetBool("CavesOfOoo.NativeSaveIsolation.active",false))
            {EditorApplication.update-=CompleteWhenStopped;EditorApplication.update+=CompleteWhenStopped;return;}
            EditorApplication.update-=CompleteWhenStopped;
            EditorApplication.delayCall-=AfterNativeCleanup;EditorApplication.playModeStateChanged-=OnPlayState;
            int code=SessionState.GetInt(Prefix+"code",4);string mode=SessionState.GetString(Prefix+"mode","");string root=SessionState.GetString(Prefix+"root","");
            var receipt=new Cleanup{mode=mode,privateRoot=root,privateRootRemoved=!string.IsNullOrEmpty(root)&&!Directory.Exists(root),nativeCleanupCode=SessionState.GetInt("CavesOfOoo.NativeSaveIsolation.lastCode",4)};
            if(!receipt.privateRootRemoved||receipt.nativeCleanupCode!=0)code=4;
            receipt.oldRequestedSeed=SessionState.GetInt(Prefix+"oldRequestedSeed",0);
            RestoreSeed();receipt.restoredRequestedSeed=NativeAuditBootstrapSettings.RequestedSeed;
            receipt.seedSettingRestored=receipt.oldRequestedSeed==receipt.restoredRequestedSeed;
            if(!receipt.seedSettingRestored)code=4;
            try{RestoreScenes();receipt.scenesRestored=true;}catch(Exception error){code=4;receipt.sceneError=error.ToString();Debug.LogError(error);}
            try{RestoreView();receipt.gameViewRestored=true;}catch(Exception error){code=4;receipt.viewError=error.ToString();Debug.LogError(error);}
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/Village3D"));Directory.CreateDirectory(dir);
            string native=Path.Combine(dir,"V3D-"+mode+"-native.json");
            try
            {
                string json=File.Exists(native)?File.ReadAllText(native):null;
                receipt.finalNativeVerified=ValidateFinalReportForRun(json,mode,root);
                // Do not label an older successful report as this attempt's run.
                if(receipt.finalNativeVerified)receipt.runId=JsonUtility.FromJson<FinalNative>(json).runId;
                else code=4;
            }
            catch(Exception error){code=4;receipt.reportError=error.ToString();}
            receipt.exitCode=code;File.WriteAllText(Path.Combine(dir,"V3D-"+mode+"-cleanup.json"),JsonUtility.ToJson(receipt,true));
            SessionState.SetBool(Prefix+"active",false);SessionState.SetBool(Prefix+"finishing",false);Debug.Log("[Village3DNativeAudit] Scene/view/private-save cleanup exit="+code);
            if(SessionState.GetBool(Prefix+"exit",false))EditorApplication.Exit(code);
        }
        private static bool ValidateFinalReportForRun(string json,string expectedMode,string privateRoot)
        {
            if(string.IsNullOrWhiteSpace(json)||string.IsNullOrWhiteSpace(privateRoot)
                ||(expectedMode!="before"&&expectedMode!="after"))return false;
            try
            {
                var final=JsonUtility.FromJson<FinalNative>(json);
                return final!=null&&Guid.TryParseExact(final.runId,"N",out var runId)&&runId!=Guid.Empty
                    &&string.Equals(final.mode,expectedMode,StringComparison.Ordinal)
                    &&string.Equals(final.saveRoot,privateRoot,StringComparison.Ordinal)
                    &&final.failures==0&&final.unexpectedErrors==0&&final.workloadComplete
                    &&final.shutdownObserved&&final.shutdownRootHeld&&final.shutdownSavingUnregistered
                    &&(expectedMode=="before"||final.displayPreferencesRestored);
            }
            catch(Exception){return false;}
        }
        private static void RestoreSeed()
        {
            if(!SessionState.GetBool(Prefix+"seedArmed",false))return;
            NativeAuditBootstrapSettings.RequestedSeed=SessionState.GetInt(Prefix+"oldRequestedSeed",0);
            SessionState.SetBool(Prefix+"seedArmed",false);
        }
        private static void RestoreScenes()
        {
            var saved=JsonUtility.FromJson<Scenes>(SessionState.GetString(Prefix+"scenes","{}"));
            if(saved?.rows==null||saved.rows.Length==0)return;
            var setup=new SceneSetup[saved.rows.Length];for(int i=0;i<setup.Length;i++)setup[i]=new SceneSetup{path=saved.rows[i].path,isLoaded=saved.rows[i].isLoaded,isActive=saved.rows[i].isActive};
            if(setup.Length==1&&string.IsNullOrEmpty(setup[0].path))EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            else EditorSceneManager.RestoreSceneManagerSetup(setup);
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
            for(int i=0;i<built+custom;i++)
            {
                var size=gt.GetMethod("GetGameViewSize",All).Invoke(group,new object[]{i});
                int w=(int)size.GetType().GetProperty("width",All).GetValue(size),h=(int)size.GetType().GetProperty("height",All).GetValue(size);
                if(w==1920&&h==1080){chosen=i;break;}
            }
            if(chosen<0)
            {
                var sizeType=EditorType("UnityEditor.GameViewSize");var enumType=EditorType("UnityEditor.GameViewSizeType");
                var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.Parse(enumType,"FixedResolution"),1920,1080,"Village3D audit 1920x1080"},null);
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
        [Serializable] private sealed class Scenes{public SceneRow[] rows;}
        [Serializable] private sealed class SceneRow{public string path;public bool isLoaded,isActive;}
        [Serializable] private sealed class FinalNative{public string runId,mode,saveRoot;public int failures,unexpectedErrors;public bool workloadComplete,shutdownObserved,shutdownRootHeld,shutdownSavingUnregistered,displayPreferencesRestored;}
        [Serializable] private sealed class Cleanup{public string runId,mode,privateRoot,sceneError,viewError,reportError;public bool privateRootRemoved,scenesRestored,gameViewRestored,finalNativeVerified,seedSettingRestored;public int nativeCleanupCode,exitCode,oldRequestedSeed,restoredRequestedSeed;}
    }
}
