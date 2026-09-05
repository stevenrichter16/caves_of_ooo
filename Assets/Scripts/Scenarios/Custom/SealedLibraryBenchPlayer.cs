using System;
using System.IO;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Scenario-only native input workload and whole-interval CPU
    /// capture. The capture uses actual input-driven walking through the open archive door.</summary>
    public sealed class SealedLibraryBenchPlayer : MonoBehaviour
    {
        public const double CaptureSeconds = 75;
        private const int Capacity = 120000;
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        public string ReportPath { get; private set; }
        private ScenarioContext _ctx;
        private SealedLibraryBench _bench;
        private double _created, _started;
        private int _frames, _startTicks, _stage, _moves;
        private bool _pressed;
        private double _nextPulse;
        private (int x,int y) _lastPosition;
        private readonly string[] _names = { "COO.Turns.ProcessUntilPlayerTurn", "COO.World.ArchiveBarrier", "COO.Input.Update", "COO.ZoneRenderer.LateUpdate", "GC Allocated In Frame" };
        private ProfilerRecorder[] _recorders;
        private long[][] _samples;
        private Keyboard _keyboard;
        private bool _ownsKeyboard;
        private InputSettings _previousInputSettings, _scenarioInputSettings;
        private bool _previousBackground;

        public void Initialize(ScenarioContext ctx, SealedLibraryBench bench)
        {
            _ctx = ctx; _bench = bench; _created = Time.realtimeSinceStartupAsDouble;
            Failures = bench.Failures;
            // Batch editors have no focused Game view. Use a disposable
            // settings clone so native queued input reaches the game without
            // changing the project's saved input preferences.
            _previousInputSettings = InputSystem.settings;
            _scenarioInputSettings = Instantiate(_previousInputSettings);
            _scenarioInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _scenarioInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _scenarioInputSettings;
            _previousBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>(); _ownsKeyboard = true;
        }

        private void Update()
        {
            if (_ctx == null || Finished) return;
            double elapsed = Time.realtimeSinceStartupAsDouble - _created;
            if (_stage == 0 && elapsed >= 0.5) { SaveGameService.RegisterRuntime(null, null); QueueKey(Key.N); _stage = 1; }
            else if (_stage == 1 && elapsed >= 0.7) { Release(); _stage = 2; }
            else if (_stage == 2 && elapsed >= 2) { QueueKey(Key.D); _stage = 3; }
            else if (_stage == 3 && elapsed >= 2.2)
            {
                Release(); Audit("native_keyless_bump", _bench.Door.GetPart<LockPart>().IsLocked
                    && _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (_bench.DoorX - 1, _bench.DoorY)); _stage = 4;
            }
            else if (_stage == 4 && elapsed >= 2.4)
            { SealedLibraryBench.GiveTestKey(_ctx.PlayerEntity); QueueKey(Key.D); _stage = 5; }
            else if (_stage == 5 && elapsed >= 2.5)
            {
                Release(); Audit("native_matching_key", !_bench.Door.GetPart<LockPart>().IsLocked); _stage = 6;
            }
            else if (_stage == 6 && elapsed >= 2.8) { QueueKey(Key.D); _stage = 7; }
            else if (_stage == 7 && elapsed >= 2.9)
            {
                Release(); var p = _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity);
                Audit("native_enters_door", p == (_bench.DoorX, _bench.DoorY)); _stage = 8;
            }
            else if (_stage == 8 && elapsed >= 3.2)
            {
                _recorders = new ProfilerRecorder[_names.Length]; _samples = new long[_names.Length][];
                for (int i = 0; i < _names.Length; i++)
                {
                    _samples[i] = new long[Capacity];
                    _recorders[i] = ProfilerRecorder.StartNew(i == _names.Length - 1 ? ProfilerCategory.Memory : ProfilerCategory.Scripts,
                        _names[i], 2, ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
                }
                _started = Time.realtimeSinceStartupAsDouble; _startTicks = _ctx.Turns.TickCount;
                _lastPosition = _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity); _nextPulse = _started; _stage = 9;
            }
            if (_stage == 9)
            {
                double now = Time.realtimeSinceStartupAsDouble;
                var pos = _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity);
                if (pos != _lastPosition) { _moves++; _lastPosition = pos; }
                if (now >= _nextPulse)
                {
                    if (_pressed) { Release(); _pressed = false; }
                    else { QueueKey(pos.x <= _bench.DoorX ? Key.D : Key.A); _pressed = true; }
                    _nextPulse = now + .03;
                }
                _ctx.PlayerEntity.GetStat("Hitpoints").Penalty = 0;
                if (now - _started >= CaptureSeconds) Finish();
            }
        }
        private void Audit(string name, bool passed) { _bench.Check(name, passed); if (!passed) Failures++; }

        private void LateUpdate()
        {
            if (_stage != 9 || Finished) return;
            if (_frames >= Capacity) { Failures++; Finish(); return; }
            for (int i = 0; i < _recorders.Length; i++)
                _samples[i][_frames] = _recorders[i].Valid ? _recorders[i].LastValue : -1;
            _frames++;
        }

        private void Finish()
        {
            Release(); Finished = true;
            int ticks = _ctx.Turns.TickCount - _startTicks;
            if (_frames < 100) Failures++;
            var pos = _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity);
            Audit("native_repeated_open_entry", _moves >= 500 && ticks >= 500
                && !_bench.Door.GetPart<LockPart>().IsLocked
                && pos.y == _bench.DoorY && pos.x >= _bench.DoorX && pos.x <= _bench.DoorX + 1);
            var report = new Report { runId = _bench.RunId, seconds = Time.realtimeSinceStartupAsDouble - _started,
                frames = _frames, ticks = ticks, moves = _moves, audit = _bench.Audit.ToArray(), metrics = new Metric[_names.Length] };
            var csv = new StringBuilder("frame," + string.Join(",", _names) + "\n");
            for (int frame = 0; frame < _frames; frame++)
            {
                csv.Append(frame);
                for (int i = 0; i < _names.Length; i++) csv.Append(',').Append(_samples[i][frame]);
                csv.Append('\n');
            }
            for (int i = 0; i < _names.Length; i++)
            {
                var metric = new Metric { name = _names[i], unit = i == _names.Length - 1 ? "bytes" : "nanoseconds",
                    available = _recorders[i].Valid };
                var sorted = new long[_frames]; Array.Copy(_samples[i], sorted, _frames); Array.Sort(sorted);
                double sum = 0;
                foreach (long value in sorted) { if (value > 0) { metric.activeFrames++; sum += value; } }
                if (_frames > 0) { metric.max = sorted[_frames - 1]; metric.p99 = sorted[(int)((_frames - 1) * .99)]; metric.mean = sum / _frames; }
                if (i <= 1 && (!metric.available || metric.activeFrames == 0)) Failures++;
                report.metrics[i] = metric; _recorders[i].Dispose();
            }
            _recorders = null; report.failures = Failures;
            string directory = Path.Combine(Application.dataPath, "../Docs/Verification/FellingW6");
            Directory.CreateDirectory(directory); ReportPath = Path.Combine(directory, "W66-live-profile.json");
            File.WriteAllText(Path.Combine(directory, "W66-live-frames.csv"), csv.ToString());
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            Diag.Record("scenario", "SealedLibraryProfile", payload: new { runId = _bench.RunId, ticks, frames = _frames, failures = Failures });
            Debug.Log("[SealedLibraryBench] " + JsonUtility.ToJson(report));
        }

        private void QueueKey(Key key) => InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
        private void Release() { if (_keyboard != null) InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); }
        private void OnDestroy()
        {
            Release();
            if (_recorders != null) foreach (var recorder in _recorders) recorder.Dispose();
            if (_ownsKeyboard && _keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_previousInputSettings != null) InputSystem.settings = _previousInputSettings;
            if (_scenarioInputSettings != null) Destroy(_scenarioInputSettings);
            Application.runInBackground = _previousBackground;
        }
        [Serializable] private sealed class Metric
        { public string name, unit; public bool available; public int activeFrames; public long max, p99; public double mean; }
        [Serializable] private sealed class Report
        { public string runId; public double seconds; public int frames, ticks, moves, failures; public string[] audit; public Metric[] metrics; }
    }
}
