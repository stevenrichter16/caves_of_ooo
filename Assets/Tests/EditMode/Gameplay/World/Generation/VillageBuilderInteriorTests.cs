using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M4 — verifies that VillageBuilder.BuildRoom tags interior floor cells
    /// with <see cref="Cell.IsInterior"/>=true while leaving wall (edge)
    /// cells as exterior. These tests are the regression safety net for the
    /// MoveToInterior/ExteriorGoal predicate (which keys off IsInterior).
    /// </summary>
    public class VillageBuilderInteriorTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        [Test]
        public void BuildZone_InteriorFloorCell_HasIsInteriorTrue()
        {
            var poi = new PointOfInterest(POIType.Village, "Starting Village", "Villagers");
            var builder = new VillageBuilder(BiomeType.Cave, poi);
            var zone = new Zone("Overworld.10.10.0");

            Assert.IsTrue(builder.BuildZone(zone, _factory, new Random(42)));

            // Find ANY cell that has a StoneFloor entity — that's one of the
            // interior cells BuildRoom just painted. Assert it was tagged.
            bool foundAnInteriorFloor = false;
            for (int x = 0; x < Zone.Width && !foundAnInteriorFloor; x++)
            {
                for (int y = 0; y < Zone.Height && !foundAnInteriorFloor; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell == null) continue;
                    bool hasStoneFloor = false;
                    foreach (var obj in cell.Objects)
                    {
                        if (obj != null && obj.BlueprintName == "StoneFloor")
                        {
                            hasStoneFloor = true;
                            break;
                        }
                    }
                    if (!hasStoneFloor) continue;

                    Assert.IsTrue(cell.IsInterior,
                        $"StoneFloor cell ({x},{y}) should be tagged IsInterior=true.");
                    foundAnInteriorFloor = true;
                }
            }
            Assert.IsTrue(foundAnInteriorFloor, "Expected VillageBuilder to place at least one StoneFloor.");
        }

        [Test]
        public void BuildZone_WallCell_HasIsInteriorFalse()
        {
            var poi = new PointOfInterest(POIType.Village, "Starting Village", "Villagers");
            var builder = new VillageBuilder(BiomeType.Cave, poi);
            var zone = new Zone("Overworld.10.10.0");

            Assert.IsTrue(builder.BuildZone(zone, _factory, new Random(42)));

            // Find a cell that has a Wall-tagged entity (Solid with no
            // StoneFloor). BuildRoom only tags non-edge cells as interior;
            // wall cells should stay IsInterior=false.
            bool foundAWall = false;
            for (int x = 0; x < Zone.Width && !foundAWall; x++)
            {
                for (int y = 0; y < Zone.Height && !foundAWall; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell == null) continue;
                    if (!cell.IsSolid()) continue;
                    // Must be from the village-building wall phase, not
                    // the cave wall fill outside the village. Easiest
                    // proxy: its 4-neighbor is a StoneFloor cell (i.e.
                    // this wall borders an interior room).
                    bool bordersInterior = false;
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        var nCell = zone.GetCell(x + dx, y + dy);
                        if (nCell == null) continue;
                        foreach (var obj in nCell.Objects)
                        {
                            if (obj != null && obj.BlueprintName == "StoneFloor")
                            {
                                bordersInterior = true;
                                break;
                            }
                        }
                        if (bordersInterior) break;
                    }
                    if (!bordersInterior) continue;

                    Assert.IsFalse(cell.IsInterior,
                        $"Wall cell ({x},{y}) bordering a village interior should NOT be tagged IsInterior.");
                    foundAWall = true;
                }
            }
            Assert.IsTrue(foundAWall, "Expected at least one village-building wall cell in the zone.");
        }
    }
}
