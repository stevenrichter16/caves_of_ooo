using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    internal static class MorrowfastTestWorld
    {
        internal static readonly (int x, int y)[] Cardinal = { (0, -1), (1, 0), (0, 1), (-1, 0) };
        internal static EntityFactory Factory()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            return factory;
        }
        internal static Entity Actor(EntityFactory factory, Zone zone, int x = 40, int y = 24)
        {
            var actor = factory.CreateEntity("Player");
            Assert.IsTrue(zone.AddEntity(actor, x, y), "The fixture player must really enter its zone.");
            return actor;
        }
        internal static HashSet<(int x, int y)> Flood(Zone zone, Entity actor, int x = 40, int y = 24)
        {
            var result = new HashSet<(int, int)>(); var queue = new Queue<(int x, int y)>();
            if (zone.GetCell(x, y)?.BlocksMovement(actor) != false) return result;
            result.Add((x, y)); queue.Enqueue((x, y));
            while (queue.Count != 0)
            {
                var p = queue.Dequeue();
                foreach (var d in Cardinal)
                {
                    var q = (p.x + d.x, p.y + d.y);
                    if (zone.GetCell(q.Item1, q.Item2)?.BlocksMovement(actor) == false && result.Add(q)) queue.Enqueue(q);
                }
            }
            return result;
        }
        internal static (int x, int y) Approach(Zone zone, Entity actor, Entity owner)
        {
            Assert.IsNotNull(owner, "A real semantic owner is required.");
            var at = zone.GetEntityPosition(owner); var reachable = Flood(zone, actor);
            var contact = new List<(int x, int y)> { at };
            var spec = MorrowfastSceneDefinition.Load().owners.FirstOrDefault(o => ReferenceEquals(MorrowfastSceneRuntime.FindOwner(zone, o.id), owner));
            if (spec != null) contact.AddRange(spec.footprint.Select(c => (c.x + at.x - spec.anchorX, c.y + at.y - spec.anchorY)));
            foreach (var occupied in contact) foreach (var d in Cardinal)
                if (reachable.Contains((occupied.x + d.x, occupied.y + d.y))) return (occupied.x + d.x, occupied.y + d.y);
            Assert.Fail("No cardinal reachable approach to the actual owner/footprint: " + owner.GetDisplayName() + " at " + at);
            return (-1, -1);
        }
        internal static Entity Clearable(Zone zone, Entity actor)
        {
            var owner = MorrowfastSceneRuntime.FindOwner(zone, "provisioners-stall");
            Assert.IsNotNull(owner, "At least one authored loose prop must expose a real clear action; this is not a vacuous rejection test.");
            var p = Approach(zone, actor, owner); zone.MoveEntity(actor, p.x, p.y);
            Assert.IsTrue(MorrowfastSceneRuntime.WithinOwnerReach(actor, owner, zone), "The fixture must be within the runtime's actual interaction reach.");
            Assert.IsTrue(WorldInteractionSystem.GatherActions(owner, actor).Any(a => a.Command == MorrowfastPropPart.ClearCommand));
            return owner;
        }
        internal static void OpenAllDoors(Zone zone, Entity actor)
        {
            foreach (var b in MorrowfastSceneDefinition.Load().buildings)
            {
                var door = MorrowfastSceneRuntime.FindOwner(zone, b.doorId); var p = Approach(zone, actor, door);
                zone.MoveEntity(actor, p.x, p.y);
                Assert.IsTrue(door.GetPart<MorrowfastDoorPart>().TrySetOpen(actor, zone, true), b.doorId);
            }
            zone.MoveEntity(actor, 40, 24);
        }
        internal static GameSessionState RoundTrip(OverworldZoneManager manager, Entity actor, EntityFactory factory)
        {
            var turns = new TurnManager(); turns.AddEntity(actor);
            var state = GameSessionState.Capture("morrowfast-editmode", "Morrowfast isolated fixture", manager, turns, actor);
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                return GameSessionState.Load(new SaveReader(stream, factory));
            }
        }
    }

    public sealed class MorrowfastSceneIntegrationTests
    {
        private EntityFactory factory;
        [SetUp] public void Setup() => factory = MorrowfastTestWorld.Factory();
        private Zone Fresh() => new OverworldZoneManager(factory, 64).GetZone(MorrowfastSceneRuntime.ZoneID);

        [Test] public void DefinitionCoversEverySourceOwnerAndEveryNativeCell()
        {
            var d = MorrowfastSceneDefinition.Load(); Assert.IsNotNull(d); Assert.DoesNotThrow(d.Validate);
            Assert.AreEqual(71, d.owners.Length); Assert.AreEqual(5, d.buildings.Length); Assert.AreEqual(2000, d.cells.Length);
            Assert.AreEqual(2000, d.cells.Select(c => (c.x, c.y)).Distinct().Count());
            Assert.AreSame(d, MorrowfastSceneDefinition.Load(), "Resource parsing must not repeat on every renderer lookup.");
        }
        [TestCase(64)] [TestCase(65)] [TestCase(1729)]
        public void NormalWorldGenerationReservesMorrowfastAndPreservesFellingAndOlderdeep(int seed)
        {
            var m = new OverworldZoneManager(factory, seed);
            Assert.AreEqual("Morrowfast", m.WorldMap.GetPOI(3, 6)?.Name);
            Assert.AreEqual("Morrowfast", m.WorldMap.GetPOI(3, 6)?.Profile);
            Assert.AreEqual(POIType.FellingSite, m.WorldMap.GetPOI(3, 5)?.Type);
            Assert.AreEqual("Olderdeep", m.WorldMap.GetPOI(4, 6)?.Name);
            Assert.IsTrue(MorrowfastSceneRuntime.IsActive(m.GetZone(MorrowfastSceneRuntime.ZoneID)));
        }
        [Test] public void EverySourceOwnerHasOneLiveIdentityAndAnExamineAction()
        {
            var z = Fresh(); var ids = new HashSet<string>();
            foreach (var o in MorrowfastSceneDefinition.Load().owners)
            {
                var e = MorrowfastSceneRuntime.FindOwner(z, o.id); Assert.IsNotNull(e, o.id);
                Assert.IsNotNull(z.GetEntityCell(e), o.id); Assert.IsTrue(ids.Add(e.ID), "Two art owners alias one entity: " + o.id);
                Assert.IsTrue(WorldInteractionSystem.GatherActions(e).Any(a => a.Command == "Examine"), o.id);
            }
        }
        [Test] public void NormalFellingSouthEdgeAndMorrowfastNorthEdgeAreReciprocal()
        {
            var m = new OverworldZoneManager(factory, 64); var felling = m.GetZone(FellingSiteBuilder.ZoneID);
            var actor = MorrowfastTestWorld.Actor(factory, felling, 40, 24);
            var south = ZoneTransitionSystem.TransitionPlayer(actor, felling, TransitionDirection.South, 40, 24, m, m.WorldMap);
            Assert.IsTrue(south.Success, south.ErrorReason); Assert.IsTrue(MorrowfastSceneRuntime.IsActive(south.NewZone));
            Assert.AreEqual((40, 0), south.NewZone.GetEntityPosition(actor));
            var north = ZoneTransitionSystem.TransitionPlayer(actor, south.NewZone, TransitionDirection.North, 40, 0, m, m.WorldMap);
            Assert.IsTrue(north.Success, north.ErrorReason); Assert.AreEqual(FellingSiteBuilder.ZoneID, north.NewZone.ZoneID);
            Assert.AreEqual((40, 24), north.NewZone.GetEntityPosition(actor)); Assert.IsTrue(FellingSceneRuntime.IsActive(north.NewZone));
        }
        [Test] public void WorldMapDescentUsesTheAuthoredSettlementAndReturnsNormally()
        {
            var m = new OverworldZoneManager(factory, 64); var map = m.GetZone(WorldMap.WorldMapZoneID);
            var p = WorldMap.WorldCellToZoneCell(3, 6); var actor = MorrowfastTestWorld.Actor(factory, map, p.zoneX, p.zoneY);
            var down = WorldMapTraversal.Descend(actor, map, m);
            Assert.IsTrue(down.Success); Assert.IsTrue(MorrowfastSceneRuntime.IsActive(down.NewZone));
            var up = WorldMapTraversal.Ascend(actor, down.NewZone, m); Assert.IsTrue(up.Success);
            Assert.IsTrue(WorldMap.IsWorldMapZoneID(up.NewZone.ZoneID)); Assert.AreEqual((p.zoneX, p.zoneY), up.NewZone.GetEntityPosition(actor));
        }
        [Test] public void AllFiveDoorsOpenIntoReachableRoomsAndEveryOwnerHasAnApproach()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); MorrowfastTestWorld.OpenAllDoors(z, actor);
            var seen = MorrowfastTestWorld.Flood(z, actor); Assert.IsTrue(seen.Contains((40, 0))); Assert.IsTrue(seen.Contains((40, 24)));
            foreach (var b in MorrowfastSceneDefinition.Load().buildings)
            {
                Assert.IsTrue(MorrowfastSceneRuntime.IsDoorOpen(z, b.doorId));
                Assert.IsTrue(b.interior.Any(c => seen.Contains((c.x, c.y))), "No reachable interior: " + b.id);
            }
            foreach (var o in MorrowfastSceneDefinition.Load().owners) MorrowfastTestWorld.Approach(z, actor, MorrowfastSceneRuntime.FindOwner(z, o.id));
        }
        [Test] public void DoorOpenAndCloseChangeItsRealCollision()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); var b = MorrowfastSceneDefinition.Load().buildings[0];
            var owner = MorrowfastSceneRuntime.FindOwner(z, b.doorId); var at = z.GetEntityPosition(owner); var p = MorrowfastTestWorld.Approach(z, actor, owner);
            z.MoveEntity(actor, p.x, p.y); Assert.IsTrue(z.GetCell(at.x, at.y).BlocksMovement(actor));
            var door = owner.GetPart<MorrowfastDoorPart>(); Assert.IsTrue(door.TrySetOpen(actor, z, true)); Assert.IsTrue(door.IsOpen);
            Assert.IsFalse(z.GetCell(at.x, at.y).BlocksMovement(actor)); Assert.IsTrue(door.TrySetOpen(actor, z, false));
            Assert.IsFalse(door.IsOpen); Assert.IsTrue(z.GetCell(at.x, at.y).BlocksMovement(actor));
        }
        [Test] public void NativeClearRemovesOneOwnerAndRecordsItsStateWithoutGrantingItems()
        {
            var z = Fresh(); var actor = MorrowfastTestWorld.Actor(factory, z); var e = MorrowfastTestWorld.Clearable(z, actor);
            string id = e.GetPart<MorrowfastPropPart>().ComponentId; int count = actor.GetPart<InventoryPart>().Objects.Count;
            var eventData = GameEvent.New("InventoryAction"); eventData.SetParameter("Command", MorrowfastPropPart.ClearCommand);
            eventData.SetParameter("Actor", (object)actor); eventData.SetParameter("Zone", (object)z); e.FireEventAndRelease(eventData);
            Assert.IsNull(z.GetEntityCell(e)); Assert.IsFalse(MorrowfastSceneRuntime.IsPresent(z, id));
            Assert.AreEqual(count, actor.GetPart<InventoryPart>().Objects.Count, "Clearing scenery is not a duplicate loot generator.");
        }
        [Test] public void RealSessionRoundTripRetainsOpenDoorRemovedOwnerAndForeignItemIdentity()
        {
            var m = new OverworldZoneManager(factory, 64); var z = m.GetZone(MorrowfastSceneRuntime.ZoneID);
            var actor = MorrowfastTestWorld.Actor(factory, z); MorrowfastTestWorld.OpenAllDoors(z, actor);
            var e = MorrowfastTestWorld.Clearable(z, actor); string id = e.GetPart<MorrowfastPropPart>().ComponentId;
            Assert.IsTrue(e.GetPart<MorrowfastPropPart>().TryRemove(actor, z));
            var item = factory.CreateEntity("Tepuibone"); item.SetIntProperty("MorrowfastForeignMarker", 37); Assert.IsTrue(z.AddEntity(item, 40, 24));
            m.SetActiveZone(z); var loaded = MorrowfastTestWorld.RoundTrip(m, actor, factory); var restored = loaded.ZoneManager.ActiveZone;
            Assert.IsTrue(MorrowfastSceneRuntime.IsActive(restored)); Assert.IsFalse(MorrowfastSceneRuntime.IsPresent(restored, id));
            Assert.IsTrue(MorrowfastSceneDefinition.Load().buildings.All(b => MorrowfastSceneRuntime.IsDoorOpen(restored, b.doorId)));
            Assert.IsTrue(restored.GetAllEntities().Any(x => x.ID == item.ID && x.GetIntProperty("MorrowfastForeignMarker") == 37));
            Assert.IsNotNull(restored.GetEntityCell(loaded.Player));
        }
        [Test] public void OlderSavedMapWithoutMorrowfastAcquiresTheNewAuthoredPoiOnRealLoad()
        {
            var manager = new OverworldZoneManager(factory, 64); manager.WorldMap.SetPOI(3, 6, null);
            var zone = manager.GetZone("Overworld.10.10.0"); var actor = MorrowfastTestWorld.Actor(factory, zone);
            manager.SetActiveZone(zone); var loaded = MorrowfastTestWorld.RoundTrip(manager, actor, factory);
            Assert.AreEqual("Morrowfast", loaded.ZoneManager.WorldMap.GetPOI(3, 6)?.Name);
            Assert.AreEqual("Morrowfast", loaded.ZoneManager.WorldMap.GetPOI(3, 6)?.Profile);
            Assert.AreEqual("Olderdeep", loaded.ZoneManager.WorldMap.GetPOI(4, 6)?.Name);
            Assert.IsTrue(MorrowfastSceneRuntime.IsActive(loaded.ZoneManager.GetZone(MorrowfastSceneRuntime.ZoneID)));
        }
    }
}
