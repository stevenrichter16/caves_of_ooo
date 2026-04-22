using System.Linq;
using CavesOfOoo.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    internal static class PerformanceCaptureMenu
    {
        [MenuItem("Caves Of Ooo/Diagnostics/Log Startup Metrics")]
        private static void LogStartupMetrics()
        {
            var payload = new StartupCaptureWrapper
            {
                startupTotalMs = PerformanceDiagnostics.StartupTotalMs,
                phases = PerformanceDiagnostics.StartupPhases
                    .Select(phase => new StartupPhaseEntry
                    {
                        PhaseName = phase.PhaseName,
                        DurationMs = phase.DurationMs
                    })
                    .ToArray()
            };

            Debug.Log($"[PerfCapture] Startup {JsonUtility.ToJson(payload)}");
        }

        [MenuItem("Caves Of Ooo/Diagnostics/Log Frame Snapshot")]
        private static void LogFrameSnapshot()
        {
            var snapshot = PerformanceDiagnostics.LastCompletedFrameSnapshot;
            if (snapshot == null)
            {
                Debug.Log("[PerfCapture] Snapshot {}");
                return;
            }

            var payload = new SnapshotCaptureWrapper
            {
                UnityFrame = snapshot.UnityFrame,
                TurnTick = snapshot.TurnTick,
                Paused = snapshot.Paused,
                RendererDirty = snapshot.RendererDirty,
                ZoneRendererLateUpdateMs = snapshot.ZoneRendererLateUpdateMs,
                FullZoneRedrawCount = snapshot.FullZoneRedrawCount,
                CellsRendered = snapshot.CellsRendered,
                TilemapClearCount = snapshot.TilemapClearCount,
                SidebarRenderCount = snapshot.SidebarRenderCount,
                HotbarRenderCount = snapshot.HotbarRenderCount,
                InventoryRenderCount = snapshot.InventoryRenderCount,
                TradeRenderCount = snapshot.TradeRenderCount,
                DialogueRenderCount = snapshot.DialogueRenderCount,
                ActiveProjectiles = snapshot.ActiveProjectiles,
                ActiveBursts = snapshot.ActiveBursts,
                ActiveParticles = snapshot.ActiveParticles,
                ActiveAuras = snapshot.ActiveAuras,
                ActiveBeams = snapshot.ActiveBeams,
                ActiveChargeOrbits = snapshot.ActiveChargeOrbits,
                ActiveRingWaves = snapshot.ActiveRingWaves,
                ActiveChainArcs = snapshot.ActiveChainArcs,
                ActiveColumnRises = snapshot.ActiveColumnRises,
                ActiveDustMotes = snapshot.ActiveDustMotes,
                MarkDirtyCount = snapshot.MarkDirtyCount,
                markDirtyBySource = snapshot.MarkDirtyBySource
                    .Select(kvp => new MarkDirtyEntry { source = kvp.Key, count = kvp.Value })
                    .ToArray(),
                inventorySessionRenders = PerformanceDiagnostics.LastCompletedInventorySessionRenderCount
            };

            Debug.Log($"[PerfCapture] Snapshot {JsonUtility.ToJson(payload)}");
        }

        [MenuItem("Caves Of Ooo/Diagnostics/Enable Verbose Perf Logging")]
        private static void EnableVerbosePerfLogging()
        {
            PerformanceDiagnostics.VerboseLoggingEnabled = true;
            Debug.Log("[PerfCapture] Verbose logging enabled");
        }

        [MenuItem("Caves Of Ooo/Diagnostics/Disable Verbose Perf Logging")]
        private static void DisableVerbosePerfLogging()
        {
            PerformanceDiagnostics.VerboseLoggingEnabled = false;
            Debug.Log("[PerfCapture] Verbose logging disabled");
        }

        [System.Serializable]
        private struct StartupPhaseEntry
        {
            public string PhaseName;
            public double DurationMs;
        }

        [System.Serializable]
        private struct StartupCaptureWrapper
        {
            public double startupTotalMs;
            public StartupPhaseEntry[] phases;
        }

        [System.Serializable]
        private struct MarkDirtyEntry
        {
            public string source;
            public int count;
        }

        [System.Serializable]
        private struct SnapshotCaptureWrapper
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
            public MarkDirtyEntry[] markDirtyBySource;
            public int inventorySessionRenders;
        }
    }
}
