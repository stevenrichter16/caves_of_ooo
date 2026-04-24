using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M6.1 — TriggerOnStepPart + MovementSystem EntityEnteredCell wiring.
    ///
    /// Covers:
    /// <list type="bullet">
    ///   <item>MovementSystem fires <c>EntityEnteredCell</c> on non-mover occupants.</item>
    ///   <item>Mover itself is excluded from dispatch (self-iter guard).</item>
    ///   <item>Rune applies damage + status effect on trigger.</item>
    ///   <item>Rune is consumed (removed from zone) when ConsumeOnTrigger=true.</item>
    ///   <item>Faction filter skips allies of the layer.</item>
    ///   <item>ConsumeOnTrigger=false leaves the rune in place for repeat triggers.</item>
    ///   <item>Three rune variants (Flame / Frost / Poison) apply the right effect.</item>
    /// </list>
    ///
    /// Mirrors Qud's <c>Tinkering_Mine.cs:428</c> <c>ObjectEnteredCellEvent</c>
    /// handler; CoO's equivalent is <c>EntityEnteredCell</c> fired from
    /// <see cref="MovementSystem.FireCellEnteredEvents"/>.
    /// </summary>
    [TestFixture]
    public class TriggerOnStepPartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity CreateStepper(Zone zone, int x, int y, int hp = 20, string faction = null)
        {
            var e = new Entity { BlueprintName = "TestStepper", ID = "stepper-1" };
            e.AddPart(new RenderPart { DisplayName = "stepper" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            if (faction != null) e.SetTag("Faction", faction);
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity CreateRune<TPart>(Zone zone, int x, int y, TPart part)
            where TPart : TriggerOnStepPart
        {
            var e = new Entity { BlueprintName = "TestRune", ID = "rune-1" };
            e.AddPart(new RenderPart { DisplayName = "rune" });
            // Rune is NOT solid — the stepper must be able to enter its cell.
            e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(part);
            zone.AddEntity(e, x, y);
            return e;
        }

        // ====================================================================
        // MovementSystem wiring
        // ====================================================================

        [Test]
        public void Movement_FiresEntityEnteredCell_OnNonMoverOccupants()
        {
            var zone = new Zone("TestZone");
            var witness = new Entity { BlueprintName = "Witness", ID = "w-1" };
            witness.AddPart(new PhysicsPart { Solid = false });

            // A trivial test-only part that records a receipt when the event fires.
            var receipt = new TestEnteredCellRecorder();
            witness.AddPart(receipt);
            zone.AddEntity(witness, 5, 5);

            var mover = CreateStepper(zone, 4, 5);
            bool moved = MovementSystem.TryMove(mover, zone, dx: 1, dy: 0);

            Assert.IsTrue(moved, "Movement into an occupied, non-solid cell should succeed.");
            Assert.AreEqual(1, receipt.Count,
                "EntityEnteredCell must fire exactly once on the non-mover occupant.");
            Assert.AreSame(mover, receipt.LastActor,
                "Event 'Actor' parameter must be the moving entity.");
        }

        [Test]
        public void Movement_DoesNotFireEntityEnteredCell_OnMoverItself()
        {
            // A mover carrying a TriggerOnStepPart-style listener should NOT
            // dispatch the event to itself when it arrives at a new cell.
            // Guards against self-triggering rune-laying NPCs.
            var zone = new Zone("TestZone");
            var mover = new Entity { BlueprintName = "SelfMover", ID = "sm-1" };
            mover.AddPart(new PhysicsPart { Solid = false });
            var receipt = new TestEnteredCellRecorder();
            mover.AddPart(receipt);
            mover.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            zone.AddEntity(mover, 4, 5);

            bool moved = MovementSystem.TryMove(mover, zone, dx: 1, dy: 0);

            Assert.IsTrue(moved);
            Assert.AreEqual(0, receipt.Count,
                "Mover must be excluded from its own EntityEnteredCell dispatch.");
        }

        // ====================================================================
        // Rune of Flame
        // ====================================================================

        [Test]
        public void RuneFlame_DamagesStepper_AndAppliesSmoldering()
        {
            var zone = new Zone("TestZone");
            var rune = CreateRune(zone, 5, 5, new RuneFlameTriggerPart { Damage = 5, SmolderDuration = 3 });
            var stepper = CreateStepper(zone, 4, 5, hp: 20);

            bool moved = MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            Assert.IsTrue(moved, "Stepper should be able to walk onto a non-solid rune.");
            Assert.AreEqual(15, stepper.GetStatValue("Hitpoints"),
                "Flame rune should apply its Damage value to the stepper.");
            var sep = stepper.GetPart<StatusEffectsPart>();
            Assert.IsNotNull(sep, "Stepper should auto-gain a StatusEffectsPart when an effect is applied.");
            Assert.IsTrue(sep.HasEffect<SmolderingEffect>(),
                "Flame rune should apply SmolderingEffect on trigger.");
        }

        [Test]
        public void RuneFlame_ConsumedOnTrigger_ByDefault()
        {
            var zone = new Zone("TestZone");
            var rune = CreateRune(zone, 5, 5, new RuneFlameTriggerPart { Damage = 5 });
            var stepper = CreateStepper(zone, 4, 5);

            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            Assert.IsNull(zone.GetEntityCell(rune),
                "ConsumeOnTrigger defaults to true — the rune should be removed from the zone after firing.");
        }

        [Test]
        public void Rune_NotConsumed_WhenConsumeOnTriggerFalse()
        {
            var zone = new Zone("TestZone");
            var rune = CreateRune(zone, 5, 5,
                new RuneFlameTriggerPart { Damage = 1, ConsumeOnTrigger = false });
            var stepper = CreateStepper(zone, 4, 5);

            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            var cell = zone.GetEntityCell(rune);
            Assert.IsNotNull(cell, "ConsumeOnTrigger=false must leave the rune in place.");
            Assert.AreEqual(5, cell.X);
            Assert.AreEqual(5, cell.Y);
        }

        // ====================================================================
        // Faction filter
        // ====================================================================

        [Test]
        public void Rune_SkipsSteppersOfSameFaction()
        {
            var zone = new Zone("TestZone");
            var rune = CreateRune(zone, 5, 5,
                new RuneFlameTriggerPart { Damage = 5, TriggerFaction = "Cultists" });
            var cultist = CreateStepper(zone, 4, 5, hp: 20, faction: "Cultists");

            MovementSystem.TryMove(cultist, zone, dx: 1, dy: 0);

            Assert.AreEqual(20, cultist.GetStatValue("Hitpoints"),
                "Rune must not trigger on actors whose faction matches TriggerFaction.");
            Assert.IsNotNull(zone.GetEntityCell(rune),
                "Rune should remain in the zone when the filter rejects the stepper.");
        }

        [Test]
        public void Rune_TriggersOnSteppersOfDifferentFaction()
        {
            var zone = new Zone("TestZone");
            var rune = CreateRune(zone, 5, 5,
                new RuneFlameTriggerPart { Damage = 5, TriggerFaction = "Cultists" });
            var hero = CreateStepper(zone, 4, 5, hp: 20, faction: "Heroes");

            MovementSystem.TryMove(hero, zone, dx: 1, dy: 0);

            Assert.AreEqual(15, hero.GetStatValue("Hitpoints"),
                "Rune must fire on actors whose faction differs from TriggerFaction.");
        }

        // ====================================================================
        // Variant coverage — Frost & Poison
        // ====================================================================

        [Test]
        public void RuneFrost_AppliesFrozenEffect_AndDamage()
        {
            var zone = new Zone("TestZone");
            CreateRune(zone, 5, 5, new RuneFrostTriggerPart { Damage = 4, Cold = 1.5f });
            var stepper = CreateStepper(zone, 4, 5, hp: 20);

            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            Assert.AreEqual(16, stepper.GetStatValue("Hitpoints"));
            var sep = stepper.GetPart<StatusEffectsPart>();
            Assert.IsNotNull(sep);
            Assert.IsTrue(sep.HasEffect<FrozenEffect>(),
                "Frost rune should apply FrozenEffect on trigger.");
        }

        [Test]
        public void RunePoison_AppliesPoisonedEffect_AndDamage()
        {
            var zone = new Zone("TestZone");
            CreateRune(zone, 5, 5,
                new RunePoisonTriggerPart { Damage = 2, PoisonDuration = 5, PoisonDice = "1d3" });
            var stepper = CreateStepper(zone, 4, 5, hp: 20);

            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            Assert.AreEqual(18, stepper.GetStatValue("Hitpoints"));
            var sep = stepper.GetPart<StatusEffectsPart>();
            Assert.IsNotNull(sep);
            Assert.IsTrue(sep.HasEffect<PoisonedEffect>(),
                "Poison rune should apply PoisonedEffect on trigger.");
        }

        // ====================================================================
        // CR-01 regression — mid-dispatch death / double-trigger
        // ====================================================================

        [Test]
        public void Movement_BreaksDispatchLoop_WhenMoverDiesMidSweep()
        {
            // M6 review CR-01: two co-located runes used to both fire on
            // a single step. If the first kills the stepper, the second
            // would call ApplyDamage on a dead entity which re-enters
            // HandleDeath (not idempotent). This test pins the fix —
            // FireCellEnteredEvents must break the dispatch loop when
            // the mover is removed from the zone mid-sweep.
            var zone = new Zone("TestZone");

            // Two flame runes at the same cell, 10 damage each, consumed on trigger.
            var runeA = CreateRune(zone, 5, 5, new RuneFlameTriggerPart { Damage = 10 });
            var runeB = CreateRune(zone, 5, 5, new RuneFlameTriggerPart { Damage = 10 });

            // Stepper has exactly 10 HP — first rune kills; second must NOT fire.
            var stepper = CreateStepper(zone, 4, 5, hp: 10);

            MovementSystem.TryMove(stepper, zone, dx: 1, dy: 0);

            // HP went to 0 from the first rune, NOT to -10 from a second
            // hit (stat floors at 0 via Stat.Min regardless, but the
            // critical signal is: only one "killed" message fired).
            int killedMessages = 0;
            foreach (var msg in MessageLog.GetMessages())
            {
                if (msg.Contains("is killed by")) killedMessages++;
            }
            Assert.AreEqual(1, killedMessages,
                "HandleDeath must fire exactly once — CR-01 would produce two \"is killed\" entries.");

            // Zone must only have consumed the rune that actually triggered.
            // The second rune should still be in the zone.
            int runesRemaining = 0;
            foreach (var e in zone.GetReadOnlyEntities())
            {
                if (e.HasTag("Rune") || e.BlueprintName == "TestRune") runesRemaining++;
            }
            // RuneA was consumed by its trigger; runeB's trigger never fired
            // (loop broke on mover death), so ConsumeOnTrigger never ran on it.
            Assert.AreEqual(1, runesRemaining,
                "Only one rune should have fired and consumed itself — the loop break must leave rune B untouched.");
        }

        // ====================================================================
        // P-01 regression — scratch-list reuse doesn't leak across calls
        // ====================================================================

        [Test]
        public void Movement_ScratchList_DoesNotLeakBetweenCalls()
        {
            // FireCellEnteredEvents now reuses a static List<Entity> to
            // avoid per-move allocation. If the list isn't cleared at the
            // start of each call, a second move would dispatch stale
            // EntityEnteredCell events to entities that no longer live at
            // the target cell.
            //
            // Setup: mover at (0,0). Cell (1,0) has THREE witnesses.
            // Cell (2,0) has ONE witness. Move mover into (1,0) then (2,0)
            // and verify each witness saw exactly the dispatches for its
            // own cell, nothing more.
            var zone = new Zone("TestZone");

            var w1 = new Entity { BlueprintName = "W1", ID = "w1" };
            w1.AddPart(new PhysicsPart { Solid = false });
            var r1 = new TestEnteredCellRecorder();
            w1.AddPart(r1);
            zone.AddEntity(w1, 1, 0);

            var w2 = new Entity { BlueprintName = "W2", ID = "w2" };
            w2.AddPart(new PhysicsPart { Solid = false });
            var r2 = new TestEnteredCellRecorder();
            w2.AddPart(r2);
            zone.AddEntity(w2, 1, 0);

            var w3 = new Entity { BlueprintName = "W3", ID = "w3" };
            w3.AddPart(new PhysicsPart { Solid = false });
            var r3 = new TestEnteredCellRecorder();
            w3.AddPart(r3);
            zone.AddEntity(w3, 1, 0);

            var lone = new Entity { BlueprintName = "Lone", ID = "lone" };
            lone.AddPart(new PhysicsPart { Solid = false });
            var rLone = new TestEnteredCellRecorder();
            lone.AddPart(rLone);
            zone.AddEntity(lone, 2, 0);

            var mover = CreateStepper(zone, 0, 0);

            MovementSystem.TryMove(mover, zone, dx: 1, dy: 0);
            Assert.AreEqual(1, r1.Count, "w1 should have received one dispatch.");
            Assert.AreEqual(1, r2.Count, "w2 should have received one dispatch.");
            Assert.AreEqual(1, r3.Count, "w3 should have received one dispatch.");
            Assert.AreEqual(0, rLone.Count, "lone is in (2,0), not reached yet.");

            MovementSystem.TryMove(mover, zone, dx: 1, dy: 0);
            Assert.AreEqual(1, r1.Count, "w1 must not receive a second dispatch — mover is now at (2,0). Scratch list leakage would make this fail.");
            Assert.AreEqual(1, r2.Count, "Same for w2.");
            Assert.AreEqual(1, r3.Count, "Same for w3.");
            Assert.AreEqual(1, rLone.Count, "lone receives exactly one dispatch on the second move.");
        }

        // ====================================================================
        // Test-only helper part
        // ====================================================================

        /// <summary>
        /// Records EntityEnteredCell dispatches for assertion. Not a
        /// production part — lives only in this test file to exercise the
        /// MovementSystem wiring independently of the TriggerOnStepPart
        /// subclasses.
        /// </summary>
        private class TestEnteredCellRecorder : Part
        {
            public int Count;
            public Entity LastActor;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "EntityEnteredCell")
                {
                    Count++;
                    LastActor = e.GetParameter<Entity>("Actor");
                }
                return true;
            }
        }
    }
}
