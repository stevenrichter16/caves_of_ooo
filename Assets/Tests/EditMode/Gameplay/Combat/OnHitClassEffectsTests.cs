using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Class-based on-hit effect hook tests. Pins the contract:
    ///
    ///   Bludgeoning damage → some chance of StunnedEffect
    ///   Cutting damage     → some chance of BleedingEffect
    ///   Piercing damage    → some chance of ConfusedEffect
    ///   Other classes      → no class effect fires
    ///   actualDamage = 0   → no class effects fire (vetoed/fully-resisted)
    ///   null defender      → no crash
    ///   no StatusEffectsPart on defender → auto-created (silent)
    ///
    /// Probabilities are configurable via constants in
    /// <see cref="OnHitClassEffects"/>. Tests assert "across N seeds, at
    /// least one observation" for positive cases, and "across N seeds,
    /// zero observations" for counter-checks.
    /// </summary>
    public class OnHitClassEffectsTests
    {
        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // 1. Positive: each class applies its effect across many seeds
        // ====================================================================

        [Test]
        public void BludgeoningHit_HasChance_ToApplyStunned()
        {
            bool observed = false;
            for (int seed = 0; seed < 200 && !observed; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttribute("Bludgeoning");
                OnHitClassEffects.Apply(damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                if (defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                "Across 200 seeds, at least one Bludgeoning hit should produce " +
                "Stunned. None observed — chance gate is broken or always rolls high.");
        }

        [Test]
        public void CuttingHit_HasChance_ToApplyBleeding()
        {
            bool observed = false;
            for (int seed = 0; seed < 200 && !observed; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                OnHitClassEffects.Apply(damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                if (defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                "Across 200 seeds, at least one Cutting hit should produce Bleeding.");
        }

        [Test]
        public void PiercingHit_HasChance_ToApplyConfused()
        {
            bool observed = false;
            for (int seed = 0; seed < 500 && !observed; seed++)
            {
                // Higher seed cap because Confuse chance is only 10% (1 in 10).
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttribute("Piercing");
                OnHitClassEffects.Apply(damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                if (defender.GetPart<StatusEffectsPart>().HasEffect<ConfusedEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                "Across 500 seeds, at least one Piercing hit should produce Confused.");
        }

        // ====================================================================
        // 2. Counter-checks: non-matching classes don't trigger the effect
        // ====================================================================

        [Test]
        public void NonBludgeoning_DoesNotStun()
        {
            // Cutting + Piercing + elemental damage — no Bludgeoning attribute.
            for (int seed = 0; seed < 500; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("Piercing");
                damage.AddAttribute("Fire");
                OnHitClassEffects.Apply(damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    $"Seed {seed}: non-Bludgeoning damage should NEVER produce Stunned.");
            }
        }

        [Test]
        public void NonCutting_DoesNotBleed()
        {
            for (int seed = 0; seed < 500; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttribute("Bludgeoning");
                damage.AddAttribute("Piercing");
                OnHitClassEffects.Apply(damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: non-Cutting damage should NEVER produce Bleeding.");
            }
        }

        [Test]
        public void NonPiercing_DoesNotConfuse()
        {
            for (int seed = 0; seed < 500; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("Bludgeoning");
                OnHitClassEffects.Apply(damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<ConfusedEffect>(),
                    $"Seed {seed}: non-Piercing damage should NEVER produce Confused.");
            }
        }

        // ====================================================================
        // 3. Adversarial: edge cases and null-safety
        // ====================================================================

        [Test]
        public void OnHitClassEffects_OnZeroDamage_NoOp()
        {
            // Vetoed/fully-resisted hits (actualDamage = 0) should not trigger
            // any class effect even if attributes are present.
            for (int seed = 0; seed < 200; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttribute("Bludgeoning");
                damage.AddAttribute("Cutting");
                damage.AddAttribute("Piercing");
                OnHitClassEffects.Apply(damage, actualDamage: 0, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                var sep = defender.GetPart<StatusEffectsPart>();
                Assert.IsFalse(sep.HasEffect<StunnedEffect>(),
                    $"Seed {seed}: zero-damage hit should never apply Stunned.");
                Assert.IsFalse(sep.HasEffect<BleedingEffect>(),
                    $"Seed {seed}: zero-damage hit should never apply Bleeding.");
                Assert.IsFalse(sep.HasEffect<ConfusedEffect>(),
                    $"Seed {seed}: zero-damage hit should never apply Confused.");
            }
        }

        [Test]
        public void OnHitClassEffects_NullDefender_NoCrash()
        {
            var damage = new Damage(10);
            damage.AddAttribute("Bludgeoning");
            Assert.DoesNotThrow(() =>
                OnHitClassEffects.Apply(damage, actualDamage: 10, defender: null,
                    attacker: null, zone: null, rng: new Random(0)));
        }

        [Test]
        public void OnHitClassEffects_NullDamage_NoCrash()
        {
            var defender = MakeFighter();
            Assert.DoesNotThrow(() =>
                OnHitClassEffects.Apply(damage: null, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(0)));
        }

        [Test]
        public void OnHitClassEffects_NullRng_NoCrash()
        {
            var defender = MakeFighter();
            var damage = new Damage(10);
            damage.AddAttribute("Bludgeoning");
            Assert.DoesNotThrow(() =>
                OnHitClassEffects.Apply(damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: null));
        }

        [Test]
        public void OnHitClassEffects_DefenderWithoutStatusEffectsPart_AutoCreates()
        {
            // Make a fighter without StatusEffectsPart added explicitly.
            // Entity.ApplyEffect calls EnsureStatusEffectsPart() which auto-creates.
            var defender = new Entity { ID = "bare" };
            defender.Statistics["Hitpoints"] = new Stat
                { Owner = defender, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            defender.Statistics["Toughness"] = new Stat
                { Owner = defender, Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            // Confirm starting state: no StatusEffectsPart.
            Assert.IsNull(defender.GetPart<StatusEffectsPart>(),
                "Test setup precondition: defender has no StatusEffectsPart.");

            // Force a roll that will succeed: seed 0 with high-chance Cutting.
            // Try a few seeds in case seed 0 happens to whiff.
            for (int seed = 0; seed < 200; seed++)
            {
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                OnHitClassEffects.Apply(damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                var sep = defender.GetPart<StatusEffectsPart>();
                if (sep != null && sep.HasEffect<BleedingEffect>())
                {
                    // Auto-creation worked.
                    Assert.Pass();
                    return;
                }
            }
            Assert.Fail("Across 200 seeds, no BleedingEffect was applied — " +
                "either the auto-create path is broken or the chance gate is too low.");
        }

        // ====================================================================
        // 4. Adversarial: Stunned stacking (two Bludgeoning hits extend duration)
        // ====================================================================

        [Test]
        public void OnHitClassEffects_StackingBludgeoning_ExtendsStunDuration()
        {
            // Two seeded Bludgeoning hits that both trigger Stun should stack
            // duration per StunnedEffect.OnStack contract (Duration += incoming).
            var defender = MakeFighter();

            // Find two seeds that both trigger Stun.
            int? firstSeed = null, secondSeed = null;
            for (int seed = 0; seed < 300; seed++)
            {
                var rng = new Random(seed);
                if (rng.Next(100) < OnHitClassEffects.BLUDGEONING_STUN_CHANCE_PERCENT)
                {
                    if (firstSeed == null) firstSeed = seed;
                    else { secondSeed = seed; break; }
                }
            }
            Assert.IsNotNull(firstSeed, "Couldn't find any seed triggering Stun in 300 tries.");
            Assert.IsNotNull(secondSeed, "Couldn't find a second seed triggering Stun.");

            var dmg = new Damage(10);
            dmg.AddAttribute("Bludgeoning");

            OnHitClassEffects.Apply(dmg, 10, defender, null, null, new Random(firstSeed.Value));
            int durationAfterFirst = defender.GetPart<StatusEffectsPart>()
                .GetEffect<StunnedEffect>().Duration;

            OnHitClassEffects.Apply(dmg, 10, defender, null, null, new Random(secondSeed.Value));
            int durationAfterSecond = defender.GetPart<StatusEffectsPart>()
                .GetEffect<StunnedEffect>().Duration;

            Assert.Greater(durationAfterSecond, durationAfterFirst,
                $"Stacking Bludgeoning Stun should extend duration. " +
                $"Got {durationAfterFirst} → {durationAfterSecond}.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeFighter()
        {
            var entity = new Entity { ID = "fighter" };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat
                { Owner = entity, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            entity.Statistics["Toughness"] = new Stat
                { Owner = entity, Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat
                { Owner = entity, Name = "Agility", BaseValue = 10, Min = 0, Max = 50 };
            entity.Statistics["DV"] = new Stat
                { Owner = entity, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            entity.AddPart(new RenderPart { DisplayName = "fighter" });
            entity.AddPart(new StatusEffectsPart());
            return entity;
        }
    }
}
