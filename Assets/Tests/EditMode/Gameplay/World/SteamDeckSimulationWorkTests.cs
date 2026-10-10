using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class SteamDeckSimulationWorkTests
    {
        internal static IReadOnlyList<Entity> Indexed<T>(Zone zone) where T : Part
        {
            var method = typeof(Zone).GetMethod("GetReadOnlyEntitiesWithPart");
            Assert.NotNull(method, "Relevant owners need a maintained part index.");
            return (IReadOnlyList<Entity>)method.MakeGenericMethod(typeof(T)).Invoke(zone, null);
        }
        internal sealed class DerivedThermal : ThermalPart { }
        internal sealed class TurnProbe : Part
        {
            public override string Name => "DeckTurnProbe";
            public int Turns;
            public Action OnTurn;
            public override bool HandleEvent(GameEvent e)
            { if (e.ID == "EndTurn") { Turns++; OnTurn?.Invoke(); } return true; }
        }
        [Test] public void PartIndexTracksEntryMovementAttachmentRemovalAndInheritance()
        {
            var zone = new Zone(); var e = new Entity();
            var view = Indexed<ThermalPart>(zone); Assert.Zero(view.Count);
            zone.AddEntity(e, 2, 3); Assert.Zero(view.Count);
            var first = new DerivedThermal(); e.AddPart(first);
            Assert.AreSame(view, Indexed<ThermalPart>(zone)); CollectionAssert.AreEqual(new[] { e }, view);
            var second = new ThermalPart(); e.AddPart(second); zone.MoveEntity(e, 3, 3);
            Assert.AreEqual(1, view.Count); e.RemovePart(first); Assert.AreEqual(1, view.Count);
            e.RemovePart(second); Assert.Zero(view.Count); e.AddPart(first); zone.RemoveEntity(e);
            Assert.Zero(view.Count); zone.AddEntity(e, 5, 5); Assert.AreEqual(1, view.Count);
        }
        [Test] public void RebuildDiscardsHydrationStaleMembership()
        {
            var zone = new Zone(); var e = new Entity(); e.AddPart(new ThermalPart()); zone.AddEntity(e, 3, 3);
            Assert.AreEqual(1, Indexed<ThermalPart>(zone).Count);
            e.Parts.Clear(); var source = new TileStateSourcePart { ParentEntity = e }; e.Parts.Add(source);
            zone.RebuildEntityCellsFromCells();
            Assert.Zero(Indexed<ThermalPart>(zone).Count); CollectionAssert.AreEqual(new[] { e }, Indexed<TileStateSourcePart>(zone));
        }
        [Test] public void MaterialDiscoverySkipsInertOwnersButReadsDirectTemperatureChanges()
        {
            var zone = new Zone();
            for (int i = 0; i < 300; i++) zone.AddEntity(new Entity(), i % Zone.Width, i / Zone.Width);
            var e = new Entity(); var thermal = new ThermalPart(); var probe = new TurnProbe();
            e.AddPart(thermal); e.AddPart(probe); zone.AddEntity(e, 4, 4);
            MaterialSimSystem.TickMaterialEntities(zone); Assert.Zero(probe.Turns);
            thermal.Temperature = 100; MaterialSimSystem.TickMaterialEntities(zone);
            Assert.AreEqual(1, probe.Turns); Assert.Less(thermal.Temperature, 100);
            var count = typeof(MaterialSimSystem).GetProperty("LastCandidateCount"); Assert.NotNull(count);
            Assert.AreEqual(1, count.GetValue(null), "Only potentially active material owners should be inspected.");
            e.SetTag("Creature"); thermal.Temperature = 100; MaterialSimSystem.TickMaterialEntities(zone);
            Assert.AreEqual(1, probe.Turns); Assert.AreEqual(100, thermal.Temperature);
        }
        [Test] public void MaterialSnapshotPreservesCellOrderAndDoesNotTickNewOwners()
        {
            var zone = new Zone(); var order = new List<string>();
            Entity Make(string id, int x, int y)
            {
                var e = new Entity { ID = id }; e.AddPart(new LifespanPart { TurnsRemaining = 9 });
                e.AddPart(new TurnProbe { OnTurn = () => order.Add(id) }); zone.AddEntity(e, x, y); return e;
            }
            Make("last", 9, 0); var first = Make("first", 1, 8); Make("second", 1, 8); Make("third", 1, 9);
            first.GetPart<TurnProbe>().OnTurn = () => { order.Add("first"); Make("new", 0, 0); };
            MaterialSimSystem.TickMaterialEntities(zone);
            CollectionAssert.AreEqual(new[] { "first", "second", "third", "last" }, order);
        }
        [Test] public void TerrainSourceAttachmentAndRemovalUpdateTheNextSeed()
        {
            var zone = new Zone(); var e = new Entity(); zone.AddEntity(e, 3, 4);
            Indexed<TileStateSourcePart>(zone);
            ZoneTileStateSystem.SeedTerrainSources(zone); Assert.False(zone.TileState.HasCoating(3, 4, "water"));
            var source = new TileStateSourcePart { Coating = "water", CoatingTurns = 2 }; e.AddPart(source);
            ZoneTileStateSystem.SeedTerrainSources(zone); Assert.True(zone.TileState.HasCoating(3, 4, "water"));
            e.RemovePart(source); zone.TileState.Tick(); zone.TileState.Tick();
            ZoneTileStateSystem.SeedTerrainSources(zone); Assert.False(zone.TileState.HasCoating(3, 4, "water"));
        }
        [Test] public void PlayerMovementPublishesGeometryCellsAsWellAsVisibility()
        {
            var oldCell = ZoneRenderHooks.CellDirtyCallback; var oldFull = ZoneRenderHooks.FullDirtyCallback;
            try
            {
                var cells = new HashSet<int>(); var full = new List<string>();
                ZoneRenderHooks.CellDirtyCallback = (x, y, source) => cells.Add(y * Zone.Width + x);
                ZoneRenderHooks.FullDirtyCallback = source => full.Add(source);
                var e = new Entity(); e.SetTag("Player");
                typeof(MovementSystem).GetMethod("DirtyForMove", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { e, 1, 2, 2, 2 });
                CollectionAssert.AreEquivalent(new[] { 161, 162 }, cells); CollectionAssert.AreEqual(new[] { "Move.Player" }, full);
                cells.Clear(); full.Clear(); e.Tags.Remove("Player");
                typeof(MovementSystem).GetMethod("DirtyForMove", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { e, 1, 2, 2, 2 });
                Assert.AreEqual(2, cells.Count); Assert.Zero(full.Count);
            }
            finally { ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; }
        }
        [Test] public void FullRedrawRetainsGeometryHintsAndUnknownSourcesStayConservative()
        {
            var go = new GameObject("Deck dirty state");
            try
            {
                var renderer = go.AddComponent<ZoneRenderer>();
                typeof(ZoneRenderer).GetProperty("CurrentZone").SetValue(renderer, new Zone());
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                renderer.MarkDirty("Move.Player"); renderer.MarkCellDirty(4, 5, "damage");
                var cells = (HashSet<int>)typeof(ZoneRenderer).GetField("_dirtyCells", flags).GetValue(renderer);
                Assert.True(cells.Contains(404), "A visibility repaint must retain intervening geometry changes.");
                var field = typeof(ZoneRenderer).GetField("_nativeGeometryFullDirty", flags); Assert.NotNull(field);
                field.SetValue(renderer, false); renderer.MarkDirty("Move.Player"); Assert.False((bool)field.GetValue(renderer));
                renderer.MarkDirty("unclassified-world-mutation"); renderer.MarkDirty("Move.Player"); Assert.True((bool)field.GetValue(renderer));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
