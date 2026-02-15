using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class UndergroundGenerationTests
    {
        private EntityFactory _factory;
        private ZoneManager _zoneManager;

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
              ""Name"": ""SandstoneWall"",
              ""Inherits"": ""Wall"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""sandstone wall"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&W"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""SandstoneFloor"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""sandstone floor"" },
                  { ""Key"": ""RenderString"", ""Value"": ""."" },
                  { ""Key"": ""ColorString"", ""Value"": ""&W"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""StairsDown"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""stairs leading down"" },
                  { ""Key"": ""RenderString"", ""Value"": "">"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&W"" }
                ]},
                { ""Name"": ""StairsDown"", ""Params"": [] }
              ],
              ""Stats"": [],
              ""Tags"": [{ ""Key"": ""StairsDown"", ""Value"": """" }]
            },
            {
              ""Name"": ""StairsUp"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""stairs leading up"" },
                  { ""Key"": ""RenderString"", ""Value"": ""<"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&W"" }
                ]},
                { ""Name"": ""StairsUp"", ""Params"": [] }
              ],
              ""Stats"": [],
              ""Tags"": [{ ""Key"": ""StairsUp"", ""Value"": """" }]
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
            _zoneManager = new ZoneManager(_factory, 42);
        }

        // ========================
        // SolidEarthBuilder Tests
        // ========================

        [Test]
        public void SolidEarthBuilder_FillsAllCells()
        {
            var zone = new Zone("Overworld.5.5.1");
            var builder = new SolidEarthBuilder("SandstoneWall");
            builder.BuildZone(zone, _factory, new System.Random(42));

            int wallCount = 0;
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsWall()) wallCount++;
            });

            Assert.AreEqual(Zone.Width * Zone.Height, wallCount,
                "SolidEarthBuilder should fill every cell with a wall");
        }

        [Test]
        public void SolidEarthBuilder_UsesCorrectBlueprint()
        {
            var zone = new Zone("Overworld.5.5.1");
            var builder = new SolidEarthBuilder("SandstoneWall");
            builder.BuildZone(zone, _factory, new System.Random(42));

            var cell = zone.GetCell(10, 10);
            bool hasSandstone = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var render = cell.Objects[i].GetPart("Render") as RenderPart;
                if (render != null && render.DisplayName == "sandstone wall")
                    hasSandstone = true;
            }
            Assert.IsTrue(hasSandstone, "Walls should use the specified blueprint");
        }

        [Test]
        public void SolidEarthBuilder_GetMaterialsForDepth_ReturnsSandstoneForShallow()
        {
            var (wall, floor) = SolidEarthBuilder.GetMaterialsForDepth(1);
            Assert.AreEqual("SandstoneWall", wall);
            Assert.AreEqual("SandstoneFloor", floor);

            (wall, floor) = SolidEarthBuilder.GetMaterialsForDepth(2);
            Assert.AreEqual("SandstoneWall", wall);
            Assert.AreEqual("SandstoneFloor", floor);
        }

        [Test]
        public void SolidEarthBuilder_GetMaterialsForDepth_ProgressesWithDepth()
        {
            var (wall3, _) = SolidEarthBuilder.GetMaterialsForDepth(3);
            Assert.AreEqual("LimestoneWall", wall3);

            var (wall5, _) = SolidEarthBuilder.GetMaterialsForDepth(5);
            Assert.AreEqual("ShaleWall", wall5);

            var (wall7, _) = SolidEarthBuilder.GetMaterialsForDepth(7);
            Assert.AreEqual("SlateWall", wall7);

            var (wall9, _) = SolidEarthBuilder.GetMaterialsForDepth(9);
            Assert.AreEqual("QuartziteWall", wall9);

            var (wall11, _) = SolidEarthBuilder.GetMaterialsForDepth(11);
            Assert.AreEqual("ObsidianWall", wall11);
        }

        // ========================
        // StrataBuilder Tests
        // ========================

        [Test]
        public void StrataBuilder_CarvesOpenSpaces()
        {
            var zone = new Zone("Overworld.5.5.1");
            // Fill with walls first
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone, _factory, new System.Random(42));

            // Now carve
            var strata = new StrataBuilder(1, "SandstoneWall", "SandstoneFloor");
            strata.BuildZone(zone, _factory, new System.Random(42));

            int walls = 0, passable = 0;
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsWall()) walls++;
                if (cell.IsPassable()) passable++;
            });

            Assert.Greater(walls, 0, "Should still have some walls");
            Assert.Greater(passable, 50, "Should have carved open spaces");
        }

        [Test]
        public void StrataBuilder_DeepCavesHaveMoreWalls()
        {
            // Shallow cave (depth 1)
            var zoneShallow = new Zone("Overworld.5.5.1");
            new SolidEarthBuilder("SandstoneWall").BuildZone(zoneShallow, _factory, new System.Random(42));
            new StrataBuilder(1, "SandstoneWall", "SandstoneFloor").BuildZone(zoneShallow, _factory, new System.Random(42));

            int shallowWalls = 0;
            zoneShallow.ForEachCell((cell, x, y) =>
            {
                if (cell.IsWall()) shallowWalls++;
            });

            // Deep cave (depth 10)
            var zoneDeep = new Zone("Overworld.5.5.10");
            new SolidEarthBuilder("SandstoneWall").BuildZone(zoneDeep, _factory, new System.Random(42));
            new StrataBuilder(10, "SandstoneWall", "SandstoneFloor").BuildZone(zoneDeep, _factory, new System.Random(42));

            int deepWalls = 0;
            zoneDeep.ForEachCell((cell, x, y) =>
            {
                if (cell.IsWall()) deepWalls++;
            });

            Assert.Greater(deepWalls, shallowWalls,
                "Deep caves should have more walls (tighter tunnels) than shallow caves");
        }

        [Test]
        public void StrataBuilder_Deterministic()
        {
            var zone1 = new Zone("Overworld.5.5.1");
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone1, _factory, new System.Random(42));
            new StrataBuilder(1, "SandstoneWall", "SandstoneFloor").BuildZone(zone1, _factory, new System.Random(99));

            var zone2 = new Zone("Overworld.5.5.1");
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone2, _factory, new System.Random(42));
            new StrataBuilder(1, "SandstoneWall", "SandstoneFloor").BuildZone(zone2, _factory, new System.Random(99));

            zone1.ForEachCell((cell1, x, y) =>
            {
                var cell2 = zone2.GetCell(x, y);
                Assert.AreEqual(cell1.IsWall(), cell2.IsWall(),
                    $"Wall mismatch at ({x},{y})");
            });
        }

        // ========================
        // StairsDownBuilder Tests
        // ========================

        [Test]
        public void StairsDownBuilder_PlacesStairs()
        {
            var zone = new Zone("Overworld.5.5.1");
            // Create passable zone first
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone, _factory, new System.Random(42));
            new StrataBuilder(1, "SandstoneWall", "SandstoneFloor").BuildZone(zone, _factory, new System.Random(42));
            new ConnectivityBuilder { FloorBlueprint = "SandstoneFloor" }.BuildZone(zone, _factory, new System.Random(42));

            var builder = new StairsDownBuilder(_zoneManager);
            builder.BuildZone(zone, _factory, new System.Random(42));

            bool foundStairs = false;
            zone.ForEachCell((cell, x, y) =>
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].HasTag("StairsDown"))
                        foundStairs = true;
                }
            });

            Assert.IsTrue(foundStairs, "StairsDownBuilder should place stairs in the zone");
        }

        [Test]
        public void StairsDownBuilder_RegistersConnection()
        {
            var zone = new Zone("Overworld.5.5.1");
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone, _factory, new System.Random(42));
            new StrataBuilder(1, "SandstoneWall", "SandstoneFloor").BuildZone(zone, _factory, new System.Random(42));
            new ConnectivityBuilder { FloorBlueprint = "SandstoneFloor" }.BuildZone(zone, _factory, new System.Random(42));

            var builder = new StairsDownBuilder(_zoneManager);
            builder.BuildZone(zone, _factory, new System.Random(42));

            var connections = _zoneManager.GetConnections("Overworld.5.5.1");
            Assert.Greater(connections.Count, 0, "Should register a connection");
            Assert.AreEqual("StairsDown", connections[0].Type);
            Assert.AreEqual("Overworld.5.5.1", connections[0].SourceZoneID);
            Assert.AreEqual("Overworld.5.5.2", connections[0].TargetZoneID);
        }

        [Test]
        public void StairsDownBuilder_PlacesInPassableCell()
        {
            var zone = new Zone("Overworld.5.5.1");
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone, _factory, new System.Random(42));
            new StrataBuilder(1, "SandstoneWall", "SandstoneFloor").BuildZone(zone, _factory, new System.Random(42));
            new ConnectivityBuilder { FloorBlueprint = "SandstoneFloor" }.BuildZone(zone, _factory, new System.Random(42));

            new StairsDownBuilder(_zoneManager).BuildZone(zone, _factory, new System.Random(42));

            zone.ForEachCell((cell, x, y) =>
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].HasTag("StairsDown"))
                    {
                        // The cell should be passable (stairs are terrain, not walls)
                        Assert.IsFalse(cell.IsWall(),
                            $"Stairs at ({x},{y}) should not be in a wall cell");
                    }
                }
            });
        }

        // ========================
        // StairsUpBuilder Tests
        // ========================

        [Test]
        public void StairsUpBuilder_PlacesStairsFromConnection()
        {
            // Simulate a connection registered from the zone above
            _zoneManager.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "Overworld.5.5.0",
                SourceX = 20, SourceY = 10,
                TargetZoneID = "Overworld.5.5.1",
                TargetX = 20, TargetY = 10,
                Type = "StairsDown"
            });

            var zone = new Zone("Overworld.5.5.1");
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone, _factory, new System.Random(42));

            var builder = new StairsUpBuilder(_zoneManager);
            builder.BuildZone(zone, _factory, new System.Random(42));

            var cell = zone.GetCell(20, 10);
            bool hasStairsUp = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (cell.Objects[i].HasTag("StairsUp"))
                    hasStairsUp = true;
            }
            Assert.IsTrue(hasStairsUp, "StairsUp should be placed at the connection target position");
        }

        [Test]
        public void StairsUpBuilder_ClearsWallsAtStairsPosition()
        {
            _zoneManager.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "Overworld.5.5.0",
                SourceX = 20, SourceY = 10,
                TargetZoneID = "Overworld.5.5.1",
                TargetX = 20, TargetY = 10,
                Type = "StairsDown"
            });

            var zone = new Zone("Overworld.5.5.1");
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone, _factory, new System.Random(42));

            new StairsUpBuilder(_zoneManager).BuildZone(zone, _factory, new System.Random(42));

            var cell = zone.GetCell(20, 10);
            Assert.IsFalse(cell.IsWall(), "Wall should be cleared at StairsUp position");
        }

        [Test]
        public void StairsUpBuilder_NoConnectionSkipsPlacement()
        {
            var zone = new Zone("Overworld.5.5.5");
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone, _factory, new System.Random(42));

            // No connections registered — should do nothing
            new StairsUpBuilder(_zoneManager).BuildZone(zone, _factory, new System.Random(42));

            bool foundStairsUp = false;
            zone.ForEachCell((cell, x, y) =>
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].HasTag("StairsUp"))
                        foundStairsUp = true;
                }
            });

            Assert.IsFalse(foundStairsUp, "No StairsUp should be placed without a connection");
        }

        // ========================
        // StairConnectorBuilder Tests
        // ========================

        [Test]
        public void StairConnectorBuilder_ConnectsStairs()
        {
            // Register a connection so StairsUpBuilder places stairs
            _zoneManager.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "Overworld.5.5.0",
                SourceX = 5, SourceY = 5,
                TargetZoneID = "Overworld.5.5.1",
                TargetX = 5, TargetY = 5,
                Type = "StairsDown"
            });

            var zone = new Zone("Overworld.5.5.1");
            var rng = new System.Random(42);

            // Build full underground pipeline manually
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone, _factory, rng);
            new StrataBuilder(1, "SandstoneWall", "SandstoneFloor").BuildZone(zone, _factory, rng);
            new ConnectivityBuilder { FloorBlueprint = "SandstoneFloor" }.BuildZone(zone, _factory, rng);
            new StairsUpBuilder(_zoneManager).BuildZone(zone, _factory, rng);
            new StairsDownBuilder(_zoneManager).BuildZone(zone, _factory, rng);
            new StairConnectorBuilder("SandstoneFloor").BuildZone(zone, _factory, rng);

            // Find StairsUp and StairsDown positions
            int upX = -1, upY = -1, downX = -1, downY = -1;
            zone.ForEachCell((cell, x, y) =>
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].HasTag("StairsUp")) { upX = x; upY = y; }
                    if (cell.Objects[i].HasTag("StairsDown")) { downX = x; downY = y; }
                }
            });

            Assert.Greater(upX, -1, "Should have StairsUp");
            Assert.Greater(downX, -1, "Should have StairsDown");

            // Verify they are connected via flood fill
            var reachable = ConnectivityBuilder.FloodFill(zone, upX, upY);
            Assert.IsTrue(reachable[downX, downY],
                $"StairsDown at ({downX},{downY}) should be reachable from StairsUp at ({upX},{upY})");
        }

        // ========================
        // CaveEntranceBuilder Tests
        // ========================

        [Test]
        public void CaveEntranceBuilder_OnlySurface()
        {
            // Underground zone (z=1) — should not place stairs
            var zone = new Zone("Overworld.5.5.1");
            new SolidEarthBuilder("SandstoneWall").BuildZone(zone, _factory, new System.Random(42));
            new StrataBuilder(1, "SandstoneWall", "SandstoneFloor").BuildZone(zone, _factory, new System.Random(42));

            new CaveEntranceBuilder(_zoneManager).BuildZone(zone, _factory, new System.Random(1));

            bool foundStairs = false;
            zone.ForEachCell((cell, x, y) =>
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].HasTag("StairsDown"))
                        foundStairs = true;
                }
            });

            Assert.IsFalse(foundStairs, "CaveEntranceBuilder should not place stairs in underground zones");
        }

        [Test]
        public void CaveEntranceBuilder_RegistersConnectionToZ1()
        {
            // Surface zone with guaranteed placement (use a seed where rng.Next(100) < 50)
            var zone = new Zone("Overworld.5.5.0");
            // Need a passable surface zone — use CaveBuilder
            var caveBuilder = new CaveBuilder();
            caveBuilder.BuildZone(zone, _factory, new System.Random(42));
            new ConnectivityBuilder().BuildZone(zone, _factory, new System.Random(42));

            // Try multiple seeds until one triggers placement (50% chance)
            bool placed = false;
            for (int seed = 0; seed < 20; seed++)
            {
                var manager = new ZoneManager(_factory, 42);
                var testZone = new Zone("Overworld.5.5.0");
                caveBuilder.BuildZone(testZone, _factory, new System.Random(42));
                new ConnectivityBuilder().BuildZone(testZone, _factory, new System.Random(42));

                new CaveEntranceBuilder(manager).BuildZone(testZone, _factory, new System.Random(seed));

                var connections = manager.GetConnections("Overworld.5.5.0");
                if (connections.Count > 0)
                {
                    Assert.AreEqual("StairsDown", connections[0].Type);
                    Assert.AreEqual("Overworld.5.5.0", connections[0].SourceZoneID);
                    Assert.AreEqual("Overworld.5.5.1", connections[0].TargetZoneID);
                    placed = true;
                    break;
                }
            }

            Assert.IsTrue(placed, "CaveEntranceBuilder should place stairs in at least one of 20 seeds");
        }

        // ========================
        // Underground Pipeline Integration
        // ========================

        [Test]
        public void UndergroundPipeline_ProducesPlayableZone()
        {
            // Simulate a connection from surface
            _zoneManager.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "Overworld.5.5.0",
                SourceX = 20, SourceY = 12,
                TargetZoneID = "Overworld.5.5.1",
                TargetX = 20, TargetY = 12,
                Type = "StairsDown"
            });

            var zone = new Zone("Overworld.5.5.1");
            int depth = 1;
            var (wallBP, floorBP) = SolidEarthBuilder.GetMaterialsForDepth(depth);
            var rng = new System.Random(42);

            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new SolidEarthBuilder(wallBP));
            pipeline.AddBuilder(new StrataBuilder(depth, wallBP, floorBP));
            pipeline.AddBuilder(new ConnectivityBuilder { FloorBlueprint = floorBP });
            pipeline.AddBuilder(new StairsUpBuilder(_zoneManager));
            pipeline.AddBuilder(new StairsDownBuilder(_zoneManager));
            pipeline.AddBuilder(new StairConnectorBuilder(floorBP));
            pipeline.AddBuilder(new PopulationBuilder(PopulationTable.UndergroundTier(depth)));
            pipeline.Generate(zone, _factory, rng);

            // Should have walls and open space
            int walls = 0, passable = 0;
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsWall()) walls++;
                if (cell.IsPassable()) passable++;
            });
            Assert.Greater(walls, 0, "Should have walls");
            Assert.Greater(passable, 50, "Should have open space");

            // Should have StairsUp
            bool hasUp = false;
            zone.ForEachCell((cell, x, y) =>
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                    if (cell.Objects[i].HasTag("StairsUp")) hasUp = true;
            });
            Assert.IsTrue(hasUp, "Should have StairsUp from registered connection");

            // Should have StairsDown
            bool hasDown = false;
            zone.ForEachCell((cell, x, y) =>
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                    if (cell.Objects[i].HasTag("StairsDown")) hasDown = true;
            });
            Assert.IsTrue(hasDown, "Should have StairsDown for further descent");

            // StairsUp and StairsDown should be connected
            int upX = -1, upY = -1, downX = -1, downY = -1;
            zone.ForEachCell((cell, x, y) =>
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].HasTag("StairsUp")) { upX = x; upY = y; }
                    if (cell.Objects[i].HasTag("StairsDown")) { downX = x; downY = y; }
                }
            });

            var reachable = ConnectivityBuilder.FloodFill(zone, upX, upY);
            Assert.IsTrue(reachable[downX, downY],
                "StairsDown should be reachable from StairsUp");
        }

        [Test]
        public void UndergroundPipeline_DeterministicWithSameSeed()
        {
            _zoneManager.RegisterConnection(new ZoneConnection
            {
                SourceZoneID = "Overworld.5.5.0",
                SourceX = 20, SourceY = 12,
                TargetZoneID = "Overworld.5.5.1",
                TargetX = 20, TargetY = 12,
                Type = "StairsDown"
            });

            var buildPipeline = new System.Func<ZoneGenerationPipeline>(() =>
            {
                var p = new ZoneGenerationPipeline();
                p.AddBuilder(new SolidEarthBuilder("SandstoneWall"));
                p.AddBuilder(new StrataBuilder(1, "SandstoneWall", "SandstoneFloor"));
                p.AddBuilder(new ConnectivityBuilder { FloorBlueprint = "SandstoneFloor" });
                p.AddBuilder(new StairsUpBuilder(_zoneManager));
                return p;
            });

            var zone1 = new Zone("Overworld.5.5.1");
            buildPipeline().Generate(zone1, _factory, new System.Random(42));

            var zone2 = new Zone("Overworld.5.5.1");
            buildPipeline().Generate(zone2, _factory, new System.Random(42));

            zone1.ForEachCell((cell1, x, y) =>
            {
                var cell2 = zone2.GetCell(x, y);
                Assert.AreEqual(cell1.IsWall(), cell2.IsWall(),
                    $"Wall mismatch at ({x},{y})");
            });
        }

        // ========================
        // PopulationTable Underground Tests
        // ========================

        [Test]
        public void PopulationTable_UndergroundTier_ScalesWithDepth()
        {
            var shallow = PopulationTable.UndergroundTier(1);
            var deep = PopulationTable.UndergroundTier(9);

            var shallowResults = shallow.Roll(new System.Random(42));
            var deepResults = deep.Roll(new System.Random(42));

            Assert.Greater(deepResults.Count, shallowResults.Count,
                "Deeper tiers should produce more entities");
        }

        [Test]
        public void PopulationTable_UndergroundTier_HasLootAtHigherTiers()
        {
            var tier2 = PopulationTable.UndergroundTier(5); // depth 5 → tier 2
            bool hasLongSword = false;
            for (int i = 0; i < tier2.Entries.Count; i++)
            {
                if (tier2.Entries[i].BlueprintName == "LongSword")
                    hasLongSword = true;
            }
            Assert.IsTrue(hasLongSword, "Tier 2+ should include LongSword");
        }
    }
}
