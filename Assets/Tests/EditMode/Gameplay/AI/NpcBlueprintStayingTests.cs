using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Verifies that all village NPC blueprints (Elder, Merchant, Tinker, Warden,
    /// Farmer, Scribe, WellKeeper, Villager) are configured as Staying with
    /// AllowIdleBehavior. This is the Tier 2a integration — ensuring the
    /// real Objects.json has every village NPC opted into the goal stack idle
    /// system (so they stop wandering and use chairs).
    /// </summary>
    [TestFixture]
    public class NpcBlueprintStayingTests
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

        private static readonly string[] StayingNpcBlueprints =
        {
            "Villager",
            "Elder",
            "Merchant",
            "Tinker",
            "Warden",
            "Farmer",
            "Scribe",
            "WellKeeper"
        };

        [Test]
        public void AllVillageNpcs_HaveStayingTrue()
        {
            foreach (var blueprintName in StayingNpcBlueprints)
            {
                var entity = _factory.CreateEntity(blueprintName);
                Assert.IsNotNull(entity, $"Blueprint '{blueprintName}' should exist");

                var brain = entity.GetPart<BrainPart>();
                Assert.IsNotNull(brain, $"{blueprintName} should have a BrainPart");
                Assert.IsTrue(brain.Staying,
                    $"{blueprintName}.Brain.Staying should be true so the NPC stays at its post");
            }
        }

        [Test]
        public void AllVillageNpcs_HaveWandersFalse()
        {
            foreach (var blueprintName in StayingNpcBlueprints)
            {
                var entity = _factory.CreateEntity(blueprintName);
                var brain = entity.GetPart<BrainPart>();
                Assert.IsFalse(brain.Wanders,
                    $"{blueprintName}.Brain.Wanders should be false (Staying NPCs don't wander)");
                Assert.IsFalse(brain.WandersRandomly,
                    $"{blueprintName}.Brain.WandersRandomly should be false");
            }
        }

        [Test]
        public void AllVillageNpcs_HaveAllowIdleBehaviorTag()
        {
            foreach (var blueprintName in StayingNpcBlueprints)
            {
                var entity = _factory.CreateEntity(blueprintName);
                Assert.IsTrue(entity.HasTag("AllowIdleBehavior"),
                    $"{blueprintName} should have AllowIdleBehavior tag to enable furniture scanning");
            }
        }

        [Test]
        public void AllVillageNpcs_HaveVillagersFaction()
        {
            foreach (var blueprintName in StayingNpcBlueprints)
            {
                var entity = _factory.CreateEntity(blueprintName);
                Assert.IsTrue(entity.HasTag("Faction"),
                    $"{blueprintName} should have a Faction tag");
                Assert.AreEqual("Villagers", entity.Tags["Faction"],
                    $"{blueprintName} should be in the Villagers faction");
            }
        }

        [Test]
        public void Warden_KeepsElevatedSightRadius()
        {
            // Warden is a guard — it should still have the higher sight radius (12)
            // that was set before the Tier 2a update. The update changes wander flags,
            // not sight.
            var warden = _factory.CreateEntity("Warden");
            var brain = warden.GetPart<BrainPart>();
            Assert.AreEqual(12, brain.SightRadius,
                "Warden should retain its elevated SightRadius for guard duty");
        }

        [Test]
        public void Farmer_InheritsFromVillager()
        {
            // Farmer inherits from Villager (not Creature), so it automatically
            // picks up Villager's Staying + AllowIdleBehavior via inheritance.
            // This test asserts that inheritance still holds after our changes.
            var farmer = _factory.CreateEntity("Farmer");
            Assert.IsTrue(farmer.GetPart<BrainPart>().Staying);
            Assert.IsTrue(farmer.HasTag("AllowIdleBehavior"));
        }

        [Test]
        public void WellKeeper_InheritsFromVillager()
        {
            var wellKeeper = _factory.CreateEntity("WellKeeper");
            Assert.IsTrue(wellKeeper.GetPart<BrainPart>().Staying);
            Assert.IsTrue(wellKeeper.HasTag("AllowIdleBehavior"));
        }

        [Test]
        public void Snapjaw_DoesNotHaveStaying()
        {
            // Sanity check: hostile monsters should NOT be Staying — they should
            // still chase the player. Tier 2a only affected friendly village NPCs.
            var snapjaw = _factory.CreateEntity("Snapjaw");
            Assert.IsNotNull(snapjaw);
            Assert.IsFalse(snapjaw.GetPart<BrainPart>().Staying,
                "Snapjaw should NOT be Staying — it is a hostile monster, not a villager");
            Assert.IsFalse(snapjaw.HasTag("AllowIdleBehavior"),
                "Snapjaw should not have AllowIdleBehavior tag");
        }

        // ========================
        // Tier 2b: Interior-biased NPC spawning
        // ========================

        [Test]
        public void VillagePopulationBuilder_SpawnsNPCsInsideBuildings()
        {
            // Generate a full village and verify most NPCs spawn on StoneFloor (interior)
            var poi = new PointOfInterest(POIType.Village, "Test Village", "Villagers");
            var villageBuilder = new VillageBuilder(BiomeType.Cave, poi);
            var populationBuilder = new VillagePopulationBuilder(poi);
            var zone = new Zone("Overworld.10.10.0");

            villageBuilder.BuildZone(zone, _factory, new System.Random(42));
            populationBuilder.BuildZone(zone, _factory, new System.Random(42));

            int npcTotal = 0;
            int npcOnStoneFloor = 0;

            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (!entity.HasTag("Creature")) continue;
                npcTotal++;

                var cell = zone.GetEntityCell(entity);
                if (cell == null) continue;

                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    if (cell.Objects[i].BlueprintName == "StoneFloor")
                    {
                        npcOnStoneFloor++;
                        break;
                    }
                }
            }

            Assert.Greater(npcTotal, 0, "Village should have at least one NPC");

            // With interior-biased spawning, MOST NPCs should be on StoneFloor.
            // Allow some to be on open cells (fallback when interior cells run out).
            float interiorRatio = (float)npcOnStoneFloor / npcTotal;
            Assert.GreaterOrEqual(interiorRatio, 0.5f,
                $"At least 50% of NPCs should spawn inside buildings (got {npcOnStoneFloor}/{npcTotal} = {interiorRatio:P0}). " +
                "This verifies the Tier 2b interior-biased spawning is working.");
        }
    }
}
