using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DensityWorldMapArrivalTests
    {
        private EntityFactory factory;
        private ZoneManager manager;
        private Zone map, target;
        private Entity player;

        [SetUp]
        public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            manager = new ZoneManager(factory, 1);
            map = new Zone(WorldMap.WorldMapZoneID);
            target = new Zone(WorldMap.ToZoneID(19, 1, 0));
            manager.CachedZones[target.ZoneID] = target;
            var marker = new Entity { ID = Guid.NewGuid().ToString("N") };
            marker.AddPart(new WorldMapCellPart { WorldX = 19, WorldY = 1 });
            map.AddEntity(marker, 40, 12);
            player = new Entity { ID = Guid.NewGuid().ToString("N") };
            player.Tags["Player"] = "";
            player.Tags["Creature"] = "";
            player.AddPart(new PhysicsPart { Solid = true });
            map.AddEntity(player, 40, 12);
        }

        private Entity Block(int x, int y)
        {
            var blocker = new Entity { ID = Guid.NewGuid().ToString("N") };
            blocker.AddPart(new PhysicsPart { Solid = true });
            target.AddEntity(blocker, x, y);
            return blocker;
        }

        private ZoneTransitionResult Descend() => WorldMapTraversal.Descend(player, map, manager);

        private void AssertClearArrival(ZoneTransitionResult result)
        {
            Assert.IsTrue(result.Success, result.ErrorReason);
            Assert.IsNull(map.GetEntityCell(player));
            Assert.IsTrue(target.CanPlaceFootprint(player, result.NewPlayerX, result.NewPlayerY),
                "Arrival must satisfy the same complete-body collision rule as movement.");
            Assert.AreEqual((result.NewPlayerX, result.NewPlayerY), target.GetEntityPosition(player));
        }

        [Test]
        public void Descend_RealDesertProwlerAtCenter_RemainsSeparateFromPlayer()
        {
            var boss = factory.CreateEntity("DesertProwler");
            Assert.IsTrue(boss.GetPart<PhysicsPart>().Solid);
            Assert.IsFalse(boss.HasTag("Solid"), "this is the actual tag-versus-Physics regression");
            target.AddEntity(boss, 40, 12);
            AssertClearArrival(Descend());
            Assert.AreEqual((40, 12), target.GetEntityPosition(boss), "arrival never relocates the boss");
            Assert.AreNotEqual(target.GetEntityPosition(boss), target.GetEntityPosition(player));
        }

        [Test]
        public void Descend_PhysicsOnlyFurnitureAlsoBlocksArrival()
        {
            var furniture = Block(40, 12);
            AssertClearArrival(Descend());
            Assert.AreNotEqual(target.GetEntityPosition(furniture), target.GetEntityPosition(player));
        }

        [Test]
        public void Descend_FallbackSkipsOccupiedCandidate_KeepingExistingSearchOrder()
        {
            Block(40, 12);
            Block(39, 11);
            var result = Descend();
            AssertClearArrival(result);
            Assert.AreEqual((39, 12), target.GetEntityPosition(player));
        }

        [TestCase(true)] [TestCase(false)]
        public void Descend_SavedLocationUsesSameCollisionRule(bool blocked)
        {
            player.AddPart(new WorldMapPart { LastZoneIDOnSurface = target.ZoneID, LastZoneX = 20, LastZoneY = 9 });
            if (blocked) Block(20, 9);
            AssertClearArrival(Descend());
            Assert.AreEqual(blocked ? (19, 8) : (20, 9), target.GetEntityPosition(player));
        }

        [Test]
        public void Descend_NoCollisionFreeCell_FailsWithoutDetachingPlayer()
        {
            target.ForEachCell((cell, x, y) => Block(x, y));
            var result = Descend();
            Assert.IsFalse(result.Success);
            Assert.AreEqual((40, 12), map.GetEntityPosition(player));
            Assert.IsNull(target.GetEntityCell(player));
        }

        [Test]
        public void Descend_WholePlayerFootprintMustFitBeforeLeavingMap()
        {
            map.RemoveEntity(player);
            player.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
            map.AddEntity(player, 40, 12);
            Block(41, 12);
            var result = Descend();
            AssertClearArrival(result);
            Assert.AreNotEqual((40, 12), target.GetEntityPosition(player));
        }

        [TestCase(false)] [TestCase(true)]
        public void Descend_EmptyCenterAndReservedCenterRemainValid(bool reserved)
        {
            if (reserved) target.GenReservedCells.Add((40, 12));
            AssertClearArrival(Descend());
            Assert.AreEqual((40, 12), target.GetEntityPosition(player),
                "Generation reservations protect arrival sites; they must not prevent travel.");
        }

        [Test]
        public void Descend_NonblockingPoolAndLooseItemKeepExistingArrivalSemantics()
        {
            var pool = factory.CreateEntity("OilSlick");
            var item = factory.CreateEntity("Dagger");
            target.AddEntity(pool, 40, 12);
            target.AddEntity(item, 40, 12);
            AssertClearArrival(Descend());
            Assert.AreEqual((40, 12), target.GetEntityPosition(player));
            Assert.IsTrue(target.GetCell(40, 12).Objects.Contains(pool));
            Assert.IsTrue(target.GetCell(40, 12).Objects.Contains(item));
        }
    }
}
