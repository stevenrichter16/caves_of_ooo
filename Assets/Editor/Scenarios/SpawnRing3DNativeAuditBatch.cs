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
    public static class SpawnRing3DNativeAuditBatch
    {
        private const string Prefix="SpawnRing3D.NativeAudit.";
        private const int AuditWorldSeed=729490642;
        private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static SpawnRing3DNativeAuditBatch()
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
        [MenuItem("Caves Of Ooo/Scenarios/World/Spawn Ring 3D Acceptance")]
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
                string token=NativeSaveIsolation.Begin(Prefix,"SpawnRing3D-native-marker-"+Guid.NewGuid().ToString("N"),false);
                SessionState.SetString(Prefix+"token",token);SessionState.SetString(Prefix+"root",SaveGameService.SaveRootOverride);
                SessionState.SetBool(Prefix+"active",true);SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+900);
                Subscribe();EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");EditorApplication.isPlaying=true;
            }
            catch(Exception error)
            {
                Debug.LogError("[SpawnRing3DNativeAudit] Launch failed: "+error);
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
        {GameBootstrap.OnAfterBootstrap-=Apply;new GameObject("Spawn Ring 3D Native Acceptance").AddComponent<SpawnRing3DNativeAudit>().Initialize(SessionState.GetString(Prefix+"runId",""));}
        private static void Poll()
        {
            if(!SessionState.GetBool(Prefix+"active",false)||SessionState.GetBool(Prefix+"finishing",false))return;
            var driver=UnityEngine.Object.FindFirstObjectByType<SpawnRing3DNativeAudit>();
            if(driver!=null&&driver.Finished){driver.SetUnexpectedErrors(SessionState.GetInt(Prefix+"errors",0));Finish(driver.Failures==0?0:1);return;}
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Prefix+"deadline",0))
            {driver?.Abort("Native acceptance watchdog exceeded fifteen minutes.");Finish(2);}
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
            SaveGameService.RegisterRuntime(null,null);Debug.Log("[SpawnRing3DNativeAudit] Native capture exit="+code);
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
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/SpawnRing3D"));Directory.CreateDirectory(dir);
            string native=Path.Combine(dir,"R3D-"+runId+"-native.json");
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
            receipt.exitCode=code;File.WriteAllText(Path.Combine(dir,"R3D-"+runId+"-cleanup.json"),JsonUtility.ToJson(receipt,true));
            SessionState.SetBool(Prefix+"active",false);SessionState.SetBool(Prefix+"finishing",false);Debug.Log("[SpawnRing3DNativeAudit] Scene/view/private-save cleanup exit="+code);
            if(SessionState.GetBool(Prefix+"exit",false))EditorApplication.Exit(code);
        }
        private static bool ValidateFinalReportForRun(string json,string expectedRunId,string privateRoot)
        {
            if(string.IsNullOrWhiteSpace(json)||string.IsNullOrWhiteSpace(privateRoot)
                ||!Guid.TryParseExact(expectedRunId,"N",out var expected)||expected==Guid.Empty)return false;
            try
            {
                var final=JsonUtility.FromJson<FinalNative>(json);
                return final!=null&&string.Equals(final.runId,expectedRunId,StringComparison.Ordinal)
                    &&string.Equals(final.saveRoot,privateRoot,StringComparison.Ordinal)
                    &&final.failures==0&&final.unexpectedErrors==0&&final.workloadComplete
                    &&final.worldSeed==AuditWorldSeed&&final.screenWidth==1920&&final.screenHeight==1080
                    &&final.shutdownObserved&&final.shutdownRootHeld&&final.shutdownSavingUnregistered
                    &&final.displayPreferencesRestored&&final.inputSettingsRestored&&CompleteCoverage(final,expectedRunId)
                    &&ValidateRequestedProfile(final.profile,expectedRunId,privateRoot);
            }
            catch(Exception){return false;}
        }
        private static bool CompleteCoverage(FinalNative final,string runId)
        {
            if(final.nativeSteps<2||final.checks==null||final.checks.Length==0||final.checks.Any(c=>c==null||!c.pass)
                ||final.borders==null||final.borders.Length!=24||final.zones==null||final.zones.Length!=8||final.screenshots==null)return false;
            var edges=new HashSet<string>(StringComparer.Ordinal);var expectedZones=new HashSet<string>(StringComparer.Ordinal);
            for(int y=5;y<=7;y++)for(int x=2;x<=4;x++)
            {
                string from="Overworld."+x+"."+y+".0";
                if(x!=3||y!=6)expectedZones.Add(from);
                if(x<4){string to="Overworld."+(x+1)+"."+y+".0";edges.Add(from+">"+to);edges.Add(to+">"+from);}
                if(y<7){string to="Overworld."+x+"."+(y+1)+".0";edges.Add(from+">"+to);edges.Add(to+">"+from);}
            }
            if(final.borders.Any(b=>b==null||!b.pass||b.tickBefore!=b.tickAfter||!edges.Remove(b.from+">"+b.to))||edges.Count!=0)return false;
            foreach(var z in final.zones)
                if(z==null||!expectedZones.Remove(z.zoneId)||!z.presentationReady||!z.presentationVisible||!z.playerVisible||z.fullReveal
                    ||string.IsNullOrEmpty(z.playerId)||string.IsNullOrEmpty(z.modelId)||z.groundPatches!=40)return false;
            if(expectedZones.Count!=0)return false;
            foreach(string name in new[]{"native_N_real_new_game","two_ordinary_road_steps_with_live_turns","all_24_distinct_directed_borders",
                "all_eight_actual_current_zone_captures","real_fen_water_visible_before_erasure","native_erasure_removes_represented_water",
                "native_F5_preserves_player_turn_boundary","native_F6_exact_fen_water_and_saved_erasure","native_F6_all_cached_tile_snapshots",
                "native_F6_cached_owner_reference_replacement","presenter_rejects_old_player_reference","native_UI_display_controls_no_turn",
                "unmapped_factory_item_keeps_native_fallback_eligibility","fallback_fixture_removed","private_boot_marker_unchanged","owned_save_only"})
                if(!final.checks.Any(c=>c.name==name&&c.pass))return false;
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/SpawnRing3D"));
            var captureNames=new HashSet<string>(StringComparer.Ordinal);
            foreach(string file in final.screenshots)
            {
                if(string.IsNullOrEmpty(file)||Path.GetDirectoryName(Path.GetFullPath(file))!=directory
                    ||!Path.GetFileName(file).StartsWith("R3D-"+runId+"-",StringComparison.Ordinal)
                    ||!captureNames.Add(Path.GetFileName(file))||!Is1080pPng(file))return false;
            }
            var labels=new List<string>{"center-new-game","fen-F6-saved-erasure","look-ui","inventory-ui","original-view","low-detail","unmapped-dagger-fallback-fixture"};
            for(int y=5;y<=7;y++)for(int x=2;x<=4;x++)if(x!=3||y!=6)labels.Add("zone-Overworld."+x+"."+y+".0");
            return labels.All(label=>captureNames.Contains("R3D-"+runId+"-"+label+".png"));
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
        private static bool ValidateRequestedProfile(SpawnRing3DNativeAudit.ProfileReport profile,string runId,string root)
        {
            string requested=null;var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-spawnRing3dProfile")requested=args[i+1];
            // JsonUtility serializes a nested null class as its default object.
            // Accept exactly that empty roundtrip; any recorded work still needs
            // the explicitly requested, complete profile and raw-file checks.
            if(string.IsNullOrEmpty(requested))return profile==null
                ||JsonUtility.ToJson(profile)==JsonUtility.ToJson(new SpawnRing3DNativeAudit.ProfileReport());
            if((requested!="full"&&requested!="low")||profile==null||profile.mode!=requested||profile.runId!=runId||profile.saveRoot!=root
                ||!profile.valid||!profile.complete||profile.overflow||profile.badResolutionFrames!=0||profile.steadySeconds<80||profile.steadySeconds>90
                ||profile.worldSeed!=AuditWorldSeed||profile.screenWidth!=1920||profile.screenHeight!=1080||profile.phases==null||profile.phases.Length!=24)return false;
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/SpawnRing3D","R3D-"+runId+"-profile-"+requested+"-frames.csv.gz"));
            if(profile.rawFrames!=path||!File.Exists(path)||new FileInfo(path).Length<100)return false;
            using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(path))
                return string.Equals(profile.rawSha256,BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant(),StringComparison.Ordinal);
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
                var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.Parse(enumType,"FixedResolution"),1920,1080,"Spawn ring audit 1920x1080"},null);
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
        [Serializable] private sealed class FinalNative{public SpawnRing3DNativeAudit.ProfileReport profile;public string runId,saveRoot;public int failures,unexpectedErrors,worldSeed,screenWidth,screenHeight,nativeSteps;
            public SpawnRing3DNativeAudit.CheckRow[] checks;public SpawnRing3DNativeAudit.BorderRow[] borders;public SpawnRing3DNativeAudit.ZoneRow[] zones;public string[] screenshots;
            public bool workloadComplete,shutdownObserved,shutdownRootHeld,shutdownSavingUnregistered,displayPreferencesRestored,inputSettingsRestored;}
        [Serializable] private sealed class Cleanup{public ErrorRow[] errors;public bool errorLedgerTruncated;public string runId,privateRoot,sceneError,viewError,reportError;public bool privateRootRemoved,scenesRestored,gameViewRestored,finalNativeVerified,seedSettingRestored,inheritedSaveRootRestored,lastGamePreferenceRestored;public int nativeCleanupCode,exitCode,oldRequestedSeed,restoredRequestedSeed,totalUnexpectedErrors;}
    }
}
