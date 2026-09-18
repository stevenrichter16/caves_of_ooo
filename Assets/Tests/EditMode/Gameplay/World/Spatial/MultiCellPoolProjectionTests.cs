using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Pool terrain must match the source's committed physical body and lifetime.</summary>
    public sealed class MultiCellPoolProjectionTests
    {
        const string Square = "0,0;1,0;0,1;1,1";
        [SetUp] public void SetUp()
        {
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize("{\"Liquids\":[{\"Id\":\"oil\",\"DisplayName\":\"oil\",\"Slippery\":true,\"SlipChance\":100},{\"Id\":\"water\",\"Slippery\":false}]}");
        }
        [TearDown] public void TearDown() { LiquidRegistry.ResetForTests(); }
        static Entity Pool(Zone zone, string shape = Square, int x = 10, int y = 10, string liquid = "oil")
        {
            var entity = MultiCellSpatialTests.Body(shape, false);
            entity.AddPart(new LiquidPoolPart { LiquidId = liquid, Volume = 10 });
            Assert.IsTrue(zone.AddEntity(entity, x, y));
            return entity;
        }
        static void ProjectExpectedBodyForCleanupProbe(Zone zone, Entity pool)
        {
            // Isolate cleanup from the independently tested add-time projection:
            // this is the same permanent tile state a complete native pool owns.
            foreach (var cell in zone.GetOccupiedCells(pool))
                zone.TileState.WriteCoating(cell.X, cell.Y, pool.GetPart<LiquidPoolPart>().LiquidId, ZoneTileState.Permanent);
        }
        static void Oily(Zone zone, int x, int y, bool expected = true)
            => Assert.AreEqual(expected, zone.TileState.HasCoating(x, y, "oil"), "oil at " + x + "," + y);

        [Test] public void OrdinarySingleCellPoolStillProjectsAndUnprojectsOnlyItsCell()
        {
            var zone = new Zone(); var pool = Pool(zone, null);
            Oily(zone, 10, 10); Oily(zone, 11, 10, false);
            Assert.IsTrue(zone.RemoveEntity(pool)); Oily(zone, 10, 10, false);
        }
        [Test] public void NewWidePoolImmediatelyProjectsPermanentLiquidAtEveryPhysicalCell()
        {
            var zone = new Zone(); var pool = Pool(zone);
            foreach (var cell in zone.GetOccupiedCells(pool))
                Assert.AreEqual(ZoneTileState.Permanent, zone.TileState.CoatingTurns(cell.X, cell.Y, "oil"));
            Oily(zone, 12, 11, false);
        }
        [Test] public void HollowPoolDoesNotWetCanonicalAnchorOrInteriorHole()
        {
            var zone = new Zone(); var pool = Pool(zone, "1,0;0,1;2,1;1,2");
            Oily(zone, 10, 10, false); Oily(zone, 11, 11, false);
            foreach (var cell in zone.GetOccupiedCells(pool)) Oily(zone, cell.X, cell.Y);
        }
        [TestCase(false)] [TestCase(true)]
        public void RelocatingPoolClearsDepartedBodyAndProjectsNewBody(bool throughAdd)
        {
            var zone = new Zone(); var pool = Pool(zone); ProjectExpectedBodyForCleanupProbe(zone, pool);
            Assert.IsTrue(throughAdd ? zone.AddEntity(pool, 11, 10) : zone.MoveEntity(pool, 11, 10));
            Oily(zone, 10, 10, false); Oily(zone, 10, 11, false);
            foreach (var cell in zone.GetOccupiedCells(pool)) Oily(zone, cell.X, cell.Y);
        }
        [Test] public void RemovingPoolClearsWholeProjectionAndKeepsDifferentLiquidAndResidue()
        {
            var zone = new Zone(); var pool = Pool(zone); ProjectExpectedBodyForCleanupProbe(zone, pool);
            zone.TileState.WriteCoating(11, 11, "water", 17); zone.TileState.WriteResidue(11, 11, "ash", 8);
            Assert.IsTrue(zone.RemoveEntity(pool));
            for (int y = 10; y <= 11; y++) for (int x = 10; x <= 11; x++) Oily(zone, x, y, false);
            Assert.AreEqual(17, zone.TileState.CoatingTurns(11, 11, "water"));
            Assert.IsTrue(zone.TileState.HasResidue(11, 11, "ash"));
        }
        [Test] public void RemovingOnePoolRetainsAnotherOwnersProjectionOnTheSharedCell()
        {
            var zone = new Zone(); var first = Pool(zone, "0,0;1,0"); var other = Pool(zone, "0,0;1,0", 9, 10);
            ProjectExpectedBodyForCleanupProbe(zone, first); ProjectExpectedBodyForCleanupProbe(zone, other);
            Assert.IsTrue(zone.RemoveEntity(first)); Oily(zone, 10, 10); Oily(zone, 11, 10, false); Oily(zone, 9, 10);
            Assert.IsTrue(zone.GetOccupants(10, 10).Contains(other));
            Assert.IsTrue(zone.RemoveEntity(other)); Oily(zone, 10, 10, false); Oily(zone, 9, 10, false);
        }
        [Test] public void ChangingShapeRemovesDepartedProjectionAndKeepsOverlappingPhysicalCells()
        {
            var zone = new Zone(); var pool = Pool(zone); ProjectExpectedBodyForCleanupProbe(zone, pool);
            Assert.IsTrue(zone.TryChangeFootprint(pool, "1,0;2,0"));
            Oily(zone, 10, 10, false); Oily(zone, 10, 11, false); Oily(zone, 11, 11, false);
            Oily(zone, 11, 10); Oily(zone, 12, 10);
        }
        [Test] public void AttachingHollowFootprintRemovesOldCanonicalProjectionWithoutTouchingUnrelatedSpill()
        {
            var zone = new Zone(); var pool = Pool(zone, null); zone.TileState.WriteCoating(12, 10, "oil", 7);
            pool.AddPart(new SpatialFootprintPart { CellsRaw = "1,0;1,1" });
            Oily(zone, 10, 10, false); Oily(zone, 11, 10); Oily(zone, 11, 11);
            Assert.AreEqual(7, zone.TileState.CoatingTurns(12, 10, "oil"));
        }
        [Test] public void RemovingHollowFootprintProjectsItsRestoredSingleCellAndClearsOldBody()
        {
            var zone = new Zone(); var pool = Pool(zone, "1,0;1,1");
            zone.TileState.RemoveCoating(10, 10, "oil"); ProjectExpectedBodyForCleanupProbe(zone, pool);
            Assert.IsTrue(pool.RemovePart(pool.GetPart<SpatialFootprintPart>()));
            Oily(zone, 10, 10); Oily(zone, 11, 10, false); Oily(zone, 11, 11, false);
        }
        [Test] public void RejectedBodyChangePreservesEveryOldProjectionAndExternalWriting()
        {
            var zone = new Zone(); var pool = Pool(zone); ProjectExpectedBodyForCleanupProbe(zone, pool);
            Assert.IsTrue(zone.AddEntity(MultiCellSpatialTests.Body(null), 12, 10)); zone.TileState.WriteCoating(12, 10, "water", 9);
            Assert.IsFalse(zone.TryChangeFootprint(pool, "0,0;1,0;2,0"));
            foreach (var cell in zone.GetOccupiedCells(pool)) Oily(zone, cell.X, cell.Y);
            Assert.AreEqual(9, zone.TileState.CoatingTurns(12, 10, "water")); Oily(zone, 12, 10, false);
        }
        [Test] public void SwappingWidePoolsMovesBothCompleteLiquidProjections()
        {
            var zone = new Zone(); var oil = Pool(zone, "0,0;1,0"); var water = Pool(zone, "0,0;0,1", 20, 10, "water");
            ProjectExpectedBodyForCleanupProbe(zone, oil); ProjectExpectedBodyForCleanupProbe(zone, water);
            Assert.IsTrue(zone.TrySwapEntities(oil, water));
            Oily(zone, 10, 10, false); Oily(zone, 11, 10, false); Oily(zone, 20, 10); Oily(zone, 21, 10);
            Assert.IsFalse(zone.TileState.HasCoating(20, 10, "water")); Assert.IsFalse(zone.TileState.HasCoating(20, 11, "water"));
            Assert.IsTrue(zone.TileState.HasCoating(10, 10, "water")); Assert.IsTrue(zone.TileState.HasCoating(10, 11, "water"));
        }
        [Test] public void ZoneTransferMovesWholeProjectionAndFailedTransferLeavesItUntouched()
        {
            var source = new Zone(); var destination = new Zone(); var pool = Pool(source); ProjectExpectedBodyForCleanupProbe(source, pool);
            Assert.IsFalse(source.TryTransferEntityTo(pool, destination, 79, 24));
            foreach (var cell in source.GetOccupiedCells(pool)) Oily(source, cell.X, cell.Y);
            Assert.IsTrue(source.TryTransferEntityTo(pool, destination, 20, 10));
            for (int y = 10; y <= 11; y++) for (int x = 10; x <= 11; x++) Oily(source, x, y, false);
            foreach (var cell in destination.GetOccupiedCells(pool)) Oily(destination, cell.X, cell.Y);
        }
        [TestCase("0,0;1,0;0,1;1,1", 11, 11, true)]
        [TestCase("1,0;1,1", 10, 10, false)]
        [TestCase("0,0;1,0;0,1;1,1", 12, 11, false)]
        public void NativeMotionCandidateReadsSlipperyGroundUnderEveryRealFootButNotHoles(string shape, int oilX, int oilY, bool slippery)
        {
            var zone = new Zone(); var actor = MultiCellSpatialTests.Body(shape);
            zone.TileState.WriteCoating(oilX, oilY, "oil", 3);
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.MultiCellPilotNativeAudit")).FirstOrDefault(t => t != null);
            Assert.NotNull(type); var method = type.GetMethod("BodyHasSlipperyGroundAt", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Native deterministic displacement must read current whole-body ground before choosing a destination.");
            Assert.AreEqual(slippery, method.Invoke(null, new object[] { zone, actor, 10, 10 }));
        }
    }
}
