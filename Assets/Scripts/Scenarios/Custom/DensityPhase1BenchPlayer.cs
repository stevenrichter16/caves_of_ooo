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
    /// <summary>Finite native-key traversal to a real Beating lair. Numeric
    /// stimuli run on detached factory entities; generated live actors are only
    /// paused after arrival for a still screenshot. No zone staging or teleport.</summary>
    public sealed class DensityPhase1BenchPlayer : MonoBehaviour
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public bool Finished { get; private set; }
        public int Failures => (_bench?.Failures ?? 0) + _nativeFailures + _unexpectedErrors;
        public string ReportPath { get; private set; }
        private readonly List<string> _audit = new List<string>();
        private readonly List<Entity> _paused = new List<Entity>();
        private DensityPhase1Bench _bench;
        private ScenarioContext _ctx;
        private InputHandler _input;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _oldScenario, _cleaned, _errorsFinalized, _summaryEmitted;
        private int _x, _y, _seed, _nativeFailures, _unexpectedErrors, _steps;
        private int _oldHpBase, _oldHpMax, _oldHpPenalty;
        private Stat _hp;
        private string _root, _fatal, _screenshot;
        private readonly string _fallbackRunId = Guid.NewGuid().ToString("N");
        private string RunId => _bench?.RunId ?? _fallbackRunId;
        private System.Diagnostics.Stopwatch _clock;
        private string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Docs/Verification/DensityPhase1/Native", RunId));

        public void Initialize(ScenarioContext ctx, int worldSeed, int x, int y)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Density audit requires the isolated native launcher.");
            _ctx = ctx; _seed = worldSeed; _x = x; _y = y;
            _root = SaveGameService.SaveRootOverride;
            _clock = System.Diagnostics.Stopwatch.StartNew();
            _oldScenario = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
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
            _input = FindFirstObjectByType<InputHandler>();
            Require(_input != null, "ordinary input bootstrap");
            var boot = (BootMenuController)typeof(InputHandler).GetField("_bootMenuController", Private).GetValue(_input);
            Require(boot != null && boot.IsActive, "new-game menu is present before native N");
            yield return Tap(Key.N);
            Require(!boot.IsActive && State() == "Normal", "native N finishes new-game choice");
            var manager = _input.ZoneManager as OverworldZoneManager;
            Require(manager != null && manager.WorldSeed == _seed, "ordinary bootstrap uses owned audit seed");
            _bench = new DensityPhase1Bench();
            _bench.Apply(new ScenarioContext(_input.CurrentZone, _ctx.Factory, _input.PlayerEntity, _input.TurnManager, _seed));
            Diag.Record("scenario", "DensityPhase1NativeRun", payload: new { runId = _bench.RunId, seed = _seed, x = _x, y = _y });
            Check("numeric_matrix_complete", _bench.Cases == 18 && _bench.Failures == 0);
            Check("owned_save_root", SaveGameService.SaveRootOverride == _root);
            Require(manager.WorldMap.GetBiome(_x, _y) == BiomeType.Beating
                && manager.WorldMap.GetPOI(_x, _y)?.Type == POIType.Lair, "chosen real Beating lair POI");

            // Temporary safety only for finite traversal. Factory dodge subjects
            // above were unmodified; the original player's HP fields are restored.
            _hp = _input.PlayerEntity.GetStat("Hitpoints");
            Require(_hp != null, "player hitpoints");
            _oldHpBase = _hp.BaseValue; _oldHpMax = _hp.Max; _oldHpPenalty = _hp.Penalty;
            _hp.BaseValue = _hp.Max = 1000000; _hp.Penalty = 0;
            yield return Tap(Key.LeftShift, Key.Comma);
            Require(WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID), "native < opens world map");
            var target = WorldMap.WorldCellToZoneCell(_x, _y);
            while (Position().x != target.Item1)
                yield return Step(Position().x > target.Item1 ? Key.A : Key.D,
                    Position().x > target.Item1 ? -1 : 1, 0);
            while (Position().y != target.Item2)
                yield return Step(Position().y > target.Item2 ? Key.W : Key.S,
                    0, Position().y > target.Item2 ? -1 : 1);
            Check("native_world_map_arrival", Position() == (target.Item1, target.Item2));
            yield return Tap(Key.LeftShift, Key.Period);
            Require(_input.CurrentZone.ZoneID == $"Overworld.{_x}.{_y}.0", "native > enters the real lair");
            var zone = _input.CurrentZone;
            var creatures = zone.GetEntitiesWithTag("Creature").Where(e => e != _input.PlayerEntity).ToArray();
            Check("live_desert_prowler", creatures.Count(e => e.BlueprintName == "DesertProwler") == 1);
            Check("live_sparse_traps", zone.GetAllEntities().Count(e => e.HasTag("Trap")) >= 1
                && zone.GetAllEntities().Count(e => e.HasTag("Trap")) <= 2);
            var arrivalCell = zone.GetEntityCell(_input.PlayerEntity);
            Check("player_arrival_clear_and_trap_free", !arrivalCell.BlocksMovement(_input.PlayerEntity)
                && !arrivalCell.Objects.Any(e => e.HasTag("Trap")));
            foreach (var creature in creatures)
                if (_input.TurnManager.IsRegistered(creature)) { _input.TurnManager.RemoveEntity(creature); _paused.Add(creature); }
            Check("inspection_actors_paused", _paused.Count > 0 && _paused.All(e => !_input.TurnManager.IsRegistered(e)));
            Require(_input.CameraFollow != null && _input.ZoneRenderer != null, "native camera and renderer");
            _input.CameraFollow.SnapToPlayer();
            ZoneRenderHooks.MarkFullDirty("DensityPhase1NativeAudit");
            yield return new WaitForSecondsRealtime(1f);
            Directory.CreateDirectory(DirectoryPath);
            _screenshot = Path.Combine(DirectoryPath, "beating-lair.png");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(_screenshot);
            yield return new WaitForSecondsRealtime(.5f);
            Check("screenshot_written", File.Exists(_screenshot) && new FileInfo(_screenshot).Length > 0);
        }

        private IEnumerator Step(Key key, int dx, int dy)
        {
            Require(++_steps <= WorldMap.Width + WorldMap.Height, "bounded cardinal route");
            var before = Position();
            yield return Tap(key);
            Require(Position() == (before.x + dx, before.y + dy), "native world-map step " + key);
        }
        private IEnumerator Tap(params Key[] keys)
        {
            Require(_clock.Elapsed.TotalSeconds < 150, "finite native audit deadline");
            double start = Time.realtimeSinceStartupAsDouble;
            while (_input != null && Time.time - (float)typeof(InputHandler).GetField("_lastMoveTime", Private).GetValue(_input) < _input.MoveRepeatDelay)
            { Require(Time.realtimeSinceStartupAsDouble - start < 3, "input rate gate reopens"); yield return null; }
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            yield return new WaitForSecondsRealtime(.13f);
        }
        private (int x, int y) Position() => _input.CurrentZone.GetEntityPosition(_input.PlayerEntity);
        private string State() => typeof(InputHandler).GetField("_inputState", Private).GetValue(_input).ToString();
        private void Require(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException("Density native precondition: " + reason); }
        private void Check(string name, bool passed)
        {
            if (!passed) _nativeFailures++;
            _audit.Add((passed ? "PASS " : "FAIL ") + name);
            Diag.Record("scenario", "DensityPhase1NativeCase", payload: new { runId = RunId, name, passed });
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception failure = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception error) { failure = error; }
                if (failure != null)
                { _fatal = failure.ToString(); Check("native_precondition_failed", false); Debug.LogError("[DensityPhase1Bench] " + failure); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Finish();
        }
        public void SetUnexpectedErrors(int errors)
        {
            _unexpectedErrors = errors; _errorsFinalized = true;
            WriteReport(); EmitSummary();
        }
        public void Abort(string reason)
        { if (Finished) return; StopAllCoroutines(); _fatal = reason; Check("native_aborted", false); Finish(); }
        private void Finish()
        {
            Cleanup();
            Finished = true; WriteReport();
            // The launcher finalizes its whole-run error count before the one
            // summary is emitted. An interrupted teardown emits an incomplete one.
        }
        private void EmitSummary()
        {
            if (_summaryEmitted) return; _summaryEmitted = true;
            Diag.Record("scenario", "DensityPhase1NativeSummary", payload: new
                { runId = RunId, numericCases = _bench?.Cases ?? 0, nativeCases = _audit.Count, failures = Failures,
                    unexpectedErrors = _unexpectedErrors, errorsFinalized = _errorsFinalized,
                    complete = Complete, seed = _seed, steps = _steps, screenshot = _screenshot });
        }
        private bool Complete => Finished && _errorsFinalized && Failures == 0 && _bench?.Cases == 18 && _audit.Count == 8;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);
            ReportPath = Path.Combine(DirectoryPath, "report.json");
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report
            {
                runId = RunId, seed = _seed, numericCases = _bench?.Cases ?? 0, nativeCases = _audit.Count,
                complete = Complete, errorsFinalized = _errorsFinalized,
                failures = Failures, unexpectedErrors = _unexpectedErrors, steps = _steps, seconds = _clock?.Elapsed.TotalSeconds ?? 0,
                zone = _input?.CurrentZone?.ZoneID, screenshot = _screenshot, fatal = _fatal,
                numericAudit = _bench?.Audit.ToArray() ?? Array.Empty<string>(), nativeAudit = _audit.ToArray(),
                canVerify = "Native bootstrap; real factory numeric dodge/stun/restore controls; seeded population and lair placement; native N/world-map/cardinal/descent input; real lair boss and traps; arrival free of blocking actors and traps; screenshot file.",
                cannotVerify = "No balance, combat AI, animation quality or difficulty claim. NPC scheduling is paused after arrival for the still image. Player HP is temporarily guarded during travel and restored. Screenshot appearance still requires visual inspection. Unexpected-error count covers Application.logMessageReceived only; native backend console messages require a separate console review."
            }, true));
            Debug.Log("[DensityPhase1Bench] report=" + ReportPath + " failures=" + Failures);
        }
        private void Cleanup()
        {
            if (_cleaned) return; _cleaned = true;
            if (_keyboard != null) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.RemoveDevice(_keyboard); }
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
            if (_hp != null) { _hp.BaseValue = _oldHpBase; _hp.Max = _oldHpMax; _hp.Penalty = _oldHpPenalty; }
            if (_input?.TurnManager != null)
                foreach (var creature in _paused) if (_input.CurrentZone.GetEntityCell(creature) != null) _input.TurnManager.AddEntity(creature);
        }
        private void OnDestroy()
        {
            if (!Finished && _clock != null) { _fatal = "Play stopped before completion."; Check("native_interrupted", false); Finish(); }
            Cleanup(); EmitSummary(); Diag.SetChannel("scenario", _oldScenario);
        }
        [Serializable] private sealed class Report
        {
            public string runId, zone, screenshot, fatal, canVerify, cannotVerify;
            public int seed, numericCases, nativeCases, failures, unexpectedErrors, steps;
            public double seconds;
            public bool complete, errorsFinalized;
            public string[] numericAudit, nativeAudit;
        }
    }
}
