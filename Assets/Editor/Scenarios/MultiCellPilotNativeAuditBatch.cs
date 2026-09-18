using System;
using System.Collections.Generic;
using System.Linq;
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
    public static class MultiCellPilotNativeAuditBatch
    {
        private const string Prefix="MultiCellPilot.NativeAudit.";
        private const int AuditWorldSeed=729490642;
        private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static MultiCellPilotNativeAuditBatch()
        {
            // Save isolation can finish before this type initializes after a reload.
            // Its owner-active flag is then false while our scene/view cleanup remains.
            if(SessionState.GetBool(Prefix+"finishing",false))
            { EditorApplication.update+=CompleteWhenStopped; Application.logMessageReceived-=OnLog; Application.logMessageReceived+=OnLog; }
            else if(SessionState.GetBool(Prefix+"active",false))Subscribe();
        }
        private static void CompleteWhenStopped()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode
                || SessionState.GetBool("CavesOfOoo.NativeSaveIsolation.active",false))return;
            EditorApplication.update-=CompleteWhenStopped;
            AfterNativeCleanup();
        }
        public static void RunFromCommandLine()=>RunCore(true);
        [MenuItem("Caves Of Ooo/Scenarios/World/Multi-Cell Ridge Acceptance")]
        public static void Launch()=>RunCore(false);
        private static void RunCore(bool exitEditor)
        {
            if(Application.isBatchMode)throw new InvalidOperationException("Use a native editor without -batchmode; actual GameView screenshots require rendering.");
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit current Play before starting the audit.");
            if(SessionState.GetBool(Prefix+"active",false))throw new InvalidOperationException("Audit already active.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save current scene edits first; audit refuses to discard them.");
            var saved=new Scenes{rows=new SceneRow[setup.Length]};for(int i=0;i<setup.Length;i++)saved.rows[i]=new SceneRow{path=setup[i].path,isLoaded=setup[i].isLoaded,isActive=setup[i].isActive};
            SessionState.SetString(Prefix+"scenes",JsonUtility.ToJson(saved));SessionState.SetString(Prefix+"runId",Guid.NewGuid().ToString("N"));
            SessionState.SetBool(Prefix+"oldSaveRootWasNull",SaveGameService.SaveRootOverride==null);
            SessionState.SetString(Prefix+"oldSaveRoot",SaveGameService.SaveRootOverride??"");
            SessionState.SetBool(Prefix+"oldLastGamePresent",PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey));
            SessionState.SetString(Prefix+"oldLastGame",PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));
            SessionState.SetInt(Prefix+"oldRequestedSeed",NativeAuditBootstrapSettings.RequestedSeed);
            SessionState.SetBool(Prefix+"seedArmed",false);
            SessionState.SetBool(Prefix+"exit",exitEditor);SessionState.SetBool(Prefix+"finishing",false);SessionState.SetInt(Prefix+"code",4);
            SessionState.SetInt(Prefix+"errors",0);SessionState.SetString(Prefix+"errorLedger","{}");SessionState.SetBool(Prefix+"viewConfigured",false);
            try
            {
                Configure1080p();
                string token=NativeSaveIsolation.Begin(Prefix,"MultiCellPilot-native-marker-"+Guid.NewGuid().ToString("N"),false);
                SessionState.SetString(Prefix+"token",token);SessionState.SetString(Prefix+"root",SaveGameService.SaveRootOverride);
                SessionState.SetBool(Prefix+"active",true);SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+900);
                Subscribe();EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");EditorApplication.isPlaying=true;
            }
            catch(Exception error)
            {
                Debug.LogError("[MultiCellPilotNativeAudit] Launch failed: "+error);
                if(SessionState.GetBool(Prefix+"active",false))Finish(4);else{RestoreSeed();RestoreView();RestoreScenes();throw;}
            }
        }
        private static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix,SessionState.GetString(Prefix+"token",""),Finish);
            // SessionState preserves the original setting across domain reload. Re-arm
            // before scene Start, after re-establishing the same private audit root.
            NativeAuditBootstrapSettings.RequestedSeed=AuditWorldSeed;
            SessionState.SetBool(Prefix+"seedArmed",true);
            GameBootstrap.OnAfterBootstrap-=Apply;GameBootstrap.OnAfterBootstrap+=Apply;
            EditorApplication.update-=Poll;EditorApplication.update+=Poll;
            Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;
            EditorApplication.playModeStateChanged-=OnPlayState;EditorApplication.playModeStateChanged+=OnPlayState;
        }
        private static void Apply(Zone zone,EntityFactory factory,Entity player,TurnManager turns)
        {GameBootstrap.OnAfterBootstrap-=Apply;new GameObject("Multi-Cell Ridge Native Acceptance").AddComponent<MultiCellPilotNativeAudit>().Initialize(SessionState.GetString(Prefix+"runId",""));}
        private static void Poll()
        {
            if(!SessionState.GetBool(Prefix+"active",false)||SessionState.GetBool(Prefix+"finishing",false))return;
            var driver=UnityEngine.Object.FindFirstObjectByType<MultiCellPilotNativeAudit>();
            if(driver!=null&&driver.Finished)
            {
                int code=4;
                try { driver.SetUnexpectedErrors(SessionState.GetInt(Prefix+"errors",0));code=driver.Failures==0?0:1; }
                finally { Finish(code); }
                return;
            }
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Prefix+"deadline",0))
            {
                try { driver?.Abort("Native acceptance watchdog exceeded fifteen minutes."); }
                finally { Finish(2); }
            }
        }
        private static void OnLog(string message,string trace,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
            SessionState.SetInt(Prefix+"errors",SessionState.GetInt(Prefix+"errors",0)+1);
            var ledger=JsonUtility.FromJson<ErrorLedger>(SessionState.GetString(Prefix+"errorLedger","{}"))??new ErrorLedger();
            if(ledger.rows==null)ledger.rows=Array.Empty<ErrorRow>();
            if(ledger.rows.Length<64)
            {
                int n=ledger.rows.Length;Array.Resize(ref ledger.rows,n+1);
                ledger.rows[n]=new ErrorRow{type=type.ToString(),message=message,trace=trace,editorSeconds=EditorApplication.timeSinceStartup};
                SessionState.SetString(Prefix+"errorLedger",JsonUtility.ToJson(ledger));
            }
        }
        private static void Finish(int code)
        {
            if(SessionState.GetBool(Prefix+"finishing",false))return;
            SessionState.SetBool(Prefix+"finishing",true);SessionState.SetInt(Prefix+"code",code);
            GameBootstrap.OnAfterBootstrap-=Apply;EditorApplication.update-=Poll;
            SaveGameService.RegisterRuntime(null,null);Debug.Log("[MultiCellPilotNativeAudit] Native capture exit="+code);
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
            int code=SessionState.GetInt(Prefix+"code",4);string runId=SessionState.GetString(Prefix+"runId","");string root=SessionState.GetString(Prefix+"root","");
            var receipt=new Cleanup{runId=runId,privateRoot=root,privateRootRemoved=!string.IsNullOrEmpty(root)&&!Directory.Exists(root),nativeCleanupCode=SessionState.GetInt("CavesOfOoo.NativeSaveIsolation.lastCode",4)};
            if(!receipt.privateRootRemoved||receipt.nativeCleanupCode!=0)code=4;
            receipt.oldRequestedSeed=SessionState.GetInt(Prefix+"oldRequestedSeed",0);
            RestoreSeed();receipt.restoredRequestedSeed=NativeAuditBootstrapSettings.RequestedSeed;
            receipt.seedSettingRestored=receipt.oldRequestedSeed==receipt.restoredRequestedSeed;
            if(!receipt.seedSettingRestored)code=4;
            try{RestoreScenes();receipt.scenesRestored=SceneSetupRestored();if(!receipt.scenesRestored)code=4;}catch(Exception error){code=4;receipt.sceneError=error.ToString();Debug.LogError(error);}
            try{RestoreView();receipt.gameViewRestored=ViewConfigurationRestored();if(!receipt.gameViewRestored)code=4;}catch(Exception error){code=4;receipt.viewError=error.ToString();Debug.LogError(error);}
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/MultiCellPilot"));Directory.CreateDirectory(dir);
            string native=Path.Combine(dir,"MCN-"+runId+"-native.json");
            try
            {
                string json=File.Exists(native)?File.ReadAllText(native):null;
                receipt.finalNativeVerified=ValidateFinalReportForRun(json,runId,root);
                // Do not label an older successful report as this attempt's run.
                if(!receipt.finalNativeVerified)code=4;
            }
            catch(Exception error){code=4;receipt.reportError=error.ToString();}
            string oldRoot=SessionState.GetBool(Prefix+"oldSaveRootWasNull",false)?null:SessionState.GetString(Prefix+"oldSaveRoot","");
            receipt.inheritedSaveRootRestored=string.Equals(SaveGameService.SaveRootOverride,oldRoot,StringComparison.Ordinal);
            bool hadPref=SessionState.GetBool(Prefix+"oldLastGamePresent",false);
            receipt.lastGamePreferenceRestored=PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey)==hadPref
                &&(!hadPref||PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey)==SessionState.GetString(Prefix+"oldLastGame",""));
            if(!receipt.inheritedSaveRootRestored||!receipt.lastGamePreferenceRestored)code=4;
            receipt.totalUnexpectedErrors=SessionState.GetInt(Prefix+"errors",0);
            receipt.errors=JsonUtility.FromJson<ErrorLedger>(SessionState.GetString(Prefix+"errorLedger","{}"))?.rows??Array.Empty<ErrorRow>();
            receipt.errorLedgerTruncated=receipt.totalUnexpectedErrors>receipt.errors.Length;
            if(receipt.totalUnexpectedErrors!=0)code=4;
            Application.logMessageReceived-=OnLog;
            receipt.exitCode=code;File.WriteAllText(Path.Combine(dir,"MCN-"+runId+"-cleanup.json"),JsonUtility.ToJson(receipt,true));
            SessionState.SetBool(Prefix+"active",false);SessionState.SetBool(Prefix+"finishing",false);Debug.Log("[MultiCellPilotNativeAudit] Scene/view/private-save cleanup exit="+code);
            if(SessionState.GetBool(Prefix+"exit",false))EditorApplication.Exit(code);
        }
        private static readonly string[] RequiredChecks={"native_N_real_new_game","native_south_entry","authored_native_owner_contract","native_presenter_binding",
            "physical_edge_selection_and_hole","native_physical_edge_pick","physical_edge_reach","one_structural_damage_owner",
            "destroy_clears_whole_body_once","duplicate_destroy_is_idempotent","destroyed_owner_view_removed","large_creature_path_respects_full_body",
            "large_creature_forced_motion_and_restoration","large_creature_blocked_far_edge_atomic",
            "pipe_force_move_whole_body","pipe_blocked_displacement_atomic","pipe_native_drag_preserves_grip","pipe_not_takeable_or_throwable",
            "revisit_preserves_damage_absence_and_pipe","native_F5_preserves_player_boundary","postcheckpoint_countermutation",
            "native_F6_exact_owner_and_tile_state","native_F6_replaces_all_owner_aliases","native_pilot_80second_profile_complete",
            "private_boot_marker_unchanged","owned_save_only"};
        private static bool ValidateReceiptMetadata(string json,string expectedRunId,string privateRoot)
        {
            if(string.IsNullOrWhiteSpace(json)||string.IsNullOrWhiteSpace(privateRoot)
                ||!Guid.TryParseExact(expectedRunId,"N",out var expected)||expected==Guid.Empty)return false;
            try
            {
                var f=JsonUtility.FromJson<FinalNative>(json);
                if(f==null||f.runId!=expectedRunId||f.saveRoot!=privateRoot||f.failures!=0||f.unexpectedErrors!=0||!f.workloadComplete
                    ||f.worldSeed!=AuditWorldSeed||f.screenWidth!=1920||f.screenHeight!=1080||f.nativeSteps<2
                    ||!f.shutdownObserved||!f.shutdownRootHeld||!f.shutdownSavingUnregistered||!f.displayPreferencesRestored||!f.inputSettingsRestored
                    ||f.checks==null||f.checks.Any(c=>c==null||!c.pass||string.IsNullOrEmpty(c.name))
                    ||f.checks.Select(c=>c.name).Distinct(StringComparer.Ordinal).Count()!=f.checks.Length
                    ||RequiredChecks.Any(name=>!f.checks.Any(c=>c.name==name)))return false;
                var p=f.profile;
                if(p==null||p.runId!=expectedRunId||p.saveRoot!=privateRoot||p.mode!="full"||!p.valid||!p.complete||p.overflow
                    ||p.frames<=1||p.badResolutionFrames!=0||p.worldSeed!=AuditWorldSeed||p.screenWidth!=1920||p.screenHeight!=1080
                    ||p.steadySeconds<80||p.steadySeconds>90||p.phases==null||p.phases.Length!=4
                    ||string.IsNullOrWhiteSpace(p.rawFrames)||p.rawSha256==null||p.rawSha256.Length!=64
                    ||p.rawSha256.Any(c=>!(c>='0'&&c<='9')&&!(c>='a'&&c<='f')))return false;
                var kinds=new HashSet<string>(new[]{"pristine_idle","pristine_walk","restored_idle","restored_walk"},StringComparer.Ordinal);
                foreach(var phase in p.phases)
                {
                    if(phase==null||!kinds.Remove(phase.kind)||phase.zoneId!=MultiCellPilotRuntime.ZoneID||phase.seconds<20||phase.seconds>22.5||phase.frames<=1)return false;
                    if(phase.kind.EndsWith("_idle",StringComparison.Ordinal))
                    {if(phase.startTick!=phase.endTick||phase.startX!=phase.endX||phase.startY!=phase.endY)return false;}
                    else if(phase.successfulSteps<2||phase.inputAttempts<phase.successfulSteps||phase.endTick<=phase.startTick)return false;
                }
                return kinds.Count==0&&Math.Abs(p.phases.Sum(phase=>phase.seconds)-p.steadySeconds)<.01;
            }
            catch(Exception){return false;}
        }
        private static bool ValidateFinalReportForRun(string json,string expectedRunId,string privateRoot)
        {
            if(!ValidateReceiptMetadata(json,expectedRunId,privateRoot))return false;
            try
            {
                var f=JsonUtility.FromJson<FinalNative>(json);var p=f.profile;
                string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/MultiCellPilot"));
                string stem="MCN-"+expectedRunId;
                if(f.screenshots==null||f.screenshots.Length!=3||f.screenshots.Distinct(StringComparer.Ordinal).Count()!=3)return false;
                foreach(var label in new[]{"south-entry","destructible-ridge","restored-save"})
                {
                    string path=Path.Combine(directory,stem+"-"+label+".png");
                    if(!f.screenshots.Contains(path)||!Is1080pPng(path))return false;
                }
                string frames=Path.Combine(directory,stem+"-profile-full-frames.csv.gz");
                if(p.rawFrames!=frames||!File.Exists(frames)||new FileInfo(frames).Length<100)return false;
                using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(frames))
                    if(p.rawSha256!=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant())return false;
                if(p.metrics==null)return false;
                for(int phase=0;phase<4;phase++)foreach(string name in new[]{"COO.ZoneRenderer.LateUpdate","COO.Input.Update","Main Thread","GC Allocated In Frame"})
                    if(!p.metrics.Any(m=>m!=null&&m.phase==phase&&m.name==name&&m.available&&m.count>1&&(name=="GC Allocated In Frame"||m.positive>0)))return false;
                foreach(string suffix in new[]{"owners","tiles"})
                {
                    string saved=Path.Combine(directory,stem+"-saved-"+suffix+".json"),loaded=Path.Combine(directory,stem+"-loaded-"+suffix+".json");
                    if(!File.Exists(saved)||!File.Exists(loaded)||new FileInfo(saved).Length<10||File.ReadAllText(saved)!=File.ReadAllText(loaded))return false;
                }
                return true;
            }
            catch(Exception){return false;}
        }
        private static bool Is1080pPng(string file)
        {
            if(!File.Exists(file)||new FileInfo(file).Length<1000)return false;
            var header=new byte[24];using(var stream=File.OpenRead(file))if(stream.Read(header,0,header.Length)!=header.Length)return false;
            byte[] signature={137,80,78,71,13,10,26,10};for(int i=0;i<signature.Length;i++)if(header[i]!=signature[i])return false;
            if(header[12]!='I'||header[13]!='H'||header[14]!='D'||header[15]!='R')return false;
            int width=(header[16]<<24)|(header[17]<<16)|(header[18]<<8)|header[19];
            int height=(header[20]<<24)|(header[21]<<16)|(header[22]<<8)|header[23];return width==1920&&height==1080;
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
        private static bool SceneSetupRestored()
        {
            var saved=JsonUtility.FromJson<Scenes>(SessionState.GetString(Prefix+"scenes","{}"));
            var actual=EditorSceneManager.GetSceneManagerSetup();
            if(saved?.rows==null||saved.rows.Length!=actual.Length)return false;
            for(int i=0;i<actual.Length;i++)
                if(saved.rows[i].path!=actual[i].path||saved.rows[i].isLoaded!=actual[i].isLoaded||saved.rows[i].isActive!=actual[i].isActive)return false;
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)return false;
            return true;
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
                var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.Parse(enumType,"FixedResolution"),1920,1080,"Multi-cell ridge audit 1920x1080"},null);
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
        private static bool ViewConfigurationRestored()
        {
            var viewType=EditorType("UnityEditor.GameView");var views=Resources.FindObjectsOfTypeAll(viewType);
            bool hadView=SessionState.GetBool(Prefix+"hadView",true);
            var group=Group();int count=(int)group.GetType().GetMethod("GetCustomCount",All).Invoke(group,null);
            if(count!=SessionState.GetInt(Prefix+"oldCustomCount",-1))return false;
            if(!hadView)return views.Length==0;
            // RestoreView uses the same GetWindow selection path as Configure1080p.
            return views.Length>0&&(int)viewType.GetProperty("selectedSizeIndex",All).GetValue(View())==SessionState.GetInt(Prefix+"oldViewIndex",-1);
        }
        [Serializable] private sealed class ErrorRow{public string type,message,trace;public double editorSeconds;}
        [Serializable] private sealed class ErrorLedger{public ErrorRow[] rows;}
        [Serializable] private sealed class Scenes{public SceneRow[] rows;}
        [Serializable] private sealed class SceneRow{public string path;public bool isLoaded,isActive;}
        [Serializable] private sealed class FinalNative{public MultiCellPilotNativeAudit.ProfileReport profile;public string runId,saveRoot;public int failures,unexpectedErrors,worldSeed,screenWidth,screenHeight,nativeSteps;
            public MultiCellPilotNativeAudit.CheckRow[] checks;public string[] screenshots;
            public bool workloadComplete,shutdownObserved,shutdownRootHeld,shutdownSavingUnregistered,displayPreferencesRestored,inputSettingsRestored;}
        [Serializable] private sealed class Cleanup{public ErrorRow[] errors;public bool errorLedgerTruncated;public string runId,privateRoot,sceneError,viewError,reportError;public bool privateRootRemoved,scenesRestored,gameViewRestored,finalNativeVerified,seedSettingRestored,inheritedSaveRootRestored,lastGamePreferenceRestored;public int nativeCleanupCode,exitCode,oldRequestedSeed,restoredRequestedSeed,totalUnexpectedErrors;}
    }
}
