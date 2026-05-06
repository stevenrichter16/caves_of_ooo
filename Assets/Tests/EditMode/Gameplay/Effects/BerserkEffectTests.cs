using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP3.6 — BerserkEffect contract: +Strength bonus, -DV penalty
    /// while owned. OnStack refreshes duration (doesn't accumulate).
    /// </summary>
    public class BerserkEffectTests
    {
        [SetUp] public void Setup() => MessageLog.Clear();

        private static Entity MakeFighter()
        {
            var e = new Entity { ID = "fighter" };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = 10, Min = 1, Max = 30 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "fighter" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        [Test]
        public void Berserk_OnApply_AddsStrengthBonus_AndDVPenalty()
        {
            var target = MakeFighter();
            int strBefore = target.GetStatValue("Strength");
            int dvBefore = target.GetStatValue("DV");
            target.ApplyEffect(new BerserkEffect(5), source: null, zone: null);
            Assert.AreEqual(strBefore + BerserkEffect.STR_BONUS, target.GetStatValue("Strength"),
                "Berserk should add STR_BONUS to Strength.");
            Assert.AreEqual(dvBefore - BerserkEffect.DV_PENALTY, target.GetStatValue("DV"),
                "Berserk should subtract DV_PENALTY from DV.");
        }

        [Test]
        public void Berserk_OnRemove_RestoresStats()
        {
            var target = MakeFighter();
            int strBefore = target.GetStatValue("Strength");
            int dvBefore = target.GetStatValue("DV");
            var effect = new BerserkEffect(5);
            target.ApplyEffect(effect, source: null, zone: null);
            target.GetPart<StatusEffectsPart>().RemoveEffect(effect);
            Assert.AreEqual(strBefore, target.GetStatValue("Strength"));
            Assert.AreEqual(dvBefore, target.GetStatValue("DV"));
        }

        [Test]
        public void Berserk_OnStack_RefreshesDuration_NotStacks()
        {
            // Duration of 5 first, then 10 second. Result: max(5, 10) = 10.
            // Second (longer) refreshes; doesn't sum.
            var target = MakeFighter();
            target.ApplyEffect(new BerserkEffect(5), source: null, zone: null);
            target.ApplyEffect(new BerserkEffect(10), source: null, zone: null);
            var b = target.GetPart<StatusEffectsPart>().GetEffect<BerserkEffect>();
            Assert.AreEqual(10, b.Duration,
                "Re-applying Berserk while active should pick the longer duration, " +
                "not sum durations.");
            // Strength bonus should not double-stack either.
            Assert.AreEqual(10 + BerserkEffect.STR_BONUS, target.GetStatValue("Strength"),
                "Strength bonus should not double-stack on re-apply.");
        }
    }
}
