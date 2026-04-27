using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase F — `BeforeTakeDamage` event hook (Qud parity with
    /// <c>BeforeApplyDamageEvent</c> at Physics.cs:3418).
    ///
    /// User-visible invariants pinned by these tests:
    ///
    ///   1. When a Damage object is about to apply, listeners on the target
    ///      can hook a `BeforeTakeDamage` event to mutate the damage (reduce
    ///      by 2, change attributes) — and the mutation propagates to the
    ///      eventual HP decrement.
    ///
    ///   2. Listeners can VETO damage entirely by calling `e.Cancel()`. A
    ///      vetoed damage:
    ///        - does NOT fire `TakeDamage`
    ///        - does NOT decrement HP
    ///        - does fire `DamageFullyResisted` so observers know the attack
    ///          was attempted
    ///
    ///   3. `BeforeTakeDamage` fires BEFORE resistance is applied — listeners
    ///      see the pre-resistance damage. This lets resistance itself be
    ///      eventually re-implemented as a BeforeTakeDamage listener.
    ///
    ///   4. `BeforeTakeDamage` fires AFTER the dead-target / no-Hitpoints
    ///      guard, so listeners only fire on damageable targets.
    ///
    /// Counter-checks (Methodology Template §3.4):
    ///   • A listener that does nothing leaves damage unchanged
    ///   • A listener on the SOURCE (attacker) does NOT fire — only target's
    ///     listeners do
    ///   • A listener that mutates a Damage's attributes (e.g., adding "Fire")
    ///     causes downstream resistance to apply on the new attribute
    /// </summary>
    public class BeforeTakeDamageTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ====================================================================
        // 1. Listener can mutate damage.Amount before it applies
        // ====================================================================

        [Test]
        public void BeforeTakeDamage_ListenerReducesDamage_HpDecrementUsesMutated()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            zone.AddEntity(target, 5, 5);

            // Listener subtracts 5 from damage during BeforeTakeDamage
            var probe = new BeforeTakeDamageReducerProbe { ReduceBy = 5 };
            target.AddPart(probe);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(20), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // 20 → 15 via BeforeTakeDamage listener
            Assert.AreEqual(hpBefore - 15, hpAfter,
                "BeforeTakeDamage listener mutation must propagate to HP decrement");
        }

        // ====================================================================
        // 2. Listener can VETO damage by setting e.Cancel = true
        // ====================================================================

        [Test]
        public void BeforeTakeDamage_VetoListener_NoHpLoss_TakeDamageNotFired()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            zone.AddEntity(target, 5, 5);

            var vetoProbe = new BeforeTakeDamageVetoProbe();
            target.AddPart(vetoProbe);

            int takeDamageFired = 0;
            int fullyResistedFired = 0;
            var observer = new EventCaptureProbe();
            observer.OnEvent = e =>
            {
                if (e.ID == "TakeDamage") takeDamageFired++;
                if (e.ID == "DamageFullyResisted") fullyResistedFired++;
            };
            target.AddPart(observer);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(20), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter, "Vetoed damage must not decrement HP");
            Assert.AreEqual(0, takeDamageFired, "Vetoed damage must NOT fire TakeDamage");
            Assert.AreEqual(1, fullyResistedFired,
                "Vetoed damage should fire DamageFullyResisted so observers know the attack happened");
        }

        // ====================================================================
        // 3. BeforeTakeDamage fires BEFORE resistance — the listener sees
        //    pre-resistance damage value
        // ====================================================================

        [Test]
        public void BeforeTakeDamage_FiresBeforeResistance_ListenerSeesPreResistAmount()
        {
            // Setup: target has 50% AcidResistance. Damage starts at 20.
            // If BeforeTakeDamage fires BEFORE resistance, listener sees 20.
            // If AFTER resistance, listener sees 10.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            int amountSeenByListener = -1;
            var probe = new BeforeTakeDamageAmountProbe();
            probe.OnBefore = (d) => amountSeenByListener = d.Amount;
            target.AddPart(probe);

            var damage = new Damage(20);
            damage.AddAttribute("Acid");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);

            Assert.AreEqual(20, amountSeenByListener,
                "BeforeTakeDamage listener must see PRE-resistance damage (full 20, not the 10 after 50% resist)");
        }

        // ====================================================================
        // 4. BeforeTakeDamage fires AFTER the dead-target / no-Hitpoints guard
        // ====================================================================

        [Test]
        public void BeforeTakeDamage_DoesNotFire_OnAlreadyDeadTarget()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.GetStat("Hitpoints").BaseValue = 0;  // already dead
            zone.AddEntity(target, 5, 5);

            int beforeFired = 0;
            var probe = new EventCaptureProbe();
            probe.OnEvent = e =>
            {
                if (e.ID == "BeforeTakeDamage") beforeFired++;
            };
            target.AddPart(probe);

            CombatSystem.ApplyDamage(target, new Damage(20), source: null, zone: zone);

            Assert.AreEqual(0, beforeFired,
                "BeforeTakeDamage must NOT fire on already-dead targets " +
                "(matches the existing dead-target guard's contract)");
        }

        [Test]
        public void BeforeTakeDamage_DoesNotFire_OnTargetWithoutHitpointsStat()
        {
            var zone = new Zone();
            var target = new Entity();
            target.BlueprintName = "StatueProp";
            // Intentionally NO Hitpoints stat — represents a statue/prop
            target.AddPart(new RenderPart { DisplayName = "statue" });
            zone.AddEntity(target, 5, 5);

            int beforeFired = 0;
            var probe = new EventCaptureProbe();
            probe.OnEvent = e =>
            {
                if (e.ID == "BeforeTakeDamage") beforeFired++;
            };
            target.AddPart(probe);

            CombatSystem.ApplyDamage(target, new Damage(20), source: null, zone: zone);

            Assert.AreEqual(0, beforeFired,
                "BeforeTakeDamage must NOT fire on entities without a Hitpoints stat");
        }

        // ====================================================================
        // Counter-check: no-op listener leaves damage unchanged
        // ====================================================================

        [Test]
        public void BeforeTakeDamage_NoOpListener_DamageUnchanged()
        {
            // Counter-check for tests #1/#2: confirm that adding a listener
            // doesn't accidentally affect damage on its own; only mutations do.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            zone.AddEntity(target, 5, 5);

            // Listener that observes but does nothing
            var probe = new EventCaptureProbe();
            probe.OnEvent = e => { /* observe only */ };
            target.AddPart(probe);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(20), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 20, hpAfter,
                "No-op listener should leave damage unmodified (counter-check vs Test #1)");
        }

        // ====================================================================
        // Counter-check: listener mutating damage.Attributes lets downstream
        // resistance pick up the new attribute. Pins the "attribute mutation
        // propagates to resistance phase" contract.
        // ====================================================================

        [Test]
        public void BeforeTakeDamage_ListenerAddsFireAttribute_HeatResistanceApplies()
        {
            // Setup: target has 50% HeatResistance but no AcidResistance.
            // Initial damage tagged Acid. A BeforeTakeDamage listener adds
            // "Fire" attribute. Downstream resistance should now apply
            // HeatResistance (50% reduction) since the damage now has Fire.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            // Listener that adds Fire attribute on BeforeTakeDamage
            var probe = new BeforeTakeDamageAttrAddProbe { AttrToAdd = "Fire" };
            target.AddPart(probe);

            int hpBefore = target.GetStatValue("Hitpoints");
            var damage = new Damage(20);
            damage.AddAttribute("Acid");  // initially only Acid, no fire
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            // Without Fire attribute → no HeatResistance match → 20 dmg
            // With Fire attribute added by listener → HeatResistance halves → 10 dmg
            Assert.AreEqual(hpBefore - 10, hpAfter,
                "Listener-added Fire attribute should let HeatResistance reduce damage from 20 to 10");
        }

        // ====================================================================
        // Counter-check: listener on SOURCE doesn't fire (only target's listens)
        // ====================================================================

        [Test]
        public void BeforeTakeDamage_FiresOnTarget_NotOnSource()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            var source = MakeFighter(hp: 100);
            zone.AddEntity(target, 5, 5);
            zone.AddEntity(source, 6, 5);

            int beforeOnTarget = 0;
            int beforeOnSource = 0;
            target.AddPart(new EventCaptureProbe { OnEvent = e =>
                { if (e.ID == "BeforeTakeDamage") beforeOnTarget++; } });
            source.AddPart(new EventCaptureProbe { OnEvent = e =>
                { if (e.ID == "BeforeTakeDamage") beforeOnSource++; } });

            CombatSystem.ApplyDamage(target, new Damage(10), source, zone);

            Assert.AreEqual(1, beforeOnTarget, "Target's BeforeTakeDamage should fire");
            Assert.AreEqual(0, beforeOnSource, "Source's BeforeTakeDamage should NOT fire");
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
            entity.AddPart(new RenderPart { DisplayName = "fighter" });
            entity.AddPart(new PhysicsPart { Solid = true });
            return entity;
        }
    }

    /// <summary>
    /// Test probe that subtracts <see cref="ReduceBy"/> from damage during
    /// BeforeTakeDamage.
    /// </summary>
    public class BeforeTakeDamageReducerProbe : Part
    {
        public override string Name => "BeforeTakeDamageReducer";
        public int ReduceBy = 0;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeTakeDamage" && e.GetParameter("Damage") is Damage d)
                d.Amount -= ReduceBy;
            return true;
        }
    }

    /// <summary>
    /// Test probe that vetoes BeforeTakeDamage by returning false from
    /// <see cref="Part.HandleEvent"/>. This causes <c>FireEvent</c> to return
    /// false, which CombatSystem treats as a cancellation signal.
    /// </summary>
    public class BeforeTakeDamageVetoProbe : Part
    {
        public override string Name => "BeforeTakeDamageVeto";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeTakeDamage")
                return false;  // veto — same pattern as BeforeMeleeAttack cancellation
            return true;
        }
    }

    /// <summary>
    /// Test probe that captures the damage amount visible at BeforeTakeDamage time.
    /// </summary>
    public class BeforeTakeDamageAmountProbe : Part
    {
        public override string Name => "BeforeTakeDamageAmountProbe";
        public Action<Damage> OnBefore;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeTakeDamage" && e.GetParameter("Damage") is Damage d)
                OnBefore?.Invoke(d);
            return true;
        }
    }

    /// <summary>
    /// Test probe that adds a damage attribute during BeforeTakeDamage.
    /// Used to verify mutation propagates to downstream resistance.
    /// </summary>
    public class BeforeTakeDamageAttrAddProbe : Part
    {
        public override string Name => "BeforeTakeDamageAttrAddProbe";
        public string AttrToAdd;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeTakeDamage" && e.GetParameter("Damage") is Damage d
                && !string.IsNullOrEmpty(AttrToAdd))
            {
                d.AddAttribute(AttrToAdd);
            }
            return true;
        }
    }
}
