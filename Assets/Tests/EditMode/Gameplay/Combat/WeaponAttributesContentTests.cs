using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tier 2.2 — Weapon damage attributes shipped on a representative
    /// subset of weapons (both blueprint-loaded and natural).
    ///
    /// User-visible invariant: "When a weapon strikes, its declared
    /// `Attributes` propagate into the resulting <see cref="Damage"/>'s
    /// attribute list, in addition to the always-on `Melee` and the
    /// weapon's `Stat` name. A `ShortSword` swing produces damage
    /// tagged Cutting; a `Cudgel` swing produces damage tagged
    /// Bludgeoning."
    ///
    /// Coverage targets (representative cross-section, not exhaustive):
    /// Blueprint-loaded:
    ///   - Dagger          → "Piercing"
    ///   - ShortSword      → "Cutting LongBlades"
    ///   - Cudgel          → "Bludgeoning"
    ///   - Warhammer       → "Bludgeoning Cudgel"
    /// Natural (NaturalWeaponFactory):
    ///   - DefaultFist     → "Bludgeoning Unarmed"
    ///   - SnapjawClaw     → "Cutting Animal"
    /// </summary>
    public class WeaponAttributesContentTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _harness = new ScenarioTestHarness();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _harness?.Dispose();
            _harness = null;
        }

        // ====================================================================
        // Blueprint-loaded weapons (read from real Objects.json)
        // ====================================================================

        [Test]
        public void Dagger_HasPiercingAttribute()
        {
            var dagger = _harness.Factory.CreateEntity("Dagger");
            var weapon = dagger.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "Dagger must have a MeleeWeaponPart");
            Assert.AreEqual("Piercing", weapon.Attributes,
                "Dagger.Attributes should declare 'Piercing'");
        }

        [Test]
        public void ShortSword_HasCuttingLongBladesAttribute()
        {
            var sword = _harness.Factory.CreateEntity("ShortSword");
            var weapon = sword.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.AreEqual("Cutting LongBlades", weapon.Attributes);
        }

        [Test]
        public void Cudgel_HasBludgeoningAttribute()
        {
            var cudgel = _harness.Factory.CreateEntity("Cudgel");
            var weapon = cudgel.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.AreEqual("Bludgeoning", weapon.Attributes);
        }

        [Test]
        public void Warhammer_HasBludgeoningCudgelAttribute()
        {
            var hammer = _harness.Factory.CreateEntity("Warhammer");
            var weapon = hammer.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.AreEqual("Bludgeoning Cudgel", weapon.Attributes);
        }

        // ====================================================================
        // Counter-checks: attributes don't leak across categories
        // ====================================================================

        [Test]
        public void Dagger_DoesNotHaveBludgeoning_OrCutting()
        {
            var dagger = _harness.Factory.CreateEntity("Dagger");
            var weapon = dagger.GetPart<MeleeWeaponPart>();
            Assert.IsFalse(weapon.Attributes.Contains("Bludgeoning"),
                "Dagger is Piercing, not Bludgeoning");
            Assert.IsFalse(weapon.Attributes.Contains("Cutting"),
                "Dagger is Piercing, not Cutting");
        }

        [Test]
        public void Cudgel_DoesNotHavePiercing_OrCutting()
        {
            var cudgel = _harness.Factory.CreateEntity("Cudgel");
            var weapon = cudgel.GetPart<MeleeWeaponPart>();
            Assert.IsFalse(weapon.Attributes.Contains("Piercing"));
            Assert.IsFalse(weapon.Attributes.Contains("Cutting"));
        }

        // ====================================================================
        // Natural weapons (NaturalWeaponFactory)
        // ====================================================================

        [Test]
        public void DefaultFist_HasBludgeoningUnarmedAttribute()
        {
            var fist = NaturalWeaponFactory.Create("DefaultFist");
            var weapon = fist.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.AreEqual("Bludgeoning Unarmed", weapon.Attributes);
        }

        [Test]
        public void SnapjawClaw_HasCuttingAnimalAttribute()
        {
            var claw = NaturalWeaponFactory.Create("SnapjawClaw");
            var weapon = claw.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.AreEqual("Cutting Animal", weapon.Attributes);
        }

        // Note: full Attributes-propagate-to-Damage integration is already
        // covered by `CombatDamageIntegrationTests` (Phase C) using synthetic
        // entities. T2.2 unit tests above prove the JSON / NaturalWeaponFactory
        // half (the new part). Together the chain is verified end-to-end.
    }
}
