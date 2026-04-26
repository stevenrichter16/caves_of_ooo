using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M4 (Cell.IsInterior + MoveToInteriorGoal + MoveToExteriorGoal +
    /// FindNearestCellWhere + VillageBuilder + MarkDungeonInterior)
    /// adversarial cold-eye tests. Per Docs/QUD-PARITY.md §3.9.
    ///
    /// Companion to M4CoverageGapTests (commit 23ace19) which was
    /// gap-coverage style (read production first, pin observed
    /// contract). This file deliberately targets behaviors I have NOT
    /// already probed — sealed rooms, mid-flight cell-flag flips,
    /// stacked goals of the same type, bounds-of-zone BFS, runtime
    /// IsInterior mutability, OnPop on detached goals.
    ///
    /// I'm NOT re-reading production code while writing these. Each
    /// test commits a PREDICTION and CONFIDENCE; failures get
    /// classified test-wrong / code-wrong / setup-wrong in the commit
    /// message. Predictions I made by reading production line-by-line
    /// during the gap-coverage pass do NOT count as cold-eye — those
    /// I'm avoiding here.
    /// </summary>
    [TestFixture]
    public class M4AdversarialTests
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
            return entity;
        }

        /// <summary>
        /// Place a Solid wall at (x, y) so cell.IsPassable() returns false
        /// and the BFS treats it as a barrier.
        /// </summary>
        private static void BuildWall(Zone zone, int x, int y)
        {
            var wall = new Entity { BlueprintName = "Wall" };
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart { DisplayName = "wall" });
            wall.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(wall, x, y);
        }

        // ============================================================
        // FindNearestCellWhere — sealed-room + bounds-of-zone edges
        // ============================================================

        /// <summary>
        /// PREDICTION: BFS from inside a sealed room (no passable cells
        /// connect to the rest of the zone) returns null when the
        /// predicate-matching cell is outside the seal.
        /// CONFIDENCE: high — BFS only expands through IsPassable cells,
        /// so a wall on every side of the start should isolate.
        /// </summary>
        [Test]
        public void FindNearestCellWhere_SealedRoom_NoExitToMatch_ReturnsNull()
        {
            var zone = new Zone("SealedZone");
            // Seal a 1×1 cell at (5,5) with walls on all 4 cardinals.
            BuildWall(zone, 4, 5);
            BuildWall(zone, 6, 5);
            BuildWall(zone, 5, 4);
            BuildWall(zone, 5, 6);
            // Tag a cell OUTSIDE the seal as interior. The BFS should
            // never reach it.
            zone.GetCell(10, 10).IsInterior = true;

            var result = AIHelpers.FindNearestCellWhere(
                zone, fromX: 5, fromY: 5,
                predicate: c => c.IsInterior,
                maxRadius: 40);

            Assert.IsNull(result,
                "BFS sealed inside a 1×1 room with walls on all 4 cardinals " +
                "must return null when the only matching cell is outside the " +
                "seal. If non-null, the BFS is leaking through walls.");
        }

        /// <summary>
        /// PREDICTION: BFS starts from the zone's corner (0,0) and finds a
        /// match, even though most neighbor checks will hit out-of-bounds.
        /// The InBounds() guard at production line 447 protects the
        /// expansion loop.
        /// CONFIDENCE: high — defensive guard is the obvious shape.
        /// </summary>
        [Test]
        public void FindNearestCellWhere_StartAtZoneCorner_FindsMatchInBounds()
        {
            var zone = new Zone("CornerZone");
            // Match at (3, 3). Start at (0, 0) — three of the four
            // neighbors at start are out-of-bounds.
            zone.GetCell(3, 3).IsInterior = true;

            var result = AIHelpers.FindNearestCellWhere(
                zone, fromX: 0, fromY: 0,
                predicate: c => c.IsInterior,
                maxRadius: 10);

            Assert.IsNotNull(result,
                "BFS starting at zone corner must still find in-zone matches " +
                "without crashing on the OOB neighbor probes.");
            Assert.AreEqual((3, 3), (result.Value.x, result.Value.y));
        }

        /// <summary>
        /// PREDICTION: BFS does NOT revisit cells. With a maze-like setup
        /// and a far match, the BFS's visited set keeps it from ever
        /// looping. Specifically: place a long corridor that doubles back;
        /// the result must still be deterministically the cardinal-shortest
        /// match.
        /// CONFIDENCE: medium — visited-set is implemented (line 424); the
        /// open question is whether it's truly per-call or accidentally
        /// shared across calls.
        /// </summary>
        [Test]
        public void FindNearestCellWhere_VisitedSet_FreshPerCall()
        {
            // Two consecutive calls on the same zone with different
            // start positions must each return their own correct answer,
            // not be polluted by the previous call's visited set.
            var zone = new Zone("DoubleZone");
            zone.GetCell(8, 5).IsInterior = true;
            zone.GetCell(2, 5).IsInterior = true;

            // Call 1: start at (0, 5), expect to find (2, 5) (closer).
            var r1 = AIHelpers.FindNearestCellWhere(
                zone, fromX: 0, fromY: 5,
                predicate: c => c.IsInterior,
                maxRadius: 20);
            Assert.IsNotNull(r1);
            Assert.AreEqual((2, 5), (r1.Value.x, r1.Value.y));

            // Call 2: start at (10, 5), expect to find (8, 5) (closer
            // to the new start). If the visited set leaked across calls,
            // (8, 5) would be marked already-visited and skipped, returning
            // (2, 5) (or null).
            var r2 = AIHelpers.FindNearestCellWhere(
                zone, fromX: 10, fromY: 5,
                predicate: c => c.IsInterior,
                maxRadius: 20);
            Assert.IsNotNull(r2);
            Assert.AreEqual((8, 5), (r2.Value.x, r2.Value.y),
                "Second call must produce its own answer, not be polluted " +
                "by the previous call's visited set.");
        }

        /// <summary>
        /// PREDICTION: BFS expansion order is FIFO (queue), not LIFO (stack).
        /// This is what makes it a genuine BFS rather than a DFS. With two
        /// matches at the same cardinal distance, the BFS picks the one
        /// reached first by the queue order — not necessarily deterministic
        /// from the caller's perspective, but should be ONE of the two
        /// correct answers.
        /// CONFIDENCE: medium — the queue type is what determines this.
        /// </summary>
        [Test]
        public void FindNearestCellWhere_TiedDistanceMatches_ReturnsOneCorrectly()
        {
            var zone = new Zone("TiedZone");
            // Two interior cells, both at cardinal-distance 3 from start.
            zone.GetCell(8, 5).IsInterior = true;  // east (BFS dist 3)
            zone.GetCell(2, 5).IsInterior = true;  // west (BFS dist 3)

            var result = AIHelpers.FindNearestCellWhere(
                zone, fromX: 5, fromY: 5,
                predicate: c => c.IsInterior,
                maxRadius: 10);

            Assert.IsNotNull(result, "Should find one of the matches.");
            // Either is acceptable — pin only that ONE was returned.
            bool isEast = result.Value == (8, 5);
            bool isWest = result.Value == (2, 5);
            Assert.IsTrue(isEast || isWest,
                $"Returned cell ({result.Value.x},{result.Value.y}) should be " +
                "one of the two tied matches.");
        }

        // ============================================================
        // MoveToInteriorGoal — stacked goals, mid-flight flag flip,
        // FailToParent inside TakeAction
        // ============================================================

        /// <summary>
        /// PREDICTION: pushing two MoveToInteriorGoals onto the same stack
        /// is allowed (BrainPart doesn't gate by goal type at the
        /// PushGoal level — only AIBehaviorPart subclasses gate via
        /// HasGoal). Each runs independently from the top.
        /// CONFIDENCE: medium — no idempotency at the goal level. Could
        /// be there's some auto-dedup I missed.
        /// </summary>
        [Test]
        public void MoveToInteriorGoal_PushedTwiceOnStack_BothPresent()
        {
            var zone = new Zone("DoublePushZone");
            var npc = CreateNpc(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();

            brain.PushGoal(new MoveToInteriorGoal());
            brain.PushGoal(new MoveToInteriorGoal());

            int interiorGoalCount = 0;
            for (int i = 0; i < brain.GoalCount; i++)
            {
                if (brain.PeekGoalAt(i) is MoveToInteriorGoal) interiorGoalCount++;
            }

            Assert.AreEqual(2, interiorGoalCount,
                "BrainPart.PushGoal must NOT auto-dedup. Two MoveToInteriorGoals " +
                "must coexist on the stack — idempotency lives at AIBehaviorPart " +
                "level (HasGoal gate), not GoalHandler.");
        }

        /// <summary>
        /// PREDICTION: when an NPC stands on a cell that becomes IsInterior=true
        /// AFTER the goal was pushed (e.g., a future "build roof" mechanic
        /// flipping the cell flag), the next Finished() check returns true
        /// and the goal pops. The goal is reactive to the current cell's
        /// state, not the state at push-time.
        /// CONFIDENCE: medium-high — Finished() reads cell.IsInterior live.
        /// </summary>
        [Test]
        public void MoveToInteriorGoal_CellBecomesInteriorAfterPush_FinishedFlipsTrue()
        {
            var zone = new Zone("FlipZone");
            // Cell starts as exterior.
            Assert.IsFalse(zone.GetCell(5, 5).IsInterior, "Setup: starts exterior.");

            var npc = CreateNpc(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();
            var goal = new MoveToInteriorGoal();
            brain.PushGoal(goal);

            // Pre-flip: not finished, NPC is on exterior cell.
            Assert.IsFalse(goal.Finished(), "Pre-flip: must not be finished.");

            // Flip the flag — simulating a future "roof built over me"
            // gameplay event.
            zone.GetCell(5, 5).IsInterior = true;

            Assert.IsTrue(goal.Finished(),
                "Mid-flight IsInterior=true on the NPC's cell must satisfy " +
                "Finished(). The goal reads the live state, not a snapshot.");
        }

        /// <summary>
        /// PREDICTION: TakeAction with no zone match (BFS returns null)
        /// triggers FailToParent. That should pop the goal (FailToParent
        /// → Pop). After TakeAction returns, the goal should NOT still be
        /// on the stack.
        /// CONFIDENCE: medium-high — FailToParent semantic is well-known.
        /// </summary>
        [Test]
        public void MoveToInteriorGoal_NoMatchInZone_FailToParent_PopsGoal()
        {
            var zone = new Zone("NoInteriorZone");
            // Zero interior cells in this zone.
            var npc = CreateNpc(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();
            var goal = new MoveToInteriorGoal(maxSearchRadius: 2);
            brain.PushGoal(goal);

            Assume.That(brain.HasGoal<MoveToInteriorGoal>(), "Setup: goal pushed.");

            goal.TakeAction();

            Assert.IsFalse(brain.HasGoal<MoveToInteriorGoal>(),
                "When BFS returns null, the goal must FailToParent and pop. " +
                "If still present after TakeAction, FailToParent isn't being " +
                "called or isn't popping correctly.");
        }

        /// <summary>
        /// PREDICTION: pushing a NEW MoveToInteriorGoal while one is
        /// already running (and pushing children) leaves both on the stack;
        /// the newer one is on top and runs first. The older one resumes
        /// after the newer one finishes.
        /// CONFIDENCE: medium. Unsure if BrainPart cleanup interferes.
        /// </summary>
        [Test]
        public void MoveToInteriorGoal_NewerGoalOnTopRunsFirst()
        {
            var zone = new Zone("OrderZone");
            zone.GetCell(7, 5).IsInterior = true;
            var npc = CreateNpc(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();

            var older = new MoveToInteriorGoal(maxSearchRadius: 5, maxTurns: 100);
            var newer = new MoveToInteriorGoal(maxSearchRadius: 5, maxTurns: 50);
            brain.PushGoal(older);
            brain.PushGoal(newer);

            // The TOP goal must be the newer one — verify by peek.
            var top = brain.PeekGoalAt(brain.GoalCount - 1);
            Assert.AreSame(newer, top,
                "PushGoal LIFO contract — newer goal sits on top.");
        }

        // ============================================================
        // OnPop — detached and unusual states
        // ============================================================

        /// <summary>
        /// PREDICTION: OnPop on a goal whose ParentBrain was never set
        /// must not throw. The thought-clearing path uses
        /// ParentBrain?-style null-shortcuts.
        /// CONFIDENCE: medium. If OnPop dereferences without null-checks,
        /// this will crash.
        /// </summary>
        [Test]
        public void MoveToInteriorGoal_OnPop_NoBrainAttached_DoesNotThrow()
        {
            var goal = new MoveToInteriorGoal();
            // Never PushGoal — ParentBrain stays null.

            Assert.DoesNotThrow(() => goal.OnPop(),
                "OnPop on a never-pushed goal must not crash. ParentBrain " +
                "is null — the production must null-shortcut its access.");
        }

        /// <summary>
        /// PREDICTION: OnPop fires when the goal is removed via
        /// BrainPart.RemoveGoal — even if it was never reached via the
        /// natural Finished() → cleanup path. The OnPop hook is
        /// independent of how the goal exited.
        /// CONFIDENCE: medium-high.
        /// </summary>
        [Test]
        public void MoveToInteriorGoal_RemoveGoal_FiresOnPop_WritesTerminalThought()
        {
            var zone = new Zone("OnPopZone");
            zone.GetCell(5, 5).IsInterior = true;  // NPC starts inside
            var npc = CreateNpc(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();

            var goal = new MoveToInteriorGoal();
            brain.PushGoal(goal);

            brain.RemoveGoal(goal);

            // OnPop runs and writes "sheltered" because the cell IS interior.
            Assert.AreEqual("sheltered", brain.LastThought,
                "Manual RemoveGoal must still fire OnPop. If LastThought " +
                "doesn't reflect the terminal thought, OnPop wasn't called.");
        }

        // ============================================================
        // Cell.IsInterior — runtime mutability
        // ============================================================

        /// <summary>
        /// PREDICTION: Cell.IsInterior is a public field, freely
        /// mutable at runtime. Future mechanics like "build a roof"
        /// or "destroy a wall" can flip it.
        /// CONFIDENCE: high — it's a public field, no setter logic.
        /// </summary>
        [Test]
        public void Cell_IsInterior_RuntimeMutability_BothDirections()
        {
            var zone = new Zone("MutateZone");
            var cell = zone.GetCell(5, 5);

            Assert.IsFalse(cell.IsInterior, "Default false.");
            cell.IsInterior = true;
            Assert.IsTrue(cell.IsInterior, "Flipped true.");
            cell.IsInterior = false;
            Assert.IsFalse(cell.IsInterior, "Flipped back false.");
        }
    }
}
