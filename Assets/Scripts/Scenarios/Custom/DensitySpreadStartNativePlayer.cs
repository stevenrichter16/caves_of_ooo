using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Two real default scene bootstraps with native N and Continue input.
    /// A labelled scene reload preserves one isolated save destination.</summary>
    public sealed class DensitySpreadStartNativePlayer : MonoBehaviour
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        public string RunId { get; }=Guid.NewGuid().ToString("N");
        public bool Finished { get; private set; }
        public int Failures=>_failures+_unexpectedErrors;
        public string ReportPath { get; private set; }
        private readonly List<string> _audit=new List<string>(),_screenshots=new List<string>(),_candidates=new List<string>();
        private InputHandler _input; private ScenarioContext _context;
        private Keyboard _keyboard,_oldKeyboard;private InputSettings _oldSettings,_settings;
        private bool _oldBackground,_oldScenario,_cleaned,_errorsFinalized,_summaryEmitted;
        private int _failures,_unexpectedErrors;private string _ownedRoot,_fatal;
        private System.Diagnostics.Stopwatch _clock;
        private string _checkpointHash; private int _firstSeed,_secondSeed;
        private NativeState _firstStart,_checkpoint,_freshSecond,_continued;
        private string _firstPlayerID,_savedGameID,_savedGear,_scenePath;
        private bool _sceneReloadRequested;
        private OverworldZoneManager Manager=>(OverworldZoneManager)_input.ZoneManager;
        private Cell Cell()=>_input.CurrentZone.GetEntityCell(_input.PlayerEntity);
        private string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/SpreadBiome/NativeStart",RunId));
        private readonly List<NativeKeyStep> _nativeKeys = new List<NativeKeyStep>();
        private NativeState _failureState;
        [Serializable] private sealed class NativeState
        {
            public string label, state, zone; public int hp, maxHp, x, y, tick, energy;
            public bool deathHandled, blockingFx; public string[] lastMessages;
        }
        [Serializable] private sealed class NativeKeyStep
        { public string keys; public int sequence; public NativeState before, after; }
        private NativeState Snapshot(string label)
        {
            var actor = _input?.PlayerEntity; var zone = _input?.CurrentZone;
            var cell = actor == null ? null : zone?.GetEntityCell(actor);
            return new NativeState { label = label, state = _input == null ? "uninitialized" : State(), zone = zone?.ZoneID,
                hp = actor?.GetStatValue("Hitpoints") ?? -1, maxHp = actor?.GetStat("Hitpoints")?.Max ?? -1,
                x = cell?.X ?? -1, y = cell?.Y ?? -1, tick = _input?.TurnManager?.TickCount ?? -1,
                energy = actor == null || _input?.TurnManager == null ? -1 : _input.TurnManager.GetEnergy(actor),
                deathHandled = actor != null && CombatSystem.IsDeathHandled(actor),
                blockingFx = _input?.ZoneRenderer?.WorldFx?.HasBlockingFx == true,
                lastMessages = MessageLog.GetRecentEntries(12).Select(e => e.Text).ToArray() };
        }
        private string RuntimeDetails()
        {
            var state = Snapshot("precondition");
            return "state=" + state.state + "; HP=" + state.hp + "/" + state.maxHp
                + "; dead=" + state.deathHandled + "; zone=" + state.zone + "; cell=" + state.x + "," + state.y
                + "; FX=" + state.blockingFx + "; messages=" + string.Join(" | ", state.lastMessages);
        }
        private IEnumerator WaitForNativeFx()
        {
            double began = Time.realtimeSinceStartupAsDouble;
            while (_input != null && (State() == "WaitingForFxResolution" || _input.ZoneRenderer?.WorldFx?.HasBlockingFx == true))
            {
                Require(_input.PlayerEntity.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(_input.PlayerEntity), "actor died while native FX resolved; " + RuntimeDetails());
                Require(Time.realtimeSinceStartupAsDouble - began < 8, "native FX did not settle; " + RuntimeDetails());
                yield return null;
            }
        }
        private static readonly string[] RequiredChecks={"ordinary_seed_policy","ordinary_default_spread_start","ordinary_kit_and_stats","initial_cell_connected","native_N_creates_checkpoint","native_single_step_paid","native_F5_updates_checkpoint","real_scene_reload_creates_distinct_fresh_graph","second_default_is_actual_spread","native_C_restores_exact_saved_graph","continue_keeps_saved_bytes","ordinary_finish"};
        public void Initialize(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Completion audit requires its isolated native launcher.");
            _context = context; _ownedRoot = SaveGameService.SaveRootOverride;
            DontDestroyOnLoad(gameObject);
            _clock = System.Diagnostics.Stopwatch.StartNew();
            _oldScenario = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            _oldKeyboard = Keyboard.current; _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(RunAudit()));
        }

        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            _input=FindFirstObjectByType<InputHandler>(); Require(_input!=null,"actual native input");
            var bootstrap=FindFirstObjectByType<GameBootstrap>();
            Require(bootstrap!=null&&string.IsNullOrWhiteSpace(bootstrap.FreshGameZoneID),"actual saved SampleScene uses ordinary default sentinel");
            _scenePath=SceneManager.GetActiveScene().path;
            Require(!string.IsNullOrEmpty(_scenePath),"actual saved startup scene");
            Require(((BootMenuController)Field(_input,"_bootMenuController")).IsActive,"actual first boot menu");
            _firstSeed=Manager.WorldSeed;
            Check("ordinary_seed_policy",NativeAuditBootstrapSettings.ResolveSeed()==0);
            yield return Tap(Key.N);
            Require(!((BootMenuController)Field(_input,"_bootMenuController")).IsActive&&State()=="Normal","native N commits new graph");
            var actor=_input.PlayerEntity; _firstPlayerID=actor.ID; _firstStart=Snapshot("first-native-N");
            Check("ordinary_default_spread_start",ActualSpread()&&ReferenceEquals(Manager.ActiveZone,_input.CurrentZone));
            Check("ordinary_kit_and_stats",Ordinary(actor)&&TradeSystem.GetDrams(actor)==50&&actor.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="Dagger")&&actor.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==2);
            var from=Cell();var reachable=ConnectivityBuilder.FloodFill(_input.CurrentZone,from.X,from.Y);
            Check("initial_cell_connected",!from.BlocksMovement(actor)&&!from.Occupants.Any(e=>e!=actor&&e.HasTag("Creature"))&&(Enumerable.Range(0,Zone.Width).Any(x=>reachable[x,0]||reachable[x,Zone.Height-1])||Enumerable.Range(0,Zone.Height).Any(y=>reachable[0,y]||reachable[Zone.Width-1,y])));
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"native N writes actual checkpoint metadata");
            _savedGameID=info.GameID;string path=Path.Combine(_ownedRoot,_savedGameID,"Quick.sav.gz");string beforeHash=HashFile(path);
            Check("native_N_creates_checkpoint",info.ActiveZoneID==_input.CurrentZone.ZoneID&&new FileInfo(path).Length>0);
            yield return Capture("01-default-native-new-game");
            var step=SafeStep();Require(step!=null,"one actual safe cardinal keyboard step without grants or relocation");
            int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);string beforeGear=Gear(actor);
            yield return Tap(Direction(step.X-from.X,step.Y-from.Y));
            Check("native_single_step_paid",ReferenceEquals(_input.PlayerEntity,actor)&&Cell()==step&&_input.TurnManager.TickCount>tick&&_input.TurnManager.GetEnergy(actor)==energy-TurnManager.ActionThreshold+(_input.TurnManager.TickCount-tick)*actor.GetStatValue("Speed",TurnManager.DefaultSpeed)&&Gear(actor)==beforeGear);
            _checkpoint=Snapshot("before-native-F5");_savedGear=Gear(actor);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);_checkpointHash=HashFile(path);
            Check("native_F5_updates_checkpoint",MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."&&_checkpointHash!=beforeHash&&SaveGameService.GetSaveInfo("Quick")?.GameID==_savedGameID&&SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID==_checkpoint.zone);
            yield return Capture("02-saved-real-movement");
            // A real saved-scene reload reruns normal bootstrap and its menu.
            // The persistent observer only retains evidence/input ownership.
            // This is labelled restart stimulus, not a claimed application quit.
            _sceneReloadRequested=true;WriteReport();var oldInput=_input;
            SceneManager.LoadScene(_scenePath,LoadSceneMode.Single);
            double began=Time.realtimeSinceStartupAsDouble;
            while(true)
            {
                Require(Time.realtimeSinceStartupAsDouble-began<45,"second actual scene bootstrap completes");
                var next=FindFirstObjectByType<InputHandler>();
                if(next!=null&&!ReferenceEquals(next,oldInput)&&next.PlayerEntity!=null&&((BootMenuController)Field(next,"_bootMenuController")).IsActive){_input=next;break;}
                yield return null;
            }
            yield return new WaitForSecondsRealtime(.2f);
            var fresh=_input.PlayerEntity;_secondSeed=Manager.WorldSeed;_freshSecond=Snapshot("second-fresh-before-C");
            Check("real_scene_reload_creates_distinct_fresh_graph",!ReferenceEquals(actor,fresh)&&fresh.ID!=_firstPlayerID&&NativeAuditBootstrapSettings.ResolveSeed()==0&&SaveGameService.SaveRootOverride==_ownedRoot);
            Check("second_default_is_actual_spread",ActualSpread()&&Ordinary(fresh)&&string.IsNullOrWhiteSpace(FindFirstObjectByType<GameBootstrap>().FreshGameZoneID));
            yield return Capture("03-real-second-boot-menu");
            yield return Tap(Key.C);began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(fresh,_input.PlayerEntity)){Require(Time.realtimeSinceStartupAsDouble-began<8,"native C replaces fresh player with saved graph");yield return null;}
            _continued=Snapshot("after-native-C");
            var loaded=_input.PlayerEntity;
            Check("native_C_restores_exact_saved_graph",!ReferenceEquals(actor,loaded)&&loaded.ID==_firstPlayerID&&_continued.zone==_checkpoint.zone&&_continued.x==_checkpoint.x&&_continued.y==_checkpoint.y&&_continued.tick==_checkpoint.tick&&_continued.energy==_checkpoint.energy&&_continued.hp==_checkpoint.hp&&_continued.maxHp==_checkpoint.maxHp&&Manager.WorldSeed==_firstSeed&&Gear(loaded)==_savedGear&&!((BootMenuController)Field(_input,"_bootMenuController")).IsActive);
            Check("continue_keeps_saved_bytes",HashFile(path)==_checkpointHash&&SaveGameService.GetSaveInfo("Quick")?.GameID==_savedGameID);
            Check("ordinary_finish",State()=="Normal"&&Ordinary(loaded)&&TradeSystem.GetDrams(loaded)==50&&ActualSpread()&&SaveGameService.SaveRootOverride==_ownedRoot);
            yield return Capture("04-native-continue-restored");
        }
        private bool ActualSpread()
        {
            string id=_input.CurrentZone.ZoneID;if(!WorldMap.IsOverworldZoneID(id))return false;
            var at=WorldMap.FromZoneID(id);return at.z==0&&Manager.WorldMap.InBounds(at.x,at.y)&&Manager.WorldMap.GetBiome(at.x,at.y)==BiomeType.Spread;
        }
        private static bool Ordinary(Entity actor)=>!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&!DebugInvincibility.IsEnabled(actor)&&!actor.HasTag("Invulnerable")&&!CombatSystem.IsDeathHandled(actor)&&actor.GetStatValue("Hitpoints")==40&&actor.GetStat("Hitpoints").Max==40&&actor.GetStatValue("Strength")==18&&actor.GetStatValue("Agility")==18&&actor.GetStatValue("Toughness")==18;
        private Cell SafeStep()
        {
            var zone=_input.CurrentZone;var actor=_input.PlayerEntity;var from=Cell();
            foreach(var offset in new[]{(0,-1),(1,0),(0,1),(-1,0)})
            {
                int x=from.X+offset.Item1,y=from.Y+offset.Item2;if(!zone.CanPlaceFootprint(actor,x,y))continue;
                var cells=zone.GetOccupiedCells(actor,x,y);
                if(cells.Any(c=>c==null||c.BlocksMovement(actor)||c.Occupants.Any(e=>e!=actor&&(e.HasTag("Creature")||e.GetPart<PhysicsPart>()?.Takeable==true||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()))))continue;
                if(cells.Any(c=>{var t=zone.TileState.Get(c.X,c.Y);return t!=null&&(t.Heat>0||t.Cold>0||t.Charge>0||t.Coatings.Count>0||!string.IsNullOrEmpty(t.Cloud));}))continue;
                if(zone.GetReadOnlyEntities().Any(e=>e.HasTag("Creature")&&!CombatSystem.IsDeathHandled(e)&&(FactionManager.IsHostile(e,actor)||e.GetPart<BrainPart>()?.IsPersonallyHostileTo(actor)==true)&&SpatialQuery.DistanceToCell(zone,e,x,y)<=3))continue;
                return zone.GetCell(x,y);
            }
            return null;
        }
        private static string Gear(Entity actor)
        {
            var inv=actor.GetPart<InventoryPart>();
            string items=string.Join("|",inv.Objects.Concat(inv.EquippedItems.Values).Distinct().OrderBy(e=>e.ID)
                .Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)
                    +":carried="+(e.GetPart<PhysicsPart>()?.InInventory?.ID??"")+":equipped="+(e.GetPart<PhysicsPart>()?.Equipped?.ID??"")));
            string slots=string.Join("|",inv.EquippedItems.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.ID));
            string body=string.Join("|",actor.GetPart<Body>().GetParts().Where(p=>p.Equipped!=null).OrderBy(p=>p.ID).Select(p=>p.ID+":"+p.Equipped.ID));
            return items+";slots="+slots+";body="+body;
        }
        private static string HashFile(string path)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        private static Key Direction(int x,int y)
        {
            if(x==0)return y<0?Key.W:Key.S;if(y==0)return x<0?Key.A:Key.D;
            return y<0?(x<0?Key.Numpad7:Key.Numpad9):(x<0?Key.Numpad1:Key.Numpad3);
        }
        private string State() => Field(_input, "_inputState").ToString();
        private static object Field(object owner, string name)
        {
            var member = owner.GetType().GetField(name, Private | BindingFlags.Public);
            if (member == null) throw new InvalidOperationException("Missing observed field " + owner.GetType().Name + "." + name);
            return member.GetValue(owner);
        }
        private IEnumerator Tap(params Key[] keys)
        {
            Require(_clock.Elapsed.TotalSeconds < 240, "finite native content-completion deadline");
            double began = Time.realtimeSinceStartupAsDouble;
            while (_input != null && Time.time - (float)Field(_input, "_lastMoveTime") < _input.MoveRepeatDelay)
            { Require(Time.realtimeSinceStartupAsDouble - began < 3, "input rate gate reopens"); yield return null; }
            yield return WaitForNativeFx();
            var action = new NativeKeyStep { sequence = _nativeKeys.Count, keys = string.Join("+", keys.Select(k => k.ToString())), before = Snapshot("before-key") };
            _nativeKeys.Add(action);
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            yield return new WaitForSecondsRealtime(.13f);
            action.after = Snapshot("after-key"); WriteReport();
            yield return WaitForNativeFx();
            action.after = Snapshot("after-native-fx"); WriteReport();
            if (_input?.PlayerEntity != null)
                Require(_input.PlayerEntity.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(_input.PlayerEntity), "actor died after native key; " + RuntimeDetails());
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.15f); yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, name + ".png");
            DensityNativeScreenshot.CaptureToFile(path);
            Require(File.Exists(path) && new FileInfo(path).Length > 0, "screenshot " + name);
            _screenshots.Add(path);
        }
        private static void Require(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException("Density completion precondition: " + reason); }
        private void Check(string name, bool passed)
        {
            if (!passed) _failures++;
            _audit.Add((passed ? "PASS " : "FAIL ") + name);
            Diag.Record("scenario", "DensitySpreadStartNativeCase", payload: new { runId = RunId, name, passed });
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception caught) { error = caught; }
                if (error != null) { _failureState = Snapshot("failure"); _fatal = error.ToString(); Check("native_precondition_failed", false); Debug.LogError("[DensitySpreadStartNative] " + error); break; }
                if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            Finish();
        }
        public void SetUnexpectedErrors(int errors)
        { _unexpectedErrors = errors; _errorsFinalized = true; WriteReport(); EmitSummary(); }
        public void Abort(string reason)
        { if (Finished) return; StopAllCoroutines(); _fatal = reason; Check("native_aborted", false); Finish(); }
        private void Finish() { Cleanup(); Finished = true; WriteReport(); }
        private void EmitSummary()
        {
            if (_summaryEmitted) return; _summaryEmitted = true;
            Diag.Record("scenario", "DensitySpreadStartNativeSummary", payload: new
            { runId = RunId, cases = _audit.Count, failures = Failures, complete = Complete, errorsFinalized = _errorsFinalized, screenshots = _screenshots.Count });
        }
        private bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_screenshots.Count>=4;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report{runId=RunId,complete=Complete,errorsFinalized=_errorsFinalized,cases=_audit.Count,failures=Failures,unexpectedErrors=_unexpectedErrors,seconds=_clock?.Elapsed.TotalSeconds??0,fatal=_fatal,zone=_input?.CurrentZone?.ZoneID,checkpointHash=_checkpointHash,firstSeed=_firstSeed,secondSeed=_secondSeed,firstPlayerID=_firstPlayerID,savedGameID=_savedGameID,scenePath=_scenePath,sceneReloadRequested=_sceneReloadRequested,audit=_audit.ToArray(),screenshots=_screenshots.ToArray(),nativeKeys=_nativeKeys.ToArray(),failureState=_failureState,firstStart=_firstStart,checkpoint=_checkpoint,freshSecond=_freshSecond,continued=_continued,
                canVerify="Two actual saved-scene bootstraps with zero requested audit seed; ordinary default Spread placement and kit; native N, one paid keyboard movement and F5 checkpoint; native C restores the original saved identity/location/seed/HP/gear/clock/energy through a replacement graph; unchanged saved bytes. No grants, transfers, chosen fixed seeds or direct graph loads.",
                cannotVerify="The audit requests one labelled SceneManager reload while Play remains active; this is not a full application quit/relaunch. The initial scenario is otherwise an ordinary new game. One initial safe cell/step is not combat immunity or biome-wide balance. The separate biome audit owns chunk/POI/lair rendering coverage. Captures require visual inspection."},true));
        }
        private void Cleanup()
        {
            if (_cleaned) return; _cleaned = true;
            if (_keyboard != null) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.RemoveDevice(_keyboard); }
            if (_oldKeyboard != null && _oldKeyboard.added) _oldKeyboard.MakeCurrent();
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;

        }
        private void OnDestroy()
        {
            if (!Finished && _clock != null) { _fatal = "Play stopped before completion."; Check("native_interrupted", false); Finish(); }
            Cleanup(); EmitSummary(); Diag.SetChannel("scenario", _oldScenario);
        }
        [Serializable]private sealed class Report
        {
            public string runId,fatal,zone,checkpointHash,firstPlayerID,savedGameID,scenePath,canVerify,cannotVerify;
            public bool complete,errorsFinalized,sceneReloadRequested;
            public int cases,failures,unexpectedErrors,firstSeed,secondSeed;public double seconds;
            public string[] audit,screenshots;public NativeKeyStep[] nativeKeys;
            public NativeState failureState,firstStart,checkpoint,freshSecond,continued;
        }
    }
}
