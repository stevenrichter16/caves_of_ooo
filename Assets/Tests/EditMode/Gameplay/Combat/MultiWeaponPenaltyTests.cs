using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase G — multiweapon-fighting hook for the off-hand penalty.
    ///
    /// Qud reference: <c>GetMeleeAttackChanceEvent</c> at Combat.cs:775 lets
    /// skill parts modify the per-attack chance for off-hand swings. We don't
    /// have a skill system, but we mirror the intent with a stat-driven hook.
    ///
    /// User-visible invariants:
    ///
    ///   1. An attacker's <c>MultiWeaponSkillBonus</c> stat adds to the
    ///      off-hand hit-bonus. Stat=0 keeps the existing −2 penalty.
    ///      Stat=2 cancels the penalty. Stat=5 over-corrects to +3.
    ///
    ///   2. Primary hand swings are unaffected by <c>MultiWeaponSkillBonus</c>.
    ///
    ///   3. Negative <c>MultiWeaponSkillBonus</c> stacks WITH the −2 penalty
    ///      (e.g., a cursed weapon or "Clumsy" effect could push off-hand
    ///      to −5).
    ///
    /// Counter-checks (Methodology Template §3.4):
    ///   • Stat exists but value is 0 → identical to no-stat behavior
    ///   • Two-handed weapon (only one slot) → no off-hand to test, no
    ///     stat impact
    /// </summary>
    public class MultiWeaponPenaltyTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ====================================================================
        // 1. Default behavior preserved when stat absent
        // ====================================================================

        [Test]
        public void OffHandPenalty_NoStat_ReturnsBasePenalty()
        {
            var attacker = MakeFighter();
            int penalty = CombatSystem.GetOffHandHitBonus(attacker);
            Assert.AreEqual(CombatSystem.OFF_HAND_HIT_PENALTY, penalty,
                "Default off-hand penalty equals OFF_HAND_HIT_PENALTY constant");
        }

        // ====================================================================
        // 2. Stat-zero is identical to no-stat (counter-check)
        // ====================================================================

        [Test]
        public void OffHandPenalty_StatZero_IdenticalToNoStat()
        {
            var attacker = MakeFighter();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 0, Min = -10, Max = 10 };
            int penalty = CombatSystem.GetOffHandHitBonus(attacker);
            Assert.AreEqual(CombatSystem.OFF_HAND_HIT_PENALTY, penalty,
                "MultiWeaponSkillBonus=0 must yield the same result as having no stat");
        }

        // ====================================================================
        // 3. Stat = +2 cancels the −2 penalty
        // ====================================================================

        [Test]
        public void OffHandPenalty_StatTwo_CancelsBasePenalty()
        {
            var attacker = MakeFighter();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 2, Min = -10, Max = 10 };
            int penalty = CombatSystem.GetOffHandHitBonus(attacker);
            Assert.AreEqual(0, penalty,
                "Stat=+2 should fully cancel the −2 base penalty");
        }

        // ====================================================================
        // 4. Stat over-correction yields a positive bonus
        // ====================================================================

        [Test]
        public void OffHandPenalty_StatFive_YieldsPositiveBonus()
        {
            var attacker = MakeFighter();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 5, Min = -10, Max = 10 };
            int penalty = CombatSystem.GetOffHandHitBonus(attacker);
            Assert.AreEqual(3, penalty,
                "Stat=+5 should over-correct: −2 + 5 = +3");
        }

        // ====================================================================
        // 5. Negative stat stacks with base penalty (debuffs)
        // ====================================================================

        [Test]
        public void OffHandPenalty_NegativeStat_StacksWithBasePenalty()
        {
            var attacker = MakeFighter();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = -3, Min = -10, Max = 10 };
            int penalty = CombatSystem.GetOffHandHitBonus(attacker);
            Assert.AreEqual(-5, penalty,
                "Stat=-3 should stack: −2 base + −3 stat = −5");
        }

        // ====================================================================
        // 6. Integration — primary hand unaffected, off-hand penalty applied
        //    in the actual attack flow
        // ====================================================================

        [Test]
        public void Integration_OffHandStatBonus_PropagatesToPerformSingleAttack()
        {
            // Setup: attacker with two equipped weapons. Boost
            // MultiWeaponSkillBonus high enough that even at unfavorable
            // hit rolls, off-hand swings should land at least sometimes.
            var zone = new Zone();
            var attacker = MakeFighterWithBody(strength: 16, agility: 16);
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 5, Min = -10, Max = 10 };
            zone.AddEntity(attacker, 5, 5);

            var hands = GetHands(attacker);
            Assert.AreEqual(2, hands.Count, "Test setup: humanoid must have 2 hands");

            var primaryWpn = MakeWeapon("primary_blade", "1d4");
            var offHandWpn = MakeWeapon("offhand_blade", "1d4");
            var inv = attacker.GetPart<InventoryPart>();
            inv.EquipToBodyPart(primaryWpn, hands[0]);
            inv.EquipToBodyPart(offHandWpn, hands[1]);

            var defender = MakeFighterWithBody(strength: 10, agility: 10);
            defender.GetPart<ArmorPart>().AV = 0;
            defender.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(defender, 6, 5);

            int offHandHitsLanded = 0;
            for (int seed = 0; seed < 30; seed++)
            {
                MessageLog.Clear();
                defender.SetStatValue("Hitpoints", 9999);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(seed));
                foreach (var msg in MessageLog.GetRecent(20))
                {
                    if (msg.Contains("offhand_blade") && msg.Contains("hits"))
                    {
                        offHandHitsLanded++;
                        break;
                    }
                }
            }

            Assert.Greater(offHandHitsLanded, 0,
                "With MultiWeaponSkillBonus=+5, off-hand swings should land at least sometimes");
        }

        // ====================================================================
        // 6b. Counter-check: primary-hand swings are NOT affected by
        //     MultiWeaponSkillBonus (the stat is off-hand-only).
        //
        //     Strategy: give the attacker a NEGATIVE stat large enough that,
        //     if it accidentally applied to the primary hand, primary swings
        //     would miss most of the time. Then verify primary hits land
        //     reliably while off-hand swings miss (stat correctly off-hand-only).
        // ====================================================================

        [Test]
        public void Integration_PrimaryHand_NotAffectedByMultiWeaponSkillBonus()
        {
            var zone = new Zone();
            var attacker = MakeFighterWithBody(strength: 16, agility: 16);
            // Crippling stat — would push hit roll to 1d20 - 50, missing virtually
            // every swing IF it accidentally applied to the primary hand.
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = -50, Min = -100, Max = 100 };
            zone.AddEntity(attacker, 5, 5);

            var hands = GetHands(attacker);
            var primaryWpn = MakeWeapon("primary_blade", "1d4");
            var offHandWpn = MakeWeapon("offhand_blade", "1d4");
            var inv = attacker.GetPart<InventoryPart>();
            inv.EquipToBodyPart(primaryWpn, hands[0]);
            inv.EquipToBodyPart(offHandWpn, hands[1]);

            var defender = MakeFighterWithBody(strength: 10, agility: 10);
            defender.GetPart<ArmorPart>().AV = 0;
            defender.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(defender, 6, 5);

            int primaryHits = 0;
            int offHandHits = 0;
            for (int seed = 0; seed < 30; seed++)
            {
                MessageLog.Clear();
                defender.SetStatValue("Hitpoints", 9999);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(seed));
                foreach (var msg in MessageLog.GetRecent(20))
                {
                    if (msg.Contains("primary_blade") && msg.Contains("hits") && !msg.Contains("misses"))
                        primaryHits++;
                    if (msg.Contains("offhand_blade") && msg.Contains("hits") && !msg.Contains("misses"))
                        offHandHits++;
                }
            }

            // Primary swings have HitBonus=30, so they hit reliably regardless.
            // Off-hand swings get an additional -50, so they should miss most of the time.
            Assert.Greater(primaryHits, 20,
                $"Primary swings must NOT be affected by the off-hand stat (got {primaryHits} hits)");
            Assert.Less(offHandHits, primaryHits,
                $"Off-hand swings SHOULD be affected by the stat (got {offHandHits} off-hand hits vs {primaryHits} primary)");
        }

        // ====================================================================
        // 7. Adversarial: extreme stat values don't cause overflow
        // ====================================================================

        [Test]
        public void OffHandPenalty_LargeStat_NoOverflow()
        {
            var attacker = MakeFighter();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 1000, Min = -10000, Max = 10000 };
            int penalty = CombatSystem.GetOffHandHitBonus(attacker);
            // OFF_HAND_HIT_PENALTY (-2) + 1000 = 998. No overflow.
            Assert.AreEqual(998, penalty,
                "Large stat values must not overflow; arithmetic stays sane");
        }

        // ====================================================================
        // 8. Adversarial: null attacker doesn't crash
        // ====================================================================

        [Test]
        public void OffHandPenalty_NullAttacker_ReturnsBasePenalty()
        {
            int penalty = CombatSystem.GetOffHandHitBonus(null);
            Assert.AreEqual(CombatSystem.OFF_HAND_HIT_PENALTY, penalty,
                "Null attacker should fall back to base penalty (no crash)");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity MakeFighter()
        {
            var entity = new Entity();
            entity.BlueprintName = "TestFighter";
            entity.Tags["Creature"] = "";
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            return entity;
        }

        private Entity MakeFighterWithBody(int strength, int agility, int hp = 100)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestFighter";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = agility, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Owner = entity, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "fighter" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            var body = new Body();
            entity.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return entity;
        }

        private Entity MakeWeapon(string name, string damage)
        {
            var entity = new Entity();
            entity.BlueprintName = name;
            entity.Tags["Item"] = "";
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = damage, PenBonus = 5, HitBonus = 30 });
            entity.AddPart(new EquippablePart { Slot = "Hand" });
            return entity;
        }

        private System.Collections.Generic.List<BodyPart> GetHands(Entity entity)
        {
            var body = entity.GetPart<Body>();
            var all = body.GetParts();
            var hands = new System.Collections.Generic.List<BodyPart>();
            for (int i = 0; i < all.Count; i++)
                if (all[i].Type == "Hand") hands.Add(all[i]);
            return hands;
        }
    }
}
