using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP2.1 — ShatterArmorEffect contract tests + GetAV integration:
    ///   - OnApply / OnRemove (no stat modification — AV is computed)
    ///   - OnStack accumulates StackCount + extends Duration
    ///   - CombatSystem.GetAV reads ShatterArmorEffect and subtracts
    ///     AV_REDUCTION * StackCount, clamped to 0
    /// </summary>
    public class ShatterArmorEffectTests
    {
        [SetUp] public void Setup() => MessageLog.Clear();

        private static Entity MakeArmoredFighter(int avRating)
        {
            var e = new Entity { ID = "fighter" };
            e.AddPart(new RenderPart { DisplayName = "fighter" });
            e.AddPart(new StatusEffectsPart());
            // Synthetic natural-armor AV via a Body-less ArmorPart for test simplicity.
            e.AddPart(new ArmorPart { AV = avRating, DV = 0 });
            return e;
        }

        [Test]
        public void GetAV_WithNoShatter_ReturnsBaseArmor()
        {
            var target = MakeArmoredFighter(avRating: 6);
            Assert.AreEqual(6, CombatSystem.GetAV(target),
                "Without ShatterArmorEffect, GetAV should return the natural-armor AV.");
        }

        [Test]
        public void GetAV_WithSingleShatter_SubtractsReduction()
        {
            var target = MakeArmoredFighter(avRating: 6);
            target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            Assert.AreEqual(6 - ShatterArmorEffect.AV_REDUCTION,
                CombatSystem.GetAV(target),
                "With one ShatterArmorEffect stack, GetAV should subtract AV_REDUCTION (2).");
        }

        [Test]
        public void GetAV_WithMultipleShatterStacks_SubtractsScaled()
        {
            // Apply ShatterArmor twice — stack count becomes 2, reduction = 4.
            var target = MakeArmoredFighter(avRating: 6);
            target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            Assert.AreEqual(6 - 2 * ShatterArmorEffect.AV_REDUCTION,
                CombatSystem.GetAV(target),
                "Two stacks should subtract AV_REDUCTION * 2 (4 total).");
        }

        [Test]
        public void GetAV_ClampsToZero_NotNegative()
        {
            var target = MakeArmoredFighter(avRating: 1);
            // Apply many stacks — far more reduction than AV.
            for (int i = 0; i < 5; i++)
                target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            Assert.AreEqual(0, CombatSystem.GetAV(target),
                "GetAV must clamp to 0 — armor can be shattered to nothing but not negative.");
        }

        [Test]
        public void OnStack_ExtendsDuration_AndIncrementsCount()
        {
            var target = MakeArmoredFighter(avRating: 6);
            target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            var shatter = target.GetPart<StatusEffectsPart>().GetEffect<ShatterArmorEffect>();
            Assert.IsNotNull(shatter);
            Assert.AreEqual(8, shatter.Duration, "Duration should be 4 + 4 = 8 after stack.");
            Assert.AreEqual(2, shatter.StackCount, "StackCount should be 1 + 1 = 2.");
        }
    }
}
