using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tier-2 trap furniture content — SpikeTrap, FireTrap, BearTrap.
    /// All three reuse <see cref="TriggerOnStepPart"/>'s
    /// <c>EntityEnteredCell</c> dispatch (the same path runes use).
    /// Test pattern mirrors <c>TriggerOnStepPartTests</c>.
    ///
    /// User-visible invariant: stepping onto a trap fires once,
    /// applies the trap's damage + status payload, and consumes the
    /// trap entity (single-use). Trap-faction layers don't trigger
    /// their own traps.
    /// </summary>
    [TestFixture]
    public class TrapFurnitureTests
    {
        [SetUp] public void Setup() => MessageLog.Clear();

        // ====================================================================
        // SpikeTrap
        // ====================================================================

        [Test]
        public void SpikeTrap_OnStep_DealsPiercingDamage()
        {
            var zone = new Zone("TestZone");
            var trap = MakeTrap(zone, 5, 5, new SpikeTrapTriggerPart { Damage = 12 });
            var stepper = MakeStepper(zone, 4, 5, hp: 30);

            int hpBefore = stepper.GetStatValue("Hitpoints");
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);
            int hpAfter = stepper.GetStatValue("Hitpoints");

            Assert.AreEqual(12, hpBefore - hpAfter,
                "SpikeTrap must deal exactly its configured Damage on step.");
        }

        [Test]
        public void SpikeTrap_OnStep_RemovesSelfFromZone()
        {
            var zone = new Zone("TestZone");
            var trap = MakeTrap(zone, 5, 5, new SpikeTrapTriggerPart { Damage = 12 });
            var stepper = MakeStepper(zone, 4, 5);

            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            Assert.IsNull(zone.GetEntityCell(trap),
                "SpikeTrap with ConsumeOnTrigger=true (default) must remove itself after firing.");
        }

        [Test]
        public void SpikeTrap_TriggeredTwice_OnlyFiresOnce()
        {
            // After the first stepper triggers + removes the trap, a
            // second stepper entering the same cell should take no
            // damage. Adversarial guard against the "consumed but still
            // present" double-fire.
            var zone = new Zone("TestZone");
            var trap = MakeTrap(zone, 5, 5, new SpikeTrapTriggerPart { Damage = 12 });
            var first = MakeStepper(zone, 4, 5, hp: 30);
            var second = MakeStepper(zone, 6, 5, hp: 30);
            second.ID = "stepper-2";

            MovementSystem.TryMove(first, zone, dx: 1, dy: 0);
            int firstDmg = 30 - first.GetStatValue("Hitpoints");

            MovementSystem.TryMove(second, zone, dx: -1, dy: 0);
            int secondDmg = 30 - second.GetStatValue("Hitpoints");

            Assert.Greater(firstDmg, 0, "First stepper triggers the trap.");
            Assert.AreEqual(0, secondDmg,
                "Second stepper entering same cell after consumption must take no damage.");
        }

        [Test]
        public void SpikeTrap_LayerFaction_DoesNotTrigger()
        {
            // TriggerFaction set to "Goblins" — a Goblin-faction stepper
            // walks over the trap unscathed. Mirrors how rune-laying
            // NPCs avoid their own runes.
            var zone = new Zone("TestZone");
            var trap = MakeTrap(zone, 5, 5,
                new SpikeTrapTriggerPart { Damage = 12, TriggerFaction = "Goblins" });
            var goblin = MakeStepper(zone, 4, 5, hp: 30, faction: "Goblins");

            MovementSystem.TryMove(goblin, zone, dx: 1, dy: 0);

            Assert.AreEqual(30, goblin.GetStatValue("Hitpoints"),
                "Faction-matched stepper must not trigger the trap.");
            Assert.IsNotNull(zone.GetEntityCell(trap),
                "Faction-matched skipped trap must NOT consume itself.");
        }

        // ====================================================================
        // FireTrap
        // ====================================================================

        [Test]
        public void FireTrap_OnStep_AppliesBurningEffect()
        {
            var zone = new Zone("TestZone");
            var trap = MakeTrap(zone, 5, 5,
                new FireTrapTriggerPart { Damage = 8, BurnIntensity = 1.5f });
            var stepper = MakeStepper(zone, 4, 5, hp: 30);

            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            Assert.IsTrue(stepper.HasEffect<BurningEffect>(),
                "FireTrap must apply BurningEffect on step.");
        }

        [Test]
        public void FireTrap_OnStep_DealsFireDamage()
        {
            var zone = new Zone("TestZone");
            var trap = MakeTrap(zone, 5, 5, new FireTrapTriggerPart { Damage = 8 });
            var stepper = MakeStepper(zone, 4, 5, hp: 50);

            int hpBefore = stepper.GetStatValue("Hitpoints");
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);
            int hpAfter = stepper.GetStatValue("Hitpoints");

            Assert.AreEqual(8, hpBefore - hpAfter,
                "FireTrap deals exactly Damage on the trigger hit (no resistance on this stepper).");
        }

        [Test]
        public void FireTrap_OnStep_DamageRoutesThroughHeatResistance()
        {
            // Counter-check: Glowmaw-shaped target with HR=50 should take
            // less damage than the plain stepper above. Confirms the
            // "Fire" attribute is set on the Damage instance.
            var zone = new Zone("TestZone");
            var trap = MakeTrap(zone, 5, 5, new FireTrapTriggerPart { Damage = 8 });
            var resistant = MakeStepper(zone, 4, 5, hp: 50);
            resistant.Statistics["HeatResistance"] = new Stat
                { Owner = resistant, Name = "HeatResistance", BaseValue = 50, Min = 0, Max = 200 };

            int hpBefore = resistant.GetStatValue("Hitpoints");
            MovementSystem.TryMove(resistant, zone, dx: 1, dy: 0);
            int hpAfter = resistant.GetStatValue("Hitpoints");

            Assert.Less(hpBefore - hpAfter, 8,
                "HeatResistance=50 must reduce FireTrap damage; pre-routing fix it would have taken full 8.");
        }

        // ====================================================================
        // BearTrap
        // ====================================================================

        [Test]
        public void BearTrap_OnStep_AppliesAllThreePayloads()
        {
            // Damage + Stunned + Bleeding all in one step.
            var zone = new Zone("TestZone");
            var trap = MakeTrap(zone, 5, 5,
                new BearTrapTriggerPart { Damage = 15, StunDuration = 1, BleedSaveTarget = 14 });
            var stepper = MakeStepper(zone, 4, 5, hp: 50);

            int hpBefore = stepper.GetStatValue("Hitpoints");
            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            int hpAfter = stepper.GetStatValue("Hitpoints");
            Assert.AreEqual(15, hpBefore - hpAfter, "BearTrap deals 15 piercing damage.");

            Assert.IsTrue(stepper.HasEffect<StunnedEffect>(), "BearTrap applies Stunned.");
            Assert.IsTrue(stepper.HasEffect<BleedingEffect>(), "BearTrap applies Bleeding.");
        }

        [Test]
        public void BearTrap_StunnedEffect_BlocksAction()
        {
            // Adversarial: the Stunned the trap applies must actually
            // block the next turn's action. Pin the AllowAction=false
            // contract end-to-end through the trap path.
            var zone = new Zone("TestZone");
            var trap = MakeTrap(zone, 5, 5, new BearTrapTriggerPart());
            var stepper = MakeStepper(zone, 4, 5, hp: 50);

            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            var stun = stepper.GetEffect<StunnedEffect>();
            Assert.IsNotNull(stun);
            Assert.IsFalse(stun.AllowAction(stepper),
                "Stunned applied by BearTrap must block actions while active.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeStepper(Zone zone, int x, int y, int hp = 20, string faction = null)
        {
            var e = new Entity { BlueprintName = "TestStepper", ID = "stepper-" + x + "-" + y };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "stepper" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat
                { Name = "Hitpoints", Owner = e, BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Toughness"] = new Stat
                { Name = "Toughness", Owner = e, BaseValue = 10 };
            if (faction != null) e.SetTag("Faction", faction);
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity MakeTrap<TPart>(Zone zone, int x, int y, TPart part)
            where TPart : TriggerOnStepPart
        {
            var e = new Entity { BlueprintName = "TestTrap", ID = "trap-" + x + "-" + y };
            e.AddPart(new RenderPart { DisplayName = "trap" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(part);
            zone.AddEntity(e, x, y);
            return e;
        }
    }
}
