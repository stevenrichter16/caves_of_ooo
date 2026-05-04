using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tier 2.1 — Critical-hit MessageLog feedback.
    ///
    /// User-visible invariant: when a natural-20 melee hit lands successfully,
    /// the message log contains a line indicating the hit was critical. The
    /// player should SEE that the crit happened, not just have it tagged
    /// silently in the Damage object.
    ///
    /// The exact wording is captured by `CombatSystem.CRITICAL_HIT_TAG` so
    /// future polish (color codes, particle effects) can change the line
    /// without breaking tests.
    /// </summary>
    public class CriticalHitMessageTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        [Test]
        public void NaturalTwenty_LandsCritHit_LogContainsCriticalTag()
        {
            var zone = new Zone();
            var attacker = MakeFighter(strength: 20, agility: 20);
            attacker.Tags["Player"] = "";  // for AutoPen so crits land
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 5;
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 10, agility: 10);
            defender.GetPart<ArmorPart>().AV = 0;
            defender.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(defender, 6, 5);

            // Run seeds until we observe a nat-20 hit (confirmed by "Critical"
            // attribute on the Damage object via probe).
            bool foundCrit = false;
            string critMessage = null;
            for (int seed = 0; seed < 200 && !foundCrit; seed++)
            {
                MessageLog.Clear();
                defender.SetStatValue("Hitpoints", 999);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));

                // Was it a crit? Check the log for the marker.
                foreach (var msg in MessageLog.GetRecent(20))
                {
                    if (msg.Contains(CombatSystem.CRITICAL_HIT_TAG))
                    {
                        foundCrit = true;
                        critMessage = msg;
                        break;
                    }
                }
            }

            Assert.IsTrue(foundCrit,
                "Across 200 seeded attacks, at least one nat-20 hit should produce " +
                $"a log line containing '{CombatSystem.CRITICAL_HIT_TAG}'. None observed.");
            Assert.IsNotNull(critMessage);
            // Spot-check: the line should also reference the attacker and defender
            Assert.IsTrue(critMessage.Contains(attacker.GetDisplayName()),
                $"Crit log line should mention attacker. Got: {critMessage}");
        }

        [Test]
        public void NonCritHit_LogDoesNotContainCriticalTag()
        {
            // Counter-check: most hits are non-crit. Across many seeds we should
            // see at least one HIT (not miss, not pen-fail) that doesn't carry
            // the critical tag in its log line.
            var zone = new Zone();
            var attacker = MakeFighter(strength: 20, agility: 20);
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 5;
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 10, agility: 10);
            defender.GetPart<ArmorPart>().AV = 0;
            defender.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(defender, 6, 5);

            bool foundNonCritHit = false;
            for (int seed = 0; seed < 200 && !foundNonCritHit; seed++)
            {
                MessageLog.Clear();
                defender.SetStatValue("Hitpoints", 999);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));

                bool sawHit = false;
                bool sawCrit = false;
                foreach (var msg in MessageLog.GetRecent(20))
                {
                    if (msg.Contains("hits") && msg.Contains("damage")) sawHit = true;
                    if (msg.Contains(CombatSystem.CRITICAL_HIT_TAG)) sawCrit = true;
                }
                if (sawHit && !sawCrit)
                    foundNonCritHit = true;
            }

            Assert.IsTrue(foundNonCritHit,
                "Across 200 seeds, at least one non-crit hit should land — " +
                "and its log line should NOT contain the critical tag.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity MakeFighter(int strength, int agility, int hp = 30)
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
            entity.AddPart(new MeleeWeaponPart());
            entity.AddPart(new ArmorPart());
            return entity;
        }
    }
}
