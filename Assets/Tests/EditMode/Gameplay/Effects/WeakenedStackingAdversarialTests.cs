using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Cross-effect and operation-sequence probes after the focused fix.
    /// Stat identity remains stable as in existing actor lifecycle; malformed
    /// arbitrary public-field edits are not a new save-migration contract.</summary>
    public sealed class WeakenedStackingAdversarialTests
    {
        WeakenedAuditPresentationScope presentation;
        [SetUp] public void Setup()=>presentation=new WeakenedAuditPresentationScope();
        [TearDown] public void Cleanup(){presentation?.Dispose();presentation=null;}
        static Entity Actor(string id="owner")
        {
            var e=new Entity{ID=id,BlueprintName="WeakenedAudit"};
            foreach(var name in new[]{"Strength","Agility","DV"})
                e.Statistics[name]=new Stat{Owner=e,Name=name,BaseValue=16,Min=1,Max=50};
            return e;
        }
        static void Tick(Entity e)
        {var end=GameEvent.New("EndTurn");try{e.FireEvent(end);}finally{end.Release();}}

        [TestCase(false)][TestCase(true)]
        public void ParchedAndBerserkContributionsSurviveWeakeningExpiry(bool save)
        {
            var actor=Actor();Assert.True(actor.ApplyEffect(new ParchedEffect()));Assert.True(actor.ApplyEffect(new ParchedEffect()));
            Assert.True(actor.ApplyEffect(new BerserkEffect(20)));Assert.True(actor.ApplyEffect(new WeakenedEffect(2,1)));Assert.True(actor.ApplyEffect(new WeakenedEffect(6,3)));
            if(save)actor=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(8,actor.GetStat("Strength").Penalty);Assert.AreEqual(5,actor.GetStat("Strength").Bonus);
            for(int n=0;n<3;n++)Tick(actor);
            Assert.IsNull(actor.GetEffect<WeakenedEffect>());Assert.AreEqual(2,actor.GetStat("Strength").Penalty);Assert.AreEqual(5,actor.GetStat("Strength").Bonus);
            actor.RemoveEffect<ParchedEffect>();Assert.Zero(actor.GetStat("Strength").Penalty);Assert.AreEqual(21,actor.GetStatValue("Strength"));
            actor.RemoveEffect<BerserkEffect>();Assert.AreEqual(16,actor.GetStatValue("Strength"));
        }
        [TestCase(false)][TestCase(true)]
        public void IndependentPenaltyAddedAfterStackSurvivesExplicitRemoval(bool save)
        {
            var actor=Actor();actor.ApplyEffect(new WeakenedEffect(2,4));actor.ApplyEffect(new WeakenedEffect(7,9));actor.GetStat("Strength").Penalty+=3;
            if(save)actor=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.True(actor.RemoveEffect<WeakenedEffect>());Assert.AreEqual(3,actor.GetStat("Strength").Penalty);
        }
        [TestCase(false)][TestCase(true)]
        public void ForcedAndOrdinaryReapplicationShareOneContribution(bool forced)
        {
            var actor=Actor();actor.ApplyEffect(new WeakenedEffect(2,4));var incoming=new WeakenedEffect(6,8);
            Assert.True(forced?actor.ForceApplyEffect(incoming):actor.ApplyEffect(incoming));Assert.AreEqual(6,actor.GetStat("Strength").Penalty);
            Assert.AreEqual(1,actor.GetPart<StatusEffectsPart>().EffectCount);Assert.IsNull(incoming.Owner);
        }
        [Test] public void RemovedInstanceCannotAffectAReplacementThroughNormalStatusApi()
        {
            var actor=Actor();var old=new WeakenedEffect(3,4);actor.ApplyEffect(old);Assert.True(actor.GetPart<StatusEffectsPart>().RemoveEffect(old));
            actor.ApplyEffect(new WeakenedEffect(2,4));Assert.False(actor.GetPart<StatusEffectsPart>().RemoveEffect(old));Assert.AreEqual(2,actor.GetStat("Strength").Penalty);
        }
        [Test] public void IndefiniteDurationSurvivesTicksAndRoundTripUntilExplicitRemoval()
        {
            var actor=Actor();actor.ApplyEffect(new WeakenedEffect(2,3));actor.ApplyEffect(new WeakenedEffect(5,Effect.DURATION_INDEFINITE));
            actor=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);for(int n=0;n<8;n++)Tick(actor);
            Assert.AreEqual(-1,actor.GetEffect<WeakenedEffect>().Duration);Assert.AreEqual(5,actor.GetStat("Strength").Penalty);actor.RemoveEffect<WeakenedEffect>();Assert.Zero(actor.GetStat("Strength").Penalty);
        }
        [TestCase(1)][TestCase(7)][TestCase(64)][TestCase(1729)][TestCase(4096)][TestCase(9001)]
        public void BoundedStackTickSaveSequenceMatchesIndependentContributionModel(int seed)
        {
            var rng=new Random(seed);var actor=Actor();int magnitude=0,duration=0,independent=0;
            for(int step=0;step<90;step++)
            {
                int op=rng.Next(5);
                if(op==0)
                {
                    int incoming=rng.Next(1,9),turns=rng.Next(1,8);Assert.True(actor.ApplyEffect(new WeakenedEffect(incoming,turns)));
                    magnitude=Math.Max(magnitude,incoming);duration=Math.Max(duration,turns);
                }
                else if(op==1)
                {Tick(actor);if(duration>0&&--duration==0)magnitude=0;}
                else if(op==2)
                {Assert.AreEqual(magnitude!=0,actor.RemoveEffect<WeakenedEffect>());magnitude=duration=0;}
                else if(op==3)actor=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
                else {independent++;actor.GetStat("Strength").Penalty++;}
                Assert.AreEqual(independent+magnitude,actor.GetStat("Strength").Penalty,"seed"+seed+" step"+step);
                Assert.AreEqual(duration,actor.GetEffect<WeakenedEffect>()?.Duration??0);
                Assert.AreEqual(magnitude==0?0:1,actor.GetPart<StatusEffectsPart>()?.GetAllEffects().Count(e=>e is WeakenedEffect)??0);
            }
            actor.RemoveEffect<WeakenedEffect>();Assert.AreEqual(independent,actor.GetStat("Strength").Penalty);
        }
    }
}
