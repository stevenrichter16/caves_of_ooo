using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D2.4 hook integration tests — <c>TurnManager</c> emits a
    /// <c>turn/Begin</c> record when an actor's turn starts and a
    /// <c>turn/End</c> record when <see cref="TurnManager.EndTurn"/>
    /// fires. Boundary markers for windowed queries.
    ///
    /// Plan ref: <c>Docs/D2-HOOKS-PLAN.md</c> §4 D2.4.
    ///
    /// Six invariants:
    ///   1. BeginTakeAction produces a turn/Begin record.
    ///   2. EndTurn produces a turn/End record.
    ///   3. Begin precedes End in the buffer (oldest-first ordering).
    ///   4. ActorId matches the actor whose turn it is.
    ///   5. Payload carries hp at the turn boundary.
    ///   6. Counter-check: blocked turn (stun-block returns false from
    ///      BeginTakeAction → EndTurn cleanup path runs anyway) still
    ///      produces BOTH Begin AND End records.
    /// </summary>
    public class DiagTurnHookTests
    {
        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. Turn start produces Begin record
        // ====================================================================

        [Test]
        public void BeginTakeAction_ProducesTurnBeginRecord()
        {
            var player = MakePlayer();
            var tm = new TurnManager();
            tm.AddEntity(player);
            tm.ProcessUntilPlayerTurn();

            var begins = Diag.Snapshot(2000)
                .Where(r => r.Category == "turn" && r.Kind == "Begin" && r.ActorId == player.ID)
                .ToList();
            Assert.GreaterOrEqual(begins.Count, 1,
                $"Expected at least one turn/Begin record for the player. " +
                $"Got: [{string.Join(", ", Diag.Snapshot(2000).Select(r => r.Category + "/" + r.Kind + ":" + r.ActorId))}]");
        }

        // ====================================================================
        // 2. EndTurn produces End record
        // ====================================================================

        [Test]
        public void EndTurn_ProducesTurnEndRecord()
        {
            var player = MakePlayer();
            var tm = new TurnManager();
            tm.AddEntity(player);
            tm.ProcessUntilPlayerTurn();

            int beforeEndCount = Diag.Snapshot(2000)
                .Count(r => r.Category == "turn" && r.Kind == "End");

            tm.EndTurn(player);

            int afterEndCount = Diag.Snapshot(2000)
                .Count(r => r.Category == "turn" && r.Kind == "End");

            Assert.Greater(afterEndCount, beforeEndCount,
                "EndTurn must produce a turn/End record.");
        }

        // ====================================================================
        // 3. Begin precedes End in oldest-first ordering
        // ====================================================================

        [Test]
        public void TurnPair_BeginBeforeEnd_OrderedInBuffer()
        {
            var player = MakePlayer();
            var tm = new TurnManager();
            tm.AddEntity(player);
            tm.ProcessUntilPlayerTurn();
            tm.EndTurn(player);

            var turnRecords = Diag.Snapshot(2000)
                .Where(r => r.Category == "turn" && r.ActorId == player.ID)
                .ToList();

            Assert.GreaterOrEqual(turnRecords.Count, 2,
                "Expected at least one Begin + one End for player.");

            int firstBeginIdx = turnRecords.FindIndex(r => r.Kind == "Begin");
            int firstEndIdx = turnRecords.FindIndex(r => r.Kind == "End");
            Assert.GreaterOrEqual(firstBeginIdx, 0, "Begin must exist.");
            Assert.GreaterOrEqual(firstEndIdx, 0, "End must exist.");
            Assert.Less(firstBeginIdx, firstEndIdx,
                "Begin record must appear BEFORE End in the oldest-first " +
                "buffer ordering. If End appears first, the hook insertion " +
                "sites are reversed.");
        }

        // ====================================================================
        // 4. ActorId matches
        // ====================================================================

        [Test]
        public void TurnRecord_ActorIdMatchesActor()
        {
            var player = MakePlayer();
            player.ID = "player-007";
            var tm = new TurnManager();
            tm.AddEntity(player);
            tm.ProcessUntilPlayerTurn();
            tm.EndTurn(player);

            var turnRecords = Diag.Snapshot(2000)
                .Where(r => r.Category == "turn")
                .ToList();
            foreach (var r in turnRecords)
            {
                Assert.AreEqual("player-007", r.ActorId,
                    $"All turn records must carry the acting entity's ID. Got: {r.ActorId}");
            }
        }

        // ====================================================================
        // 5. Payload carries hp at the turn boundary
        // ====================================================================

        [Test]
        public void TurnRecord_PayloadCarriesHpAtTurnBoundary()
        {
            var player = MakePlayer(hp: 73);
            var tm = new TurnManager();
            tm.AddEntity(player);
            tm.ProcessUntilPlayerTurn();

            var begin = Diag.Snapshot(2000)
                .First(r => r.Category == "turn" && r.Kind == "Begin");
            Assert.IsTrue(begin.PayloadJson.Contains("\"hp\":73"),
                $"turn/Begin payload must include hp at turn-start. Got: {begin.PayloadJson}");
        }

        // ====================================================================
        // 6. Counter-check: blocked turn produces both Begin and End
        // ====================================================================

        [Test]
        public void BlockedTurn_StillProducesBothBeginAndEnd()
        {
            // Apply a stun (Duration=2 so the JustApplied skip leaves it
            // active for the next turn) and let TurnManager run a turn
            // — BeginTakeAction returns false (action blocked), and the
            // EndTurn cleanup path fires inside TurnManager itself.
            // Both Begin AND End records must still appear.
            var player = MakePlayer();
            player.ApplyEffect(new StunnedEffect(duration: 2));

            var tm = new TurnManager();
            tm.AddEntity(player);
            tm.ProcessUntilPlayerTurn();
            tm.EndTurn(player);
            // At this point Stun.Duration ticked 2→1 (apply turn skipped).
            // Process again — BeginTakeAction will be blocked by Stun(1).
            // The TurnManager's internal flow runs EndTurn from line ~244
            // when BeginTakeAction returns false.
            Diag.ResetAll();   // isolate this turn's records from setup noise

            tm.ProcessUntilPlayerTurn();

            var records = Diag.Snapshot(2000)
                .Where(r => r.Category == "turn" && r.ActorId == player.ID)
                .ToList();
            int beginCount = records.Count(r => r.Kind == "Begin");
            int endCount = records.Count(r => r.Kind == "End");
            Assert.GreaterOrEqual(beginCount, 1,
                "Even a blocked turn must record a turn/Begin (the actor's " +
                "turn started; the action was blocked, but the boundary is real).");
            Assert.GreaterOrEqual(endCount, 1,
                "Even a blocked turn must record a turn/End (TurnManager " +
                "calls EndTurn from line ~244 when BeginTakeAction returns false). " +
                $"Got Begin={beginCount}, End={endCount}");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakePlayer(int hp = 100)
        {
            var e = new Entity { BlueprintName = "TestPlayer" };
            e.Tags["Player"] = "";
            e.Tags["Creature"] = "";
            e.ID = "test-player-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = hp, Max = hp, Owner = e };
            e.Statistics["DV"] = new Stat { BaseValue = 4, Owner = e };
            e.Statistics["Toughness"] = new Stat { BaseValue = 10, Owner = e };
            e.Statistics["Speed"] = new Stat { BaseValue = 100, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test player" });
            return e;
        }
    }
}
