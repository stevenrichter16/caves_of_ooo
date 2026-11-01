using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using CavesOfOoo.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    internal static class PerformanceCaptureMenu
    {
        // Editor-only observation of ordinary manually controlled gameplay.
        // Recorder buffers are bounded; report allocation/sorting happens after Stop.
        const int ManualCapacity = 120000;
        static readonly string[] ManualNames = { "COO.ZoneRenderer.LateUpdate", "Main Thread",
            "GC Allocated In Frame", "Batches Count", "Draw Calls Count" };
        static ProfilerRecorder[] manualRecorders;
        static ManualCapture manualCapture;
        static System.Diagnostics.Stopwatch manualClock;

        [MenuItem("Caves Of Ooo/Diagnostics/Start Manual Gameplay Profile")]
        static void StartManualGameplayProfile()
        {
            if (!EditorApplication.isPlaying || manualRecorders != null)
            { Debug.LogWarning("[PerfCapture] Requires Play and no active manual profile."); return; }
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var available = handles.Select(ProfilerRecorderHandle.GetDescription).ToArray();
            manualCapture = new ManualCapture {
                beganUtc = DateTime.UtcNow.ToString("O"), startZone = CurrentZone(),
                width = Screen.width, height = Screen.height, unity = Application.unityVersion,
                platform = Application.platform.ToString(), device = SystemInfo.graphicsDeviceName,
                cpu = SystemInfo.processorType, targetFrameRate = Application.targetFrameRate,
                vSync = QualitySettings.vSyncCount, metrics = new ManualMetric[ManualNames.Length] };
            manualRecorders = new ProfilerRecorder[ManualNames.Length];
            try
            {
                for (int i = 0; i < ManualNames.Length; i++)
                {
                    var row = new ManualMetric { name = ManualNames[i] }; manualCapture.metrics[i] = row;
                    var matches = available.Where(d => d.Name == row.name).ToArray();
                    if (matches.Length != 1) { row.failure = "Unavailable or ambiguous exact marker"; continue; }
                    var d = matches[0]; row.units = d.UnitType.ToString(); row.category = d.Category.Name;
                    var options = ProfilerRecorderOptions.Default;
                    if (row.name.StartsWith("COO.", StringComparison.Ordinal)) options |= ProfilerRecorderOptions.SumAllSamplesInFrame;
                    manualRecorders[i] = ProfilerRecorder.StartNew(d.Category, d.Name, ManualCapacity, options);
                    row.valid = manualRecorders[i].Valid;
                    if (!row.valid) row.failure = "Native recorder unavailable";
                }
                manualClock = System.Diagnostics.Stopwatch.StartNew();
                AssemblyReloadEvents.beforeAssemblyReload += AbortManualProfile;
                EditorApplication.playModeStateChanged += OnManualPlayChanged;
                Debug.Log("[PerfCapture] Manual gameplay profile started; play normally, then stop after 60–90 seconds.");
            }
            catch { DisposeManualProfile(); throw; }
        }
        [MenuItem("Caves Of Ooo/Diagnostics/Stop Manual Gameplay Profile")]
        static void StopManualGameplayProfile() => FinishManualProfile(true);
        static void AbortManualProfile() => FinishManualProfile(false);
        static void OnManualPlayChanged(PlayModeStateChange state)
        { if (state == PlayModeStateChange.ExitingPlayMode) AbortManualProfile(); }
        static string CurrentZone()
            => UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.InputHandler>()?.CurrentZone?.ZoneID;
        static void FinishManualProfile(bool requested)
        {
            if (manualRecorders == null) return;
            try
            {
                manualClock?.Stop(); manualCapture.seconds = manualClock?.Elapsed.TotalSeconds ?? 0;
                manualCapture.endedUtc = DateTime.UtcNow.ToString("O"); manualCapture.endZone = CurrentZone();
                manualCapture.stoppedNormally = requested;
                for (int i = 0; i < manualRecorders.Length; i++)
                {
                    if (!manualRecorders[i].Valid) continue;
                    manualRecorders[i].Stop();
                }
                for (int i = 0; i < manualRecorders.Length; i++)
                {
                    if (!manualRecorders[i].Valid) continue;
                    var row = manualCapture.metrics[i]; row.wrapped = manualRecorders[i].WrappedAround;
                    var samples = manualRecorders[i].ToArray(); row.count = samples.Length;
                    if (row.count == 0) { row.failure = "No completed samples; not zero cost"; continue; }
                    var values = samples.Select(sample => (double)sample.Value).OrderBy(v => v).ToArray();
                    row.mean = values.Average(); row.p50 = values[(int)((values.Length - 1) * .50)];
                    row.p95 = values[(int)((values.Length - 1) * .95)]; row.p99 = values[(int)((values.Length - 1) * .99)];
                    row.max = values[values.Length - 1];
                }
                string directory = Path.GetFullPath("Docs/Verification/GraphicsExecution/Profiles");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "manual-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json");
                File.WriteAllText(path, JsonUtility.ToJson(manualCapture, true));
                Debug.Log("[PerfCapture] Manual observation saved: " + path);
            }
            finally { DisposeManualProfile(); }
        }
        static void DisposeManualProfile()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= AbortManualProfile;
            EditorApplication.playModeStateChanged -= OnManualPlayChanged;
            if (manualRecorders != null) foreach (var recorder in manualRecorders) recorder.Dispose();
            manualRecorders = null; manualCapture = null; manualClock = null;
        }
        [Serializable] sealed class ManualCapture
        {
            public string beganUtc, endedUtc, startZone, endZone, unity, platform, device, cpu;
            public string scope = "Editor manual gameplay observation. Exact native marker units, overlapping markers not summed. Main Thread includes Editor scheduling; not packaged performance. Missing/empty markers are unknown. Wrapped buffers are truncated. No gameplay or renderer settings changed.";
            public int width, height, targetFrameRate, vSync;
            public double seconds;
            public bool stoppedNormally;
            public ManualMetric[] metrics;
        }
        [Serializable] sealed class ManualMetric
        {
            public string name, category, units, failure;
            public bool valid, wrapped;
            public int count;
            public double mean, p50, p95, p99, max;
        }

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
