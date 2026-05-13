using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.3.3 — <see cref="EnhancementGlowQuartz"/>
    /// contract pin.
    ///
    /// <para><b>What's pinned:</b></para>
    /// <list type="bullet">
    ///   <item>Applicable: requires <see cref="EquippablePart"/>;
    ///         rejects non-equippable items.</item>
    ///   <item>TierConfigure: RadiusBonus = Tier * 1.</item>
    ///   <item>OnEquipped: item gains a LightSourcePart (or its
    ///         existing one is augmented) with +RadiusBonus.</item>
    ///   <item>OnUnequipped: LightSourcePart radius restored.</item>
    ///   <item>Atomicity: double-equip idempotent; unequip-without-
    ///         equip is a no-op.</item>
    ///   <item>Save/load preserves Tier + RadiusBonus + AppliedBonus.</item>
    /// </list>
    /// </summary>
    public class EnhancementGlowQuartzTests
    {
        [SetUp]
        public void Setup()
        {
            Diag.ResetAll();
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(EnhancementGlowQuartz));
        }

        private static Entity MakeWeapon()
        {
            var e = new Entity { ID = "longsword", BlueprintName = "longsword" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "longsword" });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d6" });
            e.AddPart(new EquippablePart { Slot = "MainHand" });
            return e;
        }

        private static Entity MakeWeaponWithLight(int baseRadius = 4)
        {
            var e = MakeWeapon();
            e.AddPart(new LightSourcePart { Radius = baseRadius, LightColor = "&R" });
            return e;
        }

        private static Entity MakeNonEquippable()
        {
            var e = new Entity { ID = "tonic", BlueprintName = "tonic" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "tonic" });
            e.AddPart(new PhysicsPart { Takeable = true });
            return e;
        }

        private static Entity MakeActor() =>
            new Entity { ID = "hero", BlueprintName = "hero" };

        // ── Applicable ────────────────────────────────────────────

        [Test]
        public void Applicable_EquippableWeapon_True()
        {
            Assert.IsTrue(new EnhancementGlowQuartz().Applicable(MakeWeapon()));
        }

        [Test]
        public void Applicable_NonEquippableItem_False()
        {
            // Counter-check pair to Applicable_EquippableWeapon_True.
            Assert.IsFalse(new EnhancementGlowQuartz().Applicable(MakeNonEquippable()));
        }

        [Test]
        public void Applicable_NullItem_False()
        {
            Assert.IsFalse(new EnhancementGlowQuartz().Applicable(null));
        }

        // ── Tier scaling ──────────────────────────────────────────

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        public void TierConfigure_ScalesRadiusLinearly(int tier, int expected)
        {
            var enh = new EnhancementGlowQuartz();
            enh.ApplyTier(tier);
            Assert.AreEqual(expected, enh.RadiusBonus);
        }

        // ── OnEquipped — adds LightSourcePart if missing ──────────

        [Test]
        public void OnEquipped_OnWeaponWithoutLight_CreatesLightSourcePart()
        {
            var weapon = MakeWeapon();
            Assert.IsNull(weapon.GetPart<LightSourcePart>(),
                "Precondition: weapon has no LightSourcePart.");

            var enh = new EnhancementGlowQuartz();
            enh.ApplyTier(2);  // +2 radius
            weapon.AddPart(enh);

            enh.OnEquipped(MakeActor(), weapon);

            var light = weapon.GetPart<LightSourcePart>();
            Assert.IsNotNull(light, "OnEquipped creates LightSourcePart.");
            Assert.AreEqual(2, light.Radius);
            Assert.IsTrue(enh.AppliedBonus);
        }

        [Test]
        public void OnEquipped_OnWeaponWithExistingLight_ExtendsRadius()
        {
            var weapon = MakeWeaponWithLight(baseRadius: 4);
            var enh = new EnhancementGlowQuartz();
            enh.ApplyTier(3);  // +3 radius
            weapon.AddPart(enh);

            enh.OnEquipped(MakeActor(), weapon);

            Assert.AreEqual(7, weapon.GetPart<LightSourcePart>().Radius,
                "Existing radius 4 + GlowQuartz bonus 3 = 7.");
        }

        [Test]
        public void OnEquipped_EmitsBonusAppliedDiag()
        {
            var weapon = MakeWeapon();
            var enh = new EnhancementGlowQuartz();
            enh.ApplyTier(1);
            weapon.AddPart(enh);
            Diag.ResetAll();

            enh.OnEquipped(MakeActor(), weapon);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "BonusApplied",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("EnhancementGlowQuartz", recs[0].PayloadJson);
        }

        // ── OnUnequipped — symmetric inverse ──────────────────────

        [Test]
        public void OnUnequipped_AfterEquip_RestoresOriginalRadius()
        {
            var weapon = MakeWeaponWithLight(baseRadius: 4);
            var enh = new EnhancementGlowQuartz();
            enh.ApplyTier(2);
            weapon.AddPart(enh);

            enh.OnEquipped(MakeActor(), weapon);
            enh.OnUnequipped(MakeActor(), weapon);

            Assert.AreEqual(4, weapon.GetPart<LightSourcePart>().Radius,
                "Equip + unequip is net-zero on Radius.");
            Assert.IsFalse(enh.AppliedBonus);
        }

        [Test]
        public void OnUnequipped_WithoutPriorEquip_NoOp()
        {
            var weapon = MakeWeaponWithLight(baseRadius: 4);
            var enh = new EnhancementGlowQuartz();
            enh.ApplyTier(3);
            weapon.AddPart(enh);

            enh.OnUnequipped(MakeActor(), weapon);

            Assert.AreEqual(4, weapon.GetPart<LightSourcePart>().Radius,
                "Unequip without prior equip is a no-op.");
        }

        // ── Atomicity ────────────────────────────────────────────

        [Test]
        public void OnEquipped_CalledTwice_AppliesOnce()
        {
            var weapon = MakeWeaponWithLight(baseRadius: 4);
            var enh = new EnhancementGlowQuartz();
            enh.ApplyTier(2);
            weapon.AddPart(enh);

            enh.OnEquipped(MakeActor(), weapon);
            enh.OnEquipped(MakeActor(), weapon);  // duplicate

            Assert.AreEqual(6, weapon.GetPart<LightSourcePart>().Radius,
                "Second OnEquipped is a no-op — Radius stays at +2, not +4.");
        }

        [Test]
        public void OnEquipped_NullItem_NoCrash()
        {
            var enh = new EnhancementGlowQuartz();
            enh.ApplyTier(1);
            Assert.DoesNotThrow(() => enh.OnEquipped(MakeActor(), null));
            Assert.IsFalse(enh.AppliedBonus);
        }

        // ── Save/load round-trip ─────────────────────────────────

        [Test]
        public void RoundTrip_PreservesTierAndState()
        {
            var weapon = MakeWeapon();
            ItemEnhancing.Apply(weapon, nameof(EnhancementGlowQuartz), tier: 3);
            var src = weapon.GetPart<EnhancementGlowQuartz>();
            src.OnEquipped(MakeActor(), weapon);

            Entity loaded = PartRoundTripHelper.RoundTripEntity(weapon);

            var enh = loaded.GetPart<EnhancementGlowQuartz>();
            Assert.IsNotNull(enh);
            Assert.AreEqual(3, enh.Tier);
            Assert.AreEqual(3, enh.RadiusBonus);
            Assert.IsTrue(enh.AppliedBonus);
        }

        // ── ItemEnhancing.Apply integration ──────────────────────

        [Test]
        public void ItemEnhancing_Apply_OnWeapon_Attaches()
        {
            var weapon = MakeWeapon();
            bool ok = ItemEnhancing.Apply(weapon, nameof(EnhancementGlowQuartz), tier: 2);
            Assert.IsTrue(ok);
            var enh = weapon.GetPart<EnhancementGlowQuartz>();
            Assert.IsNotNull(enh);
            Assert.AreEqual(2, enh.RadiusBonus);
        }

        [Test]
        public void ItemEnhancing_Apply_OnTonic_RejectsAtApplicable()
        {
            var tonic = MakeNonEquippable();
            Diag.ResetAll();
            bool ok = ItemEnhancing.Apply(tonic, nameof(EnhancementGlowQuartz));
            Assert.IsFalse(ok);
            Assert.IsNull(tonic.GetPart<EnhancementGlowQuartz>());
        }
    }
}
