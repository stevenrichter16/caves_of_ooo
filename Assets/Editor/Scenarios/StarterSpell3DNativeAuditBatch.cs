using System;
using System.Collections.Generic;
using System.Globalization;
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
    public static class StarterSpell3DNativeAuditBatch
    {
        private const string Prefix="StarterSpell3D.NativeAudit.";
        private const int AuditWorldSeed=729490642;
        private const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static StarterSpell3DNativeAuditBatch()
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
        [MenuItem("Caves Of Ooo/Scenarios/Magic/Starter Spell 3D Acceptance")]
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
            SessionState.SetBool(Prefix+"emberMotion",StarterSpell3DEmberMotionCapture.IsRequested(Environment.GetCommandLineArgs()));
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
                string token=NativeSaveIsolation.Begin(Prefix,"StarterSpell3D-native-marker-"+Guid.NewGuid().ToString("N"),false);
                SessionState.SetString(Prefix+"token",token);SessionState.SetString(Prefix+"root",SaveGameService.SaveRootOverride);
                SessionState.SetBool(Prefix+"active",true);SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+900);
                Subscribe();EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");EditorApplication.isPlaying=true;
            }
            catch(Exception error)
            {
                Debug.LogError("[StarterSpell3DNativeAudit] Launch failed: "+error);
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
        {
            GameBootstrap.OnAfterBootstrap-=Apply;
            try
            {
                string runId=SessionState.GetString(Prefix+"runId","");
                WarmEditorDiscovery(runId);
                var driver=new GameObject("Starter Spell 3D Native Acceptance").AddComponent<StarterSpell3DNativeAudit>();
                driver.EnableEmberMotionCapture=SessionState.GetBool(Prefix+"emberMotion",false);
                driver.Initialize(runId);
            }
            catch(Exception error)
            {Debug.LogError("[StarterSpell3DNativeAudit] Setup failed: "+error);Finish(4);}
        }
        private static void WarmEditorDiscovery(string runId)
        {
            // Optional Editor tooling only. Warm its current Play-domain caches before
            // the driver starts; never alter runtime clocks or discard profile stalls.
            var receipt=new DiscoveryWarmup{runId=runId,privateRoot=SaveGameService.SaveRootOverride,
                utc=DateTime.UtcNow.ToString("O"),inPlayDomain=EditorApplication.isPlaying,
                scope="Editor discovery cache preparation before driver.Initialize; heap deltas are net used memory, not total allocations."};
            var rows=new List<DiscoveryStep>();
            var timer=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Type locator=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("MCPForUnity.Editor.Services.MCPServiceLocator",false)).FirstOrDefault(t=>t!=null);
                receipt.pluginPresent=locator!=null;
                if(locator==null){receipt.skipped=true;receipt.reason="optional-plugin-absent";}
                else
                {
                    string[] properties={"ResourceDiscovery","ToolDiscovery"};
                    string[] methods={"DiscoverAllResources","DiscoverAllTools"};
                    for(int i=0;i<properties.Length;i++)
                    {
                        var row=new DiscoveryStep{service=properties[i],method=methods[i]};rows.Add(row);
                        long before=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
                        var step=System.Diagnostics.Stopwatch.StartNew();
                        try
                        {
                            var property=locator.GetProperty(properties[i],BindingFlags.Public|BindingFlags.Static)
                                ??throw new MissingMemberException(locator.FullName,properties[i]);
                            object service=property.GetValue(null)??throw new InvalidOperationException(properties[i]+" returned null.");
                            var method=service.GetType().GetMethod(methods[i],BindingFlags.Public|BindingFlags.Instance,null,Type.EmptyTypes,null)
                                ??throw new MissingMethodException(service.GetType().FullName,methods[i]);
                            var result=method.Invoke(service,null) as System.Collections.ICollection
                                ??throw new InvalidOperationException(methods[i]+" did not return a counted collection.");
                            row.count=result.Count;row.pass=true;
                        }
                        finally{row.seconds=step.Elapsed.TotalSeconds;row.managedHeapUsedDeltaBytes=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-before;}
                    }
                }
                receipt.pass=true;
            }
            catch(Exception error){receipt.error=error.ToString();throw;}
            finally
            {
                receipt.seconds=timer.Elapsed.TotalSeconds;receipt.steps=rows.ToArray();
                string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/StarterSpell3D/Integration/NativeAudit"));Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir,"SSN-"+runId+"-editor-warmup.json"),JsonUtility.ToJson(receipt,true));
            }
        }
        private static void Poll()
        {
            if(!SessionState.GetBool(Prefix+"active",false)||SessionState.GetBool(Prefix+"finishing",false))return;
            var driver=UnityEngine.Object.FindFirstObjectByType<StarterSpell3DNativeAudit>();
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
            SaveGameService.RegisterRuntime(null,null);Debug.Log("[StarterSpell3DNativeAudit] Native capture exit="+code);
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
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/StarterSpell3D/Integration/NativeAudit"));Directory.CreateDirectory(dir);
            string native=Path.Combine(dir,"SSN-"+runId+"-native.json");
            try
            {
                string json=File.Exists(native)?File.ReadAllText(native):null;
                receipt.finalNativeVerified=ValidateFinalReportForRun(json,runId,root);
                // Do not label an older successful report as this attempt's run.
                if(!receipt.finalNativeVerified)code=4;
                receipt.emberMotionRequested=SessionState.GetBool(Prefix+"emberMotion",false);
                if(receipt.emberMotionRequested)
                {
                    receipt.emberMotionVerified=StarterSpell3DEmberMotionCapture.ValidateFinalFiles(Path.Combine(dir,"SSN-"+runId+"-ember-motion.json"),runId);
                    if(!receipt.emberMotionVerified)code=4;
                }
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
            receipt.exitCode=code;File.WriteAllText(Path.Combine(dir,"SSN-"+runId+"-cleanup.json"),JsonUtility.ToJson(receipt,true));
            SessionState.SetBool(Prefix+"active",false);SessionState.SetBool(Prefix+"finishing",false);Debug.Log("[StarterSpell3DNativeAudit] Scene/view/private-save cleanup exit="+code);
            if(SessionState.GetBool(Prefix+"exit",false))EditorApplication.Exit(code);
        }
        private static readonly string[] RequiredChecks={"native_N_real_new_game","native_keyboard_starter_cast","native_town_ready","native_south_entry","all_seven_command_outcomes","rime_denied_reduced_feedback","rain_empty_no_crop_mesh","paired_80second_profile_complete","private_boot_marker_unchanged","owned_save_only"};
        private static readonly string[] RequiredSpells={"Pyromancy_EmberSpit","Pyromancy_FlamingHands","Hydromancy_JetBlast","Galvanism_GroundSurge","Cryomancy_RimeGrip","Spellcraft_Calm","Hydromancy_ConjureRain"};
        private static bool ValidateReceiptMetadata(string json,string expectedRunId,string privateRoot)
        {
            if(string.IsNullOrWhiteSpace(json)||string.IsNullOrWhiteSpace(privateRoot)||!Guid.TryParseExact(expectedRunId,"N",out var id)||id==Guid.Empty)return false;
            try
            {
                var f=JsonUtility.FromJson<StarterSpell3DNativeAudit.Report>(json);
                if(f==null||f.runId!=expectedRunId||f.saveRoot!=privateRoot||f.failures!=0||f.unexpectedErrors!=0||!f.workloadComplete
                    ||f.worldSeed!=AuditWorldSeed||f.screenWidth!=1920||f.screenHeight!=1080||f.nativeKeyboardCasts<1
                    ||!f.shutdownObserved||!f.shutdownRootHeld||!f.shutdownSavingUnregistered||!f.displayPreferencesRestored||!f.inputSettingsRestored
                    ||f.checks==null||f.checks.Any(c=>c==null||!c.pass||string.IsNullOrEmpty(c.name))
                    ||f.checks.Select(c=>c.name).Distinct(StringComparer.Ordinal).Count()!=f.checks.Length||RequiredChecks.Any(name=>!f.checks.Any(c=>c.name==name))
                    ||f.spells==null||f.spells.Length!=7||f.spells.Distinct(StringComparer.Ordinal).Count()!=7||RequiredSpells.Except(f.spells).Any()
                    ||f.phases==null||f.phases.Length!=4||f.profileSeconds<80||f.profileSeconds>90||f.profileFrames<4||f.activeFrames<1
                    ||string.IsNullOrWhiteSpace(f.rawFrames)||f.rawSha256==null||f.rawSha256.Length!=64||f.rawSha256.Any(c=>!(c>='0'&&c<='9')&&!(c>='a'&&c<='f')))return false;
                for(int i=0;i<4;i++)
                {
                    var p=f.phases[i];if(p==null||p.zoneId!=(i<2?"Overworld.3.6.0":"Overworld.3.7.0")||p.mode!=(i%2==0?"Full":"Reduced")
                        ||p.seconds<20||p.seconds>23||p.frames<=1||p.casts<7||p.activeFrames<=0||p.maxMeshes<=0)return false;
                }
                return Math.Abs(f.phases.Sum(p=>p.seconds)-f.profileSeconds)<.01&&f.phases.Sum(p=>p.frames)==f.profileFrames&&f.phases.Sum(p=>p.activeFrames)==f.activeFrames;
            }
            catch(Exception){return false;}
        }
        private const string FrameHeader="phase,unity_frame,wall_seconds,unscaled_delta,tick,native_meshes,allocated_views,zone_renderer,input,main_thread,gc_bytes,draw_calls,triangles";
        private static readonly string[] RequiredCounters={"COO.ZoneRenderer.LateUpdate","COO.Input.Update","Main Thread","GC Allocated In Frame","Draw Calls Count","Triangles Count"};
        private static bool ValidateFinalReportForRun(string json,string expectedRunId,string privateRoot)
        {
            if(!ValidateReceiptMetadata(json,expectedRunId,privateRoot))return false;
            try
            {
                var f=JsonUtility.FromJson<StarterSpell3DNativeAudit.Report>(json);
                string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/StarterSpell3D/Integration/NativeAudit"));string stem="SSN-"+expectedRunId;
                if(f.screenshots==null||f.screenshots.Length<8||f.loopFrames==null||f.loopFrames.Length<56||f.loopFrames.Distinct(StringComparer.Ordinal).Count()!=f.loopFrames.Length)return false;
                foreach(var file in f.loopFrames)
                    if(Path.GetDirectoryName(Path.GetFullPath(file))!=directory||!Path.GetFileName(file).StartsWith(stem+"-",StringComparison.Ordinal)||!Is1080pPng(file))return false;
                if(f.screenshots.Any(p=>!f.loopFrames.Contains(p)))return false;
                string raw=Path.Combine(directory,stem+"-frames.csv");if(f.rawFrames!=raw||!File.Exists(raw)||new FileInfo(raw).Length<1000)return false;
                using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(raw))if(f.rawSha256!=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant())return false;
                if(f.counterNames==null||!f.counterNames.SequenceEqual(RequiredCounters)
                    ||f.counterAvailable==null||f.counterAvailable.Length!=6||f.counterAvailable.Take(4).Any(v=>!v)
                    ||f.counterUnits==null||f.counterUnits.Length!=6)return false;
                for(int i=0;i<6;i++)
                    if(f.counterAvailable[i]&&f.counterUnits[i]!=(i<3?"TimeNanoseconds":i==3?"Bytes":"Count"))return false;
                if(f.casts==null||f.casts.Length<40||f.casts.Any(c=>c==null||!c.pass||!c.resultStable||c.cooldown<=0))return false;
                if(!f.checks.Any(c=>c.name=="native_direction_matrix"&&c.pass)||!StarterSpell3DNativeAudit.HasDirectionalEvidence(f.casts))return false;
                // Every captured file must belong to exactly one cast in this run and
                // retain the bytes recorded after ScreenCapture finished writing.
                var captures=new Dictionary<string,StarterSpell3DNativeAudit.CaptureFrame>(StringComparer.Ordinal);
                foreach(var cast in f.casts)
                {
                    if(cast.frames==null)return false;
                    double previous=-1;
                    foreach(var frame in cast.frames)
                    {
                        if(frame==null||string.IsNullOrEmpty(frame.path)||captures.ContainsKey(frame.path)
                            ||!Finite(frame.wallSeconds)||frame.wallSeconds<previous||frame.meshes<0
                            ||!HashMatches(frame.path,frame.sha256))return false;
                        previous=frame.wallSeconds;captures.Add(frame.path,frame);
                    }
                }
                if(captures.Count!=f.loopFrames.Length||f.loopFrames.Any(p=>!captures.ContainsKey(p)))return false;
                foreach(string spell in RequiredSpells)
                    if(!f.casts.Any(c=>c.spell==spell&&c.mode=="showcase"&&c.peakMeshes>0&&c.frames!=null&&c.frames.Count>=4))return false;
                var cold=f.firstCast;
                if(cold==null||cold.samples==null||cold.samples.Length<2||cold.wallSeconds<=0||cold.maxFrameSeconds<=0||cold.maxMainNanoseconds<=0||cold.maxInputNanoseconds<=0||cold.poolAfter<=0)return false;
                // Validate the raw measurement coverage, not only its summarized labels.
                var lines=File.ReadAllLines(raw);if(lines.Length!=f.profileFrames+1||lines[0]!=FrameHeader)return false;
                int[] counts=new int[4],active=new int[4],maxMeshes=new int[4];
                int previousFrame=-1,previousPhase=0;double previousSeconds=-1;
                foreach(var line in lines.Skip(1))
                {
                    var cells=line.Split(',');if(cells.Length!=13||!int.TryParse(cells[0],out int phase)||phase<previousPhase||phase>=4)return false;
                    if(!int.TryParse(cells[1],out int unityFrame)||unityFrame<=previousFrame
                        ||!double.TryParse(cells[2],NumberStyles.Float,CultureInfo.InvariantCulture,out double seconds)||!Finite(seconds)||seconds<=previousSeconds
                        ||!double.TryParse(cells[3],NumberStyles.Float,CultureInfo.InvariantCulture,out double delta)||!Finite(delta)||delta<=0
                        ||!int.TryParse(cells[4],out int tick)||tick<0
                        ||!int.TryParse(cells[5],out int meshes)||meshes<0
                        ||!int.TryParse(cells[6],out int pool)||pool<meshes)return false;
                    for(int i=0;i<6;i++)
                        if(!long.TryParse(cells[7+i],out long value)||(f.counterAvailable[i]?value<0:value!=-1))return false;
                    counts[phase]++;if(meshes>0)active[phase]++;maxMeshes[phase]=Math.Max(maxMeshes[phase],meshes);
                    previousFrame=unityFrame;previousSeconds=seconds;previousPhase=phase;
                }
                int first=0;
                for(int i=0;i<4;i++)
                {
                    if(counts[i]!=f.phases[i].frames||active[i]!=f.phases[i].activeFrames||maxMeshes[i]!=f.phases[i].maxMeshes||f.phases[i].firstFrame!=first)return false;
                    first+=counts[i];
                }
                return true;
            }
            catch(Exception){return false;}
        }
        private static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        private static bool HashMatches(string file,string expected)
        {
            if(expected==null||expected.Length!=64||!File.Exists(file))return false;
            using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(file))
                return expected==BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
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
                var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{Enum.Parse(enumType,"FixedResolution"),1920,1080,"Starter spell audit 1920x1080"},null);
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
        [Serializable] private sealed class DiscoveryWarmup{public string runId,privateRoot,utc,scope,reason,error;public bool inPlayDomain,pluginPresent,skipped,pass;public double seconds;public DiscoveryStep[] steps;}
        [Serializable] private sealed class DiscoveryStep{public string service,method;public int count;public bool pass;public double seconds;public long managedHeapUsedDeltaBytes;}
        [Serializable] private sealed class SceneRow{public string path;public bool isLoaded,isActive;}
        [Serializable] private sealed class Cleanup{public ErrorRow[] errors;public bool errorLedgerTruncated;public string runId,privateRoot,sceneError,viewError,reportError;public bool privateRootRemoved,scenesRestored,gameViewRestored,finalNativeVerified,seedSettingRestored,inheritedSaveRootRestored,lastGamePreferenceRestored,emberMotionRequested,emberMotionVerified;public int nativeCleanupCode,exitCode,oldRequestedSeed,restoredRequestedSeed,totalUnexpectedErrors;}
    }
}
