using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastFaunaTests
    {
        private EntityFactory factory; private Zone zone;
        [SetUp] public void Setup() { factory = MorrowfastTestWorld.Factory(); zone = new OverworldZoneManager(factory, 64).GetZone(MorrowfastSceneRuntime.ZoneID); }
        private Entity Fauna(string id)
        {
            var e = MorrowfastSceneRuntime.FindOwner(zone, id); Assert.NotNull(e);
            Assert.NotNull(e.GetPart<MorrowfastFaunaPart>(), "The authored native fauna must receive its home-area part."); return e;
        }
        private bool Before(Entity owner, Cell target, Entity actor = null, Cell source = null)
        {
            var ev = GameEvent.New("BeforeMove"); ev.SetParameter("Actor", (object)(actor ?? owner));
            ev.SetParameter("SourceCell", (object)(source ?? zone.GetEntityCell(owner))); ev.SetParameter("TargetCell", (object)target);
            bool allowed = owner.GetPart<MorrowfastFaunaPart>().HandleEvent(ev); ev.Release(); return allowed;
        }
        [TestCase("western-bank-frog", 21, 27, 15, 23)]
        [TestCase("southern-tortoise", 35, 41, 21, 24)]
        public void BoundariesRejectAllFourOutsideEdges(string id, int minX, int maxX, int minY, int maxY)
        {
            var e = Fauna(id);
            Assert.IsFalse(Before(e, zone.GetCell(minX - 1, minY)));
            Assert.IsFalse(Before(e, zone.GetCell(maxX + 1, maxY)));
            Assert.IsFalse(Before(e, zone.GetCell(minX, minY - 1)));
            Assert.IsFalse(Before(e, zone.GetCell(maxX, maxY + 1)));
        }
        [TestCase("western-bank-frog")][TestCase("southern-tortoise")]
        public void EveryHouseInteriorDoorAndThresholdIsRejectedEvenWhenOpen(string id)
        {
            var e = Fauna(id); var d = MorrowfastSceneDefinition.Load();
            var player = MorrowfastTestWorld.Actor(factory, zone); MorrowfastTestWorld.OpenAllDoors(zone, player);
            foreach (var b in d.buildings)
            {
                foreach (var c in b.interior) Assert.IsFalse(Before(e, zone.GetCell(c.x, c.y)), b.id + " interior");
                var door = d.FindOwner(b.doorId);
                foreach (var c in door.footprint) Assert.IsFalse(Before(e, zone.GetCell(c.x, c.y)), b.id + " door");
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                    Assert.IsFalse(Before(e, zone.GetCell(b.entryX + dx, b.entryY + dy)), b.id + " approach");
            }
        }
        [Test] public void SouthernArrivalLaneCannotBeOccupiedByVoluntaryTortoiseWandering()
        { var e = Fauna("southern-tortoise"); for (int y = 21; y <= 24; y++) Assert.IsFalse(Before(e, zone.GetCell(40, y))); }
        [TestCase("western-bank-frog", 21, 27, 15, 23)]
        [TestCase("southern-tortoise", 35, 41, 21, 24)]
        public void OrdinaryMovementActuallyWandersInsideTheOutdoorHomeWithoutChangingVitals(string id, int minX, int maxX, int minY, int maxY)
        {
            var e = Fauna(id); var brain = e.GetPart<BrainPart>(); int hp = e.GetStatValue("Hitpoints");
            Assert.IsTrue(brain.Wanders); Assert.IsTrue(brain.WandersRandomly); Assert.NotNull(e.GetPart<Body>());
            var rng = new Random(7183); int moves = 0;
            for (int n = 0; n < 160; n++)
            {
                var delta = MovementSystem.DirectionToDelta(rng.Next(8));
                if (MovementSystem.TryMove(e, zone, delta.dx, delta.dy)) moves++;
                var p = zone.GetEntityPosition(e);
                Assert.That(p.x, Is.InRange(minX, maxX)); Assert.That(p.y, Is.InRange(minY, maxY));
                Assert.IsFalse(zone.GetCell(p.x, p.y).IsInterior); Assert.IsNull(MorrowfastSceneDefinition.Load().RoomAt(p.x, p.y));
            }
            Assert.Greater(moves, 5, "The rule must preserve real wandering, not freeze the animals."); Assert.AreEqual(hp, e.GetStatValue("Hitpoints"));
        }
        [Test] public void ForgedActorSourceAndForeignDestinationCannotAuthorizeTheNativeOwner()
        {
            var e = Fauna("southern-tortoise"); var target = zone.GetCell(38, 23);
            Assert.IsTrue(Before(e, target));
            Assert.IsFalse(Before(e, target, factory.CreateEntity("Player")));
            Assert.IsFalse(Before(e, target, source: zone.GetCell(0, 0)));
            Assert.IsFalse(Before(e, new Zone(MorrowfastSceneRuntime.ZoneID).GetCell(38, 23)));
            Assert.IsFalse(Before(e, null));
        }
        [Test] public void AnOrdinaryWildCreatureOrForeignZoneIsNotGivenASettlementMovementRule()
        {
            var ordinary = factory.CreateEntity("GlasspaneFrog");
            ordinary.AddPart(new MorrowfastFaunaPart { ComponentId = "western-bank-frog" });
            zone.AddEntity(ordinary, 36, 20);
            Assert.IsTrue(Before(ordinary, zone.GetCell(36, 19)), "A copied part cannot govern a different source owner.");
            var foreign = new Zone("Overworld.2.6.0"); zone.RemoveEntity(ordinary); foreign.AddEntity(ordinary, 10, 10);
            Assert.IsTrue(Before(ordinary, foreign.GetCell(10, 11), source: foreign.GetCell(10, 10)));
        }
        [Test] public void NonMovementEventsRemainAvailableAndForcedCombatMovementIsNotVetoed()
        {
            var e = Fauna("southern-tortoise"); var ev = GameEvent.New("BeforeTakeDamage");
            Assert.IsTrue(e.GetPart<MorrowfastFaunaPart>().HandleEvent(ev)); ev.Release();
            Assert.IsTrue(MovementSystem.ForceMoveTo(e, zone, 37, 20));
            Assert.AreEqual((37, 20), zone.GetEntityPosition(e));
            Assert.IsTrue(Before(e, zone.GetCell(37, 21)), "An animal pushed just outside its range can step home.");
        }
        [Test] public void LegacyUnguardedAnimalReturnsHomeWithoutReplacingItsIdentityInventoryOrHealth()
        {
            var animal = Fauna("southern-tortoise"); animal.RemovePart(animal.GetPart<MorrowfastFaunaPart>());
            var item = factory.CreateEntity("FireClay"); animal.GetPart<InventoryPart>().AddObject(item);
            int hp = Math.Max(1, animal.GetStatValue("Hitpoints") - 2); animal.SetStatValue("Hitpoints", hp);
            var brain = animal.GetPart<BrainPart>(); string id = animal.ID;
            zone.MoveEntity(animal, 32, 12);
            Assert.IsTrue(MorrowfastFaunaPart.EnsureHabitat(zone));
            Assert.AreSame(animal, MorrowfastSceneRuntime.FindOwner(zone, "southern-tortoise"));
            Assert.AreEqual((37, 23), zone.GetEntityPosition(animal)); Assert.AreEqual(id, animal.ID);
            Assert.AreSame(brain, animal.GetPart<BrainPart>()); Assert.AreEqual(hp, animal.GetStatValue("Hitpoints"));
            Assert.IsTrue(animal.GetPart<InventoryPart>().Objects.Contains(item)); Assert.AreSame(animal, item.GetPart<PhysicsPart>().InInventory);
            Assert.IsTrue(MorrowfastFaunaPart.EnsureHabitat(zone)); Assert.AreEqual(1, animal.Parts.OfType<MorrowfastFaunaPart>().Count());
            Assert.AreEqual((37, 23), zone.GetEntityPosition(animal));
        }
        [Test] public void MissingHabitatOnAValidOwnerIsAddedWithoutMovingIt()
        {
            var animal = Fauna("western-bank-frog"); var before = zone.GetEntityPosition(animal);
            animal.RemovePart(animal.GetPart<MorrowfastFaunaPart>());
            Assert.IsTrue(MorrowfastFaunaPart.EnsureHabitat(zone));
            Assert.AreEqual(before, zone.GetEntityPosition(animal)); Assert.AreEqual("western-bank-frog", animal.GetPart<MorrowfastFaunaPart>().ComponentId);
        }
        [Test] public void AlreadyGuardedAnimalsAreNotTeleportedAfterLegitimateForcedMovement()
        {
            var animal = Fauna("southern-tortoise"); var part = animal.GetPart<MorrowfastFaunaPart>();
            Assert.IsTrue(MovementSystem.ForceMoveTo(animal, zone, 37, 20));
            Assert.IsTrue(MorrowfastFaunaPart.EnsureHabitat(zone));
            Assert.AreEqual((37, 20), zone.GetEntityPosition(animal)); Assert.AreSame(part, animal.GetPart<MorrowfastFaunaPart>());
        }
        [Test] public void AnOccupiedOriginalAnchorChoosesAnotherFreeHomeCell()
        {
            var animal = Fauna("southern-tortoise"); animal.RemovePart(animal.GetPart<MorrowfastFaunaPart>()); zone.MoveEntity(animal, 32, 12);
            var blocker = MorrowfastTestWorld.Actor(factory, zone, 37, 23);
            Assert.IsTrue(MorrowfastFaunaPart.EnsureHabitat(zone));
            var after = zone.GetEntityPosition(animal); Assert.AreNotEqual((37, 23), after); Assert.AreNotEqual((32, 12), after);
            Assert.IsTrue(Before(animal, zone.GetCell(after.x, after.y))); Assert.AreEqual((37, 23), zone.GetEntityPosition(blocker));
            Assert.IsFalse(zone.GetCell(after.x, after.y).BlocksMovement(animal));
        }
        [Test] public void AFullHabitatLeavesTheLegacyOwnerUntouchedForALaterSafeRepair()
        {
            var animal = Fauna("southern-tortoise"); animal.RemovePart(animal.GetPart<MorrowfastFaunaPart>()); zone.MoveEntity(animal, 32, 12);
            var blockers = new System.Collections.Generic.List<Entity>();
            for (int x = 35; x <= 41; x++) for (int y = 21; y <= 24; y++)
            { var e = new Entity(); e.AddPart(new PhysicsPart { Solid = true }); zone.AddEntity(e, x, y); blockers.Add(e); }
            Assert.IsFalse(MorrowfastFaunaPart.EnsureHabitat(zone)); Assert.AreEqual((32, 12), zone.GetEntityPosition(animal));
            Assert.IsNull(animal.GetPart<MorrowfastFaunaPart>());
            foreach (var e in blockers) zone.RemoveEntity(e);
            Assert.IsTrue(MorrowfastFaunaPart.EnsureHabitat(zone)); Assert.AreEqual((37, 23), zone.GetEntityPosition(animal));
        }
        [Test] public void HabitatRepairDoesNotResurrectDeadOwnersOrTouchForeignZones()
        {
            var animal = Fauna("southern-tortoise"); animal.RemovePart(animal.GetPart<MorrowfastFaunaPart>()); animal.SetStatValue("Hitpoints", 0);
            Assert.IsTrue(MorrowfastFaunaPart.EnsureHabitat(zone)); Assert.AreEqual(0, animal.GetStatValue("Hitpoints")); Assert.IsNull(animal.GetPart<MorrowfastFaunaPart>());
            var foreign = new Zone("Overworld.2.6.0"); var frog = factory.CreateEntity("GlasspaneFrog"); foreign.AddEntity(frog, 10, 10);
            Assert.IsFalse(MorrowfastFaunaPart.EnsureHabitat(foreign)); Assert.AreEqual((10, 10), foreign.GetEntityPosition(frog)); Assert.IsNull(frog.GetPart<MorrowfastFaunaPart>());
        }
    }
}
