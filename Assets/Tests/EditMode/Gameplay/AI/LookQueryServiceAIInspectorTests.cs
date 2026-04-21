using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 10 Commit 4 — verifies <see cref="LookQueryService"/> populates
    /// <see cref="LookSnapshot.GoalStackLines"/> and
    /// <see cref="LookSnapshot.LastThought"/> ONLY when all three gates pass:
    /// <see cref="AIDebug.AIInspectorEnabled"/> is true, the primary entity
    /// is Creature-tagged, and it has a <see cref="BrainPart"/>.
    ///
    /// Each test resets <c>AIDebug.AIInspectorEnabled</c> in [TearDown] so
    /// cross-fixture state bleed can't make a later test pass spuriously.
    /// </summary>
    [TestFixture]
    public class LookQueryServiceAIInspectorTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [TearDown]
        public void TearDown()
        {
            // Defensive: ensure no test leaves the static toggle on for the
            // next fixture. A leaked `true` could make an unrelated test pass
            // by accident (or fail, if it asserts `null`).
            AIDebug.AIInspectorEnabled = false;
        }

        // ========================
        // Gate combinations: (inspector on/off) × (target kind)
        // ========================

        [Test]
        public void Inspector_Off_CreatureTarget_GoalStackLinesAndThoughtAreNull()
        {
            // Baseline: with toggle OFF, the inspector contributes nothing
            // even when the hover target is a perfectly valid Creature with
            // a Brain. Proves the zero-cost default path.
            AIDebug.AIInspectorEnabled = false;
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").AtPlayerOffset(2, 0);
            var pos = ctx.Zone.GetEntityPosition(snapjaw);

            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);

            Assert.AreSame(snapjaw, snap.PrimaryEntity,
                "Sanity: snapshot's primary should be the spawned Snapjaw.");
            Assert.IsNull(snap.GoalStackLines,
                "Inspector OFF → GoalStackLines must stay null.");
            Assert.IsNull(snap.LastThought,
                "Inspector OFF → LastThought must stay null.");
        }

        [Test]
        public void Inspector_On_CreatureTargetWithBrain_GoalStackLinesPopulated()
        {
            // Positive path: toggle on, hover a Creature-with-Brain, expect
            // GoalStackLines non-null and containing at least one rendered goal.
            // The snapjaw just spawned — its BoredGoal hasn't been pushed yet
            // (that happens on first TakeTurn), so the stack may be empty OR
            // contain BoredGoal after a single tick. Fire one TakeTurn to
            // stabilize, then assert.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var snapjaw = ctx.Spawn("Snapjaw").AtPlayerOffset(4, 0);
            snapjaw.FireEvent(GameEvent.New("TakeTurn"));

            var pos = ctx.Zone.GetEntityPosition(snapjaw);
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);

            Assert.IsNotNull(snap.GoalStackLines,
                "Inspector ON + Creature + BrainPart → GoalStackLines must populate.");
            Assert.GreaterOrEqual(snap.GoalStackLines.Count, 1,
                "After one TakeTurn the Snapjaw should have at least one goal on the stack (BoredGoal or KillGoal).");
            Assert.IsNotNull(snap.LastThought,
                "LastThought is populated with 'none' when the creature hasn't thought yet — never raw-null when other inspector fields populate.");
        }

        [Test]
        public void Inspector_On_NonCreatureTarget_GoalStackLinesStaysNull()
        {
            // Counter-check: the inspector must NOT render for a non-Creature
            // cell (e.g. a chest). If this ever fires for items, hovering a
            // chest would render an empty "Goals:" section with stale "none"
            // thought — ugly and wrong.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var chest = ctx.World.PlaceObject("Chest").AtPlayerOffset(2, 0);
            var cell = ctx.Zone.GetEntityCell(chest);

            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, cell.X, cell.Y);

            Assert.IsFalse(snap.PrimaryEntity != null && snap.PrimaryEntity.HasTag("Creature"),
                "Sanity: chest should not be Creature-tagged.");
            Assert.IsNull(snap.GoalStackLines,
                "Non-Creature target → GoalStackLines stays null even with inspector on.");
            Assert.IsNull(snap.LastThought,
                "Non-Creature target → LastThought stays null even with inspector on.");
        }

        [Test]
        public void Inspector_On_EmptyCell_GoalStackLinesStaysNull()
        {
            // Counter-check: empty floor cell has no primary entity.
            // LookSnapshot.PrimaryEntity is null; the inspector must bail
            // cleanly without a NullReferenceException.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Look at a cell far from any spawn — should be empty ground
            // (or at worst, just a Floor tile with no Creature tag).
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, p.x + 15, p.y + 5);

            Assert.IsNull(snap.GoalStackLines,
                "Empty cell → GoalStackLines stays null.");
            Assert.IsNull(snap.LastThought,
                "Empty cell → LastThought stays null.");
        }

        // ========================
        // Run-length collapsing (Qud's xN convention)
        // ========================

        [Test]
        public void Inspector_RunLengthCollapsing_CollapsesConsecutiveDuplicateDescriptions()
        {
            // Pins Qud's xN format: consecutive goals with identical
            // GetDescription() collapse into one line "<desc> xN". A future
            // refactor that changes the separator (" x2" → "*2") or drops
            // collapsing entirely would break this test.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var creature = ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);

            // Replace the stack with three identical synthetic goals so we
            // can assert the rendered form deterministically. (Using real
            // goals with identical descriptions is brittle — they'd diverge
            // as state changes.)
            var brain = creature.GetPart<BrainPart>();
            brain.ClearGoals();
            brain.PushGoal(new SyntheticDescGoal("Stub"));
            brain.PushGoal(new SyntheticDescGoal("Stub"));
            brain.PushGoal(new SyntheticDescGoal("Stub"));

            var pos = ctx.Zone.GetEntityPosition(creature);
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);

            Assert.IsNotNull(snap.GoalStackLines);
            Assert.AreEqual(1, snap.GoalStackLines.Count,
                "Three identical-description goals should collapse into a single rendered line.");
            Assert.AreEqual("Stub x3", snap.GoalStackLines[0]);
        }

        [Test]
        public void Inspector_RunLengthCollapsing_DistinctDescriptionsStayOnSeparateLines()
        {
            // Counter-check: non-duplicate descriptions must render on
            // separate lines — the "x2" collapse must not over-merge.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var creature = ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);

            var brain = creature.GetPart<BrainPart>();
            brain.ClearGoals();
            brain.PushGoal(new SyntheticDescGoal("A"));
            brain.PushGoal(new SyntheticDescGoal("B"));
            brain.PushGoal(new SyntheticDescGoal("A")); // not consecutive — must NOT collapse with the bottom "A"

            var pos = ctx.Zone.GetEntityPosition(creature);
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);

            Assert.AreEqual(3, snap.GoalStackLines.Count,
                "Non-consecutive identical descriptions must not be merged.");
            // Top-down order: index 0 = innermost / most-recent pushed = "A"
            Assert.AreEqual("A", snap.GoalStackLines[0]);
            Assert.AreEqual("B", snap.GoalStackLines[1]);
            Assert.AreEqual("A", snap.GoalStackLines[2]);
        }

        [Test]
        public void Inspector_LastThought_ReflectsBrainLastThoughtField()
        {
            // The Think-text surfaced on the snapshot must be whatever the
            // creature last set via Brain.Think. Regression guard for the
            // "none" sentinel — it's used ONLY when LastThought is null/empty.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var creature = ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);
            var brain = creature.GetPart<BrainPart>();
            brain.Think("I see you, player");

            var pos = ctx.Zone.GetEntityPosition(creature);
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);

            Assert.AreEqual("I see you, player", snap.LastThought);
        }

        [Test]
        public void Inspector_LastThought_NoneSentinel_WhenBrainHasNoThought()
        {
            // Counter-check for the "none" sentinel: a brand-new creature
            // that has never Thought must render "none" rather than null,
            // so the sidebar renderer can unconditionally draw the
            // "Thought: <x>" line without a null check.
            AIDebug.AIInspectorEnabled = true;
            var ctx = _harness.CreateContext();
            var creature = ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);

            var pos = ctx.Zone.GetEntityPosition(creature);
            var snap = LookQueryService.BuildSnapshot(
                ctx.PlayerEntity, ctx.Zone, pos.x, pos.y);

            Assert.AreEqual("none", snap.LastThought);
        }

        /// <summary>
        /// Test-only goal that returns a caller-supplied GetDescription()
        /// string. Lets run-length tests assert the collapse format without
        /// depending on any shipped goal's actual description text.
        /// </summary>
        private sealed class SyntheticDescGoal : GoalHandler
        {
            private readonly string _desc;
            public SyntheticDescGoal(string desc) { _desc = desc; }
            public override void TakeAction() { /* no-op */ }
            public override string GetDescription() => _desc;
        }
    }
}
