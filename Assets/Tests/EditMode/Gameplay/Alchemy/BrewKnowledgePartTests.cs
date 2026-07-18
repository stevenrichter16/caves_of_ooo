using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BrewKnowledgePart — the permanent discovery log, keyed by rule ID.
    /// Mirrors BitLockerPart's known-recipes storage + restore-shim contract.
    /// </summary>
    public class BrewKnowledgePartTests
    {
        [Test]
        public void Discover_FirstTimeTrue_SecondTimeFalse()
        {
            var knowledge = new BrewKnowledgePart();

            Assert.IsTrue(knowledge.Discover("brew_burning"), "first discovery must report newly-discovered.");
            Assert.IsFalse(knowledge.Discover("brew_burning"), "re-discovery must report already-known.");
            Assert.IsTrue(knowledge.Knows("brew_burning"));
        }

        [Test]
        public void Knows_UndiscoveredRule_False()
        {
            // Counter-check: knowing one rule must not imply knowing others.
            var knowledge = new BrewKnowledgePart();
            knowledge.Discover("brew_burning");

            Assert.IsFalse(knowledge.Knows("brew_frost"));
        }

        [Test]
        public void RuleIds_AreCaseInsensitive()
        {
            var knowledge = new BrewKnowledgePart();
            knowledge.Discover("Brew_Burning");

            Assert.IsTrue(knowledge.Knows("brew_burning"));
            Assert.IsFalse(knowledge.Discover("BREW_BURNING"), "case variants are the same rule.");
        }

        [Test]
        public void NullOrWhitespace_NeverDiscovers_NeverKnows()
        {
            var knowledge = new BrewKnowledgePart();

            Assert.IsFalse(knowledge.Discover(null));
            Assert.IsFalse(knowledge.Discover("   "));
            Assert.IsFalse(knowledge.Knows(null));
            Assert.AreEqual(0, knowledge.GetDiscoveredRules().Count);
        }

        [Test]
        public void Restore_RoundTripsDiscoveredSet()
        {
            // Save/load shim contract, mirroring BitLockerPart.RestoreBitsAndRecipes.
            var original = new BrewKnowledgePart();
            original.Discover("brew_burning");
            original.Discover("brew_frost");

            var restored = new BrewKnowledgePart();
            restored.Discover("brew_acid"); // pre-existing state must be replaced
            restored.RestoreDiscoveredRules(original.GetDiscoveredRules());

            Assert.IsTrue(restored.Knows("brew_burning"));
            Assert.IsTrue(restored.Knows("brew_frost"));
            Assert.IsFalse(restored.Knows("brew_acid"), "restore must REPLACE, not merge.");
        }
    }
}
