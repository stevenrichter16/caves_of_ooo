using System;
using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven AI tests. Pre-fix: "why is the NPC standing
    /// still?" was un-debuggable without log-grep + execute_code state
    /// probing. Post-fix: BrainPart emits four diag kinds under
    /// <c>category="ai"</c>:
    ///
    /// <list type="bullet">
    ///   <item><c>GoalPushed</c> — every PushGoal call (type + details + new depth)</item>
    ///   <item><c>GoalPopped</c> — every RemoveGoal call (type + details + depth after)</item>
    ///   <item><c>GoalSelected</c> — top-of-stack goal at start of each AI turn (type + details + age + hasTarget)</item>
    ///   <item><c>TurnSkipped</c> — early-return paths (NoZone / NotInZone / InConversation) with reason</item>
    /// </list>
    ///
    /// <para>Player frames are NOT emitted to avoid flooding the buffer
    /// — see BrainPart.HandleTakeTurn early-return path for Player tag.</para>
    /// </summary>
    public class AIObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeNPC(Zone zone, int x, int y, string id = "npc")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(new BrainPart
            {
                CurrentZone = zone,
                Rng = new Random(123),
            });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static void DumpAIRecords(string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai",
                Limit = 20,
            }).Records;

            TestContext.WriteLine($"\n=== {label} ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine(
                    $"  [{i}] {r.Kind,-15} actor={r.ActorId,-10} :: {r.PayloadJson}");
            }
        }

        // ── Tests ────────────────────────────────────────────────

        [Test]
        public void PushGoal_EmitsGoalPushedRecord()
        {
            var zone = new Zone("AIZone");
            var npc = MakeNPC(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();

            brain.PushGoal(new WanderRandomlyGoal());

            DumpAIRecords("push WanderRandomlyGoal");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Actor = "npc", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("GoalPushed", records[0].Kind);
            StringAssert.Contains("\"goal\":\"WanderRandomlyGoal\"", records[0].PayloadJson);
            StringAssert.Contains("\"stackDepth\":1", records[0].PayloadJson);
        }

        [Test]
        public void RemoveGoal_EmitsGoalPoppedRecord()
        {
            var zone = new Zone("AIZone");
            var npc = MakeNPC(zone, 5, 5);
            var brain = npc.GetPart<BrainPart>();
            var goal = new WanderRandomlyGoal();
            brain.PushGoal(goal);
            Diag.ResetAll();  // ignore the push, focus on pop

            brain.RemoveGoal(goal);

            DumpAIRecords("pop the wander goal");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Actor = "npc", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("GoalPopped", records[0].Kind);
            StringAssert.Contains("\"goal\":\"WanderRandomlyGoal\"", records[0].PayloadJson);
            StringAssert.Contains("\"stackDepthAfter\":0", records[0].PayloadJson);
        }

        [Test]
        public void TakeTurn_EmitsGoalSelected_ForTopOfStack()
        {
            // BoredGoal is auto-pushed by HandleTakeTurn when the stack
            // is empty. Each TakeTurn emits GoalSelected for the top.
            var zone = new Zone("AIZone");
            var npc = MakeNPC(zone, 5, 5);

            // Fire the TakeTurn event manually
            var ev = GameEvent.New("TakeTurn");
            ev.SetParameter("Actor", (object)npc);
            npc.FireEventAndRelease(ev);

            DumpAIRecords("NPC's first turn — should auto-push Bored");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Actor = "npc", Limit = 20,
            }).Records;
            // Expected sequence on the first AI turn:
            //   GoalPushed:BoredGoal (because stack empty) → GoalSelected:BoredGoal
            // BoredGoal.TakeAction may push child goals (WanderRandomly, etc.)
            // and immediately execute them; those produce additional records.
            Assert.IsTrue(records.Count >= 2,
                "First turn should emit at least GoalPushed + GoalSelected.");

            // Top of stack at selection time MUST be BoredGoal — that's the
            // auto-fallback "what's the NPC doing right now?" record.
            var selected = records.First(r => r.Kind == "GoalSelected");
            StringAssert.Contains("\"goal\":\"BoredGoal\"", selected.PayloadJson);
        }

        [Test]
        public void TakeTurn_InConversation_EmitsTurnSkippedWithReason()
        {
            var zone = new Zone("AIZone");
            var npc = MakeNPC(zone, 5, 5);
            npc.GetPart<BrainPart>().InConversation = true;

            var ev = GameEvent.New("TakeTurn");
            ev.SetParameter("Actor", (object)npc);
            npc.FireEventAndRelease(ev);

            DumpAIRecords("NPC mid-conversation skips turn");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Actor = "npc", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("TurnSkipped", records[0].Kind);
            StringAssert.Contains("\"reason\":\"InConversation\"", records[0].PayloadJson);
        }

        [Test]
        public void TakeTurn_DetachedFromZone_EmitsTurnSkippedNotInZone()
        {
            // Edge case: NPC has BrainPart with CurrentZone set, but it's
            // NOT placed in the zone (dead/teleported state). Should skip.
            var zone = new Zone("AIZone");
            var npc = new Entity { ID = "ghost", BlueprintName = "ghost" };
            npc.Tags["Creature"] = "";
            npc.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            npc.AddPart(new RenderPart { DisplayName = "ghost" });
            npc.AddPart(new BrainPart { CurrentZone = zone });
            // INTENTIONALLY NOT calling zone.AddEntity

            var ev = GameEvent.New("TakeTurn");
            ev.SetParameter("Actor", (object)npc);
            npc.FireEventAndRelease(ev);

            DumpAIRecords("detached NPC skip");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Actor = "ghost", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("TurnSkipped", records[0].Kind);
            StringAssert.Contains("\"reason\":\"NotInZone\"", records[0].PayloadJson);
        }

        [Test]
        public void TakeTurn_NoZone_EmitsTurnSkippedNoZone()
        {
            // Counter-check the NoZone path.
            var npc = new Entity { ID = "limbo", BlueprintName = "limbo" };
            npc.Tags["Creature"] = "";
            npc.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            npc.AddPart(new RenderPart { DisplayName = "limbo" });
            npc.AddPart(new BrainPart { CurrentZone = null });

            var ev = GameEvent.New("TakeTurn");
            ev.SetParameter("Actor", (object)npc);
            npc.FireEventAndRelease(ev);

            DumpAIRecords("no-zone skip");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Actor = "limbo", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("TurnSkipped", records[0].Kind);
            StringAssert.Contains("\"reason\":\"NoZone\"", records[0].PayloadJson);
        }

        [Test]
        public void PlayerTakeTurn_DoesNotEmit_ToAvoidFlood()
        {
            // Counter-check: player frames must NOT emit AI records.
            // The HandleTakeTurn fast-path returns silently for Player tag
            // because it runs every frame.
            var zone = new Zone("AIZone");
            var player = new Entity { ID = "p", BlueprintName = "player" };
            player.Tags["Creature"] = "";
            player.Tags["Player"] = "";
            player.AddPart(new RenderPart { DisplayName = "you" });
            player.AddPart(new BrainPart { CurrentZone = zone });
            zone.AddEntity(player, 5, 5);

            var ev = GameEvent.New("TakeTurn");
            ev.SetParameter("Actor", (object)player);
            player.FireEventAndRelease(ev);

            DumpAIRecords("player frame — should be silent");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Actor = "p", Limit = 20,
            }).Records;
            Assert.AreEqual(0, records.Count,
                "Player frames must not emit AI diag records (would flood the buffer).");
        }

        // ─── FindPath PathFailed records ─────────────────────────

        [Test]
        public void FindPath_NullZone_EmitsPathFailedNullZone()
        {
            FindPath.Search(null, 0, 0, 5, 5);

            DumpAIRecords("null-zone pathfind");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Kind = "PathFailed", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"reason\":\"NullZone\"", records[0].PayloadJson);
        }

        [Test]
        public void FindPath_OutOfBounds_EmitsPathFailedOutOfBounds()
        {
            var zone = new Zone("AIZone");
            FindPath.Search(zone, -5, -5, 100, 100);

            DumpAIRecords("out-of-bounds pathfind");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Kind = "PathFailed", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"reason\":\"OutOfBounds\"", records[0].PayloadJson);
        }

        [Test]
        public void FindPath_SameCell_DoesNotEmit_SuccessIsSilent()
        {
            // Counter-check: success paths must NOT emit PathFailed.
            // Same-cell is a trivial success short-circuit.
            var zone = new Zone("AIZone");
            var result = FindPath.Search(zone, 5, 5, 5, 5);
            Assert.IsTrue(result.Usable);

            DumpAIRecords("same-cell pathfind (silent success)");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Limit = 20,
            }).Records;
            Assert.AreEqual(0, records.Count,
                "Successful pathfind must be silent to avoid flooding " +
                "the buffer on populated zones with many AI ticks.");
        }

        [Test]
        public void FindPath_BlockedByWalls_EmitsPathFailedNoPath()
        {
            // Build a tiny zone with a wall ring around the start so the
            // pathfind cannot reach a goal on the other side.
            var zone = new Zone("AIZone");
            // Surround (5,5) with Solid walls at distance 1.
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var wall = new Entity { ID = $"wall{dx}_{dy}", BlueprintName = "Wall" };
                    wall.Tags["Solid"] = "";
                    wall.AddPart(new PhysicsPart { Solid = true });
                    zone.AddEntity(wall, 5 + dx, 5 + dy);
                }
            }
            // Goal is far away
            var result = FindPath.Search(zone, 5, 5, 20, 15);
            Assert.IsFalse(result.Usable);

            DumpAIRecords("walled-in pathfind (NoPath)");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Kind = "PathFailed", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"reason\":\"NoPath\"", records[0].PayloadJson);
            StringAssert.Contains("\"fromX\":5", records[0].PayloadJson);
            StringAssert.Contains("\"toY\":15", records[0].PayloadJson);
        }

        [Test]
        public void TwoTurns_SameNPC_GoalSelectedFiresTwice()
        {
            // Counter-check: each AI turn produces its own GoalSelected.
            // If only one fired, the per-turn correlation would be broken.
            var zone = new Zone("AIZone");
            var npc = MakeNPC(zone, 5, 5);

            for (int i = 0; i < 2; i++)
            {
                var ev = GameEvent.New("TakeTurn");
                ev.SetParameter("Actor", (object)npc);
                npc.FireEventAndRelease(ev);
            }

            DumpAIRecords("two consecutive AI turns");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "ai", Actor = "npc", Kind = "GoalSelected", Limit = 20,
            }).Records;
            Assert.AreEqual(2, records.Count,
                "Each AI turn must emit its own GoalSelected.");
        }
    }
}
