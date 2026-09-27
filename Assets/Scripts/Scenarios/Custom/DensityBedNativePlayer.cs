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
    /// <summary>Finite native evidence from an actual generated village bed.
    /// One ordinary actor, one labelled start, and no injury/reward grants.</summary>
    public sealed class DensityBedNativePlayer : MonoBehaviour
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
        private Entity _bed;private Zone _sourceZone;private Cell _approach;private string _ownerID,_bedID,_checkpointHash;
        private bool _startShortcut,_ownerFixture;private int _walks;
        private int _ordinaryZonesInspected;
        private OverworldZoneManager Manager=>(OverworldZoneManager)_input.ZoneManager;
        private Cell Cell()=>_input.CurrentZone.GetEntityCell(_input.PlayerEntity);
        private string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/Everyday/Beds/Native",RunId));
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
        private static readonly string[] RequiredChecks={"ordinary_start","actual_generated_unowned_bed","labelled_single_start","adjacent_sleep_refused","native_walk_onto_bed","underfoot_bed_selected","ordinary_bed_sixty_ticks","next_band_sleep_timing","bed_no_inn_price_or_rewards","owner_fixture_native_refusal","checkpoint_saved","real_checkpoint_mutation","checkpoint_restores_owner_and_actor","loaded_owner_native_refusal","ordinary_finish"};
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
            FindSource();Require(_bed!=null,"actual eligible bed in bounded ordinary-village source search");_bedID=_bed.ID;
            Check("actual_generated_unowned_bed",_bed.BlueprintName=="Bed"&&string.IsNullOrEmpty(_bed.GetPart<BedPart>().Owner)&&!_bed.GetPart<BedPart>().Occupied);
            var old=_input.CurrentZone;Require(old.TryTransferEntityTo(actor,_sourceZone,_approach.X,_approach.Y),"labelled actual bed approach shortcut");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=_sourceZone,NewPlayerX=_approach.X,NewPlayerY=_approach.Y}});
            _startShortcut=true;_input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("DensityBedAuditStart");
            Check("labelled_single_start",Cell()==_approach&&Gear(actor)==starterGear&&TradeSystem.GetDrams(actor)==coins);yield return Capture("01-real-generated-bed");
            int tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);long serial=MessageLog.NextSerialValue;
            yield return BedAction(PlayerBedService.RestCommand,false);
            Check("adjacent_sleep_refused",_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(actor)==energy&&MessageLog.GetRecentEntries(6).Any(e=>e.Serial>=serial&&e.Text.Contains("Step onto the bed"))&&!_bed.GetPart<BedPart>().Occupied);
            var at=_sourceZone.GetEntityCell(_bed);yield return Tap(Direction(at.X-Cell().X,at.Y-Cell().Y));_walks++;
            Check("native_walk_onto_bed",SpatialQuery.Distance(_sourceZone,actor,_bed)==0);yield return Capture("02-on-real-bed");
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);
            yield return BedAction(PlayerBedService.RestCommand,true);
            Check("ordinary_bed_sixty_ticks",_input.TurnManager.TickCount==tick+60&&_input.TurnManager.GetEnergy(actor)==energy&&!_bed.GetPart<BedPart>().Occupied&&actor.GetStatValue("Hitpoints")==40);
            yield return Capture("03-sixty-tick-rest");
            tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);int next=tick+WorldClock.BandLengthTicks-tick%WorldClock.BandLengthTicks;
            yield return BedAction(PlayerBedService.NextBandCommand,true);
            Check("next_band_sleep_timing",_input.TurnManager.TickCount==next&&_input.TurnManager.GetEnergy(actor)==energy&&!_bed.GetPart<BedPart>().Occupied);
            Check("bed_no_inn_price_or_rewards",!actor.HasEffect<WellRestedEffect>()&&TradeSystem.GetDrams(actor)==coins&&Gear(actor)==starterGear&&actor.GetStatValue("Hitpoints")==40);yield return Capture("04-next-band-rest");
            // Diagnostic access-control fixture on the actual source, not a claim
            // that ordinary generated beds currently receive owner assignments.
            Require(_sourceZone.GetReadOnlyEntities().Any(e=>e.ID==_ownerID&&e!=actor),"actual NPC remains the labelled authority identity");
            _bed.GetPart<BedPart>().Owner=_ownerID;_ownerFixture=true;tick=_input.TurnManager.TickCount;energy=_input.TurnManager.GetEnergy(actor);serial=MessageLog.NextSerialValue;
            yield return BedAction(PlayerBedService.RestCommand,true);
            Check("owner_fixture_native_refusal",OwnerRefused(tick,energy,serial));yield return Capture("05-labelled-owner-refusal");
            yield return ProveCheckpoint(starterGear,coins);
            Check("ordinary_finish",_input.PlayerEntity.GetStatValue("Hitpoints")==40&&!CombatSystem.IsDeathHandled(_input.PlayerEntity)&&!DevMode.Enabled&&!_input.PlayerEntity.HasEffect<WellRestedEffect>()&&TradeSystem.GetDrams(_input.PlayerEntity)==coins&&Gear(_input.PlayerEntity)==starterGear);
            yield return Capture("06-restored-owned-bed");
        }
        private void FindSource()
        {
            int tried=0;
            for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
            {
                if(Manager.WorldMap.GetPOI(x,y)?.Type!=POIType.Village)continue;
                string id=WorldMap.ToZoneID(x,y,0);
                var pipeline=(ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",Private).Invoke(Manager,new object[]{id});
                if(!pipeline.Builders.Any(b=>b is VillageBuilder)){_candidates.Add(id+":authored/nonordinary pipeline excluded");continue;}
                if(tried++>=32)return;_ordinaryZonesInspected++;_candidates.Add(id+":ordinary VillageBuilder source inspected");var zone=Manager.GetZone(id);if(zone==null){_candidates.Add(id+":generation refused");continue;}
                if(!zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="Bed"))_candidates.Add(id+":no actual generated Bed");
                var npc=zone.GetReadOnlyEntities().FirstOrDefault(e=>e.HasTag("Creature")&&!e.HasTag("Player")&&e.HasPart<BrainPart>()&&!string.IsNullOrEmpty(e.ID));
                foreach(var bed in zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Bed").OrderBy(e=>zone.GetEntityCell(e).Y).ThenBy(e=>zone.GetEntityCell(e).X))
                {
                    var part=bed.GetPart<BedPart>();var cell=zone.GetEntityCell(bed);
                    string prefix=id+":"+bed.ID+"@"+cell.X+","+cell.Y+":";
                    if(part==null||part.Occupied||!string.IsNullOrEmpty(part.Owner)||cell.Occupants.Any(e=>e.HasTag("Creature"))||npc==null){_candidates.Add(prefix+"occupied/owned/invalid source");continue;}
                    if(zone.GetReadOnlyEntities().Any(e=>e.HasPart<BrainPart>()&&FactionManager.IsHostile(e,_input.PlayerEntity)&&SpatialQuery.DistanceToCell(zone,e,cell.X,cell.Y)<=RestSystem.HostileScanRadius)){_candidates.Add(prefix+"actual nearby hostile");continue;}
                    foreach(var d in new[]{(1,0),(0,1),(-1,0),(0,-1)})
                    {
                        var a=zone.GetCell(cell.X+d.Item1,cell.Y+d.Item2);
                        if(a==null||!a.IsInterior||a.BlocksMovement(_input.PlayerEntity)||a.Occupants.Any(e=>e.HasTag("Creature")||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()))continue;
                        _bed=bed;_sourceZone=zone;_approach=a;_ownerID=npc.ID;_candidates.Add(prefix+"selected ordinary generated bed; owner fixture candidate="+npc.ID);return;
                    }
                    _candidates.Add(prefix+"no legal adjacent interior approach");
                }
                if(zone!=_input.CurrentZone)Manager.UnloadZone(id);
            }
        }
        private IEnumerator BedAction(string command,bool underfoot)
        {
            Require(State()=="Normal","bed action begins in normal input");var before=Cell();var bedCell=_input.CurrentZone.GetEntityCell(_bed);Require(bedCell!=null,"actual current bed");
            yield return Tap(Key.C);Require(State()=="AwaitingTalkDirection","native C asks direction");
            yield return Tap(underfoot?Key.Period:Direction(Math.Sign(bedCell.X-before.X),Math.Sign(bedCell.Y-before.Y)));
            Require(State()=="WorldActionMenuOpen","native bed interaction menu");
            bool selectedUnderfootRow = false;
            if(!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,_bed))
            {
                string pick = WorldInteractionSystem.PickTargetCommandPrefix+_bed.ID;
                var rows = (List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");
                selectedUnderfootRow = underfoot && rows.Any(a => a.Command == pick && a.Name == "Underfoot");
                yield return MenuAction(pick);
            }
            Require(State()=="WorldActionMenuOpen"&&ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,_bed),"native exact bed target");
            if(underfoot&&!_audit.Contains("PASS underfoot_bed_selected"))Check("underfoot_bed_selected",selectedUnderfootRow&&SpatialQuery.Distance(_input.CurrentZone,_input.PlayerEntity,_bed)==0);
            yield return MenuAction(command);yield return CloseToNormal();
        }
        private IEnumerator MenuAction(string command)
        {
            var actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");int index=actions.FindIndex(a=>a.Command==command);
            Require(index>=0,"actual native menu command "+command+"; available="+string.Join(",",actions.Select(a=>a.Command)));
            yield return Tap((Key)Enum.Parse(typeof(Key),MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString()));
        }
        private bool OwnerRefused(int tick,int energy,long serial)=>_bed.GetPart<BedPart>().Owner==_ownerID&&!_bed.GetPart<BedPart>().Occupied&&_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(_input.PlayerEntity)==energy&&MessageLog.GetRecentEntries(8).Any(e=>e.Serial>=serial&&e.Text.Contains("reserved for someone else"));
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
            var actor=_input.PlayerEntity;var bedBefore=_bed;var at=Cell();int x=at.X,y=at.Y,tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(actor);
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"native checkpoint metadata");string path=Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz");string old=HashFile(path);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);_checkpointHash=HashFile(path);
            Check("checkpoint_saved",MessageLog.GetLast()=="Game saved."&&MessageLog.NextSerialValue>serial&&_checkpointHash!=old&&SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID==_sourceZone.ZoneID&&SaveGameService.GetSaveInfo("Quick")?.GameID==info.GameID);
            Require(!_approach.BlocksMovement(actor)&&!_approach.Occupants.Any(e=>e.HasTag("Creature")),"real saved approach still free");
            yield return Tap(Direction(_approach.X-x,_approach.Y-y));_walks++;_bed.GetPart<BedPart>().Owner="";
            Check("real_checkpoint_mutation",Cell().X==_approach.X&&Cell().Y==_approach.Y&&_input.TurnManager.TickCount>tick&&_input.TurnManager.GetEnergy(actor)==energy-TurnManager.ActionThreshold+(_input.TurnManager.TickCount-tick)*actor.GetStatValue("Speed",TurnManager.DefaultSpeed)&&_bed.GetPart<BedPart>().Owner==""&&HashFile(path)==_checkpointHash);
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(actor,_input.PlayerEntity)){Require(Time.realtimeSinceStartupAsDouble-began<8,"F6 replaces player graph");yield return null;}
            _bed=_input.CurrentZone.GetReadOnlyEntities().Single(e=>e.ID==_bedID);
            Check("checkpoint_restores_owner_and_actor",!ReferenceEquals(bedBefore,_bed)&&_input.CurrentZone.ZoneID==_sourceZone.ZoneID&&_bed.GetPart<BedPart>().Owner==_ownerID&&!_bed.GetPart<BedPart>().Occupied&&Cell().X==x&&Cell().Y==y&&_input.TurnManager.TickCount==tick&&_input.TurnManager.GetEnergy(_input.PlayerEntity)==energy&&_input.PlayerEntity.GetStatValue("Hitpoints")==40&&Gear(_input.PlayerEntity)==gear&&TradeSystem.GetDrams(_input.PlayerEntity)==coins&&HashFile(path)==_checkpointHash);
            serial=MessageLog.NextSerialValue;yield return BedAction(PlayerBedService.RestCommand,true);Check("loaded_owner_native_refusal",OwnerRefused(tick,energy,serial));
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
            Diag.Record("scenario", "DensityBedNativeCase", payload: new { runId = RunId, name, passed });
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception caught) { error = caught; }
                if (error != null) { _failureState = Snapshot("failure"); _fatal = error.ToString(); Check("native_precondition_failed", false); Debug.LogError("[DensityBedNative] " + error); break; }
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
            Diag.Record("scenario", "DensityBedNativeSummary", payload: new
            { runId = RunId, cases = _audit.Count, failures = Failures, complete = Complete, errorsFinalized = _errorsFinalized, screenshots = _screenshots.Count });
        }
        private bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_screenshots.Count>=6;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report{runId=RunId,complete=Complete,errorsFinalized=_errorsFinalized,cases=_audit.Count,failures=Failures,unexpectedErrors=_unexpectedErrors,seconds=_clock?.Elapsed.TotalSeconds??0,fatal=_fatal,zone=_input?.CurrentZone?.ZoneID,bedID=_bedID,ownerID=_ownerID,checkpointHash=_checkpointHash,startShortcut=_startShortcut,ownerFixture=_ownerFixture,walks=_walks,ordinaryZonesInspected=_ordinaryZonesInspected,audit=_audit.ToArray(),screenshots=_screenshots.ToArray(),candidates=_candidates.ToArray(),nativeKeys=_nativeKeys.ToArray(),failureState=_failureState,
                canVerify="Actual generated VillageBuilder bed; ordinary new-game actor; adjacent refusal, native underfoot choice,60-tick and next-band rest with unchanged energy/HP/coins/kit/no inn buff; labelled owner refusal fixture; real F5/movement+owner mutation/F6 exact graph and owner restoration.",
                cannotVerify="One explicit start-position shortcut. The actual bed starts unowned; only its Owner string is deliberately assigned an existing NPC ID for a labelled diagnostic access/save control, then changed after saving. No health, injury, item, money, AI or source grants, manual scheduler bypass or NPC schedule injection. Full-HP timing does not prove healing/cure; core tests cover those and dead/occupied/reentrant cases. Rest is instant world-clock advancement, not simulated sleeping NPC turns. No naturally owned-bed prevalence, discovery, combat-balance or art-quality claim. Captures require visual review."},true));
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
            public string runId,fatal,zone,bedID,ownerID,checkpointHash,canVerify,cannotVerify;
            public bool complete,errorsFinalized,startShortcut,ownerFixture;
            public int cases,failures,unexpectedErrors,walks,ordinaryZonesInspected;public double seconds;
            public string[] audit,screenshots,candidates;public NativeKeyStep[] nativeKeys;public NativeState failureState;
        }
    }
}
