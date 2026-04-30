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

        // ====================================================================
        // IceWight — first creature with FULL elemental immunity (CR=100)
        // AND first with negative HeatResistance via the standard
        // creature path (BrassHusk has ER=-50; IceWight is HR=-50, the
        // mirror image: Cold-immune AND Fire-vulnerable).
        // ====================================================================

        [Test]
        public void IceWight_BlueprintExists_AndIsCreature()
        {
            var iceWight = _harness.Factory.CreateEntity("IceWight");
            Assert.IsNotNull(iceWight,
                "IceWight blueprint must exist in Objects.json.");
            Assert.IsTrue(iceWight.HasTag("Creature"),
                "IceWight must inherit Creature tag (Inherits: Creature).");
        }

        [Test]
        public void IceWight_HasColdResistance100()
        {
            // The new pin: first creature with 100% elemental resistance.
            // Exercises the resistance ≥ 100 = total negation path.
            var iceWight = _harness.Factory.CreateEntity("IceWight");
            int resist = iceWight.GetStatValue("ColdResistance", -999);
            Assert.AreEqual(100, resist,
                "IceWight should declare ColdResistance: 100 (full Cold immunity — " +
                "the thematic premise: a creature literally made of ice cannot be " +
                "harmed by cold).");
        }

        [Test]
        public void IceWight_HasHeatResistance_NegativeFifty()
        {
            // The mirror pin: a creature with NEGATIVE HeatResistance, so
            // Fire damage is amplified 1.5×. Mirrors BrassHusk's ER=-50
            // but on the Fire/Cold axis instead of Lightning.
            var iceWight = _harness.Factory.CreateEntity("IceWight");
            int resist = iceWight.GetStatValue("HeatResistance", -999);
            Assert.AreEqual(-50, resist,
                "IceWight should declare HeatResistance: -50 (Fire vulnerability — " +
                "ice melts under heat). This is the mirror image of Glowmaw's " +
                "HeatResistance: +50 and the flip side of BrassHusk's negative ER.");
        }

        // ====================================================================
        // Behavioral: Cold damage is fully negated by IceWight's CR=100
        // ====================================================================

        [Test]
        public void IceWight_ColdDamage_FullyNegated()
        {
            var iceWight = _harness.Factory.CreateEntity("IceWight");
            int hpBefore = iceWight.GetStat("Hitpoints").BaseValue;

            var damage = new Damage(30);
            damage.AddAttribute("Cold");
            CombatSystem.ApplyDamage(iceWight, damage, source: null, zone: null);

            int hpAfter = iceWight.GetStat("Hitpoints").BaseValue;
            Assert.AreEqual(hpBefore, hpAfter,
                "30 Cold damage * (100 - 100)/100 = 0 actual damage. " +
                "ColdResistance=100 must fully negate Cold-attributed damage.");
        }

        // ====================================================================
        // Behavioral: Fire damage is amplified 1.5× by IceWight's HR=-50
        // ====================================================================

        [Test]
        public void IceWight_FireDamage_AmplifiedByNegativeResistance()
        {
            var iceWight = _harness.Factory.CreateEntity("IceWight");
            int hpBefore = iceWight.GetStat("Hitpoints").BaseValue;

            var damage = new Damage(10);
            damage.AddAttribute("Fire");
            CombatSystem.ApplyDamage(iceWight, damage, source: null, zone: null);

            int hpAfter = iceWight.GetStat("Hitpoints").BaseValue;
            // 10 Fire * (100 - (-50))/100 = 10 * 1.5 = 15
            Assert.AreEqual(hpBefore - 15, hpAfter,
                "10 Fire damage * (100 - HR(-50))/100 = 15 actual damage. " +
                "Negative HeatResistance must amplify Fire-attributed damage.");
        }

        [Test]
        public void IceWight_AcidDamage_FullDamage_NoMatchingResist()
        {
            // Counter-check: IceWight's Cold/Heat resistances should NOT
            // affect Acid damage — different element.
            var iceWight = _harness.Factory.CreateEntity("IceWight");
            int hpBefore = iceWight.GetStat("Hitpoints").BaseValue;

            var damage = new Damage(10);
            damage.AddAttribute("Acid");
            CombatSystem.ApplyDamage(iceWight, damage, source: null, zone: null);

            int hpAfter = iceWight.GetStat("Hitpoints").BaseValue;
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "IceWight's Cold/Heat resistances must not reduce or amplify Acid " +
                "damage — different element.");
        }

        // ====================================================================
        // CharredHusk — second creature with FULL elemental immunity
        // (HR=100), and second with negative resistance (CR=-50, Cold
        // vulnerability). Heat-axis mirror of IceWight.
        // ====================================================================

        [Test]
        public void CharredHusk_BlueprintExists_AndIsCreature()
        {
            var husk = _harness.Factory.CreateEntity("CharredHusk");
            Assert.IsNotNull(husk,
                "CharredHusk blueprint must exist in Objects.json.");
            Assert.IsTrue(husk.HasTag("Creature"),
                "CharredHusk must inherit Creature tag (Inherits: Creature).");
        }

        [Test]
        public void CharredHusk_HasHeatResistance100()
        {
            // The second 100%-immune creature (IceWight is the first, on Cold).
            // Mirrors IceWight's CR=100 on the Heat axis.
            var husk = _harness.Factory.CreateEntity("CharredHusk");
            int resist = husk.GetStatValue("HeatResistance", -999);
            Assert.AreEqual(100, resist,
                "CharredHusk should declare HeatResistance: 100 (full Fire immunity " +
                "— a creature already burned to charcoal cannot be further harmed " +
                "by fire).");
        }

        [Test]
        public void CharredHusk_HasColdResistance_NegativeFifty()
        {
            // Mirror of IceWight's HR=-50 on the Cold axis. Charred flesh
            // is brittle; cold makes it shatter.
            var husk = _harness.Factory.CreateEntity("CharredHusk");
            int resist = husk.GetStatValue("ColdResistance", -999);
            Assert.AreEqual(-50, resist,
                "CharredHusk should declare ColdResistance: -50 (Cold vulnerability " +
                "— charred flesh is brittle and shatters under cold). Mirrors " +
                "IceWight's HR=-50 on the inverse axis.");
        }

        // ====================================================================
        // Behavioral: Fire damage is fully negated by CharredHusk's HR=100
        // ====================================================================

        [Test]
        public void CharredHusk_FireDamage_FullyNegated()
        {
            var husk = _harness.Factory.CreateEntity("CharredHusk");
            int hpBefore = husk.GetStat("Hitpoints").BaseValue;

            var damage = new Damage(30);
            damage.AddAttribute("Fire");
            CombatSystem.ApplyDamage(husk, damage, source: null, zone: null);

            int hpAfter = husk.GetStat("Hitpoints").BaseValue;
            Assert.AreEqual(hpBefore, hpAfter,
                "30 Fire damage * (100 - 100)/100 = 0 actual damage. " +
                "HeatResistance=100 must fully negate Fire-attributed damage.");
        }

        // ====================================================================
        // Behavioral: Cold damage is amplified 1.5× by CharredHusk's CR=-50
        // ====================================================================

        [Test]
        public void CharredHusk_ColdDamage_AmplifiedByNegativeResistance()
        {
            var husk = _harness.Factory.CreateEntity("CharredHusk");
            int hpBefore = husk.GetStat("Hitpoints").BaseValue;

            var damage = new Damage(10);
            damage.AddAttribute("Cold");
            CombatSystem.ApplyDamage(husk, damage, source: null, zone: null);

            int hpAfter = husk.GetStat("Hitpoints").BaseValue;
            // 10 Cold * (100 - (-50))/100 = 10 * 1.5 = 15
            Assert.AreEqual(hpBefore - 15, hpAfter,
                "10 Cold damage * (100 - CR(-50))/100 = 15 actual damage. " +
                "Negative ColdResistance must amplify Cold-attributed damage.");
        }

        [Test]
        public void CharredHusk_AcidDamage_FullDamage_NoMatchingResist()
        {
            // Counter-check: CharredHusk's Heat/Cold resistances should NOT
            // affect Acid damage — different element.
            var husk = _harness.Factory.CreateEntity("CharredHusk");
            int hpBefore = husk.GetStat("Hitpoints").BaseValue;

            var damage = new Damage(10);
            damage.AddAttribute("Acid");
            CombatSystem.ApplyDamage(husk, damage, source: null, zone: null);

            int hpAfter = husk.GetStat("Hitpoints").BaseValue;
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "CharredHusk's Heat/Cold resistances must not reduce or amplify " +
                "Acid damage — different element.");
        }
    }
}
