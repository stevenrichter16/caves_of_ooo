using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CavePresentationAddressCacheTests
    {
        EntityEquipmentContentFixture content;
        OverworldZoneManager manager;
        string surface, cave;
        int x, y;

        [SetUp] public void SetUp()
        {
            content = new EntityEquipmentContentFixture();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            surface = BiomeCropPlan.CaveColumns(manager).First();
            var at = WorldMap.FromZoneID(surface); x = at.x; y = at.y;
            cave = WorldMap.ToZoneID(x, y, 1);
            Assert.IsTrue(BiomeCropPlan.IsOrdinaryCave(manager, cave), "The receiving column must actually be ordinary.");
        }
        [TearDown] public void TearDown() => content.Dispose();

        static int Parses
        {
            get
            {
                var counter = typeof(BiomeCropRecipes).GetProperty("OrdinaryCaveAddressParseCount",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(counter, "Expose actual address parser work before claiming repeated queries are cheap.");
                return (int)counter.GetValue(null);
            }
        }
        Zone Attach(string id)
        {
            var zone = new Zone(id); manager.SetActiveZone(zone); return zone;
        }

        [TestCase(0)] [TestCase(1)]
        public void WarmSurfaceAndCaveQueriesDoNotReparseOrCreateWorldContent(int depth)
        {
            var zone = Attach(WorldMap.ToZoneID(x, y, depth));
            int before = Parses;
            Assert.AreEqual(depth > 0, BiomeCropRecipes.IsOrdinaryCave(zone));
            Assert.AreEqual(before + 1, Parses, "The cold query really parses the current address once.");
            for (int i = 0; i < 4096; i++)
                Assert.AreEqual(depth > 0, BiomeCropRecipes.IsOrdinaryCave(zone));
            Assert.AreEqual(before + 1, Parses, "Visible cells and owners must reuse immutable address data.");
            Assert.AreEqual(1, manager.CachedZoneCount);
            Assert.Zero(zone.EntityCount);
        }

        [Test]
        public void MutableZoneIdReparsesOnceAndDoesNotReusePreviousDepth()
        {
            var zone = Attach(cave); Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); int before = Parses;
            manager.CachedZones.Remove(cave); zone.ZoneID = surface; manager.SetActiveZone(zone);
            Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(before + 1, Parses);
            Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(before + 1, Parses);
            manager.CachedZones.Remove(surface); zone.ZoneID = cave; manager.SetActiveZone(zone);
            Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(before + 2, Parses);
        }

        [Test]
        public void LivePoiMutationChangesAuthorityWithoutReparsing()
        {
            var zone = Attach(cave); Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); int before = Parses;
            manager.WorldMap.SetPOI(x, y, new PointOfInterest(POIType.Village, "late authority"));
            Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(before, Parses);
            manager.WorldMap.SetPOI(x, y, null);
            Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(before, Parses);
        }

        [Test]
        public void SavedLairClaimStillBlocksAfterMutablePoiIsRemoved()
        {
            var zone = Attach(cave); Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); int before = Parses;
            manager.WorldMap.Tiles[x, y] = BiomeType.Spread;
            manager.WorldMap.SetPOI(x, y, new PointOfInterest(POIType.Lair, "late lair claim", null, 2, "JungleStalker"));
            Assert.NotNull(manager.GetZone(surface)); Assert.NotNull(LairStacks.Inspect(manager, surface));
            manager.WorldMap.SetPOI(x, y, null);
            Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(zone));
            // Generation may itself query other addresses; the warmed owner must
            // still use its cached address after that independent work.
            int afterGeneration = Parses;
            Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(afterGeneration, Parses);
            Assert.GreaterOrEqual(afterGeneration, before);
        }

        [Test]
        public void ReattachedGraphUsesReceivingManagerRatherThanCachedAuthority()
        {
            var zone = Attach(cave); Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); int before = Parses;
            var other = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            other.WorldMap.SetPOI(x, y, new PointOfInterest(POIType.Village, "receiving authority"));
            other.SetActiveZone(zone);
            Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(before, Parses);
            manager.SetActiveZone(zone);
            Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(before, Parses);
        }

        [Test]
        public void ExactCachedGraphMembershipRemainsLiveAfterAddressIsWarm()
        {
            var zone = Attach(cave); Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); int before = Parses;
            var replacement = new Zone(cave); manager.CachedZones[cave] = replacement;
            Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(zone));
            Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(replacement), "A dictionary insertion alone does not attach receiving-world context.");
            Assert.AreEqual(before, Parses);
            manager.CachedZones[cave] = zone;
            Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(before, Parses);
        }

        [TestCase(true)] [TestCase(false)]
        public void MissingMapArraysRejectImmediatelyAndRecoverWithoutStaleAnswer(bool tiles)
        {
            var zone = Attach(cave); Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); int before = Parses;
            var savedTiles = manager.WorldMap.Tiles; var savedPois = manager.WorldMap.POIs;
            try
            {
                if (tiles) manager.WorldMap.Tiles = null; else manager.WorldMap.POIs = null;
                Assert.IsFalse(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(before, Parses);
            }
            finally { manager.WorldMap.Tiles = savedTiles; manager.WorldMap.POIs = savedPois; }
            Assert.IsTrue(BiomeCropRecipes.IsOrdinaryCave(zone)); Assert.AreEqual(before, Parses);
        }
    }
}
