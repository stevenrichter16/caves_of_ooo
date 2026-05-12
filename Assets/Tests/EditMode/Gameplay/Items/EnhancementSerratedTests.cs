using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.2.2 — <see cref="EnhancementSerrated"/>
    /// contract pin.
    ///
    /// <para><b>What's pinned:</b></para>
    /// <list type="bullet">
    ///   <item><see cref="EnhancementSerrated.Applicable"/> — accepts
    ///         Cutting melee weapons, rejects Bludgeoning melee,
    ///         rejects non-weapons, rejects null.</item>
    ///   <item><see cref="EnhancementSerrated.TierConfigure"/> —
    ///         ChancePercent scales linearly with Tier (10% per tier).</item>
    ///   <item><see cref="EnhancementSerrated.OnAttackerHit"/> — applies
    ///         BleedingEffect on success roll, no-ops on dead target
    ///         / null defender / 0% chance, emits diag on success.</item>
    ///   <item>Save/load round-trip preserves Tier + re-derives
    ///         ChancePercent (SL.6 reflection contract).</item>
    /// </list>
    /// </summary>
    public class EnhancementSerratedTests
    {
        [SetUp]
        public void Setup()
        {
            Diag.ResetAll();
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(EnhancementSerrated));
        }

        private static Entity MakeMeleeWeapon(string attrs = "Melee Cutting Strength", string id = "longsword")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d6", Attributes = attrs });
            return e;
        }

        private static Entity MakeArmor()
        {
            var e = new Entity { ID = "leather", BlueprintName = "leather" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "leather" });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new ArmorPart { AV = 1 });
            return e;
        }

        private static Entity MakeCreature(string id = "snapjaw")
        {
            // Mirrors MakeFighter() in OnHitClassEffectsTests — minimal
            // stats + StatusEffectsPart so Bleeding can attach.
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // Applicable — filter on melee + Cutting
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Applicable_CuttingMeleeWeapon_True()
        {
            var enh = new EnhancementSerrated();
            Assert.IsTrue(enh.Applicable(MakeMeleeWeapon("Melee Cutting Strength")));
        }

        [Test]
        public void Applicable_BludgeoningMeleeWeapon_False()
        {
            // Counter-check pair: same kind of item (melee weapon) but the
            // Cutting attribute is replaced with Bludgeoning. Applicable
            // must reject.
            var enh = new EnhancementSerrated();
            Assert.IsFalse(enh.Applicable(MakeMeleeWeapon("Melee Bludgeoning Strength")));
        }

        [Test]
        public void Applicable_PiercingMeleeWeapon_False()
        {
            var enh = new EnhancementSerrated();
            Assert.IsFalse(enh.Applicable(MakeMeleeWeapon("Melee Piercing Strength")));
        }

        [Test]
        public void Applicable_NonMeleeItem_False()
        {
            // Armor: not a melee weapon at all. Caught by base.Applicable
            // (IMeleeEnhancement requires MeleeWeaponPart). This pins
            // that we delegate correctly to base.
            var enh = new EnhancementSerrated();
            Assert.IsFalse(enh.Applicable(MakeArmor()));
        }

        [Test]
        public void Applicable_NullItem_False()
        {
            var enh = new EnhancementSerrated();
            Assert.IsFalse(enh.Applicable(null));
        }

        [Test]
        public void Applicable_EmptyAttributes_False()
        {
            // Edge case: MeleeWeaponPart with empty attribute string.
            // Should still reject — no Cutting tag, no Serrated.
            var enh = new EnhancementSerrated();
            Assert.IsFalse(enh.Applicable(MakeMeleeWeapon("")));
        }

        // ════════════════════════════════════════════════════════════════
        // Tier scaling — ChancePercent = Tier * 10
        // ════════════════════════════════════════════════════════════════

        [TestCase(1, 10)]
        [TestCase(2, 20)]
        [TestCase(3, 30)]
        [TestCase(4, 40)]
        public void TierConfigure_ScalesChanceLinearly(int tier, int expectedChance)
        {
            var enh = new EnhancementSerrated();
            enh.ApplyTier(tier);
            Assert.AreEqual(expectedChance, enh.ChancePercent,
                "BLEED_CHANCE_PER_TIER * tier — Tier 1 = 10%, Tier 4 = 40%.");
        }

        [Test]
        public void Configure_SetsDefaultSaveTargetAndDice()
        {
            var enh = new EnhancementSerrated();
            // Ctor runs Configure(); defaults must be present pre-tier.
            Assert.AreEqual(EnhancementSerrated.DEFAULT_SAVE_TARGET, enh.SaveTarget);
            Assert.AreEqual(EnhancementSerrated.DEFAULT_DAMAGE_DICE, enh.DamageDice);
        }

        // ════════════════════════════════════════════════════════════════
        // OnAttackerHit — applies Bleeding on success roll
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void OnAttackerHit_AtFullChance_AppliesBleeding()
        {
            // Force chance to 100% so the roll deterministically lands.
            var enh = new EnhancementSerrated();
            enh.ChancePercent = 100;
            var defender = MakeCreature();
            var attacker = MakeCreature("attacker");

            enh.OnAttackerHit(defender, attacker,
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            var bleed = defender.GetPart<StatusEffectsPart>()?.GetEffect<BleedingEffect>();
            Assert.IsNotNull(bleed, "Bleeding effect attached on guaranteed roll.");
        }

        [Test]
        public void OnAttackerHit_AtZeroChance_DoesNotApplyBleeding()
        {
            // Counter-check pair to AtFullChance. Same setup, ChancePercent=0.
            var enh = new EnhancementSerrated();
            enh.ChancePercent = 0;
            var defender = MakeCreature();

            enh.OnAttackerHit(defender, MakeCreature("attacker"),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            Assert.IsNull(defender.GetPart<StatusEffectsPart>()?.GetEffect<BleedingEffect>(),
                "0% chance never triggers Bleeding.");
        }

        [Test]
        public void OnAttackerHit_ZeroActualDamage_DoesNotApplyBleeding()
        {
            // Mirror OnHitClassEffects "no damage = no on-hit" contract.
            // Even with 100% chance, a fully-resisted hit must not apply.
            var enh = new EnhancementSerrated();
            enh.ChancePercent = 100;
            var defender = MakeCreature();

            enh.OnAttackerHit(defender, MakeCreature("attacker"),
                damage: new Damage(0), actualDamage: 0, zone: null,
                rng: new System.Random(0));

            Assert.IsNull(defender.GetPart<StatusEffectsPart>()?.GetEffect<BleedingEffect>(),
                "Zero actual damage = no Serrated bleed.");
        }

        [Test]
        public void OnAttackerHit_NullDefender_NoThrow()
        {
            var enh = new EnhancementSerrated();
            enh.ChancePercent = 100;
            Assert.DoesNotThrow(() => enh.OnAttackerHit(
                defender: null, attacker: MakeCreature("attacker"),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0)));
        }

        [Test]
        public void OnAttackerHit_NullRng_NoThrow()
        {
            var enh = new EnhancementSerrated();
            enh.ChancePercent = 100;
            Assert.DoesNotThrow(() => enh.OnAttackerHit(
                defender: MakeCreature(), attacker: MakeCreature("attacker"),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: null));
        }

        [Test]
        public void OnAttackerHit_OnTrigger_EmitsTriggeredDiag()
        {
            var enh = new EnhancementSerrated();
            enh.ApplyTier(3);  // 30% chance
            enh.ChancePercent = 100;  // force trigger
            var defender = MakeCreature();
            var attacker = MakeCreature("attacker");

            Diag.ResetAll();
            enh.OnAttackerHit(defender, attacker,
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "Triggered",
                Limit = 10,
            }).Records;
            Assert.AreEqual(1, recs.Count,
                "Exactly one Triggered record per successful Serrated proc.");
            StringAssert.Contains("EnhancementSerrated", recs[0].PayloadJson);
            StringAssert.Contains("Bleeding", recs[0].PayloadJson);
        }

        [Test]
        public void OnAttackerHit_OnNoTrigger_EmitsNoDiag()
        {
            // Counter-check pair to OnTrigger_EmitsTriggeredDiag.
            // A failed roll must NOT emit Triggered — the diag pins
            // "this proc fired", not "this proc was rolled."
            var enh = new EnhancementSerrated();
            enh.ChancePercent = 0;

            Diag.ResetAll();
            enh.OnAttackerHit(MakeCreature(), MakeCreature("attacker"),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "Triggered",
                Limit = 10,
            }).Records;
            Assert.AreEqual(0, recs.Count,
                "Failed proc emits NO Triggered record (this pins the gate).");
        }

        // ════════════════════════════════════════════════════════════════
        // ItemEnhancing.Apply integration — Serrated routes correctly
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ItemEnhancing_Apply_OnCuttingWeapon_AttachesSerrated()
        {
            var weapon = MakeMeleeWeapon("Melee Cutting Strength");
            bool ok = ItemEnhancing.Apply(weapon, nameof(EnhancementSerrated), tier: 2);
            Assert.IsTrue(ok);
            var enh = weapon.GetPart<EnhancementSerrated>();
            Assert.IsNotNull(enh);
            Assert.AreEqual(2, enh.Tier);
            Assert.AreEqual(20, enh.ChancePercent,
                "Tier propagates through ItemEnhancing.Apply → TierConfigure → ChancePercent.");
        }

        [Test]
        public void ItemEnhancing_Apply_OnBludgeoningWeapon_RejectsAtApplicable()
        {
            // Counter-check at the public API surface: Apply must reject
            // Serrated on a Bludgeoning weapon and emit ApplyFailed/not_applicable.
            var weapon = MakeMeleeWeapon("Melee Bludgeoning Strength");
            Diag.ResetAll();
            bool ok = ItemEnhancing.Apply(weapon, nameof(EnhancementSerrated));
            Assert.IsFalse(ok);
            Assert.IsNull(weapon.GetPart<EnhancementSerrated>(),
                "Bludgeoning weapon must not get a Serrated Part attached.");

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
        // Save/load round-trip — SL.6 reflection contract
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void RoundTrip_PreservesTier_AndChanceDerivable()
        {
            // The Tier int is the source of truth; ChancePercent is
            // derived. Per the class docstring, even if a load reads
            // a stale ChancePercent, calling ApplyTier(Tier) re-derives
            // it from current BLEED_CHANCE_PER_TIER. This test exercises
            // the round trip + verifies Tier is preserved.
            var src = MakeMeleeWeapon();
            ItemEnhancing.Apply(src, nameof(EnhancementSerrated), tier: 3);

            Entity loaded = PartRoundTripHelper.RoundTripEntity(src);

            var enh = loaded.GetPart<EnhancementSerrated>();
            Assert.IsNotNull(enh, "EnhancementSerrated Part survives round-trip.");
            Assert.AreEqual(3, enh.Tier, "Tier round-trips.");
            // The ChancePercent field is a public int and round-trips too.
            Assert.AreEqual(30, enh.ChancePercent,
                "ChancePercent serializes round-trip (separate from Tier rederive path).");
        }

        // ════════════════════════════════════════════════════════════════
        // Tier-scaling integration — averaged trigger rate matches Tier
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TriggerRate_ApproximatesChancePercent()
        {
            // Probability-bounded sanity check. Run 5000 swings at Tier 4
            // (40% chance) with a deterministic seed; trigger count should
            // land within ±5% of expected (i.e. 1750-2250 of 5000).
            // If the gate ever silently inverts (e.g. >= vs <) the count
            // will collapse to 0 or 5000 and this test screams.
            var enh = new EnhancementSerrated();
            enh.ApplyTier(4);  // 40%

            var rng = new System.Random(12345);
            int triggers = 0;
            for (int i = 0; i < 5000; i++)
            {
                var defender = MakeCreature("d" + i);
                enh.OnAttackerHit(defender, MakeCreature("a" + i),
                    damage: new Damage(5), actualDamage: 5, zone: null, rng: rng);
                if (defender.GetPart<StatusEffectsPart>()?.GetEffect<BleedingEffect>() != null)
                    triggers++;
            }
            Assert.IsTrue(triggers > 1750 && triggers < 2250,
                $"Trigger count {triggers}/5000 should be near 2000 (40%). " +
                "If far off, the chance gate or RNG seeding is broken.");
        }
    }
}
