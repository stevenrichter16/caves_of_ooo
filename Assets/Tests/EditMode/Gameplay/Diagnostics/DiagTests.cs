using System;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D1.1 substrate tests for the AI-debugging diag substrate.
    ///
    /// Plan ref: <c>Docs/D1-SPIKE-PLAN.md</c> §5 D1.1.
    /// Design contract: <c>Docs/AI-OBSERVABILITY.md</c>.
    ///
    /// Five core invariants (with two counter-checks):
    ///   1. Substrate accepts arbitrary new category strings without
    ///      code changes (P8 single hook surface, multi-flavored).
    ///   2. Records fired outside the turn loop carry Turn=null
    ///      (the schema generality fix from §11 third-pass).
    ///   3. Ring buffer overwrites oldest on overflow; dropped count
    ///      tracks the wrap.
    ///   4. Disabled channels do not record (P9 off-by-default for
    ///      chatty categories).
    ///   5. Payloads are eagerly serialized (§3 Layer 0 pin) so
    ///      mutating the payload object after Record() does NOT
    ///      affect the captured state.
    ///
    /// Counter-checks:
    ///   6. Enabled channel DOES record (verify #4 isn't vacuous).
    ///   7. Payload with circular reference doesn't blow up (R1
    ///      mitigation: ReferenceLoopHandling.Ignore + MaxDepth=4).
    /// </summary>
    public class DiagTests
    {
        [SetUp]
        public void SetUp()
        {
            // Static substrate state leaks between tests by default.
            Diag.ResetAll();
        }

        // ====================================================================
        // 1. Substrate accepts arbitrary categories without substrate changes
        // ====================================================================

        [Test]
        public void Diag_AcceptsArbitraryNewCategoryWithoutCodeChanges()
        {
            Diag.SetChannel("smoke_test_category", true);
            Diag.Record("smoke_test_category", "TestKind",
                payload: new { foo = "bar", count = 7 });

            var records = Diag.Snapshot(10);
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("smoke_test_category", records[0].Category);
            Assert.AreEqual("TestKind", records[0].Kind);
            Assert.IsTrue(records[0].PayloadJson.Contains("\"foo\""),
                "Payload field 'foo' must appear in serialized JSON");
            Assert.IsTrue(records[0].PayloadJson.Contains("\"bar\""),
                "Payload value 'bar' must appear in serialized JSON");
        }

        // ====================================================================
        // 2. Out-of-turn events carry Turn=null
        // ====================================================================

        [Test]
        public void Diag_AcceptsNullTurn_ForOutOfTurnEvents()
        {
            // Establish a fresh TurnManager so prior tests' state doesn't
            // leak. CurrentActor is null on a freshly-constructed
            // TurnManager (no one has been assigned a turn yet).
            new TurnManager();
            Assert.IsNull(TurnManager.Active.CurrentActor,
                "Sanity: fresh TurnManager must have no CurrentActor");

            Diag.SetChannel("worldgen", true);
            Diag.Record("worldgen", "TestPlace", payload: new { x = 5, y = 8 });

            var records = Diag.Snapshot(10);
            Assert.AreEqual(1, records.Count);
            Assert.IsNull(records[0].Turn,
                "Records fired with no CurrentActor must carry Turn=null. " +
                "Worldgen, save, bootstrap, UI events fire outside the turn loop.");
        }

        // ====================================================================
        // 3. Ring buffer wraps + tracks dropped count on overflow
        // ====================================================================

        [Test]
        public void Diag_RingBuffer_OverwritesOldestOnOverflow()
        {
            int capacity = Diag.BufferCapacity;
            int overflow = 76;

            for (int i = 0; i < capacity + overflow; i++)
                Diag.Record("event", "Index" + i);

            var records = Diag.Snapshot(capacity * 2);
            Assert.AreEqual(capacity, records.Count,
                "Snapshot must cap at buffer capacity even when more requested");
            Assert.AreEqual(overflow, Diag.DroppedCount,
                "DroppedCount must reflect records that overflowed");
            Assert.AreEqual("Index" + overflow, records[0].Kind,
                "After overflow, the oldest still-held record is the (capacity-overflow)+1 index");
            Assert.AreEqual("Index" + (capacity + overflow - 1), records[capacity - 1].Kind,
                "Newest record must be the last one written");
        }

        // ====================================================================
        // 4. Disabled channels are no-ops
        // ====================================================================

        [Test]
        public void Diag_DisabledChannel_DoesNotRecord()
        {
            Diag.SetChannel("foo", false);
            Diag.Record("foo", "Anything", payload: new { x = 1 });

            var records = Diag.Snapshot(10);
            Assert.AreEqual(0, records.Count,
                "Records to a disabled channel must be dropped silently (no buffer write, " +
                "no DroppedCount increment — just discarded).");
            Assert.AreEqual(0, Diag.DroppedCount,
                "Channel-disabled drops are NOT overflow drops; DroppedCount must stay 0");
        }

        // ====================================================================
        // 5. Payloads are eagerly serialized
        // ====================================================================

        [Test]
        public void Diag_PayloadIsEagerlySerialized()
        {
            // A mutable container we'll change AFTER the Record call.
            var payload = new MutableContainer { Value = "before" };
            Diag.SetChannel("event", true);
            Diag.Record("event", "MutateTest", payload: payload);
            payload.Value = "after";   // mutate AFTER the Record call

            var records = Diag.Snapshot(10);
            Assert.AreEqual(1, records.Count);
            Assert.IsTrue(records[0].PayloadJson.Contains("\"before\""),
                "Payload must be JSON-serialized synchronously inside Record(); " +
                "the captured snapshot must reflect the pre-mutation state");
            Assert.IsFalse(records[0].PayloadJson.Contains("\"after\""),
                "Mutating the payload object after Record() must NOT affect " +
                "the captured record (lazy serialization would be a bug — " +
                "see AI-OBSERVABILITY.md §3 Layer 0 'PayloadJson is eager.')");
        }

        // ====================================================================
        // 6. Counter-check: enabled channel actually records
        //    (verifies #4's precondition wasn't vacuous)
        // ====================================================================

        [Test]
        public void Diag_EnabledChannel_DoesRecord()
        {
            Diag.SetChannel("foo", true);
            Diag.Record("foo", "Anything", payload: new { x = 1 });

            var records = Diag.Snapshot(10);
            Assert.AreEqual(1, records.Count,
                "When the channel IS enabled, Record() must write to the buffer " +
                "(this is the counterpart to Diag_DisabledChannel_DoesNotRecord; " +
                "if THIS asserts fails, #4's assertion was vacuously true).");
            Assert.AreEqual("foo", records[0].Category);
        }

        // ====================================================================
        // 7. Counter-check: circular-reference payloads don't recurse forever
        //    (R1 mitigation in D1-SPIKE-PLAN.md §3)
        // ====================================================================

        [Test]
        public void Diag_PayloadWithCircularRef_DoesNotRecurse()
        {
            var loop = new SelfReferentialNode { Name = "root" };
            loop.Self = loop;   // direct cycle

            Diag.SetChannel("event", true);
            Diag.Record("event", "LoopTest", payload: loop);

            var records = Diag.Snapshot(10);
            Assert.AreEqual(1, records.Count,
                "Record must complete despite reference loop in payload " +
                "(JsonSerializerSettings.ReferenceLoopHandling = Ignore).");
            Assert.IsTrue(records[0].PayloadJson.Contains("\"root\""),
                "Captured payload must include the named field even when " +
                "the cycle was suppressed.");
        }

        // ====================================================================
        // Helper types
        // ====================================================================

        private class MutableContainer
        {
            public string Value;
        }

        private class SelfReferentialNode
        {
            public string Name;
            public SelfReferentialNode Self;
        }
    }
}
