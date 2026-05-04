using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase E of the Qud-parity port: elemental resistance stats.
    ///
    /// Qud reference (XRL.World.Parts/Physics.cs:3351-3417):
    ///   • Four resistance stats: AcidResistance, HeatResistance, ColdResistance, ElectricResistance
    ///   • Applied to <see cref="Damage"/> based on its type-check helpers (IsAcidDamage, etc.)
    ///   • Positive resistance: damage *= (100 − resist) / 100, with min 1 if resist < 100
    ///   • Negative resistance: damage += damage * (-resist / 100) (vulnerability)
    ///   • Damage with "IgnoreResist" attribute bypasses all resistance
    ///   • Resistance fires inside the TakeDamage handler; for our port we apply
    ///     it inline in ApplyDamage before the HP decrement
    /// </summary>
    public class ResistanceTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ====================================================================
        // 1. Positive resistance reduces damage
        // ====================================================================

        [Test]
        public void AcidResistance_50_Halves_AcidDamage()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 50, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Acid");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 10, hpAfter,
                "AcidResistance=50 should halve acid damage (20 → 10)");
        }

        [Test]
        public void HeatResistance_100_NullifiesFireDamage()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 100, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(50);
            damage.AddAttribute("Fire");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "HeatResistance=100 should nullify fire damage entirely (no HP loss)");
        }

        [Test]
        public void ColdResistance_25_ReducesColdDamage_RoundsDown()
        {
            // 20 damage * (100-25)/100 = 20 * 0.75 = 15
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["ColdResistance"] = new Stat
                { Owner = target, Name = "ColdResistance", BaseValue = 25, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Cold");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 15, hpAfter,
                "ColdResistance=25 on 20 damage should yield 15 actual (20 * 0.75)");
        }

        [Test]
        public void Resistance_99_With_LowDamage_ClampsToMinimum1()
        {
            // 5 damage * (100-99)/100 = 0.05 → 0 after int truncation. Min-1 clause forces 1.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 99, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(5);
            damage.AddAttribute("Acid");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 1, hpAfter,
                "Resistance=99 with low damage should clamp to minimum 1 (mirroring Qud)");
        }

        // ====================================================================
        // 2. Negative resistance = vulnerability
        // ====================================================================

        [Test]
        public void NegativeResistance_50_AmplifiesDamage_By1Point5x()
        {
            // -50 → +50% damage. 20 → 30.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["ElectricResistance"] = new Stat
                { Owner = target, Name = "ElectricResistance", BaseValue = -50, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Electric");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 30, hpAfter,
                "ElectricResistance=-50 (vulnerability) should boost damage 50% (20 → 30)");
        }

        [Test]
        public void NegativeResistance_100_Doubles_Damage()
        {
            // -100 = +100% (2x).
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = -100, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Fire");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 40, hpAfter,
                "HeatResistance=-100 should double damage (20 → 40)");
        }

        // ====================================================================
        // 3. Type mismatches: resistance only applies to matching damage type
        // ====================================================================

        [Test]
        public void AcidResistance_DoesNotAffect_FireDamage()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 75, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Fire");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 20, hpAfter,
                "AcidResistance must NOT apply to fire damage");
        }

        // ====================================================================
        // 4. IgnoreResist attribute bypasses ALL resistance
        // ====================================================================

        [Test]
        public void IgnoreResist_Attribute_Bypasses_AllResistance()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 100, Min = 0, Max = 200 };
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 100, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Acid");
            damage.AddAttribute("Fire");
            damage.AddAttribute("IgnoreResist");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 20, hpAfter,
                "IgnoreResist must bypass even 100% resistance to all matching types");
        }

        // ====================================================================
        // 5. No resistance stat → no effect
        // ====================================================================

        [Test]
        public void NoResistanceStat_DoesNothing()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            // No resistance stats added
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Acid");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 20, hpAfter,
                "No resistance stat = damage passes through unchanged");
        }

        // ====================================================================
        // 6. Multi-type damage: each applicable resistance applies in sequence
        // ====================================================================

        [Test]
        public void MultiTypeDamage_AppliesAllMatchingResistances()
        {
            // Damage tagged Cold AND Fire. Target has 50 ColdResist AND 50 HeatResist.
            // Order: cold first → 20 * 0.5 = 10. Then heat → 10 * 0.5 = 5.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["ColdResistance"] = new Stat
                { Owner = target, Name = "ColdResistance", BaseValue = 50, Min = 0, Max = 200 };
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(20);
            damage.AddAttribute("Cold");
            damage.AddAttribute("Fire");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 5, hpAfter,
                "Multi-type damage should chain resistances (20 → 10 → 5)");
        }

        // ====================================================================
        // 7. Adversarial: resistance doesn't underflow on already-zero damage
        // ====================================================================

        [Test]
        public void Resistance_OnZeroDamage_NoUnderflow_NoCrash()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = -100, Min = -100, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(0);  // already zero
            damage.AddAttribute("Acid");

            int hpBefore = target.GetStatValue("Hitpoints");
            // Should not throw, should not crash, should not change HP
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter, "Zero damage with vulnerability stays zero");
        }

        // ====================================================================
        // 8. Adversarial: 100% resistance + IgnoreResist still allows damage through
        //    (catches mutation: !HasAttribute check inverted)
        // ====================================================================

        [Test]
        public void IgnoreResist_With_FullResistance_StillDamages()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 100, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(15);
            damage.AddAttribute("Acid");
            damage.AddAttribute("IgnoreResist");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 15, hpAfter,
                "100% resistance + IgnoreResist must NOT block damage");
        }

        // ====================================================================
        // 9. Adversarial: melee damage is unaffected by elemental resistances
        //    (only "Cutting"/"Strength"/etc — none of the elemental types)
        // ====================================================================

        [Test]
        public void MeleeDamage_NoElementalAttributes_UnaffectedByElementalResistances()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 100, Min = 0, Max = 200 };
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 100, Min = 0, Max = 200 };
            target.Statistics["ColdResistance"] = new Stat
                { Owner = target, Name = "ColdResistance", BaseValue = 100, Min = 0, Max = 200 };
            target.Statistics["ElectricResistance"] = new Stat
                { Owner = target, Name = "ElectricResistance", BaseValue = 100, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(15);
            damage.AddAttribute("Melee");
            damage.AddAttribute("Cutting");
            damage.AddAttribute("Strength");

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 15, hpAfter,
                "Pure melee damage (no elemental tag) ignores all elemental resistances");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity MakeFighter(int hp = 100)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestFighter";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.AddPart(new RenderPart { DisplayName = "fighter" });
            entity.AddPart(new PhysicsPart { Solid = true });
            return entity;
        }
    }
}
