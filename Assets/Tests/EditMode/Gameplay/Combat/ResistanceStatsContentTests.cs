using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tier 2.3 — Resistance stats shipped on thematic creatures.
    ///
    /// User-visible invariant: "When you attack a thematically-themed
    /// creature with the matching elemental damage type, that creature's
    /// resistance reduces the damage you deal. A Glowmaw (phosphorus,
    /// glowing) shrugs off Fire damage; a Snapjaw (cold-tolerant
    /// scavenger) shrugs off some Cold damage."
    ///
    /// Coverage targets:
    ///   - Glowmaw         → HeatResistance: 50  (themed phosphorus/glowing)
    ///   - Snapjaw         → ColdResistance: 25  (cold-tolerant baseline)
    ///   - SnapjawHunter   → ColdResistance: 50  (tougher elite override)
    ///
    /// (Kept to 3 to limit content scope; the resistance code path
    /// already has comprehensive unit-level coverage in ResistanceTests.cs.)
    /// </summary>
    public class ResistanceStatsContentTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _harness = new ScenarioTestHarness();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _harness?.Dispose();
            _harness = null;
        }

        // ====================================================================
        // Resistance stat presence (blueprint loads them correctly)
        // ====================================================================

        [Test]
        public void Glowmaw_HasHeatResistance50()
        {
            var glowmaw = _harness.Factory.CreateEntity("Glowmaw");
            int resist = glowmaw.GetStatValue("HeatResistance", -999);
            Assert.AreEqual(50, resist,
                "Glowmaw should declare HeatResistance: 50 (phosphorus theme)");
        }

        [Test]
        public void Snapjaw_HasColdResistance25()
        {
            var snapjaw = _harness.Factory.CreateEntity("Snapjaw");
            int resist = snapjaw.GetStatValue("ColdResistance", -999);
            Assert.AreEqual(25, resist,
                "Snapjaw should declare ColdResistance: 25 (cold-tolerant scavenger)");
        }

        [Test]
        public void SnapjawHunter_OverridesParent_ColdResistance50()
        {
            // SnapjawHunter inherits from Snapjaw but should have a
            // higher ColdResistance via override.
            var hunter = _harness.Factory.CreateEntity("SnapjawHunter");
            int resist = hunter.GetStatValue("ColdResistance", -999);
            Assert.AreEqual(50, resist,
                "SnapjawHunter overrides parent — ColdResistance: 50");
        }

        // ====================================================================
        // Counter-check: creatures NOT given a resistance don't have one
        // ====================================================================

        [Test]
        public void Glowmaw_DoesNotHaveColdResistance()
        {
            var glowmaw = _harness.Factory.CreateEntity("Glowmaw");
            // GetStatValue returns the default when the stat is missing.
            // A sentinel value confirms the stat truly isn't on the entity.
            int resist = glowmaw.GetStatValue("ColdResistance", -999);
            Assert.AreEqual(-999, resist,
                "Glowmaw should not declare ColdResistance (only Heat). " +
                "If this fires, the test sentinel was matched as a real value.");
        }

        // ====================================================================
        // Behavioral: HeatResistance halves Fire damage on Glowmaw
        // ====================================================================

        [Test]
        public void Glowmaw_HeatDamage_HalvedByResistance()
        {
            var glowmaw = _harness.Factory.CreateEntity("Glowmaw");
            int hpBefore = glowmaw.GetStat("Hitpoints").BaseValue;

            var damage = new Damage(20);
            damage.AddAttribute("Fire");
            CombatSystem.ApplyDamage(glowmaw, damage, source: null, zone: null);

            int hpAfter = glowmaw.GetStat("Hitpoints").BaseValue;
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "20 Fire damage * (100-50)/100 = 10 actual damage");
        }

        [Test]
        public void Glowmaw_AcidDamage_FullDamage_NoMatchingResist()
        {
            // Counter-check: Glowmaw's HeatResistance should NOT affect
            // Acid damage — different element.
            var glowmaw = _harness.Factory.CreateEntity("Glowmaw");
            int hpBefore = glowmaw.GetStat("Hitpoints").BaseValue;

            var damage = new Damage(10);
            damage.AddAttribute("Acid");
            CombatSystem.ApplyDamage(glowmaw, damage, source: null, zone: null);

            int hpAfter = glowmaw.GetStat("Hitpoints").BaseValue;
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "Glowmaw's HeatResistance must not reduce Acid damage");
        }
    }
}
