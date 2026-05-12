using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.1.5 — adversarial sweep for the E.1
    /// infrastructure layer.
    ///
    /// <para>E.1 hits these taxonomy surfaces from CLAUDE.md:</para>
    /// <list type="bullet">
    ///   <item>State atomicity — Apply attaches + fires; on partial failure, state unrolled</item>
    ///   <item>Diag dispatch — exactly one record per call (no double-emit, no zero-emit on failure)</item>
    ///   <item>Save/load reach — round-trip preserves Tier + fields (also pinned by E.1.2)</item>
    ///   <item>Anti-exploit — slot-cap can't be bypassed; Remove freeing slot can't double-apply</item>
    ///   <item>Stacking semantics — two enhancements of the same type? (Spec: vetoed via Applicable in concrete content, not at infra layer)</item>
    ///   <item>Null-safety — every public API survives nulls (also pinned by E.1.4)</item>
    ///   <item>Cross-system aggregation — multiple enhancements on same item all dispatch HandleEvent</item>
    /// </list>
    ///
    /// <para><b>Honesty bound:</b> 0 bugs found doesn't prove E.1 is
    /// bug-free. Real adversarial coverage comes in E.2-E.3 when
    /// concrete enhancements exercise the full event-hook surface.
    /// E.1's adversarial pass is regression infrastructure for the
    /// substrate contracts.</para>
    /// </summary>
    public class ItemEnhancementAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(StubA));
            EnhancementFactory.Register(typeof(StubB));
            EnhancementFactory.Register(typeof(StubC));
            EnhancementFactory.Register(typeof(StubD));
            Diag.ResetAll();
        }

        public class StubA : IItemEnhancement
        {
            public override string Name => nameof(StubA);
            public override string GetDisplayName() => "Stub A";
            public int ApplyCount;
            public int RemoveCount;
            public override void Apply(Entity item) { ApplyCount++; }
            public override void Remove(Entity item) { RemoveCount++; }
        }
        public class StubB : IItemEnhancement
        {
            public override string Name => nameof(StubB);
            public override string GetDisplayName() => "Stub B";
        }
        public class StubC : IItemEnhancement
        {
            public override string Name => nameof(StubC);
            public override string GetDisplayName() => "Stub C";
        }
        public class StubD : IItemEnhancement
        {
            public override string Name => nameof(StubD);
            public override string GetDisplayName() => "Stub D";
        }

        private static Entity MakeItem(string id = "weapon")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Takeable = true });
            return e;
        }

        private static int CountDiag(string kind, string reasonContains = null)
        {
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Kind = kind, Limit = 100,
            }).Records;
            if (reasonContains == null) return recs.Count;
            int n = 0;
            for (int i = 0; i < recs.Count; i++)
                if (recs[i].PayloadJson != null && recs[i].PayloadJson.Contains(reasonContains)) n++;
            return n;
        }

        // ════════════════════════════════════════════════════════════════
        // STATE ATOMICITY — Apply attaches AND fires; Remove detaches AND fires
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Apply_FiresEnhancementApplyOnce()
        {
            var item = MakeItem();
            ItemEnhancing.Apply(item, nameof(StubA));
            var attached = item.GetPart<StubA>();
            Assert.AreEqual(1, attached.ApplyCount,
                "Apply hook fires EXACTLY ONCE per ItemEnhancing.Apply call. " +
                "Mirrors F.2.2 RecruitedEffect lesson: a non-idempotent " +
                "hook firing twice would corrupt state.");
        }

        [Test]
        public void Adversarial_Remove_FiresEnhancementRemoveOnce()
        {
            var item = MakeItem();
            ItemEnhancing.Apply(item, nameof(StubA));
            var attached = item.GetPart<StubA>();
            ItemEnhancing.Remove(item, nameof(StubA));
            Assert.AreEqual(1, attached.RemoveCount,
                "Remove hook fires EXACTLY ONCE per ItemEnhancing.Remove call.");
        }

        [Test]
        public void Adversarial_ApplyRemoveApplyRemove_Cycle_NoOrphans()
        {
            // 100 apply/remove cycles → no orphan Parts, no orphan diag
            // records (each cycle = 1 Applied + 1 Removed).
            var item = MakeItem();
            for (int i = 0; i < 100; i++)
            {
                ItemEnhancing.Apply(item, nameof(StubA));
                ItemEnhancing.Remove(item, nameof(StubA));
            }
            Assert.AreEqual(0, ItemEnhancing.CountEnhancements(item),
                "After 100 apply/remove cycles, item has 0 enhancements " +
                "(no orphan Parts).");
            Assert.AreEqual(100, CountDiag("Applied"));
            Assert.AreEqual(100, CountDiag("Removed"));
        }

        // ════════════════════════════════════════════════════════════════
        // DIAG DISPATCH — exactly one record per call
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_EveryRejectionPath_EmitsExactlyOneApplyFailed()
        {
            // Each rejection reason fires exactly 1 ApplyFailed record.
            // Pin: null_item, null_name, unknown_enhancement, at_slot_cap.
            // (not_applicable is exercised by IMeleeEnhancement tests;
            // skipping here to keep this fixture infra-focused.)

            Diag.ResetAll();
            ItemEnhancing.Apply(null, nameof(StubA));
            Assert.AreEqual(1, CountDiag("ApplyFailed", "null_item"));

            Diag.ResetAll();
            ItemEnhancing.Apply(MakeItem(), null);
            Assert.AreEqual(1, CountDiag("ApplyFailed", "null_name"));

            Diag.ResetAll();
            ItemEnhancing.Apply(MakeItem(), "NotRegistered");
            Assert.AreEqual(1, CountDiag("ApplyFailed", "unknown_enhancement"));

            Diag.ResetAll();
            var capItem = MakeItem();
            ItemEnhancing.Apply(capItem, nameof(StubA));
            ItemEnhancing.Apply(capItem, nameof(StubB));
            Diag.ResetAll();
            ItemEnhancing.Apply(capItem, nameof(StubC));
            Assert.AreEqual(1, CountDiag("ApplyFailed", "at_slot_cap"));
        }

        [Test]
        public void Adversarial_SuccessPath_EmitsZeroApplyFailed()
        {
            var item = MakeItem();
            ItemEnhancing.Apply(item, nameof(StubA));
            Assert.AreEqual(1, CountDiag("Applied"));
            Assert.AreEqual(0, CountDiag("ApplyFailed"),
                "Success path emits zero ApplyFailed — invariant. If a future " +
                "change leaks ApplyFailed into the success branch, this test " +
                "breaks visibly.");
        }

        // ════════════════════════════════════════════════════════════════
        // ANTI-EXPLOIT — slot cap can't be bypassed via timing
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SlotCap_HoldsEvenWithMixedTypes()
        {
            // Three DIFFERENT enhancement types can't bypass the cap.
            var item = MakeItem();
            Assert.IsTrue(ItemEnhancing.Apply(item, nameof(StubA)));
            Assert.IsTrue(ItemEnhancing.Apply(item, nameof(StubB)));
            Assert.IsFalse(ItemEnhancing.Apply(item, nameof(StubC)));
            Assert.IsFalse(ItemEnhancing.Apply(item, nameof(StubD)));
            Assert.AreEqual(2, ItemEnhancing.CountEnhancements(item),
                "Cap is on ENHANCEMENT COUNT, not enhancement TYPE.");
        }

        [Test]
        public void Adversarial_SlotCap_NotByBypassedByRemoveThenApplyChain()
        {
            // Anti-exploit: removing + re-applying must respect the cap.
            // Two apply, one remove, one apply — final count = 2.
            var item = MakeItem();
            ItemEnhancing.Apply(item, nameof(StubA));
            ItemEnhancing.Apply(item, nameof(StubB));
            ItemEnhancing.Remove(item, nameof(StubA));
            ItemEnhancing.Apply(item, nameof(StubC));
            Assert.AreEqual(2, ItemEnhancing.CountEnhancements(item),
                "Cap holds after remove/re-apply churn.");
            // Trying a 3rd is still vetoed.
            Assert.IsFalse(ItemEnhancing.Apply(item, nameof(StubD)));
        }

        // ════════════════════════════════════════════════════════════════
        // SAVE/LOAD — multiple enhancements survive round-trip
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TwoEnhancements_RoundTrip_BothPreserved()
        {
            // E.1.2 already pins single-enhancement round-trip.
            // This adversarial extends: both enhancements survive AND
            // their Tier fields preserved independently.
            var item = MakeItem("rt-weapon");
            ItemEnhancing.Apply(item, nameof(StubA), tier: 2);
            ItemEnhancing.Apply(item, nameof(StubB), tier: 4);

            Entity loaded = PartRoundTripHelper.RoundTripEntity(item);

            Assert.IsNotNull(loaded.GetPart<StubA>());
            Assert.IsNotNull(loaded.GetPart<StubB>());
            Assert.AreEqual(2, loaded.GetPart<StubA>().Tier);
            Assert.AreEqual(4, loaded.GetPart<StubB>().Tier);
        }

        // ════════════════════════════════════════════════════════════════
        // FACTORY HYGIENE — Register/Reset across many entries
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ManyRegistrations_AllResolvable()
        {
            // Register N stubs (limited by what's in this fixture); all
            // resolve. Bulk registration is the F.5+ JSON-content path,
            // so this is the regression test for that flow.
            EnhancementFactory.ResetForTests();
            var types = new[] { typeof(StubA), typeof(StubB), typeof(StubC), typeof(StubD) };
            foreach (var t in types) EnhancementFactory.Register(t);
            foreach (var t in types)
                Assert.IsTrue(EnhancementFactory.TryGet(t.Name, out _),
                    $"Type {t.Name} resolves after bulk register.");
        }
    }
}
