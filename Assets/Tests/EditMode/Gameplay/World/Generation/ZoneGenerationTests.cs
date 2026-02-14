using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using System.Collections.Generic;

namespace CavesOfOoo.Tests
{
    public class ZoneGenerationTests
    {
        private EntityFactory _factory;

        private const string TestBlueprints = @"{
          ""Objects"": [
            {
              ""Name"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""?"" }] },
                { ""Name"": ""Physics"", ""Params"": [] }
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Terrain"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderLayer"", ""Value"": ""0"" }] }
              ],
              ""Stats"": [],
              ""Tags"": [{ ""Key"": ""Terrain"", ""Value"": """" }]
            },
            {
              ""Name"": ""Floor"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""floor"" },
                  { ""Key"": ""RenderString"", ""Value"": ""."" },
                  { ""Key"": ""ColorString"", ""Value"": ""&K"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Rubble"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""rubble"" },
                  { ""Key"": ""RenderString"", ""Value"": "","" },
                  { ""Key"": ""ColorString"", ""Value"": ""&y"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Creature"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderLayer"", ""Value"": ""10"" }] },
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] }
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 10, ""Min"": 0, ""Max"": 100 },
                { ""Name"": ""Speed"", ""Value"": 100, ""Min"": 25, ""Max"": 200 }
              ],
              ""Tags"": [{ ""Key"": ""Creature"", ""Value"": """" }]
            },
            {
              ""Name"": ""Snapjaw"",
              ""Inherits"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""snapjaw"" },
                  { ""Key"": ""RenderString"", ""Value"": ""s"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 15, ""Min"": 0, ""Max"": 15 }
              ],
              ""Tags"": [{ ""Key"": ""Faction"", ""Value"": ""Snapjaws"" }]
            },
            {
              ""Name"": ""SnapjawScavenger"",
              ""Inherits"": ""Snapjaw"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""snapjaw scavenger"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 20, ""Min"": 0, ""Max"": 20 }
              ],
              ""Tags"": []
            },
            {
              ""Name"": ""SnapjawHunter"",
              ""Inherits"": ""Snapjaw"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""snapjaw hunter"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 25, ""Min"": 0, ""Max"": 25 }
              ],
              ""Tags"": []
            },
            {
              ""Name"": ""Wall"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""wall"" },
                  { ""Key"": ""RenderString"", ""Value"": ""#"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""0"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] }
              ],
              ""Stats"": [],
              ""Tags"": [
                { ""Key"": ""Solid"", ""Value"": """" },
                { ""Key"": ""Wall"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""Item"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Takeable"", ""Value"": ""true"" }] }
              ],
              ""Stats"": [],
              ""Tags"": [{ ""Key"": ""Item"", ""Value"": """" }]
            },
            {
              ""Name"": ""MeleeWeapon"",
              ""Inherits"": ""Item"",
              ""Parts"": [],
              ""Stats"": [],
              ""Tags"": [{ ""Key"": ""MeleeWeapon"", ""Value"": """" }]
            },
            {
              ""Name"": ""Dagger"",
              ""Inherits"": ""MeleeWeapon"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""dagger"" },
                  { ""Key"": ""RenderString"", ""Value"": ""/"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""5"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""LongSword"",
              ""Inherits"": ""MeleeWeapon"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""long sword"" },
                  { ""Key"": ""RenderString"", ""Value"": ""/"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""5"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Stalagmite"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""stalagmite"" },
                  { ""Key"": ""RenderString"", ""Value"": ""^"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""1"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] }
              ],
              ""Stats"": [],
              ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" }]
            }
          ]
        }";

        [SetUp]
        public void SetUp()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprints);
        }

        // ========================
        // CellularAutomata Tests
        // ========================

        [Test]
        public void CellularAutomata_Generate_ProducesWallsAndOpenSpace()
        {
            var ca = new CellularAutomata(80, 25);
            ca.Generate(new System.Random(12345));

            int walls = 0, open = 0;
            for (int x = 0; x < 80; x++)
                for (int y = 0; y < 25; y++)
                    if (ca.IsWall(x, y)) walls++; else open++;

            Assert.Greater(walls, 0, "Should have some walls");
            Assert.Greater(open, 0, "Should have some open space");
        }

        [Test]
        public void CellularAutomata_Borders_AreAlwaysWalls()
        {
            var ca = new CellularAutomata(80, 25);
            ca.SeedBorders = true;
            ca.Generate(new System.Random(42));

            for (int x = 0; x < 80; x++)
            {
                Assert.IsTrue(ca.IsWall(x, 0), $"Top border at ({x},0)");
                Assert.IsTrue(ca.IsWall(x, 24), $"Bottom border at ({x},24)");
            }
            for (int y = 0; y < 25; y++)
            {
                Assert.IsTrue(ca.IsWall(0, y), $"Left border at (0,{y})");
                Assert.IsTrue(ca.IsWall(79, y), $"Right border at (79,{y})");
            }
        }

        [Test]
        public void CellularAutomata_Deterministic_SameSeedSameResult()
        {
            var ca1 = new CellularAutomata(80, 25);
            ca1.Generate(new System.Random(99));
            var ca2 = new CellularAutomata(80, 25);
            ca2.Generate(new System.Random(99));

            for (int x = 0; x < 80; x++)
                for (int y = 0; y < 25; y++)
                    Assert.AreEqual(ca1.Cells[x, y], ca2.Cells[x, y],
                        $"Mismatch at ({x},{y})");
        }

        [Test]
        public void CellularAutomata_DifferentSeeds_DifferentResults()
        {
            var ca1 = new CellularAutomata(80, 25);
            ca1.Generate(new System.Random(1));
            var ca2 = new CellularAutomata(80, 25);
            ca2.Generate(new System.Random(2));

            bool anyDifferent = false;
            for (int x = 2; x < 78; x++)
                for (int y = 2; y < 23; y++)
                    if (ca1.Cells[x, y] != ca2.Cells[x, y])
                        anyDifferent = true;

            Assert.IsTrue(anyDifferent, "Different seeds should produce different caves");
        }

        // ========================
        // SimpleNoise Tests
        // ========================

        [Test]
        public void SimpleNoise_GenerateField_ValuesInRange()
        {
            var field = SimpleNoise.GenerateField(80, 25, new System.Random(42));
            for (int x = 0; x < 80; x++)
                for (int y = 0; y < 25; y++)
                {
                    Assert.GreaterOrEqual(field[x, y], 0f, $"Value at ({x},{y}) below 0");
                    Assert.LessOrEqual(field[x, y], 1f, $"Value at ({x},{y}) above 1");
                }
        }

        [Test]
        public void SimpleNoise_Deterministic()
        {
            var f1 = SimpleNoise.GenerateField(80, 25, new System.Random(42));
            var f2 = SimpleNoise.GenerateField(80, 25, new System.Random(42));

            for (int x = 0; x < 80; x++)
                for (int y = 0; y < 25; y++)
                    Assert.AreEqual(f1[x, y], f2[x, y], 0.0001f, $"Mismatch at ({x},{y})");
        }

        // ========================
        // CaveBuilder Tests
        // ========================

        [Test]
        public void CaveBuilder_ProducesWallsAndOpenCells()
        {
            var zone = new Zone("test");
            var builder = new CaveBuilder();
            builder.BuildZone(zone, _factory, new System.Random(42));

            int walls = 0, passable = 0;
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsWall()) walls++;
                if (cell.IsPassable()) passable++;
            });

            Assert.Greater(walls, 0, "Cave should have walls");
            Assert.Greater(passable, 100, "Cave should have substantial open space");
        }

        [Test]
        public void CaveBuilder_FloorAndRubblePlaced()
        {
            var zone = new Zone("test");
            var builder = new CaveBuilder();
            builder.BuildZone(zone, _factory, new System.Random(42));

            var floors = zone.GetEntitiesWithTag("Terrain");
            Assert.Greater(floors.Count, 0, "Should have terrain entities (floor/rubble)");
        }

        // ========================
        // ConnectivityBuilder Tests
        // ========================

        [Test]
        public void ConnectivityBuilder_AllOpenCellsReachable()
        {
            var zone = new Zone("test");
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new CaveBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            pipeline.Generate(zone, _factory, new System.Random(42));

            // Find first passable cell
            int sx = -1, sy = -1;
            for (int x = 0; x < Zone.Width && sx < 0; x++)
                for (int y = 0; y < Zone.Height && sx < 0; y++)
                    if (zone.GetCell(x, y).IsPassable())
                    { sx = x; sy = y; }

            Assert.Greater(sx, -1, "Should have at least one passable cell");

            var reachable = ConnectivityBuilder.FloodFill(zone, sx, sy);

            // Every passable cell should be reachable
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsPassable())
                    Assert.IsTrue(reachable[x, y],
                        $"Passable cell ({x},{y}) should be reachable");
            });
        }

        // ========================
        // PopulationTable Tests
        // ========================

        [Test]
        public void PopulationTable_Roll_ProducesBlueprintNames()
        {
            var table = PopulationTable.CaveTier1();
            var results = table.Roll(new System.Random(42));
            Assert.Greater(results.Count, 0, "Should produce at least some entities");
        }

        [Test]
        public void PopulationTable_Roll_Deterministic()
        {
            var table = PopulationTable.CaveTier1();
            var r1 = table.Roll(new System.Random(99));
            var r2 = table.Roll(new System.Random(99));
            CollectionAssert.AreEqual(r1, r2);
        }

        // ========================
        // PopulationBuilder Tests
        // ========================

        [Test]
        public void PopulationBuilder_PlacesCreaturesInOpenCells()
        {
            var zone = new Zone("test");
            var pipeline = ZoneGenerationPipeline.CreateCavePipeline(PopulationTable.CaveTier1());
            pipeline.Generate(zone, _factory, new System.Random(42));

            var creatures = zone.GetEntitiesWithTag("Creature");
            Assert.Greater(creatures.Count, 0, "Should have placed some creatures");

            foreach (var creature in creatures)
            {
                var cell = zone.GetEntityCell(creature);
                Assert.IsFalse(cell.IsWall(),
                    $"Creature at ({cell.X},{cell.Y}) should not be in a wall");
            }
        }

        // ========================
        // Full Pipeline Tests
        // ========================

        [Test]
        public void FullPipeline_DeterministicWithSameSeed()
        {
            var zone1 = new Zone("test1");
            var zone2 = new Zone("test2");
            var p1 = ZoneGenerationPipeline.CreateCavePipeline();
            var p2 = ZoneGenerationPipeline.CreateCavePipeline();
            p1.Generate(zone1, _factory, new System.Random(42));
            p2.Generate(zone2, _factory, new System.Random(42));

            zone1.ForEachCell((cell1, x, y) =>
            {
                var cell2 = zone2.GetCell(x, y);
                Assert.AreEqual(cell1.IsWall(), cell2.IsWall(),
                    $"Wall mismatch at ({x},{y})");
            });
        }

        [Test]
        public void FullPipeline_EdgesHavePassableCells()
        {
            // Like Qud: no border walls, but ConnectivityBuilder ensures
            // at least one passable cell on each edge for zone transitions
            var zone = new Zone("test");
            var pipeline = ZoneGenerationPipeline.CreateCavePipeline();
            pipeline.Generate(zone, _factory, new System.Random(42));

            bool hasNorth = false, hasSouth = false, hasEast = false, hasWest = false;
            for (int x = 0; x < Zone.Width; x++)
            {
                if (zone.GetCell(x, 0).IsPassable()) hasNorth = true;
                if (zone.GetCell(x, Zone.Height - 1).IsPassable()) hasSouth = true;
            }
            for (int y = 0; y < Zone.Height; y++)
            {
                if (zone.GetCell(0, y).IsPassable()) hasWest = true;
                if (zone.GetCell(Zone.Width - 1, y).IsPassable()) hasEast = true;
            }
            Assert.IsTrue(hasNorth, "North edge should have at least one passable cell");
            Assert.IsTrue(hasSouth, "South edge should have at least one passable cell");
            Assert.IsTrue(hasEast, "East edge should have at least one passable cell");
            Assert.IsTrue(hasWest, "West edge should have at least one passable cell");
        }

        // ========================
        // ZoneManager Tests
        // ========================

        [Test]
        public void ZoneManager_GetZone_GeneratesAndCaches()
        {
            var manager = new ZoneManager(_factory, worldSeed: 42);
            var zone = manager.GetZone("CaveLevel_1");
            Assert.IsNotNull(zone);
            Assert.AreEqual("CaveLevel_1", zone.ZoneID);
            Assert.AreEqual(1, manager.CachedZoneCount);

            var zone2 = manager.GetZone("CaveLevel_1");
            Assert.AreSame(zone, zone2, "Second call should return cached instance");
        }

        [Test]
        public void ZoneManager_DifferentZoneIDs_DifferentZones()
        {
            var manager = new ZoneManager(_factory, worldSeed: 42);
            var z1 = manager.GetZone("CaveLevel_1");
            var z2 = manager.GetZone("CaveLevel_2");
            Assert.AreNotSame(z1, z2);
            Assert.AreEqual(2, manager.CachedZoneCount);
        }

        [Test]
        public void ZoneManager_SameSeed_DeterministicGeneration()
        {
            var m1 = new ZoneManager(_factory, worldSeed: 42);
            var m2 = new ZoneManager(_factory, worldSeed: 42);
            var z1 = m1.GetZone("CaveLevel_1");
            var z2 = m2.GetZone("CaveLevel_1");

            z1.ForEachCell((cell1, x, y) =>
            {
                var cell2 = z2.GetCell(x, y);
                Assert.AreEqual(cell1.IsWall(), cell2.IsWall(),
                    $"Wall mismatch at ({x},{y})");
            });
        }

        [Test]
        public void ZoneManager_UnloadZone_RemovesFromCache()
        {
            var manager = new ZoneManager(_factory, worldSeed: 42);
            manager.GetZone("CaveLevel_1");
            Assert.AreEqual(1, manager.CachedZoneCount);

            manager.UnloadZone("CaveLevel_1");
            Assert.AreEqual(0, manager.CachedZoneCount);
        }
    }
}
