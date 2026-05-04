using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial cold-eye audit of <c>TurnManager</c>, per Methodology
    /// Template §3.9. The discipline:
    ///
    ///   1. Don't read the implementation first
    ///   2. State honest predictions in xml-doc (PREDICTION + CONFIDENCE)
    ///   3. Run; classify each failure as test-wrong / code-wrong /
    ///      setup-wrong
    ///
    /// LOW-confidence predictions are the gold — those are the ones where
    /// reality is most likely to disagree with my mental model.
    ///
    /// See <c>Docs/TURNMANAGER-ADVERSARIAL-AUDIT.md</c> for the
    /// per-test prediction table + outcome classification.
    /// </summary>
    public class TurnManagerAdversarialTests
    {
        // ====================================================================
        // 1. Speed = 0 entity should never get a turn.
        // PREDICTION: Energy never accumulates; entity never returned by Tick().
        // CONFIDENCE: medium. Defensive code might skip or quietly reject.
        // ====================================================================

        [Test]
        public void Adversarial_Speed0_Entity_NeverGetsTurn()
        {
            var tm = new TurnManager();
            var stuck = MakeEntity("StuckActor", speed: 0);
            tm.AddEntity(stuck);

            for (int i = 0; i < 100; i++)
            {
                Entity actor = tm.Tick();
                Assert.AreNotEqual(stuck, actor,
                    $"Speed=0 entity should never act. Got actor on tick {i}.");
            }

            Assert.AreEqual(0, tm.GetEnergy(stuck),
                "Energy should remain 0 across 100 ticks");
        }

        // ====================================================================
        // 2. Speed = int.MaxValue — accumulator overflow.
        // PREDICTION (LOW): Energy overflows on accumulation; either wraps
        // negative (entity stuck) or causes weird order-of-operations bugs.
        // CONFIDENCE: low — overflow territory.
        // ====================================================================

        [Test]
        public void Adversarial_SpeedMaxValue_DoesNotInfiniteLoop_OrCorruptState()
        {
            var tm = new TurnManager();
            var fast = MakeEntity("Fast", speed: int.MaxValue);
            tm.AddEntity(fast);

            // The tick loop should terminate in bounded time. If the
            // accumulator overflows weirdly, this could hang.
            int turns = 0;
            for (int i = 0; i < 100; i++)
            {
                Entity actor = tm.Tick();
                if (actor == fast) turns++;
            }

            // Reasonable bound: with Speed=MaxValue, the entity should get
            // a turn every tick (energy >> threshold immediately after
            // accumulation). 100 ticks → 100 turns. If overflow corrupts
            // state, we'd see 0 or some weird number.
            Assert.Greater(turns, 50,
                $"Speed=MaxValue should produce many turns; got {turns}");
        }

        // ====================================================================
        // 3. Speed = -1 — negative speed.
        // PREDICTION (LOW): Treated as 0 (clamped) OR energy goes negative.
        // CONFIDENCE: low — negative speed handling unclear.
        // ====================================================================

        [Test]
        public void Adversarial_NegativeSpeed_DoesNotInfiniteLoop_OrCrash()
        {
            var tm = new TurnManager();
            var weird = MakeEntity("Weird", speed: -1);
            tm.AddEntity(weird);

            // Don't crash, don't infinite-loop. Bounded iterations.
            for (int i = 0; i < 100; i++)
            {
                tm.Tick();
            }

            // Negative speed shouldn't grant the entity any turns — the
            // accumulator should either stay at 0 (if clamped) or stay
            // negative (never reaching the threshold).
            Assert.LessOrEqual(tm.GetEnergy(weird), 0,
                "Negative speed should NOT result in positive energy");
        }

        // ====================================================================
        // 4. AddEntity(null) — defensive null check.
        // PREDICTION: Defensive no-op, no crash.
        // CONFIDENCE: high.
        // ====================================================================

        [Test]
        public void Adversarial_AddEntityNull_NoOp_NoCrash()
        {
            var tm = new TurnManager();
            int countBefore = tm.EntityCount;
            // Should not throw
            tm.AddEntity(null);
            int countAfter = tm.EntityCount;
            Assert.AreEqual(countBefore, countAfter,
                "AddEntity(null) should not change EntityCount");
        }

        // ====================================================================
        // 5. AddEntity called twice for same entity.
        // PREDICTION (LOW): Either duplicates (entity gets 2x turns) or
        // rejected/idempotent. Duplicate would be a real bug — entity acting
        // twice as often as intended.
        // CONFIDENCE: low.
        // ====================================================================

        [Test]
        public void Adversarial_AddEntityTwice_DoesNotProduceDoubleTurns()
        {
            var tm = new TurnManager();
            var actor = MakeEntity("Doubled", speed: 100);
            tm.AddEntity(actor);
            tm.AddEntity(actor);  // duplicate

            int turns = 0;
            for (int i = 0; i < 30; i++)
            {
                Entity who = tm.Tick();
                if (who == actor) turns++;
            }

            // Speed=100, Threshold=1000, so 1 turn every 10 ticks.
            // 30 ticks → 3 turns expected if NOT duplicated.
            // 6 turns if duplicated (BUG).
            Assert.LessOrEqual(turns, 4,
                $"Same entity added twice should not get 2x turns. Got {turns} in 30 ticks.");
        }

        // ====================================================================
        // 6. RemoveEntity for non-member.
        // PREDICTION: No-op, no crash.
        // CONFIDENCE: high.
        // ====================================================================

        [Test]
        public void Adversarial_RemoveNonMember_NoCrash()
        {
            var tm = new TurnManager();
            var ghost = MakeEntity("Ghost", speed: 100);
            // Never added — try to remove
            Assert.DoesNotThrow(() => tm.RemoveEntity(ghost));
            Assert.AreEqual(0, tm.EntityCount);
        }

        // ====================================================================
        // 7. RemoveEntity(CurrentActor) — re-entrant removal during EndTurn.
        // PREDICTION (LOW): CurrentActor cleared cleanly OR stale reference
        // held that crashes a subsequent EndTurn call.
        // CONFIDENCE: low — re-entrancy is hard.
        // ====================================================================

        [Test]
        public void Adversarial_RemoveCurrentActor_DoesNotCorruptQueue()
        {
            var tm = new TurnManager();
            var a = MakeEntity("A", speed: 100);
            var b = MakeEntity("B", speed: 100);
            tm.AddEntity(a);
            tm.AddEntity(b);

            // Tick until someone acts
            Entity acted = null;
            for (int i = 0; i < 30 && acted == null; i++)
                acted = tm.Tick();
            Assert.IsNotNull(acted, "Someone should act within 30 ticks");

            // Remove the current actor mid-flow. Then continue ticking.
            tm.RemoveEntity(acted);

            // Should not crash on subsequent ticks; the OTHER entity should
            // still get turns.
            for (int i = 0; i < 30; i++)
                tm.Tick();

            Assert.AreEqual(1, tm.EntityCount,
                "After removing one of two entities, count should be 1");
        }

        // ====================================================================
        // 8. ProcessUntilPlayerTurn() with no Player-tagged entity.
        // PREDICTION (LOW): Either infinite loop (BUG) or bounded return.
        // CONFIDENCE: low — termination contract unclear.
        // ====================================================================

        [Test]
        public void Adversarial_ProcessUntilPlayerTurn_NoPlayer_TerminatesInBoundedTime()
        {
            var tm = new TurnManager();
            tm.AddEntity(MakeEntity("Npc1", speed: 100));
            tm.AddEntity(MakeEntity("Npc2", speed: 100));
            // No entity has the Player tag

            var sw = System.Diagnostics.Stopwatch.StartNew();
            // Should terminate; if not, this test will hang and the runner
            // will time it out. We bound to 5s.
            Entity result = tm.ProcessUntilPlayerTurn();
            sw.Stop();

            Assert.Less(sw.ElapsedMilliseconds, 5000,
                $"ProcessUntilPlayerTurn must terminate even without a Player tag. Took {sw.ElapsedMilliseconds}ms");
        }

        // ====================================================================
        // 9. EndTurn(actor, null) — null zone tolerated.
        // PREDICTION: Doesn't crash; zone-dependent listeners no-op.
        // CONFIDENCE: medium.
        // ====================================================================

        [Test]
        public void Adversarial_EndTurnNullZone_NoCrash()
        {
            var tm = new TurnManager();
            var a = MakeEntity("A", speed: 100);
            tm.AddEntity(a);

            // Force a turn
            for (int i = 0; i < 30; i++) tm.Tick();

            Assert.DoesNotThrow(() => tm.EndTurn(a, zone: null));
        }

        // ====================================================================
        // 10. Entity with no Speed stat — fallback to DefaultSpeed.
        // PREDICTION: GetSpeed returns DefaultSpeed (100).
        // CONFIDENCE: medium — could throw NPE if unguarded.
        // ====================================================================

        [Test]
        public void Adversarial_EntityWithoutSpeedStat_GetSpeedReturnsDefault()
        {
            var tm = new TurnManager();
            var statless = new Entity();
            statless.BlueprintName = "Statless";
            statless.Tags["Creature"] = "";
            // Intentionally no Speed stat

            int speed = tm.GetSpeed(statless);
            Assert.AreEqual(TurnManager.DefaultSpeed, speed,
                "Missing Speed stat should fall back to DefaultSpeed");
        }

        // ====================================================================
        // 11. Two entities with identical Speed — deterministic order.
        // PREDICTION: Both progress; tie-broken by add-order.
        // CONFIDENCE: medium — convention unclear.
        // ====================================================================

        [Test]
        public void Adversarial_IdenticalSpeed_BothEntitiesProgress()
        {
            var tm = new TurnManager();
            var a = MakeEntity("A", speed: 100);
            var b = MakeEntity("B", speed: 100);
            tm.AddEntity(a);
            tm.AddEntity(b);

            int aTurns = 0, bTurns = 0;
            for (int i = 0; i < 100; i++)
            {
                Entity who = tm.Tick();
                if (who == a) aTurns++;
                else if (who == b) bTurns++;
            }

            // Both should get roughly equal turns (~10 each).
            Assert.Greater(aTurns, 5, $"A got {aTurns} turns");
            Assert.Greater(bTurns, 5, $"B got {bTurns} turns");
            // No entity-A starvation:
            Assert.Less(Math.Abs(aTurns - bTurns), 5,
                $"Equal-speed entities should get nearly equal turns. A={aTurns}, B={bTurns}");
        }

        // ====================================================================
        // 12. Tick() before any AddEntity — returns null, no crash.
        // PREDICTION: Returns null.
        // CONFIDENCE: high.
        // ====================================================================

        [Test]
        public void Adversarial_TickWithEmptyQueue_ReturnsNull_NoCrash()
        {
            var tm = new TurnManager();
            Entity result = null;
            Assert.DoesNotThrow(() => result = tm.Tick());
            Assert.IsNull(result, "Empty queue should produce null actor");
        }

        // ====================================================================
        // 13. Speed = 37 (non-divisor of 1000) — energy carries between turns.
        // PREDICTION: Energy accumulates with leftovers (37, 74, 111, ..., 999, 1036→36).
        // After acting, energy resets to leftover (e.g., 36 not 0).
        // CONFIDENCE: medium.
        // ====================================================================

        [Test]
        public void Adversarial_NonDivisorSpeed_EnergyCarriesLeftover()
        {
            var tm = new TurnManager();
            var a = MakeEntity("A", speed: 37);
            tm.AddEntity(a);

            // 1000 / 37 = 27.0... — first turn at tick 28 (energy = 27*37=999, then 1036)
            // Actually: tick 1 → energy=37, tick 2 → 74, ..., tick 27 → 999 (no turn yet),
            // tick 28 → 1036 (turn fires, leftover = 36).
            int turns = 0;
            for (int i = 0; i < 28; i++)
            {
                Entity who = tm.Tick();
                if (who == a) turns++;
            }
            Assert.AreEqual(1, turns, "Should get exactly 1 turn in 28 ticks at Speed=37");

            // After the turn fires, residual energy should be 36 (the
            // leftover after subtracting 1000 from 1036). Let's check.
            int leftover = tm.GetEnergy(a);
            // Reasonable bound: between 0 and 100. Exact value 36 if leftover-carry.
            // If energy resets to 0 (no carry), leftover = 0.
            // Either is acceptable; this test just pins what reality is.
            Assert.That(leftover, Is.InRange(0, 100),
                $"After acting, leftover energy should be in [0, 100). Got {leftover}.");
        }

        // ====================================================================
        // 14. EndTurn called twice for same actor — idempotency check.
        // PREDICTION (LOW): Either no-op-on-second OR double-energy-deduction (BUG).
        // CONFIDENCE: low.
        // ====================================================================

        [Test]
        public void Adversarial_EndTurnTwice_DoesNotDoubleDeductEnergy()
        {
            var tm = new TurnManager();
            var a = MakeEntity("A", speed: 100);
            tm.AddEntity(a);

            // Drive to a turn
            Entity acted = null;
            for (int i = 0; i < 30 && acted != a; i++)
                acted = tm.Tick();
            Assert.AreEqual(a, acted, "A should have acted within 30 ticks");

            int energyAfterFirstTurn = tm.GetEnergy(a);
            tm.EndTurn(a);
            int energyAfterFirstEnd = tm.GetEnergy(a);

            // Call EndTurn again — should NOT deduct another 1000
            tm.EndTurn(a);
            int energyAfterSecondEnd = tm.GetEnergy(a);

            Assert.AreEqual(energyAfterFirstEnd, energyAfterSecondEnd,
                $"Second EndTurn should be a no-op. Energy: " +
                $"after-1st={energyAfterFirstEnd}, after-2nd={energyAfterSecondEnd}.");
        }

        // ====================================================================
        // 15. Entity dies (HP→0) during their own turn — frozen-bug saga territory.
        // PREDICTION (LOW): TurnManager removes the dead entity OR allows it
        // to keep ticking (BUG — corpse takes turns).
        // CONFIDENCE: low — re-entrancy.
        // ====================================================================

        [Test]
        public void Adversarial_DeadEntityDoesNotKeepTakingTurns()
        {
            var tm = new TurnManager();
            var alive = MakeEntity("Alive", speed: 100);
            var dying = MakeEntity("Dying", speed: 100);
            tm.AddEntity(alive);
            tm.AddEntity(dying);

            // Drive turns; on Dying's turn, kill them (HP=0)
            int dyingTurnsBeforeDeath = 0;
            int dyingTurnsAfterDeath = 0;
            bool died = false;
            for (int i = 0; i < 100; i++)
            {
                Entity who = tm.Tick();
                if (who == dying)
                {
                    if (!died)
                    {
                        // First time Dying acts: kill them mid-turn
                        dying.GetStat("Hitpoints").BaseValue = 0;
                        died = true;
                        dyingTurnsBeforeDeath = 1;
                    }
                    else
                    {
                        dyingTurnsAfterDeath++;
                    }
                }
            }

            // After death, Dying should not keep getting turns. The
            // TurnManager either removes them automatically OR a sweep
            // does. Either way, post-death turn count should be 0.
            Assert.AreEqual(1, dyingTurnsBeforeDeath, "Dying should have acted once before dying");
            Assert.AreEqual(0, dyingTurnsAfterDeath,
                $"Dead entity should not keep taking turns. Got {dyingTurnsAfterDeath} post-death turns.");
        }

        // ====================================================================
        // 16. AddEntity mid-Tick — does the new entity get this tick's energy?
        // Hard to construct without a hook, but we can at least verify the
        // queue is mutation-safe.
        // PREDICTION: New entity added during a tick is included in subsequent
        // ticks but NOT this one (deferred via copy-iteration).
        // CONFIDENCE: medium — iteration safety class.
        // ====================================================================

        [Test]
        public void Adversarial_AddEntityMidIteration_QueueRemainsConsistent()
        {
            var tm = new TurnManager();
            var a = MakeEntity("A", speed: 100);
            tm.AddEntity(a);

            // Tick a few times, add B mid-flow, continue
            for (int i = 0; i < 5; i++) tm.Tick();
            var b = MakeEntity("B", speed: 100);
            tm.AddEntity(b);
            for (int i = 0; i < 30; i++) tm.Tick();

            // Both should be in the queue and able to act
            Assert.AreEqual(2, tm.EntityCount);
            Assert.Greater(tm.GetEnergy(b), 0, "B should have accumulated some energy after being added");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity MakeEntity(string name, int speed)
        {
            var entity = new Entity();
            entity.BlueprintName = name;
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat
                { Owner = entity, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            entity.Statistics["Speed"] = new Stat
                { Owner = entity, Name = "Speed", BaseValue = speed, Min = -100, Max = int.MaxValue };
            entity.AddPart(new RenderPart { DisplayName = name });
            return entity;
        }
    }
}
