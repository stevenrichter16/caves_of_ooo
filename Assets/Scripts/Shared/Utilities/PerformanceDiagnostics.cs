using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Profiling;
using UnityEngine;

namespace CavesOfOoo.Diagnostics
{
    [Serializable]
    public sealed class PerformanceFrameSnapshot
    {
        public int UnityFrame;
        public int TurnTick;
        public bool Paused;
        public bool RendererDirty;
        public double ZoneRendererLateUpdateMs;
        public int FullZoneRedrawCount;
        public int CellsRendered;
        public int TilemapClearCount;
        public int SidebarRenderCount;
        public int HotbarRenderCount;
        public int InventoryRenderCount;
        public int TradeRenderCount;
        public int DialogueRenderCount;
        public int ActiveProjectiles;
        public int ActiveBursts;
        public int ActiveParticles;
        public int ActiveAuras;
        public int ActiveBeams;
        public int ActiveChargeOrbits;
        public int ActiveRingWaves;
        public int ActiveChainArcs;
        public int ActiveColumnRises;
        public int ActiveDustMotes;
        public int MarkDirtyCount;
        public readonly Dictionary<string, int> MarkDirtyBySource = new Dictionary<string, int>(StringComparer.Ordinal);

        public PerformanceFrameSnapshot()
        {
        }

        public PerformanceFrameSnapshot(PerformanceFrameSnapshot other)
        {
            if (other == null)
                return;

            UnityFrame = other.UnityFrame;
            TurnTick = other.TurnTick;
            Paused = other.Paused;
            RendererDirty = other.RendererDirty;
            ZoneRendererLateUpdateMs = other.ZoneRendererLateUpdateMs;
            FullZoneRedrawCount = other.FullZoneRedrawCount;
            CellsRendered = other.CellsRendered;
            TilemapClearCount = other.TilemapClearCount;
            SidebarRenderCount = other.SidebarRenderCount;
            HotbarRenderCount = other.HotbarRenderCount;
            InventoryRenderCount = other.InventoryRenderCount;
            TradeRenderCount = other.TradeRenderCount;
            DialogueRenderCount = other.DialogueRenderCount;
            ActiveProjectiles = other.ActiveProjectiles;
            ActiveBursts = other.ActiveBursts;
            ActiveParticles = other.ActiveParticles;
            ActiveAuras = other.ActiveAuras;
            ActiveBeams = other.ActiveBeams;
            ActiveChargeOrbits = other.ActiveChargeOrbits;
            ActiveRingWaves = other.ActiveRingWaves;
            ActiveChainArcs = other.ActiveChainArcs;
            ActiveColumnRises = other.ActiveColumnRises;
            ActiveDustMotes = other.ActiveDustMotes;
            MarkDirtyCount = other.MarkDirtyCount;

            foreach (KeyValuePair<string, int> kvp in other.MarkDirtyBySource)
                MarkDirtyBySource[kvp.Key] = kvp.Value;
        }
    }

    [Serializable]
    public sealed class StartupPhaseTiming
    {
        public string PhaseName;
        public double DurationMs;
    }

    /// <summary>
    /// Lightweight, development-friendly performance diagnostics collected alongside profiler markers.
    /// Counters reset per rendered frame and session-level UI metrics are tracked separately so hitch captures
    /// can be correlated with what the game was doing without changing gameplay behavior.
    /// </summary>
    public static class PerformanceDiagnostics
    {
        private static readonly List<StartupPhaseTiming> StartupPhasesInternal = new List<StartupPhaseTiming>();
        private static readonly PerformanceFrameSnapshot CurrentFrame = new PerformanceFrameSnapshot();

        public const double DefaultSpikeThresholdMs = 16.7d;

        public static bool VerboseLoggingEnabled;
        public static bool DetailedCellProfilingEnabled;
        public static double SpikeThresholdMs { get; set; } = DefaultSpikeThresholdMs;
        public static PerformanceFrameSnapshot LastCompletedFrameSnapshot { get; private set; } = new PerformanceFrameSnapshot();
        public static IReadOnlyList<StartupPhaseTiming> StartupPhases => StartupPhasesInternal;
        public static double StartupTotalMs { get; private set; }
        public static int CurrentInventorySessionRenderCount { get; private set; }
        public static int LastCompletedInventorySessionRenderCount { get; private set; }
        public static int InventorySessionId { get; private set; }

        public static void ResetAll()
        {
            StartupPhasesInternal.Clear();
            StartupTotalMs = 0d;
            ResetCurrentFrame();
            LastCompletedFrameSnapshot = new PerformanceFrameSnapshot();
            CurrentInventorySessionRenderCount = 0;
            LastCompletedInventorySessionRenderCount = 0;
            InventorySessionId = 0;
        }

        public static void BeginFrame(int turnTick, bool paused, bool rendererDirty)
        {
            ResetCurrentFrame();
            CurrentFrame.UnityFrame = Time.frameCount;
            CurrentFrame.TurnTick = turnTick;
            CurrentFrame.Paused = paused;
            CurrentFrame.RendererDirty = rendererDirty;
        }

        public static void EndFrame(double zoneRendererLateUpdateMs)
        {
            CurrentFrame.ZoneRendererLateUpdateMs = zoneRendererLateUpdateMs;
            LastCompletedFrameSnapshot = new PerformanceFrameSnapshot(CurrentFrame);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (VerboseLoggingEnabled && zoneRendererLateUpdateMs >= SpikeThresholdMs)
            {
                UnityEngine.Debug.Log(
                    "[Perf] ZoneRenderer spike " +
                    $"frame={CurrentFrame.UnityFrame} tick={CurrentFrame.TurnTick} ms={zoneRendererLateUpdateMs:F2} " +
                    $"dirty={CurrentFrame.RendererDirty} redraws={CurrentFrame.FullZoneRedrawCount} " +
                    $"cells={CurrentFrame.CellsRendered} clears={CurrentFrame.TilemapClearCount} " +
                    $"sidebar={CurrentFrame.SidebarRenderCount} hotbar={CurrentFrame.HotbarRenderCount} " +
                    $"inventoryRenders={CurrentFrame.InventoryRenderCount} " +
                    $"fx[p={CurrentFrame.ActiveProjectiles},b={CurrentFrame.ActiveBursts},pt={CurrentFrame.ActiveParticles},a={CurrentFrame.ActiveAuras},bm={CurrentFrame.ActiveBeams}] " +
                    $"markDirty={FormatMarkDirtySources(CurrentFrame.MarkDirtyBySource)}");
            }
#endif
        }

        public static void BeginInventorySession()
        {
            InventorySessionId++;
            CurrentInventorySessionRenderCount = 0;
        }

        public static void EndInventorySession()
        {
            LastCompletedInventorySessionRenderCount = CurrentInventorySessionRenderCount;
            CurrentInventorySessionRenderCount = 0;
        }

        public static void RecordZoneRedraw(int cellsRendered)
        {
            CurrentFrame.FullZoneRedrawCount++;
            CurrentFrame.CellsRendered += Math.Max(0, cellsRendered);
        }

        public static void RecordTilemapClear(int count = 1)
        {
            CurrentFrame.TilemapClearCount += Math.Max(0, count);
        }

        public static void RecordSidebarRender()
        {
            CurrentFrame.SidebarRenderCount++;
        }

        public static void RecordHotbarRender()
        {
            CurrentFrame.HotbarRenderCount++;
        }

        public static void RecordInventoryRender()
        {
            CurrentFrame.InventoryRenderCount++;
            CurrentInventorySessionRenderCount++;
        }

        public static void RecordTradeRender()
        {
            CurrentFrame.TradeRenderCount++;
        }

        public static void RecordDialogueRender()
        {
            CurrentFrame.DialogueRenderCount++;
        }

        public static void RecordMarkDirty(string source)
        {
            string bucket = string.IsNullOrWhiteSpace(source) ? "Unknown" : source;
            CurrentFrame.MarkDirtyCount++;
            if (CurrentFrame.MarkDirtyBySource.TryGetValue(bucket, out int existing))
                CurrentFrame.MarkDirtyBySource[bucket] = existing + 1;
            else
                CurrentFrame.MarkDirtyBySource[bucket] = 1;
        }

        public static void RecordAsciiFxCounts(
            int activeProjectiles,
            int activeBursts,
            int activeParticles,
            int activeAuras,
            int activeBeams,
            int activeChargeOrbits,
            int activeRingWaves,
            int activeChainArcs,
            int activeColumnRises,
            int activeDustMotes)
        {
            CurrentFrame.ActiveProjectiles = Math.Max(0, activeProjectiles);
            CurrentFrame.ActiveBursts = Math.Max(0, activeBursts);
            CurrentFrame.ActiveParticles = Math.Max(0, activeParticles);
            CurrentFrame.ActiveAuras = Math.Max(0, activeAuras);
            CurrentFrame.ActiveBeams = Math.Max(0, activeBeams);
            CurrentFrame.ActiveChargeOrbits = Math.Max(0, activeChargeOrbits);
            CurrentFrame.ActiveRingWaves = Math.Max(0, activeRingWaves);
            CurrentFrame.ActiveChainArcs = Math.Max(0, activeChainArcs);
            CurrentFrame.ActiveColumnRises = Math.Max(0, activeColumnRises);
            CurrentFrame.ActiveDustMotes = Math.Max(0, activeDustMotes);
        }

        public static void RecordStartupPhase(string phaseName, double durationMs)
        {
            StartupPhasesInternal.Add(new StartupPhaseTiming
            {
                PhaseName = phaseName,
                DurationMs = durationMs
            });
        }

        public static T MeasureStartupPhase<T>(string phaseName, ProfilerMarker marker, Func<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            long start = Stopwatch.GetTimestamp();
            using (marker.Auto())
            {
                T result = action();
                double elapsedMs = ElapsedMilliseconds(start, Stopwatch.GetTimestamp());
                RecordStartupPhase(phaseName, elapsedMs);
                return result;
            }
        }

        public static void MeasureStartupPhase(string phaseName, ProfilerMarker marker, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            long start = Stopwatch.GetTimestamp();
            using (marker.Auto())
            {
                action();
                double elapsedMs = ElapsedMilliseconds(start, Stopwatch.GetTimestamp());
                RecordStartupPhase(phaseName, elapsedMs);
            }
        }

        public static void RecordStartupTotal(double totalMs)
        {
            StartupTotalMs = totalMs;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (VerboseLoggingEnabled)
            {
                UnityEngine.Debug.Log(
                    "[Perf] Bootstrap startup " +
                    $"total={totalMs:F2}ms phases={FormatStartupPhases()}");
            }
#endif
        }

        public static double ElapsedMilliseconds(long startTimestamp, long endTimestamp)
        {
            return (endTimestamp - startTimestamp) * 1000d / Stopwatch.Frequency;
        }

        private static void ResetCurrentFrame()
        {
            CurrentFrame.UnityFrame = 0;
            CurrentFrame.TurnTick = 0;
            CurrentFrame.Paused = false;
            CurrentFrame.RendererDirty = false;
            CurrentFrame.ZoneRendererLateUpdateMs = 0d;
            CurrentFrame.FullZoneRedrawCount = 0;
            CurrentFrame.CellsRendered = 0;
            CurrentFrame.TilemapClearCount = 0;
            CurrentFrame.SidebarRenderCount = 0;
            CurrentFrame.HotbarRenderCount = 0;
            CurrentFrame.InventoryRenderCount = 0;
            CurrentFrame.TradeRenderCount = 0;
            CurrentFrame.DialogueRenderCount = 0;
            CurrentFrame.ActiveProjectiles = 0;
            CurrentFrame.ActiveBursts = 0;
            CurrentFrame.ActiveParticles = 0;
            CurrentFrame.ActiveAuras = 0;
            CurrentFrame.ActiveBeams = 0;
            CurrentFrame.ActiveChargeOrbits = 0;
            CurrentFrame.ActiveRingWaves = 0;
            CurrentFrame.ActiveChainArcs = 0;
            CurrentFrame.ActiveColumnRises = 0;
            CurrentFrame.ActiveDustMotes = 0;
            CurrentFrame.MarkDirtyCount = 0;
            CurrentFrame.MarkDirtyBySource.Clear();
        }

        private static string FormatStartupPhases()
        {
            if (StartupPhasesInternal.Count == 0)
                return "(none)";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < StartupPhasesInternal.Count; i++)
            {
                StartupPhaseTiming phase = StartupPhasesInternal[i];
                if (i > 0)
                    builder.Append(", ");
                builder.Append(phase.PhaseName);
                builder.Append('=');
                builder.Append(phase.DurationMs.ToString("F2"));
                builder.Append("ms");
            }

            return builder.ToString();
        }

        private static string FormatMarkDirtySources(Dictionary<string, int> markDirtyBySource)
        {
            if (markDirtyBySource == null || markDirtyBySource.Count == 0)
                return "(none)";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            bool first = true;
            foreach (KeyValuePair<string, int> kvp in markDirtyBySource)
            {
                if (!first)
                    builder.Append(", ");
                first = false;
                builder.Append(kvp.Key);
                builder.Append(':');
                builder.Append(kvp.Value);
            }

            return builder.ToString();
        }
    }
}
