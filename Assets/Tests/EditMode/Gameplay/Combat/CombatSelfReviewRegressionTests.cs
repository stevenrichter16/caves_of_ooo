using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Regression tests pinning the fixes from the Phase A/C/D/E self-review.
    /// See <c>Docs/COMBAT-PARITY-PORT-REVIEW.md</c> for the findings.
    ///
    /// Each test method name references the finding number it pins.
    /// </summary>
    public class CombatSelfReviewRegressionTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ====================================================================
        // Finding 1 — TakeDamage listeners can mutate damage.Amount in-flight,
        // and the mutation propagates to the HP decrement.
        //
        // Pre-fix: ApplyDamage captured `int amount = damage.Amount` BEFORE
        // firing the TakeDamage event, so listener mutations were ignored.
        //
        // Post-fix: amount is read AFTER the event, so listener mutations
        // (e.g., a "StoneSkin" effect reducing damage by 2) actually apply.
        // ====================================================================

        [Test]
        public void Finding1_TakeDamageListener_CanReduceDamageBeforeHpDecrement()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            zone.AddEntity(target, 5, 5);

            // Probe that subtracts 5 from damage when TakeDamage fires —
            // simulates a "StoneSkin" damage-reduction effect.
            var probe = new DamageReducerProbe { ReduceBy = 5 };
            target.AddPart(probe);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(20), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // Without the fix: HP would drop by 20 (captured-before-event).
            // With the fix: HP drops by 15 (20 - 5 from probe).
            Assert.AreEqual(hpBefore - 15, hpAfter,
                "Listener mutation of damage.Amount must propagate to HP decrement");
        }

        [Test]
        public void Finding1_TakeDamageListener_CanFullyAbsorbDamage_NoHpLoss()
        {
            // A listener can reduce damage to 0; HP should not change.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            zone.AddEntity(target, 5, 5);

            var probe = new DamageReducerProbe { ReduceBy = 999 };  // over-reduce
            target.AddPart(probe);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(20), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "Listener that drops damage to 0 must result in no HP loss");
        }

        [Test]
        public void Finding1_TakeDamageListener_CannotHealViaNegativeAmount()
        {
            // Defensive: even if a malformed listener sets damage.Amount to a
            // negative number (which the Damage setter clamps to 0), HP must
            // not increase. The post-event Math.Max(0, damage.Amount) guards
            // this.
            var zone = new Zone();
            var target = MakeFighter(hp: 50);
            zone.AddEntity(target, 5, 5);

            var probe = new DamageReducerProbe { ReduceBy = 999 };  // over-reduce → Amount setter clamps to 0
            target.AddPart(probe);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(10), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter, "Negative-amount mutation must not heal target");
        }

        // ====================================================================
        // Finding 4 — Full-resistance fires DamageFullyResisted event so
        // listeners (UI, AI retaliation, achievements) can react even when
        // no HP was lost.
        // ====================================================================

        [Test]
        public void Finding4_FullResistance_FiresDamageFullyResistedEvent()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 100, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            bool fullyResistedFired = false;
            Damage capturedDamage = null;
            var probe = new EventCaptureProbe();
            probe.OnEvent = e =>
            {
                if (e.ID == "DamageFullyResisted")
                {
                    fullyResistedFired = true;
                    if (e.GetParameter("Damage") is Damage d) capturedDamage = d;
                }
            };
            target.AddPart(probe);

            var damage = new Damage(20);
            damage.AddAttribute("Acid");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);

            Assert.IsTrue(fullyResistedFired,
                "100% resistance should fire DamageFullyResisted so listeners observe the attack");
            Assert.IsNotNull(capturedDamage, "Damage object should be on the event");
            Assert.AreEqual(0, capturedDamage.Amount, "Amount should be 0 by the time the event fires");
        }

        [Test]
        public void Finding4_FullResistance_DoesNotFire_TakeDamageEvent()
        {
            // The TakeDamage event should NOT fire when resistance fully
            // absorbs — the dedicated DamageFullyResisted event takes its place.
            // This keeps poison/bleed/on-hit-status listeners from triggering on
            // a no-op attack.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 100, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            int takeDamageFires = 0;
            var probe = new EventCaptureProbe();
            probe.OnEvent = e =>
            {
                if (e.ID == "TakeDamage") takeDamageFires++;
            };
            target.AddPart(probe);

            var damage = new Damage(20);
            damage.AddAttribute("Fire");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);

            Assert.AreEqual(0, takeDamageFires,
                "Fully-resisted damage must not fire TakeDamage (would trigger spurious on-hit effects)");
        }

        [Test]
        public void Finding4_PartialResistance_StillFires_TakeDamage_NotFullyResisted()
        {
            // Counter-check: if resistance only PARTIALLY absorbs, TakeDamage
            // still fires (and DamageFullyResisted does NOT).
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            int takeDamageFires = 0;
            int fullyResistedFires = 0;
            var probe = new EventCaptureProbe();
            probe.OnEvent = e =>
            {
                if (e.ID == "TakeDamage") takeDamageFires++;
                if (e.ID == "DamageFullyResisted") fullyResistedFires++;
            };
            target.AddPart(probe);

            var damage = new Damage(20);
            damage.AddAttribute("Acid");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);

            Assert.AreEqual(1, takeDamageFires, "Partial resistance still lets TakeDamage fire");
            Assert.AreEqual(0, fullyResistedFires, "DamageFullyResisted only fires on full absorption");
        }

        // ====================================================================
        // Finding 5 — LEGACY_UNCAPPED_MAX_STR_BONUS constant is exposed and
        // matches the documented value (50). This pins the constant against
        // accidental rename or value drift.
        // ====================================================================

        [Test]
        public void Finding5_LegacyUncappedConstant_ExposesExpectedValue()
        {
            Assert.AreEqual(50, CombatSystem.LEGACY_UNCAPPED_MAX_STR_BONUS,
                "Constant pins the legacy 'uncapped' substitute value");
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

    /// <summary>
    /// Test probe that subtracts <see cref="ReduceBy"/> from damage when
    /// <c>TakeDamage</c> fires. Simulates a "StoneSkin"-style listener.
    /// </summary>
    public class DamageReducerProbe : Part
    {
        public override string Name => "DamageReducerProbe";
        public int ReduceBy = 0;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "TakeDamage" && e.GetParameter("Damage") is Damage d)
            {
                d.Amount -= ReduceBy;  // Damage.Amount setter clamps to ≥ 0
            }
            return true;
        }
    }

    /// <summary>
    /// Test probe that captures arbitrary events. Useful for asserting
    /// presence/absence of fired events.
    /// </summary>
    public class EventCaptureProbe : Part
    {
        public override string Name => "EventCaptureProbe";
        public Action<GameEvent> OnEvent;

        public override bool HandleEvent(GameEvent e)
        {
            OnEvent?.Invoke(e);
            return true;
        }
    }
}
