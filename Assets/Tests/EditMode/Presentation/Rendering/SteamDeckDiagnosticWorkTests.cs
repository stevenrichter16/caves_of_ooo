using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckDiagnosticWorkTests
    {
        static void Detailed(bool enabled)
        {
            var property = typeof(Diag).GetProperty("DetailedCaptureEnabled", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(property, "An explicit release-safe detailed capture policy is required."); property.SetValue(null, enabled);
        }
        static int Snapshots()
        {
            var property = typeof(PerformanceDiagnostics).GetProperty("SnapshotMaterializationCount");
            Assert.NotNull(property, "Count actual detached snapshot allocations, not just EndFrame calls."); return (int)property.GetValue(null);
        }
        [TearDown] public void Restore() { Diag.ResetAll(); PerformanceDiagnostics.ResetAll(); }
        [Test] public void UnreadCompletedFramesAllocateNoDetachedSnapshotAndRetainedReadStaysImmutable()
        {
            PerformanceDiagnostics.ResetAll(); int before = Snapshots();
            for (int i = 0; i < 20; i++) { PerformanceDiagnostics.BeginFrame(i, false, false); PerformanceDiagnostics.RecordMarkDirty("A"); PerformanceDiagnostics.EndFrame(1); }
            Assert.AreEqual(before, Snapshots()); var retained = PerformanceDiagnostics.LastCompletedFrameSnapshot;
            Assert.AreEqual(before + 1, Snapshots()); Assert.AreEqual(19, retained.TurnTick);
            Assert.AreSame(retained, PerformanceDiagnostics.LastCompletedFrameSnapshot); Assert.AreEqual(before + 1, Snapshots());
            PerformanceDiagnostics.BeginFrame(99, true, true); PerformanceDiagnostics.RecordMarkDirty("B");
            Assert.AreSame(retained, PerformanceDiagnostics.LastCompletedFrameSnapshot, "An in-progress frame cannot replace the last completed observation.");
            PerformanceDiagnostics.EndFrame(3); Assert.AreEqual(before + 1, Snapshots());
            var next = PerformanceDiagnostics.LastCompletedFrameSnapshot; Assert.AreNotSame(retained, next);
            Assert.AreEqual(19, retained.TurnTick); Assert.True(retained.MarkDirtyBySource.ContainsKey("A")); Assert.False(retained.MarkDirtyBySource.ContainsKey("B"));
            Assert.AreEqual(99, next.TurnTick); Assert.False(next.MarkDirtyBySource.ContainsKey("A")); Assert.True(next.MarkDirtyBySource.ContainsKey("B"));
        }
        sealed class Payload { public int Reads; public int Value { get { Reads++; return 42; } } }
        [TestCase("crop", "CropTimeReconciled")][TestCase("worldgen", "PopulationRolled")][TestCase("tile", "PropagationWave")]
        public void RoutineReleaseDetailDoesNotSerializeButExplicitCaptureDoes(string category, string kind)
        {
            Diag.ResetAll(); Detailed(false); var payload = new Payload();
            Diag.Record(category, kind, payload: payload); Assert.Zero(payload.Reads); Assert.IsEmpty(Diag.Snapshot());
            Detailed(true); Diag.Record(category, kind, payload: payload); Assert.Greater(payload.Reads, 0); Assert.AreEqual(1, Diag.Snapshot().Count);
        }
        [TestCase("crop", "CropTimeRejected")][TestCase("worldgen", "PopulationPlacementRejected")]
        [TestCase("event", "WeaponCraftingRollbackFailed")][TestCase("event", "ExplorationActionCompleted")]
        [TestCase("crop", "CropPlanted")][TestCase("damage", "DamageDealt")][TestCase("turn", "Begin")]
        public void ReleasePolicyRetainsFailureAndActionEvidenceAtRecordTime(string category, string kind)
        {
            Diag.ResetAll(); Detailed(false); var payload = new Dictionary<string, int> { ["value"] = 3 };
            Diag.Record(category, kind, payload: payload); payload["value"] = 9;
            var entries = Diag.Snapshot(); Assert.AreEqual(1, entries.Count); StringAssert.Contains(":3", entries[0].PayloadJson); StringAssert.DoesNotContain(":9", entries[0].PayloadJson);
            Diag.SetChannel(category, false); Diag.Record(category, kind); Assert.AreEqual(1, Diag.Snapshot().Count, "Explicit channel disable remains authoritative.");
        }
        [Test] public void ExplicitChannelOptInRetainsDetailedEvidenceEvenUnderReleasePolicy()
        {
            Diag.ResetAll(); Detailed(false); Diag.SetChannel("worldgen", true);
            Diag.Record("worldgen", "PopulationRolled", payload: new { count = 4 }); Assert.AreEqual(1, Diag.Snapshot().Count);
        }
        static int PaintPasses(WorldFxCoordinator world)
        {
            var property = typeof(WorldFxCoordinator).GetProperty("BackendUpdatePassCount");
            Assert.NotNull(property, "Count real three-backend update passes."); return (int)property.GetValue(world);
        }
        [Test] public void EmptyFxQueuesPaintOnceButNewReadoutPaintsImmediatelyAndTimesOutNormally()
        {
            AsciiFxBus.Clear(); SpellFxBus.Clear(); var prior = SpellFxSettings.Mode; SpellFxSettings.Mode = SpellFxMode.Full;
            var root = new GameObject("Deck FX work", typeof(Grid)); var map = new GameObject("Effects", typeof(Tilemap), typeof(TilemapRenderer)); map.transform.SetParent(root.transform);
            var tiles = map.GetComponent<Tilemap>(); var ascii = new AsciiFxRenderer(tiles);
            using (var world = new WorldFxCoordinator(ascii, root.transform))
            {
                try
                {
                    var zone = new Zone("DeckFxWork"); foreach (var cell in zone.Cells) cell.IsVisible = cell.Explored = true; world.SetZone(zone);
                    int before = PaintPasses(world); world.Update(.01f); Assert.AreEqual(before + 1, PaintPasses(world));
                    AsciiFxBus.EmitParticle(zone, 4, 4, '7', "&W", .2f); before = PaintPasses(world); world.Update(.01f);
                    Assert.AreEqual(before + 2, PaintPasses(world)); Assert.NotNull(tiles.GetTile(new Vector3Int(4, Zone.Height - 5, 0)));
                    before = PaintPasses(world); world.Update(.01f); Assert.AreEqual(before + 1, PaintPasses(world)); Assert.AreEqual(1, ascii.ActiveParticleCount);
                    world.Update(1f); Assert.Zero(ascii.ActiveParticleCount); Assert.IsNull(tiles.GetTile(new Vector3Int(4, Zone.Height - 5, 0)));
                }
                finally { SpellFxSettings.Mode = prior; UnityEngine.Object.DestroyImmediate(root); }
            }
        }
    }
}
