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
namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite native evidence from an actual generated village door.
    /// One ordinary actor, one labelled start, and no injury/reward grants.</summary>
    public sealed class DensityDoorNativePlayer : MonoBehaviour
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
        private Entity _door;private Zone _sourceZone;private Cell _approach,_inside;private string _doorID,_checkpointHash;
        private readonly List<DoorObservation> _doors=new List<DoorObservation>();
        [Serializable]private sealed class DoorObservation{public string label,id,zone,model;public int x,y,quarter,tick,energy;public bool open,present,drawn;}
        private bool _startShortcut;private int _walks;
        private int _ordinaryZonesInspected;
        private OverworldZoneManager Manager=>(OverworldZoneManager)_input.ZoneManager;
        private Cell Cell()=>_input.CurrentZone.GetEntityCell(_input.PlayerEntity);
        private string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/Everyday/Doors/Native",RunId));
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
        private static readonly string[] RequiredChecks={"ordinary_start","actual_generated_open_door","labelled_single_start","native_menu_close_paid","native_closed_model","native_menu_open_paid","native_open_model","closed_before_bump","native_bump_opens_without_moving","native_step_enters_after_open","native_passes_inside","native_close_from_inside","checkpoint_saved","real_checkpoint_mutation","checkpoint_restores_door_and_actor","ordinary_finish"};
        public void Initialize(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Completion audit requires its isolated native launcher.");
            _context = context; _ownedRoot = SaveGameService.SaveRootOverride;
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
            yield return new WaitForSecondsRealtime(.8f);_input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"native input");
            var boot=(BootMenuController)Field(_input,"_bootMenuController");Require(boot!=null&&boot.IsActive,"isolated native menu");
            yield return Tap(Key.N);Require(State()=="Normal"&&Manager.WorldSeed==64,"ordinary seed64 new game");
            var actor=_input.PlayerEntity;string starterGear=Gear(actor);int coins=TradeSystem.GetDrams(actor);
            Check("ordinary_start",!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&!actor.HasTag("Invulnerable")&&actor.GetStatValue("Hitpoints")==40&&actor.GetStat("Hitpoints").Max==40&&actor.GetStatValue("Strength")==18&&actor.GetStatValue("Agility")==18&&actor.GetStatValue("Toughness")==18&&coins==50);
            FindSource();Require(_door!=null,"actual eligible door in bounded ordinary-village source search");_doorID=_door.ID;
            Check("actual_generated_open_door",_door.BlueprintName=="VillageDoor"&&string.IsNullOrEmpty(_door.GetPart<DoorPart>().OwnerId)&&_door.GetPart<DoorPart>().IsOpen&&!_door.GetPart<DoorPart>().IsClosed);
            var old=_input.CurrentZone;Require(old.TryTransferEntityTo(actor,_sourceZone,_approach.X,_approach.Y),"labelled actual door approach shortcut");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=_sourceZone,NewPlayerX=_approach.X,NewPlayerY=_approach.Y}});
            _startShortcut=true;_input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("DensityDoorAuditStart");
            Check("labelled_single_start",Cell()==_approach&&Gear(actor)==starterGear&&TradeSystem.GetDrams(actor)==coins);yield return Capture("01-real-generated-open-door");ObserveDoor("initial");
            int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);
            yield return DoorAction(DoorPart.CloseCommand);
            Check("native_menu_close_paid",_door.GetPart<DoorPart>().IsClosed&&Cell()==_approach&&OneAction(tick,energy));
            yield return Capture("02-closed-by-native-menu");Check("native_closed_model",ObserveDoor("closed-menu").StartsWith("stillleaf-door-",StringComparison.Ordinal));
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);yield return DoorAction(DoorPart.OpenCommand);
            Check("native_menu_open_paid",!_door.GetPart<DoorPart>().IsClosed&&Cell()==_approach&&OneAction(tick,energy));
            yield return Capture("03-opened-by-native-menu");Check("native_open_model",ObserveDoor("opened-menu").StartsWith("stillleaf-open-door-",StringComparison.Ordinal));
            yield return DoorAction(DoorPart.CloseCommand);Check("closed_before_bump",_door.GetPart<DoorPart>().IsClosed);
            var at=_sourceZone.GetEntityCell(_door);int dx=at.X-Cell().X,dy=at.Y-Cell().Y;
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);yield return Tap(Direction(dx,dy));
            Check("native_bump_opens_without_moving",!_door.GetPart<DoorPart>().IsClosed&&Cell()==_approach&&OneAction(tick,energy)&&actor.GetStatValue("Hitpoints")==40);
            yield return Capture("04-bump-open-is-stationary");ObserveDoor("bump-open");
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);yield return Tap(Direction(dx,dy));_walks++;
            Check("native_step_enters_after_open",Cell()==at&&OneAction(tick,energy));
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);yield return Tap(Direction(dx,dy));_walks++;
            Check("native_passes_inside",Cell()==_inside&&Cell().IsInterior&&OneAction(tick,energy));
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);yield return DoorAction(DoorPart.CloseCommand);
            Check("native_close_from_inside",_door.GetPart<DoorPart>().IsClosed&&Cell()==_inside&&OneAction(tick,energy));
            yield return Capture("05-inside-closed-door");ObserveDoor("inside-closed");
            yield return ProveCheckpoint(starterGear,coins);
            Check("ordinary_finish",_input.PlayerEntity.GetStatValue("Hitpoints")==40&&!CombatSystem.IsDeathHandled(_input.PlayerEntity)&&!DevMode.Enabled&&TradeSystem.GetDrams(_input.PlayerEntity)==coins&&Gear(_input.PlayerEntity)==starterGear);
            yield return Capture("06-restored-closed-door");ObserveDoor("loaded-closed");
        }
        private bool OneAction(int tick,int energy)=>_input.TurnManager.TickCount>tick&&_input.TurnManager.GetEnergy(_input.PlayerEntity)==energy-TurnManager.ActionThreshold+(_input.TurnManager.TickCount-tick)*_input.PlayerEntity.GetStatValue("Speed",TurnManager.DefaultSpeed);
        private string ObserveDoor(string label)
        {
            var part=_door.GetPart<DoorPart>();var cell=_input.CurrentZone.GetEntityCell(_door);var presenter=_input.ZoneRenderer?.SpawnRing3D;
            GameObject view=null;string model=null;bool represented=presenter!=null&&presenter.TryGetEntityView(_door,out view,out model);
            bool drawn=represented&&view!=null&&view.GetComponentsInChildren<Renderer>(true).Any(x=>x.enabled&&x.gameObject.activeInHierarchy&&!x.forceRenderingOff);
            _doors.Add(new DoorObservation{label=label,id=_door.ID,zone=_input.CurrentZone.ZoneID,model=model,x=cell?.X??-1,y=cell?.Y??-1,quarter=part.QuarterTurns,open=part.IsOpen,present=cell!=null,drawn=drawn,tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(_input.PlayerEntity)});
            Require(cell!=null&&drawn&&!string.IsNullOrEmpty(model),"actual current native door model: "+label);return model;
        }
        private void FindSource()
        {
            int tried=0;
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {
                if(Manager.WorldMap.GetPOI(x,y)?.Type!=POIType.Village)continue;
                string id=WorldMap.ToZoneID(x,y,0);var pipeline=(ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",Private).Invoke(Manager,new object[]{id});
                if(!pipeline.Builders.Any(b=>b is VillageBuilder)||!pipeline.Builders.Any(b=>b is VillageDoorPlacementBuilder)){_candidates.Add(id+":authored/nonordinary pipeline excluded");continue;}
                if(tried++>=32)return;_ordinaryZonesInspected++;_candidates.Add(id+":ordinary recorded-aperture pipeline inspected");var zone=Manager.GetZone(id);if(zone==null){_candidates.Add(id+":generation refused");continue;}
                var doors=zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="VillageDoor").OrderBy(e=>zone.GetEntityCell(e).Y).ThenBy(e=>zone.GetEntityCell(e).X).ToArray();
                if(doors.Length==0)_candidates.Add(id+":no actual generated VillageDoor");
                foreach(var door in doors)
                {
                    var part=door.GetPart<DoorPart>();var cell=zone.GetEntityCell(door);string prefix=id+":"+door.ID+"@"+cell.X+","+cell.Y+":";
                    if(part==null||part.IsClosed||!string.IsNullOrEmpty(part.OwnerId)||cell.Occupants.Any(e=>e.HasTag("Creature"))){_candidates.Add(prefix+"occupied/owned/closed/invalid");continue;}
                    if(zone.GetReadOnlyEntities().Any(e=>e.HasPart<BrainPart>()&&FactionManager.IsHostile(e,_input.PlayerEntity)&&SpatialQuery.DistanceToCell(zone,e,cell.X,cell.Y)<32)){_candidates.Add(prefix+"hostile within32 native cells");continue;}
                    int dx=part.QuarterTurns==0?0:1,dy=part.QuarterTurns==0?1:0;
                    foreach(int sign in new[]{1,-1})
                    {
                        var a=zone.GetCell(cell.X+dx*sign,cell.Y+dy*sign);var inside=zone.GetCell(cell.X-dx*sign,cell.Y-dy*sign);
                        if(a==null||inside==null||a.IsInterior||!inside.IsInterior||!Clear(zone,a)||!Clear(zone,inside)||!Clear(zone,cell))continue;
                        _door=door;_sourceZone=zone;_approach=a;_inside=inside;_candidates.Add(prefix+"selected actual generated aperture axis="+part.QuarterTurns+" outside="+a.X+","+a.Y+" inside="+inside.X+","+inside.Y);return;
                    }
                    _candidates.Add(prefix+"no clear real outside/inside approach");
                }
                if(zone!=_input.CurrentZone)Manager.UnloadZone(id);
            }
        }
        private bool Clear(Zone zone,Cell cell)=>!cell.BlocksMovement(_input.PlayerEntity)&&!cell.Occupants.Any(e=>e.HasTag("Creature")||e.GetPart<PhysicsPart>()?.Takeable==true||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>())&&zone.TileState.Heat(cell.X,cell.Y)<=0;
        private IEnumerator DoorAction(string command)
        {
            Require(State()=="Normal","door action begins in normal input");var before=Cell();var cell=_input.CurrentZone.GetEntityCell(_door);Require(cell!=null&&SpatialQuery.Distance(_input.CurrentZone,_input.PlayerEntity,_door)==1,"actual adjacent door");
            yield return Tap(Key.C);Require(State()=="AwaitingTalkDirection","native C asks direction");yield return Tap(Direction(cell.X-before.X,cell.Y-before.Y));
            Require(State()=="WorldActionMenuOpen","native door interaction menu");
            if(!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,_door))
            {
                var actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");
                if(actions.Any(a=>a.Command==WorldInteractionSystem.PickCellCommand))yield return MenuAction(WorldInteractionSystem.PickCellCommand);
                yield return MenuAction(WorldInteractionSystem.PickTargetCommandPrefix+_door.ID);
            }
            Require(State()=="WorldActionMenuOpen"&&ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,_door),"native exact door target");yield return MenuAction(command);yield return CloseToNormal();
        }
        private IEnumerator MenuAction(string command)
        {
            var actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");int index=actions.FindIndex(a=>a.Command==command);
            Require(index>=0,"actual native menu command "+command+"; available="+string.Join(",",actions.Select(a=>a.Command)));
            yield return Tap((Key)Enum.Parse(typeof(Key),MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString()));
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
        private IEnumerator ProveCheckpoint(string gear,int coins)
        {
            var actor=_input.PlayerEntity;var doorBefore=_door;var at=Cell();int x=at.X,y=at.Y,tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor),quarter=_door.GetPart<DoorPart>().QuarterTurns;string zoneID=_input.CurrentZone.ZoneID;
            Require(_door.GetPart<DoorPart>().IsClosed,"save a genuinely closed door");
            var savedDoorCell=_input.CurrentZone.GetEntityCell(_door);int doorX=savedDoorCell.X,doorY=savedDoorCell.Y;
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"native checkpoint metadata");string path=Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz");string old=HashFile(path);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);_checkpointHash=HashFile(path);
            Check("checkpoint_saved",MessageLog.GetLast()=="Game saved."&&MessageLog.NextSerialValue>serial&&_checkpointHash!=old&&SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID==zoneID&&SaveGameService.GetSaveInfo("Quick")?.GameID==info.GameID);
            yield return DoorAction(DoorPart.OpenCommand);
            Check("real_checkpoint_mutation",!_door.GetPart<DoorPart>().IsClosed&&Cell().X==x&&Cell().Y==y&&OneAction(tick,energy)&&HashFile(path)==_checkpointHash);
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(actor,_input.PlayerEntity)){Require(Time.realtimeSinceStartupAsDouble-began<8,"F6 replaces player graph");yield return null;}
            _door=_input.CurrentZone.GetReadOnlyEntities().Single(e=>e.ID==_doorID);
            Check("checkpoint_restores_door_and_actor",!ReferenceEquals(doorBefore,_door)&&_input.CurrentZone.ZoneID==zoneID&&_input.CurrentZone.GetEntityCell(_door)==_input.CurrentZone.GetCell(doorX,doorY)&&_input.CurrentZone.GetCell(doorX,doorY).Objects.Contains(_door)&&_door.GetPart<DoorPart>().IsClosed&&_door.GetPart<DoorPart>().QuarterTurns==quarter&&string.IsNullOrEmpty(_door.GetPart<DoorPart>().OwnerId)&&Cell().X==x&&Cell().Y==y&&_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(_input.PlayerEntity)==energy&&_input.PlayerEntity.GetStatValue("Hitpoints")==40&&Gear(_input.PlayerEntity)==gear&&TradeSystem.GetDrams(_input.PlayerEntity)==coins&&HashFile(path)==_checkpointHash);
        }
        private IEnumerator CloseToNormal()
        {
            for (int i = 0; State() != "Normal" && i < 4; i++) yield return Tap(Key.Escape);
            Require(State() == "Normal", "native menus close");
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
            Diag.Record("scenario", "DensityDoorNativeCase", payload: new { runId = RunId, name, passed });
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception caught) { error = caught; }
                if (error != null) { _failureState = Snapshot("failure"); _fatal = error.ToString(); Check("native_precondition_failed", false); Debug.LogError("[DensityDoorNative] " + error); break; }
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
            Diag.Record("scenario", "DensityDoorNativeSummary", payload: new
            { runId = RunId, cases = _audit.Count, failures = Failures, complete = Complete, errorsFinalized = _errorsFinalized, screenshots = _screenshots.Count });
        }
        private bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_screenshots.Count>=6;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report{runId=RunId,complete=Complete,errorsFinalized=_errorsFinalized,cases=_audit.Count,failures=Failures,unexpectedErrors=_unexpectedErrors,seconds=_clock?.Elapsed.TotalSeconds??0,fatal=_fatal,zone=_input?.CurrentZone?.ZoneID,doorID=_doorID,checkpointHash=_checkpointHash,startShortcut=_startShortcut,walks=_walks,ordinaryZonesInspected=_ordinaryZonesInspected,audit=_audit.ToArray(),screenshots=_screenshots.ToArray(),candidates=_candidates.ToArray(),nativeKeys=_nativeKeys.ToArray(),failureState=_failureState,doors=_doors.ToArray(),
                canVerify="Actual ordinary VillageBuilder+late-placement source, saved real aperture axis and native owner; native menu close/open cost one stationary action; bump opens before a later movement; actual threshold traversal; exact open/closed borrowed model identity; real F5/native-open-mutation/F6 replacement graph with closed-state/orientation restoration.",
                cannotVerify="One labelled start-position shortcut and bounded source search. No door, key, money, injury, HP, equipment or permission grants; no manual scheduler bypass or NPC schedule injection. Core/native unit tests separately cover optional locks, occupied/foreign/refused actions and actor paths. This replay does not establish NPC autonomous door use, stationary FOV/light-cache correctness, natural discovery frequency or visual quality; captures need human review."},true));
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
            public string runId,fatal,zone,doorID,checkpointHash,canVerify,cannotVerify;
            public bool complete,errorsFinalized,startShortcut;
            public int cases,failures,unexpectedErrors,walks,ordinaryZonesInspected;public double seconds;
            public string[] audit,screenshots,candidates;public NativeKeyStep[] nativeKeys;public NativeState failureState;public DoorObservation[] doors;
        }
    }
}
