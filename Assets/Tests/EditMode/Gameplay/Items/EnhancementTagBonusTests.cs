using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.3.3 — combined contract pin for
    /// <see cref="EnhancementPaleSalt"/> (vs Undead) and
    /// <see cref="EnhancementChoirIron"/> (vs Fungal) plus their
    /// shared base <see cref="EnhancementTagBonusBase"/>.
    ///
    /// <para><b>What's pinned (parametrized across both concrete classes):</b></para>
    /// <list type="bullet">
    ///   <item>Applicable: melee-weapon-only (inherits IMeleeEnhancement).</item>
    ///   <item>TargetMaterialTag: "Undead" for PaleSalt, "Fungal" for ChoirIron.</item>
    ///   <item>TierConfigure: BonusDamage = Tier * 2.</item>
    ///   <item>OnAttackerHit on tagged defender: applies BonusDamage.</item>
    ///   <item>OnAttackerHit on UN-tagged defender: NO bonus (counter-check).</item>
    ///   <item>OnAttackerHit on actualDamage=0: NO bonus (fully-resisted hits).</item>
    ///   <item>Diag "Triggered" emitted on success; NOT emitted on no-hit.</item>
    ///   <item>Save/load preserves Tier + BonusDamage.</item>
    /// </list>
    /// </summary>
    public class EnhancementTagBonusTests
    {
        [SetUp]
        public void Setup()
        {
            Diag.ResetAll();
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(EnhancementPaleSalt));
            EnhancementFactory.Register(typeof(EnhancementChoirIron));
        }

        private static Entity MakeCuttingWeapon()
        {
            var e = new Entity { ID = "longsword", BlueprintName = "longsword" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "longsword" });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d6", Attributes = "Melee Cutting" });
            return e;
        }

        private static Entity MakeArmor()
        {
            var e = new Entity { ID = "leather", BlueprintName = "leather" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "leather" });
            e.AddPart(new ArmorPart { AV = 1 });
            return e;
        }

        private static Entity MakeTaggedCreature(string materialTag, int hp = 50, string id = "target")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 8, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new MaterialPart { MaterialTagsRaw = materialTag });
            // Force MaterialTagsRaw → MaterialTags via Initialize.
            e.GetPart<MaterialPart>().Initialize();
            return e;
        }

        private static Entity MakeAttacker() => MakeTaggedCreature("Organic", hp: 100, id: "attacker");

        // ════════════════════════════════════════════════════════════════
        // Applicable — both reject non-melee items
        // ════════════════════════════════════════════════════════════════

        [TestCase(typeof(EnhancementPaleSalt))]
        [TestCase(typeof(EnhancementChoirIron))]
        public void Applicable_OnMeleeWeapon_True(System.Type t)
        {
            var enh = (EnhancementTagBonusBase)System.Activator.CreateInstance(t);
            Assert.IsTrue(enh.Applicable(MakeCuttingWeapon()));
        }

        [TestCase(typeof(EnhancementPaleSalt))]
        [TestCase(typeof(EnhancementChoirIron))]
        public void Applicable_OnArmor_False(System.Type t)
        {
            // Counter-check: armor isn't a melee weapon → both reject.
            var enh = (EnhancementTagBonusBase)System.Activator.CreateInstance(t);
            Assert.IsFalse(enh.Applicable(MakeArmor()));
        }

        // ════════════════════════════════════════════════════════════════
        // TargetMaterialTag — concrete classes hardcode the right tag
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void PaleSalt_TargetTag_IsUndead()
        {
            Assert.AreEqual("Undead", new EnhancementPaleSalt().TargetMaterialTag);
        }

        [Test]
        public void ChoirIron_TargetTag_IsFungal()
        {
            Assert.AreEqual("Fungal", new EnhancementChoirIron().TargetMaterialTag);
        }

        // ════════════════════════════════════════════════════════════════
        // Tier scaling (shared base)
        // ════════════════════════════════════════════════════════════════

        [TestCase(1, 2)]
        [TestCase(2, 4)]
        [TestCase(3, 6)]
        [TestCase(4, 8)]
        public void PaleSalt_TierConfigure_ScalesBonusDamage(int tier, int expected)
        {
            var enh = new EnhancementPaleSalt();
            enh.ApplyTier(tier);
            Assert.AreEqual(expected, enh.BonusDamage);
        }

        [TestCase(1, 2)]
        [TestCase(4, 8)]
        public void ChoirIron_TierConfigure_ScalesBonusDamage(int tier, int expected)
        {
            var enh = new EnhancementChoirIron();
            enh.ApplyTier(tier);
            Assert.AreEqual(expected, enh.BonusDamage);
        }

        // ════════════════════════════════════════════════════════════════
        // OnAttackerHit — Pale-Salt on Undead applies bonus damage
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void PaleSalt_OnUndeadDefender_AppliesBonusDamage()
        {
            var enh = new EnhancementPaleSalt();
            enh.ApplyTier(3);  // +6 bonus
            var defender = MakeTaggedCreature("Bone,Dry,Undead");
            int hpBefore = defender.GetStatValue("Hitpoints");

            enh.OnAttackerHit(defender, MakeAttacker(),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            int hpAfter = defender.GetStatValue("Hitpoints");
            Assert.AreEqual(hpBefore - 6, hpAfter,
                "Tier-3 Pale-Salt on Undead defender deals +6 bonus damage.");
        }

        [Test]
        public void PaleSalt_OnNonUndeadDefender_NoBonusDamage()
        {
            // Counter-check pair to OnUndeadDefender_AppliesBonusDamage.
            var enh = new EnhancementPaleSalt();
            enh.ApplyTier(3);
            var defender = MakeTaggedCreature("Organic,Living");  // not Undead
            int hpBefore = defender.GetStatValue("Hitpoints");

            enh.OnAttackerHit(defender, MakeAttacker(),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Non-Undead defender takes no Pale-Salt bonus.");
        }

        // ════════════════════════════════════════════════════════════════
        // OnAttackerHit — Choir-Iron on Fungal applies bonus damage
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ChoirIron_OnFungalDefender_AppliesBonusDamage()
        {
            var enh = new EnhancementChoirIron();
            enh.ApplyTier(2);  // +4 bonus
            var defender = MakeTaggedCreature("Organic,Fungal,Living");
            int hpBefore = defender.GetStatValue("Hitpoints");

            enh.OnAttackerHit(defender, MakeAttacker(),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            Assert.AreEqual(hpBefore - 4, defender.GetStatValue("Hitpoints"));
        }

        [Test]
        public void ChoirIron_OnNonFungalDefender_NoBonusDamage()
        {
            var enh = new EnhancementChoirIron();
            enh.ApplyTier(2);
            var defender = MakeTaggedCreature("Bone,Dry,Undead");  // Undead, not Fungal
            int hpBefore = defender.GetStatValue("Hitpoints");

            enh.OnAttackerHit(defender, MakeAttacker(),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Non-Fungal defender takes no Choir-Iron bonus.");
        }

        // ════════════════════════════════════════════════════════════════
        // OnAttackerHit — zero actualDamage gates the bonus
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void TagBonus_ZeroActualDamage_NoBonusEvenOnTaggedDefender()
        {
            // Pin: a fully-resisted primary hit (actualDamage=0) must
            // NOT still trigger the bonus. Mirrors OnHitClassEffects.
            var enh = new EnhancementPaleSalt();
            enh.ApplyTier(4);
            var defender = MakeTaggedCreature("Bone,Undead");
            int hpBefore = defender.GetStatValue("Hitpoints");

            enh.OnAttackerHit(defender, MakeAttacker(),
                damage: new Damage(0), actualDamage: 0, zone: null,
                rng: new System.Random(0));

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Fully-resisted hit blocks the bonus too.");
        }

        // ════════════════════════════════════════════════════════════════
        // OnAttackerHit — defender without MaterialPart is a no-op
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void TagBonus_DefenderWithoutMaterialPart_NoBonus_NoCrash()
        {
            // Some defenders don't have a MaterialPart (e.g. pure-Stats
            // creatures). The bonus check must short-circuit safely.
            var defender = new Entity { ID = "ghost", BlueprintName = "ghost" };
            defender.Tags["Creature"] = "";
            defender.Statistics["Hitpoints"] = new Stat
                { Owner = defender, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            defender.AddPart(new RenderPart { DisplayName = "ghost" });
            defender.AddPart(new StatusEffectsPart());

            var enh = new EnhancementPaleSalt();
            enh.ApplyTier(4);

            Assert.DoesNotThrow(() => enh.OnAttackerHit(
                defender, MakeAttacker(),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0)));

            Assert.AreEqual(50, defender.GetStatValue("Hitpoints"),
                "Missing MaterialPart → no bonus, no crash.");
        }

        [Test]
        public void TagBonus_NullDefender_NoCrash()
        {
            var enh = new EnhancementPaleSalt();
            enh.ApplyTier(4);
            Assert.DoesNotThrow(() => enh.OnAttackerHit(
                defender: null, attacker: MakeAttacker(),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0)));
        }

        // ════════════════════════════════════════════════════════════════
        // Diag emission — Triggered on success, none on miss
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void TagBonus_OnTrigger_EmitsTriggeredDiag()
        {
            var enh = new EnhancementChoirIron();
            enh.ApplyTier(2);
            var defender = MakeTaggedCreature("Fungal");
            Diag.ResetAll();

            enh.OnAttackerHit(defender, MakeAttacker(),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "Triggered",
                Limit = 5,
            }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("EnhancementChoirIron", recs[0].PayloadJson);
            StringAssert.Contains("Fungal", recs[0].PayloadJson);
        }

        [Test]
        public void TagBonus_OnNonTriggerDefender_EmitsNoDiag()
        {
            // Counter-check pair: a non-Fungal defender should NOT emit
            // a Triggered record (else queries lose grep-precision).
            var enh = new EnhancementChoirIron();
            enh.ApplyTier(2);
            var defender = MakeTaggedCreature("Bone,Undead");
            Diag.ResetAll();

            enh.OnAttackerHit(defender, MakeAttacker(),
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = ItemEnhancing.DIAG_CATEGORY,
                Kind = "Triggered",
                Limit = 5,
            }).Records;
            Assert.AreEqual(0, recs.Count,
                "Missed bonus emits no Triggered record.");
        }

        // ════════════════════════════════════════════════════════════════
        // Save/load round-trip
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void PaleSalt_RoundTrip_PreservesTierAndBonus()
        {
            var weapon = MakeCuttingWeapon();
            ItemEnhancing.Apply(weapon, nameof(EnhancementPaleSalt), tier: 3);

            Entity loaded = PartRoundTripHelper.RoundTripEntity(weapon);

            var enh = loaded.GetPart<EnhancementPaleSalt>();
            Assert.IsNotNull(enh);
            Assert.AreEqual(3, enh.Tier);
            Assert.AreEqual(6, enh.BonusDamage);
            Assert.AreEqual("Undead", enh.TargetMaterialTag);
        }

        [Test]
        public void ChoirIron_RoundTrip_PreservesTierAndBonus()
        {
            var weapon = MakeCuttingWeapon();
            ItemEnhancing.Apply(weapon, nameof(EnhancementChoirIron), tier: 2);

            Entity loaded = PartRoundTripHelper.RoundTripEntity(weapon);

            var enh = loaded.GetPart<EnhancementChoirIron>();
            Assert.IsNotNull(enh);
            Assert.AreEqual(2, enh.Tier);
            Assert.AreEqual(4, enh.BonusDamage);
            Assert.AreEqual("Fungal", enh.TargetMaterialTag);
        }

        // ════════════════════════════════════════════════════════════════
        // ItemEnhancing.Apply integration
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ItemEnhancing_Apply_OnMeleeWeapon_Attaches()
        {
            var weapon = MakeCuttingWeapon();
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementPaleSalt), tier: 2));
            Assert.IsTrue(ItemEnhancing.Apply(weapon, nameof(EnhancementChoirIron), tier: 2));
            // Slot cap = 2, both took.
            Assert.AreEqual(2, ItemEnhancing.CountEnhancements(weapon));
        }

        [Test]
        public void ItemEnhancing_Apply_OnArmor_RejectsAtApplicable()
        {
            Diag.ResetAll();
            bool ok = ItemEnhancing.Apply(MakeArmor(), nameof(EnhancementPaleSalt));
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
    }
}
