using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// DissolutionMaul — special integration test for the BONUS Acid-routing
    /// behavior introduced by the weapon-attribute backfill. The maul's
    /// existing Material declares the `Corrosive` tag; promoting that to a
    /// damage attribute (`Acid`) makes it route through AcidResistance,
    /// turning it into a second Acid-routing weapon alongside AcidicDagger.
    ///
    /// This is a BEHAVIOR CHANGE for the maul (previously its damage carried
    /// only `[Melee, Strength]` and bypassed all elemental resistance).
    /// Pinning the new contract here so it can't silently regress.
    /// </summary>
    public class DissolutionMaulContentTests
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
        // DissolutionMaul's Attributes route through AcidResistance.
        // ====================================================================

        [Test]
        public void DissolutionMaul_AcidAttribute_RoutesThrough_AcidResistance()
        {
            var maul = _harness.Factory.CreateEntity("DissolutionMaul");
            var weapon = maul.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "DissolutionMaul must have MeleeWeaponPart");
            Assert.IsTrue(weapon.Attributes.Contains("Acid"),
                "DissolutionMaul must declare Acid in its Attributes string for " +
                "this routing test to be meaningful");

            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 50, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            // Synthesize damage exactly as CombatSystem.PerformSingleAttack does.
            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute(weapon.Stat);
            damage.AddAttributes(weapon.Attributes);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // AR=+50 → 20 × 0.5 = 10
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "DissolutionMaul's Acid attribute should route through " +
                "AcidResistance and halve damage on a target with AR=+50. " +
                $"Got delta {hpBefore - hpAfter} (expected 10).");
        }

        // ====================================================================
        // Counter-check: same maul against a target without AR — no reduction.
        // ====================================================================

        [Test]
        public void DissolutionMaul_OnTargetWithoutAR_LandsFullDamage()
        {
            var maul = _harness.Factory.CreateEntity("DissolutionMaul");
            var weapon = maul.GetPart<MeleeWeaponPart>();

            var zone = new Zone();
            var target = MakeFighter(hp: 100);  // no AR stat at all
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute(weapon.Stat);
            damage.AddAttributes(weapon.Attributes);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // No AR → AR loop returns early → full 20 lands
            Assert.AreEqual(hpBefore - 20, hpAfter,
                "DissolutionMaul on a target without AcidResistance should land " +
                $"full damage. Got delta {hpBefore - hpAfter} (expected 20).");
        }

        private static Entity MakeFighter(int hp = 100)
        {
            var entity = new Entity { BlueprintName = "TestFighter" };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat
                { Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat
                { Owner = entity, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.AddPart(new RenderPart { DisplayName = "fighter" });
            entity.AddPart(new PhysicsPart { Solid = true });
            return entity;
        }
    }
}
