using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// AcidicDagger — end-to-end content test. Mirrors the
    /// FlamingSword / IceSword / ThunderHammer pattern on the Acid
    /// elemental axis. Like ThunderHammerContentTests, this fixture
    /// covers BOTH the resistance path (positive AR halves) AND the
    /// vulnerability path (negative AR amplifies).
    ///
    /// Test layout:
    ///   1. Resistance path (synthetic, AR=+50): halved damage.
    ///   2. Counter-check: same-shape damage minus the Acid attribute
    ///      is NOT halved.
    ///   3. Vulnerability path (synthetic, AR=−50): damage amplified 1.5×.
    ///   4. Real CaveSlime (AR=+50): takes less than control.
    ///   5. Real Scorpion (AR=−50): takes more than control.
    ///
    /// The single-attribute nature of `IsAcidDamage()` (only matches
    /// "Acid", unlike Cold which matches "Cold|Ice|Freeze") means the
    /// attribute string MUST contain literal "Acid" — a typo'd "acid"
    /// or "Acidic" would silently fail to fire AR. The blueprint-shape
    /// tests in WeaponAttributesContentTests pin that.
    /// </summary>
    public class AcidicDaggerContentTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _harness?.Dispose();
            _harness = null;
        }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // 1. Positive AcidResistance halves Acid-tagged damage
        // ====================================================================

        [Test]
        public void AcidicDagger_AttributesViaCombatPath_TriggerAcidResistance()
        {
            var dagger = _harness.Factory.CreateEntity("AcidicDagger");
            var weapon = dagger.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "AcidicDagger must have MeleeWeaponPart");

            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 50, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute(weapon.Stat);
            damage.AddAttributes(weapon.Attributes);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // AR=+50 → 20 × 0.5 = 10
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "AcidicDagger damage on an AcidResistance=+50 target should be halved. " +
                $"Got delta {hpBefore - hpAfter} (expected 10).");
        }

        // ====================================================================
        // 2. Counter-check: no Acid attribute → no resistance fires
        // ====================================================================

        [Test]
        public void NonAcidDamage_OnAcidResistantTarget_NotReduced()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 50, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            // Same damage shape MINUS the Acid attribute.
            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute("Strength");
            damage.AddAttribute("Piercing");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // No Acid attribute → IsAcidDamage()=false → AcidResistance never fires.
            Assert.AreEqual(hpBefore - 20, hpAfter,
                "Damage without 'Acid' attribute should NOT be reduced by " +
                $"AcidResistance — full 20 should land. Got delta {hpBefore - hpAfter}.");
        }

        // ====================================================================
        // 3. Vulnerability path — negative AR amplifies damage
        // ====================================================================

        [Test]
        public void AcidicDagger_OnAcidVulnerableTarget_AmplifiesDamage()
        {
            // AR=-50 → amount += amount × 0.5 → 1.5×
            var dagger = _harness.Factory.CreateEntity("AcidicDagger");
            var weapon = dagger.GetPart<MeleeWeaponPart>();

            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = -50, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute(weapon.Stat);
            damage.AddAttributes(weapon.Attributes);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // 20 + 20 × 0.5 = 30
            Assert.AreEqual(hpBefore - 30, hpAfter,
                "AcidicDagger damage on an AcidResistance=-50 (vulnerable) target " +
                "should be amplified by 50% (20 → 30). " +
                $"Got delta {hpBefore - hpAfter} (expected 30).");
        }

        // ====================================================================
        // 4. Real CaveSlime (AR=+50) takes less than control
        // ====================================================================

        [Test]
        public void AcidicDagger_OnCaveSlime_TakesLessDamageThan_Control()
        {
            var dagger = _harness.Factory.CreateEntity("AcidicDagger");
            var weapon = dagger.GetPart<MeleeWeaponPart>();

            var slime = _harness.Factory.CreateEntity("CaveSlime");
            Assert.IsNotNull(slime, "CaveSlime blueprint must exist");
            Assert.AreEqual(50, slime.GetStatValue("AcidResistance", 0),
                "CaveSlime blueprint should declare AcidResistance=+50 (the thematic premise)");

            var zone = new Zone();
            zone.AddEntity(slime, 5, 5);

            var control = MakeFighter(hp: 200);
            zone.AddEntity(control, 8, 8);

            int slimeHpBefore = slime.GetStatValue("Hitpoints");
            int controlHpBefore = control.GetStatValue("Hitpoints");

            CombatSystem.ApplyDamage(slime, BuildAcidDamage(weapon, 20), source: null, zone: zone);
            CombatSystem.ApplyDamage(control, BuildAcidDamage(weapon, 20), source: null, zone: zone);

            int slimeDelta = slimeHpBefore - slime.GetStatValue("Hitpoints");
            int controlDelta = controlHpBefore - control.GetStatValue("Hitpoints");

            Assert.Less(slimeDelta, controlDelta,
                "CaveSlime (AR=+50) should take strictly less AcidicDagger damage " +
                $"than the control. Got slime delta {slimeDelta} vs control delta {controlDelta}.");
        }

        // ====================================================================
        // 5. Real Scorpion (AR=-50) takes more than control (vulnerability)
        // ====================================================================

        [Test]
        public void AcidicDagger_OnScorpion_TakesMoreDamageThan_Control()
        {
            var dagger = _harness.Factory.CreateEntity("AcidicDagger");
            var weapon = dagger.GetPart<MeleeWeaponPart>();

            var scorpion = _harness.Factory.CreateEntity("Scorpion");
            Assert.IsNotNull(scorpion, "Scorpion blueprint must exist");
            Assert.AreEqual(-50, scorpion.GetStatValue("AcidResistance", 0),
                "Scorpion blueprint should declare AcidResistance=-50 (vulnerability premise)");

            // Pad Scorpion's natural HP (default 10) so the amplification doesn't
            // bottleneck against the HP=0 floor. With 20 raw → 30 amplified vs the
            // 10-HP default, delta caps at 10 — looks like LESS damage than the
            // control's 20, which would be a false failure. Padding to 200 lets
            // the full delta surface.
            scorpion.Statistics["Hitpoints"].Max = 200;
            scorpion.Statistics["Hitpoints"].BaseValue = 200;

            var zone = new Zone();
            zone.AddEntity(scorpion, 5, 5);

            var control = MakeFighter(hp: 200);
            zone.AddEntity(control, 8, 8);

            int scorpionHpBefore = scorpion.GetStatValue("Hitpoints");
            int controlHpBefore = control.GetStatValue("Hitpoints");

            CombatSystem.ApplyDamage(scorpion, BuildAcidDamage(weapon, 20), source: null, zone: zone);
            CombatSystem.ApplyDamage(control, BuildAcidDamage(weapon, 20), source: null, zone: zone);

            int scorpionDelta = scorpionHpBefore - scorpion.GetStatValue("Hitpoints");
            int controlDelta = controlHpBefore - control.GetStatValue("Hitpoints");

            Assert.Greater(scorpionDelta, controlDelta,
                "Scorpion (AR=-50) should take strictly MORE AcidicDagger damage " +
                $"than the control. Got scorpion delta {scorpionDelta} vs control delta {controlDelta}.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Damage BuildAcidDamage(MeleeWeaponPart weapon, int baseAmount)
        {
            var d = new Damage(baseAmount);
            d.AddAttribute("Melee");
            d.AddAttribute(weapon.Stat);
            d.AddAttributes(weapon.Attributes);
            return d;
        }

        private static Entity MakeFighter(int hp = 100)
        {
            var entity = new Entity { BlueprintName = "TestFighter" };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat
                { Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat
                { Owner = entity, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat
                { Owner = entity, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.AddPart(new RenderPart { DisplayName = "fighter" });
            entity.AddPart(new PhysicsPart { Solid = true });
            return entity;
        }
    }
}
