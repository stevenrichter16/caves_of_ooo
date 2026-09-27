using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityQualifiedCombatFeedbackTests
    {
        private Action<string> previousObserver;
        [SetUp] public void Setup()
        { previousObserver = MessageLog.OnMessage; MessageLog.OnMessage = null; MessageLog.Clear(); }
        [TearDown] public void Cleanup()
        { MessageLog.OnMessage = previousObserver; MessageLog.Clear(); }

        public static IEnumerable Cases()
        {
            foreach (string outcome in new[] { "miss", "blocked", "zero", "hit", "critical" })
                foreach (bool playerSubject in new[] { false, true })
                    foreach (bool qualified in new[] { false, true })
                        yield return new TestCaseData(outcome, playerSubject, qualified);
        }

        [TestCaseSource(nameof(Cases))]
        public void ActualCombatUsesItsSubjectsVerbAndRetainsOutcome(string outcome, bool playerSubject, bool qualified)
        {
            var zone = new Zone();
            var attacker = Fighter(playerSubject ? "you" : "Sella");
            // Both are tagged Player deliberately: grammar follows the displayed
            // subject, not an unrelated gameplay permission/critical-hit flag.
            attacker.SetTag("Player", "");
            var defender = Fighter("practice target");
            defender.SetStatValue("DV", outcome == "miss" ? 99 : 0);
            defender.GetPart<ArmorPart>().AV = outcome == "blocked" ? 99 : 0;
            var weapon = new MeleeWeaponPart { BaseDamage = outcome == "zero" ? "1d1-1" : "1d1", PenBonus = 0 };
            attacker.AddPart(weapon);
            Assert.True(zone.AddEntity(attacker, 5, 5)); Assert.True(zone.AddEntity(defender, 6, 5));
            string source = qualified ? "[left hand: dagger]" : null;
            string published = null; MessageLog.OnMessage = text => published = text;
            CombatSystem.PerformSingleAttack(attacker, defender, weapon, true, zone,
                new BranchRandom(outcome == "critical" ? 20 : 10), source);
            string prefix = (playerSubject ? "You" : "Sella") + (qualified ? " " + source : "") + " ";
            string verb = playerSubject ? "hit" : "hits";
            string expected;
            if (outcome == "miss") expected = prefix + (playerSubject ? "miss" : "misses") + " practice target!";
            else if (outcome == "blocked") expected = prefix + verb + " practice target but " + (playerSubject ? "fail" : "fails") + " to penetrate!";
            else if (outcome == "zero") expected = prefix + verb + " practice target but " + (playerSubject ? "deal" : "deals") + " no damage!";
            else
            {
                int damage = 999 - defender.GetStatValue("Hitpoints");
                Assert.Greater(damage, 0, "Successful branches must actually damage the target.");
                expected = prefix + (outcome == "critical" ? CombatSystem.CRITICAL_HIT_TAG + "LY " : "")
                    + verb + " practice target for " + damage + " damage! (" + defender.GetStatValue("Hitpoints") + " HP remaining)";
            }
            if (outcome == "miss" || outcome == "blocked" || outcome == "zero")
                Assert.AreEqual(999, defender.GetStatValue("Hitpoints"));
            Assert.AreEqual(expected, MessageLog.GetLast());
            Assert.AreEqual(expected, published, "The UI observer receives the same actual combat line.");
        }

        private static Entity Fighter(string name)
        {
            var entity = new Entity { BlueprintName = "FeedbackFighter" };
            entity.SetTag("Creature", "");
            foreach (var pair in new[] { ("Hitpoints", 999), ("Strength", 16), ("Agility", 16), ("DV", 0) })
                entity.Statistics[pair.Item1] = new Stat { Owner = entity, Name = pair.Item1, BaseValue = pair.Item2, Min = 0, Max = 999 };
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ArmorPart());
            return entity;
        }

        private sealed class BranchRandom : Random
        {
            private readonly int first; private bool used;
            public BranchRandom(int hit) { first = hit; }
            public override int Next(int minValue, int maxValue)
            {
                if (!used) { used = true; Assert.AreEqual(1, minValue); Assert.AreEqual(21, maxValue); return first; }
                return Math.Min(minValue + 4, maxValue - 1);
            }
        }
    }
}
