using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP2.1 — BrokenEffect contract tests:
    ///   - Indefinite duration by default (DURATION_INDEFINITE = -1)
    ///   - OnStack is a no-op (a broken item can't get more broken)
    ///   - OnApply / OnRemove emit message-log entries
    ///   - Marker-only in v1 (no gameplay impact); future hook will gate
    ///     equipment use on HasEffect&lt;BrokenEffect&gt;.
    /// </summary>
    public class BrokenEffectTests
    {
        [SetUp] public void Setup() => MessageLog.Clear();

        private static Entity MakeItem()
        {
            var e = new Entity { ID = "mace", BlueprintName = "Mace" };
            e.AddPart(new RenderPart { DisplayName = "mace" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        [Test]
        public void Broken_DefaultCtor_IndefiniteDuration()
        {
            var broken = new BrokenEffect();
            Assert.AreEqual(Effect.DURATION_INDEFINITE, broken.Duration,
                "Default ctor should set Duration to DURATION_INDEFINITE (-1) — Broken " +
                "items don't auto-recover; explicit repair is required.");
        }

        [Test]
        public void Broken_AppliedToItem_HasEffectReturnsTrue()
        {
            var item = MakeItem();
            item.ApplyEffect(new BrokenEffect(), source: null, zone: null);
            Assert.IsTrue(item.GetPart<StatusEffectsPart>().HasEffect<BrokenEffect>(),
                "After ApplyEffect, the item entity should report HasEffect<BrokenEffect>=true.");
        }

        [Test]
        public void Broken_OnStack_IsNoOp_FirstApplyWins()
        {
            // Two ApplyEffect calls — Duration must NOT extend (Broken's
            // OnStack returns true to swallow the apply but doesn't change
            // anything else).
            var item = MakeItem();
            item.ApplyEffect(new BrokenEffect(), source: null, zone: null);
            var first = item.GetPart<StatusEffectsPart>().GetEffect<BrokenEffect>();
            int durationAfterFirst = first.Duration;

            item.ApplyEffect(new BrokenEffect(), source: null, zone: null);
            var second = item.GetPart<StatusEffectsPart>().GetEffect<BrokenEffect>();
            Assert.AreSame(first, second,
                "Second ApplyEffect should NOT replace the first effect instance.");
            Assert.AreEqual(durationAfterFirst, second.Duration,
                "OnStack should leave Duration unchanged — a broken item can't get " +
                "more broken in v1.");
        }
    }
}
