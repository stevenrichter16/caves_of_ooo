using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Scenario-only queued-key observer. Reflection reads UI/registration state only.
    /// HaulBarrel retains its shipped yellow CP437 0; this audit claims no barrel sprite coverage.</summary>
    public sealed class GameAuditHaulingBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures => (_bench == null ? 0 : _bench.Failures) + _fatalFailures + _unexpectedErrors;
        private ScenarioContext _ctx;
        private GameAuditHaulingBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _oldDragChannel;
        private int _fatalFailures, _unexpectedErrors;
        private string _root, _markerID, _freshID, _barrelID;
        private System.Diagnostics.Stopwatch _clock;
        private Report _report;
        private bool _staleBaselineObserved, _removalBaselineObserved;
        private const int SampleCapacity = 200000;
        private const int StepCapacity = 4000;
        private const double PhaseSeconds = 25;
        private static readonly Key[] LoopKeys = { Key.UpArrow, Key.RightArrow, Key.DownArrow, Key.LeftArrow };
        private static readonly int[] LoopDX = { 0, 1, 0, -1 }, LoopDY = { -1, 0, 1, 0 };
        private readonly string[] _metricNames = { "COO.Input.Update", "COO.Turns.Tick", "COO.Turns.EndTurn", "COO.Turns.ProcessUntilPlayerTurn", "COO.ZoneRenderer.LateUpdate", "GC Allocated In Frame" };
        private ProfilerRecorder[] _recorders;
        private long[][] _samples;
        private double[] _sampleTimes;
        private int[] _samplePhases;
        private Step[] _steps;
        private int _frames, _stepCount, _phase = -1, _loopStep;
        private bool _measuring;
        private double _measurementStart, _phaseStart, _lastObservedStep;
        private readonly Phase[] _phases = { new Phase { name = "idle" }, new Phase { name = "discrete" }, new Phase { name = "held" } };
        private string Prefix => _bench.VerifyFixes ? "GA03e-after" : "GA03e-before";
        private string DirectoryPath => Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit");
        private double Now => _clock.Elapsed.TotalSeconds;
        public void Initialize(ScenarioContext ctx, GameAuditHaulingBench bench)
        {
            _ctx = ctx; _bench = bench; _clock = System.Diagnostics.Stopwatch.StartNew();
            _root = SaveGameService.SaveRootOverride; _markerID = PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            _barrelID = bench.Barrel.ID; _oldDragChannel = Diag.IsChannelEnabled("drag"); Diag.SetChannel("drag", true);
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings; _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>(); StartCoroutine(RunSafely(Audit()));
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; } catch (Exception ex) { error = ex; }
                if (error != null) { if (_bench.Failures == 0) _fatalFailures++; Debug.LogError(error); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Queue(); _measuring = false; DisposeRecorders(); Finish();
        }
        private IEnumerator Audit()
        {
            yield return new WaitForSecondsRealtime(.7f);
            var input = FindFirstObjectByType<InputHandler>(); if (input == null) throw new InvalidOperationException("Missing native InputHandler.");
            var boot = Read(input, "_bootMenuController") as BootMenuController;
            _bench.Check("real_boot_marker_menu", boot != null && boot.IsActive);
            yield return Tap(Key.N); yield return new WaitForSecondsRealtime(.2f);
            _freshID = SaveGameService.GetSaveInfo("Quick")?.GameID;
            _bench.Check("fresh_checkpoint_and_arena", !boot.IsActive && State(input) == "Normal" && _freshID != null && _freshID != _markerID
                && ReferenceEquals(input.PlayerEntity, _ctx.PlayerEntity) && input.CurrentZone.GetEntityPosition(input.PlayerEntity) == (20, 12)
                && input.CurrentZone.GetEntityPosition(_bench.Barrel) == (21, 12) && SaveGameService.HasQuickSave());
            _bench.Check("actual_barrel_glyph_handling_hp", _bench.Barrel.BlueprintName == "HaulBarrel"
                && _bench.Barrel.GetPart<RenderPart>().RenderString == "0" && _bench.Barrel.GetPart<RenderPart>().RenderLayer == 1
                && !EnvironmentSpriteRenderer.GlyphClaimAllowed('0', "HaulBarrel") && BarrelHP(_bench.Barrel) == 8
                && _bench.Barrel.GetPart<HandlingPart>().Weight == 75 && !_bench.Barrel.GetPart<HandlingPart>().Carryable
                && !_bench.Barrel.GetPart<PhysicsPart>().Takeable && input.PlayerEntity.GetStatValue("Strength") == 16
                && input.PlayerEntity.GetStatValue("Speed") == 100 && input.MoveRepeatDelay > 0 && input.MoveRepeatDelay < .3f);
            yield return HaulMenu(input, _bench.Barrel, false, "initial_grab");
            yield return HaulMenu(input, _bench.Barrel, true, "release_control");
            yield return HaulMenu(input, _bench.Barrel, false, "measurement_grab");
            yield return Measure(input, _bench.Barrel);
            _bench.Check("measurement_closed_loop_preserves_barrel", input.CurrentZone.GetEntityPosition(input.PlayerEntity) == (20, 12)
                && input.CurrentZone.GetEntityPosition(_bench.Barrel) == (21, 12) && Healthy(input.PlayerEntity, _bench.Barrel) && BarrelHP(_bench.Barrel) == 8);

            // Healthy checkpoint is the control and later restores the identical arena for ordinary removal.
            yield return SaveWithReceipt("healthy_checkpoint");
            string path = OwnedQuickPath(); byte[] healthyBytes = File.ReadAllBytes(path);
            var oldActor = input.PlayerEntity; int savedTick = input.TurnManager.TickCount, savedEnergy = input.TurnManager.GetEnergy(oldActor);
            yield return Tap(Key.F6);
            var barrel = FindBarrel(input); // Never use the old ScenarioContext entities/turns after load.
            _bench.Check("healthy_load_fresh_actor_exact_aliases", !ReferenceEquals(oldActor, input.PlayerEntity) && barrel != null
                && Healthy(input.PlayerEntity, barrel) && ReferenceEquals(input.TurnManager, TurnManager.Active)
                && ReferenceEquals(input.PlayerEntity, input.TurnManager.CurrentActor) && ReferenceEquals(DragSystem.GetDragged(input.PlayerEntity), barrel)
                && input.TurnManager.TickCount == savedTick && input.TurnManager.GetEnergy(input.PlayerEntity) == savedEnergy && State(input) == "Normal"
                && MessageLog.GetLast() == "Game loaded.");

            // Historical-save fixture only: bypass runtime removal cleanup by removing raw cell membership.
            // The retained link causes the serializer to include this absent load in its token graph.
            var cell = input.CurrentZone.GetEntityCell(barrel);
            bool removedRaw = cell != null && cell.Objects.Remove(barrel); input.CurrentZone.RebuildEntityCellsFromCells();
            _bench.Check("fixture_historical_unplaced_load_keeps_link", removedRaw && input.CurrentZone.GetEntityCell(barrel) == null && Healthy(input.PlayerEntity, barrel));
            yield return SaveWithReceipt("historical_stale_checkpoint");
            oldActor = input.PlayerEntity; int beforeLoadTick = input.TurnManager.TickCount, beforeLoadEnergy = input.TurnManager.GetEnergy(oldActor);
            // Post-fix metadata probe: advance the discarded clock without
            // moving the absent load, then restore the earlier checkpoint.
            if (_bench.VerifyFixes)
            {
                yield return Tap(Key.Period);
                _bench.Check("live_clock_advances_before_historical_load", input.TurnManager.TickCount > beforeLoadTick
                    && input.CurrentZone.GetEntityCell(barrel) == null && State(input) == "Normal");
            }
            yield return Tap(Key.F6);
            _bench.Check("historical_checkpoint_really_loaded", !ReferenceEquals(oldActor, input.PlayerEntity) && ReferenceEquals(input.TurnManager, TurnManager.Active)
                && input.TurnManager.TickCount == beforeLoadTick && input.TurnManager.GetEnergy(input.PlayerEntity) == beforeLoadEnergy
                && FindBarrel(input) == null && MessageLog.GetLast() == "Game loaded.");
            var stale = DragSystem.GetDragged(input.PlayerEntity);
            bool cleaned = !input.PlayerEntity.HasPart<DragPart>() && input.PlayerEntity.GetStatValue("Speed") == 100;
            if (_bench.VerifyFixes)
            {
                _bench.Check("after_stale_load_releases_and_refunds", cleaned);
                var repairEntries = MessageLog.GetRecentEntries(2);
                _bench.Check("repair_message_uses_saved_clock", repairEntries.Count == 2
                    && repairEntries[0].Text.Contains("slips from your grip") && repairEntries[0].Tick == beforeLoadTick
                    && repairEntries[1].Text == "Game loaded." && repairEntries[1].Tick == beforeLoadTick);
            }
            else { _staleBaselineObserved = stale != null && stale.ID == _barrelID && Healthy(input.PlayerEntity, stale); _bench.Check("baseline_observed_stale_load_link_and_penalty", _staleBaselineObserved); }
            var oldPosition = input.CurrentZone.GetEntityPosition(input.PlayerEntity);
            yield return Tap(Key.UpArrow);
            _bench.Check("historical_probe_uses_real_walk", input.CurrentZone.GetEntityPosition(input.PlayerEntity) == (oldPosition.Item1, oldPosition.Item2 - 1));
            if (_bench.VerifyFixes) _bench.Check("after_historical_load_cannot_resurrect", FindBarrel(input) == null && input.PlayerEntity.GetStatValue("Speed") == 100);
            else _bench.Check("baseline_observed_removed_load_resurrection", FindBarrel(input) == stale && input.CurrentZone.GetEntityPosition(stale) == oldPosition);

            // Restore an owned healthy file, then isolate the normal RemoveEntity pathway.
            File.WriteAllBytes(path, healthyBytes); oldActor = input.PlayerEntity; yield return Tap(Key.F6); barrel = FindBarrel(input);
            _bench.Check("normal_removal_control_starts_healthy", !ReferenceEquals(oldActor, input.PlayerEntity) && barrel != null && Healthy(input.PlayerEntity, barrel)
                && input.CurrentZone.GetEntityPosition(input.PlayerEntity) == (20, 12) && input.CurrentZone.GetEntityPosition(barrel) == (21, 12));
            int removalTick = input.TurnManager.TickCount, removalEnergy = input.TurnManager.GetEnergy(input.PlayerEntity);
            bool removed = input.CurrentZone.RemoveEntity(barrel); ZoneRenderHooks.MarkFullDirty("HaulingAudit.RemoveFixture");
            _bench.Check("normal_remove_succeeds_without_action_cost", removed && input.CurrentZone.GetEntityCell(barrel) == null
                && input.TurnManager.TickCount == removalTick && input.TurnManager.GetEnergy(input.PlayerEntity) == removalEnergy);
            if (_bench.VerifyFixes) _bench.Check("after_normal_remove_clears_both_parts_and_refunds", !input.PlayerEntity.HasPart<DragPart>() && !barrel.HasPart<DraggedPart>() && input.PlayerEntity.GetStatValue("Speed") == 100);
            else { _removalBaselineObserved = Healthy(input.PlayerEntity, barrel); _bench.Check("baseline_observed_normal_remove_stale_grip", _removalBaselineObserved); }
            oldPosition = input.CurrentZone.GetEntityPosition(input.PlayerEntity); yield return Tap(Key.UpArrow);
            _bench.Check("normal_removal_probe_uses_real_walk", input.CurrentZone.GetEntityPosition(input.PlayerEntity) == (oldPosition.Item1, oldPosition.Item2 - 1));
            if (_bench.VerifyFixes) _bench.Check("after_normal_removal_does_not_resurrect", FindBarrel(input) == null && input.PlayerEntity.GetStatValue("Speed") == 100);
            else { _bench.Check("baseline_observed_normal_remove_resurrection", FindBarrel(input) == barrel && input.CurrentZone.GetEntityPosition(barrel) == oldPosition); yield return HaulMenu(input, barrel, true, "baseline_release_after_observation"); }
            yield return SaveWithReceipt("final_save_after_lifecycle_probes");
            _bench.Check("all_slots_stay_disposable", SaveGameService.SaveRootOverride == _root && _freshID != _markerID && File.Exists(path)
                && File.Exists(Path.Combine(_root, _markerID, "Quick.sav.gz")) && Directory.GetFiles(_root, "Quick.sav.gz", SearchOption.AllDirectories).Length == 2
                && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _freshID))
                && !Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", _markerID)));
        }
        private IEnumerator HaulMenu(InputHandler input, Entity barrel, bool release, string label)
        {
            var actor = input.PlayerEntity; int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(actor);
            var a = input.CurrentZone.GetEntityPosition(actor); var b = input.CurrentZone.GetEntityPosition(barrel);
            Key direction = Direction(b.Item1 - a.Item1, b.Item2 - a.Item2);
            int before = DragCount(release ? "Released" : "Grabbed", actor, barrel);
            yield return Tap(Key.C); _bench.Check(label + "_awaits_direction", State(input) == "AwaitingTalkDirection"); yield return Tap(direction);
            var menu = input.WorldActionMenuUI; var rows = Read(menu, "_actions") as List<InventoryAction>; var shortcuts = Read(menu, "_shortcuts") as char[];
            string command = release ? HandlingPart.ReleaseCommand : HandlingPart.HaulCommand;
            int index = rows == null ? -1 : rows.FindIndex(row => row.Command == command);
            _bench.Check(label + "_exact_target_displayed_G_action", menu != null && menu.IsOpen && State(input) == "WorldActionMenuOpen" && ReferenceEquals(menu.SelectedTarget, barrel)
                && index >= 0 && shortcuts != null && shortcuts[index] == 'g' && rows[index].Key == 'g' && rows[index].Display == (release ? "let go" : "haul"));
            yield return Tap(Key.G);
            _bench.Check(label + "_keyboard_dispatch_and_no_time_cost", !menu.IsOpen && State(input) == "Normal" && input.CurrentZone.GetEntityPosition(actor) == a
                && input.TurnManager.TickCount == tick && input.TurnManager.GetEnergy(actor) == energy && DragCount(release ? "Released" : "Grabbed", actor, barrel) == before + 1);
            _bench.Check(label + "_reciprocal_state_and_speed", release ? !actor.HasPart<DragPart>() && !barrel.HasPart<DraggedPart>() && actor.GetStatValue("Speed") == 100 : Healthy(actor, barrel));
        }
        private IEnumerator SaveWithReceipt(string label)
        {
            string path = OwnedQuickPath(); byte[] before = File.Exists(path) ? File.ReadAllBytes(path) : null;
            int serial = MessageLog.NextSerialValue; yield return Tap(Key.F5);
            _bench.Check(label + "_new_native_success_message", MessageLog.GetLast() == "Game saved." && MessageLog.NextSerialValue > serial
                && SaveGameService.HasQuickSave() && SaveGameService.GetSaveInfo("Quick")?.GameID == _freshID);
            byte[] after = File.ReadAllBytes(path); _bench.Check(label + "_writes_new_owned_payload", before == null || !before.SequenceEqual(after));
        }
        private IEnumerator Measure(InputHandler input, Entity barrel)
        {
            _samples = new long[_metricNames.Length][]; _recorders = new ProfilerRecorder[_metricNames.Length];
            _sampleTimes = new double[SampleCapacity]; _samplePhases = new int[SampleCapacity]; _steps = new Step[StepCapacity];
            for (int i = 0; i < _metricNames.Length; i++)
            {
                _samples[i] = new long[SampleCapacity];
                _recorders[i] = ProfilerRecorder.StartNew(i == _metricNames.Length - 1 ? ProfilerCategory.Memory : ProfilerCategory.Scripts,
                    _metricNames[i], 2, ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
            }
            _measurementStart = Now; _loopStep = 0;
            var origin = input.CurrentZone.GetEntityPosition(input.PlayerEntity); int idleTick = input.TurnManager.TickCount, idleEnergy = input.TurnManager.GetEnergy(input.PlayerEntity);
            BeginPhase(input, 0); while (Now - _phaseStart < PhaseSeconds) yield return null; EndPhase(input);
            _bench.Check("idle_is_stationary_and_unspent", input.CurrentZone.GetEntityPosition(input.PlayerEntity) == origin && input.TurnManager.TickCount == idleTick
                && input.TurnManager.GetEnergy(input.PlayerEntity) == idleEnergy && Healthy(input.PlayerEntity, barrel));
            BeginPhase(input, 1);
            while (Now - _phaseStart < PhaseSeconds) yield return DiscreteStep(input, barrel);
            EndPhase(input);
            // Finish the same 8-step perimeter outside measured phases, so held input starts at the exact same corner.
            while (_loopStep % 8 != 0) yield return DiscreteStep(input, barrel);
            BeginPhase(input, 2);
            while (Now - _phaseStart < PhaseSeconds) yield return HeldLeg(input, barrel);
            EndPhase(input);
            while (_loopStep % 8 != 0) yield return HeldLeg(input, barrel);
            Queue(); yield return new WaitForSecondsRealtime(.2f);
            WritePerformance();
            _bench.Check("three_measured_phases_cover_75_seconds", _phases.All(p => p.seconds >= PhaseSeconds && p.seconds < PhaseSeconds + 3)
                && _frames > 300 && _frames < SampleCapacity && _phases.All(p => p.frames > 50)
                && _phases[1].steps >= 50 && _phases[2].steps >= 50 && _phases[1].steps == _phases[1].attempts
                && _phases[2].steps == _phases[2].attempts && _phases[2].heldRepeats > 20 && _phases.All(p => p.failures == 0));
        }
        private void BeginPhase(InputHandler input, int phase)
        { _phase = phase; _phaseStart = Now; _lastObservedStep = -1; _phases[phase].startSeconds = Now - _measurementStart; _phases[phase].startTick = input.TurnManager.TickCount; _measuring = true; }
        private void EndPhase(InputHandler input)
        { _measuring = false; _phases[_phase].seconds = Now - _phaseStart; _phases[_phase].endTick = input.TurnManager.TickCount; }
        private IEnumerator DiscreteStep(InputHandler input, Entity barrel)
        {
            int direction = (_loopStep % 8) / 2; var before = input.CurrentZone.GetEntityPosition(input.PlayerEntity); int tick = input.TurnManager.TickCount; double start = Now;
            if (_measuring) _phases[_phase].attempts++; Queue(LoopKeys[direction]);
            while (input.CurrentZone.GetEntityPosition(input.PlayerEntity) == before && Now - start < 2) yield return null;
            Queue(); ObserveStep(input, barrel, before.Item1, before.Item2, direction, tick, start, false); _loopStep++;
            yield return new WaitForSecondsRealtime(Mathf.Max(.18f, input.MoveRepeatDelay + .04f));
        }
        private IEnumerator HeldLeg(InputHandler input, Entity barrel)
        {
            int direction = (_loopStep % 8) / 2; var previous = input.CurrentZone.GetEntityPosition(input.PlayerEntity); int tick = input.TurnManager.TickCount;
            double started = Now, last = started; int observed = 0;
            Queue(LoopKeys[direction]);
            // The key stays physically held for BOTH observed steps. Release only after the second; never sleep a guessed distance.
            while (observed < 2 && Now - started < 3)
            {
                yield return null; var current = input.CurrentZone.GetEntityPosition(input.PlayerEntity);
                if (current == previous) continue;
                if (_measuring) _phases[_phase].attempts++;
                bool repeated = observed == 1 && _keyboard[LoopKeys[direction]].isPressed && !_keyboard[LoopKeys[direction]].wasPressedThisFrame;
                ObserveStep(input, barrel, previous.Item1, previous.Item2, direction, tick, last, repeated);
                observed++; _loopStep++; previous = current; tick = input.TurnManager.TickCount; last = Now;
            }
            Queue(); if (observed != 2) throw new InvalidOperationException("Held hauling leg failed to deliver exactly two observed steps.");
            yield return new WaitForSecondsRealtime(Mathf.Max(.16f, input.MoveRepeatDelay + .02f));
        }
        private void ObserveStep(InputHandler input, Entity barrel, int oldX, int oldY, int direction, int oldTick, double requested, bool repeated)
        {
            var current = input.CurrentZone.GetEntityPosition(input.PlayerEntity); var load = input.CurrentZone.GetEntityPosition(barrel);
            bool valid = current == (oldX + LoopDX[direction], oldY + LoopDY[direction]) && load == (oldX, oldY)
                && Healthy(input.PlayerEntity, barrel) && BarrelHP(barrel) == 8 && State(input) == "Normal" && input.TurnManager.TickCount > oldTick;
            if (_measuring)
            {
                var phase = _phases[_phase]; phase.steps++; if (!valid) phase.failures++; if (repeated) phase.heldRepeats++;
                if (_stepCount >= StepCapacity) throw new InvalidOperationException("Step sample capacity exhausted.");
                _steps[_stepCount++] = new Step { phase = _phase, seconds = Now - _measurementStart, latency = Now - requested,
                    interval = _lastObservedStep < 0 ? -1 : Now - _lastObservedStep, tickDelta = input.TurnManager.TickCount - oldTick,
                    energy = input.TurnManager.GetEnergy(input.PlayerEntity), x = current.Item1, y = current.Item2, loadX = load.Item1, loadY = load.Item2, repeated = repeated, valid = valid };
                _lastObservedStep = Now;
            }
            if (!valid) throw new InvalidOperationException("Native hauling step changed distance, load position, Speed, HP, mode, or failed to advance the scheduler.");
        }
        private void LateUpdate()
        {
            if (!_measuring || _recorders == null) return;
            if (_frames >= SampleCapacity) return;
            _sampleTimes[_frames] = Now - _measurementStart; _samplePhases[_frames] = _phase; _phases[_phase].frames++;
            for (int i = 0; i < _recorders.Length; i++) _samples[i][_frames] = _recorders[i].Valid ? _recorders[i].LastValue : -1;
            _frames++;
        }
        private void WritePerformance()
        {
            Directory.CreateDirectory(DirectoryPath); var metrics = new List<Metric>();
            for (int phase = 0; phase < 3; phase++) for (int i = 0; i < _metricNames.Length; i++)
            {
                int p = phase, m = i; var values = Enumerable.Range(0, _frames).Where(f => _samplePhases[f] == p).Select(f => _samples[m][f]).OrderBy(v => v).ToArray();
                metrics.Add(new Metric { phase = _phases[phase].name, name = _metricNames[i], unit = i == _metricNames.Length - 1 ? "bytes" : "nanoseconds",
                    valid = _recorders[i].Valid && values.Length > 0, samples = values.Length, max = values.Length == 0 ? -1 : values[values.Length - 1],
                    p99 = values.Length == 0 ? -1 : values[Math.Min(values.Length - 1, (int)(values.Length * .99))], mean = values.Length == 0 ? -1 : values.Average(v => (double)v) });
            }
            var csv = new StringBuilder("frame,seconds,phase," + string.Join(",", _metricNames) + "\n");
            for (int f = 0; f < _frames; f++) { csv.Append(f).Append(',').Append(F(_sampleTimes[f])).Append(',').Append(_phases[_samplePhases[f]].name); for (int i = 0; i < _metricNames.Length; i++) csv.Append(',').Append(_samples[i][f]); csv.Append('\n'); }
            File.WriteAllText(Path.Combine(DirectoryPath, Prefix + "-frames.csv"), csv.ToString());
            csv = new StringBuilder("step,seconds,phase,request_to_step_seconds,step_interval_seconds,tick_delta,energy,x,y,load_x,load_y,held_repeat,valid\n");
            for (int i = 0; i < _stepCount; i++) { var s = _steps[i]; csv.Append(i).Append(',').Append(F(s.seconds)).Append(',').Append(_phases[s.phase].name).Append(',').Append(F(s.latency)).Append(',').Append(F(s.interval)).Append(',').Append(s.tickDelta).Append(',').Append(s.energy).Append(',').Append(s.x).Append(',').Append(s.y).Append(',').Append(s.loadX).Append(',').Append(s.loadY).Append(',').Append(s.repeated).Append(',').Append(s.valid).Append('\n'); }
            File.WriteAllText(Path.Combine(DirectoryPath, Prefix + "-steps.csv"), csv.ToString());
            File.WriteAllText(Path.Combine(DirectoryPath, Prefix + "-perf.json"), JsonUtility.ToJson(new PerfReport { runId = _bench.RunId, mode = _bench.VerifyFixes ? "after" : "before", frames = _frames,
                measuredSeconds = _phases.Sum(p => p.seconds), wallSeconds = Now - _measurementStart, phases = _phases, metrics = metrics.ToArray(),
                bounds = "Scenario observer and queued input overhead included. Per-frame marker observations; no isolated drag timing or zero-GC claim. Alignment steps between/after phases are outside sampling. HaulBarrel uses shipped CP437 glyph, not barrel sprite." }, true));
            bool validMetrics = metrics.All(m => m.valid); DisposeRecorders(); _bench.Check("native_profiler_markers_valid", validMetrics);
        }
        private void DisposeRecorders()
        { if (_recorders == null) return; for (int i = 0; i < _recorders.Length; i++) _recorders[i].Dispose(); _recorders = null; }
        private static bool Healthy(Entity actor, Entity barrel)
        { var part = actor.GetPart<DragPart>(); return part != null && ReferenceEquals(part.Dragged, barrel) && ReferenceEquals(DragSystem.GetDragger(barrel), actor) && part.AppliedPenalty == 30 && part.SpeedPenalty == 30 && actor.GetStatValue("Speed") == 70; }
        private Entity FindBarrel(InputHandler input)
        { foreach (var entity in input.CurrentZone.GetReadOnlyEntities()) if (entity.ID == _barrelID) return entity; return null; }
        private string OwnedQuickPath()
        {
            if (string.IsNullOrEmpty(_root) || string.IsNullOrEmpty(_freshID) || _freshID.IndexOfAny(new[] { '/', '\\' }) >= 0 || SaveGameService.SaveRootOverride != _root)
                throw new InvalidOperationException("Owned native save destination missing or changed.");
            return Path.Combine(_root, _freshID, "Quick.sav.gz");
        }
        private static int BarrelHP(Entity barrel) => barrel.GetPart<DestructiblePart>().HP;
        private static int DragCount(string kind, Entity actor, Entity load) => DiagQuery.Count(new DiagQuery.Filter { Category = "drag", Kind = kind, Actor = actor.ID, Target = load.ID }).Count;
        private static string F(double value) => value.ToString("F6", CultureInfo.InvariantCulture);
        private static object Read(object owner, string field) => owner?.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private static string State(InputHandler input) => Read(input, "_inputState")?.ToString();
        private static Key Direction(int dx, int dy)
        {
            if (Math.Abs(dx) > 1 || Math.Abs(dy) > 1 || (dx == 0 && dy == 0)) throw new InvalidOperationException("Haul target must occupy an adjacent cell.");
            if (dx == 0) return dy < 0 ? Key.UpArrow : Key.DownArrow;
            if (dy == 0) return dx < 0 ? Key.LeftArrow : Key.RightArrow;
            return dx < 0 ? (dy < 0 ? Key.Numpad7 : Key.Numpad1) : (dy < 0 ? Key.Numpad9 : Key.Numpad3);
        }
        private void Queue(params Key[] keys) { if (_keyboard != null) InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); }
        private IEnumerator Tap(Key key) { Queue(key); yield return new WaitForSecondsRealtime(.06f); Queue(); yield return new WaitForSecondsRealtime(.2f); }
        public void SetUnexpectedErrors(int count) { _unexpectedErrors = count; if (_report != null) WriteReport(); }
        private void Finish()
        {
            _report = new Report { runId = _bench.RunId, mode = _bench.VerifyFixes ? "after" : "before", root = _root, freshID = _freshID, markerID = _markerID,
                seconds = Now, measuredSeconds = _phases.Sum(p => p.seconds), baselineStaleLinkObserved = _staleBaselineObserved, baselineRemovalObserved = _removalBaselineObserved,
                bounds = "Native queued keyboard: haul/release/movement/F5/F6. Raw membership removal and owned checkpoint restoration are fixture setup. Before mode expects known stale-link defects; PASS there is baseline reproduction, not repair. Historical and ordinary removal probes occur after performance capture. Shipped yellow CP437 barrel glyph only. Read final post-teardown JSON and raw logs; process code is selected before teardown." };
            WriteReport(); Debug.Log("[GameAuditHaulingBench] " + JsonUtility.ToJson(_report)); Finished = true;
        }
        private void WriteReport()
        {
            _report.cases = _bench.Cases; _report.failures = Failures; _report.unexpectedErrors = _unexpectedErrors; _report.audit = _bench.Audit.ToArray();
            Directory.CreateDirectory(DirectoryPath); File.WriteAllText(Path.Combine(DirectoryPath, Prefix + "-native.json"), JsonUtility.ToJson(_report, true));
        }
        private void OnDestroy()
        {
            _measuring = false; DisposeRecorders();
            try
            {
                if (_report != null)
                {
                    bool rootHeld = SaveGameService.SaveRootOverride == _root;
                    const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
                    bool unregistered = typeof(SaveGameService).GetField("_captureCurrent", flags).GetValue(null) == null && typeof(SaveGameService).GetField("_applyLoaded", flags).GetValue(null) == null;
                    _report.shutdownObserved = true; _report.shutdownSeconds = Now; _report.shutdownRootHeld = rootHeld; _report.shutdownSavingUnregistered = unregistered;
                    if (!rootHeld || !unregistered) _fatalFailures++; WriteReport();
                }
            }
            finally
            {
                if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
                if (_oldSettings != null) InputSystem.settings = _oldSettings;
                if (_settings != null) Destroy(_settings);
                Application.runInBackground = _oldBackground; Diag.SetChannel("drag", _oldDragChannel);
            }
        }
        [Serializable] private sealed class Report
        { public string runId, mode, root, freshID, markerID, bounds; public int cases, failures, unexpectedErrors; public double seconds, measuredSeconds, shutdownSeconds; public bool baselineStaleLinkObserved, baselineRemovalObserved, shutdownObserved, shutdownRootHeld, shutdownSavingUnregistered; public string[] audit; }
        [Serializable] private sealed class PerfReport
        { public string runId, mode, bounds; public int frames; public double measuredSeconds, wallSeconds; public Phase[] phases; public Metric[] metrics; }
        [Serializable] private sealed class Phase
        { public string name; public int frames, attempts, steps, heldRepeats, failures, startTick, endTick; public double startSeconds, seconds; }
        [Serializable] private sealed class Metric
        { public string phase, name, unit; public bool valid; public int samples; public long max, p99; public double mean; }
        private struct Step
        { public int phase, tickDelta, energy, x, y, loadX, loadY; public double seconds, latency, interval; public bool repeated, valid; }
    }
}
