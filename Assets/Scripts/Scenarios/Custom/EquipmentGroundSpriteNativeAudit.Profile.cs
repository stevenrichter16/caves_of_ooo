using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class EquipmentGroundSpriteNativeAudit
    {
        private const int FrameCapacity = 30000;
        private const double PhaseSeconds = 25;
        private static readonly string[] MetricNames = { "COO.EnvSprites.PostRender", "COO.ZoneRenderer.LateUpdate",
            "COO.Input.Update", "Main Thread", "GC Allocated In Frame" };
        private readonly List<Phase> phases = new List<Phase>(3);
        private ProfilerRecorder[] recorders;
        private string[] recorderUnits;
        private Frame[] raw;
        private int rawCount;
        private int[] consumedSamples;
        private readonly WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

        private void StartRecorders()
        {
            raw = new Frame[FrameCapacity];
            recorders = new ProfilerRecorder[MetricNames.Length]; recorderUnits = new string[MetricNames.Length]; consumedSamples = new int[MetricNames.Length];
            var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
            for (int i = 0; i < MetricNames.Length; i++)
            {
                bool found = false;
                foreach (var handle in handles)
                {
                    var description = ProfilerRecorderHandle.GetDescription(handle);
                    if (description.Name != MetricNames[i]) continue;
                    // Main Thread is a marker too; every recorder needs frame aggregation.
                    // Deliberately omit WrapAround so each sample is consumed exactly once.
                    var options = ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.SumAllSamplesInFrame;
                    recorders[i] = ProfilerRecorder.StartNew(description.Category, description.Name, FrameCapacity, options);
                    recorderUnits[i] = description.UnitType.ToString(); found = true; break;
                }
                Require(found && recorders[i].Valid, "Required real profiler marker unavailable: " + MetricNames[i]);
                Require(recorderUnits[i] == (i == 4 ? "Bytes" : "TimeNanoseconds"), "Unexpected profiler units: " + MetricNames[i]);
            }
        }

        private IEnumerator MeasurePhase(int phaseIndex, string name)
        {
            // Each phase measures actual native Perf counters from zero. Restore
            // the inherited counters at teardown; MaxTicks is a phase maximum,
            // not a subtraction of two historical maxima.
            EnvironmentSpriteRenderer.Perf.Reset();
            var phase = new Phase { name = name, metrics = new Metric[MetricNames.Length], rawStart = rawCount };
            for (int i = 0; i < MetricNames.Length; i++) phase.metrics[i] = new Metric
                { name = MetricNames[i], units = recorderUnits[i], available = recorders[i].Valid };
            phases.Add(phase);
            for (int i = 0; i < recorders.Length; i++) consumedSamples[i] = recorders[i].Count;
            double begin = Time.realtimeSinceStartupAsDouble;
            while (Time.realtimeSinceStartupAsDouble - begin < PhaseSeconds)
            {
                double elapsed = Time.realtimeSinceStartupAsDouble - begin;
                bool changedItem = false;
                // Fixed work count/cadence: no per-frame operation count bias.
                if (phaseIndex != 0 && phase.operations < OperationsPerPhase
                    && elapsed >= phase.operations * (PhaseSeconds / OperationsPerPhase))
                {
                    if (phaseIndex == 1)
                    {
                        int dx = (phase.operations / 10) % 2 == 0 ? 1 : -1;
                        Require(MovementSystem.TryMove(actor, zone, dx, 0), "Native benchmark move refused.");
                        phase.moves++;
                    }
                    else
                    {
                        bool pickup = phase.operations % 2 == 0;
                        Require(pickup ? InventorySystem.Pickup(actor, workItem, zone) : InventorySystem.Drop(actor, workItem, zone),
                            pickup ? "Native pickup refused." : "Native drop refused.");
                        bool held = actor.GetPart<InventoryPart>().Contains(workItem);
                        Require(held == pickup && (zone.GetEntityCell(workItem) == null) == pickup, "Native item membership changed incorrectly.");
                        renderer.MarkCellDirty(30, 21, "GroundAudit.Inventory");
                        if (pickup) phase.pickups++; else phase.drops++;
                        changedItem = true;
                    }
                    phase.operations++;
                }
                yield return endOfFrame;
                if (changedItem)
                {
                    bool held = phase.operations % 2 == 1;
                    string painted = TileName(new Vector3Int(30, Zone.Height - 1 - 21, 0));
                    Require(held ? painted != null && painted.StartsWith("floor_m", StringComparison.Ordinal)
                        : painted == ExpectedTile(0), "Native pickup/drop overlay failed to release or restore its ground item: held=" + held + ", tile=" + (painted ?? "<null>"));
                    Require(zone.GetCell(30, 21).GetTopVisibleObject()?.BlueprintName == (held ? "Floor" : "Dagger"), "Carrier visibility must not vacuously hide the item.");
                    phase.overlayChecks++;
                }
                Require(rawCount < FrameCapacity, "Raw frame capacity exceeded.");
                var sample = new Frame { phase = phaseIndex, unityFrame = Time.frameCount,
                    seconds = Time.realtimeSinceStartupAsDouble - started };
                for (int i = 0; i < recorders.Length; i++)
                {
                    var metric = phase.metrics[i];
                    metric.observedFrames++;
                    if (!recorders[i].Valid || recorders[i].WrappedAround)
                    { metric.available = false; sample.Set(i, -1); continue; }
                    int count = recorders[i].Count;
                    if (count < consumedSamples[i] || count >= FrameCapacity)
                    { metric.available = false; sample.Set(i, -1); continue; }
                    long value = 0; int fresh = count - consumedSamples[i];
                    // Consume each actual profiler sample once. A valid sparse
                    // marker can emit nothing in an idle frame; explicitly count
                    // that condition instead of replaying a stale LastValue.
                    for (int n = consumedSamples[i]; n < count; n++)
                    {
                        var reading = recorders[i].GetSample(n); value += reading.Value;
                        metric.count++; metric.invocations += reading.Count;
                    }
                    consumedSamples[i] = count;
                    if (fresh == 0) metric.zeroEmissionFrames++;
                    metric.sum += value; if (value > metric.max) metric.max = value;
                    sample.Set(i, value);
                }
                raw[rawCount++] = sample; phase.frames++;
                // EndOfFrame yields again next frame instead of sampling the
                // same rendered frame multiple times in a nested enumerator.
                yield return null;
            }
            phase.seconds = Time.realtimeSinceStartupAsDouble - begin;
            phase.counters = new long[8]; ReadPerf(phase.counters);
            for (int i = 0; i < phase.metrics.Length; i++)
            {
                var m = phase.metrics[i]; m.average = m.observedFrames == 0 ? 0 : (double)m.sum / m.observedFrames;
                var sorted = new long[phase.frames];
                for (int j = 0; j < sorted.Length; j++) sorted[j] = raw[phase.rawStart + j].Get(i);
                Array.Sort(sorted); m.p99 = sorted.Length == 0 ? -1 : sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(sorted.Length * .99) - 1)];
                Add(name + "_marker_" + i, m.available && m.observedFrames == phase.frames && (i == 0 && phaseIndex == 0 || m.count > 10)
                    && (i == 0 && phaseIndex == 0 || i == 4 || m.max > 0), MetricNames[i]);
            }
            Add(name + "_duration", phase.seconds >= PhaseSeconds && phase.seconds <= PhaseSeconds + 3 && phase.frames >= 100);
            Add(name + "_work_count", phase.operations == (phaseIndex == 0 ? 0 : OperationsPerPhase));
            Add(name + "_claim_lifecycle", phase.overlayChecks == (phaseIndex == 2 ? OperationsPerPhase : 0));
            Add(name + "_sparse_marker", phaseIndex != 0 || phase.metrics[0].sum == 0,
                "Valid EnvSprites handle with zero emitted work is legitimate idle; missing handles are rejected at discovery.");
            // Paired renderer-work controls. Idle should not resolve cells;
            // moving the Player-tagged carrier must redraw full80x25;
            // stationary native acquisition only dirties a small neighborhood.
            Add(name + "_renderer_path", phaseIndex == 0 ? phase.counters[0] == 0 && phase.counters[3] == 0
                : phaseIndex == 1 ? phase.counters[1] >= OperationsPerPhase && phase.counters[3] >= OperationsPerPhase * Zone.Width * Zone.Height
                : phase.counters[2] >= OperationsPerPhase && phase.counters[1] == 0 && phase.counters[3] <= OperationsPerPhase * 18,
                "Counters: Frames,FullPasses,IncrementalPasses,CellsResolved,ClaimsMade,TilemapWrites,TotalTicks,MaxTicks.");
            yield return null;
        }

        private void StopRecorders()
        {
            if (recorders == null) return;
            for (int i = 0; i < recorders.Length; i++) if (recorders[i].Valid) recorders[i].Dispose();
            recorders = null;
        }
        private void WriteRawFrames()
        {
            using (var writer = new StreamWriter(Path.Combine(output, stem + "-frames.csv")))
            {
                writer.WriteLine("phase,unityFrame,seconds,EnvSprites_ns,ZoneRenderer_ns,InputIdle_ns,MainThread_ns,GC_bytes");
                for (int i = 0; i < rawCount; i++)
                {
                    var f = raw[i]; writer.WriteLine(f.phase + "," + f.unityFrame + "," + f.seconds.ToString("R", CultureInfo.InvariantCulture)
                        + "," + f.env + "," + f.zone + "," + f.input + "," + f.main + "," + f.gc);
                }
            }
        }
        private static void ReadPerf(long[] p)
        {
            p[0] = EnvironmentSpriteRenderer.Perf.Frames; p[1] = EnvironmentSpriteRenderer.Perf.FullPasses;
            p[2] = EnvironmentSpriteRenderer.Perf.IncrementalPasses; p[3] = EnvironmentSpriteRenderer.Perf.CellsResolved;
            p[4] = EnvironmentSpriteRenderer.Perf.ClaimsMade; p[5] = EnvironmentSpriteRenderer.Perf.TilemapWrites;
            p[6] = EnvironmentSpriteRenderer.Perf.TotalTicks; p[7] = EnvironmentSpriteRenderer.Perf.MaxTicks;
        }
        private static void WritePerf(long[] p)
        {
            EnvironmentSpriteRenderer.Perf.Frames = p[0]; EnvironmentSpriteRenderer.Perf.FullPasses = p[1];
            EnvironmentSpriteRenderer.Perf.IncrementalPasses = p[2]; EnvironmentSpriteRenderer.Perf.CellsResolved = p[3];
            EnvironmentSpriteRenderer.Perf.ClaimsMade = p[4]; EnvironmentSpriteRenderer.Perf.TilemapWrites = p[5];
            EnvironmentSpriteRenderer.Perf.TotalTicks = p[6]; EnvironmentSpriteRenderer.Perf.MaxTicks = p[7];
        }
        private struct Frame
        {
            public int phase, unityFrame; public double seconds; public long env, zone, input, main, gc;
            public long Get(int i) => i == 0 ? env : i == 1 ? zone : i == 2 ? input : i == 3 ? main : gc;
            public void Set(int i, long v) { if (i == 0) env = v; else if (i == 1) zone = v; else if (i == 2) input = v; else if (i == 3) main = v; else gc = v; }
        }
        [Serializable] public sealed class Phase
        {
            public string name; public double seconds; public int frames, rawStart, operations, moves, pickups, drops, overlayChecks;
            public long[] counters; public Metric[] metrics;
        }
        [Serializable] public sealed class Metric
        { public string name, units; public bool available; public int count, observedFrames, zeroEmissionFrames; public long sum, max, p99, invocations; public double average; }
    }
}
