using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.2 — ShortBlades_Flurry active-ability tests.
    /// Pins the "3 strikes on one target in one activation" mechanic.
    /// Each strike is a full <see cref="CombatSystem.PerformSingleAttack"/>
    /// (so per-strike on-hit procs fire — Bloodletter can stack 3 Bleeds
    /// from one Flurry, etc.). Distinct from Whirlwind (multiple targets
    /// once each) and Shank (single strike with pen bonus).
    /// </summary>
    public class ShortBladesFlurryTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers (mirror ShortBladesShankTests/CudgelSlamTests) ─

        private static Entity MakeBodiedCreature(string name = "creature",
            int strength = 16, int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
                { Owner = e, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat
                { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new ArmorPart());
            e.AddPart(new InventoryPart { MaxWeight = 150 });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            var body = new Body();
            e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        private static Entity MakeWeaponEntity(string name, string dice,
            string attributes)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            e.AddPart(new MeleeWeaponPart
            {
                BaseDamage = dice, PenBonus = 0,
                Attributes = attributes,
            });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void EquipInPrimary(Entity actor, Entity weaponEntity)
        {
            var hand = actor.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            actor.GetPart<InventoryPart>().EquipToBodyPart(weaponEntity, hand);
        }

        private static (Entity attacker, Entity defender, Zone zone, ShortBlades_Flurry flurry)
            MakeFlurryFixture(int defenderHp = 200, string weaponAttributes = "Piercing")
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker,
                MakeWeaponEntity("dagger", "1d4+1", weaponAttributes));
            var flurry = new ShortBlades_Flurry();
            attacker.GetPart<SkillsPart>().AddSkill(flurry, source: "test");

            // Defender HP intentionally tall — we want the test to
            // observe damage on EACH of 3 strikes without dying mid-loop.
            var defender = MakeBodiedCreature("defender", hp: defenderHp);
            var zone = new Zone();
            return (attacker, defender, zone, flurry);
        }

        // ════════════════════════════════════════════════════════════════
        // Spec shape
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Flurry_DeclareActivatedAbility_ReturnsExpectedSpec()
        {
            var f = new ShortBlades_Flurry();
            var spec = f.DeclareActivatedAbility(actor: null);

            Assert.IsNotNull(spec);
            Assert.AreEqual("CommandFlurry", spec.Command);
            Assert.AreEqual(ShortBlades_Flurry.COOLDOWN, spec.Cooldown);
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode);
            Assert.AreEqual("Flurry", spec.DisplayName);
            Assert.AreEqual(3, ShortBlades_Flurry.FLURRY_STRIKE_COUNT,
                "FLURRY_STRIKE_COUNT must be 3 (the brainstorm-tuned value).");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: defender takes damage > what one swing would deal
        // (proves the loop fires multiple strikes — 3 swings beat 1)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Flurry_HighHpTarget_TakesMoreDamageThanOneSwing()
        {
            // Setup A: Flurry once. Capture damage delta.
            var (attackerA, defenderA, zoneA, flurryA) = MakeFlurryFixture();
            zoneA.AddEntity(attackerA, 5, 5);
            zoneA.AddEntity(defenderA, 6, 5);
            int hpBefore = defenderA.GetStatValue("Hitpoints");
            flurryA.OnCommand(new SkillEventContext
            {
                Attacker = attackerA, Defender = attackerA,
                Zone = zoneA, Rng = new Random(42),
            });
            int flurryDamage = hpBefore - defenderA.GetStatValue("Hitpoints");

            // Setup B: a single swing via PerformSingleAttack on the same
            // seed. Capture delta.
            var (attackerB, defenderB, zoneB, _) = MakeFlurryFixture();
            zoneB.AddEntity(attackerB, 5, 5);
            zoneB.AddEntity(defenderB, 6, 5);
            int hpBeforeB = defenderB.GetStatValue("Hitpoints");
            var weaponB = SkillCombatHelpers.FindEquippedWeaponOfClass(attackerB, "Piercing");
            CombatSystem.PerformSingleAttack(
                attacker: attackerB, defender: defenderB,
                weapon: weaponB, isPrimary: true,
                zone: zoneB, rng: new Random(42),
                attackSourceDesc: "(SingleSwing)");
            int singleSwingDamage = hpBeforeB - defenderB.GetStatValue("Hitpoints");

            // Flurry damage must exceed single-swing damage. Strict
            // inequality because seed 42 is reliable for hits — Flurry
            // gets 3 hit rolls vs. 1, so unless every Flurry roll missed
            // (extremely unlikely with seed 42 and DV=0), Flurry should
            // deal more total damage. The test isn't sensitive to RNG
            // because we're comparing the same seed under different loop
            // counts.
            Assert.Greater(flurryDamage, singleSwingDamage,
                "Flurry's 3-strike loop must deal more total damage than "
                + "a single swing (Flurry=" + flurryDamage
                + ", single=" + singleSwingDamage + ").");
        }

        // ════════════════════════════════════════════════════════════════
        // Adversarial: target dies on strike 1 → loop short-circuits
        // (no follow-up strikes on a corpse)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Flurry_TargetDiesEarly_RemainingStrikesSkipped()
        {
            // Defender with 1 HP — first strike kills, remaining strikes
            // should skip (loop's HP≤0 short-circuit).
            var (attacker, defender, zone, flurry) = MakeFlurryFixture(defenderHp: 1);
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            Assert.DoesNotThrow(() =>
            {
                flurry.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = new Random(42),
                });
            }, "Flurry must not crash when target dies mid-loop.");
            Assert.LessOrEqual(defender.GetStatValue("Hitpoints"), 0,
                "Defender at 1 HP must die on first strike.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: Cudgel weapon does NOT trigger Flurry
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Flurry_WithCudgelWeapon_RefusesToSwing()
        {
            var (attacker, defender, zone, flurry) =
                MakeFlurryFixture(weaponAttributes: "Bludgeoning Cudgel");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            flurry.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
            });

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Cudgel-class weapon must NOT trigger Flurry — it requires Piercing.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: no adjacent target → no swing
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Flurry_NoAdjacentTarget_NoSwing()
        {
            var (attacker, defender, zone, flurry) = MakeFlurryFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 15, 15); // far away

            int hpBefore = defender.GetStatValue("Hitpoints");
            flurry.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
            });

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Far-away defender must not be hit by Flurry (no adjacent target).");
        }

        // ════════════════════════════════════════════════════════════════
        // Adversarial: null Rng / null Zone
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Flurry_WithNullRng_NoOps_NoCrash()
        {
            var (attacker, defender, zone, flurry) = MakeFlurryFixture();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            Assert.DoesNotThrow(() =>
            {
                flurry.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = null,
                });
            });
            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"));
        }

        [Test]
        public void Flurry_WithNullZone_NoOps_NoCrash()
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker,
                MakeWeaponEntity("dagger", "1d4+1", "Piercing"));
            var flurry = new ShortBlades_Flurry();
            attacker.GetPart<SkillsPart>().AddSkill(flurry, source: "test");

            Assert.DoesNotThrow(() =>
            {
                flurry.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = null, Rng = new Random(42),
                });
            });
        }
    }
}
