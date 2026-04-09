using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class MaterialSystemTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ========================
        // Helpers
        // ========================

        private Entity CreateEntity(
            string materialID = "Wood",
            float combustibility = 0.7f,
            string tags = "Organic,Flammable",
            float flameTemp = 400f,
            float heatCapacity = 1.0f,
            float fuelMass = 100f,
            int hp = 100)
        {
            var e = new Entity();
            e.BlueprintName = "TestObject";
            e.Statistics["Hitpoints"] = new Stat { BaseValue = hp, Max = hp, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test object" });
            e.AddPart(new MaterialPart
            {
                MaterialID = materialID,
                Combustibility = combustibility,
                MaterialTagsRaw = tags
            });
            e.AddPart(new ThermalPart
            {
                FlameTemperature = flameTemp,
                HeatCapacity = heatCapacity
            });
            if (fuelMass > 0)
            {
                e.AddPart(new FuelPart
                {
                    FuelMass = fuelMass,
                    MaxFuel = fuelMass,
                    BurnRate = 1.0f,
                    HeatOutput = 1.0f
                });
            }
            return e;
        }

        private Entity CreateCreature(int hp = 100)
        {
            var e = new Entity();
            e.BlueprintName = "TestCreature";
            e.Statistics["Hitpoints"] = new Stat { BaseValue = hp, Max = hp, Owner = e };
            e.Statistics["HP"] = new Stat { BaseValue = hp, Max = hp, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test creature" });
            e.AddPart(new MaterialPart
            {
                MaterialID = "Flesh",
                Combustibility = 0.15f,
                MaterialTagsRaw = "Organic"
            });
            e.AddPart(new ThermalPart
            {
                FlameTemperature = 500f,
                HeatCapacity = 2.0f
            });
            return e;
        }

        // ========================
        // MaterialPart
        // ========================

        [Test]
        public void MaterialPart_ParsesCommaSeparatedTags()
        {
            var mp = new MaterialPart { MaterialTagsRaw = "Organic,Flammable,Wood" };
            var e = new Entity();
            e.AddPart(mp);

            Assert.IsTrue(mp.HasMaterialTag("Organic"));
            Assert.IsTrue(mp.HasMaterialTag("Flammable"));
            Assert.IsTrue(mp.HasMaterialTag("Wood"));
            Assert.IsFalse(mp.HasMaterialTag("Metal"));
        }

        [Test]
        public void MaterialPart_EmptyTagsRaw_NoTags()
        {
            var mp = new MaterialPart { MaterialTagsRaw = "" };
            var e = new Entity();
            e.AddPart(mp);

            Assert.AreEqual(0, mp.MaterialTags.Count);
        }

        [Test]
        public void MaterialPart_TagsAppliedToEntity()
        {
            var e = CreateEntity(tags: "Organic,Flammable");

            Assert.IsTrue(e.HasTag("Organic"));
            Assert.IsTrue(e.HasTag("Flammable"));
        }

        [Test]
        public void MaterialPart_ZeroCombustibility_CancelsTryIgnite()
        {
            var e = CreateEntity(combustibility: 0f);

            var tryIgnite = GameEvent.New("TryIgnite");
            bool result = e.FireEvent(tryIgnite);

            Assert.IsFalse(result, "TryIgnite should be blocked by zero combustibility");
            tryIgnite.Release();
        }

        [Test]
        public void MaterialPart_PositiveCombustibility_AllowsTryIgnite()
        {
            var e = CreateEntity(combustibility: 0.7f);

            var tryIgnite = GameEvent.New("TryIgnite");
            bool result = e.FireEvent(tryIgnite);

            Assert.IsTrue(result, "TryIgnite should be allowed with positive combustibility");
            tryIgnite.Release();
        }

        [Test]
        public void MaterialPart_QueryMaterial_ExposesProperties()
        {
            var e = CreateEntity(materialID: "Wood", combustibility: 0.7f);

            var query = GameEvent.New("QueryMaterial");
            e.FireEvent(query);

            Assert.AreEqual("Wood", query.GetStringParameter("MaterialID"));
            query.Release();
        }

        // ========================
        // ThermalPart
        // ========================

        [Test]
        public void ThermalPart_DirectHeat_IncreasesTemperature()
        {
            var e = CreateEntity(heatCapacity: 1.0f);
            var thermal = e.GetPart<ThermalPart>();
            float before = thermal.Temperature;

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)100f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.Greater(thermal.Temperature, before, "Direct heat should increase temperature");
            Assert.AreEqual(before + 100f, thermal.Temperature, 0.01f);
        }

        [Test]
        public void ThermalPart_DirectHeat_ScaledByHeatCapacity()
        {
            var e = CreateEntity(heatCapacity: 2.0f);
            var thermal = e.GetPart<ThermalPart>();
            float before = thermal.Temperature;

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)100f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.AreEqual(before + 50f, thermal.Temperature, 0.01f,
                "Heat capacity 2.0 should halve temperature increase");
        }

        [Test]
        public void ThermalPart_RadiantHeat_ConvergesAsymptotically()
        {
            var e = CreateEntity(heatCapacity: 1.0f);
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 25f;

            // Apply radiant heat many times — should converge toward joules value, never exceed it
            for (int i = 0; i < 100; i++)
            {
                var heat = GameEvent.New("ApplyHeat");
                heat.SetParameter("Joules", (object)500f);
                heat.SetParameter("Radiant", (object)true);
                e.FireEvent(heat);
                heat.Release();
            }

            Assert.Less(thermal.Temperature, 500f,
                "Radiant heat should converge asymptotically, never reaching source temperature");
            Assert.Greater(thermal.Temperature, 25f,
                "Radiant heat should increase temperature");
        }

        [Test]
        public void ThermalPart_AmbientDecay_CoolsOverEndTurn()
        {
            var e = CreateEntity();
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 200f;
            thermal.AmbientDecayRate = 0.1f;
            thermal.AmbientTemperature = 25f;

            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.Less(thermal.Temperature, 200f, "Temperature should decrease toward ambient");
            Assert.Greater(thermal.Temperature, 25f, "Should not instantly reach ambient");
        }

        [Test]
        public void ThermalPart_AmbientDecay_SnapsWhenClose()
        {
            var e = CreateEntity();
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 25.3f;
            thermal.AmbientTemperature = 25f;
            thermal.AmbientDecayRate = 0.5f;

            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.AreEqual(25f, thermal.Temperature, 0.01f,
                "Temperature should snap to ambient when close enough");
        }

        // ========================
        // FuelPart
        // ========================

        [Test]
        public void FuelPart_ConsumeFuel_ReducesMass()
        {
            var e = CreateEntity(fuelMass: 100f);
            var fuel = e.GetPart<FuelPart>();

            var consume = GameEvent.New("ConsumeFuel");
            consume.SetParameter("Intensity", (object)1.0f);
            e.FireEvent(consume);

            bool exhausted = consume.GetParameter<object>("Exhausted") is bool ex && ex;
            consume.Release();

            Assert.Less(fuel.FuelMass, 100f, "Fuel mass should decrease after consumption");
            Assert.IsFalse(exhausted, "Should not be exhausted after one consumption");
        }

        [Test]
        public void FuelPart_ConsumeFuel_ReportsExhaustion()
        {
            var e = CreateEntity(fuelMass: 0.5f);
            var fuel = e.GetPart<FuelPart>();
            fuel.BurnRate = 1.0f;

            var consume = GameEvent.New("ConsumeFuel");
            consume.SetParameter("Intensity", (object)1.0f);
            e.FireEvent(consume);

            bool exhausted = consume.GetParameter<object>("Exhausted") is bool ex && ex;
            consume.Release();

            Assert.IsTrue(exhausted, "Fuel should report exhaustion when depleted");
            Assert.AreEqual(0f, fuel.FuelMass, 0.01f);
        }

        [Test]
        public void FuelPart_ConsumeFuel_ReportsHeatProduced()
        {
            var e = CreateEntity(fuelMass: 100f);
            var fuel = e.GetPart<FuelPart>();
            fuel.HeatOutput = 2.0f;
            fuel.BurnRate = 1.0f;

            var consume = GameEvent.New("ConsumeFuel");
            consume.SetParameter("Intensity", (object)1.0f);
            e.FireEvent(consume);

            float heatProduced = consume.GetParameter<object>("HeatProduced") is float h ? h : 0f;
            consume.Release();

            Assert.AreEqual(2.0f, heatProduced, 0.01f,
                "HeatProduced should be BurnRate * Intensity * HeatOutput");
        }

        // ========================
        // Ignition
        // ========================

        [Test]
        public void Ignition_CrossingFlameTemperature_AppliesBurning()
        {
            var e = CreateEntity(flameTemp: 300f, combustibility: 0.7f);
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 290f;

            // Apply enough direct heat to cross 300
            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)20f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.IsTrue(e.HasEffect<BurningEffect>(),
                "Crossing flame temperature should apply BurningEffect");
        }

        [Test]
        public void Ignition_ZeroCombustibility_PreventsIgnition()
        {
            var e = CreateEntity(flameTemp: 300f, combustibility: 0f);
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 290f;

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)20f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.IsFalse(e.HasEffect<BurningEffect>(),
                "Zero combustibility should prevent ignition even above flame temperature");
        }

        [Test]
        public void Ignition_WetEffect_SuppressesIgnition()
        {
            var e = CreateEntity(flameTemp: 300f, combustibility: 0.7f);
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 290f;
            e.ApplyEffect(new WetEffect(moisture: 0.5f));

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)20f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.IsFalse(e.HasEffect<BurningEffect>(),
                "WetEffect with moisture > 0.35 should suppress ignition");
        }

        [Test]
        public void Ignition_WetEffect_LowMoisture_AllowsIgnition()
        {
            var e = CreateEntity(flameTemp: 300f, combustibility: 0.7f);
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 290f;
            e.ApplyEffect(new WetEffect(moisture: 0.2f));

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)20f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.IsTrue(e.HasEffect<BurningEffect>(),
                "WetEffect with moisture <= 0.35 should allow ignition");
        }

        // ========================
        // BurningEffect
        // ========================

        [Test]
        public void BurningEffect_ConsumesFuelPerTurn()
        {
            var e = CreateEntity(fuelMass: 100f);
            e.ApplyEffect(new BurningEffect(intensity: 1.0f, rng: new Random(42)));
            var fuel = e.GetPart<FuelPart>();
            float fuelBefore = fuel.FuelMass;

            e.FireEvent(GameEvent.New("TakeTurn"));

            Assert.Less(fuel.FuelMass, fuelBefore, "Burning should consume fuel each turn");
        }

        [Test]
        public void BurningEffect_RemovesSelfOnExhaustion()
        {
            var e = CreateEntity(fuelMass: 0.5f);
            var fuel = e.GetPart<FuelPart>();
            fuel.BurnRate = 10f; // will exhaust immediately

            e.ApplyEffect(new BurningEffect(intensity: 1.0f, rng: new Random(42)));

            // TakeTurn triggers OnTurnStart which consumes fuel
            e.FireEvent(GameEvent.New("TakeTurn"));
            // EndTurn cleans up effects with Duration=0
            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.IsFalse(e.HasEffect<BurningEffect>(),
                "Burning should be removed when fuel is exhausted");
            Assert.IsTrue(e.HasEffect<SmolderingEffect>(),
                "SmolderingEffect should be applied after fuel exhaustion");
            Assert.IsTrue(e.HasEffect<CharredEffect>(),
                "CharredEffect should be applied after fuel exhaustion");
        }

        [Test]
        public void BurningEffect_NoFuelPart_UsesFallbackDuration()
        {
            var e = CreateEntity(fuelMass: 0f); // no FuelPart
            e.RemovePart(e.GetPart<FuelPart>());

            var burn = new BurningEffect(intensity: 2.0f, rng: new Random(42));
            e.ApplyEffect(burn);

            // Duration should be ceil(2.0 * 3) = 6
            Assert.AreEqual(6, burn.Duration,
                "Without FuelPart, duration should be ceil(Intensity * 3)");
        }

        [Test]
        public void BurningEffect_Stacking_IncreasesIntensity()
        {
            var e = CreateEntity();
            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            e.ApplyEffect(burn);

            e.ApplyEffect(new BurningEffect(intensity: 2.0f, rng: new Random(42)));

            // Intensity should be 1.0 + 2.0 * 0.5 = 2.0
            Assert.AreEqual(2.0f, burn.Intensity, 0.01f,
                "Stacking should increase intensity by incoming * 0.5");
        }

        [Test]
        public void BurningEffect_Stacking_CappedAt5()
        {
            var e = CreateEntity();
            var burn = new BurningEffect(intensity: 4.5f, rng: new Random(42));
            e.ApplyEffect(burn);

            e.ApplyEffect(new BurningEffect(intensity: 4.0f, rng: new Random(42)));

            Assert.AreEqual(5.0f, burn.Intensity, 0.01f,
                "Intensity should be capped at 5.0");
        }

        [Test]
        public void BurningEffect_DealsDamage()
        {
            var e = CreateCreature(hp: 100);
            e.ApplyEffect(new BurningEffect(intensity: 1.0f, rng: new Random(42)));

            int hpBefore = e.GetStatValue("Hitpoints");
            e.FireEvent(GameEvent.New("TakeTurn"));
            int hpAfter = e.GetStatValue("Hitpoints");

            Assert.Less(hpAfter, hpBefore, "Burning should deal damage");
        }

        // ========================
        // Heat Propagation
        // ========================

        [Test]
        public void HeatPropagation_BurningEntity_HeatsNeighbors()
        {
            var zone = new Zone("TestZone");

            // Place a burning barrel at center
            var barrel = CreateEntity();
            zone.AddEntity(barrel, 40, 12);
            barrel.ApplyEffect(new BurningEffect(intensity: 2.0f, rng: new Random(42)));

            // Place a neighbor one cell east
            var neighbor = CreateEntity();
            zone.AddEntity(neighbor, 41, 12);
            var neighborThermal = neighbor.GetPart<ThermalPart>();
            float tempBefore = neighborThermal.Temperature;

            // Emit heat (what BurningEffect does during OnTurnStart)
            MaterialSimSystem.EmitHeatToAdjacent(barrel, zone, 60f);

            Assert.Greater(neighborThermal.Temperature, tempBefore,
                "Adjacent entity should be heated by burning neighbor");
        }

        [Test]
        public void HeatPropagation_DoesNotHeatSelf()
        {
            var zone = new Zone("TestZone");

            var barrel = CreateEntity();
            zone.AddEntity(barrel, 40, 12);
            var barrelThermal = barrel.GetPart<ThermalPart>();
            float tempBefore = barrelThermal.Temperature;

            MaterialSimSystem.EmitHeatToAdjacent(barrel, zone, 60f);

            Assert.AreEqual(tempBefore, barrelThermal.Temperature, 0.01f,
                "EmitHeatToAdjacent should not heat the source entity");
        }

        [Test]
        public void ChainIgnition_TorchToBarrel_OverSeveralTurns()
        {
            var zone = new Zone("TestZone");

            // Burning torch at (40,12)
            var torch = CreateEntity(fuelMass: 50f, flameTemp: 300f);
            zone.AddEntity(torch, 40, 12);
            torch.GetPart<ThermalPart>().Temperature = 450f;
            torch.ApplyEffect(new BurningEffect(intensity: 2.0f, rng: new Random(42)));

            // Cold barrel at (41,12)
            var barrel = CreateEntity(fuelMass: 150f, flameTemp: 350f);
            zone.AddEntity(barrel, 41, 12);
            barrel.GetPart<ThermalPart>().Temperature = 25f;

            // Simulate several turns of heat propagation
            bool barrelIgnited = false;
            for (int turn = 0; turn < 50; turn++)
            {
                MaterialSimSystem.EmitHeatToAdjacent(torch, zone, 60f);
                if (barrel.HasEffect<BurningEffect>())
                {
                    barrelIgnited = true;
                    break;
                }
            }

            Assert.IsTrue(barrelIgnited,
                "Barrel should eventually ignite from nearby burning torch");
        }

        // ========================
        // WetEffect
        // ========================

        [Test]
        public void WetEffect_EvaporatesWhenHot()
        {
            var e = CreateEntity();
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 200f;

            var wet = new WetEffect(moisture: 0.5f);
            e.ApplyEffect(wet);
            float moistureBefore = wet.Moisture;

            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.Less(wet.Moisture, moistureBefore,
                "Moisture should decrease when entity is hot");
        }

        [Test]
        public void WetEffect_RemovedAtZeroMoisture()
        {
            var e = CreateEntity();
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 500f; // very hot, fast evaporation

            var wet = new WetEffect(moisture: 0.1f);
            e.ApplyEffect(wet);

            // Tick until removed
            for (int i = 0; i < 20; i++)
            {
                e.FireEvent(GameEvent.New("EndTurn"));
                if (!e.HasEffect<WetEffect>())
                    break;
            }

            Assert.IsFalse(e.HasEffect<WetEffect>(),
                "WetEffect should be removed when moisture reaches 0");
        }

        [Test]
        public void WetEffect_Stacking_AddsMoisture()
        {
            var e = CreateEntity();
            var wet = new WetEffect(moisture: 0.4f);
            e.ApplyEffect(wet);

            e.ApplyEffect(new WetEffect(moisture: 0.3f));

            Assert.AreEqual(0.7f, wet.Moisture, 0.01f,
                "Stacking wet effects should add moisture");
        }

        [Test]
        public void WetEffect_Stacking_CappedAt1()
        {
            var e = CreateEntity();
            var wet = new WetEffect(moisture: 0.8f);
            e.ApplyEffect(wet);

            e.ApplyEffect(new WetEffect(moisture: 0.5f));

            Assert.AreEqual(1.0f, wet.Moisture, 0.01f,
                "Moisture should be capped at 1.0");
        }

        // ========================
        // CharredEffect
        // ========================

        [Test]
        public void CharredEffect_ReducesCombustibility()
        {
            var e = CreateEntity(combustibility: 1.0f);
            e.ApplyEffect(new CharredEffect());

            var material = e.GetPart<MaterialPart>();
            Assert.AreEqual(0.3f, material.Combustibility, 0.01f,
                "CharredEffect should reduce combustibility by 70%");
        }

        [Test]
        public void CharredEffect_Permanent()
        {
            var e = CreateEntity();
            var charred = new CharredEffect();
            e.ApplyEffect(charred);

            Assert.AreEqual(Effect.DURATION_INDEFINITE, charred.Duration,
                "CharredEffect should be permanent (indefinite duration)");
        }

        [Test]
        public void CharredEffect_RestoresOnRemove()
        {
            var e = CreateEntity(combustibility: 0.8f);
            e.ApplyEffect(new CharredEffect());

            var material = e.GetPart<MaterialPart>();
            Assert.AreEqual(0.24f, material.Combustibility, 0.01f);

            e.RemoveEffect<CharredEffect>();
            Assert.AreEqual(0.8f, material.Combustibility, 0.01f,
                "Combustibility should be restored after CharredEffect removal");
        }

        // ========================
        // Material Reaction (data-driven)
        // ========================

        [Test]
        public void Reaction_FirePlusOrganic_IncreasesIntensity()
        {
            // Initialize resolver with test reaction
            string json = @"{
                ""Reactions"": [{
                    ""ID"": ""fire_plus_organic"",
                    ""Priority"": 40,
                    ""Conditions"": {
                        ""SourceState"": ""Burning"",
                        ""TargetMaterialTag"": ""Organic"",
                        ""MinTemperature"": 180,
                        ""MaxMoisture"": 0.35
                    },
                    ""Effects"": [
                        { ""Type"": ""ModifyBurnIntensity"", ""FloatValue"": 0.5, ""StringValue"": """" }
                    ]
                }]
            }";
            MaterialReactionResolver.Initialize(json);

            var e = CreateEntity(tags: "Organic,Flammable");
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 200f;

            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            float intensityBefore = burn.Intensity;

            MaterialReactionResolver.EvaluateReactions(e, null, burn);

            Assert.Greater(burn.Intensity, intensityBefore,
                "fire+organic reaction should increase burn intensity");
        }

        [Test]
        public void Reaction_WetEntity_DoesNotMatch()
        {
            string json = @"{
                ""Reactions"": [{
                    ""ID"": ""fire_plus_organic"",
                    ""Priority"": 40,
                    ""Conditions"": {
                        ""SourceState"": ""Burning"",
                        ""TargetMaterialTag"": ""Organic"",
                        ""MinTemperature"": 180,
                        ""MaxMoisture"": 0.35
                    },
                    ""Effects"": [
                        { ""Type"": ""ModifyBurnIntensity"", ""FloatValue"": 0.5, ""StringValue"": """" }
                    ]
                }]
            }";
            MaterialReactionResolver.Initialize(json);

            var e = CreateEntity(tags: "Organic,Flammable");
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 200f;
            e.ApplyEffect(new WetEffect(moisture: 0.5f));

            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            float intensityBefore = burn.Intensity;

            MaterialReactionResolver.EvaluateReactions(e, null, burn);

            Assert.AreEqual(intensityBefore, burn.Intensity, 0.01f,
                "Wet entity should not match fire+organic reaction");
        }

        // ========================
        // IAuraProvider
        // ========================

        [Test]
        public void BurningEffect_ImplementsIAuraProvider()
        {
            var burn = new BurningEffect(intensity: 1.0f);
            Assert.IsTrue(burn is IAuraProvider);
            Assert.AreEqual(AsciiFxTheme.Fire, ((IAuraProvider)burn).GetAuraTheme());
        }

        [Test]
        public void PoisonedEffect_ImplementsIAuraProvider()
        {
            var poison = new PoisonedEffect();
            Assert.IsTrue(poison is IAuraProvider);
            Assert.AreEqual(AsciiFxTheme.Poison, ((IAuraProvider)poison).GetAuraTheme());
        }
    }
}
