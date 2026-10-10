using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class ControllerTravelTests
    {
        internal static Zone KnownZone(bool allKnown = true, string id = "Travel.0.0.0")
        {
            var zone = new Zone(id);
            if (allKnown)
                for (int x = 0; x < Zone.Width; x++)
                    for (int y = 0; y < Zone.Height; y++) zone.GetCell(x, y).Explored = true;
            return zone;
        }

        internal static void Know(Zone zone, int x, int y, bool visible = false)
        { zone.GetCell(x, y).Explored = true; zone.GetCell(x, y).IsVisible = visible; }

        internal static Entity Actor(Zone zone, int x = 5, int y = 5, string shape = null)
        {
            var actor = new Entity { ID = Guid.NewGuid().ToString("N") };
            actor.SetTag("Creature"); actor.SetTag("Player");
            actor.AddPart(new PhysicsPart { Solid = true });
            actor.AddPart(new BrainPart());
            actor.AddPart(new StatusEffectsPart());
            actor.AddPart(new RenderPart { Visible = true });
            foreach (string name in new[] { "Hitpoints", "Strength", "Toughness" })
                actor.Statistics[name] = new Stat { Owner = actor, Name = name, BaseValue = 20, Max = 20 };
            if (shape != null) actor.AddPart(new SpatialFootprintPart { CellsRaw = shape });
            Assert.IsTrue(zone.AddEntity(actor, x, y));
            foreach (var cell in zone.GetOccupiedCells(actor)) Know(zone, cell.X, cell.Y, true);
            return actor;
        }

        internal static Entity Place(Zone zone, int x, int y, Part part = null, string tag = null)
        {
            var owner = new Entity { ID = Guid.NewGuid().ToString("N") };
            if (tag != null) owner.SetTag(tag);
            if (part != null) owner.AddPart(part);
            Assert.IsTrue(zone.AddEntity(owner, x, y));
            return owner;
        }

        internal static Zone Corridor(int from = 5, int to = 12, int y = 5)
        {
            var zone = KnownZone(false);
            for (int x = from; x <= to; x++) Know(zone, x, y);
            return zone;
        }

        internal static void NoPoint(Zone zone, Entity actor, int x, int y)
        {
            int dx = 99, dy = 99;
            Assert.IsFalse(ControllerTravel.TryStepTowardPoint(zone, actor, x, y, out dx, out dy));
            Assert.AreEqual((0, 0), (dx, dy));
        }

        internal static Entity Threat(Zone zone, Entity actor, int x, int y, bool visible = true)
        {
            var enemy = Place(zone, x, y, new BrainPart(), "Creature");
            enemy.AddPart(new RenderPart { Visible = true });
            enemy.Statistics["Hitpoints"] = new Stat { Owner = enemy, Name = "Hitpoints", BaseValue = 20, Max = 20 };
            enemy.GetPart<BrainPart>().PersonalEnemies.Add(actor);
            zone.GetCell(x, y).IsVisible = visible;
            return enemy;
        }

        internal static void WalkTo(Zone zone, Entity actor, int x, int y)
        {
            for (int n = 0; n < Zone.Width * Zone.Height; n++)
            {
                var from = zone.GetEntityPosition(actor);
                if (from == (x, y)) { NoPoint(zone, actor, x, y); return; }
                Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, x, y, out int dx, out int dy));
                Assert.That(Math.Abs(dx), Is.LessThanOrEqualTo(1));
                Assert.That(Math.Abs(dy), Is.LessThanOrEqualTo(1));
                Assert.AreNotEqual((0, 0), (dx, dy));
                foreach (var cell in zone.GetOccupiedCells(actor, from.x + dx, from.y + dy))
                    Assert.IsTrue(cell != null && (cell.Explored || cell.IsVisible));
                Assert.IsTrue(zone.MoveEntity(actor, from.x + dx, from.y + dy));
            }
            Assert.Fail("Bounded local route did not arrive.");
        }

        [TestCase(0, -1)] [TestCase(1, -1)] [TestCase(1, 0)] [TestCase(1, 1)]
        [TestCase(0, 1)] [TestCase(-1, 1)] [TestCase(-1, 0)] [TestCase(-1, -1)]
        public void KnownPointPrefersEachRequestedDirection(int dx, int dy)
        {
            var zone = KnownZone(); var actor = Actor(zone, 40, 12);
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 40 + dx * 3, 12 + dy * 3, out int sx, out int sy));
            Assert.AreEqual((dx, dy), (sx, sy));
            Assert.AreEqual((40, 12), zone.GetEntityPosition(actor), "planning never moves");
        }

        [TestCase(0, -1)] [TestCase(1, -1)] [TestCase(1, 0)] [TestCase(1, 1)]
        [TestCase(0, 1)] [TestCase(-1, 1)] [TestCase(-1, 0)] [TestCase(-1, -1)]
        public void KnownEdgePrefersEachRequestedDirection(int dx, int dy)
        {
            var zone = KnownZone(); var actor = Actor(zone, 40, 12);
            Assert.IsTrue(ControllerTravel.TryStepTowardEdge(zone, actor, dx, dy, out int sx, out int sy));
            Assert.AreEqual((dx, dy), (sx, sy));
        }

        [Test]
        public void EdgeApproachesKnownHeadingWithoutReadingUnknownDestination()
        {
            var zone = Corridor(); var actor = Actor(zone);
            Assert.IsTrue(ControllerTravel.TryStepTowardEdge(zone, actor, 1, 0, out int dx, out int dy));
            Assert.AreEqual((1, 0), (dx, dy));
            Assert.IsTrue(zone.MoveEntity(actor, 12, 5));
            Assert.IsFalse(ControllerTravel.TryStepTowardEdge(zone, actor, 1, 0, out dx, out dy));
            Assert.AreEqual((0, 0), (dx, dy));
        }

        [Test]
        public void ClosedBarrierRequiresDetourWithoutOpeningOrUnlocking()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var door = new DoorPart { IsOpen = false };
            var owner = Place(zone, 7, 5, door);
            owner.AddPart(new LockPart { IsLocked = true });
            NoPoint(zone, actor, 12, 5);
            for (int x = 5; x <= 9; x++) Know(zone, x, 4);
            WalkTo(zone, actor, 12, 5);
            Assert.IsFalse(door.IsOpen); Assert.IsTrue(owner.GetPart<LockPart>().IsLocked);
        }

        [Test]
        public void PhysicsOnlyWallBlocksAndRemovalRestoresRoute()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var wall = Place(zone, 7, 5, new PhysicsPart { Solid = true });
            NoPoint(zone, actor, 12, 5);
            zone.RemoveEntity(wall); WalkTo(zone, actor, 12, 5);
        }

        [Test]
        public void PositiveFiniteGasCostIsForbiddenEvenAsOnlyRoute()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var gas = new GasPoolPart { Density = 20 };
            Place(zone, 8, 5, gas);
            Assert.Greater(TerrainNavigationWeight.ForStep(zone, 8, 5, actor), 0);
            NoPoint(zone, actor, 12, 5);
            gas.Density = 0; WalkTo(zone, actor, 12, 5);
        }

        [Test]
        public void ActualFireBlocksButCoolUnburningOwnerDoesNot()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var thermal = new ThermalPart { Temperature = 500, FlameTemperature = 400 };
            Place(zone, 8, 5, thermal);
            NoPoint(zone, actor, 12, 5);
            thermal.Temperature = 25; WalkTo(zone, actor, 12, 5);
        }

        [Test]
        public void ActiveStepTrapBlocksWithoutTriggeringOrConsumingIt()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var trap = Place(zone, 8, 5, new SpikeTrapTriggerPart());
            NoPoint(zone, actor, 12, 5);
            Assert.AreSame(zone.GetCell(8, 5), zone.GetEntityCell(trap));
            Assert.AreEqual(20, actor.GetStatValue("Hitpoints"));
            zone.RemoveEntity(trap); WalkTo(zone, actor, 12, 5);
        }

        [Test]
        public void VisibleLivingThreatStopsAllThreePlanningModes()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            Threat(zone, actor, 8, 7);
            Assert.IsTrue(ControllerTravel.HasDanger(zone, actor));
            NoPoint(zone, actor, 12, 5);
            Assert.IsFalse(ControllerTravel.TryStepTowardEdge(zone, actor, 1, 0, out int dx, out int dy));
            Assert.AreEqual((0, 0), (dx, dy));
            Assert.IsFalse(ControllerTravel.TryStepTowardFrontier(zone, actor, out dx, out dy));
            Assert.AreEqual((0, 0), (dx, dy));
        }

        [Test]
        public void FrontierStopsAtKnownCellBesideUnknownWithoutEnteringIt()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            zone.GetCell(10, 5).Explored = false;
            Assert.IsTrue(ControllerTravel.TryStepTowardFrontier(zone, actor, out int dx, out int dy));
            Assert.AreEqual(1, dx);
            Assert.That(Math.Abs(dy), Is.LessThanOrEqualTo(1));
            Assert.IsTrue(zone.MoveEntity(actor, 9, 5));
            Assert.IsFalse(ControllerTravel.TryStepTowardFrontier(zone, actor, out dx, out dy));
            Assert.AreEqual((0, 0), (dx, dy));
            Assert.IsFalse(zone.GetCell(10, 5).Explored);
        }

        [Test]
        public void CompletelyKnownZoneHasNoFrontier()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            Assert.IsFalse(ControllerTravel.TryStepTowardFrontier(zone, actor, out int dx, out int dy));
            Assert.AreEqual((0, 0), (dx, dy));
        }

        [Test]
        public void UnknownPointRefusesWhileVisibleOrExploredPointWorks()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var target = zone.GetCell(12, 5); target.Explored = false;
            NoPoint(zone, actor, 12, 5);
            target.IsVisible = true;
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 12, 5, out _, out _));
            target.IsVisible = false; target.Explored = true;
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 12, 5, out _, out _));
        }

        [Test]
        public void SamePointAndAlreadyReachedEdgeAreNoOps()
        {
            var zone = KnownZone(); var actor = Actor(zone, Zone.Width - 1, 12);
            NoPoint(zone, actor, Zone.Width - 1, 12);
            Assert.IsFalse(ControllerTravel.TryStepTowardEdge(zone, actor, 1, 0, out int dx, out int dy));
            Assert.AreEqual((0, 0), (dx, dy));
        }
    }
}
