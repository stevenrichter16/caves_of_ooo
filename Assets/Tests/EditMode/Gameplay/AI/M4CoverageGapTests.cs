using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M4 (Cell.IsInterior + MoveToInteriorGoal + MoveToExteriorGoal +
    /// AIHelpers.FindNearestCellWhere + VillageBuilder interior tagging)
    /// coverage-gap tests.
    ///
    /// Existing M4 coverage in MoveToInteriorExteriorGoalTests.cs +
    /// VillageBuilderInteriorTests.cs is broad — happy-path BFS, MaxTurns,
    /// thought-write timing, blueprint tagging. Remaining gaps cluster in:
    ///
    ///   - Tuning-constant anchors (MaxSearchRadius=40, MaxTurns=50).
    ///   - Constructor field-wiring sanity.
    ///   - Failed(child) → FailToParent propagation (the override is
    ///     present in production but not exercised by any test).
    ///   - The `Math.Max(1, MaxTurns - Age)` clamp on the child
    ///     MoveToGoal's MaxTurns budget — boundary-prone math.
    ///   - FindNearestCellWhere defensive paths (null zone, null predicate,
    ///     non-passable start cell).
    ///   - Boundary cases on MaxRadius=0 (only the start cell is checked).
    ///   - Cell.IsInterior default value.
    ///
    /// Per Docs/QUD-PARITY.md §2.1 (gap-fill style — read production first,
    /// pin observed contract). Distinct from the §3.9 adversarial style
    /// applied to M1/M2 (no-read first). Failures here are most likely
    /// test-misconfig; bug-find rate expected near zero.
    /// </summary>
    [TestFixture]
    public class M4CoverageGapTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ============================================================
        // Helpers
        // ============================================================

        /// <summary>Mirrors the simple-creature pattern used in
        /// MoveToInteriorExteriorGoalTests.</summary>
        private static Entity CreateNpc(Zone zone, int x, int y)
        {
            var entity = new Entity { BlueprintName = "TestNpc" };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "npc" });
            entity.AddPart(new PhysicsPart { Solid = false });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        /// <summary>Open zone where every cell is passable.</summary>
        private static Zone CreateOpenZone()
        {
            return new Zone("OpenZone");
        }

        // ============================================================
        // MoveToInteriorGoal — defaults, ctor, Failed propagation,
        // remaining-turns clamp
        // ============================================================

        [Test]
        public void MoveToInteriorGoal_DefaultMaxSearchRadius_Is40()
        {
            var goal = new MoveToInteriorGoal();
            Assert.AreEqual(40, goal.MaxSearchRadius,
                "Default MaxSearchRadius=40 covers any 80×25 zone. Tuning " +
                "anchor — a tighter default would silently make some shrines " +
                "unreachable from the zone's far corners.");
        }

        [Test]
        public void MoveToInteriorGoal_DefaultMaxTurns_Is50()
        {
            var goal = new MoveToInteriorGoal();
            Assert.AreEqual(50, goal.MaxTurns,
                "Default MaxTurns=50 — the M4.2 spec safety net. Without an " +
                "anchor here, future tuning could shorten this and surprise " +
                "scenarios that take more than the new value to walk.");
        }

        [Test]
        public void MoveToInteriorGoal_Constructor_StoresBothFields()
        {
            // Argument-order regression pin.
            var goal = new MoveToInteriorGoal(maxSearchRadius: 7, maxTurns: 13);
            Assert.AreEqual(7, goal.MaxSearchRadius, "MaxSearchRadius");
            Assert.AreEqual(13, goal.MaxTurns, "MaxTurns");
        }

        [Test]
        public void MoveToInteriorGoal_Failed_PropagatesViaFailToParent()
        {
            // The Failed override pops the goal and notifies the parent.
            // The override is in production line 74-78 but not previously
            // exercised. Pin it via a SentinelParentGoal that records
            // whether its Failed handler fired.
            var zone = CreateOpenZone();
            var npc = CreateNpc(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();

            var sentinel = new SentinelParentGoal();
            brain.PushGoal(sentinel);

            var interiorGoal = new MoveToInteriorGoal();
            sentinel.PushChildGoal(interiorGoal);

            // Simulate a child MoveToGoal failure.
            interiorGoal.Failed(new MoveToGoal(0, 0));

            Assert.IsTrue(sentinel.ChildFailed,
                "MoveToInteriorGoal.Failed must propagate up via FailToParent.");
            Assert.IsFalse(brain.HasGoal<MoveToInteriorGoal>(),
                "MoveToInteriorGoal should have popped during FailToParent.");
        }

        [Test]
        public void MoveToInteriorGoal_RemainingTurnsClamp_AtAgeEqualMaxTurns_StillOne()
        {
            // Production line 70 clamps remainingTurns to >= 1 even when
            // Age == MaxTurns. Without this, MoveToGoal would be pushed
            // with MaxTurns=0, immediately satisfying its `Age > MaxTurns`
            // exit and popping with no progress. Pin the clamp.
            var zone = CreateOpenZone();
            zone.GetCell(10, 5).IsInterior = true;
            var npc = CreateNpc(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();

            var goal = new MoveToInteriorGoal(maxSearchRadius: 40, maxTurns: 5);
            brain.PushGoal(goal);

            // Manually force Age past MaxTurns.
            goal.Age = 10;

            goal.TakeAction();

            // Expect a MoveToGoal child with MaxTurns clamped to 1.
            var child = brain.FindGoal<MoveToGoal>();
            Assert.IsNotNull(child, "Child MoveToGoal should be pushed.");
            Assert.GreaterOrEqual(child.MaxTurns, 1,
                "Remaining-turns clamp must keep the child's MaxTurns ≥ 1 " +
                "even when Age > MaxTurns. Without the clamp, the child " +
                "would push with MaxTurns=0 (immediate-pop) or negative.");
        }

        [Test]
        public void MoveToInteriorGoal_TakeAction_OnInteriorCell_DoesNotPushChild()
        {
            // Production line 57: early-return when already on interior.
            // No MoveToGoal pushed (the parent will pop on next tick via
            // Finished()).
            var zone = CreateOpenZone();
            zone.GetCell(5, 5).IsInterior = true;
            var npc = CreateNpc(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();

            var goal = new MoveToInteriorGoal();
            brain.PushGoal(goal);

            int countBefore = brain.GoalCount;
            goal.TakeAction();
            int countAfter = brain.GoalCount;

            Assert.AreEqual(countBefore, countAfter,
                "TakeAction on an interior cell must not push a child MoveToGoal — " +
                "the parent is already satisfied and Finished() will pop it.");
        }

        // ============================================================
        // MoveToExteriorGoal — symmetric defaults, ctor, Failed,
        // remaining-turns clamp
        // ============================================================

        [Test]
        public void MoveToExteriorGoal_DefaultMaxSearchRadius_Is40()
        {
            var goal = new MoveToExteriorGoal();
            Assert.AreEqual(40, goal.MaxSearchRadius);
        }

        [Test]
        public void MoveToExteriorGoal_DefaultMaxTurns_Is50()
        {
            var goal = new MoveToExteriorGoal();
            Assert.AreEqual(50, goal.MaxTurns);
        }

        [Test]
        public void MoveToExteriorGoal_Constructor_StoresBothFields()
        {
            var goal = new MoveToExteriorGoal(maxSearchRadius: 11, maxTurns: 22);
            Assert.AreEqual(11, goal.MaxSearchRadius);
            Assert.AreEqual(22, goal.MaxTurns);
        }

        [Test]
        public void MoveToExteriorGoal_Failed_PropagatesViaFailToParent()
        {
            // Symmetric to MoveToInteriorGoal_Failed_PropagatesViaFailToParent.
            var zone = CreateOpenZone();
            var npc = CreateNpc(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();

            var sentinel = new SentinelParentGoal();
            brain.PushGoal(sentinel);

            var exteriorGoal = new MoveToExteriorGoal();
            sentinel.PushChildGoal(exteriorGoal);

            exteriorGoal.Failed(new MoveToGoal(0, 0));

            Assert.IsTrue(sentinel.ChildFailed,
                "MoveToExteriorGoal.Failed must propagate up via FailToParent.");
        }

        [Test]
        public void MoveToExteriorGoal_TakeAction_OnExteriorCell_DoesNotPushChild()
        {
            // Symmetric to interior early-return. Production line 55.
            var zone = CreateOpenZone();
            // Default Cell.IsInterior=false → cell at (5,5) is exterior.
            var npc = CreateNpc(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();

            var goal = new MoveToExteriorGoal();
            brain.PushGoal(goal);

            int countBefore = brain.GoalCount;
            goal.TakeAction();
            int countAfter = brain.GoalCount;

            Assert.AreEqual(countBefore, countAfter,
                "TakeAction on an exterior cell must not push a child.");
        }

        // ============================================================
        // FindNearestCellWhere — defensive paths + boundary cases
        // ============================================================

        [Test]
        public void FindNearestCellWhere_NullZone_ReturnsNull()
        {
            // Production line 412 guards on null zone.
            var result = AIHelpers.FindNearestCellWhere(
                zone: null, fromX: 5, fromY: 5,
                predicate: c => true,
                maxRadius: 5);
            Assert.IsNull(result,
                "Null zone must return null cleanly. The defensive guard " +
                "at production line 412 catches misuse from incomplete " +
                "test setup or partly-initialised entities.");
        }

        [Test]
        public void FindNearestCellWhere_NullPredicate_ReturnsNull()
        {
            // Symmetric defensive — line 412 also catches null predicate.
            var zone = CreateOpenZone();
            var result = AIHelpers.FindNearestCellWhere(
                zone, fromX: 5, fromY: 5,
                predicate: null,
                maxRadius: 5);
            Assert.IsNull(result,
                "Null predicate must return null cleanly.");
        }

        [Test]
        public void FindNearestCellWhere_StartCellNotPassable_ReturnsNull()
        {
            // Production line 420: BFS rejects a non-passable start cell.
            // This was post-review fix #2 (`ac9c5cc`). Pin it.
            var zone = CreateOpenZone();

            // Place a Solid entity on the start cell so it reads as
            // !IsPassable.
            var wall = new Entity { BlueprintName = "Wall" };
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart { DisplayName = "wall" });
            wall.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(wall, 5, 5);

            var result = AIHelpers.FindNearestCellWhere(
                zone, fromX: 5, fromY: 5,
                predicate: c => true,
                maxRadius: 5);
            Assert.IsNull(result,
                "Non-passable start cell must return null. Without this guard " +
                "(post-review fix in commit ac9c5cc), the BFS would silently " +
                "search from inside a wall, reaching nonsensical 'reachable' " +
                "cells.");
        }

        [Test]
        public void FindNearestCellWhere_StartCellMatchesPredicate_ReturnsStart()
        {
            // Boundary: the BFS tests the start cell first (production
            // line 436 inside the loop). If the start matches, return
            // the start position immediately, distance 0.
            var zone = CreateOpenZone();
            zone.GetCell(5, 5).IsInterior = true;

            var result = AIHelpers.FindNearestCellWhere(
                zone, fromX: 5, fromY: 5,
                predicate: c => c.IsInterior,
                maxRadius: 5);

            Assert.IsNotNull(result, "Match exists at start.");
            Assert.AreEqual(5, result.Value.x);
            Assert.AreEqual(5, result.Value.y);
        }

        [Test]
        public void FindNearestCellWhere_MaxRadiusZero_OnlyChecksStartCell()
        {
            // Boundary: maxRadius=0 means "only the starting cell." The
            // BFS pops the start, evaluates the predicate, and returns
            // null without expanding (line 438: `if (dist >= maxRadius)
            // continue;`).
            var zone = CreateOpenZone();
            zone.GetCell(5, 5).IsInterior = false;
            zone.GetCell(6, 5).IsInterior = true; // adjacent match

            var result = AIHelpers.FindNearestCellWhere(
                zone, fromX: 5, fromY: 5,
                predicate: c => c.IsInterior,
                maxRadius: 0);

            Assert.IsNull(result,
                "maxRadius=0 must NOT find an adjacent match. The BFS only " +
                "visits the start cell and returns null. If this finds (6,5), " +
                "the radius bound is off-by-one (>= vs >).");
        }

        [Test]
        public void FindNearestCellWhere_MaxRadiusZero_StartCellMatch_ReturnsStart()
        {
            // Counter-pin: with radius=0, if the start itself matches,
            // it IS returned (the predicate is checked before the
            // `dist >= maxRadius` cutoff).
            var zone = CreateOpenZone();
            zone.GetCell(5, 5).IsInterior = true;

            var result = AIHelpers.FindNearestCellWhere(
                zone, fromX: 5, fromY: 5,
                predicate: c => c.IsInterior,
                maxRadius: 0);

            Assert.IsNotNull(result,
                "maxRadius=0 + start matches must return the start. " +
                "Counter-check to the previous test — confirms the bound " +
                "is on EXPANSION, not on predicate evaluation.");
            Assert.AreEqual(5, result.Value.x);
            Assert.AreEqual(5, result.Value.y);
        }

        [Test]
        public void FindNearestCellWhere_4ConnectivityNotDiagonal()
        {
            // Production uses CardinalOffsets only (line 462). A diagonal
            // match should require at least 2 cardinal steps to reach,
            // not 1. So a diagonal-only-passable predicate should NOT
            // be the closest if a cardinal-2 alternative exists.
            //
            // Setup: place an interior cell at (5,6) (1 cardinal step
            // south) and another at (6,6) (Chebyshev=1 but BFS=2). The
            // (5,6) one must win because BFS distance (Manhattan) is
            // shorter — confirms 4-connectivity.
            var zone = CreateOpenZone();
            zone.GetCell(5, 6).IsInterior = true; // cardinal-1
            zone.GetCell(6, 6).IsInterior = true; // diagonal: BFS-distance 2

            var result = AIHelpers.FindNearestCellWhere(
                zone, fromX: 5, fromY: 5,
                predicate: c => c.IsInterior,
                maxRadius: 5);

            Assert.IsNotNull(result);
            Assert.AreEqual((5, 6), (result.Value.x, result.Value.y),
                "4-connectivity BFS must pick the cardinal-adjacent match " +
                "over a diagonally-adjacent one. If (6,6) is returned, the " +
                "BFS is using 8-connectivity instead of 4.");
        }

        // ============================================================
        // Cell.IsInterior — default value
        // ============================================================

        [Test]
        public void Cell_IsInterior_DefaultsFalse()
        {
            // Anchor: a freshly-constructed Cell is exterior by default.
            // Tagging is opt-in by VillageBuilder + MarkDungeonInterior.
            var zone = CreateOpenZone();
            var cell = zone.GetCell(5, 5);
            Assert.IsFalse(cell.IsInterior,
                "Default Cell.IsInterior must be false. New zones should be " +
                "treated as exterior until a generator opts cells in.");
        }

        // ============================================================
        // Test-only helper: parent-goal sentinel
        // ============================================================

        private class SentinelParentGoal : GoalHandler
        {
            public bool ChildFailed;
            public override void TakeAction() { }
            public override void Failed(GoalHandler child)
            {
                ChildFailed = true;
            }
        }
    }
}
