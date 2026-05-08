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

        // WRS.M4 — Quartermaster wire-in. Without this test the
        // rental feature could regress to "blueprint exists but never
        // spawned" (the gap shipped pre-M4). Loading real blueprints
        // also exercises the inheritance chain that cold-eye-3 caught
        // (LoanerDagger → MeleeWeapon → Item → Stacker), confirming
        // the per-blueprint MaxStack=1 override survives bake.
        [Test]
        public void BuildZone_SpawnsQuartermasterStockedWithLoaners()
        {
            var poi = new PointOfInterest(POIType.Village, "Starting Village", "Villagers");
            var villageBuilder = new VillageBuilder(BiomeType.Cave, poi);
            var populationBuilder = new VillagePopulationBuilder(poi);
            var zone = new Zone("Overworld.10.10.0");

            Assert.IsTrue(villageBuilder.BuildZone(zone, _factory, new Random(42)));
            Assert.IsTrue(populationBuilder.BuildZone(zone, _factory, new Random(42)));

            Entity qm = FindByBlueprint(zone, "Quartermaster");
            Assert.IsNotNull(qm, "Quartermaster must spawn in every village (Docs/WEAPON-RENTAL-SYSTEM.md M4).");

            var inv = qm.GetPart<InventoryPart>();
            Assert.IsNotNull(inv, "Quartermaster must inherit InventoryPart from Creature.");

            // Stock check + inheritance trap counter-check: each
            // Loaner must round-trip through EntityFactory with
            // IsRentable == true. If the Stacker MaxStack=1 override
            // ever regresses, this assertion fires.
            string[] expectedStock = { "LoanerDagger", "LoanerSpear", "LoanerLongsword" };
            for (int i = 0; i < expectedStock.Length; i++)
            {
                Entity item = null;
                for (int j = 0; j < inv.Objects.Count; j++)
                {
                    if (inv.Objects[j].BlueprintName == expectedStock[i])
                    {
                        item = inv.Objects[j];
                        break;
                    }
                }
                Assert.IsNotNull(item, $"Quartermaster must stock {expectedStock[i]}.");
                Assert.IsTrue(RentalSystem.IsRentable(item),
                    $"{expectedStock[i]} must satisfy IsRentable on the production blueprint " +
                    "(Stacker MaxStack=1 override + Rentable tag + CommercePart).");
            }
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
