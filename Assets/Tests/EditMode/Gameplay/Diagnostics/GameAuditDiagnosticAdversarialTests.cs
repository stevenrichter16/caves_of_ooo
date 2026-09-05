using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Audit wave1: causal/filter intersections, retained/evicted history and adapter boundaries.</summary>
    public class GameAuditDiagnosticAdversarialTests
    {
        private TurnManager _previousTurns;
        private TurnManager _turns;
        private readonly Entity _actor = new Entity { ID = "audit-actor" };
        private readonly Entity _target = new Entity { ID = "audit-target" };

        [SetUp]
        public void SetUp()
        {
            _previousTurns = TurnManager.Active;
            _turns = new TurnManager();
            Diag.ResetAll();
        }

        [TearDown]
        public void TearDown()
        {
            Diag.ResetAll();
            typeof(TurnManager).GetProperty("Active", BindingFlags.Static | BindingFlags.Public)
                .SetValue(null, _previousTurns);
        }

        [TestCase("category", true)] [TestCase("category", false)]
        [TestCase("kind", true)] [TestCase("kind", false)]
        [TestCase("actor", true)] [TestCase("actor", false)]
        [TestCase("target", true)] [TestCase("target", false)]
        [TestCase("since", true)] [TestCase("since", false)]
        [TestCase("until", true)] [TestCase("until", false)]
        public void CauseIntersectsEveryFilter_WithoutConflatingActorAndTarget(string field, bool include)
        {
            Stamp(10);
            Diag.Record("event", "Match", _actor, _target, cause: "wanted");
            string wantedId = Diag.Snapshot(1)[0].TraceId;
            Diag.Record("event", "Match", _actor, _target, cause: "other");
            Stamp(field == "since" ? 5 : field == "until" ? 15 : 10);
            Diag.Record(field == "category" ? "damage" : "event",
                field == "kind" ? "Different" : "Match",
                field == "actor" ? _target : _actor,
                field == "target" ? _actor : _target, cause: "wanted");
            var filter = new DiagQuery.Filter { CauseTraceId = "wanted" };
            if (include)
            {
                if (field == "category") filter.Category = "event";
                if (field == "kind") filter.Kind = "Match";
                if (field == "actor") filter.Actor = _actor.ID;
                if (field == "target") filter.Target = _target.ID;
                if (field == "since") filter.SinceTurn = 10;
                if (field == "until") filter.UntilTurn = 10;
            }
            var query = DiagQuery.Apply(filter);
            var count = DiagQuery.Count(filter);
            Assert.AreEqual(include ? 1 : 2, query.Records.Count);
            Assert.AreEqual(query.Records.Count, count.Count);
            Assert.AreEqual(wantedId, count.SampleFirstTraceId);
            Assert.AreEqual(3, count.TotalScanned);
        }

        [TestCase("since")] [TestCase("until")] [TestCase("both")]
        public void CauseAndTurnWindow_ExcludeOutOfTurnRecords(string window)
        {
            Stamp(null);
            Diag.Record("event", "OutsideTurn", cause: "wanted");
            Stamp(10);
            Diag.Record("event", "InsideTurn", cause: "wanted");
            var filter = new DiagQuery.Filter { CauseTraceId = "wanted" };
            if (window != "until") filter.SinceTurn = 10;
            if (window != "since") filter.UntilTurn = 10;
            Assert.AreEqual("InsideTurn", DiagQuery.Apply(filter).Records[0].Kind);
            Assert.AreEqual(1, DiagQuery.Count(filter).Count);
            filter.SinceTurn = null;
            filter.UntilTurn = null;
            Assert.AreEqual(2, DiagQuery.Count(filter).Count);
        }

        [TestCase("DiagQueryTool")] [TestCase("DiagCountTool")] [TestCase("DiagAssertTool")]
        public void ActualAdapter_CauseTurnAndActorIntersection(string tool)
        {
            Stamp(10);
            Diag.Record("event", "Match", _actor, _target, cause: "wanted");
            Diag.Record("event", "Match", _target, _actor, cause: "wanted");
            Diag.Record("event", "Match", _actor, _target, cause: "other");
            Stamp(11);
            Diag.Record("event", "Match", _actor, _target, cause: "wanted");
            string json = "{\"cause_trace_id\":\"wanted\",\"actor\":\"audit-actor\",\"since_turn\":10,\"until_turn\":10}";
            Assert.AreEqual(1, DiagnosticAdapterProbe.Count(tool, DiagnosticAdapterProbe.Call(tool, json)));
            Assert.AreEqual(4, DiagnosticAdapterProbe.Count(tool, DiagnosticAdapterProbe.Call(tool, null)));
        }

        [TestCase("DiagQueryTool")] [TestCase("DiagCountTool")] [TestCase("DiagAssertTool")]
        public void EmptyCause_IsExactValueRatherThanOmitted(string tool)
        {
            Diag.Record("event", "Empty", cause: "");
            Diag.Record("event", "Other", cause: "other");
            Diag.Record("event", "Uncaused");
            Assert.AreEqual(1, DiagnosticAdapterProbe.Count(tool,
                DiagnosticAdapterProbe.Call(tool, "{\"cause_trace_id\":\"\"}")));
            Assert.AreEqual(3, DiagnosticAdapterProbe.Count(tool, DiagnosticAdapterProbe.Call(tool, "{}")));
        }

        [TestCase(1, 1)] [TestCase(500, 500)] [TestCase(0, 50)]
        [TestCase(-1, 50)] [TestCase(600, 500)]
        public void QueryLimits_DoNotCapCausalCount(int limit, int expected)
        {
            for (int i = 0; i < 600; i++) Diag.Record("event", "Match", cause: "wanted");
            Diag.Record("event", "Other", cause: "other");
            var filter = new DiagQuery.Filter { CauseTraceId = "wanted", Limit = limit };
            Assert.AreEqual(expected, DiagQuery.Apply(filter).Records.Count);
            Assert.AreEqual(600, DiagQuery.Count(filter).Count);
            Assert.AreEqual(601, DiagQuery.Count(filter).TotalScanned);
        }

        [Test]
        public void NestedCauseScope_RestoresOuterAndDoesNotLeakIntoUncausedRecords()
        {
            using (Diag.WithCause("outer"))
            {
                Diag.Record("event", "First");
                using (Diag.WithCause("inner")) Diag.Record("event", "Inner");
                Diag.Record("event", "Last");
            }
            Diag.Record("event", "NoCause");
            Assert.AreEqual(2, DiagQuery.Count(new DiagQuery.Filter { CauseTraceId = "outer" }).Count);
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { CauseTraceId = "inner" }).Count);
            Assert.IsNull(Diag.Snapshot(1)[0].CauseTraceId);
        }

        [Test]
        public void RingRotation_EvictsOnlyRecordsActuallyOverwritten()
        {
            Diag.Record("event", "Evicted", cause: "old");
            string oldId = Diag.Snapshot(1)[0].TraceId;
            Diag.Record("event", "Retained", cause: "keep");
            for (int i = 0; i < Diag.BufferCapacity - 1; i++) Diag.Record("event", "Later");
            Assert.AreEqual(1, Diag.DroppedCount);
            Assert.AreEqual(0, DiagQuery.Count(new DiagQuery.Filter { CauseTraceId = "old" }).Count);
            Assert.IsNull(DiagQuery.InspectRecord(oldId));
            Assert.AreEqual("Retained", DiagQuery.Apply(new DiagQuery.Filter { CauseTraceId = "keep" }).Records[0].Kind);
            Assert.AreEqual(Diag.BufferCapacity, DiagQuery.Count(null).Count);
        }

        [Test]
        public void ActualQueryAdapter_NarrowingAvoidsBudgetRefusalWithoutDiscardingMatchingRecords()
        {
            Diag.Record("event", "Small", cause: "small");
            Diag.Record("event", "Large", payload: new { text = new string('漢', 3000) }, cause: "large");
            var large = DiagnosticAdapterProbe.Call("DiagQueryTool", "{\"budget_kb\":1}");
            Assert.IsTrue((bool)DiagnosticAdapterProbe.Member(large, "truncated"));
            var small = DiagnosticAdapterProbe.Call("DiagQueryTool", "{\"budget_kb\":1,\"cause_trace_id\":\"small\"}");
            Assert.IsFalse((bool)DiagnosticAdapterProbe.Member(small, "truncated"));
            Assert.AreEqual(1, DiagnosticAdapterProbe.Count("DiagQueryTool", small));
            var expanded = DiagnosticAdapterProbe.Call("DiagQueryTool", "{\"budget_kb\":100}");
            Assert.AreEqual(2, DiagnosticAdapterProbe.Count("DiagQueryTool", expanded));
            Assert.AreEqual(2, DiagQuery.Count(null).Count, "Budget refusal must not consume the buffer.");
        }

        private void Stamp(int? tick) => _turns.RestoreSavedState(tick ?? 0, false,
            tick.HasValue ? _actor : null, null);
    }
}
