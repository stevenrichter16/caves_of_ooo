using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using NUnit.Framework;

namespace CavesOfOoo.Tests.TestSupport
{
    /// <summary>
    /// Tests for Phase 3b's <see cref="ScenarioContextExtensions.AdvanceTurns"/>.
    /// Verifies the test-intent naming wraps the manual TakeTurn loop correctly.
    /// </summary>
    [TestFixture]
    public class ScenarioContextExtensionsTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        /// <summary>
        /// A minimal counter part — bumps on every TakeTurn. Lets us verify
        /// exactly how many TakeTurn events an entity received without relying
        /// on downstream AI behavior.
        /// </summary>
        private class TakeTurnCounterPart : Part
        {
            public int Count;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "TakeTurn") Count++;
                return base.HandleEvent(e);
            }
        }

        private static (Entity e, TakeTurnCounterPart counter) SpawnCountingEntity(ScenarioContext ctx, int x, int y)
        {
            var e = new Entity { BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            var counter = new TakeTurnCounterPart();
            e.AddPart(counter);
            ctx.Zone.AddEntity(e, x, y);
            ctx.Turns.AddEntity(e);
            return (e, counter);
        }

        [Test]
        public void AdvanceTurns_DefaultCount1_FiresTakeTurnOnceOnEachEntity()
        {
            var ctx = _harness.CreateContext();
            var (_, counter1) = SpawnCountingEntity(ctx, 10, 10);
            var (_, counter2) = SpawnCountingEntity(ctx, 20, 10);

            ctx.AdvanceTurns();

            Assert.AreEqual(1, counter1.Count);
            Assert.AreEqual(1, counter2.Count);
        }

        [Test]
        public void AdvanceTurns_Count5_FiresFiveTimesPerEntity()
        {
            var ctx = _harness.CreateContext();
            var (_, counter) = SpawnCountingEntity(ctx, 10, 10);

            ctx.AdvanceTurns(5);

            Assert.AreEqual(5, counter.Count);
        }

        [Test]
        public void AdvanceTurns_CountZero_IsNoOp()
        {
            var ctx = _harness.CreateContext();
            var (_, counter) = SpawnCountingEntity(ctx, 10, 10);

            ctx.AdvanceTurns(0);
            ctx.AdvanceTurns(-5); // negative also no-op

            Assert.AreEqual(0, counter.Count);
        }

        [Test]
        public void AdvanceTurns_ReturnsContextForChaining()
        {
            var ctx = _harness.CreateContext();
            var result = ctx.AdvanceTurns(3);
            Assert.AreSame(ctx, result, "AdvanceTurns should return the same context for chaining.");
        }

        [Test]
        public void AdvanceTurns_SkipsEntitiesNotRegisteredWithTurnManager()
        {
            var ctx = _harness.CreateContext();

            // Registered — should tick.
            var (_, registered) = SpawnCountingEntity(ctx, 10, 10);

            // Unregistered — in zone but not in TurnManager. Should NOT tick.
            var unregistered = new Entity { BlueprintName = "Unregistered" };
            unregistered.Tags["Creature"] = "";
            var unregCounter = new TakeTurnCounterPart();
            unregistered.AddPart(unregCounter);
            ctx.Zone.AddEntity(unregistered, 15, 10);
            // Deliberately do NOT call ctx.Turns.AddEntity.

            ctx.AdvanceTurns(3);

            Assert.AreEqual(3, registered.Count, "Registered entity should tick 3 times.");
            Assert.AreEqual(0, unregCounter.Count, "Unregistered entity should not receive TakeTurn.");
        }

        [Test]
        public void AdvanceTurns_SnapshotIteration_NewEntitiesStartTickingNextStep()
        {
            // Verifies the per-step snapshot: if an entity is added mid-step,
            // it doesn't receive a TakeTurn until the NEXT step.
            var ctx = _harness.CreateContext();
            var (e1, c1) = SpawnCountingEntity(ctx, 10, 10);

            // A "late arrival" part that, on its first TakeTurn, spawns another
            // entity into the turn manager mid-step.
            Entity lateArrival = null;
            TakeTurnCounterPart lateCounter = null;
            var spawner = new LateArrivalSpawner(ctx, onFirstTick: () =>
            {
                lateArrival = new Entity { BlueprintName = "Late" };
                lateArrival.Tags["Creature"] = "";
                lateCounter = new TakeTurnCounterPart();
                lateArrival.AddPart(lateCounter);
                ctx.Zone.AddEntity(lateArrival, 12, 10);
                ctx.Turns.AddEntity(lateArrival);
            });
            e1.AddPart(spawner);

            // Step 1: e1 ticks once and spawns lateArrival. lateArrival should
            // NOT tick this step (snapshot was taken before it was added).
            ctx.AdvanceTurns(1);
            Assert.AreEqual(1, c1.Count, "e1 ticked step 1.");
            Assert.IsNotNull(lateArrival, "Spawner fired on e1's first tick.");
            Assert.AreEqual(0, lateCounter.Count, "Late arrival didn't tick on the same step it was added.");

            // Step 2: both tick.
            ctx.AdvanceTurns(1);
            Assert.AreEqual(2, c1.Count);
            Assert.AreEqual(1, lateCounter.Count, "Late arrival ticks on next step.");
        }

        [Test]
        public void AdvanceTurns_WithEmptyTurnManager_NoOpDoesNotThrow()
        {
            var ctx = _harness.CreateContext();
            Assert.DoesNotThrow(() => ctx.AdvanceTurns(5),
                "Advancing turns with no registered entities should be a silent no-op.");
        }

        [Test]
        public void TurnManager_Entities_YieldsInInsertionOrder()
        {
            // Direct test for TurnManager.Entities (the only runtime API added
            // in Phase 3). If someone refactors Entities to e.g. sort or
            // deduplicate, this pins the current contract.
            var ctx = _harness.CreateContext();
            var a = new Entity { BlueprintName = "A" };
            var b = new Entity { BlueprintName = "B" };
            var c = new Entity { BlueprintName = "C" };
            ctx.Turns.AddEntity(a);
            ctx.Turns.AddEntity(b);
            ctx.Turns.AddEntity(c);

            var yielded = new System.Collections.Generic.List<Entity>();
            foreach (var e in ctx.Turns.Entities) yielded.Add(e);

            CollectionAssert.AreEqual(new[] { a, b, c }, yielded);
        }

        [Test]
        public void AdvanceTurns_NullContext_ThrowsArgumentNullException()
        {
            // Standard extension-method contract: null receiver throws
            // ArgumentNullException rather than silently no-op'ing.
            ScenarioContext nullCtx = null;
            Assert.Throws<System.ArgumentNullException>(
                () => nullCtx.AdvanceTurns(1));
        }

        [Test]
        public void AdvanceTurns_TickOrder_IsInsertionOrder()
        {
            // Pins the current behavior: entities tick in the order they were
            // added to the TurnManager. Documents the contract — any future
            // change (e.g. speed-sorted ticking) should come with a deliberate
            // update to this test.
            var ctx = _harness.CreateContext();

            var order = new System.Collections.Generic.List<string>();
            var a = BuildOrderRecorder("A", order, ctx);
            var b = BuildOrderRecorder("B", order, ctx);
            var c = BuildOrderRecorder("C", order, ctx);

            ctx.AdvanceTurns(1);

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, order,
                "Tick order should match insertion order.");
        }

        [Test]
        public void AdvanceTurns_EntityThatDiedMidStep_StillReceivesTakeTurn()
        {
            // DOCUMENTATION TEST — not a contract we want, but a contract the
            // underlying engine has today.
            //
            // CombatSystem.HandleDeath removes the dead entity from the Zone
            // but NOT from the TurnManager. The only code path in the entire
            // codebase that calls TurnManager.RemoveEntity is ZoneBuilder.
            // So a Snapjaw killed on turn N stays in the TurnManager and
            // continues receiving TakeTurn events on turn N+1, N+2, ...
            //
            // AdvanceTurns inherits this behavior (by design — it matches how
            // AI tests have always manually fired TakeTurn). This test pins
            // the current behavior; if CombatSystem is ever fixed to also
            // de-register from TurnManager, this test will start failing and
            // should be updated deliberately.
            var ctx = _harness.CreateContext();
            var (_, counter) = SpawnCountingEntity(ctx, 10, 10);

            // Simulate mid-combat Zone removal (what HandleDeath does).
            ctx.Zone.RemoveEntity(counter.ParentEntity);
            // Deliberately do NOT remove from TurnManager — this mirrors the
            // production HandleDeath path.

            ctx.AdvanceTurns(3);

            Assert.AreEqual(3, counter.Count,
                "Dead-in-zone but still-in-TurnManager entity continues ticking. " +
                "This documents the current engine behavior — CombatSystem does not " +
                "de-register from TurnManager on death.");
        }

        /// <summary>
        /// Constructs an entity with a part that appends its label to a shared
        /// list on every TakeTurn. Used to verify tick ordering.
        /// </summary>
        private static Entity BuildOrderRecorder(string label, System.Collections.Generic.List<string> order, ScenarioContext ctx)
        {
            var e = new Entity { BlueprintName = "Order_" + label };
            e.Tags["Creature"] = "";
            e.AddPart(new OrderRecorderPart { Label = label, SharedOrder = order });
            ctx.Zone.AddEntity(e, 10 + label[0] - 'A', 10);
            ctx.Turns.AddEntity(e);
            return e;
        }

        private class OrderRecorderPart : Part
        {
            public string Label;
            public System.Collections.Generic.List<string> SharedOrder;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "TakeTurn") SharedOrder.Add(Label);
                return base.HandleEvent(e);
            }
        }

        /// <summary>
        /// Test-only part: invokes a callback the first time TakeTurn fires on its
        /// parent. Used to test mid-step entity additions.
        /// </summary>
        private class LateArrivalSpawner : Part
        {
            private readonly ScenarioContext _ctx;
            private readonly System.Action _onFirstTick;
            private bool _fired;

            public LateArrivalSpawner(ScenarioContext ctx, System.Action onFirstTick)
            {
                _ctx = ctx;
                _onFirstTick = onFirstTick;
            }

            public override bool HandleEvent(GameEvent e)
            {
                if (!_fired && e.ID == "TakeTurn")
                {
                    _fired = true;
                    _onFirstTick?.Invoke();
                }
                return base.HandleEvent(e);
            }
        }
    }
}
