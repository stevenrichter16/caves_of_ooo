using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.2.3 — <see cref="EnhancementLacquered"/>
    /// contract pin.
    ///
    /// <para><b>What's pinned:</b></para>
    /// <list type="bullet">
    ///   <item>Applicable: armor items only — rejects melee weapons,
    ///         non-equippable items, null.</item>
    ///   <item>TierConfigure: AvBonus = Tier * 1.</item>
    ///   <item>OnEquipped: AV += AvBonus, AppliedBonus flag flips true,
    ///         diag emitted.</item>
    ///   <item>OnUnequipped: AV -= AvBonus, AppliedBonus back to false,
    ///         diag emitted, symmetric net change.</item>
    ///   <item>Atomicity (F.3.4 lesson): double-equip is a no-op;
    ///         unequip without prior equip is a no-op.</item>
    ///   <item>Save/load round-trip preserves Tier + AvBonus + AppliedBonus.</item>
    /// </list>
    /// </summary>
    public class EnhancementLacqueredTests
    {
        [SetUp]
        public void Setup()
        {
            Diag.ResetAll();
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(EnhancementLacquered));
        }

        private static Entity MakeArmor(int baseAV = 2)
        {
            var e = new Entity { ID = "leather", BlueprintName = "leather" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "leather" });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new ArmorPart { AV = baseAV });
            return e;
        }

        private static Entity MakeMeleeWeapon()
        {
            var e = new Entity { ID = "longsword", BlueprintName = "longsword" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "longsword" });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d6", Attributes = "Cutting" });
            return e;
        }

        private static Entity MakeActor() =>
            new Entity { ID = "hero", BlueprintName = "hero" };

        // ════════════════════════════════════════════════════════════════
        // Applicable — armor-only filter
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Applicable_ArmorItem_True()
        {
            Assert.IsTrue(new EnhancementLacquered().Applicable(MakeArmor()));
        }

        [Test]
        public void Applicable_MeleeWeapon_False()
        {
            // Counter-check pair: same Item tag + EquippablePart-shaped,
            // but no ArmorPart. Must reject.
            Assert.IsFalse(new EnhancementLacquered().Applicable(MakeMeleeWeapon()));
        }

        [Test]
        public void Applicable_NullItem_False()
        {
            Assert.IsFalse(new EnhancementLacquered().Applicable(null));
        }

        // ════════════════════════════════════════════════════════════════
        // Tier scaling — AvBonus = Tier * 1
        // ════════════════════════════════════════════════════════════════

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        public void TierConfigure_ScalesAvBonusLinearly(int tier, int expectedBonus)
        {
            var enh = new EnhancementLacquered();
            enh.ApplyTier(tier);
            Assert.AreEqual(expectedBonus, enh.AvBonus);
        }

        // ════════════════════════════════════════════════════════════════
        // OnEquipped — applies AV bonus, sets flag, emits diag
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void OnEquipped_AppliesAvBonus_ToArmor()
        {
            var armor = MakeArmor(baseAV: 3);
            var enh = new EnhancementLacquered();
            enh.ApplyTier(2);  // +2 AV
            armor.AddPart(enh);

            enh.OnEquipped(MakeActor(), armor);

            Assert.AreEqual(5, armor.GetPart<ArmorPart>().AV,
                "Tier-2 Lacquered adds +2 to base AV=3 → 5.");
            Assert.IsTrue(enh.AppliedBonus,
                "AppliedBonus flag flips true after OnEquipped.");
        }

        [Test]
        public void OnEquipped_EmitsBonusAppliedDiag()
        {
            var armor = MakeArmor();
            var enh = new EnhancementLacquered();
            enh.ApplyTier(1);
            armor.AddPart(enh);
            Diag.ResetAll();

            enh.OnEquipped(MakeActor(), armor);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "BonusApplied",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("EnhancementLacquered", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════════
        // OnUnequipped — symmetric inverse of OnEquipped
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void OnUnequipped_AfterEquip_RestoresOriginalAv()
        {
            var armor = MakeArmor(baseAV: 3);
            var enh = new EnhancementLacquered();
            enh.ApplyTier(2);
            armor.AddPart(enh);

            enh.OnEquipped(MakeActor(), armor);
            enh.OnUnequipped(MakeActor(), armor);

            Assert.AreEqual(3, armor.GetPart<ArmorPart>().AV,
                "Equip-then-unequip is net-zero on AV.");
            Assert.IsFalse(enh.AppliedBonus,
                "AppliedBonus flag flips back to false.");
        }

        [Test]
        public void OnUnequipped_WithoutPriorEquip_NoOp()
        {
            // Counter-check pair to AfterEquip_RestoresOriginalAv.
            // If AppliedBonus is false, OnUnequipped must NOT subtract.
            // Pin: a save/load of a never-equipped Lacquered must not
            // corrupt AV on the first unequip dispatch.
            var armor = MakeArmor(baseAV: 5);
            var enh = new EnhancementLacquered();
            enh.ApplyTier(3);
            armor.AddPart(enh);
            // No OnEquipped call.
            enh.OnUnequipped(MakeActor(), armor);

            Assert.AreEqual(5, armor.GetPart<ArmorPart>().AV,
                "Unequip without prior equip is a no-op (AppliedBonus gate).");
        }

        // ════════════════════════════════════════════════════════════════
        // Atomicity — double-equip is idempotent (F.3.4 lesson)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void OnEquipped_CalledTwice_BonusAppliesOnce()
        {
            // F.3.4 atomicity lesson — eager AppliedBonus flag prevents
            // double-add if the dispatcher fires twice (e.g. via the
            // EquipCommand + UnequipCommand.UnequipAndRemove undo path).
            var armor = MakeArmor(baseAV: 3);
            var enh = new EnhancementLacquered();
            enh.ApplyTier(2);
            armor.AddPart(enh);

            enh.OnEquipped(MakeActor(), armor);
            enh.OnEquipped(MakeActor(), armor);  // duplicate

            Assert.AreEqual(5, armor.GetPart<ArmorPart>().AV,
                "Second OnEquipped is a no-op — AV stays at +2, not +4.");
        }

        [Test]
        public void OnEquipped_OnNonArmor_NoCrash_NoMutation()
        {
            // Non-armor item: OnEquipped should bail without throwing.
            // (Real call sites filter via Applicable, but the hook
            // must be defensively null-safe.)
            var weapon = MakeMeleeWeapon();
            var enh = new EnhancementLacquered();
            enh.ApplyTier(2);
            // Note: we intentionally skip Applicable check here to
            // exercise the defensive guard.
            weapon.AddPart(enh);

            Assert.DoesNotThrow(() => enh.OnEquipped(MakeActor(), weapon));
            Assert.IsFalse(enh.AppliedBonus,
                "Without ArmorPart, AppliedBonus must NOT be set " +
                "(or unequip would later mutate a non-existent AV).");
        }

        [Test]
        public void OnEquipped_NullItem_NoCrash()
        {
            var enh = new EnhancementLacquered();
            enh.ApplyTier(1);
            Assert.DoesNotThrow(() => enh.OnEquipped(MakeActor(), null));
            Assert.IsFalse(enh.AppliedBonus);
        }

        // ════════════════════════════════════════════════════════════════
        // ItemEnhancing.Apply integration
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ItemEnhancing_Apply_OnArmor_AttachesLacquered()
        {
            var armor = MakeArmor();
            bool ok = ItemEnhancing.Apply(armor, nameof(EnhancementLacquered), tier: 3);
            Assert.IsTrue(ok);
            var enh = armor.GetPart<EnhancementLacquered>();
            Assert.IsNotNull(enh);
            Assert.AreEqual(3, enh.Tier);
            Assert.AreEqual(3, enh.AvBonus);
        }

        [Test]
        public void ItemEnhancing_Apply_OnWeapon_RejectsAtApplicable()
        {
            var weapon = MakeMeleeWeapon();
            Diag.ResetAll();
            bool ok = ItemEnhancing.Apply(weapon, nameof(EnhancementLacquered));
            Assert.IsFalse(ok);
            Assert.IsNull(weapon.GetPart<EnhancementLacquered>());

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "ApplyFailed",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("not_applicable", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════════
        // Save/load round-trip — Tier + AvBonus + AppliedBonus
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void RoundTrip_PreservesTierAndState()
        {
            var armor = MakeArmor(baseAV: 3);
            ItemEnhancing.Apply(armor, nameof(EnhancementLacquered), tier: 2);
            var src = armor.GetPart<EnhancementLacquered>();
            src.OnEquipped(MakeActor(), armor);  // flag → true

            Entity loaded = PartRoundTripHelper.RoundTripEntity(armor);

            var enh = loaded.GetPart<EnhancementLacquered>();
            Assert.IsNotNull(enh);
            Assert.AreEqual(2, enh.Tier);
            Assert.AreEqual(2, enh.AvBonus);
            Assert.IsTrue(enh.AppliedBonus,
                "AppliedBonus flag round-trips — critical for atomicity. " +
                "If false on load, unequip would skip subtract and AV " +
                "would silently drift up.");
        }
    }
}
