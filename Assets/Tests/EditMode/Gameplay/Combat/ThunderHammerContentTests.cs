using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ThunderHammer — end-to-end content test. Mirrors
    /// <see cref="FlamingSwordContentTests"/> and
    /// <see cref="IceSwordContentTests"/> on the Electric side, plus
    /// adds **vulnerability** coverage (negative ElectricResistance
    /// amplifies damage by `(1 + |resist|/100)` per CombatSystem.cs:644).
    ///
    /// Three integration tests:
    ///
    ///   1. Resistance path (positive ER=50): synthesized ThunderHammer
    ///      damage is halved on a target with ElectricResistance=50.
    ///
    ///   2. Counter-check (no Lightning attribute): same-shape damage
    ///      missing the Lightning attribute is NOT halved by ER. Without
    ///      this, the positive assertion could pass for the wrong reason.
    ///
    ///   3. **Vulnerability path (negative ER=−50)**: ThunderHammer
    ///      damage is amplified by 50% on a target with ER=−50. This is
    ///      coverage NEW to this fixture (FlamingSword and IceSword tests
    ///      only cover positive resistance — the negative path was
    ///      already proven by ResistanceTests with synthetic damage, but
    ///      not via a weapon's actual Attributes string).
    ///
    /// Plus the thematic-pairing test: ThunderHammer vs the real
    /// StoneGolem and BrassHusk blueprints, asserting the *direction*
    /// of the effect (less than control on StoneGolem, more on BrassHusk)
    /// rather than exact magnitude (because both creatures have Armor
    /// that adds AV noise to the calculation).
    /// </summary>
    public class ThunderHammerContentTests
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
        // 1. Positive ElectricResistance halves Lightning-tagged damage
        // ====================================================================

        [Test]
        public void ThunderHammer_AttributesViaCombatPath_TriggerElectricResistance()
        {
            var hammer = _harness.Factory.CreateEntity("ThunderHammer");
            var weapon = hammer.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "ThunderHammer must have MeleeWeaponPart");

            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["ElectricResistance"] = new Stat
                { Owner = target, Name = "ElectricResistance", BaseValue = 50, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            // Synthesize damage exactly as CombatSystem.PerformSingleAttack does:
            //   damage.AddAttribute("Melee");
            //   damage.AddAttribute(weapon.Stat);
            //   damage.AddAttributes(weapon.Attributes);
            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute(weapon.Stat);
            damage.AddAttributes(weapon.Attributes);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // ER=50 → damage *= (100-50)/100 = 0.5 → 20 → 10
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "ThunderHammer damage on an ElectricResistance=50 target should be halved. " +
                $"Got delta {hpBefore - hpAfter} (expected 10).");
        }

        // ====================================================================
        // 2. Counter-check: no Lightning attribute → no resistance fires
        // ====================================================================

        [Test]
        public void NonLightningDamage_OnElectricResistantTarget_NotReduced()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["ElectricResistance"] = new Stat
                { Owner = target, Name = "ElectricResistance", BaseValue = 50, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            // Same damage shape MINUS any Electric/Shock/Lightning attribute.
            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute("Strength");
            damage.AddAttribute("Bludgeoning");
            damage.AddAttribute("Cudgel");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // No Electric/Shock/Lightning/Electricity → IsElectricDamage()=false → ER never fires.
            Assert.AreEqual(hpBefore - 20, hpAfter,
                "Damage without any Electric-family attribute should NOT be reduced " +
                $"by ElectricResistance — full 20 should land. Got delta {hpBefore - hpAfter}.");
        }

        // ====================================================================
        // 3. NEW: Vulnerability path — negative ER amplifies damage
        // ====================================================================

        [Test]
        public void ThunderHammer_OnElectricVulnerableTarget_AmplifiesDamage()
        {
            // Per CombatSystem.cs:644:
            //   damage.Amount += (int)(damage.Amount * (resist / -100f));
            // with resist=-50: amount += amount * 0.5 → 1.5x amplification.
            //
            // This is the lever a "Conductive" creature (BrassHusk-style)
            // exposes — Lightning damage flows through and amplifies.
            var hammer = _harness.Factory.CreateEntity("ThunderHammer");
            var weapon = hammer.GetPart<MeleeWeaponPart>();

            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["ElectricResistance"] = new Stat
                { Owner = target, Name = "ElectricResistance", BaseValue = -50, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute(weapon.Stat);
            damage.AddAttributes(weapon.Attributes);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // 20 + 20 * (50/100) = 30
            Assert.AreEqual(hpBefore - 30, hpAfter,
                "ThunderHammer damage on an ElectricResistance=-50 (vulnerable) target " +
                "should be amplified by 50% (20 → 30). " +
                $"Got delta {hpBefore - hpAfter} (expected 30).");
        }

        // ====================================================================
        // 4. Thematic pairing: real StoneGolem (ER=50) takes less than control;
        //    real BrassHusk (ER=-50) takes more.
        // ====================================================================

        [Test]
        public void ThunderHammer_OnStoneGolem_TakesLessDamageThan_Control()
        {
            var hammer = _harness.Factory.CreateEntity("ThunderHammer");
            var weapon = hammer.GetPart<MeleeWeaponPart>();

            var golem = _harness.Factory.CreateEntity("StoneGolem");
            Assert.IsNotNull(golem, "StoneGolem blueprint must exist");
            Assert.AreEqual(50, golem.GetStatValue("ElectricResistance", 0),
                "StoneGolem blueprint should declare ElectricResistance=50 (the thematic premise)");

            var zone = new Zone();
            zone.AddEntity(golem, 5, 5);

            var control = MakeFighter(hp: 200);  // bigger pool than golem; AV noise still factors but direction is robust
            zone.AddEntity(control, 8, 8);

            int golemHpBefore = golem.GetStatValue("Hitpoints");
            int controlHpBefore = control.GetStatValue("Hitpoints");

            CombatSystem.ApplyDamage(golem, BuildThunderDamage(weapon, 20), source: null, zone: zone);
            CombatSystem.ApplyDamage(control, BuildThunderDamage(weapon, 20), source: null, zone: zone);

            int golemDelta = golemHpBefore - golem.GetStatValue("Hitpoints");
            int controlDelta = controlHpBefore - control.GetStatValue("Hitpoints");

            Assert.Less(golemDelta, controlDelta,
                "StoneGolem (ER=50) should take strictly less ThunderHammer damage " +
                $"than the control. Got golem delta {golemDelta} vs control delta {controlDelta}.");
        }

        [Test]
        public void ThunderHammer_OnBrassHusk_TakesMoreDamageThan_Control()
        {
            var hammer = _harness.Factory.CreateEntity("ThunderHammer");
            var weapon = hammer.GetPart<MeleeWeaponPart>();

            var husk = _harness.Factory.CreateEntity("BrassHusk");
            Assert.IsNotNull(husk, "BrassHusk blueprint must exist");
            Assert.AreEqual(-50, husk.GetStatValue("ElectricResistance", 0),
                "BrassHusk blueprint should declare ElectricResistance=-50 (vulnerability premise)");

            var zone = new Zone();
            zone.AddEntity(husk, 5, 5);

            var control = MakeFighter(hp: 200);
            zone.AddEntity(control, 8, 8);

            int huskHpBefore = husk.GetStatValue("Hitpoints");
            int controlHpBefore = control.GetStatValue("Hitpoints");

            CombatSystem.ApplyDamage(husk, BuildThunderDamage(weapon, 20), source: null, zone: zone);
            CombatSystem.ApplyDamage(control, BuildThunderDamage(weapon, 20), source: null, zone: zone);

            int huskDelta = huskHpBefore - husk.GetStatValue("Hitpoints");
            int controlDelta = controlHpBefore - control.GetStatValue("Hitpoints");

            Assert.Greater(huskDelta, controlDelta,
                "BrassHusk (ER=-50) should take strictly MORE ThunderHammer damage " +
                $"than the control. Got husk delta {huskDelta} vs control delta {controlDelta}.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Damage BuildThunderDamage(MeleeWeaponPart weapon, int baseAmount)
        {
            // Same synthesis as CombatSystem.PerformSingleAttack.
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
