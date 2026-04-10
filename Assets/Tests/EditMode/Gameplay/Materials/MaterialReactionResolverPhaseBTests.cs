using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase B coverage: the seven data-driven material reactions shipped alongside
    /// the frost / electricity / acid / vapor primitives. Each test loads a single
    /// reaction file (via File.ReadAllText against the real Resources path) and
    /// exercises the match + effect pipeline against hand-constructed entities so
    /// failures point at a specific reaction rather than at a folder-wide smoke test.
    /// </summary>
    public class MaterialReactionResolverPhaseBTests
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

        private static string ReadReaction(string fileName)
        {
            return File.ReadAllText(Path.Combine(ReactionsFolder, fileName));
        }

        private static void LoadSingleReaction(string fileName)
        {
            MaterialReactionResolver.Initialize(ReadReaction(fileName));
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
        // Smoke test — the real content bundle loads cleanly
        // ========================

        [Test]
        public void AllReactionFiles_LoadWithoutError()
        {
            var sources = new List<string>();
            foreach (string path in Directory.GetFiles(ReactionsFolder, "*.json"))
                sources.Add(File.ReadAllText(path));

            Assert.Greater(sources.Count, 0, "Expected at least one reaction file.");

            MaterialReactionResolver.InitializeFromJsonSources(sources);

            Assert.IsTrue(MaterialReactionResolver.IsInitialized);
            // fire_plus_organic + water_plus_fire + oil_plus_fire + cold_plus_metal
            // + lightning_plus_conductor + acid_plus_organic + fire_plus_raw_meat
            // + fire_plus_raw_starapple = 8
            Assert.AreEqual(8, MaterialReactionResolver.ReactionCount,
                "Eight reaction files should produce eight reactions.");
        }

        // ========================
        // water_plus_fire
        // ========================

        [Test]
        public void WaterPlusFire_WetBurningEntity_ReducesIntensity()
        {
            LoadSingleReaction("water_plus_fire.json");

            var e = MakeEntity();
            e.ApplyEffect(new WetEffect(moisture: 0.5f));

            var burn = new BurningEffect(intensity: 3.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(e, null, burn);

            Assert.AreEqual(2.0f, burn.Intensity, 0.01f,
                "water_plus_fire should subtract 1.0 from burn intensity on a wet burning target.");
        }

        [Test]
        public void WaterPlusFire_DryBurningEntity_DoesNotMatch()
        {
            LoadSingleReaction("water_plus_fire.json");

            var e = MakeEntity();
            var burn = new BurningEffect(intensity: 3.0f, rng: new Random(42));

            MaterialReactionResolver.EvaluateReactions(e, null, burn);

            Assert.AreEqual(3.0f, burn.Intensity, 0.01f,
                "water_plus_fire must not trigger when MinMoisture is not met.");
        }

        // ========================
        // oil_plus_fire
        // ========================

        [Test]
        public void OilPlusFire_OilTaggedBurningEntity_IncreasesIntensityAndConsumesFuel()
        {
            LoadSingleReaction("oil_plus_fire.json");

            var e = MakeEntity(tags: "Liquid,Oil,Flammable,Organic", temperature: 200f);
            var fuel = e.GetPart<FuelPart>();
            float fuelBefore = fuel.FuelMass;

            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(e, null, burn);

            Assert.AreEqual(3.0f, burn.Intensity, 0.01f,
                "oil_plus_fire should add 2.0 to burn intensity.");
            Assert.Less(fuel.FuelMass, fuelBefore,
                "oil_plus_fire should drive bonus fuel consumption via ModifyFuelConsumption.");
        }

        [Test]
        public void OilPlusFire_NonOilEntity_DoesNotMatch()
        {
            LoadSingleReaction("oil_plus_fire.json");

            var e = MakeEntity(tags: "Organic,Flammable", temperature: 200f);
            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));

            MaterialReactionResolver.EvaluateReactions(e, null, burn);

            Assert.AreEqual(1.0f, burn.Intensity, 0.01f,
                "oil_plus_fire must not trigger on non-Oil-tagged targets.");
        }

        [Test]
        public void OilPlusFire_BelowMinTemperature_DoesNotMatch()
        {
            LoadSingleReaction("oil_plus_fire.json");

            var e = MakeEntity(tags: "Liquid,Oil,Flammable", temperature: 100f);
            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));

            MaterialReactionResolver.EvaluateReactions(e, null, burn);

            Assert.AreEqual(1.0f, burn.Intensity, 0.01f,
                "oil_plus_fire must not trigger below its MinTemperature threshold.");
        }

        // ========================
        // cold_plus_metal
        // ========================

        [Test]
        public void ColdPlusMetal_FrozenBrittleMetal_TakesDamage()
        {
            LoadSingleReaction("cold_plus_metal.json");

            var e = MakeEntity(
                tags: "Metal,Conductor",
                temperature: -10f,
                brittleness: 0.3f,
                hp: 20);
            e.ApplyEffect(new FrozenEffect(cold: 1.0f));
            int hpBefore = e.GetStatValue("Hitpoints");

            MaterialReactionResolver.EvaluateReactions(e, null, null);

            Assert.Less(e.GetStatValue("Hitpoints"), hpBefore,
                "cold_plus_metal should deal damage to a frozen brittle metal target.");
        }

        [Test]
        public void ColdPlusMetal_NotFrozen_DoesNotMatch()
        {
            LoadSingleReaction("cold_plus_metal.json");

            var e = MakeEntity(
                tags: "Metal,Conductor",
                temperature: -10f,
                brittleness: 0.3f,
                hp: 20);
            int hpBefore = e.GetStatValue("Hitpoints");

            MaterialReactionResolver.EvaluateReactions(e, null, null);

            Assert.AreEqual(hpBefore, e.GetStatValue("Hitpoints"),
                "cold_plus_metal must require SourceState Frozen.");
        }

        [Test]
        public void ColdPlusMetal_AboveMaxTemperature_DoesNotMatch()
        {
            LoadSingleReaction("cold_plus_metal.json");

            var e = MakeEntity(
                tags: "Metal,Conductor",
                temperature: 25f, // above MaxTemperature 0
                brittleness: 0.3f,
                hp: 20);
            e.ApplyEffect(new FrozenEffect(cold: 1.0f));
            int hpBefore = e.GetStatValue("Hitpoints");

            MaterialReactionResolver.EvaluateReactions(e, null, null);

            Assert.AreEqual(hpBefore, e.GetStatValue("Hitpoints"),
                "cold_plus_metal must require MaxTemperature <= 0.");
        }

        [Test]
        public void ColdPlusMetal_NotBrittleEnough_DoesNotMatch()
        {
            LoadSingleReaction("cold_plus_metal.json");

            var e = MakeEntity(
                tags: "Metal,Conductor",
                temperature: -10f,
                brittleness: 0.05f, // below MinBrittleness 0.15
                hp: 20);
            e.ApplyEffect(new FrozenEffect(cold: 1.0f));
            int hpBefore = e.GetStatValue("Hitpoints");

            MaterialReactionResolver.EvaluateReactions(e, null, null);

            Assert.AreEqual(hpBefore, e.GetStatValue("Hitpoints"),
                "cold_plus_metal must require MinBrittleness to fire.");
        }

        // ========================
        // lightning_plus_conductor
        // ========================

        [Test]
        public void LightningPlusConductor_PropagatesHeatToConductorNeighbor()
        {
            LoadSingleReaction("lightning_plus_conductor.json");

            var zone = new Zone("TestZone");
            var source = MakeEntity(tags: "Metal,Conductor", conductivity: 0.8f);
            zone.AddEntity(source, 5, 5);
            source.ApplyEffect(new ElectrifiedEffect(charge: 1.0f));

            var neighbor = MakeEntity(tags: "Metal,Conductor", conductivity: 0.8f);
            zone.AddEntity(neighbor, 6, 5);
            var neighborThermal = neighbor.GetPart<ThermalPart>();
            float tempBefore = neighborThermal.Temperature;

            MaterialReactionResolver.EvaluateReactions(source, zone, null);

            Assert.Greater(neighborThermal.Temperature, tempBefore,
                "lightning_plus_conductor should heat a conductor neighbor via PropagateAlongTag.");
        }

        [Test]
        public void LightningPlusConductor_LowConductivitySource_DoesNotPropagate()
        {
            LoadSingleReaction("lightning_plus_conductor.json");

            var zone = new Zone("TestZone");
            // Source has Conductor tag but fails the MinConductivity 0.5 gate.
            var source = MakeEntity(tags: "Metal,Conductor", conductivity: 0.2f);
            zone.AddEntity(source, 5, 5);
            source.ApplyEffect(new ElectrifiedEffect(charge: 1.0f));

            var neighbor = MakeEntity(tags: "Metal,Conductor", conductivity: 0.8f);
            zone.AddEntity(neighbor, 6, 5);
            var neighborThermal = neighbor.GetPart<ThermalPart>();
            float tempBefore = neighborThermal.Temperature;

            MaterialReactionResolver.EvaluateReactions(source, zone, null);

            Assert.AreEqual(tempBefore, neighborThermal.Temperature, 0.001f,
                "Reaction must reject a source that fails the MinConductivity gate.");
        }

        [Test]
        public void LightningPlusConductor_NotElectrified_DoesNotPropagate()
        {
            LoadSingleReaction("lightning_plus_conductor.json");

            var zone = new Zone("TestZone");
            var source = MakeEntity(tags: "Metal,Conductor", conductivity: 0.8f);
            zone.AddEntity(source, 5, 5);
            // No ElectrifiedEffect — should fail SourceState check.

            var neighbor = MakeEntity(tags: "Metal,Conductor", conductivity: 0.8f);
            zone.AddEntity(neighbor, 6, 5);
            var neighborThermal = neighbor.GetPart<ThermalPart>();
            float tempBefore = neighborThermal.Temperature;

            MaterialReactionResolver.EvaluateReactions(source, zone, null);

            Assert.AreEqual(tempBefore, neighborThermal.Temperature, 0.001f,
                "Reaction must require SourceState Electrified.");
        }

        // ========================
        // acid_plus_organic
        // ========================

        [Test]
        public void AcidPlusOrganic_AcidicOrganic_TakesDamage()
        {
            LoadSingleReaction("acid_plus_organic.json");

            var e = MakeEntity(tags: "Organic,Flammable", hp: 20);
            e.ApplyEffect(new AcidicEffect(corrosion: 0.5f));
            int hpBefore = e.GetStatValue("Hitpoints");

            MaterialReactionResolver.EvaluateReactions(e, null, null);

            Assert.Less(e.GetStatValue("Hitpoints"), hpBefore,
                "acid_plus_organic should deal damage to acid-coated organic matter.");
        }

        [Test]
        public void AcidPlusOrganic_NonOrganic_DoesNotMatch()
        {
            LoadSingleReaction("acid_plus_organic.json");

            var e = MakeEntity(tags: "Metal", hp: 20);
            e.ApplyEffect(new AcidicEffect(corrosion: 0.5f));
            int hpBefore = e.GetStatValue("Hitpoints");

            MaterialReactionResolver.EvaluateReactions(e, null, null);

            Assert.AreEqual(hpBefore, e.GetStatValue("Hitpoints"),
                "acid_plus_organic must not damage non-Organic targets.");
        }

        [Test]
        public void AcidPlusOrganic_BurningAcidic_AcceleratesFuelConsumption()
        {
            LoadSingleReaction("acid_plus_organic.json");

            var e = MakeEntity(tags: "Organic,Flammable", hp: 20);
            e.ApplyEffect(new AcidicEffect(corrosion: 0.5f));
            var fuel = e.GetPart<FuelPart>();
            float fuelBefore = fuel.FuelMass;

            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(e, null, burn);

            Assert.Less(fuel.FuelMass, fuelBefore,
                "acid_plus_organic should accelerate fuel consumption on a burning acidic organic.");
        }

        // ========================
        // fire_plus_raw_meat
        // ========================

        [Test]
        public void FirePlusRawMeat_HotRawMeat_SwapsToCookedMeat()
        {
            LoadSingleReaction("fire_plus_raw_meat.json");
            var factory = LoadRealFactory();
            MaterialReactionResolver.Factory = factory;

            var meat = factory.CreateEntity("RawMeat");
            Assert.IsNotNull(meat, "RawMeat blueprint should exist.");
            meat.GetPart<ThermalPart>().Temperature = 180f;

            var zone = new Zone("TestZone");
            zone.AddEntity(meat, 5, 5);

            MaterialReactionResolver.EvaluateReactions(meat, zone, null);

            // After SwapBlueprint, the original meat entity is removed from the zone
            // and a CookedMeat entity takes its place in the same cell.
            var cell = zone.GetCell(5, 5);
            Assert.IsNotNull(cell);
            bool cooked = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (cell.Objects[i].BlueprintName == "CookedMeat")
                {
                    cooked = true;
                    break;
                }
            }
            Assert.IsTrue(cooked, "Hot raw meat should swap to CookedMeat.");
        }

        [Test]
        public void FirePlusRawMeat_ColdRawMeat_DoesNotSwap()
        {
            LoadSingleReaction("fire_plus_raw_meat.json");
            var factory = LoadRealFactory();
            MaterialReactionResolver.Factory = factory;

            var meat = factory.CreateEntity("RawMeat");
            Assert.IsNotNull(meat);
            // Ambient-temperature meat should not cook.
            meat.GetPart<ThermalPart>().Temperature = 25f;

            var zone = new Zone("TestZone");
            zone.AddEntity(meat, 5, 5);

            MaterialReactionResolver.EvaluateReactions(meat, zone, null);

            var cell = zone.GetCell(5, 5);
            bool stillRaw = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (cell.Objects[i].BlueprintName == "RawMeat")
                {
                    stillRaw = true;
                    break;
                }
            }
            Assert.IsTrue(stillRaw, "Cold raw meat must not cook.");
        }

        // ========================
        // fire_plus_raw_starapple
        // ========================

        [Test]
        public void FirePlusRawStarapple_HotStarapple_SwapsToRoasted()
        {
            LoadSingleReaction("fire_plus_raw_starapple.json");
            var factory = LoadRealFactory();
            MaterialReactionResolver.Factory = factory;

            var apple = factory.CreateEntity("Starapple");
            Assert.IsNotNull(apple, "Starapple blueprint should exist.");
            apple.GetPart<ThermalPart>().Temperature = 200f;

            var zone = new Zone("TestZone");
            zone.AddEntity(apple, 3, 3);

            MaterialReactionResolver.EvaluateReactions(apple, zone, null);

            var cell = zone.GetCell(3, 3);
            Assert.IsNotNull(cell);
            bool roasted = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (cell.Objects[i].BlueprintName == "RoastedStarapple")
                {
                    roasted = true;
                    break;
                }
            }
            Assert.IsTrue(roasted, "Hot raw starapple should swap to RoastedStarapple.");
        }

        [Test]
        public void FirePlusRawStarapple_DoesNotAffectCookedMeat()
        {
            // Cross-contamination check: fire_plus_raw_starapple must not fire on meat.
            LoadSingleReaction("fire_plus_raw_starapple.json");
            var factory = LoadRealFactory();
            MaterialReactionResolver.Factory = factory;

            var meat = factory.CreateEntity("RawMeat");
            meat.GetPart<ThermalPart>().Temperature = 200f;

            var zone = new Zone("TestZone");
            zone.AddEntity(meat, 2, 2);

            MaterialReactionResolver.EvaluateReactions(meat, zone, null);

            var cell = zone.GetCell(2, 2);
            bool stillMeat = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (cell.Objects[i].BlueprintName == "RawMeat")
                {
                    stillMeat = true;
                    break;
                }
            }
            Assert.IsTrue(stillMeat,
                "fire_plus_raw_starapple must not target RawMeat.");
        }

        // ========================
        // Schema — MinMoisture field added in Chunk B
        // ========================

        [Test]
        public void MinMoistureCondition_RequiresWetEffect()
        {
            // Hand-crafted JSON exercising the new MinMoisture field so any future
            // regression in MatchesConditions shows up here rather than inside a
            // composite reaction.
            string json = @"{
                ""Reactions"": [{
                    ""ID"": ""min_moisture_probe"",
                    ""Priority"": 10,
                    ""Conditions"": { ""SourceState"": ""Burning"", ""MinMoisture"": 0.4 },
                    ""Effects"": [
                        { ""Type"": ""ModifyBurnIntensity"", ""FloatValue"": -0.5, ""StringValue"": """" }
                    ]
                }]
            }";
            MaterialReactionResolver.Initialize(json);

            var dry = MakeEntity();
            var dryBurn = new BurningEffect(intensity: 2.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(dry, null, dryBurn);
            Assert.AreEqual(2.0f, dryBurn.Intensity, 0.01f,
                "MinMoisture should reject entities without a WetEffect.");

            var wet = MakeEntity();
            wet.ApplyEffect(new WetEffect(moisture: 0.5f));
            var wetBurn = new BurningEffect(intensity: 2.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(wet, null, wetBurn);
            Assert.AreEqual(1.5f, wetBurn.Intensity, 0.01f,
                "MinMoisture should accept entities whose WetEffect meets the threshold.");

            var barelyWet = MakeEntity();
            barelyWet.ApplyEffect(new WetEffect(moisture: 0.2f));
            var barelyBurn = new BurningEffect(intensity: 2.0f, rng: new Random(42));
            MaterialReactionResolver.EvaluateReactions(barelyWet, null, barelyBurn);
            Assert.AreEqual(2.0f, barelyBurn.Intensity, 0.01f,
                "MinMoisture should reject entities whose WetEffect is below the threshold.");
        }
    }
}
