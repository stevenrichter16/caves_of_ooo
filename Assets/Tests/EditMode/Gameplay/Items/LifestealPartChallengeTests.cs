using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// RED scaffold for Challenge 2 — "Lifesteal Trait".
    /// See Docs/PROGRAMMING-CHALLENGES.md §Challenge 2.
    ///
    /// Implement LifestealPart.HandleEvent to turn the first two tests GREEN.
    /// The third (no-part counter-check) is GREEN from the start by design —
    /// it guards against healing that fires without the part.
    /// </summary>
    public class LifestealPartChallengeTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        private static Entity MakeBiter(int hp, int maxHp, int healPercent)
        {
            var e = new Entity { BlueprintName = "biter" };
            e.Statistics["Hitpoints"] =
                new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = maxHp };
            e.AddPart(new LifestealPart { HealPercent = healPercent });
            return e;
        }

        // "DamageDealt" fires on the ATTACKER (CombatSystem.cs:875), carrying an
        // int "Amount". We simulate a hit by firing that event on the attacker.
        private static void DealDamage(Entity attacker, int amount)
        {
            var e = GameEvent.New("DamageDealt");
            e.SetParameter("Amount", amount);
            attacker.FireEventAndRelease(e);
        }

        [Test]
        public void DealingDamage_HealsAttacker_ByHealPercent()
        {
            var attacker = MakeBiter(hp: 10, maxHp: 100, healPercent: 50);

            DealDamage(attacker, 10);   // 50% of 10 = +5

            Assert.AreEqual(15, attacker.GetStat("Hitpoints").BaseValue,
                "Lifesteal should heal HealPercent% of the damage dealt.");
        }

        [Test]
        public void Heal_IsClampedToMaxHitpoints()
        {
            var attacker = MakeBiter(hp: 98, maxHp: 100, healPercent: 50);

            DealDamage(attacker, 10);   // +5 would reach 103; must clamp to Max=100

            Assert.AreEqual(100, attacker.GetStat("Hitpoints").BaseValue,
                "Healing must not push Hitpoints above its Max.");
        }

        // Counter-check (expected GREEN): an identical biter with NO Lifesteal
        // part must not heal — proves the heal comes from the part, not the event.
        [Test]
        public void WithoutLifestealPart_NoHealing()
        {
            var plain = new Entity { BlueprintName = "plain" };
            plain.Statistics["Hitpoints"] =
                new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 100 };

            DealDamage(plain, 10);

            Assert.AreEqual(10, plain.GetStat("Hitpoints").BaseValue);
        }
    }
}
