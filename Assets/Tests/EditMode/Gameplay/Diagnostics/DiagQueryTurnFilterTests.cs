using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D3.1 tests for the <c>SinceTurn</c> / <c>UntilTurn</c> filter on
    /// <see cref="DiagQuery.Apply"/> and <see cref="DiagQuery.Count"/>.
    ///
    /// Plan ref: <c>Docs/D3-TOOLS-PLAN.md</c> §4 D3.1.
    ///
    /// Contract: records with <c>Turn=null</c> (out-of-turn events:
    /// worldgen, save, bootstrap, UI) are <strong>EXCLUDED</strong>
    /// from any query that uses <c>SinceTurn</c> or <c>UntilTurn</c>.
    /// Use the wall-clock filters (D4) to include them.
    ///
    /// Four invariants:
    ///   1. SinceTurn narrows to records at-or-after the bound.
    ///   2. UntilTurn narrows to records at-or-before the bound.
    ///   3. Both bounds form a window (intersection).
    ///   4. Counter-check: records with Turn=null are filtered out
    ///      from windowed queries.
    /// </summary>
    public class DiagQueryTurnFilterTests
    {
        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. SinceTurn narrows to records at-or-after
        // ====================================================================

        [Test]
        public void SinceTurnFilter_NarrowsToRecordsAtOrAfter()
        {
            // Construct entries with controlled Turn values via the
            // helper, which spins up a TurnManager pinned to each
            // turn before calling Diag.Record.
            MakeEntryWithTurn("event", "T5", 5);
            MakeEntryWithTurn("event", "T10", 10);
            MakeEntryWithTurn("event", "T15", 15);

            var result = DiagQuery.Apply(new DiagQuery.Filter { SinceTurn = 10 });
            var kinds = result.Records.Select(r => r.Kind).ToList();

            Assert.AreEqual(2, result.Records.Count,
                $"SinceTurn=10 must match Turn∈[10,∞). Got: [{string.Join(",", kinds)}]");
            Assert.IsTrue(kinds.Contains("T10") && kinds.Contains("T15"),
                $"Expected T10 and T15 in result. Got: [{string.Join(",", kinds)}]");
            Assert.IsFalse(kinds.Contains("T5"),
                $"T5 must be excluded (Turn=5 < SinceTurn=10). Got: [{string.Join(",", kinds)}]");
        }

        // ====================================================================
        // 2. UntilTurn narrows to records at-or-before
        // ====================================================================

        [Test]
        public void UntilTurnFilter_NarrowsToRecordsAtOrBefore()
        {
            MakeEntryWithTurn("event", "T5", 5);
            MakeEntryWithTurn("event", "T10", 10);
            MakeEntryWithTurn("event", "T15", 15);

            var result = DiagQuery.Apply(new DiagQuery.Filter { UntilTurn = 10 });
            var kinds = result.Records.Select(r => r.Kind).ToList();

            Assert.AreEqual(2, result.Records.Count,
                $"UntilTurn=10 must match Turn∈(-∞,10]. Got: [{string.Join(",", kinds)}]");
            Assert.IsTrue(kinds.Contains("T5") && kinds.Contains("T10"));
            Assert.IsFalse(kinds.Contains("T15"));
        }

        // ====================================================================
        // 3. Both bounds — intersection
        // ====================================================================

        [Test]
        public void BothBounds_ReturnsRecordsWithinWindow()
        {
            MakeEntryWithTurn("event", "T5", 5);
            MakeEntryWithTurn("event", "T10", 10);
            MakeEntryWithTurn("event", "T15", 15);

            var result = DiagQuery.Apply(new DiagQuery.Filter
            {
                SinceTurn = 8,
                UntilTurn = 12,
            });

            Assert.AreEqual(1, result.Records.Count,
                "Window [8,12] should contain exactly T10.");
            Assert.AreEqual("T10", result.Records[0].Kind);
        }

        // ====================================================================
        // 4. Counter-check: Turn=null records EXCLUDED from windowed queries
        // ====================================================================

        [Test]
        public void TurnNullRecords_ExcludedFromWindowedQueries()
        {
            // Diag.TryGetCurrentTurn returns null when TurnManager.Active
            // has CurrentActor=null (the worldgen / save / bootstrap path).
            // Pin that state explicitly so a previous test's TurnManager
            // doesn't leak its CurrentActor into our null-Turn record.
            ClearTurnManagerActor();

            Diag.Record("event", "TurnNull");
            // Sanity: confirm Turn is indeed null.
            var rec = Diag.Snapshot(10).First(r => r.Kind == "TurnNull");
            Assert.IsNull(rec.Turn, "Sanity: out-of-turn record must have Turn=null.");

            // Add a turn-stamped record to compare.
            MakeEntryWithTurn("event", "T10", 10);

            // Windowed queries must EXCLUDE the null-Turn record.
            var sinceResult = DiagQuery.Apply(new DiagQuery.Filter { SinceTurn = 0 });
            var sinceKinds = sinceResult.Records.Select(r => r.Kind).ToList();
            Assert.IsFalse(sinceKinds.Contains("TurnNull"),
                $"SinceTurn=0 must exclude Turn=null records (no turn to compare). " +
                $"Got: [{string.Join(",", sinceKinds)}]");
            Assert.IsTrue(sinceKinds.Contains("T10"),
                "Turn=10 record must still match SinceTurn=0.");

            // No filter at all → null-Turn record IS visible.
            var unfilteredResult = DiagQuery.Apply(null);
            var unfilteredKinds = unfilteredResult.Records.Select(r => r.Kind).ToList();
            Assert.IsTrue(unfilteredKinds.Contains("TurnNull"),
                "Without a turn filter, Turn=null records must still appear " +
                "(they only get excluded when a windowed query is asked).");
        }

        // ====================================================================
        // Bonus: Count helper also honors the turn-window filter
        // ====================================================================

        [Test]
        public void CountHelper_HonorsTurnWindowFilter()
        {
            MakeEntryWithTurn("event", "T5", 5);
            MakeEntryWithTurn("event", "T10", 10);
            MakeEntryWithTurn("event", "T15", 15);

            var result = DiagQuery.Count(new DiagQuery.Filter
            {
                SinceTurn = 10,
                UntilTurn = 15,
            });

            Assert.AreEqual(2, result.Count, "Window [10,15] should count T10 and T15.");
            Assert.AreEqual(3, result.TotalScanned, "TotalScanned reflects buffer size, not match count.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Records an entry with a controlled <see cref="Diag.Entry.Turn"/>
        /// value. Diag reads <c>TurnManager.Active.TickCount</c> when
        /// <c>CurrentActor</c> is non-null. Public setters are private,
        /// but <see cref="TurnManager.RestoreSavedState"/> exposes both
        /// fields for save/load support — we re-use it here as a clean
        /// way to pin a desired tick count.
        /// </summary>
        private static void MakeEntryWithTurn(string category, string kind, int turn)
        {
            // Ensure the channel is on (default-on for "event"; explicit for safety).
            Diag.SetChannel(category, true);

            var actor = new Entity { ID = "actor-for-turn-" + turn };
            var tm = new TurnManager();
            tm.RestoreSavedState(
                tickCount: turn,
                waitingForInput: false,
                currentActor: actor,
                entries: null);

            Diag.Record(category, kind);
        }

        /// <summary>
        /// Pins <c>TurnManager.Active.CurrentActor = null</c> so the next
        /// <c>Diag.Record</c> sees no current actor → Turn=null. Necessary
        /// because TurnManager.Active persists across tests unless reset,
        /// and a previous MakeEntryWithTurn leaves its CurrentActor set.
        /// </summary>
        private static void ClearTurnManagerActor()
        {
            var tm = new TurnManager();
            tm.RestoreSavedState(
                tickCount: 0,
                waitingForInput: false,
                currentActor: null,
                entries: null);
        }
    }
}
