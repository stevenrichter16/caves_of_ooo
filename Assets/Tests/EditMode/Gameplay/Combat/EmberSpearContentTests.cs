using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// EmberSpear — Piercing/Fire spear. The sixth elemental weapon and
    /// the SECOND Piercing-class elemental on the Heat axis (the first
    /// was AcidicDagger; CryoLance handles Piercing+Ice).
    ///
    /// Sibling of <see cref="CryoLanceContentTests"/>; same end-to-end
    /// pipeline-routing pattern, same counter-checks, same style. The
    /// new pin here is <see cref="EmberSpear_OnCharredHusk_DealsZeroDamage"/>
    /// — the **second** "100% resistance = total negation" test (mirroring
    /// CryoLance × IceWight on the Heat axis instead of the Cold axis).
    ///
    /// Chain proven by these tests:
    ///   Phase C: weapon Attributes "Piercing Fire"
    ///     → Damage.Attributes contains "Fire" → IsHeatDamage()=true
    ///     → ApplyResistanceFor(target, damage, "HeatResistance") fires
    ///     → On a HR=100 target, damage *= 0 → no HP loss.
    /// </summary>
    public class EmberSpearContentTests
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
        // Blueprint shape
        // ====================================================================

        [Test]
        public void EmberSpear_BlueprintExists_AndIsMeleeWeapon()
        {
            var spear = _harness.Factory.CreateEntity("EmberSpear");
            Assert.IsNotNull(spear,
                "EmberSpear blueprint must exist in Objects.json.");
            Assert.IsNotNull(spear.GetPart<MeleeWeaponPart>(),
                "EmberSpear must have a MeleeWeaponPart (Inherits: MeleeWeapon).");
        }

        [Test]
        public void EmberSpear_HasPiercingFireAttribute()
        {
            var spear = _harness.Factory.CreateEntity("EmberSpear");
            var weapon = spear.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.AreEqual("Piercing Fire", weapon.Attributes,
                "EmberSpear.Attributes should declare 'Piercing Fire' so it " +
                "carries both the physical class (Piercing) and the elemental " +
                "type (Fire) into the Damage object on hit. " +
                "No sub-class — matches the existing Spear blueprint convention.");
        }

        [Test]
        public void EmberSpear_AttributesContain_Fire()
        {
            // Pinned separately from the exact-string test so a future reorder
            // (e.g., "Fire Piercing") doesn't silently strip Fire and turn
            // this into a plain piercer.
            var spear = _harness.Factory.CreateEntity("EmberSpear");
            var weapon = spear.GetPart<MeleeWeaponPart>();
            Assert.IsTrue(weapon.Attributes.Contains("Fire"),
                "EmberSpear must contain 'Fire' in its Attributes — that's what " +
                "routes its damage through HeatResistance (Phase E) on " +
                "heat-resistant creatures like Glowmaw and CharredHusk.");
        }

        [Test]
        public void EmberSpear_AttributesContain_Piercing()
        {
            var spear = _harness.Factory.CreateEntity("EmberSpear");
            var weapon = spear.GetPart<MeleeWeaponPart>();
            Assert.IsTrue(weapon.Attributes.Contains("Piercing"),
                "EmberSpear is a Piercing-class elemental — physical class must " +
                "be 'Piercing'.");
        }

        [Test]
        public void EmberSpear_DoesNotHaveCutting_OrBludgeoning()
        {
            // Counter-check: physical-class is Piercing, not the others.
            var spear = _harness.Factory.CreateEntity("EmberSpear");
            var weapon = spear.GetPart<MeleeWeaponPart>();
            Assert.IsFalse(weapon.Attributes.Contains("Cutting"),
                "EmberSpear is Piercing, not Cutting — distinguishes it from " +
                "FlamingSword (which is the Cutting+Fire variant).");
            Assert.IsFalse(weapon.Attributes.Contains("Bludgeoning"),
                "EmberSpear is Piercing, not Bludgeoning.");
        }

        // ====================================================================
        // EmberSpear's Attributes, fed through the combat damage pipeline,
        // trigger HeatResistance on a heat-resistant target.
        // ====================================================================

        [Test]
        public void EmberSpear_AttributesViaCombatPath_TriggerHeatResistance()
        {
            var spear = _harness.Factory.CreateEntity("EmberSpear");
            var weapon = spear.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "EmberSpear must have MeleeWeaponPart.");

            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = BuildEmberSpearDamage(weapon, baseAmount: 20);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // HeatResistance=50 → damage *= (100-50)/100 = 0.5 → 20 → 10
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "EmberSpear damage on a HeatResistance=50 target should be halved. " +
                $"Got delta {hpBefore - hpAfter} (expected 10).");
        }

        [Test]
        public void NonFireDamage_OnHeatResistantTarget_NotReduced()
        {
            // Counter-check: identical-shape damage from a non-elemental
            // weapon (synthesized with NO Fire attribute) is NOT halved by
            // HeatResistance.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            // Same shape MINUS the Fire attribute.
            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute("Strength");
            damage.AddAttribute("Piercing");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 20, hpAfter,
                "Damage without 'Fire'/'Heat' attribute should NOT be reduced " +
                $"by HeatResistance — full 20 should land. Got delta {hpBefore - hpAfter}.");
        }

        // ====================================================================
        // EmberSpear × CharredHusk — the canonical SECOND 100% immunity pin.
        // First was CryoLance × IceWight on the Cold axis; this is the
        // mirror on the Heat axis. Pairs with the CharredHusk blueprint
        // shipped in the same commit.
        // ====================================================================

        [Test]
        public void EmberSpear_OnCharredHusk_DealsZeroDamage()
        {
            var spear = _harness.Factory.CreateEntity("EmberSpear");
            var weapon = spear.GetPart<MeleeWeaponPart>();

            var husk = _harness.Factory.CreateEntity("CharredHusk");
            Assert.IsNotNull(husk,
                "CharredHusk blueprint must exist (shipped alongside EmberSpear).");
            Assert.AreEqual(100, husk.GetStatValue("HeatResistance", -999),
                "CharredHusk must declare HeatResistance=100 (full Fire immunity " +
                "— the thematic premise: a creature already burned to charcoal " +
                "cannot be further harmed by fire).");

            var zone = new Zone();
            zone.AddEntity(husk, 5, 5);

            var damage = BuildEmberSpearDamage(weapon, baseAmount: 30);

            int hpBefore = husk.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(husk, damage, source: null, zone: zone);
            int hpAfter = husk.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "EmberSpear on CharredHusk (HR=100) MUST deal zero damage. " +
                "100% resistance = total negation. " +
                $"Got delta {hpBefore - hpAfter}.");
        }

        // ====================================================================
        // Sanity / direction check on the existing HR=50 Glowmaw.
        // ====================================================================

        [Test]
        public void EmberSpear_OnGlowmaw_TakesLessDamageThan_ControlTarget()
        {
            var spear = _harness.Factory.CreateEntity("EmberSpear");
            var weapon = spear.GetPart<MeleeWeaponPart>();

            var glowmaw = _harness.Factory.CreateEntity("Glowmaw");
            Assert.IsNotNull(glowmaw, "Glowmaw blueprint must exist.");
            Assert.AreEqual(50, glowmaw.GetStatValue("HeatResistance", 0),
                "Glowmaw blueprint should keep HeatResistance=50.");

            var zone = new Zone();
            zone.AddEntity(glowmaw, 5, 5);

            var control = MakeFighter(hp: 100);
            zone.AddEntity(control, 8, 8);

            int glowHpBefore = glowmaw.GetStatValue("Hitpoints");
            int controlHpBefore = control.GetStatValue("Hitpoints");

            var dmgGlow = BuildEmberSpearDamage(weapon, baseAmount: 20);
            var dmgControl = BuildEmberSpearDamage(weapon, baseAmount: 20);
            CombatSystem.ApplyDamage(glowmaw, dmgGlow, source: null, zone: zone);
            CombatSystem.ApplyDamage(control, dmgControl, source: null, zone: zone);

            int glowDelta = glowHpBefore - glowmaw.GetStatValue("Hitpoints");
            int controlDelta = controlHpBefore - control.GetStatValue("Hitpoints");

            Assert.Less(glowDelta, controlDelta,
                "Glowmaw should take strictly less EmberSpear damage than the " +
                "control (HeatResistance=50 halves Fire-attributed damage). " +
                $"Got Glowmaw delta {glowDelta} vs control delta {controlDelta}.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Damage BuildEmberSpearDamage(MeleeWeaponPart weapon, int baseAmount)
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
