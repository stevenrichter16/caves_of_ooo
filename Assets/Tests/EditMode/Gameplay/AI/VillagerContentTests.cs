using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tier 1 content integration tests: validates Chair and Villager blueprints
    /// plus VillageBuilder chair placement.
    /// These bridge the code-complete goal stack to observable in-game behavior.
    /// </summary>
    [TestFixture]
    public class VillagerContentTests
    {
        private EntityFactory _factory;

        // Minimal blueprint set required for Tier 1 content tests.
        // Mirrors real Objects.json for Chair + Villager + the base chain they inherit from.
        // Also includes Floor/Wall for VillageBuilder biome palette.
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
              ""Name"": ""Creature"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderLayer"", ""Value"": ""10"" }] },
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] },
                { ""Name"": ""Brain"", ""Params"": [
                  { ""Key"": ""SightRadius"", ""Value"": ""10"" },
                  { ""Key"": ""Wanders"", ""Value"": ""true"" },
                  { ""Key"": ""WandersRandomly"", ""Value"": ""true"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 10, ""Min"": 0, ""Max"": 100 },
                { ""Name"": ""Speed"", ""Value"": 100, ""Min"": 25, ""Max"": 200 }
              ],
              ""Tags"": [
                { ""Key"": ""Creature"", ""Value"": """" }
              ]
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
                  { ""Key"": ""RenderString"", ""Value"": ""."" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""StoneFloor"",
              ""Inherits"": ""Terrain"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""stone floor"" },
                  { ""Key"": ""RenderString"", ""Value"": ""."" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Wall"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }] },
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] }
              ],
              ""Stats"": [],
              ""Tags"": [
                { ""Key"": ""Solid"", ""Value"": """" },
                { ""Key"": ""Wall"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""Chair"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""wooden chair"" },
                  { ""Key"": ""RenderString"", ""Value"": ""h"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&w"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""5"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""false"" }] },
                { ""Name"": ""Chair"", ""Params"": [] }
              ],
              ""Stats"": [],
              ""Tags"": [{ ""Key"": ""Furniture"", ""Value"": """" }]
            },
            {
              ""Name"": ""Villager"",
              ""Inherits"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""villager"" },
                  { ""Key"": ""RenderString"", ""Value"": ""@"" },
                  { ""Key"": ""ColorString"", ""Value"": ""&g"" }
                ]},
                { ""Name"": ""Brain"", ""Params"": [
                  { ""Key"": ""SightRadius"", ""Value"": ""8"" },
                  { ""Key"": ""Wanders"", ""Value"": ""false"" },
                  { ""Key"": ""WandersRandomly"", ""Value"": ""false"" },
                  { ""Key"": ""Staying"", ""Value"": ""true"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": [
                { ""Key"": ""Faction"", ""Value"": ""Villagers"" },
                { ""Key"": ""AllowIdleBehavior"", ""Value"": """" }
              ]
            }
          ]
        }";

        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprints);
        }

        // ========================
        // Chair blueprint
        // ========================

        [Test]
        public void ChairBlueprint_CreatesEntityWithChairPart()
        {
            var chair = _factory.CreateEntity("Chair");

            Assert.IsNotNull(chair, "Chair blueprint should create an entity");
            Assert.IsNotNull(chair.GetPart<ChairPart>(), "Chair entity should have ChairPart");
        }

        [Test]
        public void ChairBlueprint_IsNotSolid()
        {
            var chair = _factory.CreateEntity("Chair");
            var physics = chair.GetPart<PhysicsPart>();

            Assert.IsNotNull(physics);
            Assert.IsFalse(physics.Solid, "Chair must not be solid — NPCs need to walk onto it");
        }

        [Test]
        public void ChairBlueprint_HasCorrectRender()
        {
            var chair = _factory.CreateEntity("Chair");
            var render = chair.GetPart<RenderPart>();

            Assert.IsNotNull(render);
            Assert.AreEqual("h", render.RenderString);
            Assert.AreEqual("&w", render.ColorString);
            Assert.AreEqual(5, render.RenderLayer);
            Assert.AreEqual("wooden chair", render.DisplayName);
        }

        [Test]
        public void ChairBlueprint_StartsUnoccupied()
        {
            var chair = _factory.CreateEntity("Chair");
            var chairPart = chair.GetPart<ChairPart>();

            Assert.IsFalse(chairPart.Occupied, "New chair should start unoccupied");
            Assert.AreEqual("", chairPart.Owner, "New chair should have no owner restriction");
        }

        // ========================
        // Villager blueprint
        // ========================

        [Test]
        public void VillagerBlueprint_HasStayingTrue()
        {
            var villager = _factory.CreateEntity("Villager");
            var brain = villager.GetPart<BrainPart>();

            Assert.IsNotNull(brain);
            Assert.IsTrue(brain.Staying, "Villager should have Staying=true");
        }

        [Test]
        public void VillagerBlueprint_HasWandersFalse()
        {
            var villager = _factory.CreateEntity("Villager");
            var brain = villager.GetPart<BrainPart>();

            Assert.IsFalse(brain.Wanders, "Villager should have Wanders=false");
            Assert.IsFalse(brain.WandersRandomly, "Villager should have WandersRandomly=false");
        }

        [Test]
        public void VillagerBlueprint_HasAllowIdleBehaviorTag()
        {
            var villager = _factory.CreateEntity("Villager");

            Assert.IsTrue(villager.HasTag("AllowIdleBehavior"),
                "Villager should have AllowIdleBehavior tag to enable furniture scanning");
        }

        [Test]
        public void VillagerBlueprint_HasVillagersFaction()
        {
            var villager = _factory.CreateEntity("Villager");

            Assert.IsTrue(villager.HasTag("Faction"));
            Assert.AreEqual("Villagers", villager.Tags["Faction"]);
        }

        [Test]
        public void VillagerBlueprint_IsCreature()
        {
            var villager = _factory.CreateEntity("Villager");

            Assert.IsTrue(villager.HasTag("Creature"),
                "Villager inherits from Creature and should have the Creature tag");
        }

        [Test]
        public void VillagerBlueprint_OverridesBrainSightRadius()
        {
            var villager = _factory.CreateEntity("Villager");
            var brain = villager.GetPart<BrainPart>();

            // Villager blueprint sets SightRadius to 8, Creature default is 10
            Assert.AreEqual(8, brain.SightRadius);
        }

        // ========================
        // VillageBuilder chair placement (integration)
        // ========================

        [Test]
        public void VillageBuilder_PlacesChairsInBuildings()
        {
            var zone = new Zone("TestVillage");
            var builder = new VillageBuilder(BiomeType.Cave, null, null);
            var rng = new System.Random(12345);

            builder.BuildZone(zone, _factory, rng);

            // Count chair entities in the zone
            int chairCount = 0;
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity.GetPart<ChairPart>() != null)
                    chairCount++;
            }

            Assert.Greater(chairCount, 0,
                "VillageBuilder should place at least one chair. Village has 3-5 buildings, each with 1 chair.");
            Assert.LessOrEqual(chairCount, 5,
                "Should place at most one chair per building (max 5 buildings per village)");
        }

        [Test]
        public void VillageBuilder_ChairsArePlacedOnPassableCells()
        {
            var zone = new Zone("TestVillage");
            var builder = new VillageBuilder(BiomeType.Cave, null, null);
            var rng = new System.Random(54321);

            builder.BuildZone(zone, _factory, rng);

            // Every chair should be on a passable cell (walkable by NPCs)
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity.GetPart<ChairPart>() == null) continue;

                var cell = zone.GetEntityCell(entity);
                Assert.IsNotNull(cell, "Chair should be placed in a cell");
                Assert.IsTrue(cell.IsPassable(),
                    $"Chair at ({cell.X},{cell.Y}) should be on a passable cell — NPCs must reach it");
            }
        }

        [Test]
        public void VillageBuilder_ChairsAreInsideBuildings()
        {
            // Chairs at room.CenterX, room.CenterY should be surrounded by stone floor
            // (interior) with walls somewhere in the vicinity. This is a weak check but
            // confirms chairs aren't dropped in the open village square.
            var zone = new Zone("TestVillage");
            var builder = new VillageBuilder(BiomeType.Cave, null, null);
            var rng = new System.Random(99999);

            builder.BuildZone(zone, _factory, rng);

            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity.GetPart<ChairPart>() == null) continue;

                var cell = zone.GetEntityCell(entity);
                Assert.IsNotNull(cell);

                // Chair should be on stone floor (building interior), not the biome floor (outside)
                bool onStoneFloor = false;
                foreach (var obj in cell.Objects)
                {
                    if (obj.GetPart<RenderPart>()?.DisplayName == "stone floor")
                    {
                        onStoneFloor = true;
                        break;
                    }
                }
                Assert.IsTrue(onStoneFloor,
                    $"Chair at ({cell.X},{cell.Y}) should be on stone floor (inside a building)");
            }
        }

        // ========================
        // End-to-end: Villager uses chair
        // ========================

        [Test]
        public void Villager_WithChair_CanSitViaDirectOffer()
        {
            // Bypasses the 1% scan gate — directly queries the chair and pushes
            // the pair onto the brain's stack. Verifies the full chain: Villager
            // has AllowIdleBehavior, ChairPart responds, DelegateGoal executes,
            // SittingEffect applies.
            var zone = new Zone("TestZone");

            var chair = _factory.CreateEntity("Chair");
            zone.AddEntity(chair, 5, 5);

            var villager = _factory.CreateEntity("Villager");
            var brain = villager.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(42);
            zone.AddEntity(villager, 5, 5);

            // Directly query — should succeed because villager has AllowIdleBehavior
            var offer = IdleQueryEvent.QueryOffer(chair, villager);

            Assert.IsNotNull(offer, "Villager with AllowIdleBehavior should receive an offer");
            Assert.IsTrue(chair.GetPart<ChairPart>().Occupied,
                "Chair should be reserved synchronously at query time");

            // Push the delegate goal onto the brain's stack, then execute.
            // This ensures ParentBrain is wired so the lambda can find ParentEntity.
            var delegateGoal = new DelegateGoal(offer.Action);
            brain.PushGoal(delegateGoal);
            delegateGoal.TakeAction();

            Assert.IsTrue(villager.HasEffect<SittingEffect>(),
                "Villager should have SittingEffect after executing the idle action");
        }
    }
}
