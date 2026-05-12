using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.2.5 — adversarial sweep across the three
    /// concrete enhancements (Serrated, Lacquered, Engraved) + the
    /// shared E.2.1 dispatch substrate.
    ///
    /// <para>Per CLAUDE.md "Adversarial test sweep" gate. E.2 hits
    /// these taxonomy surfaces:</para>
    /// <list type="bullet">
    ///   <item><b>State atomicity</b> — eager AppliedBonus flag in
    ///         Lacquered + Engraved (F.3.4 lesson)</item>
    ///   <item><b>Save/load reach</b> — Faction string + bool flag
    ///         + int counters round-trip via reflection</item>
    ///   <item><b>Cross-system aggregation</b> — multiple enhancements
    ///         on same item all dispatch correctly</item>
    ///   <item><b>Stacking semantics</b> — double-equip idempotent;
    ///         Serrated bleed stacks via BleedingEffect.OnStack</item>
    ///   <item><b>Anti-exploit</b> — equip/unequip churn on Engraved
    ///         doesn't drift rep; NPC equipping doesn't move player rep</item>
    ///   <item><b>Probability boundaries</b> — Serrated chance=0
    ///         vs chance=100 (covered per-fixture; this sweep extends
    ///         to negative/excessive tier inputs)</item>
    ///   <item><b>Boundary inputs</b> — extreme tier values, nulls,
    ///         actor==target, hooks-with-null-args</item>
    ///   <item><b>Diag dispatch invariants</b> — exactly-one-record
    ///         per success path; zero on rejection</item>
    /// </list>
    ///
    /// <para><b>Honesty bound:</b> 0 bugs found doesn't prove E.2 is
    /// bug-free. These tests probe the bug classes the author
    /// imagined. The value is (a) the regression target — future
    /// changes that break these contracts surface visibly, and
    /// (b) the documentation of contracts that were never explicitly
    /// written down (esp. NPC-equipping-Engraved-no-op).</para>
    /// </summary>
    public class E2EnhancementsAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            PlayerReputation.Reset();
            Diag.ResetAll();
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(EnhancementSerrated));
            EnhancementFactory.Register(typeof(EnhancementLacquered));
            EnhancementFactory.Register(typeof(EnhancementEngraved));
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeCuttingWeapon()
        {
            var e = new Entity { ID = "longsword", BlueprintName = "longsword" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "longsword" });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d6", Attributes = "Melee Cutting Strength" });
            e.AddPart(new EquippablePart { Slot = "MainHand" });
            return e;
        }

        private static Entity MakeArmor()
        {
            var e = new Entity { ID = "leather", BlueprintName = "leather" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "leather" });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new ArmorPart { AV = 2 });
            e.AddPart(new EquippablePart { Slot = "Body" });
            return e;
        }

        private static Entity MakePlayer()
        {
            var e = new Entity { ID = "hero", BlueprintName = "hero" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.AddPart(new RenderPart { DisplayName = "hero" });
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity MakeNPC()
        {
            var e = new Entity { ID = "snapjaw", BlueprintName = "snapjaw" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "snapjaw" });
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 8, Min = 1, Max = 50 };
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // CROSS-SYSTEM AGGREGATION — multiple enhancements on same item
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SerratedPlusEngravedOnSameWeapon_BothDispatch()
        {
            // Two enhancements on a Cutting weapon — both hooks must
            // fire on a single hit/equip dispatch. Pin: cross-system
            // aggregation works AND the slot cap (2) is exactly hit.
            var weapon = MakeCuttingWeapon();
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementSerrated), tier: 4));
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementEngraved), tier: 2));
            Assert.AreEqual(2, ItemEnhancing.CountEnhancements(weapon),
                "Both enhancements attached — slot cap is exactly hit.");

            // Force trigger Serrated by setting ChancePercent=100.
            var serrated = weapon.GetPart<EnhancementSerrated>();
            serrated.ChancePercent = 100;

            // Configure Engraved with a faction.
            var engraved = weapon.GetPart<EnhancementEngraved>();
            engraved.Faction = "Villagers";

            var player = MakePlayer();
            var defender = MakeNPC();
            int repBefore = PlayerReputation.Get("Villagers");

            // DispatchOnEquip: only Engraved listens here. Player rep should bump.
            ItemEnhancementDispatch.DispatchOnEquip(player, weapon);
            Assert.AreEqual(repBefore + 10, PlayerReputation.Get("Villagers"),
                "Engraved fired on equip; Serrated didn't touch rep.");

            // DispatchOnHit: only Serrated listens here. Bleed should land.
            ItemEnhancementDispatch.DispatchOnHit(weapon, defender, player,
                new Damage(5), 5, zone: null, rng: new Random(0));
            Assert.IsNotNull(defender.GetPart<StatusEffectsPart>().GetEffect<BleedingEffect>(),
                "Serrated fired on hit; Engraved didn't touch defender.");
        }

        [Test]
        public void Adversarial_LacqueredCannotStack_VetoedAtSlotCapIfSecondAdds()
        {
            // Same item gets Lacquered Tier-1 + Lacquered Tier-2?
            // Per CLAUDE.md taxonomy "stacking semantics", we expect
            // the slot-cap to fill before duplicate-type fires; both
            // would attach because they're different instances. But
            // double-apply of the same instance is idempotent.
            // This test pins: TWO Lacquered Parts CAN attach (slot
            // cap = 2, no per-type dedup at the infra layer — that's
            // E.5+ territory if it ever becomes an issue).
            var armor = MakeArmor();
            Assert.IsTrue(ItemEnhancing.Apply(armor, nameof(EnhancementLacquered), tier: 1));
            Assert.IsTrue(ItemEnhancing.Apply(armor, nameof(EnhancementLacquered), tier: 2));
            Assert.AreEqual(2, ItemEnhancing.CountEnhancements(armor));
            // Third (any kind) gets vetoed.
            Assert.IsFalse(ItemEnhancing.Apply(armor, nameof(EnhancementEngraved), tier: 1));
        }

        // ════════════════════════════════════════════════════════════════
        // ANTI-EXPLOIT — equip/unequip churn doesn't drift rep
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_EngravedEquipUnequipChurn_RepNetZero()
        {
            // 100 equip/unequip cycles → net rep change must be zero.
            // If the apply OR remove path miscounts (eager flag bug,
            // double-counting), this drifts visibly.
            var armor = MakeArmor();
            ItemEnhancing.Apply(armor, nameof(EnhancementEngraved), tier: 2);
            var engraved = armor.GetPart<EnhancementEngraved>();
            engraved.Faction = "Villagers";
            int repBefore = PlayerReputation.Get("Villagers");
            var player = MakePlayer();

            for (int i = 0; i < 100; i++)
            {
                ItemEnhancementDispatch.DispatchOnEquip(player, armor);
                ItemEnhancementDispatch.DispatchOnUnequip(player, armor);
            }

            Assert.AreEqual(repBefore, PlayerReputation.Get("Villagers"),
                "100 equip/unequip cycles → net-zero rep change. " +
                "Any drift means apply/remove count is asymmetric.");
        }

        [Test]
        public void Adversarial_LacqueredEquipUnequipChurn_AvNetZero()
        {
            var armor = MakeArmor();
            int avBefore = armor.GetPart<ArmorPart>().AV;
            ItemEnhancing.Apply(armor, nameof(EnhancementLacquered), tier: 3);

            for (int i = 0; i < 100; i++)
            {
                ItemEnhancementDispatch.DispatchOnEquip(MakePlayer(), armor);
                ItemEnhancementDispatch.DispatchOnUnequip(MakePlayer(), armor);
            }

            Assert.AreEqual(avBefore, armor.GetPart<ArmorPart>().AV,
                "100 equip/unequip cycles → AV unchanged. Atomicity flag holds.");
        }

        [Test]
        public void Adversarial_NpcEquipsEngraved_PlayerRepUnchanged()
        {
            // NPC equipping must NEVER move player rep — anti-exploit.
            // Pin: even if NPC equipping is a regular gameplay path
            // (e.g. faction NPC armed up by world gen), the player's
            // Villagers rep must not nudge.
            var armor = MakeArmor();
            ItemEnhancing.Apply(armor, nameof(EnhancementEngraved), tier: 4);
            armor.GetPart<EnhancementEngraved>().Faction = "Villagers";
            int repBefore = PlayerReputation.Get("Villagers");

            // 10 NPC equip/unequip cycles.
            for (int i = 0; i < 10; i++)
            {
                ItemEnhancementDispatch.DispatchOnEquip(MakeNPC(), armor);
                ItemEnhancementDispatch.DispatchOnUnequip(MakeNPC(), armor);
            }

            Assert.AreEqual(repBefore, PlayerReputation.Get("Villagers"),
                "NPC equip/unequip is invisible to player rep.");
        }

        // ════════════════════════════════════════════════════════════════
        // STACKING — Serrated bleeds stack via BleedingEffect.OnStack
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SerratedBleed_StacksByExtendingExisting()
        {
            // Two Serrated procs on the same defender within a turn
            // → BleedingEffect.OnStack upgrades the existing instance
            // rather than creating a second. Pin the contract carries
            // through the enhancement dispatch.
            var weapon = MakeCuttingWeapon();
            ItemEnhancing.Apply(weapon, nameof(EnhancementSerrated), tier: 4);
            var serrated = weapon.GetPart<EnhancementSerrated>();
            serrated.ChancePercent = 100;  // guarantee
            var defender = MakeNPC();
            var attacker = MakePlayer();
            var rng = new Random(0);

            // Two consecutive procs.
            serrated.OnAttackerHit(defender, attacker, new Damage(5), 5, null, rng);
            serrated.OnAttackerHit(defender, attacker, new Damage(5), 5, null, rng);

            // Bleed effect should still be a single instance after stack.
            var effects = defender.GetPart<StatusEffectsPart>();
            int bleedCount = 0;
            foreach (var eff in effects.Effects)
                if (eff is BleedingEffect) bleedCount++;
            Assert.AreEqual(1, bleedCount,
                "Stack consolidates to a single BleedingEffect instance, " +
                "not two parallel ticks (mirrors BleedingEffect.OnStack contract).");
        }

        // ════════════════════════════════════════════════════════════════
        // SAVE/LOAD REACH — multi-enhancement round-trip
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TwoEnhancements_RoundTrip_BothFieldsetPreserved()
        {
            // Pair test extends ItemEnhancementAdversarialTests's
            // two-enhancement round-trip (StubA/StubB) to two REAL
            // E.2 enhancements that carry distinct field types
            // (Serrated → int ChancePercent + int SaveTarget + string
            // DamageDice; Engraved → string Faction + int RepDelta +
            // bool AppliedBonus). Pin: heterogeneous field types
            // round-trip independently.
            var weapon = MakeCuttingWeapon();
            ItemEnhancing.Apply(weapon, nameof(EnhancementSerrated), tier: 3);
            ItemEnhancing.Apply(weapon, nameof(EnhancementEngraved), tier: 2);
            weapon.GetPart<EnhancementEngraved>().Faction = "Villagers";
            weapon.GetPart<EnhancementEngraved>().OnEquipped(MakePlayer(), weapon);

            Entity loaded = PartRoundTripHelper.RoundTripEntity(weapon);

            var s = loaded.GetPart<EnhancementSerrated>();
            Assert.AreEqual(3, s.Tier);
            Assert.AreEqual(30, s.ChancePercent);
            Assert.AreEqual(15, s.SaveTarget);
            Assert.AreEqual("1d2", s.DamageDice);

            var e = loaded.GetPart<EnhancementEngraved>();
            Assert.AreEqual(2, e.Tier);
            Assert.AreEqual(10, e.RepDelta);
            Assert.AreEqual("Villagers", e.Faction);
            Assert.IsTrue(e.AppliedBonus);
        }

        // ════════════════════════════════════════════════════════════════
        // BOUNDARY INPUTS — extreme tiers + null args
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Tier0_ChancePercentZero()
        {
            // Lockdown: ApplyTier(0) → ChancePercent=0 → never fires.
            // Counter-check against silent inversion to "always fires."
            var s = new EnhancementSerrated();
            s.ApplyTier(0);
            Assert.AreEqual(0, s.ChancePercent);
            var defender = MakeNPC();
            s.OnAttackerHit(defender, MakePlayer(), new Damage(5), 5, null, new Random(0));
            Assert.IsNull(defender.GetPart<StatusEffectsPart>().GetEffect<BleedingEffect>(),
                "Tier 0 Serrated = 0% chance = never fires.");
        }

        [Test]
        public void Adversarial_NegativeTier_ChancePercentNegative_NeverFires()
        {
            // Pin: negative tier produces negative chance. The roll
            // `rng.Next(100) >= chance` is true for all rng values
            // because chance < 0 → never fires (since `Next(100)` is
            // always ≥ 0). Defensive — no crash, no spurious procs.
            var s = new EnhancementSerrated();
            s.ApplyTier(-1);
            Assert.AreEqual(-10, s.ChancePercent);
            var defender = MakeNPC();
            for (int seed = 0; seed < 50; seed++)
            {
                s.OnAttackerHit(defender, MakePlayer(),
                    new Damage(5), 5, null, new Random(seed));
            }
            Assert.IsNull(defender.GetPart<StatusEffectsPart>().GetEffect<BleedingEffect>(),
                "Negative chance never fires across many seeds.");
        }

        [Test]
        public void Adversarial_HighTier_ChancePercentHigh_AlwaysFires()
        {
            // Pin: tier 100 produces chance=1000 which still fires
            // every roll (rng.Next(100) returns 0-99, always < 1000).
            // Catches "chance == 100 special-case branch" inversions.
            var s = new EnhancementSerrated();
            s.ApplyTier(100);
            Assert.AreEqual(1000, s.ChancePercent);
            var defender = MakeNPC();
            s.OnAttackerHit(defender, MakePlayer(), new Damage(5), 5, null, new Random(0));
            Assert.IsNotNull(defender.GetPart<StatusEffectsPart>().GetEffect<BleedingEffect>(),
                "Chance=1000 always fires.");
        }

        // ════════════════════════════════════════════════════════════════
        // DIAG DISPATCH INVARIANTS — exactly one record per branch
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SerratedTrigger_EmitsExactlyOneTriggered()
        {
            var s = new EnhancementSerrated();
            s.ChancePercent = 100;
            Diag.ResetAll();
            s.OnAttackerHit(MakeNPC(), MakePlayer(),
                new Damage(5), 5, null, new Random(0));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "Triggered",
                Limit = 50,
            }).Records;
            Assert.AreEqual(1, recs.Count, "Exactly one Triggered per proc.");
        }

        [Test]
        public void Adversarial_LacqueredEquipUnequip_EmitsPairedDiags()
        {
            var armor = MakeArmor();
            ItemEnhancing.Apply(armor, nameof(EnhancementLacquered), tier: 1);
            var enh = armor.GetPart<EnhancementLacquered>();
            Diag.ResetAll();

            enh.OnEquipped(MakePlayer(), armor);
            enh.OnUnequipped(MakePlayer(), armor);

            int applied = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY, Kind = "BonusApplied", Limit = 10,
            }).Records.Count;
            int removed = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY, Kind = "BonusRemoved", Limit = 10,
            }).Records.Count;
            Assert.AreEqual(1, applied);
            Assert.AreEqual(1, removed);
        }

        [Test]
        public void Adversarial_DoubleEquipLacquered_OnlyOneBonusAppliedDiag()
        {
            // Atomicity gate test — second equip is a no-op so it
            // must NOT emit a second BonusApplied record.
            var armor = MakeArmor();
            ItemEnhancing.Apply(armor, nameof(EnhancementLacquered), tier: 1);
            var enh = armor.GetPart<EnhancementLacquered>();
            Diag.ResetAll();

            enh.OnEquipped(MakePlayer(), armor);
            enh.OnEquipped(MakePlayer(), armor);  // duplicate

            int applied = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY, Kind = "BonusApplied", Limit = 10,
            }).Records.Count;
            Assert.AreEqual(1, applied,
                "Atomicity gate ALSO short-circuits the diag emission — " +
                "no spam if a future double-dispatch lands.");
        }

        // ════════════════════════════════════════════════════════════════
        // SELF-REFERENTIAL — actor and target are the same entity
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SerratedSelfHit_AppliesBleedToSelf()
        {
            // Pin: if attacker == defender (self-damage path, e.g.
            // confused-attacker hits self), Serrated should still
            // proc. No special-case skip; the OnAttackerHit hook is
            // agnostic to actor identity.
            var hero = MakePlayer();
            var s = new EnhancementSerrated();
            s.ChancePercent = 100;
            s.OnAttackerHit(hero, hero, new Damage(5), 5, null, new Random(0));
            Assert.IsNotNull(hero.GetPart<StatusEffectsPart>().GetEffect<BleedingEffect>(),
                "Self-hit still applies Serrated bleed.");
        }

        // ════════════════════════════════════════════════════════════════
        // PARSER / MALFORMED — unknown enhancement names, garbage strings
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ApplyUnknownEnhancement_EmitsApplyFailedReason()
        {
            var weapon = MakeCuttingWeapon();
            Diag.ResetAll();
            ItemEnhancing.Apply(weapon, "EnhancementDoesNotExist");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "ApplyFailed",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("unknown_enhancement", recs[0].PayloadJson);
        }

        [Test]
        public void Adversarial_ApplyEnhancementToTonic_RejectedAtApplicable()
        {
            // Tonic = no MeleeWeaponPart, no ArmorPart, no EquippablePart.
            // ALL three E.2 enhancements should reject it.
            var tonic = new Entity { ID = "tonic", BlueprintName = "tonic" };
            tonic.Tags["Item"] = "";
            tonic.AddPart(new RenderPart { DisplayName = "tonic" });
            tonic.AddPart(new PhysicsPart { Takeable = true });

            Assert.IsFalse(ItemEnhancing.Apply(tonic, nameof(EnhancementSerrated)));
            Assert.IsFalse(ItemEnhancing.Apply(tonic, nameof(EnhancementLacquered)));
            Assert.IsFalse(ItemEnhancing.Apply(tonic, nameof(EnhancementEngraved)));
            Assert.AreEqual(0, ItemEnhancing.CountEnhancements(tonic),
                "Zero enhancements stuck to a tonic.");
        }

        // ════════════════════════════════════════════════════════════════
        // CROSS-ACTOR — multiple players? (CoO is single-player but
        // pin the contract anyway in case future multi-actor flows)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TwoPlayersEquipSameEngraved_SecondEquipNoOp()
        {
            // Pin: if a second "player"-tagged actor tries to equip
            // an already-equipped Engraved item, AppliedBonus flag
            // gates the second apply. Defensive: in single-player
            // this shouldn't happen, but mods/multi-pawn could.
            var armor = MakeArmor();
            ItemEnhancing.Apply(armor, nameof(EnhancementEngraved), tier: 2);
            var engraved = armor.GetPart<EnhancementEngraved>();
            engraved.Faction = "Villagers";
            int repBefore = PlayerReputation.Get("Villagers");

            var p1 = MakePlayer();
            var p2 = MakePlayer();
            engraved.OnEquipped(p1, armor);
            engraved.OnEquipped(p2, armor);  // second player

            Assert.AreEqual(repBefore + 10, PlayerReputation.Get("Villagers"),
                "Second player-equip is a no-op — atomicity flag holds.");
        }

        // ════════════════════════════════════════════════════════════════
        // DISPATCHER NULL-SAFETY — combined with concrete enhancements
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_DispatchOnHit_WithEnhancementPart_NullDefender_NoCrash()
        {
            var weapon = MakeCuttingWeapon();
            ItemEnhancing.Apply(weapon, nameof(EnhancementSerrated), tier: 4);
            weapon.GetPart<EnhancementSerrated>().ChancePercent = 100;

            Assert.DoesNotThrow(() => ItemEnhancementDispatch.DispatchOnHit(
                weapon, defender: null, attacker: MakePlayer(),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new Random(0)));
        }

        [Test]
        public void Adversarial_DispatchOnEquip_WithEngravedAndNullActor_NoCrash()
        {
            var armor = MakeArmor();
            ItemEnhancing.Apply(armor, nameof(EnhancementEngraved), tier: 2);
            armor.GetPart<EnhancementEngraved>().Faction = "Villagers";
            int repBefore = PlayerReputation.Get("Villagers");

            Assert.DoesNotThrow(() => ItemEnhancementDispatch.DispatchOnEquip(
                actor: null, item: armor));
            Assert.AreEqual(repBefore, PlayerReputation.Get("Villagers"),
                "Null actor → Engraved gate rejects → no rep change.");
        }
    }
}
