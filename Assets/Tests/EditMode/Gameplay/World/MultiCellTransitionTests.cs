using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Cross-zone handoff owns one entity and admits its entire body.</summary>
    public sealed class MultiCellTransitionTests
    {
        private static Entity Actor(Zone zone, int x = 40, int y = 12, string cells = "0,0;1,0;0,1;1,1")
        {
            var actor = new Entity();
            actor.Tags["Creature"] = "";
            actor.AddPart(new PhysicsPart { Solid = true });
            actor.AddPart(new BrainPart { CurrentZone = zone });
            if (cells != null) actor.AddPart(new SpatialFootprintPart { CellsRaw = cells });
            Assert.IsTrue(zone.AddEntity(actor, x, y));
            return actor;
        }

        private static Entity Obstacle(Zone zone, int x, int y, bool exclusion = false)
        {
            var blocker = new Entity();
            blocker.Tags[exclusion ? "ExcludeZoneArrival" : "Solid"] = "";
            Assert.IsTrue(zone.AddEntity(blocker, x, y));
            return blocker;
        }

        private static ZoneManager Manager(Zone source, Zone destination)
        {
            var manager = new ZoneManager(null, 144);
            manager.CachedZones[source.ZoneID] = source;
            manager.CachedZones[destination.ZoneID] = destination;
            return manager;
        }

        private static ZoneTransitionResult Horizontal(Entity actor, Zone source, Zone destination,
            TransitionDirection direction = TransitionDirection.East)
        {
            var position = source.GetEntityPosition(actor);
            return ZoneTransitionSystem.TransitionPlayer(actor, source, direction,
                position.x, position.y, Manager(source, destination), null);
        }

        private static void AssertTransferred(Entity actor, Zone source, Zone destination, int count)
        {
            Assert.IsNull(source.GetEntityCell(actor));
            Assert.IsNotNull(destination.GetEntityCell(actor));
            Assert.AreEqual(count, destination.GetOccupiedCells(actor).Count);
            Assert.AreEqual(1, destination.GetAllEntities().Count(e => e == actor));
            for (int y = 0; y < Zone.Height; y++)
                for (int x = 0; x < Zone.Width; x++)
                    Assert.IsFalse(source.GetOccupants(x, y).Contains(actor), "Old body cell must be empty");
            foreach (var point in destination.GetOccupiedCells(actor))
                Assert.IsTrue(destination.GetOccupants(point.X, point.Y).Contains(actor));
        }

        [TestCase(TransitionDirection.East, "Overworld.4.7.0")]
        [TestCase(TransitionDirection.West, "Overworld.2.7.0")]
        [TestCase(TransitionDirection.North, "Overworld.3.6.0")]
        [TestCase(TransitionDirection.South, "Overworld.3.8.0")]
        public void HorizontalArrivalFitsTheCompleteBodyAtEveryEdge(TransitionDirection direction, string id)
        {
            var source = new Zone("Overworld.3.7.0");
            var destination = new Zone(id);
            var actor = Actor(source);
            var result = Horizontal(actor, source, destination, direction);
            Assert.IsTrue(result.Success, result.ErrorReason);
            AssertTransferred(actor, source, destination, 4);
            Assert.IsTrue(destination.CanPlaceFootprint(actor, result.NewPlayerX, result.NewPlayerY));
        }

        [TestCase(TransitionDirection.East, "Overworld.4.7.0")]
        [TestCase(TransitionDirection.West, "Overworld.2.7.0")]
        [TestCase(TransitionDirection.North, "Overworld.3.6.0")]
        [TestCase(TransitionDirection.South, "Overworld.3.8.0")]
        public void OrdinarySingleCellArrivalKeepsExactOppositeEdge(TransitionDirection direction, string id)
        {
            var source = new Zone("Overworld.3.7.0");
            var destination = new Zone(id);
            var actor = Actor(source, cells: null);
            var ideal = ZoneTransitionSystem.GetArrivalPosition(direction, 40, 12);
            var result = Horizontal(actor, source, destination, direction);
            Assert.IsTrue(result.Success, result.ErrorReason);
            Assert.AreEqual(ideal, destination.GetEntityPosition(actor));
            AssertTransferred(actor, source, destination, 1);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FarCornerBlockOrArrivalExclusionChoosesAnotherCompletePlacement(bool exclusion)
        {
            var source = new Zone("Overworld.3.7.0");
            var destination = new Zone("Overworld.4.7.0");
            var actor = Actor(source);
            Obstacle(destination, 1, 13, exclusion);
            var result = Horizontal(actor, source, destination);
            Assert.IsTrue(result.Success, result.ErrorReason);
            Assert.AreNotEqual((0, 12), destination.GetEntityPosition(actor));
            Assert.IsFalse(destination.GetOccupants(1, 13).Contains(actor));
            AssertTransferred(actor, source, destination, 4);
        }

        [Test]
        public void NegativeOffsetsSearchInwardInsteadOfClippingTheBody()
        {
            var source = new Zone("Overworld.3.7.0");
            var destination = new Zone("Overworld.4.7.0");
            var actor = Actor(source, cells: "-1,0;0,0;-1,1;0,1");
            var result = Horizontal(actor, source, destination);
            Assert.IsTrue(result.Success, result.ErrorReason);
            Assert.GreaterOrEqual(result.NewPlayerX, 1);
            AssertTransferred(actor, source, destination, 4);
        }

        [Test]
        public void HoleAtAnchorDoesNotBecomeAnArrivalExclusionContact()
        {
            var source = new Zone("Overworld.3.7.0");
            var destination = new Zone("Overworld.4.7.0");
            var actor = Actor(source, cells: "1,0;1,1");
            Obstacle(destination, 0, 12, exclusion: true);
            var result = Horizontal(actor, source, destination);
            Assert.IsTrue(result.Success, result.ErrorReason);
            Assert.AreEqual((0, 12), destination.GetEntityPosition(actor));
            Assert.IsFalse(destination.GetOccupants(0, 12).Contains(actor));
            AssertTransferred(actor, source, destination, 2);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NoEligibleHorizontalBodyPlacementLeavesTheSourceUntouched(bool exclusion)
        {
            var source = new Zone("Overworld.3.7.0");
            var destination = new Zone("Overworld.4.7.0");
            var actor = Actor(source);
            for (int x = 0; x <= 11; x++)
                for (int y = 0; y < Zone.Height; y++)
                    Obstacle(destination, x, y, exclusion);
            int sourceVersion = source.EntityVersion, destinationVersion = destination.EntityVersion;
            var result = Horizontal(actor, source, destination);
            Assert.IsFalse(result.Success);
            Assert.AreEqual((40, 12), source.GetEntityPosition(actor));
            Assert.AreEqual(sourceVersion, source.EntityVersion);
            Assert.AreEqual(destinationVersion, destination.EntityVersion);
            Assert.IsNull(destination.GetEntityCell(actor));
            Assert.IsTrue(source.GetOccupants(41, 13).Contains(actor));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerticalArrivalSkipsMatchedStairWhoseFarCornerIsUnavailable(bool exclusion)
        {
            var source = new Zone("Overworld.3.7.0");
            var destination = new Zone("Overworld.3.7.1");
            var actor = Actor(source, 10, 10);
            var near = new Entity(); near.Tags["StairsUp"] = "";
            var far = new Entity(); far.Tags["StairsUp"] = "";
            destination.AddEntity(near, 10, 10); destination.AddEntity(far, 20, 10);
            Obstacle(destination, 11, 11, exclusion);
            var result = ZoneTransitionSystem.TransitionPlayerVertical(actor, source, true,
                10, 10, Manager(source, destination));
            Assert.IsTrue(result.Success, result.ErrorReason);
            Assert.AreEqual((20, 10), destination.GetEntityPosition(actor));
            AssertTransferred(actor, source, destination, 4);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void VerticalFallbackRequiresWholeFootprintAndKeepsFailedSource(bool roomForBody)
        {
            var source = new Zone("Overworld.3.7.0");
            var destination = new Zone("Overworld.3.7.1");
            var actor = Actor(source, 10, 10);
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                    if (!(x == 10 && y == 10) && !(roomForBody && x >= 10 && x <= 11 && y >= 10 && y <= 11))
                        Obstacle(destination, x, y);
            int version = source.EntityVersion;
            var result = ZoneTransitionSystem.TransitionPlayerVertical(actor, source, true,
                10, 10, Manager(source, destination));
            Assert.AreEqual(roomForBody, result.Success);
            if (roomForBody) AssertTransferred(actor, source, destination, 4);
            else
            {
                Assert.AreEqual((10, 10), source.GetEntityPosition(actor));
                Assert.AreEqual(version, source.EntityVersion);
                Assert.IsNull(destination.GetEntityCell(actor));
                Assert.IsTrue(source.GetOccupants(11, 11).Contains(actor));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FollowerNeedsAnEntireEligibleBodyOrRemainsBehind(bool roomForBody)
        {
            var source = new Zone("Overworld.3.7.0");
            var destination = new Zone("Overworld.4.7.0");
            var leader = Actor(destination, 10, 10, cells: null);
            var follower = Actor(source);
            leader.GetPart<BrainPart>().PartyMembers.Add(follower);
            for (int x = 6; x <= 15; x++)
                for (int y = 6; y <= 15; y++)
                    if (!(x == 14 && y == 10) && !(roomForBody && x >= 14 && y >= 10 && y <= 11))
                        Obstacle(destination, x, y, exclusion: true);
            int version = source.EntityVersion;
            ZoneTransitionSystem.TransitPartyMembers(leader, source, destination, 10, 10);
            if (roomForBody)
            {
                AssertTransferred(follower, source, destination, 4);
                Assert.AreEqual((14, 10), destination.GetEntityPosition(follower));
                Assert.AreSame(destination, follower.GetPart<BrainPart>().CurrentZone);
            }
            else
            {
                Assert.AreEqual((40, 12), source.GetEntityPosition(follower));
                Assert.AreEqual(version, source.EntityVersion);
                Assert.IsNull(destination.GetEntityCell(follower));
                Assert.AreSame(source, follower.GetPart<BrainPart>().CurrentZone);
            }
        }

        [Test]
        public void FollowerCannotOverlapTheLeaderWithItsFarEdge()
        {
            var source = new Zone("Overworld.3.7.0");
            var destination = new Zone("Overworld.4.7.0");
            var leader = Actor(destination, 10, 10, cells: null);
            var follower = Actor(source);
            leader.GetPart<BrainPart>().PartyMembers.Add(follower);
            ZoneTransitionSystem.TransitPartyMembers(leader, source, destination, 10, 10);
            AssertTransferred(follower, source, destination, 4);
            Assert.IsFalse(destination.GetOccupants(10, 10).Contains(follower));
        }

        [Test]
        public void FollowerAlreadyElsewhereIsNotTransferred()
        {
            var source = new Zone("Overworld.3.7.0");
            var elsewhere = new Zone("Overworld.3.8.0");
            var destination = new Zone("Overworld.4.7.0");
            var leader = Actor(destination, 10, 10, cells: null);
            var follower = Actor(elsewhere);
            leader.GetPart<BrainPart>().PartyMembers.Add(follower);
            ZoneTransitionSystem.TransitPartyMembers(leader, source, destination, 10, 10);
            Assert.AreEqual((40, 12), elsewhere.GetEntityPosition(follower));
            Assert.IsNull(destination.GetEntityCell(follower));
            Assert.AreSame(elsewhere, follower.GetPart<BrainPart>().CurrentZone);
        }
    }
}
