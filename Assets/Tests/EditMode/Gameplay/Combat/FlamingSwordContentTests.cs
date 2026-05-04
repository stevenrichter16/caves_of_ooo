using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// FlamingSword — end-to-end content test, proving that the FlamingSword's
    /// declared Attributes flow through the same code path that
    /// <c>CombatSystem.PerformSingleAttack</c> uses, and trigger Phase E
    /// HeatResistance on a heat-resistant target.
    ///
    /// This is the "make Phase E player-visible" assertion — without it, the
    /// FlamingSword could ship with a typo'd Attributes string ("fire" vs "Fire")
    /// and the blueprint-shape tests in <see cref="WeaponAttributesContentTests"/>
    /// would catch only the literal-string case. This test exercises the
    /// full chain.
    ///
    /// Mirrors the synthesis in <c>CombatSystem.PerformSingleAttack</c> at
    /// the lines tagged "Phase C of the Qud-parity port" — Damage gets
    /// "Melee", the stat name, and the weapon's Attributes added to it
    /// before <c>ApplyDamage</c> is called.
    /// </summary>
    public class FlamingSwordContentTests
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

        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // FlamingSword's Attributes, fed through the combat damage pipeline,
        // trigger HeatResistance on a heat-resistant target.
        // ====================================================================

        [Test]
        public void FlamingSword_AttributesViaCombatPath_TriggerHeatResistance()
        {
            var sword = _harness.Factory.CreateEntity("FlamingSword");
            var weapon = sword.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "FlamingSword must have MeleeWeaponPart");

            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 50, Min = 0, Max = 200 };
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

            // HeatResistance=50 → damage *= (100-50)/100 = 0.5 → 20 → 10
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "FlamingSword damage on a HeatResistance=50 target should be halved. " +
                $"Got delta {hpBefore - hpAfter} (expected 10).");
        }

        // ====================================================================
        // Counter-check: identical-shape damage from a non-elemental weapon
        // (synthesized with NO Fire attribute) is NOT halved by HeatResistance.
        // Without this, the positive assertion above could pass for the wrong
        // reason (e.g., if some other path were halving damage).
        // ====================================================================

        [Test]
        public void NonFireDamage_OnHeatResistantTarget_NotReduced()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            // Same damage shape MINUS the Fire attribute.
            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute("Strength");
            damage.AddAttribute("Cutting");
            damage.AddAttribute("LongBlades");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // No Fire attribute → IsHeatDamage()=false → HeatResistance never fires.
            Assert.AreEqual(hpBefore - 20, hpAfter,
                "Damage without 'Fire' attribute should NOT be reduced by " +
                $"HeatResistance — full 20 should land. Got delta {hpBefore - hpAfter}.");
        }

        // ====================================================================
        // The thematic pairing: FlamingSword damage flow on a real Glowmaw
        // (HeatResistance=50, AV=2). Glowmaw also has Armor — so this test
        // is a sanity-check on the *direction* of the effect (less damage
        // than a control), not the exact magnitude.
        // ====================================================================

        [Test]
        public void FlamingSword_OnGlowmaw_TakesLessDamageThan_ControlTarget()
        {
            var sword = _harness.Factory.CreateEntity("FlamingSword");
            var weapon = sword.GetPart<MeleeWeaponPart>();

            // Real Glowmaw (HeatResistance=50, AV=2) loaded from blueprint
            var glowmaw = _harness.Factory.CreateEntity("Glowmaw");
            Assert.IsNotNull(glowmaw, "Glowmaw blueprint must exist");
            Assert.AreEqual(50, glowmaw.GetStatValue("HeatResistance", 0),
                "Glowmaw blueprint should keep HeatResistance=50 (the thematic premise)");

            var zone = new Zone();
            zone.AddEntity(glowmaw, 5, 5);

            var control = MakeFighter(hp: 100);
            zone.AddEntity(control, 8, 8);

            // Two identical Damage objects synthesized from the FlamingSword's
            // Attributes; one applied to Glowmaw, one to the control.
            int glowmawHpBefore = glowmaw.GetStatValue("Hitpoints");
            int controlHpBefore = control.GetStatValue("Hitpoints");

            var dmgGlowmaw = BuildFlamingSwordDamage(weapon, baseAmount: 20);
            var dmgControl = BuildFlamingSwordDamage(weapon, baseAmount: 20);
            CombatSystem.ApplyDamage(glowmaw, dmgGlowmaw, source: null, zone: zone);
            CombatSystem.ApplyDamage(control, dmgControl, source: null, zone: zone);

            int glowmawDelta = glowmawHpBefore - glowmaw.GetStatValue("Hitpoints");
            int controlDelta = controlHpBefore - control.GetStatValue("Hitpoints");

            Assert.Less(glowmawDelta, controlDelta,
                $"Glowmaw should take strictly less FlamingSword damage than the " +
                $"control (HeatResistance=50 halves Fire damage). " +
                $"Got Glowmaw delta {glowmawDelta} vs control delta {controlDelta}.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Damage BuildFlamingSwordDamage(MeleeWeaponPart weapon, int baseAmount)
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
