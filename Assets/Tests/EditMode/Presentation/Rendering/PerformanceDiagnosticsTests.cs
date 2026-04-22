using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class PerformanceDiagnosticsTests
    {
        [SetUp]
        public void SetUp()
        {
            PerformanceDiagnostics.ResetAll();
        }

        [Test]
        public void FrameCounters_ResetBetweenFrames_AndPersistLastSnapshot()
        {
            PerformanceDiagnostics.BeginFrame(turnTick: 42, paused: false, rendererDirty: true);
            PerformanceDiagnostics.RecordZoneRedraw(120);
            PerformanceDiagnostics.RecordTilemapClear(2);
            PerformanceDiagnostics.RecordMarkDirty("Input");
            PerformanceDiagnostics.RecordAsciiFxCounts(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            PerformanceDiagnostics.EndFrame(12.5d);

            Assert.AreEqual(42, PerformanceDiagnostics.LastCompletedFrameSnapshot.TurnTick);
            Assert.AreEqual(1, PerformanceDiagnostics.LastCompletedFrameSnapshot.FullZoneRedrawCount);
            Assert.AreEqual(120, PerformanceDiagnostics.LastCompletedFrameSnapshot.CellsRendered);
            Assert.AreEqual(2, PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);
            Assert.AreEqual(1, PerformanceDiagnostics.LastCompletedFrameSnapshot.MarkDirtyCount);
            Assert.AreEqual(10, PerformanceDiagnostics.LastCompletedFrameSnapshot.ActiveDustMotes);

            PerformanceDiagnostics.BeginFrame(turnTick: 43, paused: true, rendererDirty: false);
            PerformanceDiagnostics.EndFrame(1.5d);

            Assert.AreEqual(43, PerformanceDiagnostics.LastCompletedFrameSnapshot.TurnTick);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.FullZoneRedrawCount);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.MarkDirtyCount);
        }

        [Test]
        public void InventorySession_TracksRenderCountPerOpenInteraction()
        {
            PerformanceDiagnostics.BeginInventorySession();
            PerformanceDiagnostics.RecordInventoryRender();
            PerformanceDiagnostics.RecordInventoryRender();

            Assert.AreEqual(2, PerformanceDiagnostics.CurrentInventorySessionRenderCount);

            PerformanceDiagnostics.EndInventorySession();

            Assert.AreEqual(2, PerformanceDiagnostics.LastCompletedInventorySessionRenderCount);
            Assert.AreEqual(0, PerformanceDiagnostics.CurrentInventorySessionRenderCount);
        }

        [Test]
        public void StartupPhaseMeasurement_RecordsDurationsAndTotal()
        {
            PerformanceDiagnostics.MeasureStartupPhase(
                "LoadBlueprints",
                PerformanceMarkers.Bootstrap.LoadBlueprints,
                () => { });

            Assert.AreEqual(1, PerformanceDiagnostics.StartupPhases.Count);
            Assert.AreEqual("LoadBlueprints", PerformanceDiagnostics.StartupPhases[0].PhaseName);
            Assert.GreaterOrEqual(PerformanceDiagnostics.StartupPhases[0].DurationMs, 0d);

            PerformanceDiagnostics.RecordStartupTotal(25.0d);
            Assert.AreEqual(25.0d, PerformanceDiagnostics.StartupTotalMs);
        }
    }
}
