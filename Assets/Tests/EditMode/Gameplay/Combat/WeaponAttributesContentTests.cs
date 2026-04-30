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
        // FlamingSword — first elemental-carrier weapon
        // (Tier-1 Quick Win; ties Phase C Attributes → Phase E HeatResistance)
        // ====================================================================

        [Test]
        public void FlamingSword_HasCuttingFireLongBladesAttribute()
        {
            var sword = _harness.Factory.CreateEntity("FlamingSword");
            Assert.IsNotNull(sword, "FlamingSword blueprint must exist");
            var weapon = sword.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "FlamingSword must have a MeleeWeaponPart");
            Assert.AreEqual("Cutting Fire LongBlades", weapon.Attributes,
                "FlamingSword.Attributes should declare 'Cutting Fire LongBlades' " +
                "so it carries both the physical class (Cutting/LongBlades) and the " +
                "elemental type (Fire) into the Damage object on hit");
        }

        [Test]
        public void FlamingSword_AttributesContain_Fire()
        {
            // Pinned separately from the exact-string test so a future reorder
            // (e.g., "Fire Cutting LongBlades") doesn't silently strip Fire and
            // turn this into a plain cutter.
            var sword = _harness.Factory.CreateEntity("FlamingSword");
            var weapon = sword.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.IsTrue(weapon.Attributes.Contains("Fire"),
                "FlamingSword must contain 'Fire' in its Attributes — that's what " +
                "routes its damage through HeatResistance (Phase E) on heat-resistant " +
                "creatures like Glowmaw");
        }

        [Test]
        public void FlamingSword_DoesNotHavePiercing_OrBludgeoning()
        {
            // Counter-check: physical-class is Cutting, not the others.
            var sword = _harness.Factory.CreateEntity("FlamingSword");
            var weapon = sword.GetPart<MeleeWeaponPart>();
            Assert.IsFalse(weapon.Attributes.Contains("Piercing"),
                "FlamingSword is Cutting, not Piercing");
            Assert.IsFalse(weapon.Attributes.Contains("Bludgeoning"),
                "FlamingSword is Cutting, not Bludgeoning");
        }

        // ====================================================================
        // IceSword — Cold counterpart to FlamingSword
        // (Tier-1 Quick Win; ties Phase C Attributes → Phase E ColdResistance)
        // ====================================================================

        [Test]
        public void IceSword_HasCuttingIceLongBladesAttribute()
        {
            var sword = _harness.Factory.CreateEntity("IceSword");
            Assert.IsNotNull(sword, "IceSword blueprint must exist");
            var weapon = sword.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "IceSword must have a MeleeWeaponPart");
            Assert.AreEqual("Cutting Ice LongBlades", weapon.Attributes,
                "IceSword.Attributes should declare 'Cutting Ice LongBlades' " +
                "so it carries both the physical class (Cutting/LongBlades) " +
                "and the elemental type (Ice) into the Damage object on hit");
        }

        [Test]
        public void IceSword_AttributesContain_Ice()
        {
            // Pinned separately from the exact-string test so a future reorder
            // (e.g., "Ice Cutting LongBlades") doesn't silently strip Ice and
            // turn this into a plain cutter.
            var sword = _harness.Factory.CreateEntity("IceSword");
            var weapon = sword.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.IsTrue(weapon.Attributes.Contains("Ice"),
                "IceSword must contain 'Ice' in its Attributes — that's what " +
                "routes its damage through ColdResistance (Phase E) on " +
                "cold-resistant creatures like SnapjawHunter");
        }

        [Test]
        public void IceSword_DoesNotHavePiercing_OrBludgeoning()
        {
            // Counter-check: physical-class is Cutting, not the others.
            var sword = _harness.Factory.CreateEntity("IceSword");
            var weapon = sword.GetPart<MeleeWeaponPart>();
            Assert.IsFalse(weapon.Attributes.Contains("Piercing"),
                "IceSword is Cutting, not Piercing");
            Assert.IsFalse(weapon.Attributes.Contains("Bludgeoning"),
                "IceSword is Cutting, not Bludgeoning");
        }

        // ====================================================================
        // ThunderHammer — Lightning counterpart, Bludgeoning class
        // (Tier-1 Quick Win; ties Phase C Attributes → Phase E ElectricResistance)
        // ====================================================================

        [Test]
        public void ThunderHammer_HasBludgeoningLightningCudgelAttribute()
        {
            var hammer = _harness.Factory.CreateEntity("ThunderHammer");
            Assert.IsNotNull(hammer, "ThunderHammer blueprint must exist");
            var weapon = hammer.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "ThunderHammer must have a MeleeWeaponPart");
            Assert.AreEqual("Bludgeoning Lightning Cudgel", weapon.Attributes,
                "ThunderHammer.Attributes should declare 'Bludgeoning Lightning Cudgel' " +
                "so it carries both the physical class (Bludgeoning/Cudgel) and the " +
                "elemental type (Lightning) into the Damage object on hit");
        }

        [Test]
        public void ThunderHammer_AttributesContain_Lightning()
        {
            // Pinned separately from the exact-string test so a future reorder
            // doesn't silently strip Lightning and turn this into a plain hammer.
            var hammer = _harness.Factory.CreateEntity("ThunderHammer");
            var weapon = hammer.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.IsTrue(weapon.Attributes.Contains("Lightning"),
                "ThunderHammer must contain 'Lightning' in its Attributes — that's " +
                "what routes its damage through ElectricResistance (Phase E) on " +
                "creatures with conductive- or insulating-tagged stats");
        }

        [Test]
        public void ThunderHammer_DoesNotHaveCutting_OrPiercing()
        {
            // Counter-check: physical-class is Bludgeoning, not the others.
            var hammer = _harness.Factory.CreateEntity("ThunderHammer");
            var weapon = hammer.GetPart<MeleeWeaponPart>();
            Assert.IsFalse(weapon.Attributes.Contains("Cutting"),
                "ThunderHammer is Bludgeoning, not Cutting");
            Assert.IsFalse(weapon.Attributes.Contains("Piercing"),
                "ThunderHammer is Bludgeoning, not Piercing");
        }

        // ====================================================================
        // AcidicDagger — Acid counterpart, Piercing class
        // (Tier-1 Quick Win; ties Phase C Attributes → Phase E AcidResistance)
        // ====================================================================

        [Test]
        public void AcidicDagger_HasPiercingAcidAttribute()
        {
            var dagger = _harness.Factory.CreateEntity("AcidicDagger");
            Assert.IsNotNull(dagger, "AcidicDagger blueprint must exist");
            var weapon = dagger.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "AcidicDagger must have a MeleeWeaponPart");
            Assert.AreEqual("Piercing Acid", weapon.Attributes,
                "AcidicDagger.Attributes should declare 'Piercing Acid' so it " +
                "carries both the physical class (Piercing) and the elemental " +
                "type (Acid) into the Damage object on hit");
        }

        [Test]
        public void AcidicDagger_AttributesContain_Acid()
        {
            // Pinned separately so a future reorder doesn't silently strip
            // Acid and turn this into a plain piercer.
            var dagger = _harness.Factory.CreateEntity("AcidicDagger");
            var weapon = dagger.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.IsTrue(weapon.Attributes.Contains("Acid"),
                "AcidicDagger must contain 'Acid' in its Attributes — that's " +
                "what routes its damage through AcidResistance (Phase E) on " +
                "creatures with chemical-reactive- or chemical-inert-tagged stats");
        }

        [Test]
        public void AcidicDagger_DoesNotHaveCutting_OrBludgeoning()
        {
            // Counter-check: physical-class is Piercing, not the others.
            var dagger = _harness.Factory.CreateEntity("AcidicDagger");
            var weapon = dagger.GetPart<MeleeWeaponPart>();
            Assert.IsFalse(weapon.Attributes.Contains("Cutting"),
                "AcidicDagger is Piercing, not Cutting");
            Assert.IsFalse(weapon.Attributes.Contains("Bludgeoning"),
                "AcidicDagger is Piercing, not Bludgeoning");
        }

        // ====================================================================
        // CryoLance — first Piercing-class elemental longblade
        // (Tier-1 Quick Win; ties Phase C Attributes → Phase E ColdResistance
        // through a Piercing weapon, distinct from IceSword's Cutting variant)
        // ====================================================================

        [Test]
        public void CryoLance_HasPiercingIceLongBladesAttribute()
        {
            var lance = _harness.Factory.CreateEntity("CryoLance");
            Assert.IsNotNull(lance, "CryoLance blueprint must exist");
            var weapon = lance.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "CryoLance must have a MeleeWeaponPart");
            Assert.AreEqual("Piercing Ice LongBlades", weapon.Attributes,
                "CryoLance.Attributes should declare 'Piercing Ice LongBlades' " +
                "so it carries both the physical class (Piercing/LongBlades) " +
                "and the elemental type (Ice) into the Damage object on hit");
        }

        // ====================================================================
        // Backfill: every existing melee weapon declares Attributes
        // (Tier-1 cleanup; closes the gap where most non-elemental weapons
        // produced bare [Melee, Strength]-tagged damage with no physical-class
        // info. Each test pins the exact Attributes string so a future
        // refactor can't silently drop them.)
        // ====================================================================

        [Test]
        public void LongSword_HasCuttingLongBladesAttribute()
        {
            var w = _harness.Factory.CreateEntity("LongSword").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting LongBlades", w.Attributes);
        }

        [Test]
        public void Battleaxe_HasCuttingAxeAttribute()
        {
            var w = _harness.Factory.CreateEntity("Battleaxe").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting Axe", w.Attributes);
        }

        [Test]
        public void Greatsword_HasCuttingLongBladesAttribute()
        {
            var w = _harness.Factory.CreateEntity("Greatsword").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting LongBlades", w.Attributes);
        }

        [Test]
        public void Mace_HasBludgeoningCudgelAttribute()
        {
            var w = _harness.Factory.CreateEntity("Mace").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Bludgeoning Cudgel", w.Attributes);
        }

        [Test]
        public void Spear_HasPiercingAttribute()
        {
            var w = _harness.Factory.CreateEntity("Spear").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Piercing", w.Attributes);
        }

        [Test]
        public void Hatchet_HasCuttingAxeAttribute()
        {
            var w = _harness.Factory.CreateEntity("Hatchet").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting Axe", w.Attributes);
        }

        [Test]
        public void Claymore_HasCuttingLongBladesAttribute()
        {
            var w = _harness.Factory.CreateEntity("Claymore").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting LongBlades", w.Attributes);
        }

        [Test]
        public void Sporeblade_HasCuttingLongBladesAttribute()
        {
            var w = _harness.Factory.CreateEntity("Sporeblade").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting LongBlades", w.Attributes);
        }

        [Test]
        public void EchoKnife_HasCuttingSonicAttribute()
        {
            // Sonic is currently a descriptive attribute — no IsSonicDamage()
            // helper exists yet. Promotes the existing Sonic material tag to
            // a Damage attribute so future Sonic-resistance content (or a
            // SonicVulnerable Glass-shattering creature) has signal to read.
            var w = _harness.Factory.CreateEntity("EchoKnife").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting Sonic", w.Attributes);
        }

        [Test]
        public void ChoirSpine_HasPiercingAttribute()
        {
            var w = _harness.Factory.CreateEntity("ChoirSpine").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Piercing", w.Attributes);
        }

        [Test]
        public void OldWorldPipe_HasBludgeoningCudgelAttribute()
        {
            var w = _harness.Factory.CreateEntity("OldWorldPipe").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Bludgeoning Cudgel", w.Attributes);
        }

        [Test]
        public void TemporalShard_HasPiercingAttribute()
        {
            var w = _harness.Factory.CreateEntity("TemporalShard").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Piercing", w.Attributes);
        }

        [Test]
        public void SeveranceEdge_HasCuttingLongBladesAttribute()
        {
            var w = _harness.Factory.CreateEntity("SeveranceEdge").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting LongBlades", w.Attributes);
        }

        [Test]
        public void GlassblownStiletto_HasPiercingAttribute()
        {
            var w = _harness.Factory.CreateEntity("GlassblownStiletto").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Piercing", w.Attributes);
        }

        [Test]
        public void DissolutionMaul_HasBludgeoningCudgelAcidAttribute()
        {
            // BONUS: DissolutionMaul carries Acid because its material tag is
            // Corrosive. This makes it route through AcidResistance — a
            // SECOND Acid-routing weapon alongside AcidicDagger. The
            // integration assertion is in DissolutionMaul_AcidAttribute_*
            // (in DissolutionMaulContentTests below).
            var w = _harness.Factory.CreateEntity("DissolutionMaul").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Bludgeoning Cudgel Acid", w.Attributes);
        }

        [Test]
        public void FirstRootGlaive_HasCuttingGlaiveAttribute()
        {
            // Glaive is a new sub-class — no other glaives currently. Future
            // polearm content can hook on this descriptor.
            var w = _harness.Factory.CreateEntity("FirstRootGlaive").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting Glaive", w.Attributes);
        }

        [Test]
        public void PalimpsestBlade_HasCuttingLongBladesAttribute()
        {
            var w = _harness.Factory.CreateEntity("PalimpsestBlade").GetPart<MeleeWeaponPart>();
            Assert.AreEqual("Cutting LongBlades", w.Attributes);
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
