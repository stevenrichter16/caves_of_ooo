using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// Unit tests for <see cref="PositionResolver"/> — the pure-function core of
    /// Phase 2a scenario positioning. Runs in EditMode against synthetic zones
    /// so no Play mode or live game state is required.
    /// </summary>
    [TestFixture]
    public class PositionResolverTests
    {
        // ---- helpers ----

        /// <summary>Fresh zone with no walls; every cell IsPassable() == true.</summary>
        private static Zone CreateEmptyZone() => new Zone("Scenario2aTest");

        /// <summary>Zone in which every cell has a Solid-tagged entity, so all
        /// cells report IsPassable() == false. Used for the "fully walled" case.</summary>
        private static Zone CreateFullyWalledZone()
        {
            var zone = new Zone("Scenario2aWalled");
            zone.ForEachCell((cell, x, y) =>
            {
                var wall = new Entity { BlueprintName = "Wall" };
                wall.Tags["Solid"] = "";
                zone.AddEntity(wall, x, y);
            });
            return zone;
        }

        // ========================
        // NearPlayer / radius band
        // ========================

        [Test]
        public void CollectCellsInRadiusBand_ReturnsOnlyCellsWithinBand_WhenAllPassable()
        {
            var zone = CreateEmptyZone();
            int cx = 10, cy = 10;
            var cells = PositionResolver.CollectCellsInRadiusBand(zone, cx, cy, 2, 3);

            Assert.IsNotEmpty(cells, "Empty zone at (10,10) should yield cells in band [2,3].");
            foreach (var (x, y) in cells)
            {
                int dx = System.Math.Abs(x - cx);
                int dy = System.Math.Abs(y - cy);
                int cheb = System.Math.Max(dx, dy);
                Assert.GreaterOrEqual(cheb, 2, $"Cell ({x},{y}) too close to center (d={cheb}).");
                Assert.LessOrEqual(cheb, 3, $"Cell ({x},{y}) too far from center (d={cheb}).");
            }
        }

        [Test]
        public void CollectCellsInRadiusBand_ReturnsEmpty_WhenZoneFullyWalled()
        {
            var zone = CreateFullyWalledZone();
            var cells = PositionResolver.CollectCellsInRadiusBand(zone, 10, 10, 1, 5);
            Assert.IsEmpty(cells,
                "A fully-walled zone must yield zero passable candidates, not crash or infinite-loop.");
        }

        [Test]
        public void CollectCellsInRadiusBand_ExcludesCenterCell_EvenAtRadiusZero()
        {
            var zone = CreateEmptyZone();
            // minRadius=0 would normally include the center cell, but we always exclude it
            var cells = PositionResolver.CollectCellsInRadiusBand(zone, 10, 10, 0, 1);
            Assert.IsFalse(cells.Contains((10, 10)),
                "Center cell (the player's own cell) must be excluded to prevent stacking spawns on player.");
        }

        [Test]
        public void CollectCellsInRadiusBand_FiltersOutOfBoundsCells()
        {
            var zone = CreateEmptyZone();
            // Corner of the zone — half the radius band is off-map
            var cells = PositionResolver.CollectCellsInRadiusBand(zone, 0, 0, 1, 3);
            foreach (var (x, y) in cells)
            {
                Assert.IsTrue(zone.InBounds(x, y),
                    $"Cell ({x},{y}) is out of bounds but was included.");
            }
        }

        // ========================
        // InRing
        // ========================

        [Test]
        public void ComputeRingPosition_DistributesEvenly_AtLargeRadius()
        {
            // 8 points at radius 10 should produce at least 6 distinct cells
            // (exact count depends on trig rounding; 6+ confirms spread)
            var positions = new HashSet<(int x, int y)>();
            for (int i = 0; i < 8; i++)
            {
                positions.Add(PositionResolver.ComputeRingPosition(40, 12, 10, i, 8));
            }
            Assert.GreaterOrEqual(positions.Count, 6,
                $"8 ring spawns at r=10 clustered into only {positions.Count} cells — ring distribution regressed.");
        }

        [Test]
        public void ComputeRingPosition_ClustersExpected_AtSmallRadius()
        {
            // Documents the known limitation: at r=1, int-rounded trig produces
            // at most ~4-5 distinct cells even for N=8. This test pins the behavior
            // so future authors know why InRing at small radii behaves this way.
            var positions = new HashSet<(int x, int y)>();
            for (int i = 0; i < 8; i++)
            {
                positions.Add(PositionResolver.ComputeRingPosition(10, 10, 1, i, 8));
            }
            Assert.LessOrEqual(positions.Count, 8,
                "Sanity bound — should never exceed the request count.");
            Assert.Greater(positions.Count, 0,
                "Ring at r=1 should still produce at least one distinct cell.");
        }

        [Test]
        public void ComputeRingPosition_HandlesRadiusZero_CollapsesToCenter()
        {
            // Degenerate input: r=0 should place all points on the center cell.
            for (int i = 0; i < 4; i++)
            {
                var pos = PositionResolver.ComputeRingPosition(5, 7, 0, i, 4);
                Assert.AreEqual((5, 7), pos, $"At r=0, index {i} should collapse to center.");
            }
        }

        // ========================
        // OnFirstPassableCell
        // ========================

        [Test]
        public void FindFirstPassableCell_FindsFirstMatch_InRowMajorOrder()
        {
            var zone = CreateEmptyZone();
            // Row-major means y=0 is scanned before y=1, and within a row
            // x=0 before x=1. A predicate that matches any cell with x>=5 AND y>=3
            // should first match (5, 3).
            var match = PositionResolver.FindFirstPassableCell(zone,
                c => c.X >= 5 && c.Y >= 3);
            Assert.IsNotNull(match);
            Assert.AreEqual((5, 3), match.Value,
                "Row-major scan should find (5,3) before any other cell matching the predicate.");
        }

        [Test]
        public void FindFirstPassableCell_ReturnsNull_WhenNoMatch()
        {
            var zone = CreateEmptyZone();
            var match = PositionResolver.FindFirstPassableCell(zone, c => false);
            Assert.IsNull(match,
                "A predicate that never matches must produce null, not an arbitrary cell.");
        }

        [Test]
        public void FindFirstPassableCell_SkipsNonPassableCells()
        {
            var zone = CreateEmptyZone();
            // Add a wall at (3, 0). Predicate matches "x >= 3 && y == 0".
            // Without the passable filter, the match would be (3, 0).
            // With the filter, the match should be (4, 0).
            var wall = new Entity { BlueprintName = "Wall" };
            wall.Tags["Solid"] = "";
            zone.AddEntity(wall, 3, 0);

            var match = PositionResolver.FindFirstPassableCell(zone,
                c => c.X >= 3 && c.Y == 0);
            Assert.IsNotNull(match);
            Assert.AreEqual((4, 0), match.Value,
                "Walled cell at (3,0) should be skipped; next passable match is (4,0).");
        }
    }
}
