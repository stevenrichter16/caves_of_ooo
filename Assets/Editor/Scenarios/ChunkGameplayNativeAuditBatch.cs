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
    public static class ChunkGameplayNativeAuditBatch
    {
        private const string Prefix="ChunkGameplay.NativeAudit.";
        private const int AuditWorldSeed=64;
        private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static ChunkGameplayNativeAuditBatch()
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
        public static void RunFromCommandLine()
        {
            // Let editor startup/indexing complete before requesting the Play
            // domain reload. Launching Play during Search initialization can
            // invalidate its pending database enumeration in a copied project.
            double earliest=EditorApplication.timeSinceStartup+12;
            void StartWhenSettled()
            {
                if(EditorApplication.isCompiling||EditorApplication.isUpdating){earliest=EditorApplication.timeSinceStartup+12;return;}
                if(EditorApplication.timeSinceStartup<earliest)return;
                EditorApplication.update-=StartWhenSettled;RunCore(true);
            }
            EditorApplication.update+=StartWhenSettled;
        }
        [MenuItem("Caves Of Ooo/Scenarios/World/Chunk Gameplay Acceptance")]
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
                string token=NativeSaveIsolation.Begin(Prefix,"ChunkGameplay-native-marker-"+Guid.NewGuid().ToString("N"),false);
                SessionState.SetString(Prefix+"token",token);SessionState.SetString(Prefix+"root",SaveGameService.SaveRootOverride);
                SessionState.SetBool(Prefix+"active",true);SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+1100);
                Subscribe();EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");EditorApplication.isPlaying=true;
            }
            catch(Exception error)
            {
                Debug.LogError("[ChunkGameplayNativeAudit] Launch failed: "+error);
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
        {GameBootstrap.OnAfterBootstrap-=Apply;new GameObject("Chunk Gameplay Native Acceptance").AddComponent<ChunkGameplayNativeAudit>().Initialize(SessionState.GetString(Prefix+"runId",""));}
        private static void Poll()
        {
            if(!SessionState.GetBool(Prefix+"active",false)||SessionState.GetBool(Prefix+"finishing",false))return;
            var driver=UnityEngine.Object.FindFirstObjectByType<ChunkGameplayNativeAudit>();
            if(driver!=null&&driver.Finished)
            {
                int code=4;
                try { driver.SetUnexpectedErrors(SessionState.GetInt(Prefix+"errors",0));code=driver.Failures==0?0:1; }
                finally { Finish(code); }
                return;
            }
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Prefix+"deadline",0))
            {
                try { driver?.Abort("Native acceptance watchdog exceeded eighteen minutes."); }
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
            SaveGameService.RegisterRuntime(null,null);Debug.Log("[ChunkGameplayNativeAudit] Native capture exit="+code);
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
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/ChunkGameplayImplementation"));Directory.CreateDirectory(dir);
            string native=Path.Combine(dir,"CGN-"+runId+"-native.json");
            try
            {
                string json=File.Exists(native)?File.ReadAllText(native):null;
                receipt.finalNativeVerified=ValidateFinalReportForRun(json,runId,root);
                if(receipt.finalNativeVerified)
                {
                    var counter=JsonUtility.FromJson<ChunkGameplayNativeAudit.Report>(json);
                    double duration=counter.profileSeconds;counter.profileSeconds=59;
                    bool shortRejected=!ValidateProfile(counter);counter.profileSeconds=duration;
                    var metrics=counter.profileMetrics;counter.profileMetrics=null;
                    bool absentRejected=!ValidateProfile(counter);counter.profileMetrics=metrics;
                    bool available=metrics[0].available;metrics[0].available=false;
                    bool unavailableRejected=!ValidateProfile(counter);metrics[0].available=available;
                    int count=metrics[0].count;metrics[0].count=0;
                    bool emptyRejected=!ValidateProfile(counter);metrics[0].count=count;
                    string units=metrics[0].units;metrics[0].units="Bytes";
                    bool wrongUnitsRejected=!ValidateProfile(counter);metrics[0].units=units;
                    receipt.profileCounterchecksPassed=shortRejected&&absentRejected&&unavailableRejected&&emptyRejected&&wrongUnitsRejected&&ValidateProfile(counter);
                    if(!receipt.profileCounterchecksPassed)code=4;
                }
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
            receipt.exitCode=code;File.WriteAllText(Path.Combine(dir,"CGN-"+runId+"-cleanup.json"),JsonUtility.ToJson(receipt,true));
            SessionState.SetBool(Prefix+"active",false);SessionState.SetBool(Prefix+"finishing",false);Debug.Log("[ChunkGameplayNativeAudit] Scene/view/private-save cleanup exit="+code);
            if(SessionState.GetBool(Prefix+"exit",false))EditorApplication.Exit(code);
        }
        private static readonly string[] RequiredChecks={"native_N_real_new_game","native_cache_and_parcel_generated",
            // R4: the earned-material inspection and its bell use are part of the accepted journey.
            "material_native_single_examine","material_native_description_visible","material_native_returns_to_row",
            "material_native_examine_spends_nothing","material_native_bell_accepted","material_native_bell_diagnosed",
            "material_native_quiet_bell","material_native_no_repeat_payment","material_native_quiet_reaches_watch",
            "material_native_bell_reported","material_native_bell_restored",
            "native_opening_starts_vulnerable","native_opening_completed_without_debug",
            "native_border_east_initial","quest_cue_available","native_conversation_accept","quest_cue_active","native_Q_journal",
            "native_border_west_recovery","field_camera_restored","native_container_command_mode","native_TakeFromContainer_parcel","native_border_east_delivery",
            "native_carried_parcel_crosses_border","native_conversation_delivery","native_one_reward","native_parcel_laid_at_supper",
            "quest_cue_completed","native_no_repeat_delivery_choice","native_regional_directions","native_travel_notes","native_notes_restored","native_F5_owned_checkpoint","native_F6_reloads_completion",
            "quest_cue_reloaded","camera_and_reveal_untouched","private_boot_marker_unchanged","owned_save_only","native_profile_evidence",
            "regional_native_available","regional_native_read","regional_native_preview_walkaway","regional_native_accept",
            "regional_native_release_preview","regional_native_reaccept","regional_native_active_note","regional_native_harvest","regional_native_grove_law",
            "regional_native_delivery","regional_native_reward","regional_native_completed_cue","regional_native_no_repeat_command",
            "fullscreen_native_trade_clean","fullscreen_native_faction_clean","fullscreen_native_world_restored",
            "regional_native_note_visible","regional_native_receipt_visible","regional_native_delivered_stock_in_trade",
            "regional_native_reloaded_receipt_visible","regional_native_reloaded_no_repeat","regional_native_debug_restored","regional_native_reload","regional_native_mined_source_stays_gone",
            "native_border_regional_west_spawn","native_border_regional_north_stump","native_border_regional_west_source",
            "native_border_regional_east_stump","native_border_regional_south_spawn","native_border_regional_east_recipient",
            // SA.6: the Stillleaf Archive chain is part of the accepted journey.
            "stillleaf_native_world_map_quillhold","stillleaf_native_quillhold_arrival","stillleaf_native_accept","stillleaf_native_Q_field_note",
            "stillleaf_native_world_map_salt_vault","stillleaf_native_salt_vault_arrival","stillleaf_native_retrieve","stillleaf_native_agree","stillleaf_native_take_key",
            "stillleaf_native_world_map_stillleaf","stillleaf_native_stillleaf_arrival","stillleaf_native_vault_floor","stillleaf_native_door_unlock_bump","stillleaf_native_door_offers_reseal",
            "stillleaf_native_register_pickup","stillleaf_native_debug_restored","stillleaf_native_world_map_quillhold_return","stillleaf_native_deliver","stillleaf_native_no_repeat_deliver",
            "stillleaf_native_world_map_salt_vault_return","stillleaf_native_told_curation"};
        private static bool ValidateFinalReportForRun(string json,string expectedRunId,string privateRoot)
        {
            if(string.IsNullOrWhiteSpace(json)||string.IsNullOrWhiteSpace(privateRoot)
                ||!Guid.TryParseExact(expectedRunId,"N",out var expected)||expected==Guid.Empty)return false;
            try
            {
                var f=JsonUtility.FromJson<ChunkGameplayNativeAudit.Report>(json);
                if(f==null||f.runId!=expectedRunId||f.saveRoot!=privateRoot||f.failures!=0||f.unexpectedErrors!=0||!f.workloadComplete
                    ||f.worldSeed!=AuditWorldSeed||f.screenWidth!=1920||f.screenHeight!=1080||f.nativeSteps<3
                    ||!f.shutdownObserved||!f.shutdownRootHeld||!f.shutdownSavingUnregistered||!f.displayPreferencesRestored||!f.inputSettingsRestored
                    ||!ValidateProfile(f)
                    ||f.checks==null||f.checks.Any(c=>c==null||!c.pass||string.IsNullOrEmpty(c.name))
                    ||f.checks.Select(c=>c.name).Distinct(StringComparer.Ordinal).Count()!=f.checks.Length
                    ||RequiredChecks.Any(name=>!f.checks.Any(c=>c.name==name)))return false;
                foreach(string room in new[]{"keeper-gatehouse","dry-hem-guesthouse","long-loop-ropeshop","return-desk-archive","second-bowl-kitchen"})
                    if(!f.checks.Any(c=>c.name=="coarse_interior_"+room)||!f.checks.Any(c=>c.name=="coarse_closed_"+room))return false;
                if(f.screenshots==null||f.screenshots.Length!=33||f.screenshots.Distinct(StringComparer.Ordinal).Count()!=33)return false;
                string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/ChunkGameplayImplementation"));
                foreach(string label in new[]{"western-spawn","cue-available","cue-active","journal","recovered-parcel","completed-supper","travel-notes","restored-completion","interior-keeper-gatehouse","interior-dry-hem-guesthouse","interior-long-loop-ropeshop","interior-return-desk-archive","interior-second-bowl-kitchen","regional-request","regional-offer-preview","regional-accepted-note","regional-protected-vein","regional-delivered","regional-notes","regional-trade-stock","regional-receipt-restored","faction-standings","fullscreen-world-restored","material-fire-clay-examine","material-quiet-bell"})
                {
                    string file=Path.Combine(dir,"CGN-"+expectedRunId+"-"+label+".png");
                    if(!f.screenshots.Contains(file)||!Is1080pPng(file))return false;
                }
                return true;
            }
            catch(Exception){return false;}
        }
        private static bool ValidateProfile(ChunkGameplayNativeAudit.Report report)
        {
            if(report==null||report.profileSeconds<60||report.profileSeconds>90||report.profileMetrics==null||report.profileMetrics.Length!=4)return false;
            foreach(string name in new[]{"Main Thread","COO.Input.Update","COO.ZoneRenderer.LateUpdate","GC Allocated In Frame"})
            {
                var matches=report.profileMetrics.Where(m=>m!=null&&m.name==name).ToArray();if(matches.Length!=1)return false;
                var metric=matches[0];bool gc=name=="GC Allocated In Frame";
                if(!metric.available||metric.count<=1||metric.units!=(gc?"Bytes":"TimeNanoseconds")||metric.sum<0||metric.max<0||(!gc&&metric.max==0))return false;
            }
            return true;
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
                var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.Parse(enumType,"FixedResolution"),1920,1080,"Chunk gameplay audit 1920x1080"},null);
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
        [Serializable] private sealed class Cleanup{public ErrorRow[] errors;public bool errorLedgerTruncated,profileCounterchecksPassed;public string runId,privateRoot,sceneError,viewError,reportError;public bool privateRootRemoved,scenesRestored,gameViewRestored,finalNativeVerified,seedSettingRestored,inheritedSaveRootRestored,lastGamePreferenceRestored;public int nativeCleanupCode,exitCode,oldRequestedSeed,restoredRequestedSeed,totalUnexpectedErrors;}
    }
}
