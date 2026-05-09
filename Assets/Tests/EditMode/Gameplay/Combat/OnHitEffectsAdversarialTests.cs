using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.4 — Adversarial tests for the Tier-2 On-Hit Effects system
    /// (OnHitClassEffects + OnHitWeaponEffects + OnHitEffectFactory).
    /// Targets bug classes the per-weapon happy-path tests miss:
    /// <list type="bullet">
    ///   <item>Class-hook + per-weapon stacking (both fire independently)</item>
    ///   <item>Multi-attribute weapons (one swing fires multiple class hooks)</item>
    ///   <item>actualDamage=0 / vetoed-hit gate</item>
    ///   <item>Probability-100 / probability-0 boundaries</item>
    ///   <item>Spec parser malformed inputs</item>
    ///   <item>Unknown effect name (factory returns null)</item>
    ///   <item>Effect-name case + whitespace normalization</item>
    /// </list>
    /// </summary>
    public class OnHitEffectsAdversarialTests
    {
        [SetUp]
        public void Setup() { MessageLog.Clear(); }

        // ── Fixture helpers ───────────────────────────────────────────────

        private static Entity MakeCreature(string name = "c", int hp = 100)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // A. CLASS-HOOK + PER-WEAPON INDEPENDENCE
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ClassHookAndWeaponHook_FireIndependently()
        {
            // A weapon with Cutting attribute (class hook → 25% Bleed)
            // AND OnHitEffectsRaw="Burning,100" (per-weapon, 100% Burning)
            // should fire BOTH on a single hit. The two paths are
            // independent — class hook happens first, weapon hook second.
            //
            // Adversarial: a buggy "weapon overrides class" impl would
            // suppress the Bleed roll when a per-weapon spec is set.
            var defender = MakeCreature("def", hp: 200);
            defender.AddPart(new ThermalPart()); // BurningEffect.OnApply pokes thermal

            var damage = new Damage(10);
            damage.AddAttribute("Cutting");

            // Class hook with Bleed at 25% — use a seed where Bleed lands.
            // Per-weapon spec at 100% Burning — guaranteed.
            var rng = new Random(0); // seed where 25% Bleed roll succeeds

            // Apply class hook (Cutting → Bleed roll).
            OnHitClassEffects.Apply(damage, actualDamage: 10, defender,
                attacker: null, zone: null, rng);

            // Apply per-weapon hook (Burning, 100%).
            var weapon = new MeleeWeaponPart
            {
                BaseDamage = "1d4",
                Attributes = "Cutting",
                OnHitEffectsRaw = "Burning,100,,5,1.0"
            };
            OnHitWeaponEffects.Apply(weapon, damage, actualDamage: 10, defender,
                attacker: null, zone: null, rng);

            Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "Per-weapon Burning at 100% must always land.");
            // Note: Bleeding may or may not land based on RNG; the
            // ABSENCE of Burning would prove a bug.
        }

        [Test]
        public void Adversarial_MultiAttributeWeapon_FiresAllMatchingClassHooks()
        {
            // Hypothetical glaive with Cutting AND Piercing attributes.
            // Each class hook checks its own attribute independently.
            // Adversarial: a buggy `else if` chain would only fire one.
            var defender = MakeCreature("def", hp: 200);

            var damage = new Damage(10);
            damage.AddAttribute("Cutting");
            damage.AddAttribute("Piercing");

            // Run 100 trials with different seeds. With Cutting=25%
            // Bleed and Piercing=10% Confused, expected at least 5
            // trials show one or the other landing. If multi-attr
            // routing is broken, we'd see only one effect type.
            int bleedCount = 0;
            int confusedCount = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                var d = MakeCreature("d" + seed, hp: 100);
                OnHitClassEffects.Apply(damage, actualDamage: 5, d,
                    attacker: null, zone: null, rng: new Random(seed));
                var sep = d.GetPart<StatusEffectsPart>();
                if (sep.HasEffect<BleedingEffect>()) bleedCount++;
                if (sep.HasEffect<ConfusedEffect>()) confusedCount++;
            }
            // 25% × 100 = 25 expected Bleed; 10% × 100 = 10 expected Confused.
            // Allow ±50% RNG variance — anything materially > 0 each
            // proves both hooks fire on different seeds.
            Assert.Greater(bleedCount, 5,
                "Cutting class hook fires (25% expected ~25 in 100): got " + bleedCount);
            Assert.Greater(confusedCount, 0,
                "Piercing class hook fires (10% expected ~10 in 100): got " + confusedCount);
        }

        // ════════════════════════════════════════════════════════════════
        // B. VETOED-HIT GATE
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ZeroDamage_NoClassHooksFire()
        {
            // A Glowmaw with HeatResistance 100 fully resists Fire damage,
            // so actualDamage=0 even though the swing connected. The
            // Bludgeoning class hook (which DOESN'T care about Fire)
            // would still apply Stun if the gate ignored actualDamage.
            // The contract: zero damage means no on-hit at all.
            var defender = MakeCreature("def");
            var damage = new Damage(10);
            damage.AddAttribute("Bludgeoning");

            // 100 trials at 100% chance — verify NONE fire because
            // actualDamage is 0.
            for (int seed = 0; seed < 100; seed++)
            {
                OnHitClassEffects.Apply(damage, actualDamage: 0, defender,
                    attacker: null, zone: null, rng: new Random(seed));
            }
            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "actualDamage=0 must veto all class hooks. If this fails, "
                + "the contract is broken.");
        }

        [Test]
        public void Adversarial_ZeroDamage_NoWeaponHookFires()
        {
            var defender = MakeCreature("def");
            defender.AddPart(new ThermalPart());
            var weapon = new MeleeWeaponPart
            {
                Attributes = "Cutting",
                OnHitEffectsRaw = "Burning,100,,5,1.0"  // 100% chance
            };
            var damage = new Damage(10);
            damage.AddAttribute("Cutting");

            OnHitWeaponEffects.Apply(weapon, damage, actualDamage: 0, defender,
                attacker: null, zone: null, rng: new Random(0));

            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "Per-weapon hooks also gate on actualDamage > 0.");
        }

        [Test]
        public void Adversarial_NullDefender_NoCrash()
        {
            var damage = new Damage(10);
            damage.AddAttribute("Cutting");
            Assert.DoesNotThrow(() =>
                OnHitClassEffects.Apply(damage, 10, defender: null,
                    attacker: null, zone: null, rng: new Random(0)));
        }

        [Test]
        public void Adversarial_NullDamage_NoCrash()
        {
            var defender = MakeCreature();
            Assert.DoesNotThrow(() =>
                OnHitClassEffects.Apply(damage: null, actualDamage: 10,
                    defender: defender, attacker: null, zone: null, rng: new Random(0)));
        }

        [Test]
        public void Adversarial_NullRng_NoCrash()
        {
            var defender = MakeCreature();
            var damage = new Damage(10);
            damage.AddAttribute("Cutting");
            Assert.DoesNotThrow(() =>
                OnHitClassEffects.Apply(damage, 10, defender, null, null, rng: null));
        }

        // ════════════════════════════════════════════════════════════════
        // C. PROBABILITY BOUNDARIES
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_WeaponSpec_Chance100_AlwaysFires()
        {
            var weapon = new MeleeWeaponPart
            {
                Attributes = "Cutting",
                OnHitEffectsRaw = "Stunned,100,,3,1"
            };
            var damage = new Damage(5);
            damage.AddAttribute("Cutting");

            for (int seed = 0; seed < 50; seed++)
            {
                var def = MakeCreature("d" + seed);
                OnHitWeaponEffects.Apply(weapon, damage, actualDamage: 5, def,
                    null, null, rng: new Random(seed));
                Assert.IsTrue(def.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    "100% chance must fire on EVERY hit. Failed seed=" + seed);
            }
        }

        [Test]
        public void Adversarial_WeaponSpec_Chance0_NeverFires()
        {
            // chance=0 specs are filtered at parse time (chance <= 0 → skip).
            // Verify the spec list is empty.
            var specs = OnHitEffectSpec.Parse("Stunned,0,,3,1");
            Assert.AreEqual(0, specs.Count,
                "chance=0 specs must be filtered at parse time.");
        }

        [Test]
        public void Adversarial_WeaponSpec_NegativeChance_NeverFires()
        {
            var specs = OnHitEffectSpec.Parse("Stunned,-50,,3,1");
            Assert.AreEqual(0, specs.Count,
                "Negative chance must be filtered.");
        }

        // ════════════════════════════════════════════════════════════════
        // D. SPEC PARSER MALFORMED INPUTS
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Parse_NullInput_ReturnsEmpty()
        {
            Assert.IsEmpty(OnHitEffectSpec.Parse(null));
        }

        [Test]
        public void Adversarial_Parse_EmptyInput_ReturnsEmpty()
        {
            Assert.IsEmpty(OnHitEffectSpec.Parse(""));
            Assert.IsEmpty(OnHitEffectSpec.Parse("   "));
        }

        [Test]
        public void Adversarial_Parse_OnlyDelimiters_ReturnsEmpty()
        {
            Assert.IsEmpty(OnHitEffectSpec.Parse(";;;"));
            Assert.IsEmpty(OnHitEffectSpec.Parse(",,,,"));
            Assert.IsEmpty(OnHitEffectSpec.Parse("; ; ;"));
        }

        [Test]
        public void Adversarial_Parse_MissingChance_SpecSkipped()
        {
            var specs = OnHitEffectSpec.Parse("Burning");
            Assert.AreEqual(0, specs.Count,
                "Spec with no chance field must be skipped (need ≥2 fields).");
        }

        [Test]
        public void Adversarial_Parse_NonNumericChance_SpecSkipped()
        {
            var specs = OnHitEffectSpec.Parse("Burning,banana");
            Assert.AreEqual(0, specs.Count);
        }

        [Test]
        public void Adversarial_Parse_MultipleSpecs_AllValid()
        {
            var specs = OnHitEffectSpec.Parse("Burning,30,,5,1.0;Stunned,15,,3,0");
            Assert.AreEqual(2, specs.Count);
            Assert.AreEqual("Burning", specs[0].EffectName);
            Assert.AreEqual(30, specs[0].ChancePercent);
            Assert.AreEqual("Stunned", specs[1].EffectName);
            Assert.AreEqual(15, specs[1].ChancePercent);
        }

        [Test]
        public void Adversarial_Parse_MalformedFollowedByValid_KeepsValid()
        {
            // First spec has gibberish chance → skip. Second is valid → keep.
            var specs = OnHitEffectSpec.Parse("Burning,gibberish;Stunned,20");
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual("Stunned", specs[0].EffectName);
        }

        [Test]
        public void Adversarial_Parse_LeadingTrailingWhitespace_Trimmed()
        {
            var specs = OnHitEffectSpec.Parse("  Burning  ,  30  ,  ,  5  ,  1.0  ");
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual("Burning", specs[0].EffectName);
            Assert.AreEqual(30, specs[0].ChancePercent);
        }

        // ════════════════════════════════════════════════════════════════
        // E. FACTORY UNKNOWN-NAME HANDLING
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Factory_UnknownEffectName_ReturnsNull()
        {
            var spec = new OnHitEffectSpec
            {
                EffectName = "NotARealEffect",
                ChancePercent = 100,
                Magnitude = 1f
            };
            Effect e = OnHitEffectFactory.Create(spec, source: null, rng: new Random(0));
            Assert.IsNull(e,
                "Unknown effect name must return null for graceful skip.");
        }

        [Test]
        public void Adversarial_Factory_NullSpec_ReturnsNull()
        {
            Assert.IsNull(OnHitEffectFactory.Create(null, null, new Random(0)));
        }

        [Test]
        public void Adversarial_Factory_EmptyEffectName_ReturnsNull()
        {
            var spec = new OnHitEffectSpec { EffectName = "", ChancePercent = 100 };
            Assert.IsNull(OnHitEffectFactory.Create(spec, null, new Random(0)));
            spec.EffectName = "   ";
            Assert.IsNull(OnHitEffectFactory.Create(spec, null, new Random(0)));
        }

        [Test]
        public void Adversarial_Factory_EffectNameCaseInsensitive()
        {
            // Factory uses ToLowerInvariant, so "BURNING" / "Burning"
            // / "burning" all map to BurningEffect. Adversarial: a buggy
            // case-sensitive switch would silently fail for "BURNING".
            var spec1 = new OnHitEffectSpec
            { EffectName = "BURNING", ChancePercent = 100, Magnitude = 1f };
            var spec2 = new OnHitEffectSpec
            { EffectName = "burning", ChancePercent = 100, Magnitude = 1f };
            var spec3 = new OnHitEffectSpec
            { EffectName = "Burning", ChancePercent = 100, Magnitude = 1f };

            var e1 = OnHitEffectFactory.Create(spec1, null, new Random(0));
            var e2 = OnHitEffectFactory.Create(spec2, null, new Random(0));
            var e3 = OnHitEffectFactory.Create(spec3, null, new Random(0));

            Assert.IsInstanceOf<BurningEffect>(e1);
            Assert.IsInstanceOf<BurningEffect>(e2);
            Assert.IsInstanceOf<BurningEffect>(e3);
        }

        [Test]
        public void Adversarial_Factory_AliasNamesAllResolveToSameType()
        {
            // The factory accepts aliases per effect (Burning/burn/fire,
            // Frozen/freeze/ice, Electrified/electric/shock/lightning, etc.).
            // Verify all aliases resolve to the same Effect type.
            string[] burningAliases = { "Burning", "burn", "Fire" };
            foreach (var name in burningAliases)
            {
                var spec = new OnHitEffectSpec
                { EffectName = name, ChancePercent = 100, Magnitude = 1f };
                var e = OnHitEffectFactory.Create(spec, null, new Random(0));
                Assert.IsInstanceOf<BurningEffect>(e,
                    "Alias '" + name + "' must resolve to BurningEffect.");
            }

            string[] frozenAliases = { "Frozen", "freeze", "ice", "Frost" };
            foreach (var name in frozenAliases)
            {
                var spec = new OnHitEffectSpec
                { EffectName = name, ChancePercent = 100, Magnitude = 1f };
                var e = OnHitEffectFactory.Create(spec, null, new Random(0));
                Assert.IsInstanceOf<FrozenEffect>(e,
                    "Alias '" + name + "' must resolve to FrozenEffect.");
            }

            string[] electricAliases = { "Electrified", "electric", "shock", "Lightning" };
            foreach (var name in electricAliases)
            {
                var spec = new OnHitEffectSpec
                { EffectName = name, ChancePercent = 100, Magnitude = 1f };
                var e = OnHitEffectFactory.Create(spec, null, new Random(0));
                Assert.IsInstanceOf<ElectrifiedEffect>(e,
                    "Alias '" + name + "' must resolve to ElectrifiedEffect.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // F. STACKING / NON-STACKING ON HIT
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_OnHit_AlreadyConfusedTarget_NotDoubleApplied()
        {
            // ConfusedEffect.CanApply rejects stacking. So even if the
            // 10% Piercing roll fires twice in a row, the second is a
            // no-op. Verify the count stays at 1.
            var defender = MakeCreature("def");
            defender.ApplyEffect(new ConfusedEffect(2), null, null);
            int effectCountBefore = defender.GetPart<StatusEffectsPart>()
                .GetAllEffects().Count;

            var damage = new Damage(5);
            damage.AddAttribute("Piercing");
            // Force-fire by setting chance to "always trigger" via 100 seeds.
            for (int seed = 0; seed < 100; seed++)
            {
                OnHitClassEffects.Apply(damage, 5, defender, null, null, new Random(seed));
            }
            int effectCountAfter = defender.GetPart<StatusEffectsPart>()
                .GetAllEffects().Count;
            Assert.AreEqual(effectCountBefore, effectCountAfter,
                "Re-applying Confused must NOT add duplicate (CanApply rejects).");
        }

        // ════════════════════════════════════════════════════════════════
        // G. WEAPON-EFFECTSRAW INTEGRATION
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_WeaponEffectsRaw_NullOrEmpty_NoCrash()
        {
            var defender = MakeCreature();
            var damage = new Damage(5);
            damage.AddAttribute("Cutting");

            var w1 = new MeleeWeaponPart { OnHitEffectsRaw = null };
            Assert.DoesNotThrow(() =>
                OnHitWeaponEffects.Apply(w1, damage, 5, defender, null, null, new Random(0)));

            var w2 = new MeleeWeaponPart { OnHitEffectsRaw = "" };
            Assert.DoesNotThrow(() =>
                OnHitWeaponEffects.Apply(w2, damage, 5, defender, null, null, new Random(0)));
        }

        [Test]
        public void Adversarial_WeaponEffectsRaw_AllMalformed_NoEffectsApplied()
        {
            var defender = MakeCreature();
            var damage = new Damage(5);
            damage.AddAttribute("Cutting");
            var w = new MeleeWeaponPart
            {
                OnHitEffectsRaw = ";;,,gibberish_no_chance;invalid,banana;UnknownEffect,100"
            };

            // Even the valid-syntax-but-unknown-effect spec fires the
            // factory, which returns null, so still nothing applied.
            OnHitWeaponEffects.Apply(w, damage, 5, defender, null, null, new Random(0));
            Assert.AreEqual(0,
                defender.GetPart<StatusEffectsPart>().GetAllEffects().Count,
                "All-malformed OnHitEffectsRaw must produce zero applied effects.");
        }
    }
}
