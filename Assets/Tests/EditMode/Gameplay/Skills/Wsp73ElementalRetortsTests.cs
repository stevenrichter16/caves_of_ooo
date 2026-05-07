using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP7.3 — Tests for the 4 elemental retort powers
    /// (Pyromancy_ScorchRetort, Cryomancy_FrostRetort,
    /// Galvanism_ShockRetort, Corrosion_AcidRetort).
    ///
    /// <para>Pattern: each is a defender-side passive that fires
    /// from <c>OnDefenderAfterAttackMissed</c> when an attack on
    /// the defender misses. Deals a small flat elemental damage
    /// to the original attacker.</para>
    /// </summary>
    public class Wsp73ElementalRetortsTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeFighter(string name, int hp = 100)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new SkillsPart());
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static SkillEventContext MakeMissContext(Entity attacker, Entity defender)
        {
            return new SkillEventContext
            {
                Attacker = attacker, Defender = defender,
                Damage = null, ActualDamage = 0,
                Zone = null, Rng = new Random(0),
            };
        }

        // ════════════════════════════════════════════════════════════════
        // ScorchRetort — 3 Heat damage to attacker on miss
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ScorchRetort_OnMiss_DealsHeatDamageToAttacker()
        {
            var defender = MakeFighter("defender");
            var skill = new Pyromancy_ScorchRetort();
            defender.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            var attacker = MakeFighter("attacker");
            int hpBefore = attacker.GetStatValue("Hitpoints");
            skill.OnDefenderAfterAttackMissed(MakeMissContext(attacker, defender));

            Assert.AreEqual(Pyromancy_ScorchRetort.RETORT_DAMAGE,
                hpBefore - attacker.GetStatValue("Hitpoints"),
                "ScorchRetort must deal RETORT_DAMAGE Heat damage to attacker on miss.");
        }

        [Test]
        public void ScorchRetort_NullAttacker_NoCrash()
        {
            var skill = new Pyromancy_ScorchRetort();
            var ctx = new SkillEventContext
            {
                Attacker = null, Defender = MakeFighter("defender"),
                Rng = new Random(0),
            };
            Assert.DoesNotThrow(() => skill.OnDefenderAfterAttackMissed(ctx));
        }

        [Test]
        public void ScorchRetort_HeatResistantAttacker_TakesReducedDamage()
        {
            // Pin the elemental tagging — a Heat-resistant attacker
            // takes reduced retort damage (proves the damage is
            // properly Heat-tagged for the resistance pipeline).
            var defender = MakeFighter("defender");
            defender.GetPart<SkillsPart>().AddSkill(new Pyromancy_ScorchRetort(),
                source: "test");
            var skill = defender.GetPart<SkillsPart>().GetSkill("Pyromancy_ScorchRetort");

            var attacker = MakeFighter("attacker");
            attacker.Statistics["HeatResistance"] = new Stat
                { Owner = attacker, Name = "HeatResistance", BaseValue = 100,
                  Min = -100, Max = 100 };

            int hpBefore = attacker.GetStatValue("Hitpoints");
            skill.OnDefenderAfterAttackMissed(MakeMissContext(attacker, defender));
            int hpAfter = attacker.GetStatValue("Hitpoints");

            Assert.AreEqual(0, hpBefore - hpAfter,
                "100% HeatResistance must absorb all ScorchRetort damage. " +
                "If attacker takes damage, the retort isn't being tagged Heat correctly.");
        }

        // ════════════════════════════════════════════════════════════════
        // FrostRetort — 3 Cold damage to attacker on miss
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void FrostRetort_OnMiss_DealsColdDamageToAttacker()
        {
            var defender = MakeFighter("defender");
            var skill = new Cryomancy_FrostRetort();
            defender.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var attacker = MakeFighter("attacker");

            int hpBefore = attacker.GetStatValue("Hitpoints");
            skill.OnDefenderAfterAttackMissed(MakeMissContext(attacker, defender));
            Assert.AreEqual(Cryomancy_FrostRetort.RETORT_DAMAGE,
                hpBefore - attacker.GetStatValue("Hitpoints"));
        }

        [Test]
        public void FrostRetort_ColdResistantAttacker_TakesReducedDamage()
        {
            var defender = MakeFighter("defender");
            defender.GetPart<SkillsPart>().AddSkill(new Cryomancy_FrostRetort(),
                source: "test");
            var skill = defender.GetPart<SkillsPart>().GetSkill("Cryomancy_FrostRetort");

            var attacker = MakeFighter("attacker");
            attacker.Statistics["ColdResistance"] = new Stat
                { Owner = attacker, Name = "ColdResistance", BaseValue = 100,
                  Min = -100, Max = 100 };

            int hpBefore = attacker.GetStatValue("Hitpoints");
            skill.OnDefenderAfterAttackMissed(MakeMissContext(attacker, defender));
            Assert.AreEqual(0, hpBefore - attacker.GetStatValue("Hitpoints"));
        }

        // ════════════════════════════════════════════════════════════════
        // ShockRetort — 3 Electric damage to attacker on miss
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ShockRetort_OnMiss_DealsElectricDamageToAttacker()
        {
            var defender = MakeFighter("defender");
            var skill = new Galvanism_ShockRetort();
            defender.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var attacker = MakeFighter("attacker");

            int hpBefore = attacker.GetStatValue("Hitpoints");
            skill.OnDefenderAfterAttackMissed(MakeMissContext(attacker, defender));
            Assert.AreEqual(Galvanism_ShockRetort.RETORT_DAMAGE,
                hpBefore - attacker.GetStatValue("Hitpoints"));
        }

        [Test]
        public void ShockRetort_ElectricResistantAttacker_TakesReducedDamage()
        {
            var defender = MakeFighter("defender");
            defender.GetPart<SkillsPart>().AddSkill(new Galvanism_ShockRetort(),
                source: "test");
            var skill = defender.GetPart<SkillsPart>().GetSkill("Galvanism_ShockRetort");

            var attacker = MakeFighter("attacker");
            attacker.Statistics["ElectricResistance"] = new Stat
                { Owner = attacker, Name = "ElectricResistance", BaseValue = 100,
                  Min = -100, Max = 100 };

            int hpBefore = attacker.GetStatValue("Hitpoints");
            skill.OnDefenderAfterAttackMissed(MakeMissContext(attacker, defender));
            Assert.AreEqual(0, hpBefore - attacker.GetStatValue("Hitpoints"));
        }

        // ════════════════════════════════════════════════════════════════
        // AcidRetort — 3 Acid damage to attacker on miss
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AcidRetort_OnMiss_DealsAcidDamageToAttacker()
        {
            var defender = MakeFighter("defender");
            var skill = new Corrosion_AcidRetort();
            defender.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var attacker = MakeFighter("attacker");

            int hpBefore = attacker.GetStatValue("Hitpoints");
            skill.OnDefenderAfterAttackMissed(MakeMissContext(attacker, defender));
            Assert.AreEqual(Corrosion_AcidRetort.RETORT_DAMAGE,
                hpBefore - attacker.GetStatValue("Hitpoints"));
        }

        [Test]
        public void AcidRetort_AcidResistantAttacker_TakesReducedDamage()
        {
            var defender = MakeFighter("defender");
            defender.GetPart<SkillsPart>().AddSkill(new Corrosion_AcidRetort(),
                source: "test");
            var skill = defender.GetPart<SkillsPart>().GetSkill("Corrosion_AcidRetort");

            var attacker = MakeFighter("attacker");
            attacker.Statistics["AcidResistance"] = new Stat
                { Owner = attacker, Name = "AcidResistance", BaseValue = 100,
                  Min = -100, Max = 100 };

            int hpBefore = attacker.GetStatValue("Hitpoints");
            skill.OnDefenderAfterAttackMissed(MakeMissContext(attacker, defender));
            Assert.AreEqual(0, hpBefore - attacker.GetStatValue("Hitpoints"));
        }

        // ════════════════════════════════════════════════════════════════
        // ALL 4 RETORTS STACK — defender with all 4 elemental retorts
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AllFourRetorts_OnMiss_DealAggregateElementalDamage()
        {
            // Defender owns all 4. Attack misses → each retort fires
            // independently. Total = 4 × RETORT_DAMAGE = 12 HP if no
            // resistances apply.
            var defender = MakeFighter("defender");
            defender.GetPart<SkillsPart>().AddSkill(new Pyromancy_ScorchRetort(),
                source: "test");
            defender.GetPart<SkillsPart>().AddSkill(new Cryomancy_FrostRetort(),
                source: "test");
            defender.GetPart<SkillsPart>().AddSkill(new Galvanism_ShockRetort(),
                source: "test");
            defender.GetPart<SkillsPart>().AddSkill(new Corrosion_AcidRetort(),
                source: "test");

            var attacker = MakeFighter("attacker");
            int hpBefore = attacker.GetStatValue("Hitpoints");
            // Use the dispatcher (real path) — all 4 retorts fire.
            SkillEventDispatcher.DefenderAfterAttackMissed(defender,
                MakeMissContext(attacker, defender));

            int totalDamage = hpBefore - attacker.GetStatValue("Hitpoints");
            Assert.AreEqual(4 * Pyromancy_ScorchRetort.RETORT_DAMAGE, totalDamage,
                "All 4 retorts must fire independently via the dispatcher path. " +
                $"Expected 4×3=12 HP damage; got {totalDamage}.");
        }

        // ════════════════════════════════════════════════════════════════
        // JSON CONTENT — all 4 retort powers register
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Wsp73Retorts_AllRegisteredInSkillRegistryFromJson()
        {
            SkillRegistry.EnsureInitialized();
            string[] retorts = new[]
            {
                "Pyromancy_ScorchRetort",
                "Cryomancy_FrostRetort",
                "Galvanism_ShockRetort",
                "Corrosion_AcidRetort",
            };
            foreach (var className in retorts)
            {
                Assert.IsTrue(SkillRegistry.TryGetPowerByClass(className, out var power),
                    $"WSP7.3 retort '{className}' must register from JSON.");
                Assert.AreEqual(1, power.Cost);
                Assert.IsFalse(string.IsNullOrEmpty(power.Description));
            }
        }
    }
}
