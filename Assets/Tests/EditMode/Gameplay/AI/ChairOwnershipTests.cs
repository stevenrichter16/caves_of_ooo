using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tier 3d: Per-NPC chair ownership.
    /// Verifies that an Innkeeper spawns with exclusive ownership of a nearby chair,
    /// and that ChairPart's Owner filter correctly rejects non-owners.
    /// </summary>
    [TestFixture]
    public class ChairOwnershipTests
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
        public void Innkeeper_Blueprint_Exists()
        {
            var innkeeper = _factory.CreateEntity("Innkeeper");
            Assert.IsNotNull(innkeeper, "Innkeeper blueprint should exist");
            Assert.AreEqual("innkeeper", innkeeper.GetPart<RenderPart>()?.DisplayName);
        }

        [Test]
        public void Innkeeper_InheritsVillagerBehavior()
        {
            var innkeeper = _factory.CreateEntity("Innkeeper");
            var brain = innkeeper.GetPart<BrainPart>();

            Assert.IsNotNull(brain);
            Assert.IsTrue(brain.Staying, "Innkeeper inherits Staying from Villager");
            Assert.IsTrue(innkeeper.HasTag("AllowIdleBehavior"), "Innkeeper inherits AllowIdleBehavior tag");
            Assert.AreEqual("Villagers", innkeeper.Tags["Faction"]);
        }

        [Test]
        public void VillagePopulationBuilder_SpawnsInnkeeper()
        {
            var poi = new PointOfInterest(POIType.Village, "Test Village", "Villagers");
            var villageBuilder = new VillageBuilder(BiomeType.Cave, poi);
            var populationBuilder = new VillagePopulationBuilder(poi);
            var zone = new Zone("Overworld.10.10.0");

            villageBuilder.BuildZone(zone, _factory, new System.Random(42));
            populationBuilder.BuildZone(zone, _factory, new System.Random(42));

            Entity innkeeper = null;
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity.BlueprintName == "Innkeeper")
                {
                    innkeeper = entity;
                    break;
                }
            }

            Assert.IsNotNull(innkeeper, "Village should have an Innkeeper");
        }

        [Test]
        public void Innkeeper_OwnsNearestChair()
        {
            var poi = new PointOfInterest(POIType.Village, "Test Village", "Villagers");
            var villageBuilder = new VillageBuilder(BiomeType.Cave, poi);
            var populationBuilder = new VillagePopulationBuilder(poi);
            var zone = new Zone("Overworld.10.10.0");

            villageBuilder.BuildZone(zone, _factory, new System.Random(42));
            populationBuilder.BuildZone(zone, _factory, new System.Random(42));

            // Find the innkeeper
            Entity innkeeper = null;
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity.BlueprintName == "Innkeeper")
                {
                    innkeeper = entity;
                    break;
                }
            }
            Assert.IsNotNull(innkeeper);

            // Find the chair owned by the innkeeper
            Entity ownedChair = null;
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                var chairPart = entity.GetPart<ChairPart>();
                if (chairPart != null && chairPart.Owner == innkeeper.ID)
                {
                    ownedChair = entity;
                    break;
                }
            }

            Assert.IsNotNull(ownedChair,
                "Innkeeper should own a chair (ChairPart.Owner == innkeeper.ID)");
        }

        [Test]
        public void InnkeepersChair_RejectsOtherVillagers()
        {
            // Spawn an innkeeper with an owned chair, and a separate villager.
            // The villager should NOT be able to query the innkeeper's chair.
            var zone = new Zone("TestZone");

            var innkeeper = _factory.CreateEntity("Innkeeper");
            var innkeeperBrain = innkeeper.GetPart<BrainPart>();
            innkeeperBrain.CurrentZone = zone;
            innkeeperBrain.Rng = new System.Random(1);
            zone.AddEntity(innkeeper, 5, 5);

            var chair = _factory.CreateEntity("Chair");
            zone.AddEntity(chair, 6, 5);
            chair.GetPart<ChairPart>().Owner = innkeeper.ID;

            // Create a regular villager (different ID, has AllowIdleBehavior)
            var villager = _factory.CreateEntity("Villager");
            var villagerBrain = villager.GetPart<BrainPart>();
            villagerBrain.CurrentZone = zone;
            villagerBrain.Rng = new System.Random(2);
            zone.AddEntity(villager, 4, 5);

            // Villager tries to query the innkeeper's chair — should be rejected
            var offer = IdleQueryEvent.QueryOffer(chair, villager);

            Assert.IsNull(offer,
                "Villager should NOT be able to query the innkeeper's chair — Owner filter rejects non-owner");
            Assert.IsFalse(chair.GetPart<ChairPart>().Occupied,
                "Rejected query should not reserve the chair");
        }

        [Test]
        public void InnkeepersChair_AcceptsInnkeeper()
        {
            var zone = new Zone("TestZone");

            var innkeeper = _factory.CreateEntity("Innkeeper");
            var brain = innkeeper.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(1);
            zone.AddEntity(innkeeper, 5, 5);

            var chair = _factory.CreateEntity("Chair");
            zone.AddEntity(chair, 6, 5);
            chair.GetPart<ChairPart>().Owner = innkeeper.ID;

            // Innkeeper queries their own chair — should succeed
            var offer = IdleQueryEvent.QueryOffer(chair, innkeeper);

            Assert.IsNotNull(offer, "Innkeeper should be able to query their own chair");
            Assert.IsTrue(chair.GetPart<ChairPart>().Occupied,
                "Chair should be reserved at query time");
        }

        [Test]
        public void UnownedChair_AcceptsAnyAllowIdleBehaviorNPC()
        {
            // Sanity check: Owner="" means any NPC with AllowIdleBehavior can use it.
            // This is the default Tier 1 behavior — only innkeeper's chair is restricted.
            var zone = new Zone("TestZone");

            var villager = _factory.CreateEntity("Villager");
            var brain = villager.GetPart<BrainPart>();
            brain.CurrentZone = zone;
            brain.Rng = new System.Random(1);
            zone.AddEntity(villager, 5, 5);

            var chair = _factory.CreateEntity("Chair");
            zone.AddEntity(chair, 6, 5);
            // Owner stays empty (default)

            var offer = IdleQueryEvent.QueryOffer(chair, villager);

            Assert.IsNotNull(offer, "Unowned chair should accept any AllowIdleBehavior NPC");
        }
    }
}
