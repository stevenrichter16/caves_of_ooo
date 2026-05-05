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
        // Per-skill behavior tests fill in here as WS.2-5 land.
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
