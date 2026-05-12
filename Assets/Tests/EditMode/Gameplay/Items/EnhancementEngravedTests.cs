using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.2.4 — <see cref="EnhancementEngraved"/>
    /// contract pin.
    ///
    /// <para><b>What's pinned:</b></para>
    /// <list type="bullet">
    ///   <item>Applicable: requires EquippablePart (weapons, armor).</item>
    ///   <item>TierConfigure: RepDelta = Tier * 5.</item>
    ///   <item>OnEquipped (player): rep += RepDelta, AppliedBonus flips
    ///         true, diag emitted.</item>
    ///   <item>OnEquipped (NPC): no rep change — engraved-on-enemy
    ///         doesn't move player rep.</item>
    ///   <item>OnUnequipped (player + AppliedBonus): rep -= RepDelta.</item>
    ///   <item>Atomicity: double-equip is idempotent (F.3.4 lesson);
    ///         unequip-without-equip is a no-op.</item>
    ///   <item>Faction unset → no rep change.</item>
    ///   <item>Save/load round-trip preserves Faction + RepDelta +
    ///         AppliedBonus + Tier.</item>
    /// </list>
    /// </summary>
    public class EnhancementEngravedTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            PlayerReputation.Reset();
            Diag.ResetAll();
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(EnhancementEngraved));
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeArmor()
        {
            var e = new Entity { ID = "leather", BlueprintName = "leather" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "leather" });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new ArmorPart { AV = 1 });
            e.AddPart(new EquippablePart { Slot = "Body" });
            return e;
        }

        private static Entity MakeNonEquippable()
        {
            // Has Item tag but NO EquippablePart — e.g. a tonic or food.
            var e = new Entity { ID = "tonic", BlueprintName = "tonic" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "tonic" });
            e.AddPart(new PhysicsPart { Takeable = true });
            return e;
        }

        private static Entity MakePlayer()
        {
            var e = new Entity { ID = "hero", BlueprintName = "hero" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.AddPart(new RenderPart { DisplayName = "hero" });
            return e;
        }

        private static Entity MakeNPC()
        {
            var e = new Entity { ID = "snapjaw", BlueprintName = "snapjaw" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "snapjaw" });
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // Applicable — requires EquippablePart
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Applicable_ArmorWithEquippable_True()
        {
            Assert.IsTrue(new EnhancementEngraved().Applicable(MakeArmor()));
        }

        [Test]
        public void Applicable_NonEquippableItem_False()
        {
            // Counter-check pair: same Item tag but no EquippablePart.
            Assert.IsFalse(new EnhancementEngraved().Applicable(MakeNonEquippable()));
        }

        [Test]
        public void Applicable_NullItem_False()
        {
            Assert.IsFalse(new EnhancementEngraved().Applicable(null));
        }

        // ════════════════════════════════════════════════════════════════
        // Tier scaling — RepDelta = Tier * 5
        // ════════════════════════════════════════════════════════════════

        [TestCase(1, 5)]
        [TestCase(2, 10)]
        [TestCase(3, 15)]
        [TestCase(4, 20)]
        public void TierConfigure_ScalesRepDeltaLinearly(int tier, int expectedDelta)
        {
            var enh = new EnhancementEngraved();
            enh.ApplyTier(tier);
            Assert.AreEqual(expectedDelta, enh.RepDelta);
        }

        // ════════════════════════════════════════════════════════════════
        // OnEquipped (player) — applies rep delta, sets flag
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void OnEquipped_Player_AppliesRepDelta()
        {
            var armor = MakeArmor();
            var enh = new EnhancementEngraved { Faction = "Villagers" };
            enh.ApplyTier(2);  // +10
            armor.AddPart(enh);
            int repBefore = PlayerReputation.Get("Villagers");

            enh.OnEquipped(MakePlayer(), armor);

            Assert.AreEqual(repBefore + 10, PlayerReputation.Get("Villagers"),
                "Player equipping a Tier-2 Engraved adds +10 rep.");
            Assert.IsTrue(enh.AppliedBonus,
                "AppliedBonus flag flips true.");
        }

        [Test]
        public void OnEquipped_Player_EmitsBonusAppliedDiag()
        {
            var armor = MakeArmor();
            var enh = new EnhancementEngraved { Faction = "Villagers" };
            enh.ApplyTier(1);
            armor.AddPart(enh);
            Diag.ResetAll();

            enh.OnEquipped(MakePlayer(), armor);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "BonusApplied",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("EnhancementEngraved", recs[0].PayloadJson);
            StringAssert.Contains("Villagers", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════════
        // OnEquipped (NPC) — player-only gate
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void OnEquipped_NPC_DoesNotApplyRep()
        {
            // Counter-check pair to OnEquipped_Player_AppliesRepDelta.
            // An NPC wielding an Engraved item must NOT move player rep.
            var armor = MakeArmor();
            var enh = new EnhancementEngraved { Faction = "Villagers" };
            enh.ApplyTier(2);
            armor.AddPart(enh);
            int repBefore = PlayerReputation.Get("Villagers");

            enh.OnEquipped(MakeNPC(), armor);

            Assert.AreEqual(repBefore, PlayerReputation.Get("Villagers"),
                "NPC equipping Engraved does NOT move player rep.");
            Assert.IsFalse(enh.AppliedBonus,
                "AppliedBonus stays false — no apply, no flag.");
        }

        [Test]
        public void OnEquipped_NullActor_NoApply()
        {
            var armor = MakeArmor();
            var enh = new EnhancementEngraved { Faction = "Villagers" };
            enh.ApplyTier(2);
            armor.AddPart(enh);

            Assert.DoesNotThrow(() => enh.OnEquipped(null, armor));
            Assert.IsFalse(enh.AppliedBonus);
        }

        [Test]
        public void OnEquipped_FactionUnset_NoApply()
        {
            // Engraved with empty Faction is a no-op (Engraved with what?).
            var armor = MakeArmor();
            var enh = new EnhancementEngraved { Faction = "" };
            enh.ApplyTier(2);
            armor.AddPart(enh);

            enh.OnEquipped(MakePlayer(), armor);

            Assert.IsFalse(enh.AppliedBonus,
                "Empty Faction → no apply, no flag flip.");
        }

        // ════════════════════════════════════════════════════════════════
        // OnUnequipped — symmetric inverse
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void OnUnequipped_AfterPlayerEquip_RestoresRep()
        {
            var armor = MakeArmor();
            var enh = new EnhancementEngraved { Faction = "Villagers" };
            enh.ApplyTier(3);  // +15
            armor.AddPart(enh);
            var player = MakePlayer();
            int repBefore = PlayerReputation.Get("Villagers");

            enh.OnEquipped(player, armor);
            Assert.AreEqual(repBefore + 15, PlayerReputation.Get("Villagers"),
                "Precondition: equip applied +15.");

            enh.OnUnequipped(player, armor);

            Assert.AreEqual(repBefore, PlayerReputation.Get("Villagers"),
                "Unequip restores original rep — net change is zero.");
            Assert.IsFalse(enh.AppliedBonus);
        }

        [Test]
        public void OnUnequipped_WithoutPriorEquip_NoOp()
        {
            // Counter-check pair to AfterPlayerEquip_RestoresRep.
            // Without AppliedBonus=true, unequip must not subtract.
            var armor = MakeArmor();
            var enh = new EnhancementEngraved { Faction = "Villagers" };
            enh.ApplyTier(3);
            armor.AddPart(enh);
            int repBefore = PlayerReputation.Get("Villagers");

            enh.OnUnequipped(MakePlayer(), armor);

            Assert.AreEqual(repBefore, PlayerReputation.Get("Villagers"),
                "Unequip without prior equip is a no-op.");
        }

        // ════════════════════════════════════════════════════════════════
        // Atomicity — double-equip is idempotent
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void OnEquipped_CalledTwice_AppliesOnce()
        {
            var armor = MakeArmor();
            var enh = new EnhancementEngraved { Faction = "Villagers" };
            enh.ApplyTier(2);  // +10
            armor.AddPart(enh);
            int repBefore = PlayerReputation.Get("Villagers");

            enh.OnEquipped(MakePlayer(), armor);
            enh.OnEquipped(MakePlayer(), armor);  // duplicate

            Assert.AreEqual(repBefore + 10, PlayerReputation.Get("Villagers"),
                "Second OnEquipped is a no-op — rep stays at +10, not +20.");
        }

        // ════════════════════════════════════════════════════════════════
        // ItemEnhancing.Apply integration
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ItemEnhancing_Apply_OnEquippableItem_Attaches()
        {
            var armor = MakeArmor();
            bool ok = ItemEnhancing.Apply(armor, nameof(EnhancementEngraved), tier: 2);
            Assert.IsTrue(ok);
            var enh = armor.GetPart<EnhancementEngraved>();
            Assert.IsNotNull(enh);
            Assert.AreEqual(2, enh.Tier);
            Assert.AreEqual(10, enh.RepDelta);
        }

        [Test]
        public void ItemEnhancing_Apply_OnNonEquippable_RejectsAtApplicable()
        {
            var tonic = MakeNonEquippable();
            Diag.ResetAll();
            bool ok = ItemEnhancing.Apply(tonic, nameof(EnhancementEngraved));
            Assert.IsFalse(ok);
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
        // Save/load round-trip — Faction + RepDelta + AppliedBonus
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void RoundTrip_PreservesFactionAndState()
        {
            var armor = MakeArmor();
            ItemEnhancing.Apply(armor, nameof(EnhancementEngraved), tier: 3);
            var src = armor.GetPart<EnhancementEngraved>();
            src.Faction = "Villagers";
            src.OnEquipped(MakePlayer(), armor);  // AppliedBonus → true

            Entity loaded = PartRoundTripHelper.RoundTripEntity(armor);

            var enh = loaded.GetPart<EnhancementEngraved>();
            Assert.IsNotNull(enh);
            Assert.AreEqual(3, enh.Tier);
            Assert.AreEqual("Villagers", enh.Faction,
                "Faction (string field) round-trips via SL.6 reflection.");
            Assert.AreEqual(15, enh.RepDelta);
            Assert.IsTrue(enh.AppliedBonus,
                "AppliedBonus round-trips — critical so unequip on load " +
                "correctly subtracts the rep delta.");
        }
    }
}
