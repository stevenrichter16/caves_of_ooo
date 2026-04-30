using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// CryoLance — Piercing/Ice longblade-piercer hybrid. The fifth
    /// elemental weapon and the FIRST Piercing-class elemental
    /// (the existing four are Cutting × Fire/Ice or Bludgeoning ×
    /// Lightning, AcidicDagger is Piercing/Acid).
    ///
    /// Sibling of <see cref="IceSwordContentTests"/>; same end-to-end
    /// pipeline-routing pattern, same counter-checks. The new pin
    /// here is <see cref="CryoLance_OnIceWight_DealsZeroDamage"/> —
    /// the canonical "100% resistance = total negation" test, which
    /// also exercises the IceWight blueprint shipped alongside this
    /// weapon.
    ///
    /// Chain proven by these tests:
    ///   Phase C: weapon Attributes "Piercing Ice LongBlades"
    ///     → Damage.Attributes contains "Ice" → IsColdDamage()=true
    ///     → ApplyResistanceFor(target, damage, "ColdResistance") fires
    ///     → On a CR=100 target, damage *= 0 → no HP loss.
    /// </summary>
    public class CryoLanceContentTests
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
        public void CryoLance_BlueprintExists_AndIsMeleeWeapon()
        {
            var lance = _harness.Factory.CreateEntity("CryoLance");
            Assert.IsNotNull(lance,
                "CryoLance blueprint must exist in Objects.json.");
            Assert.IsNotNull(lance.GetPart<MeleeWeaponPart>(),
                "CryoLance must have a MeleeWeaponPart (Inherits: MeleeWeapon).");
        }

        [Test]
        public void CryoLance_HasPiercingIceLongBladesAttribute()
        {
            var lance = _harness.Factory.CreateEntity("CryoLance");
            var weapon = lance.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon);
            Assert.AreEqual("Piercing Ice LongBlades", weapon.Attributes,
                "CryoLance.Attributes should declare 'Piercing Ice LongBlades' " +
                "so it carries both the physical class (Piercing/LongBlades) and " +
                "the elemental type (Ice) into the Damage object on hit.");
        }

        [Test]
        public void CryoLance_AttributesContain_Ice()
        {
            // Pinned separately from the exact-string test so a future reorder
            // (e.g., "Ice Piercing LongBlades") doesn't silently strip Ice and
            // turn this into a plain piercer.
            var lance = _harness.Factory.CreateEntity("CryoLance");
            var weapon = lance.GetPart<MeleeWeaponPart>();
            Assert.IsTrue(weapon.Attributes.Contains("Ice"),
                "CryoLance must contain 'Ice' in its Attributes — that's what " +
                "routes its damage through ColdResistance (Phase E) on " +
                "cold-resistant creatures like SnapjawHunter and IceWight.");
        }

        [Test]
        public void CryoLance_AttributesContain_Piercing()
        {
            var lance = _harness.Factory.CreateEntity("CryoLance");
            var weapon = lance.GetPart<MeleeWeaponPart>();
            Assert.IsTrue(weapon.Attributes.Contains("Piercing"),
                "CryoLance is the first Piercing-class elemental weapon — " +
                "the physical class must be 'Piercing'.");
        }

        [Test]
        public void CryoLance_DoesNotHaveCutting_OrBludgeoning()
        {
            // Counter-check: physical-class is Piercing, not the others.
            var lance = _harness.Factory.CreateEntity("CryoLance");
            var weapon = lance.GetPart<MeleeWeaponPart>();
            Assert.IsFalse(weapon.Attributes.Contains("Cutting"),
                "CryoLance is Piercing, not Cutting — this distinguishes it " +
                "from IceSword and is what makes it the first Piercing+elemental.");
            Assert.IsFalse(weapon.Attributes.Contains("Bludgeoning"),
                "CryoLance is Piercing, not Bludgeoning.");
        }

        // ====================================================================
        // CryoLance's Attributes, fed through the combat damage pipeline,
        // trigger ColdResistance on a cold-resistant target.
        // ====================================================================

        [Test]
        public void CryoLance_AttributesViaCombatPath_TriggerColdResistance()
        {
            var lance = _harness.Factory.CreateEntity("CryoLance");
            var weapon = lance.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "CryoLance must have MeleeWeaponPart.");

            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["ColdResistance"] = new Stat
                { Owner = target, Name = "ColdResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = BuildCryoLanceDamage(weapon, baseAmount: 20);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // ColdResistance=50 → damage *= (100-50)/100 = 0.5 → 20 → 10
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "CryoLance damage on a ColdResistance=50 target should be halved. " +
                $"Got delta {hpBefore - hpAfter} (expected 10).");
        }

        [Test]
        public void NonIceDamage_OnColdResistantTarget_NotReduced()
        {
            // Counter-check: identical-shape damage from a non-elemental
            // weapon (synthesized with NO Ice attribute) is NOT halved by
            // ColdResistance.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["ColdResistance"] = new Stat
                { Owner = target, Name = "ColdResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            // Same shape MINUS the Ice attribute.
            var damage = new Damage(20);
            damage.AddAttribute("Melee");
            damage.AddAttribute("Strength");
            damage.AddAttribute("Piercing");
            damage.AddAttribute("LongBlades");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 20, hpAfter,
                "Damage without 'Ice'/'Cold'/'Freeze' attribute should NOT be reduced " +
                $"by ColdResistance — full 20 should land. Got delta {hpBefore - hpAfter}.");
        }

        // ====================================================================
        // CryoLance × IceWight — the canonical 100% immunity pin.
        // First end-to-end test of the resistance ≥ 100 = total negation
        // path. Pairs with the IceWight blueprint shipped in the same
        // commit.
        // ====================================================================

        [Test]
        public void CryoLance_OnIceWight_DealsZeroDamage()
        {
            var lance = _harness.Factory.CreateEntity("CryoLance");
            var weapon = lance.GetPart<MeleeWeaponPart>();

            var iceWight = _harness.Factory.CreateEntity("IceWight");
            Assert.IsNotNull(iceWight,
                "IceWight blueprint must exist (shipped alongside CryoLance).");
            Assert.AreEqual(100, iceWight.GetStatValue("ColdResistance", -999),
                "IceWight must declare ColdResistance=100 (full Cold immunity — " +
                "the thematic premise of this creature).");

            var zone = new Zone();
            zone.AddEntity(iceWight, 5, 5);

            var damage = BuildCryoLanceDamage(weapon, baseAmount: 30);

            int hpBefore = iceWight.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(iceWight, damage, source: null, zone: zone);
            int hpAfter = iceWight.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "CryoLance on IceWight (CR=100) MUST deal zero damage. " +
                "100% resistance = total negation. " +
                $"Got delta {hpBefore - hpAfter}.");
        }

        // ====================================================================
        // Sanity / direction check on a real cold-resistant creature.
        // ====================================================================

        [Test]
        public void CryoLance_OnSnapjawHunter_TakesLessDamageThan_ControlTarget()
        {
            var lance = _harness.Factory.CreateEntity("CryoLance");
            var weapon = lance.GetPart<MeleeWeaponPart>();

            var hunter = _harness.Factory.CreateEntity("SnapjawHunter");
            Assert.IsNotNull(hunter, "SnapjawHunter blueprint must exist.");
            Assert.AreEqual(50, hunter.GetStatValue("ColdResistance", 0),
                "SnapjawHunter blueprint should keep ColdResistance=50.");

            var zone = new Zone();
            zone.AddEntity(hunter, 5, 5);

            var control = MakeFighter(hp: 100);
            zone.AddEntity(control, 8, 8);

            int hunterHpBefore = hunter.GetStatValue("Hitpoints");
            int controlHpBefore = control.GetStatValue("Hitpoints");

            var dmgHunter = BuildCryoLanceDamage(weapon, baseAmount: 20);
            var dmgControl = BuildCryoLanceDamage(weapon, baseAmount: 20);
            CombatSystem.ApplyDamage(hunter, dmgHunter, source: null, zone: zone);
            CombatSystem.ApplyDamage(control, dmgControl, source: null, zone: zone);

            int hunterDelta = hunterHpBefore - hunter.GetStatValue("Hitpoints");
            int controlDelta = controlHpBefore - control.GetStatValue("Hitpoints");

            Assert.Less(hunterDelta, controlDelta,
                "SnapjawHunter should take strictly less CryoLance damage than the " +
                "control (ColdResistance=50 halves Cold-attributed damage). " +
                $"Got SnapjawHunter delta {hunterDelta} vs control delta {controlDelta}.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Damage BuildCryoLanceDamage(MeleeWeaponPart weapon, int baseAmount)
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
