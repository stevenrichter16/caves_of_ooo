using System.Linq;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D1.3 tests for the <see cref="DiagQuery"/> runtime filter layer
    /// (the engine that <c>DiagQueryTool</c> wraps for MCP).
    ///
    /// Plan ref: <c>Docs/D1-SPIKE-PLAN.md</c> §5 D1.3.
    ///
    /// These tests target <see cref="DiagQuery.Apply"/> directly — no
    /// Editor assembly reference, no JSON round-trip. The MCP wrapper's
    /// budget-enforcement and meta-block branches are verified by the
    /// live D1.4 round-trip (manual, via <c>/tmp/mcp-call.sh</c>).
    /// Splitting the layers like this lets fast EditMode TDD cover the
    /// filter semantics, while the WebSocket/JSON shape is asserted
    /// end-to-end exactly once at the layer where it actually matters.
    ///
    /// Filter invariants:
    ///   1. No filters → all records (up to default limit).
    ///   2. category filter narrows.
    ///   3. kind filter narrows.
    ///   4. target filter narrows.
    ///   5. actor filter narrows.
    ///   6. limit param respected.
    ///   7. Records returned oldest-first (matches <c>Diag.Snapshot</c>
    ///      ordering — older records first, newer at the tail).
    ///   8. Counter-check: omitting category does NOT secretly hard-code
    ///      one (verifies filter logic isn't accidentally pinned to a
    ///      specific category).
    /// </summary>
    public class DiagQueryTests
    {
        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. No filters → returns all records
        // ====================================================================

        [Test]
        public void DiagQuery_NoFilters_ReturnsAllRecords()
        {
            for (int i = 0; i < 5; i++)
                Diag.Record("event", "Kind" + i);

            var result = DiagQuery.Apply(null);
            Assert.AreEqual(5, result.Records.Count,
                "With no filters and 5 records, default limit ≥ 5 must return all of them");
            Assert.AreEqual(5, result.TotalScanned,
                "TotalScanned must reflect every record in the buffer");
        }

        // ====================================================================
        // 2. Category filter narrows
        // ====================================================================

        [Test]
        public void DiagQuery_CategoryFilter_NarrowsResults()
        {
            Diag.Record("event", "A");
            Diag.SetChannel("custom_cat", true);
            Diag.Record("custom_cat", "B");
            Diag.Record("event", "C");

            var result = DiagQuery.Apply(new DiagQuery.Filter { Category = "custom_cat" });
            Assert.AreEqual(1, result.Records.Count);
            Assert.AreEqual("custom_cat", result.Records[0].Category);
            Assert.AreEqual("B", result.Records[0].Kind);
            Assert.AreEqual(3, result.TotalScanned,
                "TotalScanned must include records that didn't match the filter");
        }

        // ====================================================================
        // 3. Kind filter narrows
        // ====================================================================

        [Test]
        public void DiagQuery_KindFilter_NarrowsResults()
        {
            Diag.Record("event", "OnApply");
            Diag.Record("event", "OnRemove");
            Diag.Record("event", "OnApply");

            var result = DiagQuery.Apply(new DiagQuery.Filter { Kind = "OnRemove" });
            Assert.AreEqual(1, result.Records.Count);
            Assert.AreEqual("OnRemove", result.Records[0].Kind);
        }

        // ====================================================================
        // 4. Target filter narrows
        // ====================================================================

        [Test]
        public void DiagQuery_TargetFilter_NarrowsResults()
        {
            var ent1 = MakeEntity("ent-001");
            var ent2 = MakeEntity("ent-002");
            Diag.Record("event", "Hit", target: ent1);
            Diag.Record("event", "Hit", target: ent2);
            Diag.Record("event", "Hit", target: ent1);

            var result = DiagQuery.Apply(new DiagQuery.Filter { Target = "ent-001" });
            Assert.AreEqual(2, result.Records.Count, "Two records targeting ent-001 must be returned");
            foreach (var rec in result.Records)
                Assert.AreEqual("ent-001", rec.TargetId);
        }

        // ====================================================================
        // 5. Actor filter narrows (and is distinct from target)
        // ====================================================================

        [Test]
        public void DiagQuery_ActorFilter_NarrowsResults()
        {
            var atk = MakeEntity("attacker-001");
            var def = MakeEntity("defender-001");
            // Record with attacker-001 as actor.
            Diag.Record("event", "Strike", actor: atk, target: def);
            // Role-swap: attacker-001 now appears as target, NOT actor.
            Diag.Record("event", "Strike", actor: def, target: atk);

            var result = DiagQuery.Apply(new DiagQuery.Filter { Actor = "attacker-001" });
            Assert.AreEqual(1, result.Records.Count,
                "Actor=attacker-001 must match only the record where attacker is the actor, " +
                "not the one where attacker is the target. If both come back, the filter " +
                "is conflating ActorId and TargetId.");
            Assert.AreEqual("attacker-001", result.Records[0].ActorId);
        }

        // ====================================================================
        // 6. Limit param respected
        // ====================================================================

        [Test]
        public void DiagQuery_LimitParam_Respected()
        {
            for (int i = 0; i < 10; i++)
                Diag.Record("event", "Index" + i);

            var result = DiagQuery.Apply(new DiagQuery.Filter { Limit = 5 });
            Assert.AreEqual(5, result.Records.Count);
        }

        // ====================================================================
        // 7. Records returned oldest-first
        // ====================================================================

        [Test]
        public void DiagQuery_Records_ReturnedOldestFirst()
        {
            for (int i = 0; i < 4; i++)
                Diag.Record("event", "Index" + i);

            var result = DiagQuery.Apply(null);
            Assert.AreEqual(4, result.Records.Count);
            Assert.AreEqual("Index0", result.Records[0].Kind, "First record must be the oldest");
            Assert.AreEqual("Index3", result.Records[3].Kind, "Last record must be the newest");
        }

        // ====================================================================
        // 8. Counter-check: omitting category does NOT secretly hard-code one
        // ====================================================================

        [Test]
        public void DiagQuery_NoCategoryFilter_ReturnsMultipleCategories()
        {
            Diag.Record("event", "A");
            Diag.SetChannel("cat_b", true);
            Diag.Record("cat_b", "B");
            Diag.SetChannel("cat_c", true);
            Diag.Record("cat_c", "C");

            var result = DiagQuery.Apply(null);
            Assert.AreEqual(3, result.Records.Count,
                "With NO category filter, all 3 records (across 3 categories) must come back. " +
                "If only 1 returns, the tool has implicitly hard-coded a category filter — bug.");

            var categories = result.Records.Select(r => r.Category).Distinct().ToList();
            Assert.AreEqual(3, categories.Count,
                "Three distinct categories must be present in the result set " +
                $"(got: {string.Join(", ", categories)})");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static CavesOfOoo.Core.Entity MakeEntity(string id)
        {
            return new CavesOfOoo.Core.Entity { ID = id };
        }
    }
}
