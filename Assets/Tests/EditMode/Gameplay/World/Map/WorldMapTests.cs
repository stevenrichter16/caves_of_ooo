using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using System;
using System.Collections.Generic;

namespace CavesOfOoo.Tests
{
    public class WorldMapTests
    {
        private EntityFactory _factory;

        private const string TestBlueprints = @"{
          ""Objects"": [
            {
              ""Name"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""RenderString"", ""Value"": ""?"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&y"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""0"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [] }
              ]
            },
            {
              ""Name"": ""Terrain"",
              ""Inherits"": ""PhysicalObject"",
              ""Tags"": [
                { ""Key"": ""Terrain"", ""Value"": """" }
              ]
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
              ]
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
              ]
            },
            {
              ""Name"": ""Wall"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""wall"" },
                  { ""Key"": ""RenderString"", ""Value"": ""#"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&K"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [
                  { ""Key"": ""Solid"", ""Value"": ""true"" }
                ]}
              ],
              ""Tags"": [
                { ""Key"": ""Solid"", ""Value"": """" },
                { ""Key"": ""Wall"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""Sand"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""sand"" },
                  { ""Key"": ""RenderString"", ""Value"": ""."" },
                  { ""Key"": ""ColorString"", ""Value"": ""&W"" }
                ]}
              ]
            },
            {
              ""Name"": ""SandstoneWall"",
              ""Inherits"": ""Wall"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""sandstone wall"" },
                  { ""Key"": ""RenderString"", ""Value"": ""#"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&W"" }
                ]}
              ]
            },
            {
              ""Name"": ""Rock"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""rock"" },
                  { ""Key"": ""RenderString"", ""Value"": ""o"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&y"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [
                  { ""Key"": ""Solid"", ""Value"": ""true"" }
                ]}
              ],
              ""Tags"": [
                { ""Key"": ""Solid"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""Grass"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""grass"" },
                  { ""Key"": ""RenderString"", ""Value"": ""."" },
                  { ""Key"": ""ColorString"", ""Value"": ""&g"" }
                ]}
              ]
            },
            {
              ""Name"": ""VineWall"",
              ""Inherits"": ""Wall"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""vine wall"" },
                  { ""Key"": ""RenderString"", ""Value"": ""#"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&G"" }
                ]}
              ]
            },
            {
              ""Name"": ""Tree"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""tree"" },
                  { ""Key"": ""RenderString"", ""Value"": ""T"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&G"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [
                  { ""Key"": ""Solid"", ""Value"": ""true"" }
                ]}
              ],
              ""Tags"": [
                { ""Key"": ""Solid"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""StoneFloor"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""stone floor"" },
                  { ""Key"": ""RenderString"", ""Value"": ""."" },
                  { ""Key"": ""ColorString"", ""Value"": ""&y"" }
                ]}
              ]
            },
            {
              ""Name"": ""StoneWall"",
              ""Inherits"": ""Wall"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""stone wall"" },
                  { ""Key"": ""RenderString"", ""Value"": ""#"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&w"" }
                ]}
              ]
            },
            {
              ""Name"": ""Stalagmite"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""stalagmite"" },
                  { ""Key"": ""RenderString"", ""Value"": ""^"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&y"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [
                  { ""Key"": ""Solid"", ""Value"": ""true"" }
                ]}
              ],
              ""Tags"": [
                { ""Key"": ""Solid"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""Creature"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""RenderString"", ""Value"": ""?"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&y"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""10"" }
                ]},
                { ""Name"": ""MeleeWeapon"", ""Params"": [
                  { ""Key"": ""BaseDamage"", ""Value"": ""1d2"" }
                ]},
                { ""Name"": ""Armor"", ""Params"": [] },
                { ""Name"": ""Inventory"", ""Params"": [
                  { ""Key"": ""MaxWeight"", ""Value"": ""150"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 1, ""Min"": 0, ""Max"": 999 },
                { ""Name"": ""Strength"", ""Value"": 10, ""Min"": 1, ""Max"": 50 },
                { ""Name"": ""Agility"", ""Value"": 10, ""Min"": 1, ""Max"": 50 },
                { ""Name"": ""Toughness"", ""Value"": 10, ""Min"": 1, ""Max"": 50 },
                { ""Name"": ""Intelligence"", ""Value"": 10, ""Min"": 1, ""Max"": 50 },
                { ""Name"": ""Willpower"", ""Value"": 10, ""Min"": 1, ""Max"": 50 },
                { ""Name"": ""Ego"", ""Value"": 10, ""Min"": 1, ""Max"": 50 },
                { ""Name"": ""Speed"", ""Value"": 100, ""Min"": 0, ""Max"": 999 }
              ],
              ""Tags"": [
                { ""Key"": ""Creature"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""Snapjaw"",
              ""Inherits"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""snapjaw"" },
                  { ""Key"": ""RenderString"", ""Value"": ""s"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&w"" }
                ]},
                { ""Name"": ""MeleeWeapon"", ""Params"": [
                  { ""Key"": ""BaseDamage"", ""Value"": ""1d4"" }
                ]},
                { ""Name"": ""Armor"", ""Params"": [
                  { ""Key"": ""AV"", ""Value"": ""2"" },
                  { ""Key"": ""DV"", ""Value"": ""1"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 15, ""Min"": 0, ""Max"": 15 },
                { ""Name"": ""Strength"", ""Value"": 16 },
                { ""Name"": ""Agility"", ""Value"": 14 },
                { ""Name"": ""Toughness"", ""Value"": 14 }
              ],
              ""Tags"": [
                { ""Key"": ""Faction"", ""Value"": ""Snapjaws"" },
                { ""Key"": ""Tier"", ""Value"": ""1"" }
              ]
            },
            {
              ""Name"": ""Player"",
              ""Inherits"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""player"" },
                  { ""Key"": ""RenderString"", ""Value"": ""@"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&Y"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 20, ""Min"": 0, ""Max"": 20 },
                { ""Name"": ""Strength"", ""Value"": 18 },
                { ""Name"": ""Agility"", ""Value"": 18 },
                { ""Name"": ""Toughness"", ""Value"": 18 }
              ],
              ""Tags"": [
                { ""Key"": ""Player"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""Dagger"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""dagger"" },
                  { ""Key"": ""RenderString"", ""Value"": ""/"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&c"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [
                  { ""Key"": ""Takeable"", ""Value"": ""true"" }
                ]}
              ],
              ""Tags"": [
                { ""Key"": ""Item"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""LongSword"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""long sword"" },
                  { ""Key"": ""RenderString"", ""Value"": ""/"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&y"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [
                  { ""Key"": ""Takeable"", ""Value"": ""true"" }
                ]}
              ],
              ""Tags"": [
                { ""Key"": ""Item"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""LeatherArmor"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""leather armor"" },
                  { ""Key"": ""RenderString"", ""Value"": ""["" },
                  { ""Key"": ""ColorString"", ""Value"": ""&w"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [
                  { ""Key"": ""Takeable"", ""Value"": ""true"" }
                ]}
              ],
              ""Tags"": [
                { ""Key"": ""Item"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""SnapjawScavenger"",
              ""Inherits"": ""Snapjaw"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""snapjaw scavenger"" }
                ]}
              ]
            },
            {
              ""Name"": ""SnapjawHunter"",
              ""Inherits"": ""Snapjaw"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""snapjaw hunter"" }
                ]}
              ]
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
              ""Tags"": [{ ""Key"": ""StairsUp"", ""Value"": """" }]
            },
            {
              ""Name"": ""Villager"",
              ""Inherits"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""villager"" },
                  { ""Key"": ""RenderString"", ""Value"": ""v"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&w"" }
                ]}
              ]
            },
            {
              ""Name"": ""Tinker"",
              ""Inherits"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""tinker"" },
                  { ""Key"": ""RenderString"", ""Value"": ""t"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&c"" }
                ]}
              ]
            },
            {
              ""Name"": ""Merchant"",
              ""Inherits"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""merchant"" },
                  { ""Key"": ""RenderString"", ""Value"": ""m"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&Y"" }
                ]}
              ]
            }
          ]
        }";

        [SetUp]
        public void SetUp()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprints);
        }

        // ================================================================
        // Helper methods
        // ================================================================

        /// <summary>
        /// Create an OverworldZoneManager with the test factory and a given seed.
        /// </summary>
        private OverworldZoneManager CreateManager(int seed = 42)
        {
            return new OverworldZoneManager(_factory, seed);
        }

        /// <summary>
        /// Run a biome builder pipeline (Border + builder) with a known seed and return the zone.
        /// </summary>
        private Zone RunBiomePipeline(IZoneBuilder biomeBuilder, int seed = 42)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(biomeBuilder);
            var zone = new Zone("TestZone");
            var rng = new System.Random(seed);
            pipeline.Generate(zone, _factory, rng);
            return zone;
        }

        /// <summary>
        /// Run a full biome pipeline (builder + Connectivity) with a known seed.
        /// No border walls — like Qud, edges are seamless.
        /// </summary>
        private Zone RunFullPipeline(IZoneBuilder biomeBuilder, int seed = 42)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(biomeBuilder);
            pipeline.AddBuilder(new ConnectivityBuilder());
            var zone = new Zone("TestZone");
            var rng = new System.Random(seed);
            pipeline.Generate(zone, _factory, rng);
            return zone;
        }

        /// <summary>
        /// Count passable interior cells (not on the border) in a zone.
        /// </summary>
        private int CountPassableInterior(Zone zone)
        {
            int count = 0;
            for (int x = 1; x < Zone.Width - 1; x++)
            {
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    if (zone.GetCell(x, y).IsPassable())
                        count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Check if the zone contains at least one entity created from a given blueprint.
        /// </summary>
        private bool ZoneHasBlueprint(Zone zone, string blueprintName)
        {
            var entities = zone.GetAllEntities();
            foreach (var e in entities)
            {
                if (e.BlueprintName == blueprintName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Count how many cells are reachable from a start point via flood fill.
        /// </summary>
        private int FloodFillCount(Zone zone, int startX, int startY)
        {
            var visited = ConnectivityBuilder.FloodFill(zone, startX, startY);
            int count = 0;
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                    if (visited[x, y]) count++;
            return count;
        }

        /// <summary>
        /// Create a player entity and place it in a zone at the given position.
        /// </summary>
        private Entity PlacePlayer(Zone zone, int x, int y)
        {
            var player = _factory.CreateEntity("Player");
            zone.AddEntity(player, x, y);
            return player;
        }

        // ================================================================
        // WorldMap (6 tests)
        // ================================================================

        [Test]
        public void WorldMap_ToZoneID_FormatsCorrectly()
        {
            string id = WorldMap.ToZoneID(5, 3);
            Assert.AreEqual("Overworld.5.3.0", id);
        }

        [Test]
        public void WorldMap_FromZoneID_ParsesCorrectly()
        {
            var (x, y, z) = WorldMap.FromZoneID("Overworld.5.3.0");
            Assert.AreEqual(5, x);
            Assert.AreEqual(3, y);
            Assert.AreEqual(0, z);
        }

        [Test]
        public void WorldMap_FromZoneID_LegacyFormat_DefaultsZ0()
        {
            var (x, y, z) = WorldMap.FromZoneID("Overworld.5.3");
            Assert.AreEqual(5, x);
            Assert.AreEqual(3, y);
            Assert.AreEqual(0, z);
        }

        [Test]
        public void WorldMap_FromZoneID_ParsesDepth()
        {
            var (x, y, z) = WorldMap.FromZoneID("Overworld.3.7.4");
            Assert.AreEqual(3, x);
            Assert.AreEqual(7, y);
            Assert.AreEqual(4, z);
        }

        [Test]
        public void WorldMap_IsOverworldZoneID_CorrectResults()
        {
            Assert.IsTrue(WorldMap.IsOverworldZoneID("Overworld.5.3.0"));
            Assert.IsTrue(WorldMap.IsOverworldZoneID("Overworld.5.3"));
            Assert.IsFalse(WorldMap.IsOverworldZoneID("CaveLevel_1"));
            Assert.IsFalse(WorldMap.IsOverworldZoneID(null));
        }

        [Test]
        public void WorldMap_GetAdjacentZoneID_ReturnsNeighbor()
        {
            string adjacent = WorldMap.GetAdjacentZoneID("Overworld.5.5.0", 1, 0);
            Assert.AreEqual("Overworld.6.5.0", adjacent);
        }

        [Test]
        public void WorldMap_GetAdjacentZoneID_PreservesZ()
        {
            string adjacent = WorldMap.GetAdjacentZoneID("Overworld.5.5.3", 1, 0);
            Assert.AreEqual("Overworld.6.5.3", adjacent);
        }

        [Test]
        public void WorldMap_GetAdjacentZoneID_NullAtWorldEdge()
        {
            string adjacent = WorldMap.GetAdjacentZoneID("Overworld.9.5.0", 1, 0);
            Assert.IsNull(adjacent);
        }

        [Test]
        public void WorldMap_GetZoneBelow_IncrementsZ()
        {
            string below = WorldMap.GetZoneBelow("Overworld.5.5.0");
            Assert.AreEqual("Overworld.5.5.1", below);
        }

        [Test]
        public void WorldMap_GetZoneAbove_DecrementsZ()
        {
            string above = WorldMap.GetZoneAbove("Overworld.5.5.3");
            Assert.AreEqual("Overworld.5.5.2", above);
        }

        [Test]
        public void WorldMap_GetZoneAbove_AtSurface_ReturnsNull()
        {
            string above = WorldMap.GetZoneAbove("Overworld.5.5.0");
            Assert.IsNull(above);
        }

        [Test]
        public void WorldMap_GetDepth_ReturnsZ()
        {
            Assert.AreEqual(0, WorldMap.GetDepth("Overworld.5.5.0"));
            Assert.AreEqual(4, WorldMap.GetDepth("Overworld.3.7.4"));
        }

        [Test]
        public void WorldMap_InBounds_CorrectForEdges()
        {
            var map = new WorldMap(1);
            Assert.IsTrue(map.InBounds(0, 0));
            Assert.IsTrue(map.InBounds(9, 9));
            Assert.IsFalse(map.InBounds(10, 0));
            Assert.IsFalse(map.InBounds(-1, 0));
        }

        // ================================================================
        // WorldGenerator (5 tests)
        // ================================================================

        [Test]
        public void WorldGenerator_Deterministic_SameSeedSameMap()
        {
            var map1 = WorldGenerator.Generate(42);
            var map2 = WorldGenerator.Generate(42);

            for (int x = 0; x < WorldMap.Width; x++)
            {
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    Assert.AreEqual(map1.Tiles[x, y], map2.Tiles[x, y],
                        $"Tile mismatch at ({x},{y})");
                }
            }
        }

        [Test]
        public void WorldGenerator_DifferentSeeds_DifferentMaps()
        {
            var map1 = WorldGenerator.Generate(42);
            var map2 = WorldGenerator.Generate(43);

            bool anyDifferent = false;
            for (int x = 0; x < WorldMap.Width; x++)
            {
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    if (map1.Tiles[x, y] != map2.Tiles[x, y])
                    {
                        anyDifferent = true;
                        break;
                    }
                }
                if (anyDifferent) break;
            }
            Assert.IsTrue(anyDifferent, "Two different seeds should produce at least one different tile");
        }

        [Test]
        public void WorldGenerator_CenterIsCave()
        {
            var map = WorldGenerator.Generate(42);
            Assert.AreEqual(BiomeType.Cave, map.Tiles[5, 5]);
        }

        [Test]
        public void WorldGenerator_AllBiomesPresent()
        {
            var map = WorldGenerator.Generate(42);

            bool hasCave = false, hasDesert = false, hasJungle = false, hasRuins = false;
            for (int x = 0; x < WorldMap.Width; x++)
            {
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    switch (map.Tiles[x, y])
                    {
                        case BiomeType.Cave: hasCave = true; break;
                        case BiomeType.Desert: hasDesert = true; break;
                        case BiomeType.Jungle: hasJungle = true; break;
                        case BiomeType.Ruins: hasRuins = true; break;
                    }
                }
            }
            Assert.IsTrue(hasCave, "Cave biome should be present");
            Assert.IsTrue(hasDesert, "Desert biome should be present");
            Assert.IsTrue(hasJungle, "Jungle biome should be present");
            Assert.IsTrue(hasRuins, "Ruins biome should be present");
        }

        [Test]
        public void WorldGenerator_ValidTiles()
        {
            var map = WorldGenerator.Generate(42);
            var validValues = new HashSet<BiomeType>((BiomeType[])Enum.GetValues(typeof(BiomeType)));

            for (int x = 0; x < WorldMap.Width; x++)
            {
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    Assert.IsTrue(validValues.Contains(map.Tiles[x, y]),
                        $"Tile at ({x},{y}) has invalid BiomeType: {map.Tiles[x, y]}");
                }
            }
        }

        // ================================================================
        // ZoneTransitionSystem (10 tests)
        // ================================================================

        [Test]
        public void IsEdgeTransition_TrueWhenExitingEast()
        {
            Assert.IsTrue(ZoneTransitionSystem.IsEdgeTransition(79, 12, 1, 0));
        }

        [Test]
        public void IsEdgeTransition_TrueWhenExitingWest()
        {
            Assert.IsTrue(ZoneTransitionSystem.IsEdgeTransition(0, 12, -1, 0));
        }

        [Test]
        public void IsEdgeTransition_TrueWhenExitingNorth()
        {
            Assert.IsTrue(ZoneTransitionSystem.IsEdgeTransition(40, 0, 0, -1));
        }

        [Test]
        public void IsEdgeTransition_TrueWhenExitingSouth()
        {
            Assert.IsTrue(ZoneTransitionSystem.IsEdgeTransition(40, 24, 0, 1));
        }

        [Test]
        public void IsEdgeTransition_FalseForInteriorMove()
        {
            Assert.IsFalse(ZoneTransitionSystem.IsEdgeTransition(40, 12, 1, 0));
        }

        [Test]
        public void GetArrivalPosition_EastToWest()
        {
            // Qud wraps to exact opposite edge: East arrives at x=0
            var (x, y) = ZoneTransitionSystem.GetArrivalPosition(TransitionDirection.East, 79, 12);
            Assert.AreEqual(0, x);
            Assert.AreEqual(12, y);
        }

        [Test]
        public void GetArrivalPosition_WestToEast()
        {
            var (x, y) = ZoneTransitionSystem.GetArrivalPosition(TransitionDirection.West, 0, 12);
            Assert.AreEqual(79, x);
            Assert.AreEqual(12, y);
        }

        [Test]
        public void GetArrivalPosition_NorthToSouth()
        {
            var (x, y) = ZoneTransitionSystem.GetArrivalPosition(TransitionDirection.North, 40, 0);
            Assert.AreEqual(40, x);
            Assert.AreEqual(24, y);
        }

        [Test]
        public void GetArrivalPosition_SouthToNorth()
        {
            var (x, y) = ZoneTransitionSystem.GetArrivalPosition(TransitionDirection.South, 40, 24);
            Assert.AreEqual(40, x);
            Assert.AreEqual(0, y);
        }

        [Test]
        public void TransitionPlayer_Success()
        {
            var manager = CreateManager(42);
            string startID = "Overworld.5.5.0";
            Zone startZone = manager.GetZone(startID);

            // Place player on a passable interior cell
            int px = 40, py = 12;
            var player = PlacePlayer(startZone, px, py);

            var result = ZoneTransitionSystem.TransitionPlayer(
                player, startZone, TransitionDirection.East, px, py,
                manager, manager.WorldMap);

            Assert.IsTrue(result.Success, result.ErrorReason ?? "Transition should succeed");
            Assert.IsNotNull(result.NewZone);
            Assert.AreEqual("Overworld.6.5.0", result.NewZone.ZoneID);
        }

        // ================================================================
        // Transition Integration (5 tests)
        // ================================================================

        [Test]
        public void TransitionPlayer_FailsAtWorldEdge()
        {
            var manager = CreateManager(42);
            string startID = "Overworld.9.5.0";
            Zone startZone = manager.GetZone(startID);

            var player = PlacePlayer(startZone, 79, 12);

            var result = ZoneTransitionSystem.TransitionPlayer(
                player, startZone, TransitionDirection.East, 79, 12,
                manager, manager.WorldMap);

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void TransitionPlayer_PlayerRemovedFromOldZone()
        {
            var manager = CreateManager(42);
            string startID = "Overworld.5.5.0";
            Zone startZone = manager.GetZone(startID);

            int px = 40, py = 12;
            var player = PlacePlayer(startZone, px, py);

            var result = ZoneTransitionSystem.TransitionPlayer(
                player, startZone, TransitionDirection.East, px, py,
                manager, manager.WorldMap);

            Assert.IsTrue(result.Success, result.ErrorReason ?? "Transition should succeed");
            Assert.IsNull(startZone.GetEntityCell(player),
                "Player should be removed from old zone after transition");
        }

        [Test]
        public void TransitionPlayer_PlayerInNewZone()
        {
            var manager = CreateManager(42);
            string startID = "Overworld.5.5.0";
            Zone startZone = manager.GetZone(startID);

            int px = 40, py = 12;
            var player = PlacePlayer(startZone, px, py);

            var result = ZoneTransitionSystem.TransitionPlayer(
                player, startZone, TransitionDirection.East, px, py,
                manager, manager.WorldMap);

            Assert.IsTrue(result.Success, result.ErrorReason ?? "Transition should succeed");
            Assert.IsNotNull(result.NewZone.GetEntityCell(player),
                "Player should be present in new zone after transition");
        }

        [Test]
        public void TransitionPlayer_FindsPassableCell()
        {
            // Use a zone manager and transition; even if the ideal arrival cell
            // is blocked by a wall, the system should spiral-search for a passable cell.
            var manager = CreateManager(42);
            string startID = "Overworld.5.5.0";
            Zone startZone = manager.GetZone(startID);

            // Place player near top-left (y=1) so arrival on the other side
            // is near the border where walls are likely.
            int px = 40, py = 1;
            var player = PlacePlayer(startZone, px, py);

            var result = ZoneTransitionSystem.TransitionPlayer(
                player, startZone, TransitionDirection.East, px, py,
                manager, manager.WorldMap);

            // Should still succeed even if (1, 1) is a wall — the system searches nearby
            Assert.IsTrue(result.Success, result.ErrorReason ?? "Should find a passable cell even near border");
        }

        [Test]
        public void TransitionPlayer_RoundTrip_ReturnsCachedZone()
        {
            var manager = CreateManager(42);
            string startID = "Overworld.5.5.0";
            Zone startZone = manager.GetZone(startID);

            int px = 40, py = 12;
            var player = PlacePlayer(startZone, px, py);

            // Transition east
            var result1 = ZoneTransitionSystem.TransitionPlayer(
                player, startZone, TransitionDirection.East, px, py,
                manager, manager.WorldMap);
            Assert.IsTrue(result1.Success, result1.ErrorReason ?? "First transition should succeed");

            Zone eastZone = result1.NewZone;

            // Transition back west
            var result2 = ZoneTransitionSystem.TransitionPlayer(
                player, eastZone, TransitionDirection.West, result1.NewPlayerX, result1.NewPlayerY,
                manager, manager.WorldMap);
            Assert.IsTrue(result2.Success, result2.ErrorReason ?? "Return transition should succeed");

            // The returned zone should be the same cached instance as the original start zone
            Assert.AreSame(startZone, result2.NewZone,
                "Going east then west should return the same cached zone instance");
        }

        // ================================================================
        // Biome Builders (8 tests)
        // ================================================================

        [Test]
        public void DesertBuilder_MostlyOpen()
        {
            var zone = RunBiomePipeline(new DesertBuilder(), seed: 42);
            int passable = CountPassableInterior(zone);
            int totalInterior = (Zone.Width - 2) * (Zone.Height - 2);

            float ratio = (float)passable / totalInterior;
            Assert.Greater(ratio, 0.60f,
                $"Desert should be mostly open; got {ratio:P0} passable ({passable}/{totalInterior})");
        }

        [Test]
        public void DesertBuilder_HasSand()
        {
            var zone = RunBiomePipeline(new DesertBuilder(), seed: 42);
            Assert.IsTrue(ZoneHasBlueprint(zone, "Sand"),
                "Desert zone should contain Sand entities");
        }

        [Test]
        public void JungleBuilder_HasGrassAndVines()
        {
            var zone = RunBiomePipeline(new JungleBuilder(), seed: 42);
            Assert.IsTrue(ZoneHasBlueprint(zone, "Grass"),
                "Jungle zone should contain Grass entities");
            Assert.IsTrue(ZoneHasBlueprint(zone, "VineWall"),
                "Jungle zone should contain VineWall entities");
        }

        [Test]
        public void JungleBuilder_ProducesPassableCells()
        {
            var zone = RunBiomePipeline(new JungleBuilder(), seed: 42);
            int passable = CountPassableInterior(zone);
            Assert.Greater(passable, 0,
                "Jungle should have some passable cells (not all walls)");
        }

        [Test]
        public void RuinsBuilder_HasStoneElements()
        {
            var zone = RunBiomePipeline(new RuinsBuilder(), seed: 42);
            Assert.IsTrue(ZoneHasBlueprint(zone, "StoneFloor"),
                "Ruins zone should contain StoneFloor entities");
            Assert.IsTrue(ZoneHasBlueprint(zone, "StoneWall"),
                "Ruins zone should contain StoneWall entities");
        }

        [Test]
        public void RuinsBuilder_HasOpenRooms()
        {
            var zone = RunBiomePipeline(new RuinsBuilder(), seed: 42);

            // Count contiguous passable cells in the interior.
            // Find the first passable cell and flood fill; rooms should have multiple cells.
            int startX = -1, startY = -1;
            for (int x = 1; x < Zone.Width - 1 && startX < 0; x++)
                for (int y = 1; y < Zone.Height - 1 && startX < 0; y++)
                    if (zone.GetCell(x, y).IsPassable())
                    { startX = x; startY = y; }

            Assert.Greater(startX, -1, "Should have at least one passable cell (a room)");

            int reachable = FloodFillCount(zone, startX, startY);
            // A room of minimum size 4x4 = 16 cells, but connectivity may vary.
            // Just assert that there's a contiguous region bigger than a trivial corridor.
            Assert.Greater(reachable, 5,
                "Ruins should have contiguous rooms with multiple passable cells");
        }

        [Test]
        public void BiomeBuilder_ProducesValidZones()
        {
            // All 3 biome builders should produce valid zones without border walls
            var desertZone = RunBiomePipeline(new DesertBuilder(), seed: 100);
            var jungleZone = RunBiomePipeline(new JungleBuilder(), seed: 100);
            var ruinsZone = RunBiomePipeline(new RuinsBuilder(), seed: 100);

            Assert.Greater(desertZone.EntityCount, 0, "Desert zone should have entities");
            Assert.Greater(jungleZone.EntityCount, 0, "Jungle zone should have entities");
            Assert.Greater(ruinsZone.EntityCount, 0, "Ruins zone should have entities");
        }

        [Test]
        public void BiomeBuilder_ConnectivityAfterFullPipeline()
        {
            // Run full pipelines (Border + Biome + Connectivity) and verify
            // that flood fill from a passable cell reaches most passable cells.
            IZoneBuilder[] builders = { new DesertBuilder(), new JungleBuilder(), new RuinsBuilder() };
            string[] names = { "Desert", "Jungle", "Ruins" };

            for (int i = 0; i < builders.Length; i++)
            {
                var zone = RunFullPipeline(builders[i], seed: 42);
                int totalPassable = CountPassableInterior(zone);

                // Find first passable cell
                int sx = -1, sy = -1;
                for (int x = 1; x < Zone.Width - 1 && sx < 0; x++)
                    for (int y = 1; y < Zone.Height - 1 && sx < 0; y++)
                        if (zone.GetCell(x, y).IsPassable())
                        { sx = x; sy = y; }

                Assert.Greater(sx, -1, $"{names[i]}: should have passable cells");

                int reachable = FloodFillCount(zone, sx, sy);

                // After connectivity pass, the majority of passable cells should be reachable.
                // Use a generous threshold since some edge cells may be isolated.
                float ratio = (float)reachable / totalPassable;
                Assert.Greater(ratio, 0.50f,
                    $"{names[i]}: flood fill should reach most passable cells; got {ratio:P0} ({reachable}/{totalPassable})");
            }
        }

        // ================================================================
        // OverworldZoneManager (5 tests)
        // ================================================================

        [Test]
        public void OverworldZoneManager_RoutesCaveBiome()
        {
            var manager = CreateManager(42);
            // Force a Cave biome tile
            var worldMap = manager.WorldMap;
            // Center is always Cave
            string caveID = WorldMap.ToZoneID(5, 5);
            Assert.AreEqual(BiomeType.Cave, worldMap.GetBiome(5, 5));

            Zone zone = manager.GetZone(caveID);
            Assert.IsNotNull(zone);

            // Cave zones use Wall blueprint (gray: &K), not SandstoneWall/VineWall/StoneWall
            Assert.IsTrue(ZoneHasBlueprint(zone, "Wall"),
                "Cave zone should contain Wall entities");
        }

        [Test]
        public void OverworldZoneManager_RoutesDesertBiome()
        {
            var manager = CreateManager(42);
            var worldMap = manager.WorldMap;

            // Find a desert tile
            string desertID = null;
            for (int x = 0; x < WorldMap.Width; x++)
            {
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    if (worldMap.GetBiome(x, y) == BiomeType.Desert)
                    {
                        desertID = WorldMap.ToZoneID(x, y);
                        break;
                    }
                }
                if (desertID != null) break;
            }
            Assert.IsNotNull(desertID, "World map should have at least one Desert tile");

            Zone zone = manager.GetZone(desertID);
            Assert.IsNotNull(zone);
            Assert.IsTrue(ZoneHasBlueprint(zone, "Sand"),
                "Desert zone should contain Sand entities");
        }

        [Test]
        public void OverworldZoneManager_CachesZones()
        {
            var manager = CreateManager(42);
            string zoneID = "Overworld.5.5.0";

            Zone zone1 = manager.GetZone(zoneID);
            Zone zone2 = manager.GetZone(zoneID);

            Assert.AreSame(zone1, zone2,
                "Getting the same zone ID twice should return the same cached instance");
        }

        [Test]
        public void OverworldZoneManager_DeterministicPerSeed()
        {
            var manager1 = CreateManager(42);
            var manager2 = CreateManager(42);

            // Compare world maps
            for (int x = 0; x < WorldMap.Width; x++)
            {
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    Assert.AreEqual(
                        manager1.WorldMap.Tiles[x, y],
                        manager2.WorldMap.Tiles[x, y],
                        $"World map tile mismatch at ({x},{y})");
                }
            }
        }

        [Test]
        public void OverworldZoneManager_FallbackForNonOverworld()
        {
            var manager = CreateManager(42);
            Zone zone = manager.GetZone("CaveLevel_1");

            Assert.IsNotNull(zone, "Non-overworld zone IDs should still produce a zone via fallback");
            Assert.AreEqual("CaveLevel_1", zone.ZoneID);
        }

        // ================================================================
        // Blueprint + Population (2 tests)
        // ================================================================

        [Test]
        public void NewBlueprintsLoad()
        {
            string[] blueprintNames = {
                "Sand", "Grass", "StoneFloor", "StoneWall",
                "SandstoneWall", "VineWall", "Tree", "Rock",
                "Stalagmite", "SnapjawScavenger", "SnapjawHunter",
                "LeatherArmor", "LongSword"
            };

            foreach (var name in blueprintNames)
            {
                var entity = _factory.CreateEntity(name);
                Assert.IsNotNull(entity, $"Factory should create entity from blueprint '{name}'");
                Assert.AreEqual(name, entity.BlueprintName);
            }
        }

        [Test]
        public void PopulationTables_ReturnEntries()
        {
            var rng = new System.Random(42);

            var desertEntries = PopulationTable.DesertTier1().Roll(rng);
            Assert.Greater(desertEntries.Count, 0,
                "DesertTier1 population table should return non-empty entries");

            var jungleEntries = PopulationTable.JungleTier1().Roll(rng);
            Assert.Greater(jungleEntries.Count, 0,
                "JungleTier1 population table should return non-empty entries");

            var ruinsEntries = PopulationTable.RuinsTier1().Roll(rng);
            Assert.Greater(ruinsEntries.Count, 0,
                "RuinsTier1 population table should return non-empty entries");
        }
    }
}
