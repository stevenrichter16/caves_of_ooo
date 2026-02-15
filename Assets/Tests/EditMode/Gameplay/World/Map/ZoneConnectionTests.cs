using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class ZoneConnectionTests
    {
        private ZoneManager _manager;

        [SetUp]
        public void SetUp()
        {
            var factory = new EntityFactory();
            _manager = new ZoneManager(factory, 42);
        }

        [Test]
        public void RegisterConnection_StoresConnection()
        {
            var conn = new ZoneConnection
            {
                SourceZoneID = "Overworld.5.5.0",
                SourceX = 10, SourceY = 12,
                TargetZoneID = "Overworld.5.5.1",
                TargetX = 10, TargetY = 12,
                Type = "StairsDown"
            };

            _manager.RegisterConnection(conn);

            var connections = _manager.GetConnections("Overworld.5.5.0");
            Assert.AreEqual(1, connections.Count);
            Assert.AreEqual("StairsDown", connections[0].Type);
        }

        [Test]
        public void RegisterConnection_IndexedByBothSourceAndTarget()
        {
            var conn = new ZoneConnection
            {
                SourceZoneID = "Overworld.5.5.0",
                SourceX = 10, SourceY = 12,
                TargetZoneID = "Overworld.5.5.1",
                TargetX = 10, TargetY = 12,
                Type = "StairsDown"
            };

            _manager.RegisterConnection(conn);

            // Should be findable from both source and target zone
            Assert.AreEqual(1, _manager.GetConnections("Overworld.5.5.0").Count);
            Assert.AreEqual(1, _manager.GetConnections("Overworld.5.5.1").Count);
        }

        [Test]
        public void GetConnectionsTo_FiltersCorrectly()
        {
            _manager.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "Overworld.5.5.0",
                SourceX = 10, SourceY = 12,
                TargetZoneID = "Overworld.5.5.1",
                TargetX = 10, TargetY = 12,
                Type = "StairsDown"
            });

            _manager.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "Overworld.5.5.1",
                SourceX = 30, SourceY = 8,
                TargetZoneID = "Overworld.5.5.2",
                TargetX = 30, TargetY = 8,
                Type = "StairsDown"
            });

            var toLevel1 = _manager.GetConnectionsTo("Overworld.5.5.1", "StairsDown");
            Assert.AreEqual(1, toLevel1.Count);
            Assert.AreEqual("Overworld.5.5.0", toLevel1[0].SourceZoneID);
        }

        [Test]
        public void GetConnections_ReturnsEmptyForUnknownZone()
        {
            var connections = _manager.GetConnections("Overworld.99.99.0");
            Assert.IsNotNull(connections);
            Assert.AreEqual(0, connections.Count);
        }

        [Test]
        public void GetZoneTier_SurfaceIsTier1()
        {
            Assert.AreEqual(1, ZoneManager.GetZoneTier("Overworld.5.5.0"));
        }

        [Test]
        public void GetZoneTier_ScalesByDepth()
        {
            Assert.AreEqual(1, ZoneManager.GetZoneTier("Overworld.5.5.1"));
            Assert.AreEqual(1, ZoneManager.GetZoneTier("Overworld.5.5.2"));
            Assert.AreEqual(2, ZoneManager.GetZoneTier("Overworld.5.5.3"));
            Assert.AreEqual(2, ZoneManager.GetZoneTier("Overworld.5.5.5"));
            Assert.AreEqual(3, ZoneManager.GetZoneTier("Overworld.5.5.6"));
            Assert.AreEqual(4, ZoneManager.GetZoneTier("Overworld.5.5.9"));
        }

        [Test]
        public void GetZoneTier_CapsAt8()
        {
            Assert.AreEqual(8, ZoneManager.GetZoneTier("Overworld.5.5.21"));
            Assert.AreEqual(8, ZoneManager.GetZoneTier("Overworld.5.5.50"));
        }
    }
}
