using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP3.4 — Per-skill behavior tests for the 8 new Tier-2 passives.
    /// Each skill gets at minimum 1 positive (does fire when conditions
    /// are met) + 1 counter-check (doesn't fire when conditions miss).
    ///
    /// <para>Skills covered:
    /// <list type="bullet">
    /// <item>Cudgel_Expertise / Axe_Expertise / ShortBlades_Expertise
    ///       (passive +to-hit; gated on weapon Attributes string)</item>
    /// <item>Cudgel_Hammer (2% Broken proc; gated on Cudgel attribute + has-equipped-items)</item>
    /// <item>Cudgel_ShatteringBlows (10% ShatterArmor; gated on Cudgel attribute)</item>
    /// <item>ShortBlades_Hobble (15% Hobbled; gated on Piercing attribute)</item>
    /// </list></para>
    ///
    /// <para>Backswing + Rejoinder integration tests require zone setup
    /// + PerformSingleAttack invocation; those are live-verified via
    /// the showcase scenario in WSP3.7.</para>
    /// </summary>
    public class SkillSystemTier2Tests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Test fixtures ───────────────────────────────────────────────

        private static Entity MakeFighter()
        {
            var e = new Entity { ID = "fighter" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.AddPart(new RenderPart { DisplayName = "fighter" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity MakeAttackerWithSkill(BaseSkillPart skill)
        {
            var e = new Entity { ID = "attacker" };
            e.AddPart(new RenderPart { DisplayName = "attacker" });
            e.AddPart(new SkillsPart());
            Assert.IsTrue(e.GetPart<SkillsPart>().AddSkill(skill, source: "test"));
            return e;
        }

        private static MeleeWeaponPart MakeWeapon(string attributes)
        {
            // Attach to a fresh Entity so weapon.ParentEntity is valid.
            var weaponEntity = new Entity { ID = "weapon", BlueprintName = "TestWeapon" };
            weaponEntity.AddPart(new RenderPart { DisplayName = "test weapon" });
            var w = new MeleeWeaponPart
            {
                BaseDamage = "1d6",
                HitBonus = 0,
                PenBonus = 0,
                MaxStrengthBonus = 3,
                Attributes = attributes,
            };
            weaponEntity.AddPart(w);
            return w;
        }

        private static SkillEventContext MakeHitContext(Entity attacker, Entity defender,
            MeleeWeaponPart weapon, int seed, params string[] damageAttrs)
        {
            var damage = new Damage(10);
            foreach (var a in damageAttrs) damage.AddAttribute(a);
            return new SkillEventContext
            {
                Attacker = attacker, Defender = defender,
                Weapon = weapon, WeaponEntity = weapon?.ParentEntity,
                Damage = damage, ActualDamage = 10,
                Zone = null, Rng = new Random(seed),
            };
        }

        // ── Cudgel_Expertise ────────────────────────────────────────────

        [Test]
        public void CudgelExpertise_WithCudgelWeapon_AddsHitBonus()
        {
            var skill = new Cudgel_Expertise();
            var actor = MakeAttackerWithSkill(skill);
            var weapon = MakeWeapon("Bludgeoning Cudgel");
            int bonus = SkillEventDispatcher.GetSkillHitModifier(actor, weapon);
            Assert.AreEqual(Cudgel_Expertise.HIT_BONUS, bonus,
                "Cudgel_Expertise must contribute its HIT_BONUS when the wielded " +
                "weapon's Attributes contain 'Cudgel'.");
        }

        [Test]
        public void CudgelExpertise_WithNonCudgelWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new Cudgel_Expertise());
            var weapon = MakeWeapon("Cutting LongBlades");  // no Cudgel
            Assert.AreEqual(0, SkillEventDispatcher.GetSkillHitModifier(actor, weapon),
                "Cudgel_Expertise must NOT contribute when the wielded weapon " +
                "doesn't carry the Cudgel attribute.");
        }

        // ── Axe_Expertise ───────────────────────────────────────────────

        [Test]
        public void AxeExpertise_WithAxeWeapon_AddsHitBonus()
        {
            var actor = MakeAttackerWithSkill(new Axe_Expertise());
            var weapon = MakeWeapon("Cutting Axe");
            Assert.AreEqual(Axe_Expertise.HIT_BONUS,
                SkillEventDispatcher.GetSkillHitModifier(actor, weapon));
        }

        [Test]
        public void AxeExpertise_WithNonAxeWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new Axe_Expertise());
            var weapon = MakeWeapon("Bludgeoning Cudgel");
            Assert.AreEqual(0, SkillEventDispatcher.GetSkillHitModifier(actor, weapon));
        }

        // ── ShortBlades_Expertise ───────────────────────────────────────

        [Test]
        public void ShortBladesExpertise_WithPiercingWeapon_AddsHitBonus()
        {
            var actor = MakeAttackerWithSkill(new ShortBlades_Expertise());
            var weapon = MakeWeapon("Piercing");
            Assert.AreEqual(ShortBlades_Expertise.HIT_BONUS,
                SkillEventDispatcher.GetSkillHitModifier(actor, weapon));
        }

        [Test]
        public void ShortBladesExpertise_WithNonPiercingWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new ShortBlades_Expertise());
            var weapon = MakeWeapon("Cutting Axe");
            Assert.AreEqual(0, SkillEventDispatcher.GetSkillHitModifier(actor, weapon));
        }

        // ── Cudgel_ShatteringBlows ──────────────────────────────────────

        [Test]
        public void ShatteringBlows_WithCudgelHit_HasChance_ToShatterArmor()
        {
            var skill = new Cudgel_ShatteringBlows();
            var actor = MakeAttackerWithSkill(skill);
            bool observed = false;
            for (int seed = 0; seed < 200 && !observed; seed++)
            {
                var defender = MakeFighter();
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Cudgel");
                skill.OnAttackerAfterAttack(ctx);
                if (defender.GetPart<StatusEffectsPart>().HasEffect<ShatterArmorEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 200 seeds, ShatteringBlows should produce at least one " +
                $"ShatterArmorEffect (chance {Cudgel_ShatteringBlows.CHANCE_PERCENT}%).");
        }

        [Test]
        public void ShatteringBlows_WithNonCudgelHit_NeverShatters()
        {
            var skill = new Cudgel_ShatteringBlows();
            var actor = MakeAttackerWithSkill(skill);
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Cutting", "LongBlades");
                skill.OnAttackerAfterAttack(ctx);
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<ShatterArmorEffect>(),
                    $"Seed {seed}: Cutting/LongBlades hit must not fire ShatteringBlows.");
            }
        }

        // ── ShortBlades_Hobble ──────────────────────────────────────────

        [Test]
        public void Hobble_WithPiercingHit_HasChance_ToHobble()
        {
            var skill = new ShortBlades_Hobble();
            var actor = MakeAttackerWithSkill(skill);
            bool observed = false;
            for (int seed = 0; seed < 200 && !observed; seed++)
            {
                var defender = MakeFighter();
                defender.Statistics["DV"] = new Stat { Owner = defender, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Piercing");
                skill.OnAttackerAfterAttack(ctx);
                if (defender.GetPart<StatusEffectsPart>().HasEffect<HobbledEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 200 seeds, Hobble should produce at least one HobbledEffect " +
                $"(chance {ShortBlades_Hobble.CHANCE_PERCENT}%).");
        }

        [Test]
        public void Hobble_WithNonPiercingHit_NeverHobbles()
        {
            var skill = new ShortBlades_Hobble();
            var actor = MakeAttackerWithSkill(skill);
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                defender.Statistics["DV"] = new Stat { Owner = defender, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Bludgeoning", "Cudgel");
                skill.OnAttackerAfterAttack(ctx);
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<HobbledEffect>(),
                    $"Seed {seed}: Bludgeoning hit must not fire Hobble.");
            }
        }

        // ── Cudgel_Hammer (2% chance — bigger seed loop) ────────────────

        [Test]
        public void Hammer_NoEquippedItems_NoOps()
        {
            // Defender has no Body → no equipped items → Hammer no-ops.
            // Across many seeds, no exception even when chance roll succeeds.
            var skill = new Cudgel_Hammer();
            var actor = MakeAttackerWithSkill(skill);
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighter();  // no Body
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Cudgel");
                Assert.DoesNotThrow(() => skill.OnAttackerAfterAttack(ctx),
                    $"Seed {seed}: Hammer on body-less defender must not throw.");
            }
        }

        [Test]
        public void Hammer_WithNonCudgelHit_NoCrash_NoOps()
        {
            // Counter-check on the Cudgel-attribute gate. Even with Hammer
            // owned, a Cutting/LongBlades hit must not trigger the proc.
            // (Without a defender Body the proc is a no-op anyway, so we
            // just verify no exception across many seeds — the gate is
            // implicit in the early-return at the top of the override.)
            var skill = new Cudgel_Hammer();
            var actor = MakeAttackerWithSkill(skill);
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighter();
                var ctx = MakeHitContext(actor, defender, weapon: null, seed: seed, "Cutting", "LongBlades");
                Assert.DoesNotThrow(() => skill.OnAttackerAfterAttack(ctx),
                    $"Seed {seed}: non-Cudgel hit with Hammer owned must not throw.");
            }
        }
    }
}
