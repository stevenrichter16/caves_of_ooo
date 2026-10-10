using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckDiagnosticAdversarialTests
    {
        static void ReleasePolicy()
        {
            Diag.ResetAll(); var p = typeof(Diag).GetProperty("DetailedCaptureEnabled");
            Assert.NotNull(p); p.SetValue(null, false);
        }
        [TearDown] public void Reset() { Diag.ResetAll(); PerformanceDiagnostics.ResetAll(); }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(100)]
        public void RetainedSnapshotSurvivesUnreadFramesAndDiagnosticReset(int skipped)
        {
            PerformanceDiagnostics.ResetAll(); PerformanceDiagnostics.BeginFrame(71, true, true);
            PerformanceDiagnostics.RecordMarkDirty("retained"); PerformanceDiagnostics.EndFrame(8);
            var retained = PerformanceDiagnostics.LastCompletedFrameSnapshot;
            for (int i = 0; i < skipped; i++) { PerformanceDiagnostics.BeginFrame(i, false, false); PerformanceDiagnostics.EndFrame(i); }
            PerformanceDiagnostics.ResetAll(); Assert.AreEqual(71, retained.TurnTick); Assert.True(retained.Paused);
            Assert.AreEqual(8, retained.ZoneRendererLateUpdateMs); Assert.AreEqual(1, retained.MarkDirtyBySource["retained"]);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.TurnTick);
        }
        [TestCase("worldgen", "BuilderFailed")][TestCase("worldgen", "PlacementRejected")]
        [TestCase("worldgen", "RootRefused")][TestCase("gasbench", "ProbeFailure")]
        [TestCase("questbench", "TransitionError")][TestCase("gas", "ApplyVetoed")]
        [TestCase("crop", "MatureBlocked")][TestCase("event", "StackPayloadMismatch")]
        [TestCase("worldgen", "VoxelMeshUnmapped")]
        public void ReleaseRejectionEvidenceIsRetainedAndExplicitDisableStillWins(string category, string kind)
        {
            ReleasePolicy(); Diag.Record(category, kind, payload: new { reason = "test" });
            Assert.AreEqual(kind, Diag.Snapshot().Single().Kind);
            Diag.SetChannel(category, false); Diag.Record(category, kind); Assert.AreEqual(1, Diag.Snapshot().Count);
        }
        [TestCase("crop", "CropTimeReconciled")][TestCase("worldgen", "PopulationRolled")]
        [TestCase("gas", "Spread")][TestCase("tile", "PropagationWave")]
        public void DetailedChannelOptInAndDisableAreReversibleWithoutChangingGlobalPolicy(string category, string kind)
        {
            ReleasePolicy(); Diag.Record(category, kind); Assert.IsEmpty(Diag.Snapshot());
            Diag.SetChannel(category, true); Diag.Record(category, kind); Assert.AreEqual(1, Diag.Snapshot().Count);
            Diag.SetChannel(category, false); Diag.Record(category, kind); Assert.AreEqual(1, Diag.Snapshot().Count);
        }
        [TestCase(SpellFxMode.Full, true)][TestCase(SpellFxMode.Reduced, true)]
        [TestCase(SpellFxMode.Off, true)][TestCase(SpellFxMode.Full, false)]
        public void HiddenFramesDoNotPaintAndResumePaintsOnePassWithoutReplayingExpiredReadouts(SpellFxMode mode, bool sprites)
        {
            var previous = SpellFxSettings.Mode; AsciiFxBus.Clear(); SpellFxBus.Clear(); SpellFxSettings.Mode = mode;
            var root = new GameObject("Adversarial FX work", typeof(Grid)); var child = new GameObject("map", typeof(Tilemap), typeof(TilemapRenderer)); child.transform.SetParent(root.transform);
            var ascii = new AsciiFxRenderer(child.GetComponent<Tilemap>());
            try
            {
                using (var world = new WorldFxCoordinator(ascii, root.transform))
                {
                    var property = typeof(WorldFxCoordinator).GetProperty("BackendUpdatePassCount"); Assert.NotNull(property);
                    var zone = new Zone("FxAdversarial"); foreach (var cell in zone.Cells) cell.IsVisible = cell.Explored = true; world.SetZone(zone);
                    world.Update(0, sprites); int before = (int)property.GetValue(world);
                    AsciiFxBus.EmitParticle(zone, 4, 4, '7', "&W", .1f); world.Update(.1f, sprites, false);
                    Assert.AreEqual(before, (int)property.GetValue(world)); Assert.Zero(ascii.ActiveParticleCount); Assert.Zero(AsciiFxBus.PendingCount);
                    world.Update(.1f, sprites, true); Assert.AreEqual(before + 1, (int)property.GetValue(world)); Assert.Zero(ascii.ActiveParticleCount);
                }
            }
            finally { SpellFxSettings.Mode = previous; UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
