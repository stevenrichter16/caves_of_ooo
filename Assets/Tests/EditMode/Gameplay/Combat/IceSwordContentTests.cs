using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// IceSword — end-to-end content test, sibling of
    /// <see cref="FlamingSwordContentTests"/>. Proves that the IceSword's
    /// declared Attributes flow through the same code path that
    /// <c>CombatSystem.PerformSingleAttack</c> uses, and trigger Phase E
    /// ColdResistance on a cold-resistant target.
    ///
    /// Identical structure to FlamingSwordContentTests, swapping
    ///   FlamingSword → IceSword
    ///   "Fire" → "Ice"
    ///   HeatResistance → ColdResistance
    ///   Glowmaw → SnapjawHunter (HR=50 → CR=50)
    ///
    /// The chain proven by these tests:
    ///   Phase C: weapon Attributes string includes "Ice"
    ///       → Damage.Attributes contains "Ice"
    ///       → Damage.IsColdDamage() = true
    ///       → ApplyResistanceFor(target, damage, "ColdResistance") fires
    ///       → damage halved on a CR=50 target
    /// </summary>
    public class IceSwordContentTests
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
        // IceSword's Attributes, fed through the combat damage pipeline,
        // trigger ColdResistance on a cold-resistant target.
        // ====================================================================

        [Test]
        public void IceSword_AttributesViaCombatPath_TriggerColdResistance()
        {
            var sword = _harness.Factory.CreateEntity("IceSword");
            var weapon = sword.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "IceSword must have MeleeWeaponPart");

            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["ColdResistance"] = new Stat
                { Owner = target, Name = "ColdResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            // Synthesize damage exactly as CombatSystem.PerformSingleAttack does:
            //   damage.AddAttribute("Melee");
            //   damage.AddAttribute(weapon.Stat);                // "Strength"
            //   damage.AddAttributes(weapon.Attributes);         // space-split
            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute(weapon.Stat);
            damage.AddAttributes(weapon.Attributes);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // ColdResistance=50 → damage *= (100-50)/100 = 0.5 → 20 → 10
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "IceSword damage on a ColdResistance=50 target should be halved. " +
                $"Got delta {hpBefore - hpAfter} (expected 10).");
        }

        // ====================================================================
        // Counter-check: identical-shape damage from a non-elemental weapon
        // (synthesized with NO Ice attribute) is NOT halved by ColdResistance.
        // ====================================================================

        [Test]
        public void NonIceDamage_OnColdResistantTarget_NotReduced()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["ColdResistance"] = new Stat
                { Owner = target, Name = "ColdResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            // Same damage shape MINUS the Ice attribute.
            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute("Strength");
            damage.AddAttribute("Cutting");
            damage.AddAttribute("LongBlades");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // No Ice/Cold/Freeze attribute → IsColdDamage()=false → ColdResistance never fires.
            Assert.AreEqual(hpBefore - 20, hpAfter,
                "Damage without 'Ice'/'Cold'/'Freeze' attribute should NOT be reduced by " +
                $"ColdResistance — full 20 should land. Got delta {hpBefore - hpAfter}.");
        }

        // ====================================================================
        // The thematic pairing: IceSword damage flow on a real SnapjawHunter
        // (ColdResistance=50, plus the standard Snapjaw armor profile). This
        // is a sanity-check on the *direction* of the effect (less damage
        // than a control), not the exact magnitude.
        // ====================================================================

        [Test]
        public void IceSword_OnSnapjawHunter_TakesLessDamageThan_ControlTarget()
        {
            var sword = _harness.Factory.CreateEntity("IceSword");
            var weapon = sword.GetPart<MeleeWeaponPart>();

            // Real SnapjawHunter (ColdResistance=50) loaded from blueprint
            var hunter = _harness.Factory.CreateEntity("SnapjawHunter");
            Assert.IsNotNull(hunter, "SnapjawHunter blueprint must exist");
            Assert.AreEqual(50, hunter.GetStatValue("ColdResistance", 0),
                "SnapjawHunter blueprint should keep ColdResistance=50 (the thematic premise)");

            var zone = new Zone();
            zone.AddEntity(hunter, 5, 5);

            var control = MakeFighter(hp: 100);
            zone.AddEntity(control, 8, 8);

            // Two identical Damage objects synthesized from the IceSword's
            // Attributes; one applied to SnapjawHunter, one to the control.
            int hunterHpBefore = hunter.GetStatValue("Hitpoints");
            int controlHpBefore = control.GetStatValue("Hitpoints");

            var dmgHunter = BuildIceSwordDamage(weapon, baseAmount: 20);
            var dmgControl = BuildIceSwordDamage(weapon, baseAmount: 20);
            CombatSystem.ApplyDamage(hunter, dmgHunter, source: null, zone: zone);
            CombatSystem.ApplyDamage(control, dmgControl, source: null, zone: zone);

            int hunterDelta = hunterHpBefore - hunter.GetStatValue("Hitpoints");
            int controlDelta = controlHpBefore - control.GetStatValue("Hitpoints");

            Assert.Less(hunterDelta, controlDelta,
                $"SnapjawHunter should take strictly less IceSword damage than the " +
                $"control (ColdResistance=50 halves Cold-attributed damage). " +
                $"Got SnapjawHunter delta {hunterDelta} vs control delta {controlDelta}.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Damage BuildIceSwordDamage(MeleeWeaponPart weapon, int baseAmount)
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
