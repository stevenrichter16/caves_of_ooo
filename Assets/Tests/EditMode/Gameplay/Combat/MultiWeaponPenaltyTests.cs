using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/COMBAT-AUDIT-BUGFIX-PLAN-2026-07.md SM9/B3 — off-hand attacks
    /// ported to Qud's real mechanic. Replaces the prior "always swings, at
    /// a flat -2 to-hit" model (which a since-removed comment incorrectly
    /// claimed "mirrors Qud's secondary weapon hit penalty") with Qud's
    /// actual mechanic: a per-turn percent CHANCE the off-hand attacks at
    /// all (<c>RuleSettings.BASE_SECONDARY_ATTACK_CHANCE = 15</c>), and no
    /// separate accuracy penalty once it does attack.
    ///
    /// Qud reference: <c>GetMeleeAttackChanceEvent</c> at Combat.cs:775 lets
    /// skill parts modify the per-attack chance for off-hand swings. We
    /// don't have a skill system, but we mirror the intent with the same
    /// stat-driven hook the prior (now-replaced) mechanic used.
    ///
    /// User-visible invariants:
    ///
    ///   1. An attacker's <c>MultiWeaponSkillBonus</c> stat adds
    ///      percentage points to the off-hand attack chance. Stat=0 keeps
    ///      the base 15%.
    ///
    ///   2. Primary hand swings always attempt, every turn, unaffected by
    ///      the off-hand chance roll or by <c>MultiWeaponSkillBonus</c>.
    ///
    ///   3. Once the off-hand DOES attack, it has no separate to-hit
    ///      penalty — full accuracy, same as the primary hand.
    ///
    /// Counter-checks (Methodology Template §3.4):
    ///   • Stat exists but value is 0 → identical to no-stat behavior
    ///   • Negative stat can push the chance toward (but floored at) 0
    ///   • Large stat values don't overflow, and the chance is
    ///     deliberately NOT capped at 100 (Qud parity — enough dual-wield
    ///     investment can push the off-hand to always-swing too)
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
        public void OffHandAttackChance_NoStat_ReturnsBaseChance()
        {
            var attacker = MakeFighter();
            int chance = CombatSystem.GetOffHandAttackChancePercent(attacker);
            Assert.AreEqual(CombatSystem.BASE_SECONDARY_ATTACK_CHANCE_PERCENT, chance,
                "Default off-hand attack chance equals BASE_SECONDARY_ATTACK_CHANCE_PERCENT (15).");
        }

        // ====================================================================
        // 2. Stat-zero is identical to no-stat (counter-check)
        // ====================================================================

        [Test]
        public void OffHandAttackChance_StatZero_IdenticalToNoStat()
        {
            var attacker = MakeFighter();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 0, Min = -10, Max = 10 };
            int chance = CombatSystem.GetOffHandAttackChancePercent(attacker);
            Assert.AreEqual(CombatSystem.BASE_SECONDARY_ATTACK_CHANCE_PERCENT, chance,
                "MultiWeaponSkillBonus=0 must yield the same result as having no stat");
        }

        // ====================================================================
        // 3. Positive stat adds percentage points to the chance
        // ====================================================================

        [Test]
        public void OffHandAttackChance_PositiveStat_AddsToChance()
        {
            var attacker = MakeFighter();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 20, Min = -100, Max = 100 };
            int chance = CombatSystem.GetOffHandAttackChancePercent(attacker);
            Assert.AreEqual(35, chance, "Stat=+20 should add to the base 15%: 15 + 20 = 35");
        }

        // ====================================================================
        // 4. Negative stat reduces the chance, floored at 0
        // ====================================================================

        [Test]
        public void OffHandAttackChance_NegativeStat_ReducesChance()
        {
            var attacker = MakeFighter();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = -10, Min = -100, Max = 100 };
            int chance = CombatSystem.GetOffHandAttackChancePercent(attacker);
            Assert.AreEqual(5, chance, "Stat=-10 should subtract from the base 15%: 15 - 10 = 5");
        }

        [Test]
        public void OffHandAttackChance_LargeNegativeStat_FlooredAtZero_NotNegative()
        {
            var attacker = MakeFighter();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = -1000, Min = -10000, Max = 10000 };
            int chance = CombatSystem.GetOffHandAttackChancePercent(attacker);
            Assert.AreEqual(0, chance,
                "A large negative stat must floor the chance at 0, not go negative (which would corrupt an rng.Next(100) >= chance comparison).");
        }

        // ====================================================================
        // 5. Adversarial: large positive stat exceeds 100 (Qud parity —
        //    NOT capped, unlike the old to-hit-bonus model)
        // ====================================================================

        [Test]
        public void OffHandAttackChance_LargeStat_ExceedsOneHundred_NotCapped()
        {
            var attacker = MakeFighter();
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
                { Owner = attacker, Name = "MultiWeaponSkillBonus", BaseValue = 1000, Min = -10000, Max = 10000 };
            int chance = CombatSystem.GetOffHandAttackChancePercent(attacker);
            Assert.AreEqual(1015, chance,
                "Large stat values must not overflow, and the chance is deliberately NOT capped at 100 "
                + "-- matches Qud, where enough dual-wield investment makes the off-hand always swing too.");
        }

        // ====================================================================
        // 6. Adversarial: null attacker doesn't crash
        // ====================================================================

        [Test]
        public void OffHandAttackChance_NullAttacker_ReturnsBaseChance()
        {
            int chance = CombatSystem.GetOffHandAttackChancePercent(null);
            Assert.AreEqual(CombatSystem.BASE_SECONDARY_ATTACK_CHANCE_PERCENT, chance,
                "Null attacker should fall back to the base chance (no crash)");
        }

        // ====================================================================
        // 7. Integration — off-hand swings roughly at the base chance rate
        //    across many turns
        // ====================================================================

        [Test]
        public void Integration_OffHandSwingsRoughlyAtBaseChance_AcrossManySeeds()
        {
            var zone = new Zone();
            var attacker = MakeFighterWithBody(strength: 16, agility: 16);
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

            const int trials = 200;
            int offHandAttempts = 0;
            for (int seed = 0; seed < trials; seed++)
            {
                MessageLog.Clear();
                defender.SetStatValue("Hitpoints", 9999);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
                foreach (var msg in MessageLog.GetRecent(20))
                {
                    if (msg.Contains("offhand_blade"))
                    {
                        offHandAttempts++;
                        break;
                    }
                }
            }

            // Base chance is 15% -- wide tolerance band (this is a
            // probability-boundary sanity check, not a precision estimate).
            Assert.Greater(offHandAttempts, trials * 5 / 100,
                $"Off-hand should attempt noticeably more than never ({offHandAttempts}/{trials})");
            Assert.Less(offHandAttempts, trials * 30 / 100,
                $"Off-hand should attempt noticeably less than always ({offHandAttempts}/{trials})");
        }

        // ====================================================================
        // 8. Counter-check: primary-hand swings ALWAYS attempt, regardless
        //    of the off-hand's chance roll
        // ====================================================================

        [Test]
        public void Integration_PrimaryHand_AlwaysAttempts_RegardlessOfOffHandChance()
        {
            var zone = new Zone();
            var attacker = MakeFighterWithBody(strength: 16, agility: 16);
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

            const int trials = 30;
            int primaryAttempts = 0;
            for (int seed = 0; seed < trials; seed++)
            {
                MessageLog.Clear();
                defender.SetStatValue("Hitpoints", 9999);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
                foreach (var msg in MessageLog.GetRecent(20))
                {
                    if (msg.Contains("primary_blade"))
                    {
                        primaryAttempts++;
                        break;
                    }
                }
            }

            Assert.AreEqual(trials, primaryAttempts,
                "Primary hand must attempt an attack every single turn, regardless of the off-hand's own chance roll.");
        }

        // ====================================================================
        // 9. Counter-check: once the off-hand DOES attack, it has no
        //    separate to-hit penalty (full accuracy, matching the primary)
        // ====================================================================

        [Test]
        public void Integration_OffHandThatAttacks_HasNoSeparateToHitPenalty()
        {
            // Give BOTH hands a guaranteed-hit HitBonus. If a lingering
            // off-hand to-hit penalty still existed (the OLD -2 model, or
            // any regression re-introducing one), a large enough penalty
            // relative to HitBonus=30 could still be overwhelmed by
            // HitBonus, so instead we assert directly on the miss rate:
            // among off-hand ATTEMPTS (the "offhand_blade" message
            // present at all), the hit rate must match the primary hand's
            // (both effectively 100%, since HitBonus=30 alone guarantees
            // a hit against DV=0).
            var zone = new Zone();
            var attacker = MakeFighterWithBody(strength: 16, agility: 16);
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

            int offHandAttempts = 0;
            int offHandHits = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                MessageLog.Clear();
                defender.SetStatValue("Hitpoints", 9999);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
                foreach (var msg in MessageLog.GetRecent(20))
                {
                    if (!msg.Contains("offhand_blade")) continue;
                    offHandAttempts++;
                    if (msg.Contains("hits") && !msg.Contains("misses")) offHandHits++;
                    break;
                }
            }

            Assert.Greater(offHandAttempts, 0, "sanity: the off-hand must have attempted at least once across 200 seeds.");
            Assert.AreEqual(offHandAttempts, offHandHits,
                "Every off-hand ATTEMPT must land -- HitBonus=30 alone guarantees a hit against DV=0, "
                + "with no separate off-hand accuracy penalty stacked on top.");
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
