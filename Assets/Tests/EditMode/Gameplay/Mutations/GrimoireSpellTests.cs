using System;
using NUnit.Framework;
using CavesOfOoo.Core;

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
    }
}
