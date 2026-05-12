using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.1.4 — <see cref="ItemEnhancing.Apply"/> helper
    /// contract. The user-facing API for adding an enhancement to an item.
    ///
    /// <para><b>Contract pinned here:</b></para>
    /// <list type="bullet">
    ///   <item>Looks up enhancement type via <see cref="EnhancementFactory"/></item>
    ///   <item>Instantiates + tiers it</item>
    ///   <item>Calls <see cref="IItemEnhancement.Applicable"/> as a veto</item>
    ///   <item>Enforces slot cap (Lockdown #6: <c>MAX_ENHANCEMENTS_PER_ITEM = 2</c>)</item>
    ///   <item>Attaches the Part + calls <see cref="IItemEnhancement.Apply"/></item>
    ///   <item>Emits <c>enhancement/Applied</c> or <c>enhancement/ApplyFailed</c></item>
    /// </list>
    ///
    /// <para><b>Slot enforcement</b> mirrors F.3.3's at_companion_limit
    /// pattern — veto when at cap, no destroy-item path (deferred to E.5+).</para>
    /// </summary>
    public class ItemEnhancingTests
    {
        [SetUp]
        public void Setup()
        {
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(StubA));
            EnhancementFactory.Register(typeof(StubB));
            EnhancementFactory.Register(typeof(StubC));
            EnhancementFactory.Register(typeof(StubRejecter));
            Diag.ResetAll();
        }

        // ── Stubs ─────────────────────────────────────────────────

        public class StubA : IItemEnhancement
        {
            public override string Name => nameof(StubA);
            public override string GetDisplayName() => "Stub A";
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
        public class StubRejecter : IItemEnhancement
        {
            public override string Name => nameof(StubRejecter);
            public override string GetDisplayName() => "Stub Rejecter";
            public override bool Applicable(Entity item) => false;
        }

        private static Entity MakeItem(string id = "weapon")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Takeable = true });
            return e;
        }

        private static int CountDiag(string kind)
        {
            return DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Kind = kind, Limit = 50,
            }).Records.Count;
        }

        // ── Apply: happy path ────────────────────────────────────

        [Test]
        public void Apply_Known_AddsPartAndFiresApplyHook()
        {
            var item = MakeItem();
            bool ok = ItemEnhancing.Apply(item, nameof(StubA), tier: 2);

            Assert.IsTrue(ok, "Apply returns true on success.");
            var part = item.GetPart<StubA>();
            Assert.IsNotNull(part, "Enhancement Part attached.");
            Assert.AreEqual(2, part.Tier, "Tier from Apply propagates to the instance.");
        }

        [Test]
        public void Apply_Known_EmitsAppliedDiag()
        {
            var item = MakeItem();
            ItemEnhancing.Apply(item, nameof(StubA), tier: 1);
            Assert.AreEqual(1, CountDiag("Applied"),
                "Successful Apply emits one enhancement/Applied diag.");
            Assert.AreEqual(0, CountDiag("ApplyFailed"));
        }

        // ── Apply: rejected paths ────────────────────────────────

        [Test]
        public void Apply_UnknownName_ReturnsFalse_DoesNotAttach()
        {
            var item = MakeItem();
            bool ok = ItemEnhancing.Apply(item, "NotRegistered");
            Assert.IsFalse(ok);
            Assert.AreEqual(0, CountDiag("Applied"));
            Assert.AreEqual(1, CountDiag("ApplyFailed"),
                "ApplyFailed diag emitted on unknown-name rejection.");
        }

        [Test]
        public void Apply_NullItem_ReturnsFalse()
        {
            bool ok = ItemEnhancing.Apply(null, nameof(StubA));
            Assert.IsFalse(ok);
        }

        [Test]
        public void Apply_NullName_ReturnsFalse()
        {
            var item = MakeItem();
            bool ok = ItemEnhancing.Apply(item, null);
            Assert.IsFalse(ok);
        }

        [Test]
        public void Apply_ApplicableRejects_AttachesNothing()
        {
            var item = MakeItem();
            bool ok = ItemEnhancing.Apply(item, nameof(StubRejecter));
            Assert.IsFalse(ok);
            Assert.IsNull(item.GetPart<StubRejecter>(),
                "Applicable=false → no Part attached.");
            Assert.AreEqual(1, CountDiag("ApplyFailed"));
        }

        // ── Slot cap (Lockdown #6: MAX_ENHANCEMENTS_PER_ITEM = 2) ──

        [Test]
        public void Apply_AtSlotCap_VetoesThird()
        {
            var item = MakeItem();
            Assert.IsTrue(ItemEnhancing.Apply(item, nameof(StubA)));
            Assert.IsTrue(ItemEnhancing.Apply(item, nameof(StubB)));
            // Third attempt should fail — cap is 2.
            bool ok = ItemEnhancing.Apply(item, nameof(StubC));
            Assert.IsFalse(ok, "Third enhancement vetoed (slot cap=2).");
            Assert.IsNull(item.GetPart<StubC>(),
                "Third enhancement Part not attached.");
        }

        [Test]
        public void Apply_AtSlotCap_EmitsApplyFailed_WithSlotCapReason()
        {
            var item = MakeItem();
            ItemEnhancing.Apply(item, nameof(StubA));
            ItemEnhancing.Apply(item, nameof(StubB));
            Diag.ResetAll();

            ItemEnhancing.Apply(item, nameof(StubC));

            Assert.AreEqual(1, CountDiag("ApplyFailed"),
                "Slot-cap veto emits ApplyFailed.");
            // Payload should include reason — verify via raw record.
            var rec = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "enhancement", Kind = "ApplyFailed", Limit = 10,
            }).Records[0];
            StringAssert.Contains("at_slot_cap", rec.PayloadJson,
                "Reason payload distinguishes slot-cap veto from other failures.");
        }

        [Test]
        public void Apply_BelowSlotCap_FirstAndSecondSucceed()
        {
            // Counter-pair to AtSlotCap: 1 and 2 enhancements should both
            // succeed; only the third is vetoed.
            var item = MakeItem();
            Assert.IsTrue(ItemEnhancing.Apply(item, nameof(StubA)));
            Assert.IsTrue(ItemEnhancing.Apply(item, nameof(StubB)));
            Assert.AreEqual(2, CountDiag("Applied"));
            Assert.AreEqual(0, CountDiag("ApplyFailed"));
        }

        // ── Remove ───────────────────────────────────────────────

        [Test]
        public void Remove_AttachedEnhancement_RemovesPartAndFiresRemove()
        {
            var item = MakeItem();
            ItemEnhancing.Apply(item, nameof(StubA));
            Assert.IsNotNull(item.GetPart<StubA>());
            Diag.ResetAll();

            bool ok = ItemEnhancing.Remove(item, nameof(StubA));

            Assert.IsTrue(ok);
            Assert.IsNull(item.GetPart<StubA>(),
                "Remove detaches the enhancement Part.");
            Assert.AreEqual(1, CountDiag("Removed"));
        }

        [Test]
        public void Remove_NotAttached_ReturnsFalse()
        {
            var item = MakeItem();
            bool ok = ItemEnhancing.Remove(item, nameof(StubA));
            Assert.IsFalse(ok,
                "Remove on item without that enhancement returns false.");
        }

        [Test]
        public void Remove_AfterCapReached_FreesSlot()
        {
            // Anti-exploit hygiene check (mirrors F.2 Persuasion_Dismiss
            // pattern): removing one enhancement frees a slot so a new
            // one can take its place.
            var item = MakeItem();
            ItemEnhancing.Apply(item, nameof(StubA));
            ItemEnhancing.Apply(item, nameof(StubB));
            Assert.IsFalse(ItemEnhancing.Apply(item, nameof(StubC)),
                "Precondition: cap is reached.");

            ItemEnhancing.Remove(item, nameof(StubA));

            Assert.IsTrue(ItemEnhancing.Apply(item, nameof(StubC)),
                "Slot freed by Remove → next Apply succeeds.");
        }
    }
}
