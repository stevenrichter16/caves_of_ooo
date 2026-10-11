using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class BodyCoatingCleanupAdversarialTests : BodyCoatingCleanupFixture
    {
        [TestCase("water")] [TestCase("brine")] [TestCase("acid")] [TestCase("lava")]
        [TestCase("gel")] [TestCase("memory-bath")] [TestCase("missing-liquid")]
        public void OtherLiquidsNeverAdvertiseBodyCleanup(string liquid)
        {
            var pith = Carry("PrismreedPith"); var target = Friend(); var coat = Coat(target, liquid);
            Assert.False(Offered(pith)); Assert.AreEqual(35, coat.Amount); Assert.Contains(pith, Pack.Objects);
        }
        [TestCase("outsider")] [TestCase("hostile")] [TestCase("hidden")] [TestCase("invisible")]
        [TestCase("far")] [TestCase("dead")] [TestCase("empty")] [TestCase("expired")]
        public void IneligibleRecipientNeverLeaksAnAction(string reason)
        {
            var pith = Carry("PrismreedPith"); var target = Friend(); var coat = Coat(target);
            if (reason == "outsider") target.GetPart<BrainPart>().SetPartyLeader(null);
            if (reason == "hostile") target.GetPart<BrainPart>().SetPersonallyHostile(Actor);
            if (reason == "hidden") Zone.GetCell(11,10).IsVisible = false;
            if (reason == "invisible") target.GetPart<RenderPart>().Visible = false;
            if (reason == "far") { Zone.RemoveEntity(target); Zone.AddEntity(target, 14,10); }
            if (reason == "dead") target.GetStat("Hitpoints").BaseValue = 0;
            if (reason == "empty") coat.Amount = 0;
            if (reason == "expired") coat.Duration = 0;
            Assert.False(Offered(pith)); Assert.Contains(pith, Pack.Objects);
        }
        [TestCase("replacement")] [TestCase("amount")] [TestCase("liquid")]
        [TestCase("moved")] [TestCase("source-lost")] [TestCase("source-empty")]
        [TestCase("actor-moved")] [TestCase("actor-blocked")]
        public void APreviouslyValidSelectionRejectsChangedAuthority(string change)
        {
            var pith = Carry("PrismreedPith"); var target = Friend(); var coat = Coat(target); string command = Pick(pith,target);
            if (change == "replacement") { target.GetPart<StatusEffectsPart>().RemoveEffect(coat); coat = Coat(target); }
            if (change == "amount") coat.Amount++;
            if (change == "liquid") coat.LiquidId = "honey";
            if (change == "moved") { Zone.RemoveEntity(target); Zone.AddEntity(target,10,11); }
            if (change == "source-lost") Pack.RemoveObject(pith);
            if (change == "source-empty") { pith.AddPart(new StackerPart()); pith.GetPart<StackerPart>().StackCount = 0; }
            if (change == "actor-moved") { Zone.RemoveEntity(Actor); Zone.AddEntity(Actor,9,10); }
            if (change == "actor-blocked") Actor.ApplyEffect(new ParalyzedEffect());
            int amount = coat.Amount; Assert.False(Act(pith,command)); Assert.AreEqual(amount,coat.Amount);
            Assert.AreSame(coat,target.GetEffect<LiquidCoveredEffect>());
        }
        [TestCase("before-veto")] [TestCase("after-throw")] [TestCase("after-replace")]
        public void FailureBeforeCommitKeepsSupplyAndCannotCleanAReplacement(string hook)
        {
            var pith = Carry("PrismreedPith"); var target = Friend(); var coat = Coat(target); string command = Pick(pith,target);
            int called = 0;
            Actor.AddPart(new Hook(hook == "before-veto" ? "BeforeInventoryAction" : "AfterInventoryAction", e => {
                called++;
                if(hook == "before-veto") return false;
                if(hook == "after-throw") throw new InvalidOperationException("cleanup rollback probe");
                target.GetPart<StatusEffectsPart>().RemoveEffect(coat); coat = Coat(target); return true;
            }));
            Assert.False(Act(pith,command)); Assert.AreEqual(1,called); Assert.Contains(pith,Pack.Objects);
            Assert.AreEqual(35,coat.Amount); Assert.AreSame(coat,target.GetEffect<LiquidCoveredEffect>());
        }
        [Test] public void ReentrantSameSupplyCannotCleanTwice()
        {
            var pith = Carry("PrismreedPith"); pith.AddPart(new StackerPart()); pith.GetPart<StackerPart>().StackCount = 2;
            var coat = Coat(Actor); string command = Pick(pith,Actor); bool nested = true; int calls = 0;
            Actor.AddPart(new Hook("AfterInventoryAction", e => { if(calls++ == 0) nested = Act(pith,command); return true; }));
            Assert.True(Act(pith,command)); Assert.False(nested); Assert.AreEqual(15,coat.Amount); Assert.AreEqual(1,pith.GetPart<StackerPart>().StackCount);
        }
        [Test] public void PostCommitRemovalObserverCannotRefundTheSpentPith()
        {
            var pith = Carry("PrismreedPith"); var target = Friend(); Coat(target,"pitch",10); int calls = 0;
            target.AddPart(new Hook("EffectRemoved", e => { calls++; throw new InvalidOperationException("removed observer"); }));
            Assert.True(Act(pith,Pick(pith,target))); Assert.AreEqual(1,calls); Assert.False(Pack.Objects.Contains(pith)); Assert.False(target.HasEffect<LiquidCoveredEffect>());
        }
        [Test] public void AUsedSelectionAndMalformedCommandsNeverSpendAnotherUnit()
        {
            var pith = Carry("PrismreedPith"); pith.AddPart(new StackerPart()); pith.GetPart<StackerPart>().StackCount = 3;
            var coat = Coat(Actor); string command = Pick(pith,Actor);
            Assert.True(Act(pith,command)); Assert.False(Act(pith,command));
            foreach(var malformed in new[]{"WickBody|", "WickBody|%|NaN", command + "|extra"}) Assert.False(Act(pith,malformed));
            Assert.AreEqual(2,pith.GetPart<StackerPart>().StackCount); Assert.AreEqual(15,coat.Amount);
        }
        [Test] public void MenuQueriesDoNotSpendOrAlterTheCoatAndGroundWickingStillExists()
        {
            var pith = Carry("PrismreedPith"); var coat = Coat(Actor); Zone.TileState.WriteCoating(11,10,"oil",5);
            string first = Pick(pith,Actor); Assert.AreEqual(first,Pick(pith,Actor)); Assert.AreEqual(35,coat.Amount);
            Assert.Contains(pith,Pack.Objects); Assert.True(Actions(pith).Any(a=>a.Command.StartsWith("MaterialField|Wick|",StringComparison.Ordinal)));
        }
    }
}
