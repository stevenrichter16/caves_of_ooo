using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase C integration + adversarial tests for the typed-Damage refactor.
    ///
    /// Pins behaviors:
    ///   • PerformMeleeAttack builds a typed Damage with the right attributes
    ///   • Weapon's Attributes field propagates into the Damage
    ///   • TakeDamage event carries both legacy "Amount" int and new "Damage" object
    ///   • Backward-compat int ApplyDamage overload still works for legacy callers
    ///
    /// Adversarial:
    ///   • Empty / null damage edge cases
    ///   • Listener can read typed Damage from TakeDamage event
    ///   • Concurrent attributes don't interfere
    /// </summary>
    public class CombatDamageIntegrationTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ====================================================================
        // Integration — typed Damage flows from PerformMeleeAttack to ApplyDamage
        // ====================================================================

        [Test]
        public void PerformMeleeAttack_FiresTakeDamage_WithTypedDamageObject()
        {
            var zone = new Zone();
            var attacker = MakeFighter(strength: 20, agility: 20);
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 5;
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 10;
            attacker.GetPart<MeleeWeaponPart>().Attributes = "Cutting LongBlades";
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 10, agility: 10);
            defender.GetPart<ArmorPart>().AV = 0;
            defender.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(defender, 6, 5);

            // Probe captures the Damage object passed to TakeDamage event.
            Damage capturedDamage = null;
            var probe = new TakeDamageCaptureProbe();
            probe.OnTakeDamage = e =>
            {
                if (e.GetParameter("Damage") is Damage d) capturedDamage = d;
            };
            defender.AddPart(probe);

            // Attempt several seeds; we only need ONE successful hit to inspect Damage.
            for (int seed = 0; seed < 30 && capturedDamage == null; seed++)
            {
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(seed));
            }

            Assert.IsNotNull(capturedDamage,
                "At least one seed must produce a hit so we can inspect the Damage object");
            Assert.IsTrue(capturedDamage.HasAttribute("Melee"),
                "PerformMeleeAttack must always tag damage with 'Melee'");
            Assert.IsTrue(capturedDamage.HasAttribute("Strength"),
                "Damage must carry the weapon's Stat name (default Strength)");
            Assert.IsTrue(capturedDamage.HasAttribute("Cutting"),
                "Weapon's Attributes string ('Cutting LongBlades') must propagate");
            Assert.IsTrue(capturedDamage.HasAttribute("LongBlades"),
                "All space-separated weapon attributes must propagate");
        }

        [Test]
        public void PerformMeleeAttack_NoWeaponAttributes_StillTagsMeleeAndStat()
        {
            var zone = new Zone();
            var attacker = MakeFighter(strength: 20, agility: 20);
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 10;
            // Attributes intentionally left empty
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 10, agility: 10);
            zone.AddEntity(defender, 6, 5);

            Damage capturedDamage = null;
            var probe = new TakeDamageCaptureProbe();
            probe.OnTakeDamage = e =>
            {
                if (e.GetParameter("Damage") is Damage d) capturedDamage = d;
            };
            defender.AddPart(probe);

            for (int seed = 0; seed < 30 && capturedDamage == null; seed++)
            {
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(seed));
            }

            Assert.IsNotNull(capturedDamage);
            Assert.IsTrue(capturedDamage.HasAttribute("Melee"));
            Assert.IsTrue(capturedDamage.HasAttribute("Strength"));
            // No spurious attributes from empty Attributes field
            Assert.AreEqual(2, capturedDamage.Attributes.Count,
                "Empty weapon Attributes should produce exactly 2 tags: Melee + stat name");
        }

        [Test]
        public void PerformMeleeAttack_TakeDamage_StillHasLegacyAmountParameter()
        {
            // Backward-compat: existing TakeDamage listeners that read the int "Amount"
            // parameter must keep working unchanged.
            var zone = new Zone();
            var attacker = MakeFighter(strength: 20, agility: 20);
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "10d6";
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 50;
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 50;
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 10, agility: 10);
            zone.AddEntity(defender, 6, 5);

            int capturedAmount = 0;
            var probe = new TakeDamageCaptureProbe();
            probe.OnTakeDamage = e => capturedAmount = e.GetIntParameter("Amount");
            defender.AddPart(probe);

            for (int seed = 0; seed < 10 && capturedAmount == 0; seed++)
            {
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(seed));
            }

            Assert.Greater(capturedAmount, 0, "Legacy 'Amount' parameter must still carry int damage");
        }

        // ====================================================================
        // Backward-compat overload — int ApplyDamage still works
        // ====================================================================

        [Test]
        public void ApplyDamage_IntOverload_DealsDamage_AsBefore()
        {
            var zone = new Zone();
            var target = MakeFighter(strength: 10, agility: 10);
            target.SetStatValue("Hitpoints", 30);
            zone.AddEntity(target, 5, 5);

            int before = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 7, source: null, zone: zone);
            int after = target.GetStatValue("Hitpoints");

            Assert.AreEqual(before - 7, after, "Legacy int overload must decrement HP by exact amount");
        }

        [Test]
        public void ApplyDamage_IntOverload_FiresTakeDamage_WithEmptyAttributesDamage()
        {
            // The wrapper builds Damage(amount) with no attributes. Listeners that
            // care about damage type get an empty Attributes list — which is fine
            // for legacy callers (status effect ticks, traps, falls, etc.).
            var zone = new Zone();
            var target = MakeFighter(strength: 10, agility: 10);
            zone.AddEntity(target, 5, 5);

            Damage capturedDamage = null;
            var probe = new TakeDamageCaptureProbe();
            probe.OnTakeDamage = e =>
            {
                if (e.GetParameter("Damage") is Damage d) capturedDamage = d;
            };
            target.AddPart(probe);

            CombatSystem.ApplyDamage(target, 5, source: null, zone: zone);

            Assert.IsNotNull(capturedDamage,
                "Legacy int overload must still expose the Damage object to listeners");
            Assert.AreEqual(5, capturedDamage.Amount);
            Assert.AreEqual(0, capturedDamage.Attributes.Count,
                "Legacy int overload produces an attribute-less Damage — listeners filter for typed source");
        }

        // ====================================================================
        // Adversarial — null/zero damage paths
        // ====================================================================

        [Test]
        public void ApplyDamage_NullDamage_NoOp()
        {
            var zone = new Zone();
            var target = MakeFighter(strength: 10, agility: 10);
            zone.AddEntity(target, 5, 5);

            int before = target.GetStatValue("Hitpoints");
            // Should not throw, should not crash, should not decrement HP
            CombatSystem.ApplyDamage(target, (Damage)null, source: null, zone: zone);
            int after = target.GetStatValue("Hitpoints");

            Assert.AreEqual(before, after, "Null Damage parameter must be a no-op");
        }

        [Test]
        public void ApplyDamage_ZeroAmountDamage_NoOp()
        {
            var zone = new Zone();
            var target = MakeFighter(strength: 10, agility: 10);
            zone.AddEntity(target, 5, 5);

            int before = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(0), source: null, zone: zone);
            int after = target.GetStatValue("Hitpoints");

            Assert.AreEqual(before, after, "Damage.Amount = 0 must be a no-op (matches int overload)");
        }

        [Test]
        public void ApplyDamage_NegativeAmount_DoesNothing_ViaClamp()
        {
            // Damage clamps Amount to ≥ 0 in the setter, so a -5 input yields
            // Amount = 0, which the early-exit guard treats as no-op.
            var zone = new Zone();
            var target = MakeFighter(strength: 10, agility: 10);
            zone.AddEntity(target, 5, 5);

            int before = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(-50), source: null, zone: zone);
            int after = target.GetStatValue("Hitpoints");

            Assert.AreEqual(before, after, "Negative damage clamps to 0 → no HP change");
        }

        // ====================================================================
        // Adversarial — typed Damage isn't aliased between calls
        // ====================================================================

        [Test]
        public void PerformMeleeAttack_DifferentSwings_GetIndependentDamageObjects()
        {
            // Sanity: each PerformSingleAttack constructs a fresh Damage. If the
            // Damage object were accidentally reused across attacks, attributes
            // would accumulate across attacks (visible to listeners).
            var zone = new Zone();
            var attacker = MakeFighter(strength: 20, agility: 20);
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 30;
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 10;
            attacker.GetPart<MeleeWeaponPart>().Attributes = "Slashing";
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 10, agility: 10);
            defender.SetStatValue("Hitpoints", 999);
            zone.AddEntity(defender, 6, 5);

            var capturedAttributeCounts = new List<int>();
            var probe = new TakeDamageCaptureProbe();
            probe.OnTakeDamage = e =>
            {
                if (e.GetParameter("Damage") is Damage d)
                    capturedAttributeCounts.Add(d.Attributes.Count);
            };
            defender.AddPart(probe);

            // 5 successful hits across many seeds
            for (int seed = 0; seed < 50 && capturedAttributeCounts.Count < 5; seed++)
            {
                defender.SetStatValue("Hitpoints", 999); // keep alive
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new System.Random(seed));
            }

            Assert.GreaterOrEqual(capturedAttributeCounts.Count, 3,
                "Test setup: need at least 3 successful hits to compare");
            // Every captured Damage should have the SAME small attribute count
            // (Melee + Strength + Slashing = 3). If they were aliased, counts would grow.
            for (int i = 0; i < capturedAttributeCounts.Count; i++)
            {
                Assert.AreEqual(3, capturedAttributeCounts[i],
                    $"Hit #{i}: each Damage must be fresh (Melee + Strength + Slashing = 3 attrs). " +
                    $"Growing count = aliasing bug.");
            }
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity MakeFighter(int strength, int agility, int hp = 30)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestFighter";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = agility, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Owner = entity, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "fighter" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart());
            entity.AddPart(new ArmorPart());
            return entity;
        }
    }

    /// <summary>
    /// Captures the TakeDamage event's parameters for inspection in tests.
    /// </summary>
    public class TakeDamageCaptureProbe : Part
    {
        public override string Name => "TakeDamageCaptureProbe";
        public System.Action<GameEvent> OnTakeDamage;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "TakeDamage")
                OnTakeDamage?.Invoke(e);
            return true;
        }
    }
}
