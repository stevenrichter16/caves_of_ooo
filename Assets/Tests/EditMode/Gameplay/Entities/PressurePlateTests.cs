using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// T2.3 — PressurePlateTriggerPart: rearmable variant of TriggerOnStepPart.
    ///
    /// Pins the contract that distinguishes PressurePlate from one-shot
    /// SpikeTrap: the plate persists in the zone after firing, so a second
    /// step (after stepping off and back on) re-triggers it. EntityEnteredCell
    /// fires only on cell-CHANGE moves, so a stationary actor on the plate
    /// does NOT re-trigger every turn — that's the correct semantics, not a
    /// bug to debounce.
    ///
    /// Tests pin:
    ///   1. Step on plate → damage applied (positive).
    ///   2. Step off and back on → plate re-fires (rearmable contract — this
    ///      is the asymmetry vs SpikeTrap).
    ///   3. Plate persists in zone after firing (counter-check vs SpikeTrap).
    ///   4. Configured DamageAttribute reaches the Damage object (pin the
    ///      Bludgeoning/Piercing attribute pass-through — proves the plate
    ///      isn't silently emitting untyped damage).
    ///   5. FactionMate doesn't trigger (counter-check the inherited filter
    ///      from TriggerOnStepPart base — proves the new subclass doesn't
    ///      bypass it).
    /// </summary>
    [TestFixture]
    public class PressurePlateTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            AsciiFxBus.Clear();
        }

        // ====================================================================
        // 1. Positive: step on plate deals damage
        // ====================================================================

        [Test]
        public void Step_FirstTime_DealsDamage()
        {
            var zone = new Zone("TestZone");
            var plate = MakePlate(zone, 5, 5, damage: 8);
            var stepper = MakeStepper(zone, 4, 5, hp: 30);

            int hpBefore = stepper.GetStatValue("Hitpoints");
            bool moved = MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            Assert.IsTrue(moved, "Stepper must move into the plate's cell");
            int hpAfter = stepper.GetStatValue("Hitpoints");
            Assert.Less(hpAfter, hpBefore,
                $"PressurePlate must deal damage on first step. " +
                $"HP {hpBefore} → {hpAfter}.");
            // Sanity: assume no resistances/AV → exact damage = configured
            // (hpBefore - hpAfter) should match the configured Damage of 8.
            Assert.AreEqual(8, hpBefore - hpAfter,
                "PressurePlate damage should match its configured Damage field.");
        }

        // ====================================================================
        // 2. Step off and back on → re-fires (rearmable contract)
        // ====================================================================

        [Test]
        public void StepOff_AndBackOn_FiresAgain()
        {
            var zone = new Zone("TestZone");
            var plate = MakePlate(zone, 5, 5, damage: 8);
            var stepper = MakeStepper(zone, 4, 5, hp: 50);

            // Step ON.
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);
            int hpAfterFirst = stepper.GetStatValue("Hitpoints");

            // Step OFF.
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);
            int hpAfterStepOff = stepper.GetStatValue("Hitpoints");

            // Step BACK ON.
            MovementSystem.TryMove(stepper, zone, dx: -1, dy: 0);
            int hpAfterSecond = stepper.GetStatValue("Hitpoints");

            Assert.Less(hpAfterFirst, 50,
                "First step should deal damage.");
            Assert.AreEqual(hpAfterFirst, hpAfterStepOff,
                "Stepping OFF the plate doesn't refire (no damage on leave).");
            Assert.Less(hpAfterSecond, hpAfterFirst,
                "Stepping BACK ON the plate must re-fire (rearmable contract). " +
                $"HP after first step={hpAfterFirst}, after second={hpAfterSecond}.");
        }

        // ====================================================================
        // 3. Plate persists in zone after firing (counter-check vs SpikeTrap)
        // ====================================================================

        [Test]
        public void Step_DoesNotConsumePlate()
        {
            var zone = new Zone("TestZone");
            var plate = MakePlate(zone, 5, 5, damage: 8);
            var stepper = MakeStepper(zone, 4, 5, hp: 50);

            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            // Plate must STILL be in the zone — counter-check vs SpikeTrap
            // which would have RemoveEntity'd itself.
            var plateCell = zone.GetEntityCell(plate);
            Assert.IsNotNull(plateCell,
                "PressurePlate must remain in the zone after firing " +
                "(rearmable, ConsumeOnTrigger=false). SpikeTrap would consume " +
                "itself; PressurePlate is the negative-of-that contract.");
        }

        // ====================================================================
        // 4. Damage attribute pass-through
        // ====================================================================

        [Test]
        public void Step_AppliesConfiguredDamageAttribute()
        {
            // Pin that DamageAttribute makes it onto the Damage object —
            // a future refactor that mis-routes the field would silently
            // strip resistance routing (e.g. Heat plates wouldn't fire
            // HeatResistance). Smoke this against a defender with a
            // resistance to a NON-default attribute to prove the routing
            // hits the configured one.

            var zone = new Zone("TestZone");
            var plate = MakePlate(zone, 5, 5, damage: 20,
                damageAttribute: "Fire");
            var stepper = MakeStepper(zone, 4, 5, hp: 100);
            // Defender resists Heat 100% → Fire-tagged damage fully absorbed.
            stepper.Statistics["HeatResistance"] = new Stat
            {
                Owner = stepper, Name = "HeatResistance",
                BaseValue = 100, Min = -100, Max = 200
            };

            int hpBefore = stepper.GetStatValue("Hitpoints");
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);
            int hpAfter = stepper.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "PressurePlate with DamageAttribute=Fire must route through " +
                "HeatResistance — defender at 100% Heat resist takes 0 damage. " +
                $"HP {hpBefore} → {hpAfter}. If damage landed, the attribute " +
                "isn't reaching the Damage object → resistance routing is broken.");
        }

        // ====================================================================
        // 5. End-to-end: step on plate → floating damage number emits
        //    Cold-eye Finding 9: pin the trap-→-ApplyDamage-→-floating-
        //    number wiring at the integration level. Without this, a
        //    refactor that bypasses ApplyDamage (e.g. direct HP decrement)
        //    would silently regress the user-visible feature without
        //    failing any other test.
        // ====================================================================

        [Test]
        public void Step_EmitsFloatingDamageNumber()
        {
            var zone = new Zone("TestZone");
            var plate = MakePlate(zone, 5, 5, damage: 8);
            var stepper = MakeStepper(zone, 4, 5, hp: 50);

            AsciiFxBus.Clear();
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            var requests = AsciiFxBus.Drain();
            // Damage 8 = single-digit → 1 Particle for the floating number.
            int particleCount = 0;
            foreach (var r in requests)
                if (r.Type == AsciiFxRequestType.Particle) particleCount++;

            Assert.AreEqual(1, particleCount,
                $"Stepping on the plate (damage=8, single-digit) must emit 1 " +
                $"Particle for the floating damage number. Got {particleCount}. " +
                $"If 0: ApplyDamage isn't being called by the plate (regression " +
                $"in OnTrigger), or the floating-number emit was removed from " +
                $"ApplyDamage.");
        }

        // ====================================================================
        // 6. FactionMate filter (inherited from TriggerOnStepPart)
        // ====================================================================

        [Test]
        public void FactionMate_DoesNotTrigger()
        {
            // Counter-check the inherited TriggerFaction filter on the new
            // subclass. A creature with the same Faction as the plate's
            // TriggerFaction must NOT take damage when stepping on it
            // (a builder/laying NPC stepping back over its own plate).
            var zone = new Zone("TestZone");
            var plate = MakePlate(zone, 5, 5, damage: 8,
                triggerFaction: "Cultists");
            var stepper = MakeStepper(zone, 4, 5, hp: 50, faction: "Cultists");

            int hpBefore = stepper.GetStatValue("Hitpoints");
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);
            int hpAfter = stepper.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "Faction-mate stepper must not trigger the plate. " +
                $"HP {hpBefore} → {hpAfter} indicates the inherited " +
                "TriggerFaction filter from TriggerOnStepPart is broken.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakePlate(Zone zone, int x, int y,
            int damage = 8, string damageAttribute = "Bludgeoning",
            string triggerFaction = null)
        {
            var e = new Entity { BlueprintName = "TestPlate", ID = "plate-1" };
            e.AddPart(new RenderPart { DisplayName = "pressure plate" });
            e.AddPart(new PhysicsPart { Solid = false });
            var plate = new PressurePlateTriggerPart
            {
                Damage = damage,
                DamageAttribute = damageAttribute,
                TriggerFaction = triggerFaction,
            };
            e.AddPart(plate);
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity MakeStepper(Zone zone, int x, int y,
            int hp = 30, string faction = null)
        {
            var e = new Entity { BlueprintName = "TestStepper", ID = "stepper-1" };
            e.AddPart(new RenderPart { DisplayName = "stepper" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", Owner = e, BaseValue = hp, Min = 0, Max = hp };
            if (faction != null) e.SetTag("Faction", faction);
            zone.AddEntity(e, x, y);
            return e;
        }
    }
}
