using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Per-weapon on-hit effect tests:
    ///
    ///   1. The 5 elemental weapons load with the expected OnHitEffectsRaw
    ///      string from JSON (blueprint-shape pins).
    ///   2. Parser produces well-formed OnHitEffectSpec entries.
    ///   3. Integration: a synthetic hit through OnHitWeaponEffects.Apply
    ///      eventually fires the expected status effect across many seeds.
    ///   4. Counter-check: a non-elemental weapon (Mace) has no OnHitEffectsRaw
    ///      and so applies nothing through the per-weapon hook.
    ///   5. Adversarial: malformed strings parse to zero specs, no crash.
    /// </summary>
    public class OnHitWeaponEffectsTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _harness?.Dispose();
            _harness = null;
        }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // 1. Blueprint-shape: each elemental weapon declares its OnHitEffectsRaw
        // ====================================================================

        [Test]
        public void FlamingSword_HasOnHitBurning()
        {
            var w = _harness.Factory.CreateEntity("FlamingSword").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Burning,30,,5,1.0", w.OnHitEffectsRaw);
        }

        [Test]
        public void IceSword_HasOnHitFrozen()
        {
            var w = _harness.Factory.CreateEntity("IceSword").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Frozen,30,,3,1.0", w.OnHitEffectsRaw);
        }

        [Test]
        public void ThunderHammer_HasOnHitElectrified()
        {
            var w = _harness.Factory.CreateEntity("ThunderHammer").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Electrified,30,,3,1.0", w.OnHitEffectsRaw);
        }

        [Test]
        public void AcidicDagger_HasOnHitAcidic()
        {
            var w = _harness.Factory.CreateEntity("AcidicDagger").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Acidic,30,,5,1.0", w.OnHitEffectsRaw);
        }

        [Test]
        public void DissolutionMaul_HasOnHitAcidicAtHigherMagnitude()
        {
            var w = _harness.Factory.CreateEntity("DissolutionMaul").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Acidic,40,,5,1.5", w.OnHitEffectsRaw);
        }

        [Test]
        public void Mace_HasNoOnHitEffectsRaw()
        {
            // Counter-check: base non-elemental weapons declare no per-weapon
            // overrides — they get only the Bludgeoning class hook.
            var w = _harness.Factory.CreateEntity("Mace").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("", w.OnHitEffectsRaw);
        }

        // ====================================================================
        // 2. Parser well-formedness
        // ====================================================================

        [Test]
        public void Parser_FlamingSword_ProducesOneBurningSpec()
        {
            var w = _harness.Factory.CreateEntity("FlamingSword").GetPart<MeleeWeaponPart>();
            var specs = OnHitEffectSpec.Parse(w.OnHitEffectsRaw);
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual("Burning", specs[0].EffectName);
            Assert.AreEqual(30, specs[0].ChancePercent);
            Assert.AreEqual(5, specs[0].DurationTurns);
            Assert.AreEqual(1.0f, specs[0].Magnitude);
        }

        [Test]
        public void Parser_TwoEffects_SemicolonSeparated()
        {
            var specs = OnHitEffectSpec.Parse("Burning,30,,5,1.0;Stunned,15,,1,0");
            Assert.AreEqual(2, specs.Count);
            Assert.AreEqual("Burning", specs[0].EffectName);
            Assert.AreEqual("Stunned", specs[1].EffectName);
            Assert.AreEqual(15, specs[1].ChancePercent);
        }

        // ====================================================================
        // 3. Integration: per-weapon effect fires under chance roll
        // ====================================================================

        [Test]
        public void FlamingSword_OnHit_AppliesBurning_AcrossSeeds()
        {
            var w = _harness.Factory.CreateEntity("FlamingSword").GetPart<MeleeWeaponPart>();
            bool observed = false;
            for (int seed = 0; seed < 200 && !observed; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttributes("Cutting Fire LongBlades");
                OnHitWeaponEffects.Apply(w, damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                if (defender.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                "Across 200 seeds, FlamingSword's OnHitEffectsRaw should produce " +
                "BurningEffect at least once.");
        }

        [Test]
        public void IceSword_OnHit_AppliesFrozen_AcrossSeeds()
        {
            var w = _harness.Factory.CreateEntity("IceSword").GetPart<MeleeWeaponPart>();
            bool observed = false;
            for (int seed = 0; seed < 200 && !observed; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttributes("Cutting Ice LongBlades");
                OnHitWeaponEffects.Apply(w, damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                if (defender.GetPart<StatusEffectsPart>().HasEffect<FrozenEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed);
        }

        [Test]
        public void ThunderHammer_OnHit_AppliesElectrified_AcrossSeeds()
        {
            var w = _harness.Factory.CreateEntity("ThunderHammer").GetPart<MeleeWeaponPart>();
            bool observed = false;
            for (int seed = 0; seed < 200 && !observed; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttributes("Bludgeoning Lightning Cudgel");
                OnHitWeaponEffects.Apply(w, damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                if (defender.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed);
        }

        [Test]
        public void AcidicDagger_OnHit_AppliesAcidic_AcrossSeeds()
        {
            var w = _harness.Factory.CreateEntity("AcidicDagger").GetPart<MeleeWeaponPart>();
            bool observed = false;
            for (int seed = 0; seed < 200 && !observed; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttributes("Piercing Acid");
                OnHitWeaponEffects.Apply(w, damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                if (defender.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed);
        }

        // ====================================================================
        // 4. Counter-check: weapon with no OnHitEffectsRaw applies nothing
        // ====================================================================

        [Test]
        public void Mace_OnHit_AppliesNothing_FromPerWeaponHook()
        {
            var w = _harness.Factory.CreateEntity("Mace").GetPart<MeleeWeaponPart>();
            for (int seed = 0; seed < 200; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttributes("Bludgeoning Cudgel");
                OnHitWeaponEffects.Apply(w, damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                // The per-weapon hook fires zero specs (empty OnHitEffectsRaw),
                // so no status effects should be applied here. (The class hook
                // — Bludgeoning → Stunned — runs separately and is tested in
                // OnHitClassEffectsTests.)
                var sep = defender.GetPart<StatusEffectsPart>();
                Assert.IsFalse(sep.HasEffect<BurningEffect>());
                Assert.IsFalse(sep.HasEffect<FrozenEffect>());
                Assert.IsFalse(sep.HasEffect<ElectrifiedEffect>());
                Assert.IsFalse(sep.HasEffect<AcidicEffect>());
            }
        }

        // ====================================================================
        // 5. Adversarial: malformed strings, edge cases
        // ====================================================================

        [Test]
        public void Parser_EmptyString_ZeroSpecs()
        {
            Assert.AreEqual(0, OnHitEffectSpec.Parse("").Count);
            Assert.AreEqual(0, OnHitEffectSpec.Parse(null).Count);
            Assert.AreEqual(0, OnHitEffectSpec.Parse("   ").Count);
        }

        [Test]
        public void Parser_MalformedString_NoCrash()
        {
            // Each malformed input should parse to zero or some valid specs;
            // the parser must NOT throw.
            Assert.DoesNotThrow(() => OnHitEffectSpec.Parse("garbage"));
            Assert.DoesNotThrow(() => OnHitEffectSpec.Parse(",,,"));
            Assert.DoesNotThrow(() => OnHitEffectSpec.Parse(";;;"));
            Assert.DoesNotThrow(() => OnHitEffectSpec.Parse("Burning"));
            Assert.DoesNotThrow(() => OnHitEffectSpec.Parse("Burning,not-a-number"));
            Assert.DoesNotThrow(() => OnHitEffectSpec.Parse("Burning,30,1d2,not-a-duration,1.0"));
        }

        [Test]
        public void Parser_ZeroChance_SkipsSpec()
        {
            // 0% chance is a no-op spec; parser drops it.
            var specs = OnHitEffectSpec.Parse("Burning,0,,5,1.0;Frozen,30,,3,1.0");
            Assert.AreEqual(1, specs.Count);
            Assert.AreEqual("Frozen", specs[0].EffectName);
        }

        [Test]
        public void OnHitWeaponEffects_ZeroDamage_NoOp()
        {
            var w = _harness.Factory.CreateEntity("FlamingSword").GetPart<MeleeWeaponPart>();
            for (int seed = 0; seed < 200; seed++)
            {
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttributes("Cutting Fire LongBlades");
                OnHitWeaponEffects.Apply(w, damage, actualDamage: 0, defender,
                    attacker: null, zone: null, rng: new Random(seed));
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                    $"Seed {seed}: zero-damage hit should not apply per-weapon Burning.");
            }
        }

        [Test]
        public void OnHitWeaponEffects_NullWeapon_NoCrash()
        {
            var defender = MakeFighter();
            var damage = new Damage(10);
            Assert.DoesNotThrow(() =>
                OnHitWeaponEffects.Apply(weapon: null, damage, actualDamage: 10, defender,
                    attacker: null, zone: null, rng: new Random(0)));
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
            entity.AddPart(new RenderPart { DisplayName = "fighter" });
            entity.AddPart(new StatusEffectsPart());
            return entity;
        }
    }
}
