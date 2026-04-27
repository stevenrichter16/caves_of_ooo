using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tier 2.4 — `StoneskinEffect`: a production listener that hooks
    /// Phase F's `BeforeTakeDamage` event to reduce incoming damage by
    /// a configurable amount (default 2).
    ///
    /// User-visible invariant: "An entity with the Stoneskin effect takes
    /// 2 less damage per incoming hit. The reduction stacks correctly
    /// with Phase E elemental resistances and clamps damage to a minimum
    /// of 0 (cannot heal the target)."
    ///
    /// To support Effect-based listeners on the Phase F hook,
    /// `Effect.OnBeforeTakeDamage(Entity, GameEvent)` was added as a
    /// new virtual method, and `StatusEffectsPart.HandleEvent` now
    /// routes "BeforeTakeDamage" to per-effect dispatch (mirroring the
    /// existing `OnTakeDamage` routing pattern).
    /// </summary>
    public class StoneskinEffectTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ====================================================================
        // 1. Reduction propagates to HP decrement
        // ====================================================================

        [Test]
        public void Stoneskin_Reduces_IncomingDamage_By2()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.AddPart(new StatusEffectsPart());
            target.GetPart<StatusEffectsPart>().ApplyEffect(new StoneskinEffect());
            zone.AddEntity(target, 5, 5);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(10), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 8, hpAfter,
                "Stoneskin should reduce 10 dmg → 8 actual (default Reduction=2)");
        }

        [Test]
        public void Stoneskin_DoesNotHeal_WhenReductionExceedsDamage()
        {
            // Damage of 1, Stoneskin -2 → would be -1, but Damage.Amount setter
            // clamps to 0. Resulting damage is 0; the resistance early-out path
            // takes over (DamageFullyResisted fires; HP unchanged).
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.AddPart(new StatusEffectsPart());
            target.GetPart<StatusEffectsPart>().ApplyEffect(new StoneskinEffect());
            zone.AddEntity(target, 5, 5);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(1), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "Stoneskin's reduction must clamp to 0, not heal");
        }

        // ====================================================================
        // 2. Stacks correctly with Phase E elemental resistance
        // ====================================================================

        [Test]
        public void Stoneskin_Plus_AcidResistance50_OnAcidDamage_OrderedReduction()
        {
            // Damage = 10. Stoneskin (BeforeTakeDamage) → 8. Then
            // AcidResistance 50% → 4. Final HP delta = 4.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.AddPart(new StatusEffectsPart());
            target.GetPart<StatusEffectsPart>().ApplyEffect(new StoneskinEffect());
            target.Statistics["AcidResistance"] = new Stat
                { Owner = target, Name = "AcidResistance", BaseValue = 50, Min = 0, Max = 200 };
            zone.AddEntity(target, 5, 5);

            int hpBefore = target.GetStatValue("Hitpoints");
            var damage = new Damage(10);
            damage.AddAttribute("Acid");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 4, hpAfter,
                "Stoneskin -2 then AcidRes 50% → 10 → 8 → 4 actual damage");
        }

        // ====================================================================
        // 3. Counter-check: no Stoneskin → full damage
        // ====================================================================

        [Test]
        public void NoStoneskin_TakesFullDamage()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.AddPart(new StatusEffectsPart());  // empty effects part
            zone.AddEntity(target, 5, 5);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(10), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 10, hpAfter,
                "Without Stoneskin, full damage applies");
        }

        // ====================================================================
        // 4. Custom reduction value via constructor
        // ====================================================================

        [Test]
        public void Stoneskin_CustomReduction_Reduces_ByGivenAmount()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.AddPart(new StatusEffectsPart());
            target.GetPart<StatusEffectsPart>().ApplyEffect(new StoneskinEffect(reduction: 5));
            zone.AddEntity(target, 5, 5);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(10), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 5, hpAfter,
                "Reduction=5 → 10 dmg becomes 5 actual");
        }

        // ====================================================================
        // 5. Stoneskin doesn't break the Damage attributes list
        // ====================================================================

        [Test]
        public void Stoneskin_Preserves_DamageAttributes()
        {
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.AddPart(new StatusEffectsPart());
            target.GetPart<StatusEffectsPart>().ApplyEffect(new StoneskinEffect());

            // Probe captures the Damage object during TakeDamage to inspect
            // its attributes after Stoneskin had its turn.
            Damage capturedAtTakeDamage = null;
            target.AddPart(new TakeDamageCaptureProbe
            {
                OnTakeDamage = e =>
                {
                    if (e.GetParameter("Damage") is Damage d) capturedAtTakeDamage = d;
                }
            });
            zone.AddEntity(target, 5, 5);

            var damage = new Damage(10);
            damage.AddAttribute("Cutting");
            damage.AddAttribute("LongBlades");
            CombatSystem.ApplyDamage(target, damage, source: null, zone: zone);

            Assert.IsNotNull(capturedAtTakeDamage);
            Assert.IsTrue(capturedAtTakeDamage.HasAttribute("Cutting"),
                "Stoneskin must not strip Cutting attribute");
            Assert.IsTrue(capturedAtTakeDamage.HasAttribute("LongBlades"),
                "Stoneskin must not strip LongBlades attribute");
        }

        // ====================================================================
        // 6. Stoneskin doesn't fire on already-dead targets
        // ====================================================================

        [Test]
        public void Stoneskin_DoesNotFire_OnAlreadyDeadTarget()
        {
            // The dead-target guard in ApplyDamage early-returns before
            // BeforeTakeDamage fires, so Stoneskin never gets a chance to
            // observe a damage attempt on a corpse. Verifying via probe
            // count.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.AddPart(new StatusEffectsPart());
            target.GetPart<StatusEffectsPart>().ApplyEffect(new StoneskinEffect());
            target.GetStat("Hitpoints").BaseValue = 0;  // pre-killed
            zone.AddEntity(target, 5, 5);

            int beforeFired = 0;
            target.AddPart(new EventCaptureProbe
            {
                OnEvent = e =>
                {
                    if (e.ID == "BeforeTakeDamage") beforeFired++;
                }
            });

            CombatSystem.ApplyDamage(target, new Damage(10), source: null, zone: zone);

            Assert.AreEqual(0, beforeFired,
                "BeforeTakeDamage must not fire on dead targets — Stoneskin shouldn't observe");
        }

        // ====================================================================
        // 7. Adversarial: two Stoneskin instances stack additively
        // ====================================================================

        [Test]
        public void TwoStoneskinInstances_Stack_Additively()
        {
            // Bleeding precedent: stacking applies the effect again rather
            // than duplicating. We follow the same pattern — two separate
            // StoneskinEffect applications produce two listeners, each
            // reducing damage by their own amount. Total reduction = sum.
            var zone = new Zone();
            var target = MakeFighter(hp: 100);
            target.AddPart(new StatusEffectsPart());
            var seffects = target.GetPart<StatusEffectsPart>();
            seffects.ApplyEffect(new StoneskinEffect(reduction: 2));
            seffects.ApplyEffect(new StoneskinEffect(reduction: 3));
            zone.AddEntity(target, 5, 5);

            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, new Damage(10), source: null, zone: zone);
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore - 5, hpAfter,
                "Two Stoneskin (2 + 3) should stack: 10 - 2 - 3 = 5 actual damage");
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
}
