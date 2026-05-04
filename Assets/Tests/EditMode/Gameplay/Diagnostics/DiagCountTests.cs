using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D2.5 tests for the <see cref="DiagQuery.Count"/> aggregation
    /// helper (the engine that <c>DiagCountTool</c> wraps for MCP).
    ///
    /// Plan ref: <c>Docs/D2-HOOKS-PLAN.md</c> §4 D2.5.
    ///
    /// Tests target <see cref="DiagQuery.Count"/> directly — runtime-
    /// only, no Editor reference. The MCP envelope (SuccessResponse +
    /// tool_version field) is verified via live MCP round-trip during
    /// the D2.6 self-review step.
    ///
    /// Five invariants:
    ///   1. No filter → returns total record count.
    ///   2. Category filter narrows the count.
    ///   3. Zero matches → returns count=0 with null sample fields.
    ///   4. Sample trace-id / kind populated from the first match.
    ///   5. Counter-check: Count is uncapped (returns true count
    ///      even when matches > DiagQuery.MaxLimit). Distinguishes
    ///      Count from Apply (which IS capped).
    /// </summary>
    public class DiagCountTests
    {
        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. No filter → total count
        // ====================================================================

        [Test]
        public void Count_NoFilter_ReturnsTotalRecordCount()
        {
            for (int i = 0; i < 7; i++)
                Diag.Record("event", "Iter" + i);

            var result = DiagQuery.Count(null);
            Assert.AreEqual(7, result.Count, "Unfiltered Count must equal total record count.");
            Assert.AreEqual(7, result.TotalScanned, "TotalScanned must equal buffer count.");
        }

        // ====================================================================
        // 2. Category filter narrows
        // ====================================================================

        [Test]
        public void Count_CategoryFilter_NarrowsCount()
        {
            for (int i = 0; i < 4; i++)
                Diag.Record("event", "EventKind");
            Diag.SetChannel("damage", true);
            for (int i = 0; i < 2; i++)
                Diag.Record("damage", "DamageDealt");

            var dmgResult = DiagQuery.Count(new DiagQuery.Filter { Category = "damage" });
            Assert.AreEqual(2, dmgResult.Count, "Damage-filtered count must narrow to 2.");
            Assert.AreEqual(6, dmgResult.TotalScanned,
                "TotalScanned must include unmatched records (6 total in buffer).");

            var evtResult = DiagQuery.Count(new DiagQuery.Filter { Category = "event" });
            Assert.AreEqual(4, evtResult.Count);
        }

        // ====================================================================
        // 3. Zero matches
        // ====================================================================

        [Test]
        public void Count_ZeroMatches_ReturnsZeroAndNullSamples()
        {
            Diag.Record("event", "OnlyKind");

            var result = DiagQuery.Count(new DiagQuery.Filter { Category = "nonexistent" });
            Assert.AreEqual(0, result.Count);
            Assert.IsNull(result.SampleFirstTraceId,
                "SampleFirstTraceId must be null when zero matches.");
            Assert.IsNull(result.SampleFirstKind,
                "SampleFirstKind must be null when zero matches.");
            Assert.AreEqual(1, result.TotalScanned,
                "TotalScanned must still reflect what's in the buffer (1).");
        }

        // ====================================================================
        // 4. Sample fields populated from first match
        // ====================================================================

        [Test]
        public void Count_SampleFields_PopulatedFromFirstMatch()
        {
            Diag.Record("event", "First");
            Diag.Record("event", "Second");
            Diag.Record("event", "Third");

            var result = DiagQuery.Count(new DiagQuery.Filter { Category = "event" });
            Assert.AreEqual(3, result.Count);
            Assert.IsNotNull(result.SampleFirstTraceId,
                "SampleFirstTraceId must be the first match's TraceId.");
            Assert.AreEqual("First", result.SampleFirstKind,
                "SampleFirstKind must be the OLDEST matching record's Kind " +
                "(oldest-first ordering matches Diag.Snapshot).");
        }

        // ====================================================================
        // 5. Counter-check: Count is uncapped (distinguishes from Apply)
        // ====================================================================

        [Test]
        public void Count_IsUncappedWhenMatchesExceedApplyMaxLimit()
        {
            // Apply caps at 500. To prove Count is uncapped, we need
            // > 500 matching records. Buffer capacity is 1024. Write
            // 600 records, then verify Count returns 600 (not 500).
            const int N = 600;
            for (int i = 0; i < N; i++)
                Diag.Record("event", "Iter" + i);

            var countResult = DiagQuery.Count(new DiagQuery.Filter { Category = "event" });
            Assert.AreEqual(N, countResult.Count,
                $"Count must return the FULL match count ({N}), uncapped. " +
                "If this returns 500, Count is incorrectly applying the same " +
                "cap as Apply — defeating the whole point of having a separate " +
                "aggregation method.");

            // Verify Apply DOES cap (control: confirms 500 isn't a coincidence)
            var applyResult = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "event",
                Limit = 9999  // ask for everything; Apply still clamps to MaxLimit=500
            });
            Assert.AreEqual(500, applyResult.Records.Count,
                "Sanity: Apply DOES cap at MaxLimit=500. If this fails, the " +
                "Apply/Count distinction is not what this test thought.");
        }
    }
}
