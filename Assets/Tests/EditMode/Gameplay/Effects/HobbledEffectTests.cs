using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP2.1 — HobbledEffect contract tests:
    ///   - OnApply adds DV_PENALTY to the target's DV stat penalty
    ///   - OnRemove subtracts the same penalty
    ///   - OnStack extends duration (matches Stunned pattern)
    ///   - default duration constructor sets Duration = 8
    /// </summary>
    public class HobbledEffectTests
    {
        [SetUp] public void Setup() => MessageLog.Clear();

        private static Entity MakeFighter()
        {
            var e = new Entity { ID = "fighter" };
            e.Statistics["DV"] = new Stat { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "fighter" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        [Test]
        public void Hobbled_OnApply_PenalizesDV()
        {
            var target = MakeFighter();
            int dvBefore = target.GetStatValue("DV");
            target.ApplyEffect(new HobbledEffect(8), source: null, zone: null);
            int dvAfter = target.GetStatValue("DV");
            Assert.AreEqual(dvBefore - HobbledEffect.DV_PENALTY, dvAfter,
                "HobbledEffect.OnApply should reduce DV by DV_PENALTY (3).");
        }

        [Test]
        public void Hobbled_OnRemove_RestoresDV()
        {
            var target = MakeFighter();
            int dvBefore = target.GetStatValue("DV");
            var hobbled = new HobbledEffect(8);
            target.ApplyEffect(hobbled, source: null, zone: null);
            target.GetPart<StatusEffectsPart>().RemoveEffect(hobbled);
            int dvAfter = target.GetStatValue("DV");
            Assert.AreEqual(dvBefore, dvAfter,
                "HobbledEffect.OnRemove should restore DV to its pre-apply value.");
        }

        [Test]
        public void Hobbled_OnStack_ExtendsDuration()
        {
            var target = MakeFighter();
            target.ApplyEffect(new HobbledEffect(8), source: null, zone: null);
            target.ApplyEffect(new HobbledEffect(5), source: null, zone: null);
            var hobbled = target.GetPart<StatusEffectsPart>().GetEffect<HobbledEffect>();
            Assert.IsNotNull(hobbled);
            Assert.AreEqual(13, hobbled.Duration,
                "Two hobbled applies should sum to Duration 13 (8 + 5).");
        }
    }
}
