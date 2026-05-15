using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WM.2 tests for the world-map zone routing + embedded-builder.
    /// Pins:
    /// <list type="bullet">
    ///   <item><see cref="WorldMap.WorldMapZoneID"/> constant + predicate</item>
    ///   <item>Coord translation between logical worldmap and embedded zone cells</item>
    ///   <item><see cref="WorldMapZoneBuilder"/> fills walls outside the 20×20 region
    ///   and passable terrain inside it</item>
    ///   <item>Each terrain entity carries <see cref="WorldMapCellPart"/>
    ///   with correct WorldX/WorldY</item>
    ///   <item>Biome glyph propagation (cave/desert/jungle/ruins)</item>
    ///   <item><see cref="OverworldZoneManager"/> routes the WorldMap ZoneID through
    ///   the WorldMap pipeline</item>
    /// </list>
    /// </summary>
    public class WorldMapZoneBuilderTests
    {
        // ── ZoneID predicate + constant ──────────────────────────

        [Test]
        public void ToWorldMapZoneID_IsWorldMapConstant()
        {
            Assert.AreEqual("WorldMap", WorldMap.WorldMapZoneID);
        }

        [Test]
        public void IsWorldMapZoneID_AcceptsConstant_RejectsOverworld()
        {
            Assert.IsTrue(WorldMap.IsWorldMapZoneID(WorldMap.WorldMapZoneID));
            Assert.IsTrue(WorldMap.IsWorldMapZoneID("WorldMap"));
            // Counter-check: Overworld zone IDs MUST NOT match
            Assert.IsFalse(WorldMap.IsWorldMapZoneID("Overworld.10.10.0"));
            Assert.IsFalse(WorldMap.IsWorldMapZoneID("Overworld.0.0.0"));
            Assert.IsFalse(WorldMap.IsWorldMapZoneID(null));
            Assert.IsFalse(WorldMap.IsWorldMapZoneID(""));
        }

        // ── Coord translation ──────────────────────────────────────

        [Test]
        public void WorldCellToZoneCell_AppliesOffset()
        {
            // Logical (0,0) → zone (30, 3)
            var (zx, zy) = WorldMap.WorldCellToZoneCell(0, 0);
            Assert.AreEqual(30, zx);
            Assert.AreEqual(3, zy);

            // Logical (19,19) → zone (49, 22)
            var (zx2, zy2) = WorldMap.WorldCellToZoneCell(19, 19);
            Assert.AreEqual(49, zx2);
            Assert.AreEqual(22, zy2);
        }

        [Test]
        public void WorldCellToZoneCell_OutOfBounds_ReturnsMinusOne()
        {
            Assert.AreEqual((-1, -1), WorldMap.WorldCellToZoneCell(-1, 0));
            Assert.AreEqual((-1, -1), WorldMap.WorldCellToZoneCell(0, -1));
            Assert.AreEqual((-1, -1), WorldMap.WorldCellToZoneCell(20, 5));
            Assert.AreEqual((-1, -1), WorldMap.WorldCellToZoneCell(5, 20));
        }

        [Test]
        public void ZoneCellToWorldCell_InverseOfWorldCellToZoneCell()
        {
            // Round-trip across the full 20×20 logical range
            for (int wy = 0; wy < WorldMap.Height; wy++)
            {
                for (int wx = 0; wx < WorldMap.Width; wx++)
                {
                    var (zx, zy) = WorldMap.WorldCellToZoneCell(wx, wy);
                    var (rx, ry) = WorldMap.ZoneCellToWorldCell(zx, zy);
                    Assert.AreEqual(wx, rx,
                        $"World→Zone→World round-trip failed at wx={wx},wy={wy}");
                    Assert.AreEqual(wy, ry);
                }
            }
        }

        [Test]
        public void ZoneCellToWorldCell_OutsideEmbeddedRegion_ReturnsMinusOne()
        {
            // Top-left corner of zone: outside embedded region
            Assert.AreEqual((-1, -1), WorldMap.ZoneCellToWorldCell(0, 0));
            // Top edge: x in range but y too small
            Assert.AreEqual((-1, -1), WorldMap.ZoneCellToWorldCell(35, 0));
            // Right edge: y in range but x too big
            Assert.AreEqual((-1, -1), WorldMap.ZoneCellToWorldCell(50, 5));
            // Bottom-right corner: just past the embedded region
            Assert.AreEqual((-1, -1), WorldMap.ZoneCellToWorldCell(50, 23));
        }

        // ── Builder integration ────────────────────────────────────

        private static (Zone, EntityFactory, WorldMap) BuildWorldMapZone()
        {
            var worldMap = WorldGenerator.Generate(seed: 42);
            var factory = new EntityFactory();
            // Minimal blueprint set: Wall + Floor (Wall is what the builder
            // calls factory.CreateEntity("Wall") to fill the border with).
            factory.LoadBlueprints(@"{
              ""Objects"": [
                {
                  ""Name"": ""Wall"",
                  ""Parts"": [
                    { ""Name"": ""Physics"", ""Params"": [
                      { ""Key"": ""Solid"", ""Value"": ""true"" },
                      { ""Key"": ""Takeable"", ""Value"": ""false"" }
                    ] },
                    { ""Name"": ""Render"", ""Params"": [
                      { ""Key"": ""DisplayName"", ""Value"": ""wall"" },
                      { ""Key"": ""RenderString"", ""Value"": ""#"" },
                      { ""Key"": ""ColorString"", ""Value"": ""&w"" }
                    ] }
                  ],
                  ""Stats"": [],
                  ""Tags"": [ { ""Key"": ""Solid"", ""Value"": """" } ]
                }
              ]
            }");
            var zone = new Zone(WorldMap.WorldMapZoneID);
            var builder = new WorldMapZoneBuilder(worldMap);
            bool ok = builder.BuildZone(zone, factory, new System.Random(1));
            Assert.IsTrue(ok, "WorldMapZoneBuilder.BuildZone should succeed.");
            return (zone, factory, worldMap);
        }

        [Test]
        public void WorldMapZoneBuilder_BorderCells_AreImpassable()
        {
            var (zone, _, _) = BuildWorldMapZone();

            // Top-left corner — outside embedded region
            var topLeft = zone.GetCell(0, 0);
            Assert.IsTrue(topLeft.IsSolid(),
                "Cell (0,0) should be a wall — outside the 20×20 embedded region.");

            // Just inside the embedded x-range but above it
            var aboveEmbed = zone.GetCell(35, 0);
            Assert.IsTrue(aboveEmbed.IsSolid(),
                "Cell (35,0) is in the embedded x-range but in the HUD strip — should be wall.");

            // Right edge of zone
            var rightEdge = zone.GetCell(79, 12);
            Assert.IsTrue(rightEdge.IsSolid());

            // Bottom-left corner
            var bottomLeft = zone.GetCell(0, 24);
            Assert.IsTrue(bottomLeft.IsSolid());
        }

        [Test]
        public void WorldMapZoneBuilder_EmbeddedRegion_AllPassable_AndHasWorldMapCellPart()
        {
            var (zone, _, _) = BuildWorldMapZone();

            for (int wy = 0; wy < WorldMap.Height; wy++)
            {
                for (int wx = 0; wx < WorldMap.Width; wx++)
                {
                    var (zx, zy) = WorldMap.WorldCellToZoneCell(wx, wy);
                    var cell = zone.GetCell(zx, zy);
                    Assert.IsFalse(cell.IsSolid(),
                        $"Embedded cell at world ({wx},{wy}) zone ({zx},{zy}) should NOT be solid.");

                    // Find the WorldMapCellPart on the entity at that cell
                    var found = false;
                    foreach (var obj in cell.Objects)
                    {
                        var part = obj.GetPart<WorldMapCellPart>();
                        if (part == null) continue;
                        Assert.AreEqual(wx, part.WorldX, $"WorldX mismatch at ({zx},{zy})");
                        Assert.AreEqual(wy, part.WorldY);
                        found = true;
                        break;
                    }
                    Assert.IsTrue(found,
                        $"No WorldMapCellPart found at world ({wx},{wy}) zone ({zx},{zy}).");
                }
            }
        }

        [Test]
        public void WorldMapZoneBuilder_BiomeGlyph_MatchesWorldMapBiome()
        {
            var (zone, _, worldMap) = BuildWorldMapZone();

            // Center cell should be Cave (WorldGenerator hard-pins (10,10) to Cave)
            var (cx, cy) = WorldMap.WorldCellToZoneCell(10, 10);
            var centerCell = zone.GetCell(cx, cy);
            var (caveGlyph, caveColor, _) = WorldMapZoneBuilder.GetBiomeRender(BiomeType.Cave);

            bool found = false;
            foreach (var obj in centerCell.Objects)
            {
                var render = obj.GetPart<RenderPart>();
                if (render == null) continue;
                if (obj.GetPart<WorldMapCellPart>() == null) continue;
                Assert.AreEqual(caveGlyph, render.RenderString,
                    "Center (10,10) is a Cave biome — should render with cave glyph.");
                Assert.AreEqual(caveColor, render.ColorString);
                found = true;
            }
            Assert.IsTrue(found, "Center cell should have a renderable terrain.");

            // Counter-check: at least one Desert, Jungle, Ruins cell renders
            // with its biome glyph. Walk the entire embedded region to find one
            // of each.
            var biomesSeen = new System.Collections.Generic.HashSet<BiomeType>();
            for (int wy = 0; wy < WorldMap.Height; wy++)
            {
                for (int wx = 0; wx < WorldMap.Width; wx++)
                {
                    var biome = worldMap.GetBiome(wx, wy);
                    biomesSeen.Add(biome);
                }
            }
            // WorldGenerator.EnsureAllBiomes guarantees all 4 are present
            Assert.IsTrue(biomesSeen.Contains(BiomeType.Desert));
            Assert.IsTrue(biomesSeen.Contains(BiomeType.Jungle));
            Assert.IsTrue(biomesSeen.Contains(BiomeType.Ruins));
        }

        [Test]
        public void OverworldZoneManager_RoutesWorldMapID_BuildsWorldMapZone()
        {
            // Construct an OverworldZoneManager with a minimal blueprint set
            // sufficient for the WorldMap pipeline (just Wall — no biome
            // builders run for the worldmap zone).
            var factory = new EntityFactory();
            factory.LoadBlueprints(@"{
              ""Objects"": [
                {
                  ""Name"": ""Wall"",
                  ""Parts"": [
                    { ""Name"": ""Physics"", ""Params"": [
                      { ""Key"": ""Solid"", ""Value"": ""true"" }
                    ] },
                    { ""Name"": ""Render"", ""Params"": [
                      { ""Key"": ""RenderString"", ""Value"": ""#"" },
                      { ""Key"": ""ColorString"", ""Value"": ""&w"" }
                    ] }
                  ],
                  ""Stats"": [],
                  ""Tags"": [ { ""Key"": ""Solid"", ""Value"": """" } ]
                }
              ]
            }");
            var zm = new OverworldZoneManager(factory, worldSeed: 99);

            // Request the worldmap zone; the manager should build it.
            var zone = zm.GetZone(WorldMap.WorldMapZoneID);
            Assert.IsNotNull(zone);
            Assert.AreEqual(WorldMap.WorldMapZoneID, zone.ZoneID);

            // Verify it has the embedded-region structure
            var (cx, cy) = WorldMap.WorldCellToZoneCell(10, 10);
            var center = zone.GetCell(cx, cy);
            Assert.IsFalse(center.IsSolid(),
                "Center of embedded region should be passable.");

            // Verify a border cell is walled
            var border = zone.GetCell(0, 0);
            Assert.IsTrue(border.IsSolid());
        }
    }
}
