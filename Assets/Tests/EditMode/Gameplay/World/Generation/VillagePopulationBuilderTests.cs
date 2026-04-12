using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    public class VillagePopulationBuilderTests
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
        public void BuildZone_StartingVillage_PinsGrimoireChestNearVillageSquare()
        {
            var poi = new PointOfInterest(POIType.Village, "Starting Village", "Villagers");
            var villageBuilder = new VillageBuilder(BiomeType.Cave, poi);
            var populationBuilder = new VillagePopulationBuilder(poi);
            var zone = new Zone("Overworld.10.10.0");

            Assert.IsTrue(villageBuilder.BuildZone(zone, _factory, new Random(42)));
            Assert.IsTrue(populationBuilder.BuildZone(zone, _factory, new Random(42)));

            Entity chest = FindByBlueprint(zone, "Chest");
            Assert.IsNotNull(chest, "Expected the grimoire chest to be placed in the starting village.");

            Cell chestCell = zone.GetEntityCell(chest);
            Assert.IsNotNull(chestCell);
            Assert.AreEqual(43, chestCell.X);
            Assert.AreEqual(11, chestCell.Y);

            var container = chest.GetPart<ContainerPart>();
            Assert.IsNotNull(container);
            Assert.Greater(container.Contents.Count, 0);
        }

        private static Entity FindByBlueprint(Zone zone, string blueprintName)
        {
            var entities = zone.GetAllEntities();
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].BlueprintName == blueprintName)
                    return entities[i];
            }

            return null;
        }
    }
}
