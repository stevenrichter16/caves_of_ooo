using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Elemental Crossroads (Phase C/R/E) coverage: the five new reaction files
    /// shipped to give the starting zone and its four cardinal neighbours
    /// reaction personality. Each test loads a single reaction file in
    /// isolation so failures point directly at the offending JSON.
    /// </summary>
    public class MaterialReactionPhaseCRETests
    {
        private static readonly string ReactionsFolder = Path.Combine(
            Application.dataPath, "Resources/Content/Data/MaterialReactions");

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            MaterialReactionResolver.Factory = null;
        }

        [TearDown]
        public void TearDown()
        {
            MaterialReactionResolver.Factory = null;
        }

        // ========================
        // Helpers
        // ========================

        private static void LoadSingleReaction(string fileName)
        {
            string path = Path.Combine(ReactionsFolder, fileName);
            MaterialReactionResolver.Initialize(File.ReadAllText(path));
        }

        private Entity MakeEntity(
            string tags = "",
            float temperature = 25f,
            float brittleness = 0f,
            float conductivity = 0f,
            float volatility = 0f,
            int hp = 100,
            float fuelMass = 100f,
            float burnRate = 1.0f,
            float heatOutput = 1.0f)
        {
            var e = new Entity();
            e.BlueprintName = "TestReactionTarget";
            e.Statistics["Hitpoints"] = new Stat { BaseValue = hp, Max = hp, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test target" });
            e.AddPart(new MaterialPart
            {
                MaterialID = "Test",
                Combustibility = 0.5f,
                MaterialTagsRaw = tags,
                Brittleness = brittleness,
                Conductivity = conductivity,
                Volatility = volatility
            });
            e.AddPart(new ThermalPart
            {
                Temperature = temperature,
                FlameTemperature = 500f,
                HeatCapacity = 1.0f
            });
            if (fuelMass > 0f)
            {
                e.AddPart(new FuelPart
                {
                    FuelMass = fuelMass,
                    MaxFuel = fuelMass,
                    BurnRate = burnRate,
                    HeatOutput = heatOutput
                });
            }
            return e;
        }

        private static EntityFactory LoadRealFactory()
        {
            var factory = new EntityFactory();
            string blueprintPath = Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            factory.LoadBlueprints(File.ReadAllText(blueprintPath));
            return factory;
        }

        // ========================
        // cold_plus_crystal
        // ========================

        [Test]
        public void ColdPlusCrystal_DealsDamage_WhenFrozenCrystal()
        {
            LoadSingleReaction("cold_plus_crystal.json");

            var e = MakeEntity(
                tags: "Crystal,Brittle",
                temperature: -10f,
                brittleness: 0.5f,
                hp: 20);
            e.ApplyEffect(new FrozenEffect(cold: 1.0f));
            int hpBefore = e.GetStatValue("Hitpoints");

            MaterialReactionResolver.EvaluateReactions(e, null, null);

            Assert.Less(e.GetStatValue("Hitpoints"), hpBefore,
                "cold_plus_crystal should deal damage to a frozen brittle crystal target.");
        }

        [Test]
        public void ColdPlusCrystal_NotBrittleEnough_DoesNotMatch()
        {
            LoadSingleReaction("cold_plus_crystal.json");

            var e = MakeEntity(
                tags: "Crystal",
                temperature: -10f,
                brittleness: 0.1f, // below MinBrittleness 0.3
                hp: 20);
            e.ApplyEffect(new FrozenEffect(cold: 1.0f));
            int hpBefore = e.GetStatValue("Hitpoints");

            MaterialReactionResolver.EvaluateReactions(e, null, null);

            Assert.AreEqual(hpBefore, e.GetStatValue("Hitpoints"),
                "cold_plus_crystal must require MinBrittleness 0.3.");
        }

        // ========================
        // cold_plus_chitinous
        // ========================

        [Test]
        public void ColdPlusChitinous_DealsDamage_WhenFrozenSpider()
        {
            LoadSingleReaction("cold_plus_chitinous.json");

            var e = MakeEntity(
                tags: "Organic,Chitinous",
                temperature: -5f,
                brittleness: 0.25f,
                hp: 20);
            e.ApplyEffect(new FrozenEffect(cold: 1.0f));
            int hpBefore = e.GetStatValue("Hitpoints");

            MaterialReactionResolver.EvaluateReactions(e, null, null);

            Assert.Less(e.GetStatValue("Hitpoints"), hpBefore,
                "cold_plus_chitinous should damage a frozen chitinous target (spiders/scorpions).");
        }

        [Test]
        public void ColdPlusChitinous_NotFrozen_DoesNotMatch()
        {
            LoadSingleReaction("cold_plus_chitinous.json");

            var e = MakeEntity(
                tags: "Organic,Chitinous",
                temperature: -5f,
                brittleness: 0.25f,
                hp: 20);
            // No FrozenEffect — should fail SourceState check.
            int hpBefore = e.GetStatValue("Hitpoints");

            MaterialReactionResolver.EvaluateReactions(e, null, null);

            Assert.AreEqual(hpBefore, e.GetStatValue("Hitpoints"),
                "cold_plus_chitinous must require SourceState Frozen.");
        }

        // ========================
        // fire_plus_bone
        // ========================

        [Test]
        public void FirePlusBone_DamagesBurningSkeleton()
        {
            LoadSingleReaction("fire_plus_bone.json");

            var e = MakeEntity(
                tags: "Bone,Dry,Undead",
                temperature: 600f,
                hp: 25);
            int hpBefore = e.GetStatValue("Hitpoints");

            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(e, null, burn);

            Assert.Less(e.GetStatValue("Hitpoints"), hpBefore,
                "fire_plus_bone should damage a burning bone-tagged target above its MinTemperature.");
        }

        [Test]
        public void FirePlusBone_WetBone_DoesNotMatch()
        {
            LoadSingleReaction("fire_plus_bone.json");

            var e = MakeEntity(
                tags: "Bone,Organic",
                temperature: 600f,
                hp: 25);
            e.ApplyEffect(new WetEffect(moisture: 0.6f));
            int hpBefore = e.GetStatValue("Hitpoints");

            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(e, null, burn);

            Assert.AreEqual(hpBefore, e.GetStatValue("Hitpoints"),
                "fire_plus_bone must reject wet bone targets via MaxMoisture 0.3.");
        }

        // ========================
        // fire_plus_fungal
        // ========================

        [Test]
        public void FirePlusFungal_PropagatesToAdjacentFungalEntity()
        {
            LoadSingleReaction("fire_plus_fungal.json");

            var zone = new Zone("TestZone");

            var source = MakeEntity(
                tags: "Organic,Fungal,Living",
                temperature: 260f,
                hp: 25);
            zone.AddEntity(source, 5, 5);

            var neighbor = MakeEntity(
                tags: "Organic,Fungal,Living",
                temperature: 25f,
                hp: 25);
            zone.AddEntity(neighbor, 6, 5);
            var neighborThermal = neighbor.GetPart<ThermalPart>();
            float tempBefore = neighborThermal.Temperature;

            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(source, zone, burn);

            Assert.Greater(neighborThermal.Temperature, tempBefore,
                "fire_plus_fungal should propagate heat along the Fungal tag to adjacent fungal entities.");
        }

        [Test]
        public void FirePlusFungal_DamagesBurningFungalSource()
        {
            LoadSingleReaction("fire_plus_fungal.json");

            var zone = new Zone("TestZone");
            var e = MakeEntity(
                tags: "Organic,Fungal",
                temperature: 260f,
                hp: 25);
            zone.AddEntity(e, 4, 4);
            int hpBefore = e.GetStatValue("Hitpoints");

            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(e, zone, burn);

            Assert.Less(e.GetStatValue("Hitpoints"), hpBefore,
                "fire_plus_fungal should damage the burning fungal source too.");
        }

        // ========================
        // fire_plus_ice
        // ========================

        [Test]
        public void FirePlusIce_SwapsIceEntityToWaterPuddle()
        {
            LoadSingleReaction("fire_plus_ice.json");
            var factory = LoadRealFactory();
            MaterialReactionResolver.Factory = factory;

            // Hand-build an ice target so the test doesn't depend on a specific
            // ice blueprint existing yet (Commit C adds IceStalactite/IceWight).
            var ice = MakeEntity(
                tags: "Ice,Brittle",
                temperature: 5f,
                hp: 10);
            ice.BlueprintName = "SyntheticIceTarget";

            var zone = new Zone("TestZone");
            zone.AddEntity(ice, 2, 2);

            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(ice, zone, burn);

            var cell = zone.GetCell(2, 2);
            Assert.IsNotNull(cell);
            bool puddled = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (cell.Objects[i].BlueprintName == "WaterPuddle")
                {
                    puddled = true;
                    break;
                }
            }
            Assert.IsTrue(puddled,
                "fire_plus_ice should swap a burning Ice-tagged entity into a WaterPuddle.");
        }

        [Test]
        public void FirePlusIce_NonIce_DoesNotSwap()
        {
            LoadSingleReaction("fire_plus_ice.json");
            var factory = LoadRealFactory();
            MaterialReactionResolver.Factory = factory;

            var rock = MakeEntity(
                tags: "Mineral,Stone",
                temperature: 100f,
                hp: 50);
            rock.BlueprintName = "SyntheticRock";

            var zone = new Zone("TestZone");
            zone.AddEntity(rock, 1, 1);

            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(rock, zone, burn);

            var cell = zone.GetCell(1, 1);
            bool puddled = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (cell.Objects[i].BlueprintName == "WaterPuddle")
                    puddled = true;
            }
            Assert.IsFalse(puddled,
                "fire_plus_ice must not swap non-Ice targets.");
        }
    }
}
