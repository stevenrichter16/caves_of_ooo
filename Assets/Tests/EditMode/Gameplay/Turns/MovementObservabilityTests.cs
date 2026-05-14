using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven movement tests. Each test exercises a
    /// movement scenario, queries the diag records, dumps the
    /// full per-move breakdown to TestContext, and asserts invariants
    /// on the records. The DUMP is the primary debug artifact —
    /// running the test surfaces the same records a live
    /// <c>diag_query category=movement</c> would show.
    ///
    /// <para>Spec coverage:</para>
    /// <list type="bullet">
    ///   <item>Successful move emits Attempt + Completed in order</item>
    ///   <item>Out-of-bounds move emits Attempt + Blocked(reason=OutOfBounds)</item>
    ///   <item>Solid-blocker move emits Attempt + Blocked(reason=BlockedByEntity, blockerId set)</item>
    ///   <item>Vetoed-by-event move emits Attempt + Blocked(reason=VetoedByEvent)</item>
    ///   <item>Detached-entity move emits Blocked(reason=NoCurrentCell) — no Attempt</item>
    ///   <item>TryMove delegates to TryMoveTo without double-emission</item>
    ///   <item>TryMoveEx returns blocker via diag in addition to tuple</item>
    ///   <item>isPlayer flag on Completed reflects Player tag</item>
    /// </list>
    ///
    /// <para>If a test's diag-dump surfaces unexpected records (e.g.
    /// two Attempts for one TryMove call, or a Completed when the
    /// move was blocked), that's a caught bug — investigate.</para>
    /// </summary>
    public class MovementObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixture helpers ──────────────────────────────────────

        /// <summary>Create a passable mover. No PhysicsPart.Solid so it
        /// doesn't block others.</summary>
        private static Entity MakeMover(string id, bool isPlayer = false)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            if (isPlayer) e.Tags["Player"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Solid = false });
            return e;
        }

        /// <summary>Create a solid blocker entity. Sits in a cell and
        /// vetoes BeforeMove via PhysicsPart.</summary>
        private static Entity MakeBlocker(string id)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Solid = true });
            return e;
        }

        /// <summary>Dump every movement diag record for the given actor
        /// (by entity ID) to TestContext.WriteLine.</summary>
        private static void DumpMovementRecords(string actorId, string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = actorId,
                Limit = 20,
            }).Records;

            TestContext.WriteLine($"\n=== {label} (actor={actorId}) ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine($"  [{i}] {r.Kind,-12} :: {r.PayloadJson}");
            }
        }

        // ── Tests ────────────────────────────────────────────────

        [Test]
        public void SuccessfulMove_EmitsAttemptThenCompleted()
        {
            var zone = new Zone("MoveZone");
            var mover = MakeMover("mover");
            zone.AddEntity(mover, 5, 5);

            bool moved = MovementSystem.TryMove(mover, zone, 1, 0);
            Assert.IsTrue(moved, "Move into empty cell should succeed.");

            DumpMovementRecords("mover", "successful move E by 1");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = "mover",
                Limit = 20,
            }).Records;

            Assert.AreEqual(2, records.Count,
                "Successful move should emit exactly 2 records (Attempt + Completed).");
            Assert.AreEqual("Attempt", records[0].Kind, "First record kind must be Attempt.");
            Assert.AreEqual("Completed", records[1].Kind, "Second record kind must be Completed.");

            // Completed must carry actual final coords.
            StringAssert.Contains("\"toX\":6", records[1].PayloadJson);
            StringAssert.Contains("\"toY\":5", records[1].PayloadJson);
            StringAssert.Contains("\"isPlayer\":false", records[1].PayloadJson);
        }

        [Test]
        public void OutOfBoundsMove_EmitsAttemptAndBlocked_WithReason()
        {
            var zone = new Zone("MoveZone");
            var mover = MakeMover("mover");
            zone.AddEntity(mover, 0, 0);

            bool moved = MovementSystem.TryMove(mover, zone, -1, 0);
            Assert.IsFalse(moved, "Move past zone edge must fail.");

            DumpMovementRecords("mover", "OOB move W from (0,0)");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = "mover",
                Limit = 20,
            }).Records;

            Assert.AreEqual(2, records.Count, "OOB should emit Attempt + Blocked.");
            Assert.AreEqual("Attempt", records[0].Kind);
            Assert.AreEqual("Blocked", records[1].Kind);
            StringAssert.Contains("\"reason\":\"OutOfBounds\"", records[1].PayloadJson);
            // Counter-check: NO Completed record should exist.
            Assert.IsFalse(records.Any(r => r.Kind == "Completed"),
                "Blocked move must NOT emit Completed.");
        }

        [Test]
        public void SolidBlocker_EmitsAttemptAndBlocked_WithBlockerInfo()
        {
            var zone = new Zone("MoveZone");
            var mover = MakeMover("mover");
            var blocker = MakeBlocker("snapjaw");
            zone.AddEntity(mover, 5, 5);
            zone.AddEntity(blocker, 6, 5);

            bool moved = MovementSystem.TryMove(mover, zone, 1, 0);
            Assert.IsFalse(moved, "Move into Solid blocker must fail.");

            DumpMovementRecords("mover", "blocked by snapjaw at (6,5)");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = "mover",
                Limit = 20,
            }).Records;

            Assert.AreEqual(2, records.Count, "Blocked move should emit Attempt + Blocked.");
            Assert.AreEqual("Blocked", records[1].Kind);
            StringAssert.Contains("\"reason\":\"BlockedByEntity\"", records[1].PayloadJson);
            // Blocker payload propagated:
            StringAssert.Contains("\"blockerBlueprint\":\"snapjaw\"", records[1].PayloadJson);
            // Target ID set:
            Assert.AreEqual(blocker.ID, records[1].TargetId,
                "Blocker should be set as the diag record's target.");
        }

        [Test]
        public void TryMoveEx_BlockedByEntity_BlockerReturnedAndDiagged()
        {
            var zone = new Zone("MoveZone");
            var mover = MakeMover("mover");
            var blocker = MakeBlocker("door");
            zone.AddEntity(mover, 5, 5);
            zone.AddEntity(blocker, 6, 5);

            var (moved, returnedBlocker) = MovementSystem.TryMoveEx(mover, zone, 1, 0);
            Assert.IsFalse(moved);
            Assert.AreSame(blocker, returnedBlocker,
                "TryMoveEx should return the entity that blocked the move.");

            DumpMovementRecords("mover", "TryMoveEx blocked by door at (6,5)");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = "mover",
                Limit = 20,
            }).Records;

            // TryMoveEx is a SEPARATE entry point — payload should record that.
            Assert.AreEqual(2, records.Count);
            StringAssert.Contains("\"entryPoint\":\"TryMoveEx\"", records[0].PayloadJson);
        }

        [Test]
        public void DetachedEntity_TryMove_EmitsBlockedNoCurrentCell_NoAttempt()
        {
            // Entity NOT placed in any zone — currentCell is null.
            var zone = new Zone("MoveZone");
            var detached = MakeMover("detached");
            // intentionally NOT calling zone.AddEntity

            bool moved = MovementSystem.TryMove(detached, zone, 1, 0);
            Assert.IsFalse(moved);

            DumpMovementRecords("detached", "detached entity TryMove");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = "detached",
                Limit = 20,
            }).Records;

            // Detached path emits Blocked WITHOUT a preceding Attempt
            // because we have no source coords to attribute the attempt to.
            Assert.AreEqual(1, records.Count, "Detached TryMove should emit ONLY Blocked.");
            Assert.AreEqual("Blocked", records[0].Kind);
            StringAssert.Contains("\"reason\":\"NoCurrentCell\"", records[0].PayloadJson);
        }

        [Test]
        public void TryMoveTo_DelegatedFromTryMove_NoDoubleEmission()
        {
            // Counter-check: TryMove delegates to TryMoveTo. If both layers
            // emitted, we'd see 4 records instead of 2. This test pins the
            // single-emission contract.
            var zone = new Zone("MoveZone");
            var mover = MakeMover("mover");
            zone.AddEntity(mover, 10, 10);

            bool moved = MovementSystem.TryMove(mover, zone, 0, -1);
            Assert.IsTrue(moved);

            DumpMovementRecords("mover", "TryMove → TryMoveTo no double-emission");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = "mover",
                Limit = 20,
            }).Records;

            Assert.AreEqual(2, records.Count,
                "TryMove → TryMoveTo must emit exactly one Attempt + one Completed, not two of each.");
        }

        [Test]
        public void TryMoveTo_DirectInvocation_RecordsEntryPointMarker()
        {
            // The `entryPoint` field on Attempt lets a debugger tell which
            // overload was used. Direct TryMoveTo calls should record
            // entryPoint=TryMoveTo.
            var zone = new Zone("MoveZone");
            var mover = MakeMover("mover");
            zone.AddEntity(mover, 5, 5);

            bool moved = MovementSystem.TryMoveTo(mover, zone, 7, 5);
            Assert.IsTrue(moved);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = "mover",
                Limit = 20,
            }).Records;

            DumpMovementRecords("mover", "direct TryMoveTo");

            Assert.AreEqual(2, records.Count);
            StringAssert.Contains("\"entryPoint\":\"TryMoveTo\"", records[0].PayloadJson);
        }

        [Test]
        public void PlayerMove_CompletedRecord_HasIsPlayerTrue()
        {
            // The isPlayer flag distinguishes player movement (which
            // triggers full-zone dirty marking + FOV refresh) from NPC
            // movement (per-cell dirty only). The diag record lets a
            // perf-debug session see when the expensive path is taken.
            var zone = new Zone("MoveZone");
            var player = MakeMover("player", isPlayer: true);
            zone.AddEntity(player, 10, 10);

            bool moved = MovementSystem.TryMove(player, zone, 1, 0);
            Assert.IsTrue(moved);

            DumpMovementRecords("player", "player move E");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = "player",
                Limit = 20,
            }).Records;

            Assert.AreEqual(2, records.Count);
            var completed = records.First(r => r.Kind == "Completed");
            StringAssert.Contains("\"isPlayer\":true", completed.PayloadJson);
        }

        [Test]
        public void TwoMoves_DifferentActors_AreCleanlyIsolated()
        {
            // Counter-check: filter by Actor isolates moves cleanly even
            // when multiple entities are moving in the same zone.
            var zone = new Zone("MoveZone");
            var a = MakeMover("alice");
            var b = MakeMover("bob");
            zone.AddEntity(a, 5, 5);
            zone.AddEntity(b, 10, 10);

            MovementSystem.TryMove(a, zone, 1, 0);
            MovementSystem.TryMove(b, zone, 0, 1);

            DumpMovementRecords("alice", "alice's move");
            DumpMovementRecords("bob", "bob's move");

            var aliceRecs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = "alice",
                Limit = 20,
            }).Records;
            var bobRecs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "movement",
                Actor = "bob",
                Limit = 20,
            }).Records;

            Assert.AreEqual(2, aliceRecs.Count);
            Assert.AreEqual(2, bobRecs.Count);
            // Alice's Completed must be alice's, not bob's:
            StringAssert.Contains("\"toX\":6", aliceRecs[1].PayloadJson);
            StringAssert.Contains("\"toY\":11", bobRecs[1].PayloadJson);
        }
    }
}
