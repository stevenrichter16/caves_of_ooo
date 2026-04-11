using System;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class GrimoireSpellTests
    {
        [SetUp]
        public void Setup()
        {
            AsciiFxBus.Clear();
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            MaterialReactionResolver.Factory = null;
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
        // Helpers
        // ========================

        private static Entity CreateCaster(int hp = 50)
        {
            var entity = new Entity { BlueprintName = "TestCaster" };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { BaseValue = hp, Max = hp, Owner = entity };
            entity.Statistics["HP"] = new Stat { BaseValue = hp, Max = hp, Owner = entity };
            entity.Statistics["Level"] = new Stat { BaseValue = 1, Max = 50, Owner = entity };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Willpower"] = new Stat { Name = "Willpower", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Intelligence"] = new Stat { Name = "Intelligence", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "caster" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ActivatedAbilitiesPart());
            entity.AddPart(new MutationsPart());
            entity.AddPart(new ThermalPart());
            return entity;
        }

        private static Entity CreateCreature(string name = "target", int hp = 20)
        {
            var entity = new Entity { BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { BaseValue = hp, Max = hp, Owner = entity };
            entity.Statistics["HP"] = new Stat { BaseValue = hp, Max = hp, Owner = entity };
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ThermalPart
            {
                FlameTemperature = 500f,
                HeatCapacity = 2.0f
            });
            entity.AddPart(new MaterialPart
            {
                MaterialID = "Flesh",
                Combustibility = 0.15f,
                MaterialTagsRaw = "Organic"
            });
            return entity;
        }

        private static Entity CreateCombustibleObject(
            string name = "barrel",
            float flameTemp = 300f,
            float heatCapacity = 0.6f,
            float combustibility = 0.8f)
        {
            var entity = new Entity { BlueprintName = "WoodenBarrel" };
            entity.Statistics["Hitpoints"] = new Stat { BaseValue = 30, Max = 30, Owner = entity };
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new ThermalPart
            {
                FlameTemperature = flameTemp,
                HeatCapacity = heatCapacity
            });
            entity.AddPart(new MaterialPart
            {
                MaterialID = "Wood",
                Combustibility = combustibility,
                MaterialTagsRaw = "Organic,Flammable"
            });
            entity.AddPart(new FuelPart
            {
                FuelMass = 80f,
                MaxFuel = 80f,
                BurnRate = 1.0f,
                HeatOutput = 1.0f
            });
            return entity;
        }

        private static Entity CreateGrimoireItem(string mutationClassName, int mutationLevel = 1)
        {
            var grimoire = new Entity { BlueprintName = "TestGrimoire" };
            grimoire.AddPart(new RenderPart { DisplayName = "test grimoire" });
            grimoire.AddPart(new GrimoirePart
            {
                MutationClassName = mutationClassName,
                MutationLevel = mutationLevel,
                LearnMessage = "You learn a new spell.",
                AlreadyKnownMessage = "You already know this spell."
            });
            return grimoire;
        }

        private static void FireReadGrimoire(Entity grimoire, Entity actor)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "ReadGrimoire");
            e.SetParameter("Actor", (object)actor);
            grimoire.FireEvent(e);
            e.Release();
        }

        // ========================
        // GrimoirePart Spell Granting
        // ========================

        [Test]
        public void GrimoirePart_WithMutation_GrantsSpell()
        {
            var caster = CreateCaster();
            var grimoire = CreateGrimoireItem("KindleMutation");

            FireReadGrimoire(grimoire, caster);

            var mutations = caster.GetPart<MutationsPart>();
            Assert.IsTrue(mutations.HasMutation("KindleMutation"),
                "Reading grimoire should grant the Kindle mutation");

            var abilities = caster.GetPart<ActivatedAbilitiesPart>();
            Assert.AreEqual(1, abilities.AbilityList.Count,
                "Kindle mutation should register an activated ability");
            Assert.AreEqual("Kindle", abilities.AbilityList[0].DisplayName);
        }

        [Test]
        public void GrimoirePart_WithMutation_AlreadyKnown()
        {
            var caster = CreateCaster();
            var grimoire = CreateGrimoireItem("KindleMutation");

            // Learn it once
            FireReadGrimoire(grimoire, caster);
            Assert.IsTrue(caster.GetPart<MutationsPart>().HasMutation("KindleMutation"));

            // Try to learn again
            MessageLog.Clear();
            FireReadGrimoire(grimoire, caster);

            // Should still have exactly 1 ability (not duplicated)
            Assert.AreEqual(1, caster.GetPart<ActivatedAbilitiesPart>().AbilityList.Count);
        }

        [Test]
        public void GrimoirePart_WithMutation_NoMutationsPart_GracefulFailure()
        {
            var actor = new Entity { BlueprintName = "NPC" };
            actor.AddPart(new RenderPart { DisplayName = "npc" });
            // No MutationsPart — this entity can't learn spells

            var grimoire = CreateGrimoireItem("KindleMutation");
            FireReadGrimoire(grimoire, actor);

            // Should not crash, and should not have any mutation
            Assert.IsFalse(actor.HasPart<MutationsPart>());
        }

        [Test]
        public void GrimoirePart_ExistingKnowledgeProperty_StillWorks()
        {
            var actor = new Entity { BlueprintName = "Player" };
            actor.AddPart(new RenderPart { DisplayName = "player" });

            var grimoire = new Entity { BlueprintName = "OldGrimoire" };
            grimoire.AddPart(new RenderPart { DisplayName = "old grimoire" });
            grimoire.AddPart(new GrimoirePart
            {
                KnowledgeProperty = "KnowsPurifyWater",
                LearnMessage = "You learn the rite."
            });

            FireReadGrimoire(grimoire, actor);

            Assert.IsTrue(actor.Properties.ContainsKey("KnowsPurifyWater"),
                "Knowledge-only grimoire should still work via KnowledgeProperty path");
        }

        // ========================
        // Kindle Spell
        // ========================

        [Test]
        public void Kindle_AppliesHeat_ToTarget()
        {
            var zone = new Zone("KindleZone");
            var caster = CreateCaster();
            var target = CreateCombustibleObject("barrel", flameTemp: 500f);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new KindleMutation(), 1);
            var kindle = mutations.GetMutation<KindleMutation>();

            var thermal = target.GetPart<ThermalPart>();
            float tempBefore = thermal.Temperature;

            kindle.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.Greater(thermal.Temperature, tempBefore,
                "Kindle should increase target temperature via ApplyHeat");
        }

        [Test]
        public void Kindle_CanIgnite_CombustibleEntity()
        {
            var zone = new Zone("KindleZone");
            var caster = CreateCaster();
            // Low flame temp + low heat capacity = easy to ignite with 200J
            var target = CreateCombustibleObject("kindling", flameTemp: 200f, heatCapacity: 0.5f);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new KindleMutation(), 1);
            var kindle = mutations.GetMutation<KindleMutation>();

            kindle.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(target.HasEffect<BurningEffect>(),
                "Kindle heat should trigger ignition on low-FlameTemp combustible entity");
        }

        [Test]
        public void Kindle_EmitsAdjacentHeat()
        {
            var zone = new Zone("KindleZone");
            var caster = CreateCaster();
            var target = CreateCombustibleObject("barrel", flameTemp: 9999f); // Won't ignite
            var neighbor = CreateCombustibleObject("neighbor", flameTemp: 9999f);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);
            zone.AddEntity(neighbor, 8, 5); // Adjacent to target

            var thermalNeighbor = neighbor.GetPart<ThermalPart>();
            float tempBefore = thermalNeighbor.Temperature;

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new KindleMutation(), 1);
            var kindle = mutations.GetMutation<KindleMutation>();

            kindle.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.Greater(thermalNeighbor.Temperature, tempBefore,
                "Kindle should emit radiant heat to entities adjacent to the target");
        }

        // ========================
        // Quench Spell
        // ========================

        [Test]
        public void Quench_AppliesWetEffect()
        {
            var zone = new Zone("QuenchZone");
            var caster = CreateCaster();
            var target = CreateCreature("snapjaw", 50);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new QuenchMutation(), 1);
            var quench = mutations.GetMutation<QuenchMutation>();

            quench.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(target.HasEffect<WetEffect>(),
                "Quench should apply WetEffect to target");
            var wet = target.GetEffect<WetEffect>();
            Assert.AreEqual(0.8f, wet.Moisture, 0.01f);
        }

        [Test]
        public void Quench_CoolsTarget()
        {
            var zone = new Zone("QuenchZone");
            var caster = CreateCaster();
            var target = CreateCombustibleObject("barrel", flameTemp: 9999f);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            // Heat the target first
            var thermal = target.GetPart<ThermalPart>();
            thermal.Temperature = 300f;

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new QuenchMutation(), 1);
            var quench = mutations.GetMutation<QuenchMutation>();

            quench.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.Less(thermal.Temperature, 300f,
                "Quench should cool the target via negative Joules");
        }

        [Test]
        public void Quench_ExtinguishesFire()
        {
            var zone = new Zone("QuenchZone");
            var caster = CreateCaster();
            var target = CreateCombustibleObject("barrel", flameTemp: 200f, heatCapacity: 0.5f);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            // Set target on fire
            var thermal = target.GetPart<ThermalPart>();
            thermal.Temperature = 400f;
            target.ApplyEffect(new BurningEffect(intensity: 1.0f));
            Assert.IsTrue(target.HasEffect<BurningEffect>());

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new QuenchMutation(), 1);
            var quench = mutations.GetMutation<QuenchMutation>();

            quench.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            // Cooling drops temperature below FlameTemperature
            Assert.Less(thermal.Temperature, 200f,
                "Quench should cool target below flame temperature");

            // Fire the EndTurn event to trigger ThermalPart's extinguishing check
            target.FireEvent("EndTurn");

            Assert.IsFalse(target.HasEffect<BurningEffect>(),
                "Target should be extinguished after Quench + EndTurn");
        }

        [Test]
        public void Quench_PreventsIgnition()
        {
            var zone = new Zone("QuenchZone");
            var caster = CreateCaster();
            var target = CreateCombustibleObject("barrel", flameTemp: 200f, heatCapacity: 0.5f);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            // First quench the target
            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new QuenchMutation(), 1);
            mutations.AddMutation(new KindleMutation(), 1);
            var quench = mutations.GetMutation<QuenchMutation>();
            var kindle = mutations.GetMutation<KindleMutation>();

            quench.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));
            Assert.IsTrue(target.HasEffect<WetEffect>());

            // Reset temperature to baseline before Kindle attempt
            target.GetPart<ThermalPart>().Temperature = 25f;

            // Now try to kindle it — WetEffect should suppress ignition
            kindle.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsFalse(target.HasEffect<BurningEffect>(),
                "WetEffect (moisture > 0.35) should suppress ignition from Kindle heat");
        }

        // ========================
        // Conflagration Spell
        // ========================

        [Test]
        public void Conflagration_DamagesCreaturesInRadius()
        {
            var zone = new Zone("ConflagrationZone");
            var caster = CreateCaster();
            var near = CreateCreature("snapjaw", 50);
            var far = CreateCreature("goatfolk", 50);

            zone.AddEntity(caster, 10, 10);
            zone.AddEntity(near, 11, 10);   // Distance 1
            zone.AddEntity(far, 12, 10);    // Distance 2

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ConflagrationMutation(), 1);
            var conflag = mutations.GetMutation<ConflagrationMutation>();

            conflag.Cast(zone, zone.GetCell(10, 10), new Random(42));

            Assert.Less(near.GetStatValue("Hitpoints", 50), 50,
                "Creature at range 1 should take damage");
            Assert.Less(far.GetStatValue("Hitpoints", 50), 50,
                "Creature at range 2 should take damage");
        }

        [Test]
        public void Conflagration_AppliesBurningToCreatures()
        {
            var zone = new Zone("ConflagrationZone");
            var caster = CreateCaster();
            var target = CreateCreature("snapjaw", 50);

            zone.AddEntity(caster, 10, 10);
            zone.AddEntity(target, 11, 10);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ConflagrationMutation(), 1);
            var conflag = mutations.GetMutation<ConflagrationMutation>();

            conflag.Cast(zone, zone.GetCell(10, 10), new Random(42));

            Assert.IsTrue(target.HasEffect<BurningEffect>(),
                "Conflagration should apply BurningEffect to creatures in range");
        }

        [Test]
        public void Conflagration_HeatsObjectsInRadius()
        {
            var zone = new Zone("ConflagrationZone");
            var caster = CreateCaster();
            var barrel = CreateCombustibleObject("barrel", flameTemp: 9999f); // Won't ignite

            zone.AddEntity(caster, 10, 10);
            zone.AddEntity(barrel, 11, 10);

            var thermal = barrel.GetPart<ThermalPart>();
            float tempBefore = thermal.Temperature;

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ConflagrationMutation(), 1);
            var conflag = mutations.GetMutation<ConflagrationMutation>();

            conflag.Cast(zone, zone.GetCell(10, 10), new Random(42));

            Assert.Greater(thermal.Temperature, tempBefore,
                "Conflagration should heat non-creature objects with ThermalPart in radius");
        }

        [Test]
        public void Conflagration_ChainIgnition()
        {
            var zone = new Zone("ConflagrationZone");
            var caster = CreateCaster();
            // Low flame temp barrel — 250J direct at HeatCapacity 0.6 → +416°, well above 200
            var barrel = CreateCombustibleObject("barrel", flameTemp: 200f, heatCapacity: 0.6f);

            zone.AddEntity(caster, 10, 10);
            zone.AddEntity(barrel, 11, 10);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ConflagrationMutation(), 1);
            var conflag = mutations.GetMutation<ConflagrationMutation>();

            conflag.Cast(zone, zone.GetCell(10, 10), new Random(42));

            Assert.IsTrue(barrel.HasEffect<BurningEffect>(),
                "Conflagration heat should ignite combustible objects via ThermalPart pipeline");
        }

        [Test]
        public void Conflagration_ExcludesCaster()
        {
            var zone = new Zone("ConflagrationZone");
            var caster = CreateCaster();

            zone.AddEntity(caster, 10, 10);

            int hpBefore = caster.GetStatValue("Hitpoints", 50);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ConflagrationMutation(), 1);
            var conflag = mutations.GetMutation<ConflagrationMutation>();

            conflag.Cast(zone, zone.GetCell(10, 10), new Random(42));

            Assert.AreEqual(hpBefore, caster.GetStatValue("Hitpoints", 50),
                "Caster should not be damaged by own Conflagration");
            Assert.IsFalse(caster.HasEffect<BurningEffect>(),
                "Caster should not receive BurningEffect from own Conflagration");
        }

        // ========================
        // Ice Lance Spell
        // ========================

        [Test]
        public void IceLance_ShattersBrittleFrozenMetal()
        {
            var zone = new Zone("IceLanceZone");
            var caster = CreateCaster();

            var target = new Entity { BlueprintName = "BrittleMetalStatue" };
            target.Statistics["Hitpoints"] = new Stat { BaseValue = 20, Max = 20, Owner = target };
            target.AddPart(new RenderPart { DisplayName = "brittle statue" });
            target.AddPart(new PhysicsPart { Solid = true });
            target.AddPart(new ThermalPart
            {
                Temperature = 25f,
                FreezeTemperature = 0f,
                BrittleTemperature = -100f,
                HeatCapacity = 1.0f
            });
            target.AddPart(new MaterialPart
            {
                MaterialID = "BrittleMetal",
                Brittleness = 0.9f,
                MaterialTagsRaw = "Metal"
            });

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new IceLanceMutation(), 1);
            var iceLance = mutations.GetMutation<IceLanceMutation>();

            iceLance.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(target.HasEffect<FrozenEffect>(),
                "Ice Lance's cooling heat should cross FreezeTemperature and apply FrozenEffect via TryFreeze");
            Assert.AreEqual(0, target.GetStatValue("Hitpoints", -1),
                "Brittleness >= 0.9 catastrophic shatter should drop Hitpoints to 0");
        }

        [Test]
        public void IceLance_ExtinguishesBurningTarget()
        {
            var zone = new Zone("IceLanceZone");
            var caster = CreateCaster();
            var target = CreateCombustibleObject("barrel", flameTemp: 200f, heatCapacity: 0.5f);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            // Pre-ignite the target so FrozenEffect.OnApply has to extinguish it.
            target.GetPart<ThermalPart>().Temperature = 400f;
            target.ApplyEffect(new BurningEffect(intensity: 1.0f));
            Assert.IsTrue(target.HasEffect<BurningEffect>());

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new IceLanceMutation(), 1);
            var iceLance = mutations.GetMutation<IceLanceMutation>();

            iceLance.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsFalse(target.HasEffect<BurningEffect>(),
                "FrozenEffect.OnApply should extinguish any active BurningEffect");
            Assert.IsTrue(target.HasEffect<FrozenEffect>(),
                "Ice Lance should still apply FrozenEffect after crossing FreezeTemperature");
        }

        // ========================
        // Acid Spray Spell
        // ========================

        [Test]
        public void AcidSpray_OrganicTakesDamageAndLosesCombustibility()
        {
            var zone = new Zone("AcidSprayZone");
            var caster = CreateCaster();
            var target = CreateCreature("snapjaw", hp: 50);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            float combustibilityBefore = target.GetPart<MaterialPart>().Combustibility;
            int hpBefore = target.GetStatValue("Hitpoints", 50);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new AcidSprayMutation(), 1);
            var acidSpray = mutations.GetMutation<AcidSprayMutation>();

            acidSpray.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(target.HasEffect<AcidicEffect>(),
                "Acid Spray should apply AcidicEffect to the struck target");

            // Fire the standard passive tick pair. BeginTakeAction drives
            // OnTurnStart (damage + combustibility degrade); EndTurn drives
            // corrosion decay.
            var begin = GameEvent.New("BeginTakeAction");
            begin.SetParameter("Zone", (object)zone);
            target.FireEvent(begin);
            begin.Release();
            target.FireEvent("EndTurn");

            int hpAfter = target.GetStatValue("Hitpoints", 0);
            float combustibilityAfter = target.GetPart<MaterialPart>().Combustibility;

            Assert.Less(hpAfter, hpBefore,
                "Acid tick should damage organic target");
            Assert.Less(combustibilityAfter, combustibilityBefore,
                "Acid tick should degrade organic target's combustibility");
        }

        // ========================
        // Arc Bolt Spell
        // ========================

        [Test]
        public void ArcBolt_WetCreatureDoublesCharge()
        {
            var zone = new Zone("ArcBoltZone");
            var caster = CreateCaster();
            var target = CreateCreature("snapjaw", hp: 50);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            // Pre-soak the target — ElectrifiedEffect.OnApply amplifies Charge
            // (*= 2) and extends Duration on any creature with WetEffect.Moisture > 0.2.
            target.ApplyEffect(new WetEffect(moisture: 0.8f));

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ArcBoltMutation(), 1);
            var arcBolt = mutations.GetMutation<ArcBoltMutation>();

            arcBolt.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            var electrified = target.GetEffect<ElectrifiedEffect>();
            Assert.IsNotNull(electrified,
                "Arc Bolt should apply ElectrifiedEffect to the struck creature");
            Assert.GreaterOrEqual(electrified.Charge, 1.9f,
                "Wet target's Charge should be doubled by ElectrifiedEffect.OnApply (1.0 → 2.0)");
        }

        [Test]
        public void ArcBolt_ChainsToAdjacentConductor_OnEndTurn()
        {
            var zone = new Zone("ArcBoltChainZone");
            var caster = CreateCaster();

            // The struck target must itself be a conductor so MaterialPart.
            // HandleTryChainElectricity (fired by ElectrifiedEffect.OnTurnEnd)
            // finds neighbors and jumps along them. A metal-tagged creature
            // fits: it has Hitpoints for damage, the Creature tag for stun,
            // and Metal/Conductor tags for chain propagation.
            var metalGolem = new Entity { BlueprintName = "MetalGolem" };
            metalGolem.Tags["Creature"] = "";
            metalGolem.Statistics["Hitpoints"] = new Stat { BaseValue = 40, Max = 40, Owner = metalGolem };
            metalGolem.Statistics["HP"] = new Stat { BaseValue = 40, Max = 40, Owner = metalGolem };
            metalGolem.AddPart(new RenderPart { DisplayName = "metal golem" });
            metalGolem.AddPart(new PhysicsPart { Solid = true });
            metalGolem.AddPart(new ThermalPart());
            metalGolem.AddPart(new MaterialPart
            {
                MaterialID = "Iron",
                Conductivity = 0.9f,
                MaterialTagsRaw = "Metal,Conductor"
            });

            // Adjacent conductive prop — the chain target.
            var wire = new Entity { BlueprintName = "CopperWire" };
            wire.AddPart(new RenderPart { DisplayName = "copper wire" });
            wire.AddPart(new ThermalPart());
            wire.AddPart(new MaterialPart
            {
                MaterialID = "Copper",
                Conductivity = 0.9f,
                MaterialTagsRaw = "Metal,Conductor"
            });

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(metalGolem, 6, 5);
            zone.AddEntity(wire, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ArcBoltMutation(), 1);
            var arcBolt = mutations.GetMutation<ArcBoltMutation>();

            arcBolt.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(metalGolem.HasEffect<ElectrifiedEffect>(),
                "Struck target should be electrified immediately on Cast");
            Assert.IsFalse(wire.HasEffect<ElectrifiedEffect>(),
                "Chain propagation is async — wire should NOT be electrified until EndTurn runs");

            // Fire EndTurn on the struck golem with Zone. ElectrifiedEffect.OnTurnEnd
            // fires TryChainElectricity, MaterialPart.HandleTryChainElectricity
            // scans 8 neighbors and jumps to any conductive target.
            var endTurn = GameEvent.New("EndTurn");
            endTurn.SetParameter("Zone", (object)zone);
            metalGolem.FireEvent(endTurn);
            endTurn.Release();

            Assert.IsTrue(wire.HasEffect<ElectrifiedEffect>(),
                "After EndTurn tick on struck conductor, adjacent conductive prop should chain-electrify");
        }

        // ========================
        // Rime Nova Spell
        // ========================

        [Test]
        public void RimeNova_ExtinguishesBurningBarrel()
        {
            var zone = new Zone("RimeNovaZone");
            var caster = CreateCaster();
            var barrel = CreateCombustibleObject("barrel", flameTemp: 300f, heatCapacity: 0.6f);

            zone.AddEntity(caster, 10, 10);
            zone.AddEntity(barrel, 11, 10);

            // Pre-ignite the barrel so the -200J cooling pulse has to cross
            // FlameTemperature downward and extinguish it.
            barrel.GetPart<ThermalPart>().Temperature = 500f;
            barrel.ApplyEffect(new BurningEffect(intensity: 1.0f));
            Assert.IsTrue(barrel.HasEffect<BurningEffect>());

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new RimeNovaMutation(), 1);
            var rimeNova = mutations.GetMutation<RimeNovaMutation>();

            rimeNova.Cast(zone, zone.GetCell(10, 10), new Random(42));

            Assert.IsFalse(barrel.HasEffect<BurningEffect>(),
                "Rime Nova's -200J cooling should cross FlameTemperature downward and extinguish burning props");
            Assert.Less(barrel.GetPart<ThermalPart>().Temperature, 500f,
                "Barrel temperature should have dropped from the cooling pulse");
        }

        [Test]
        public void RimeNova_FreezesCreaturesInRadius()
        {
            var zone = new Zone("RimeNovaZone");
            var caster = CreateCaster();
            var near = CreateCreature("snapjaw", 50);
            var far = CreateCreature("goatfolk", 50);

            zone.AddEntity(caster, 10, 10);
            zone.AddEntity(near, 11, 10);   // Distance 1
            zone.AddEntity(far, 12, 10);    // Distance 2

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new RimeNovaMutation(), 1);
            var rimeNova = mutations.GetMutation<RimeNovaMutation>();

            rimeNova.Cast(zone, zone.GetCell(10, 10), new Random(42));

            var nearFrost = near.GetEffect<FrozenEffect>();
            var farFrost = far.GetEffect<FrozenEffect>();
            Assert.IsNotNull(nearFrost, "Creature at range 1 should be frozen");
            Assert.IsNotNull(farFrost, "Creature at range 2 should be frozen");
            Assert.AreEqual(0.6f, nearFrost.Cold, 0.001f,
                "Rime Nova should pre-apply FrozenEffect(0.6) distinct from TryFreeze's 1.0");
            Assert.AreEqual(0.6f, farFrost.Cold, 0.001f,
                "Rime Nova should pre-apply FrozenEffect(0.6) distinct from TryFreeze's 1.0");
        }

        // ========================
        // Thunderclap Spell
        // ========================

        [Test]
        public void Thunderclap_ElectrifiesMetalProp_NotWoodProp()
        {
            var zone = new Zone("ThunderclapZone");
            var caster = CreateCaster();

            // Metal-tagged, conductive prop — should be electrified.
            var ironRod = new Entity { BlueprintName = "IronRod" };
            ironRod.AddPart(new RenderPart { DisplayName = "iron rod" });
            ironRod.AddPart(new ThermalPart());
            ironRod.AddPart(new MaterialPart
            {
                MaterialID = "Iron",
                Conductivity = 0.9f,
                MaterialTagsRaw = "Metal,Conductor"
            });

            // Wooden, non-conductive barrel — should be skipped.
            var woodBarrel = CreateCombustibleObject("barrel", flameTemp: 9999f);

            zone.AddEntity(caster, 10, 10);
            zone.AddEntity(ironRod, 11, 10);
            zone.AddEntity(woodBarrel, 9, 10);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ThunderclapMutation(), 1);
            var thunderclap = mutations.GetMutation<ThunderclapMutation>();

            thunderclap.Cast(zone, zone.GetCell(10, 10), new Random(42));

            Assert.IsTrue(ironRod.HasEffect<ElectrifiedEffect>(),
                "Thunderclap should electrify conductive metal props in radius");
            Assert.IsFalse(woodBarrel.HasEffect<ElectrifiedEffect>(),
                "Thunderclap should NOT electrify non-conductive wooden props");
        }

        [Test]
        public void Thunderclap_WetCreatureTakesDoubleDamage()
        {
            var zoneWet = new Zone("ThunderclapZoneWet");
            var casterWet = CreateCaster();
            var wetTarget = CreateCreature("wetsnapjaw", hp: 100);
            wetTarget.ApplyEffect(new WetEffect(moisture: 0.8f));

            zoneWet.AddEntity(casterWet, 10, 10);
            zoneWet.AddEntity(wetTarget, 11, 10);

            var wetMutations = casterWet.GetPart<MutationsPart>();
            wetMutations.AddMutation(new ThunderclapMutation(), 1);
            var thunderclapWet = wetMutations.GetMutation<ThunderclapMutation>();
            thunderclapWet.Cast(zoneWet, zoneWet.GetCell(10, 10), new Random(42));

            int wetHpLoss = 100 - wetTarget.GetStatValue("Hitpoints", 0);

            var zoneDry = new Zone("ThunderclapZoneDry");
            var casterDry = CreateCaster();
            var dryTarget = CreateCreature("drysnapjaw", hp: 100);

            zoneDry.AddEntity(casterDry, 10, 10);
            zoneDry.AddEntity(dryTarget, 11, 10);

            var dryMutations = casterDry.GetPart<MutationsPart>();
            dryMutations.AddMutation(new ThunderclapMutation(), 1);
            var thunderclapDry = dryMutations.GetMutation<ThunderclapMutation>();
            thunderclapDry.Cast(zoneDry, zoneDry.GetCell(10, 10), new Random(42));

            int dryHpLoss = 100 - dryTarget.GetStatValue("Hitpoints", 0);

            Assert.Greater(dryHpLoss, 0, "Dry target should take some damage");
            Assert.AreEqual(dryHpLoss * 2, wetHpLoss,
                "Wet target should take exactly double damage from Thunderclap (same seeded RNG, same 2d6 roll)");
        }

        // ========================
        // Ember Vein Spell
        // ========================

        [Test]
        public void EmberVein_IgnitesRowOfFourCrates()
        {
            var zone = new Zone("EmberVeinZone");
            var caster = CreateCaster();
            var crate1 = CreateCombustibleObject("crate", flameTemp: 200f, heatCapacity: 0.5f);
            var crate2 = CreateCombustibleObject("crate", flameTemp: 200f, heatCapacity: 0.5f);
            var crate3 = CreateCombustibleObject("crate", flameTemp: 200f, heatCapacity: 0.5f);
            var crate4 = CreateCombustibleObject("crate", flameTemp: 200f, heatCapacity: 0.5f);

            zone.AddEntity(caster, 3, 5);
            zone.AddEntity(crate1, 4, 5);
            zone.AddEntity(crate2, 5, 5);
            zone.AddEntity(crate3, 6, 5);
            zone.AddEntity(crate4, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new EmberVeinMutation(), 1);
            var emberVein = mutations.GetMutation<EmberVeinMutation>();

            emberVein.Cast(zone, zone.GetCell(3, 5), 1, 0, new Random(42));

            Assert.IsTrue(crate1.HasEffect<BurningEffect>(), "Crate 1 should ignite from per-cell heat pass");
            Assert.IsTrue(crate2.HasEffect<BurningEffect>(), "Crate 2 should ignite from per-cell heat pass");
            Assert.IsTrue(crate3.HasEffect<BurningEffect>(), "Crate 3 should ignite from per-cell heat pass");
            Assert.IsTrue(crate4.HasEffect<BurningEffect>(), "Crate 4 should ignite from per-cell heat pass");
        }

        [Test]
        public void EmberVein_HitsCreatureAndContinuesToProp()
        {
            var zone = new Zone("EmberVeinZone");
            var caster = CreateCaster();
            var creature = CreateCreature("snapjaw", hp: 100);
            var crate = CreateCombustibleObject("crate", flameTemp: 200f, heatCapacity: 0.5f);

            zone.AddEntity(caster, 3, 5);
            zone.AddEntity(creature, 5, 5);
            zone.AddEntity(crate, 7, 5);

            int hpBefore = creature.GetStatValue("Hitpoints", 100);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new EmberVeinMutation(), 1);
            var emberVein = mutations.GetMutation<EmberVeinMutation>();

            emberVein.Cast(zone, zone.GetCell(3, 5), 1, 0, new Random(42));

            Assert.Less(creature.GetStatValue("Hitpoints", 100), hpBefore,
                "Creature in beam path should take damage from HitEntities pass");
            Assert.IsTrue(crate.HasEffect<BurningEffect>(),
                "Crate beyond the struck creature should still ignite — TraceBeam passes through creatures and the per-cell heat pass iterates the full path");
        }

        // ========================
        // Everyday Grimoires
        // ========================

        [Test]
        public void KindleFlame_HeatsTorchLikeProp_NotCreature()
        {
            var zone = new Zone("KindleFlameZone");
            var caster = CreateCaster();
            var wick = CreateCombustibleObject("wick", flameTemp: 200f, heatCapacity: 0.5f);
            var creature = CreateCreature("snapjaw", 50);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(wick, 6, 5);
            zone.AddEntity(creature, 6, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new KindleFlameMutation(), 1);
            var kindleFlame = mutations.GetMutation<KindleFlameMutation>();

            float wickTempBefore = wick.GetPart<ThermalPart>().Temperature;
            float creatureTempBefore = creature.GetPart<ThermalPart>().Temperature;

            bool cast = kindleFlame.Cast(zone, zone.GetCell(6, 5));

            Assert.IsTrue(cast, "Kindle Flame should affect low-flame-temperature non-creature props.");
            Assert.Greater(wick.GetPart<ThermalPart>().Temperature, wickTempBefore);
            Assert.AreEqual(creatureTempBefore, creature.GetPart<ThermalPart>().Temperature,
                "Kindle Flame should skip creatures.");
        }

        [Test]
        public void DryingBreeze_RemovesWetInRadius()
        {
            var zone = new Zone("DryingBreezeZone");
            var caster = CreateCaster();
            var adjacent = CreateCreature("ally", 40);

            zone.AddEntity(caster, 10, 10);
            zone.AddEntity(adjacent, 11, 10);

            caster.ApplyEffect(new WetEffect(0.7f));
            adjacent.ApplyEffect(new WetEffect(0.7f));

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new DryingBreezeMutation(), 1);
            var dryingBreeze = mutations.GetMutation<DryingBreezeMutation>();

            bool cast = dryingBreeze.Cast(zone, zone.GetCell(10, 10));

            Assert.IsTrue(cast);
            Assert.IsFalse(caster.HasEffect<WetEffect>());
            Assert.IsFalse(adjacent.HasEffect<WetEffect>());
        }

        [Test]
        public void Hearthwarm_AppliesHearthAuraEffect()
        {
            var zone = new Zone("HearthwarmZone");
            var caster = CreateCaster();
            var meat = CreateCombustibleObject("meat", flameTemp: 500f, heatCapacity: 1.0f, combustibility: 0.3f);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(meat, 6, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new HearthwarmMutation(), 1);
            var hearthwarm = mutations.GetMutation<HearthwarmMutation>();

            bool cast = hearthwarm.Cast(zone, zone.GetCell(6, 5));
            Assert.IsTrue(cast);

            var aura = caster.GetEffect<HearthAuraEffect>();
            Assert.IsNotNull(aura);
            Assert.AreEqual(6, aura.TargetX);
            Assert.AreEqual(5, aura.TargetY);
        }

        [Test]
        public void Hearthwarm_CooksRawMeat_Over3Turns()
        {
            var zone = new Zone("HearthwarmCookZone");
            var caster = CreateCaster();

            var meat = new Entity { BlueprintName = "RawMeat" };
            meat.AddPart(new RenderPart { DisplayName = "raw meat" });
            meat.AddPart(new ThermalPart
            {
                Temperature = 25f,
                FlameTemperature = 500f,
                HeatCapacity = 1.0f
            });
            meat.AddPart(new MaterialPart
            {
                MaterialID = "RawMeat",
                Combustibility = 0.2f,
                MaterialTagsRaw = "Organic,RawMeat"
            });

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(meat, 6, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new HearthwarmMutation(), 1);
            var hearthwarm = mutations.GetMutation<HearthwarmMutation>();
            hearthwarm.Cast(zone, zone.GetCell(6, 5));

            // Fire 3 turns: each pulse delivers 60J direct (delta = 60 / HeatCap 1.0 = +60°C)
            // Turn 1: 25 → 85; Turn 2: 85 → 145; Turn 3: 145 → 205 (crosses 150°C cooking threshold)
            for (int i = 0; i < 3; i++)
            {
                var turnEvent = GameEvent.New("BeginTakeAction");
                turnEvent.SetParameter("Zone", (object)zone);
                caster.FireEvent(turnEvent);
                turnEvent.Release();
            }

            Assert.Greater(meat.GetPart<ThermalPart>().Temperature, 150f,
                "After 3 direct heat pulses (60J / HeatCap 1.0 = +60°C each), raw meat must exceed the 150°C cooking threshold");
        }

        [Test]
        public void ConjureWater_WetsTargetCellAndExtinguishesBurningEntity()
        {
            var zone = new Zone("ConjureWaterZone");
            var caster = CreateCaster();
            // Barrel at step 2 (dx=1, range=2). Walker reaches burning barrel on step 2 and stops.
            var barrel = CreateCombustibleObject("barrel", flameTemp: 200f, heatCapacity: 0.5f);

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(barrel, 7, 5);
            barrel.ApplyEffect(new BurningEffect(1.0f));
            Assert.IsTrue(barrel.HasEffect<BurningEffect>());

            MaterialReactionResolver.Initialize(
                "{ \"Reactions\": [ { \"ID\": \"water_plus_fire\", \"Priority\": 60, \"Conditions\": { \"SourceState\": \"Burning\", \"MinMoisture\": 0.3 }, \"Effects\": [ { \"Type\": \"ModifyBurnIntensity\", \"FloatValue\": -1.0, \"StringValue\": \"\" } ] } ] }");
            MaterialReactionResolver.Factory = LoadRealFactory();

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ConjureWaterMutation(), 1);
            var conjureWater = mutations.GetMutation<ConjureWaterMutation>();

            bool cast = conjureWater.Cast(zone, zone.GetCell(5, 5), 1, 0, 2);

            Assert.IsTrue(cast);
            Assert.IsTrue(barrel.HasEffect<WetEffect>());
            Assert.LessOrEqual(barrel.GetEffect<BurningEffect>().Intensity, 0.1f,
                "water_plus_fire should reduce intensity to ~0, causing extinction on next thermal tick.");
        }

        [Test]
        public void ConjureWater_SpawnsAtFirstBurningCell_WhenInRange()
        {
            var zone = new Zone("ConjureWaterWalkerZone");
            var caster = CreateCaster();
            // Burning barrel at step 1; empty cell at step 2. Walker should stop at step 1.
            var burningBarrel = CreateCombustibleObject("barrel", flameTemp: 200f, heatCapacity: 0.5f);
            burningBarrel.ApplyEffect(new BurningEffect(1.0f));

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(burningBarrel, 6, 5);

            MaterialReactionResolver.Initialize("{ \"Reactions\": [] }");
            MaterialReactionResolver.Factory = LoadRealFactory();

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ConjureWaterMutation(), 1);
            var conjureWater = mutations.GetMutation<ConjureWaterMutation>();

            conjureWater.Cast(zone, zone.GetCell(5, 5), 1, 0, 2);

            // Puddle must be on cell (6,5), not (7,5)
            var cell6 = zone.GetCell(6, 5);
            bool hasPuddle = false;
            for (int i = 0; i < cell6.Objects.Count; i++)
            {
                var mp = cell6.Objects[i].GetPart<MaterialPart>();
                if (mp != null && mp.MaterialID == "Water") { hasPuddle = true; break; }
            }
            Assert.IsTrue(hasPuddle, "Water puddle should land on the first burning cell (step 1), not max range");
        }

        [Test]
        public void ConjureWater_SpawnsAtLastPassableCell_WhenBlocked()
        {
            var zone = new Zone("ConjureWaterBlockedZone");
            var caster = CreateCaster();
            // Wall at step 2: walker stops at step 1.
            var wall = new Entity { BlueprintName = "StoneWall" };
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart { DisplayName = "wall" });

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(wall, 7, 5);

            MaterialReactionResolver.Initialize("{ \"Reactions\": [] }");
            MaterialReactionResolver.Factory = LoadRealFactory();

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ConjureWaterMutation(), 1);
            var conjureWater = mutations.GetMutation<ConjureWaterMutation>();

            conjureWater.Cast(zone, zone.GetCell(5, 5), 1, 0, 2);

            // Puddle should land at step 1 (6,5), not on the wall (7,5)
            var cell6 = zone.GetCell(6, 5);
            bool hasPuddle = false;
            for (int i = 0; i < cell6.Objects.Count; i++)
            {
                var mp = cell6.Objects[i].GetPart<MaterialPart>();
                if (mp != null && mp.MaterialID == "Water") { hasPuddle = true; break; }
            }
            Assert.IsTrue(hasPuddle, "Water puddle should land on the last passable cell before the wall");
        }

        [Test]
        public void ConjureWater_FailsWithZeroDirection()
        {
            var zone = new Zone("ConjureWaterZeroDirZone");
            var caster = CreateCaster();
            zone.AddEntity(caster, 5, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ConjureWaterMutation(), 1);
            var conjureWater = mutations.GetMutation<ConjureWaterMutation>();

            bool result = conjureWater.Cast(zone, zone.GetCell(5, 5), 0, 0, 2);
            Assert.IsFalse(result, "Cast with zero direction vector should return false");
        }

        [Test]
        public void ConjureWater_SpawnsWaterPuddleEntity()
        {
            var zone = new Zone("ConjureWaterPuddleZone");
            var caster = CreateCaster();
            zone.AddEntity(caster, 5, 5);

            MaterialReactionResolver.Initialize("{ \"Reactions\": [] }");
            MaterialReactionResolver.Factory = LoadRealFactory();

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ConjureWaterMutation(), 1);
            var conjureWater = mutations.GetMutation<ConjureWaterMutation>();

            conjureWater.Cast(zone, zone.GetCell(5, 5), 1, 0, 2);

            var targetCell = zone.GetCell(7, 5);
            bool hasWater = false;
            for (int i = 0; i < targetCell.Objects.Count; i++)
            {
                var mp = targetCell.Objects[i].GetPart<MaterialPart>();
                if (mp != null && mp.MaterialID == "Water") { hasWater = true; break; }
            }
            Assert.IsTrue(hasWater, "ConjureWater should place a WaterPuddle entity (MaterialID == Water) in the target cell");
        }

        [Test]
        public void ChillDraft_CoolsNearbyEntities()
        {
            var zone = new Zone("ChillDraftZone");
            var caster = CreateCaster();
            var nearby = CreateCombustibleObject("barrel", flameTemp: 500f, heatCapacity: 1.0f);

            zone.AddEntity(caster, 8, 8);
            zone.AddEntity(nearby, 9, 8);

            nearby.GetPart<ThermalPart>().Temperature = 150f;
            float before = nearby.GetPart<ThermalPart>().Temperature;

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new ChillDraftMutation(), 1);
            var chillDraft = mutations.GetMutation<ChillDraftMutation>();

            bool cast = chillDraft.Cast(zone, zone.GetCell(8, 8));

            Assert.IsTrue(cast);
            Assert.Less(nearby.GetPart<ThermalPart>().Temperature, before);
        }

        [Test]
        public void WardGleam_ClearsAcidicAndCharredOnEquippedItems()
        {
            var caster = CreateCaster();
            var inventory = new InventoryPart();
            caster.AddPart(inventory);

            var sword = new Entity { BlueprintName = "Sword" };
            sword.AddPart(new RenderPart { DisplayName = "sword" });
            sword.AddPart(new MaterialPart { MaterialID = "Steel", MaterialTagsRaw = "Metal" });
            sword.ApplyEffect(new AcidicEffect(0.8f));
            sword.ApplyEffect(new CharredEffect());

            inventory.Objects.Add(sword);
            inventory.Equip(sword, "Hand");

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new WardGleamMutation(), 1);
            var wardGleam = mutations.GetMutation<WardGleamMutation>();

            bool cast = wardGleam.Cast();

            Assert.IsTrue(cast);
            Assert.IsFalse(sword.HasEffect<AcidicEffect>());
            Assert.IsFalse(sword.HasEffect<CharredEffect>());
        }

        [Test]
        public void WardGleam_ReturnsFalse_WhenNothingToCleanse()
        {
            var caster = CreateCaster();
            var inventory = new InventoryPart();
            caster.AddPart(inventory);

            // Clean sword with no effects
            var sword = new Entity { BlueprintName = "Sword" };
            sword.AddPart(new RenderPart { DisplayName = "sword" });
            sword.AddPart(new MaterialPart { MaterialID = "Steel", MaterialTagsRaw = "Metal" });
            inventory.Objects.Add(sword);
            inventory.Equip(sword, "Hand");

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new WardGleamMutation(), 1);
            var wardGleam = mutations.GetMutation<WardGleamMutation>();

            bool cast = wardGleam.Cast();

            Assert.IsFalse(cast, "WardGleam should return false when no equipped items have AcidicEffect or CharredEffect");
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("Nothing to cleanse")),
                "WardGleam should log 'Nothing to cleanse' when cast on clean gear");
        }

        [Test]
        public void WardGleam_SkipsUnequippedItems()
        {
            var caster = CreateCaster();
            var inventory = new InventoryPart();
            caster.AddPart(inventory);

            // Corroded sword in inventory but NOT equipped
            var sword = new Entity { BlueprintName = "Sword" };
            sword.AddPart(new RenderPart { DisplayName = "sword" });
            sword.AddPart(new MaterialPart { MaterialID = "Steel", MaterialTagsRaw = "Metal" });
            sword.ApplyEffect(new AcidicEffect(0.8f));
            inventory.Objects.Add(sword);
            // Deliberately NOT calling inventory.Equip(sword, ...)

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new WardGleamMutation(), 1);
            var wardGleam = mutations.GetMutation<WardGleamMutation>();

            wardGleam.Cast();

            Assert.IsTrue(sword.HasEffect<AcidicEffect>(),
                "WardGleam should only cleanse equipped items, not items merely in inventory");
        }

        [Test]
        public void DryingBreeze_IgnoresEntities_BeyondRadius1()
        {
            var zone = new Zone("DryingBreezeRadiusZone");
            var caster = CreateCaster();
            var nearby = CreateCreature("ally", 40);   // Chebyshev distance 1 — inside radius
            var distant = CreateCreature("enemy", 40); // Chebyshev distance 2 — outside radius

            zone.AddEntity(caster, 10, 10);
            zone.AddEntity(nearby, 11, 10);
            zone.AddEntity(distant, 12, 10);

            nearby.ApplyEffect(new WetEffect(0.7f));
            distant.ApplyEffect(new WetEffect(0.7f));

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new DryingBreezeMutation(), 1);
            var dryingBreeze = mutations.GetMutation<DryingBreezeMutation>();

            dryingBreeze.Cast(zone, zone.GetCell(10, 10));

            Assert.IsFalse(nearby.HasEffect<WetEffect>(), "Entity at Chebyshev distance 1 should be dried");
            Assert.IsTrue(distant.HasEffect<WetEffect>(), "Entity at Chebyshev distance 2 should retain WetEffect");
        }

        [Test]
        public void Hearthwarm_FailsOnEmptyCell()
        {
            var zone = new Zone("HearthwarmEmptyZone");
            var caster = CreateCaster();
            zone.AddEntity(caster, 5, 5);
            // Cell (6,5) is empty — no entities with ThermalPart

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new HearthwarmMutation(), 1);
            var hearthwarm = mutations.GetMutation<HearthwarmMutation>();

            bool cast = hearthwarm.Cast(zone, zone.GetCell(6, 5));

            Assert.IsFalse(cast, "Hearthwarm should return false when the target cell has no ThermalPart entities");
        }
    }
}
