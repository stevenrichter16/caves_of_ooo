using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WS.1+ — skill-driven on-hit effect tests. Pins the contract:
    ///
    ///   No SkillsPart on attacker  → no-op (early-out)
    ///   actualDamage = 0           → no-op (vetoed/resisted hit gate)
    ///   null defender / damage / attacker / rng → no-op (no crash)
    ///
    /// Per-skill behavior tests live below the no-op section and are
    /// added as each WS.2-5 sub-milestone lands its skill branch.
    /// All seed loops are bounded; positive cases assert
    /// "across N seeds, at least one observation" and counter-checks
    /// assert "across N seeds, zero observations" (CLAUDE.md §3.4).
    /// </summary>
    public class OnHitSkillEffectsTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ====================================================================
        // Universal scaffold contract (WS.1) — Apply must be safe to call
        // even when nothing is wired up yet (skills branches fill in WS.2-5).
        // ====================================================================

        [Test]
        public void Apply_NullDefender_NoCrash()
        {
            var attacker = MakeAttacker();
            var damage = new Damage(10);
            damage.AddAttribute("Bludgeoning");

            // Defender == null → early-out. Pre-WS.2 there's nothing else
            // to verify, but this locks the null-safety contract.
            Assert.DoesNotThrow(() =>
                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender: null, attacker: attacker, zone: null,
                    rng: new Random(1)));
        }

        [Test]
        public void Apply_NullAttacker_NoCrash()
        {
            var defender = MakeFighter();
            var damage = new Damage(10);
            damage.AddAttribute("Cudgel");

            // Attacker == null → no SkillsPart to check → early-out.
            // (Some weapons/effects can damage entities without a clear
            // attacker; a falling rock, an environmental hazard, etc.)
            Assert.DoesNotThrow(() =>
                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender: defender, attacker: null, zone: null,
                    rng: new Random(1)));
        }

        [Test]
        public void Apply_AttackerWithoutSkillsPart_NoCrash()
        {
            // Counter-check for the most common live-game path: NPC
            // creatures don't have SkillsPart. Apply must early-out
            // silently and not exception. Without this, every NPC
            // melee swing would NRE post-WS.2.
            var defender = MakeFighter();
            var attacker = new Entity { ID = "npc" };
            attacker.AddPart(new RenderPart { DisplayName = "npc" });
            // Note: deliberately NO AddPart(new SkillsPart()) here.
            var damage = new Damage(10);
            damage.AddAttribute("Cudgel");

            Assert.DoesNotThrow(() =>
                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender: defender, attacker: attacker, zone: null,
                    rng: new Random(1)));

            // And — counter-check — no Stunned applied (since attacker
            // has no SkillsPart, no skill branches can fire).
            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Attacker without SkillsPart must NEVER apply skill-tier effects.");
        }

        [Test]
        public void Apply_ZeroActualDamage_NoEffect()
        {
            // Vetoed / fully-resisted hits gate (matches OnHitClassEffects).
            // Even with the right skill owned, actualDamage<=0 should
            // skip all effect application. Without this, fully-absorbed
            // attacks (Glowmaw vs Fire) would still stun/bleed/confuse.
            var defender = MakeFighter();
            var attacker = MakeAttackerWithSkill("Cudgel_Bludgeon");
            var damage = new Damage(10);
            damage.AddAttribute("Cudgel");

            for (int seed = 0; seed < 100; seed++)
            {
                OnHitSkillEffects.Apply(damage, actualDamage: 0,
                    defender, attacker, zone: null, rng: new Random(seed));
            }
            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "actualDamage=0 means no skill-tier effects fire (parity " +
                "with OnHitClassEffects.Apply's same gate).");
        }

        // ====================================================================
        // WS.2 — Cudgel_Bludgeon: Cudgel-class hit + skill owned →
        // CUDGEL_BLUDGEON_CHANCE_PERCENT (35%) chance to apply Stunned
        // for CUDGEL_BLUDGEON_DURATION (3) turns.
        // ====================================================================

        [Test]
        public void CudgelHit_WithBludgeonOwned_HasChance_ToApplyStunned()
        {
            // Positive: across many seeds, the 35% roll lands at least once.
            // Loop tightly bounded — at 35% chance, P(no observation in 100
            // tries) ≈ 4.4e-18. Test should always observe quickly.
            bool observed = false;
            for (int seed = 0; seed < 100 && !observed; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(Cudgel_Bludgeon));
                var damage = new Damage(10);
                damage.AddAttribute("Cudgel");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                if (defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 100 seeds, at least one Cudgel-attribute hit by an actor with " +
                $"Cudgel_Bludgeon owned should produce Stunned (chance " +
                $"{OnHitSkillEffects.CUDGEL_BLUDGEON_CHANCE_PERCENT}%). " +
                $"None observed — chance gate is broken or always rolls high.");
        }

        [Test]
        public void CudgelHit_WithoutBludgeonOwned_NeverAppliesStunned()
        {
            // Counter-check: same setup as positive, but attacker DOESN'T
            // own Cudgel_Bludgeon. The skill branch must be gated on
            // ownership; otherwise the universal Bludgeoning class hook
            // (which fires on the same swing in CombatSystem) would mask
            // this gate breaking. We isolate by calling OnHitSkillEffects
            // directly — class hook isn't fired here.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttacker();  // no skill added
                var damage = new Damage(10);
                damage.AddAttribute("Cudgel");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    $"Seed {seed}: actor without Cudgel_Bludgeon must never apply " +
                    $"Stunned via this hook — gating on SkillsPart.HasSkill is broken.");
            }
        }

        [Test]
        public void NonCudgelHit_WithBludgeonOwned_NeverAppliesStunned()
        {
            // Counter-check on the attribute side: skill IS owned, but
            // the damage doesn't carry the "Cudgel" sub-class attribute
            // (e.g. plain Bludgeoning, or Cutting). The skill branch must
            // gate on damage.HasAttribute("Cudgel") so a LongSword swing
            // by a Cudgel-trained character doesn't accidentally fire.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(Cudgel_Bludgeon));
                var damage = new Damage(10);
                damage.AddAttribute("Bludgeoning");  // class only, no sub-class

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    $"Seed {seed}: Bludgeoning-only damage (no Cudgel sub-class) " +
                    $"must not trigger Cudgel_Bludgeon — attribute gate is broken.");
            }

            // Counter-check on a different sub-class: Cutting damage with
            // the Cudgel skill owned must also never fire Cudgel_Bludgeon.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(Cudgel_Bludgeon));
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("LongBlades");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    $"Seed {seed}: Cutting/LongBlades damage with Cudgel_Bludgeon owned " +
                    $"must not fire Stunned — skill branches must be attribute-scoped.");
            }
        }

        // ====================================================================
        // WS.3-5 — additional skills' tests fill in here.
        // ====================================================================

        // ─────────────────────────────────────────────────────────────────
        // Test fixtures
        // ─────────────────────────────────────────────────────────────────

        private static Entity MakeFighter()
        {
            var e = new Entity { ID = "fighter" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "fighter" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity MakeAttacker()
        {
            var e = new Entity { ID = "attacker" };
            e.AddPart(new RenderPart { DisplayName = "attacker" });
            e.AddPart(new SkillsPart());
            return e;
        }

        /// <summary>
        /// Helper for per-skill behavior tests (WS.2+). Builds an attacker
        /// with the named skill in their SkillsPart so OnHitSkillEffects
        /// can branch on it. Pre-WS.2 the skill class doesn't exist yet,
        /// so callers using this helper must wait for the matching WS.
        /// Uses the string-class AddSkill overload to dodge the C# class
        /// dependency until the per-skill stubs land.
        /// </summary>
        private static Entity MakeAttackerWithSkill(string skillClass)
        {
            var e = MakeAttacker();
            e.GetPart<SkillsPart>().AddSkill(skillClass, source: "test");
            return e;
        }
    }
}
