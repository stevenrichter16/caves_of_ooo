using System.Threading.Tasks;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D2.3 tests for the <see cref="Diag.WithCause"/> ambient cause scope.
    ///
    /// Plan ref: <c>Docs/D2-HOOKS-PLAN.md</c> §4 D2.3.
    ///
    /// Replaces D1.1's no-op stub with a real <see cref="System.Threading.AsyncLocal{T}"/>-
    /// backed scope. Every <c>Diag.Record(...)</c> inside the scope auto-fills its
    /// <c>CauseTraceId</c> field with the scope's trace-ID, unless an explicit
    /// <c>cause</c> argument overrides.
    ///
    /// Six invariants:
    ///   1. Records outside any scope have null CauseTraceId (control).
    ///   2. Records inside a scope pick up the scope's cause.
    ///   3. Explicit cause argument wins over the ambient scope.
    ///   4. Nested scopes: inner shadows outer; outer restored on inner dispose.
    ///   5. AsyncLocal contract: concurrent flows have isolated scopes.
    ///   6. Counter-check: scope dispose RESTORES the previous value (not
    ///      just clears it — proves the impl wraps the previous, not just
    ///      "set on enter, null on exit").
    /// </summary>
    public class DiagWithCauseTests
    {
        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. Control: outside any scope, no ambient cause
        // ====================================================================

        [Test]
        public void Record_OutsideScope_HasNullCause()
        {
            Assert.IsNull(Diag.CurrentCause, "Sanity: no scope active.");
            Diag.Record("event", "Outside");

            var rec = Diag.Snapshot(10)[0];
            Assert.IsNull(rec.CauseTraceId,
                "A record outside any WithCause scope must have CauseTraceId=null.");
        }

        // ====================================================================
        // 2. Inside scope: record picks up the scope's cause
        // ====================================================================

        [Test]
        public void Record_InsideScope_PicksUpScopeCause()
        {
            using (Diag.WithCause("trace-abc"))
            {
                Assert.AreEqual("trace-abc", Diag.CurrentCause,
                    "CurrentCause must reflect the active scope.");
                Diag.Record("event", "Inside");
            }

            var rec = Diag.Snapshot(10)[0];
            Assert.AreEqual("trace-abc", rec.CauseTraceId,
                "Record fired inside a WithCause scope must inherit the scope's trace-ID.");
        }

        // ====================================================================
        // 3. Explicit cause overrides ambient scope
        // ====================================================================

        [Test]
        public void ExplicitCauseParam_OverridesScopeCause()
        {
            using (Diag.WithCause("ambient-abc"))
            {
                Diag.Record("event", "Override", cause: "explicit-xyz");
            }

            var rec = Diag.Snapshot(10)[0];
            Assert.AreEqual("explicit-xyz", rec.CauseTraceId,
                "Explicit cause= argument must win over the ambient scope.");
        }

        // ====================================================================
        // 4. Nested scopes: inner wins, outer restored on dispose
        // ====================================================================

        [Test]
        public void NestedScope_InnerWinsAndOuterRestoredOnDispose()
        {
            using (Diag.WithCause("outer"))
            {
                Assert.AreEqual("outer", Diag.CurrentCause);

                Diag.Record("event", "OuterRecord1");

                using (Diag.WithCause("inner"))
                {
                    Assert.AreEqual("inner", Diag.CurrentCause,
                        "Inner scope must shadow outer.");
                    Diag.Record("event", "InnerRecord");
                }

                Assert.AreEqual("outer", Diag.CurrentCause,
                    "Outer scope must be restored after inner is disposed.");
                Diag.Record("event", "OuterRecord2");
            }

            Assert.IsNull(Diag.CurrentCause,
                "After all scopes dispose, CurrentCause must be null.");

            var records = Diag.Snapshot(10);
            Assert.AreEqual(3, records.Count);
            Assert.AreEqual("outer", records[0].CauseTraceId, "OuterRecord1 in outer scope.");
            Assert.AreEqual("inner", records[1].CauseTraceId, "InnerRecord in inner scope.");
            Assert.AreEqual("outer", records[2].CauseTraceId, "OuterRecord2 back in outer scope.");
        }

        // ====================================================================
        // 5. AsyncLocal contract: a child flow's scope does not leak
        //    back to the parent. (Diag's ring buffer is single-threaded
        //    by design — see Diag.Append docstring — so this test does
        //    not race two concurrent Diag.Record calls. It tests the
        //    isolation invariant via CurrentCause reads only.)
        // ====================================================================

        [Test]
        public void ScopeIsAsyncFlowIsolated()
        {
            Assert.IsNull(Diag.CurrentCause,
                "Sanity: test thread has no scope active before the spawn.");

            Task.Run(async () =>
            {
                using (Diag.WithCause("task-flow"))
                {
                    await Task.Yield();
                    Assert.AreEqual("task-flow", Diag.CurrentCause,
                        "Inside the spawned task, scope must be active.");
                }
            }).GetAwaiter().GetResult();

            // The whole point: the task's scope did not leak back to here.
            Assert.IsNull(Diag.CurrentCause,
                "After the spawned task completes, the test thread's " +
                "CurrentCause must STILL be null. AsyncLocal isolates flows: " +
                "the task's scope did not leak back. If this fails, the impl " +
                "is using ThreadStatic or a regular static field instead of " +
                "AsyncLocal — which would break under TPL.");
        }

        // ====================================================================
        // 6. Counter-check: dispose RESTORES previous, not just clears.
        //    Without this, "scope-restore" semantics could be a vacuous
        //    pass under test #4 if the impl always-clears on dispose.
        //    Here we set a manual value, enter+exit a scope, and verify
        //    the manual value is restored.
        // ====================================================================

        [Test]
        public void ScopeDisposed_RestoresPreviousValue_NotJustNull()
        {
            using (Diag.WithCause("set-by-outer"))
            {
                Assert.AreEqual("set-by-outer", Diag.CurrentCause);

                using (Diag.WithCause("inner-temp"))
                {
                    Assert.AreEqual("inner-temp", Diag.CurrentCause);
                }

                // Critical assertion: dispose RESTORED to "set-by-outer",
                // it didn't blow away the cause to null. If this fails,
                // CauseScope.Dispose is doing `_currentCause.Value = null`
                // unconditionally instead of `_currentCause.Value = _previous`.
                Assert.AreEqual("set-by-outer", Diag.CurrentCause,
                    "Inner scope dispose must restore the previous value " +
                    "('set-by-outer'), not null. If null, the scope " +
                    "implementation isn't capturing the previous value.");
            }
        }
    }
}
