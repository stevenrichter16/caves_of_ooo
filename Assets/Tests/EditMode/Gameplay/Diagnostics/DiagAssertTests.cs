using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D3.2 tests for the <c>diag_assert</c> MCP tool. The tool itself
    /// is a thin wrapper around <see cref="DiagQuery.Count"/> — these
    /// tests target Count's semantics in the assert-shaped use case
    /// (matched = Count > 0) since the runtime layer is what
    /// determines the answer; the Editor wrapper only computes the
    /// boolean from the count.
    ///
    /// Plan ref: <c>Docs/D3-TOOLS-PLAN.md</c> §4 D3.2.
    /// MCP envelope shape (matched, count, first_trace_id,
    /// first_kind, tool_version) is verified at D3.4 live MCP
    /// round-trip — the EditMode test asmdef can't see Editor refs.
    ///
    /// Five invariants:
    ///   1. Any matching record → matched=true, count≥1.
    ///   2. No matching record → matched=false, count=0,
    ///      sample fields null (counter-check).
    ///   3. SampleFirstTraceId populated from the FIRST match
    ///      (oldest, matching Diag.Snapshot order).
    ///   4. Turn-window filter (D3.1) honored on assert path.
    ///   5. Counter-check: ResetAll between calls cleans state
    ///      so an assert sees fresh records only.
    /// </summary>
    public class DiagAssertTests
    {
        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. Any matching record → matched=true
        // ====================================================================

        [Test]
        public void Assert_AnyRecordMatching_ReturnsTrue()
        {
            Diag.Record("event", "DidHappen");

            var result = DiagQuery.Count(new DiagQuery.Filter { Kind = "DidHappen" });
            bool matched = result.Count > 0;

            Assert.IsTrue(matched, "Expected matched=true when at least one record matches.");
            Assert.GreaterOrEqual(result.Count, 1);
        }

        // ====================================================================
        // 2. No matching record → matched=false (counter-check)
        // ====================================================================

        [Test]
        public void Assert_NoRecordMatching_ReturnsFalse()
        {
            Diag.Record("event", "Something");
            // No "DidNotHappen" record exists.

            var result = DiagQuery.Count(new DiagQuery.Filter { Kind = "DidNotHappen" });
            bool matched = result.Count > 0;

            Assert.IsFalse(matched);
            Assert.AreEqual(0, result.Count);
            Assert.IsNull(result.SampleFirstTraceId,
                "Sample fields must be null when zero matches.");
            Assert.IsNull(result.SampleFirstKind);
        }

        // ====================================================================
        // 3. SampleFirstTraceId / SampleFirstKind from FIRST match
        // ====================================================================

        [Test]
        public void Assert_FirstTraceIdMatchesFirstMatch()
        {
            Diag.Record("event", "First");
            Diag.Record("event", "Second");
            Diag.Record("event", "Third");

            var result = DiagQuery.Count(new DiagQuery.Filter { Category = "event" });
            Assert.IsTrue(result.Count > 0);
            Assert.IsNotNull(result.SampleFirstTraceId);
            Assert.AreEqual("First", result.SampleFirstKind,
                "SampleFirstKind must reflect the OLDEST matching record " +
                "(matches Diag.Snapshot oldest-first ordering).");
        }

        // ====================================================================
        // 4. Turn-window filter honored on assert path
        // ====================================================================

        [Test]
        public void Assert_TurnWindowFilter_Honored()
        {
            // Record at turns 5, 10, 15. Assert with window [8, 12]
            // should match only T10.
            MakeEntryWithTurn("event", "T5", 5);
            MakeEntryWithTurn("event", "T10", 10);
            MakeEntryWithTurn("event", "T15", 15);

            var insideWindow = DiagQuery.Count(new DiagQuery.Filter
            {
                Category = "event",
                SinceTurn = 8,
                UntilTurn = 12,
            });
            Assert.AreEqual(1, insideWindow.Count, "Window [8,12] should match only T10.");
            Assert.AreEqual("T10", insideWindow.SampleFirstKind);

            // Window outside any record → matched=false.
            var outsideWindow = DiagQuery.Count(new DiagQuery.Filter
            {
                Category = "event",
                SinceTurn = 20,
                UntilTurn = 30,
            });
            Assert.AreEqual(0, outsideWindow.Count);
            Assert.IsFalse(outsideWindow.Count > 0);
        }

        // ====================================================================
        // 5. Counter-check: ResetAll cleans state between asserts
        // ====================================================================

        [Test]
        public void Assert_ResetBufferBetweenCalls_FreshState()
        {
            Diag.Record("event", "Before");
            Assert.AreEqual(1, DiagQuery.Count(null).Count, "Sanity: 1 record before reset.");

            Diag.ResetAll();
            Assert.AreEqual(0, DiagQuery.Count(null).Count,
                "After ResetAll, the buffer must be empty — assertion would " +
                "incorrectly say matched=true otherwise.");

            Diag.Record("event", "After");
            Assert.AreEqual(1, DiagQuery.Count(null).Count, "Fresh record after reset.");
            var result = DiagQuery.Count(new DiagQuery.Filter { Kind = "After" });
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("After", result.SampleFirstKind);
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static void MakeEntryWithTurn(string category, string kind, int turn)
        {
            Diag.SetChannel(category, true);
            var actor = new Entity { ID = "actor-" + turn };
            var tm = new TurnManager();
            tm.RestoreSavedState(
                tickCount: turn,
                waitingForInput: false,
                currentActor: actor,
                entries: null);
            Diag.Record(category, kind);
        }
    }
}
