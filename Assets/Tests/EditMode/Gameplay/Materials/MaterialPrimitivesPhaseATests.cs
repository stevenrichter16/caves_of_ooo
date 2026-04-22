using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase A coverage: new frost / electricity / acid / vapor primitives
    /// and the MaterialPart field consumers they wire up.
    /// </summary>
    public class MaterialPrimitivesPhaseATests
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
            float volatility = 0f,
            float porosity = 0f,
            float brittleness = 0f,
            float conductivity = 0f,
            float brittleTemp = -100f,
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
                MaterialTagsRaw = tags,
                Volatility = volatility,
                Porosity = porosity,
                Brittleness = brittleness,
                Conductivity = conductivity
            });
            e.AddPart(new ThermalPart
            {
                FlameTemperature = flameTemp,
                HeatCapacity = heatCapacity,
                BrittleTemperature = brittleTemp
            });
            return e;
        }

        private Entity CreateCreature(int hp = 100)
        {
            var e = new Entity();
            e.BlueprintName = "TestCreature";
            e.Statistics["Hitpoints"] = new Stat { BaseValue = hp, Max = hp, Owner = e };
            e.Statistics["HP"] = new Stat { BaseValue = hp, Max = hp, Owner = e };
            e.SetTag("Creature");
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
        // FrozenEffect
        // ========================

        [Test]
        public void FrozenEffect_DeepCold_BlocksAction()
        {
            var e = CreateCreature();
            var frozen = new FrozenEffect(cold: 1.0f);
            e.ApplyEffect(frozen);

            Assert.IsFalse(frozen.AllowAction(e),
                "Cold > 0.5 should block action like stun.");
        }

        [Test]
        public void FrozenEffect_ShallowCold_AllowsAction()
        {
            var e = CreateCreature();
            var frozen = new FrozenEffect(cold: 0.3f);
            e.ApplyEffect(frozen);

            Assert.IsTrue(frozen.AllowAction(e),
                "Cold <= 0.5 should allow action.");
        }

        [Test]
        public void FrozenEffect_OnApply_ExtinguishesBurningTarget()
        {
            var e = CreateEntity();
            e.ApplyEffect(new BurningEffect(intensity: 1.0f, rng: new Random(42)));
            Assert.IsTrue(e.HasEffect<BurningEffect>());

            e.ApplyEffect(new FrozenEffect(cold: 1.0f));

            Assert.IsFalse(e.HasEffect<BurningEffect>(),
                "Cold applied to a burning target should extinguish it.");
        }

        [Test]
        public void FrozenEffect_ThawsAboveFreezing()
        {
            var e = CreateEntity();
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 20f; // well above freeze
            var frozen = new FrozenEffect(cold: 1.0f);
            e.ApplyEffect(frozen);
            float coldBefore = frozen.Cold;

            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.Less(frozen.Cold, coldBefore,
                "Frozen should thaw when the owner's temperature is above freezing.");
        }

        [Test]
        public void FrozenEffect_RemovedAtZeroCold()
        {
            var e = CreateEntity();
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 200f; // very warm, fast thaw
            var frozen = new FrozenEffect(cold: 0.02f);
            e.ApplyEffect(frozen);

            for (int i = 0; i < 20; i++)
            {
                e.FireEvent(GameEvent.New("EndTurn"));
                if (!e.HasEffect<FrozenEffect>())
                    break;
            }

            Assert.IsFalse(e.HasEffect<FrozenEffect>(),
                "FrozenEffect should be removed when Cold reaches zero.");
        }

        // ========================
        // ElectrifiedEffect
        // ========================

        [Test]
        public void ElectrifiedEffect_DefaultDuration_IsShort()
        {
            var zap = new ElectrifiedEffect(charge: 1.0f);
            Assert.AreEqual(2, zap.Duration);
        }

        [Test]
        public void ElectrifiedEffect_WetTarget_AmplifiesCharge()
        {
            var e = CreateEntity();
            e.ApplyEffect(new WetEffect(moisture: 0.5f));

            var zap = new ElectrifiedEffect(charge: 1.0f);
            e.ApplyEffect(zap);

            Assert.AreEqual(2.0f, zap.Charge, 0.01f,
                "Wet targets should have their charge doubled.");
            Assert.AreEqual(3, zap.Duration,
                "Wet targets should extend duration by 1 turn.");
        }

        [Test]
        public void ElectrifiedEffect_StunsCreatureOnApply()
        {
            var e = CreateCreature();
            e.ApplyEffect(new ElectrifiedEffect(charge: 1.0f));

            Assert.IsTrue(e.HasEffect<StunnedEffect>(),
                "Creature should be stunned by electricity on apply.");
        }

        [Test]
        public void ElectrifiedEffect_NonCreature_NoStun()
        {
            var e = CreateEntity();
            e.ApplyEffect(new ElectrifiedEffect(charge: 1.0f));

            Assert.IsFalse(e.HasEffect<StunnedEffect>(),
                "Non-creature entities should not be stunned by electricity.");
        }

        // ========================
        // AcidicEffect
        // ========================

        [Test]
        public void AcidicEffect_DealsDamageToOrganic()
        {
            var e = CreateCreature();
            e.ApplyEffect(new AcidicEffect(corrosion: 1.0f));
            int hpBefore = e.GetStatValue("Hitpoints");

            e.FireEvent(GameEvent.New("TakeTurn"));

            Assert.Less(e.GetStatValue("Hitpoints"), hpBefore,
                "Acid should damage organic creatures each turn.");
        }

        [Test]
        public void AcidicEffect_DegradesCombustibility()
        {
            var e = CreateEntity(combustibility: 0.7f, tags: "Organic");
            var material = e.GetPart<MaterialPart>();
            float combBefore = material.Combustibility;

            e.ApplyEffect(new AcidicEffect(corrosion: 1.0f));
            e.FireEvent(GameEvent.New("TakeTurn"));

            Assert.Less(material.Combustibility, combBefore,
                "Acid should degrade organic material's combustibility.");
        }

        [Test]
        public void AcidicEffect_SkipsNonOrganic()
        {
            var e = CreateEntity(tags: "Metal");
            var material = e.GetPart<MaterialPart>();
            float combBefore = material.Combustibility;

            e.ApplyEffect(new AcidicEffect(corrosion: 1.0f));
            e.FireEvent(GameEvent.New("TakeTurn"));

            Assert.AreEqual(combBefore, material.Combustibility, 0.01f,
                "Acid should not degrade non-organic materials.");
        }

        [Test]
        public void AcidicEffect_DecaysOverTime()
        {
            var e = CreateEntity(tags: "Organic");
            var acid = new AcidicEffect(corrosion: 0.5f);
            e.ApplyEffect(acid);
            float before = acid.Corrosion;

            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.Less(acid.Corrosion, before,
                "Corrosion should tick down each turn end.");
        }

        // ========================
        // SteamEffect
        // ========================

        [Test]
        public void SteamEffect_DissipatesOverTime()
        {
            var e = CreateEntity();
            var steam = new SteamEffect(density: 1.0f);
            e.ApplyEffect(steam);
            float before = steam.Density;

            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.Less(steam.Density, before,
                "Steam density should decay each turn end.");
        }

        [Test]
        public void SteamEffect_RemovedWhenDensityZero()
        {
            var e = CreateEntity();
            var steam = new SteamEffect(density: 0.05f);
            e.ApplyEffect(steam);

            for (int i = 0; i < 5; i++)
                e.FireEvent(GameEvent.New("EndTurn"));

            Assert.IsFalse(e.HasEffect<SteamEffect>(),
                "SteamEffect should be removed once density reaches zero.");
        }

        // ========================
        // MaterialPart.Volatility → ThermalPart.TryIgnite
        // ========================

        [Test]
        public void Volatility_LowersEffectiveFlameTemperature()
        {
            // Oil-like target: flame temp 400 baseline, volatility 0.5 → effective 350.
            var e = CreateEntity(flameTemp: 400f, volatility: 0.5f);
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 340f;

            // Small heat pulse that would not cross baseline flame temp but does cross effective.
            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)15f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.IsTrue(e.HasEffect<BurningEffect>(),
                "Volatile material should ignite below the nominal flame temperature.");
        }

        [Test]
        public void Volatility_IgnoredWhenZero()
        {
            var e = CreateEntity(flameTemp: 400f, volatility: 0f);
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 340f;

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)15f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.IsFalse(e.HasEffect<BurningEffect>(),
                "Zero-volatility material should not ignite below nominal flame temp.");
        }

        [Test]
        public void Volatility_BumpsInitialBurnIntensity()
        {
            // Effective flame temp for volatility 0.5 is 400 - 50 = 350.
            var e = CreateEntity(flameTemp: 400f, volatility: 0.5f);
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 340f;

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)20f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            var burn = e.GetEffect<BurningEffect>();
            Assert.IsNotNull(burn);
            Assert.Greater(burn.Intensity, 1.0f,
                "Volatile material should start burning hotter than baseline 1.0.");
        }

        // ========================
        // MaterialPart.Porosity → WetEffect
        // ========================

        [Test]
        public void Porosity_ScalesMoistureAbsorptionOnApply()
        {
            var e = CreateEntity(porosity: 0.6f);
            var wet = new WetEffect(moisture: 0.5f);
            e.ApplyEffect(wet);

            Assert.Greater(wet.Moisture, 0.5f,
                "Porous material should absorb more moisture than raw input.");
            Assert.LessOrEqual(wet.Moisture, 1.0f,
                "Moisture absorption should still cap at 1.0.");
        }

        [Test]
        public void Porosity_SlowsEvaporation()
        {
            var dry = CreateEntity(porosity: 0f);
            var porous = CreateEntity(porosity: 1.0f);

            // Start both at matched moisture at neutral temperature (slow evaporation path).
            var dryWet = new WetEffect(moisture: 0.5f);
            var porousWet = new WetEffect(moisture: 0.5f);
            dry.ApplyEffect(dryWet);
            porous.ApplyEffect(porousWet);
            // Porosity may have bumped porous moisture up — re-floor for apples-to-apples.
            porousWet.Moisture = 0.5f;

            dry.FireEvent(GameEvent.New("EndTurn"));
            porous.FireEvent(GameEvent.New("EndTurn"));

            Assert.Greater(porousWet.Moisture, dryWet.Moisture,
                "Porous material should retain more moisture than dry material after one tick.");
        }

        // ========================
        // ThermalPart crossings
        // ========================

        [Test]
        public void ThermalPart_FreezeCrossing_AppliesFrozenEffect()
        {
            var e = CreateEntity();
            var thermal = e.GetPart<ThermalPart>();
            thermal.FreezeTemperature = 0f;
            thermal.Temperature = 20f;

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)(-50f));
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.IsTrue(e.HasEffect<FrozenEffect>(),
                "Crossing FreezeTemperature downward should apply FrozenEffect.");
        }

        [Test]
        public void ThermalPart_VaporCrossing_RemovesWetEffect()
        {
            var e = CreateEntity(heatCapacity: 1.0f);
            var thermal = e.GetPart<ThermalPart>();
            thermal.VaporTemperature = 500f;
            thermal.Temperature = 400f;
            e.ApplyEffect(new WetEffect(moisture: 0.5f));

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)200f); // pushes temp to 600, past 500 vapor
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.IsFalse(e.HasEffect<WetEffect>(),
                "Crossing VaporTemperature should boil off WetEffect.");
        }

        [Test]
        public void ThermalPart_ThermalShock_FiresTryShatterOnBrittle()
        {
            var e = CreateEntity(brittleness: 0.9f, hp: 100);
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 25f;
            int hpBefore = e.GetStatValue("Hitpoints");

            // Direct hit of 500 joules with heat capacity 1.0 → delta = 500 > 200 shock threshold.
            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)500f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.Less(e.GetStatValue("Hitpoints"), hpBefore,
                "Brittle material should take damage from thermal shock.");
        }

        [Test]
        public void ThermalPart_ThermalShock_IgnoresNonBrittle()
        {
            var e = CreateEntity(brittleness: 0f, hp: 100);
            var thermal = e.GetPart<ThermalPart>();
            thermal.Temperature = 25f;
            int hpBefore = e.GetStatValue("Hitpoints");

            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)500f);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();

            Assert.AreEqual(hpBefore, e.GetStatValue("Hitpoints"),
                "Non-brittle material should ignore thermal shock.");
        }

        // ========================
        // MaterialPart.TryShatter
        // ========================

        [Test]
        public void TryShatter_HighBrittleness_DestroysEntity()
        {
            var e = CreateEntity(brittleness: 1.0f, hp: 50);

            var shatter = GameEvent.New("TryShatter");
            e.FireEvent(shatter);
            shatter.Release();

            Assert.LessOrEqual(e.GetStatValue("Hitpoints"), 0,
                "Brittleness 1.0 should reduce the entity's HP to zero.");
        }

        [Test]
        public void TryShatter_LowBrittleness_Cancelled()
        {
            var e = CreateEntity(brittleness: 0.2f, hp: 50);

            var shatter = GameEvent.New("TryShatter");
            e.FireEvent(shatter);
            bool cancelled = shatter.GetParameter<bool>("Cancelled");
            shatter.Release();

            Assert.IsTrue(cancelled,
                "Low brittleness should cancel the shatter attempt.");
            Assert.AreEqual(50, e.GetStatValue("Hitpoints"),
                "HP should be unchanged when shatter is cancelled.");
        }

        // ========================
        // MaterialPart.TryChainElectricity → Conductivity consumer
        // ========================

        [Test]
        public void TryChainElectricity_ConductorPropagatesToNeighbor()
        {
            var zone = new Zone("TestZone");

            var source = CreateEntity(tags: "Metal", conductivity: 0.9f);
            var neighbor = CreateEntity(tags: "Metal", conductivity: 0.9f);
            zone.AddEntity(source, 40, 12);
            zone.AddEntity(neighbor, 41, 12);

            source.ApplyEffect(new ElectrifiedEffect(charge: 1.0f), null, zone);

            var chain = GameEvent.New("TryChainElectricity");
            chain.SetParameter("Zone", (object)zone);
            chain.SetParameter("Source", (object)source);
            chain.SetParameter("Charge", (object)1.0f);
            source.FireEvent(chain);
            chain.Release();

            Assert.IsTrue(neighbor.HasEffect<ElectrifiedEffect>(),
                "Conductive neighbor should receive ElectrifiedEffect via chain propagation.");
        }

        [Test]
        public void TryChainElectricity_NonConductorDoesNotChain()
        {
            var zone = new Zone("TestZone");

            // Source is metal, neighbor is plain wood — no chain.
            var source = CreateEntity(tags: "Metal", conductivity: 0.9f);
            var neighbor = CreateEntity(tags: "Organic", conductivity: 0f);
            zone.AddEntity(source, 40, 12);
            zone.AddEntity(neighbor, 41, 12);

            var chain = GameEvent.New("TryChainElectricity");
            chain.SetParameter("Zone", (object)zone);
            chain.SetParameter("Source", (object)source);
            chain.SetParameter("Charge", (object)1.0f);
            source.FireEvent(chain);
            chain.Release();

            Assert.IsFalse(neighbor.HasEffect<ElectrifiedEffect>(),
                "Non-conductive neighbor should not pick up electricity.");
        }
    }
}
