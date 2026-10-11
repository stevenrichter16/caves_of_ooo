using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ContainerCurrencyAdversarialTests : ContainerCurrencyFixture
    {
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MinValue)]
        public void NonpositiveSourceCannotMintCurrency(int count)
        {var coin=Coin();coin.GetPart<StackerPart>().StackCount=count;Assert.False(Take(coin).Success);Assert.AreEqual(100,TradeSystem.GetDrams(Actor));Assert.Contains(coin,Contents.Contents);Assert.AreEqual(count,coin.GetPart<StackerPart>().StackCount);}
        [TestCase(false)] [TestCase(true)] public void WalletBoundaryIsCheckedBeforeSourceRemoval(bool overflow)
        {var coin=Coin();int before=int.MaxValue-15+(overflow?1:0);TradeSystem.SetDrams(Actor,before);Assert.AreEqual(!overflow,Take(coin).Success);Assert.AreEqual(overflow?before:int.MaxValue,TradeSystem.GetDrams(Actor));Assert.AreEqual(overflow,Contents.Contents.Contains(coin));Assert.AreEqual(overflow?3:0,coin.GetPart<StackerPart>().StackCount);}
        [Test] public void MultiplicationCannotOverflowIntoSmallerPayment()
        {var coin=Coin();coin.GetPart<StackerPart>().StackCount=int.MaxValue;Assert.False(Take(coin).Success);Assert.AreEqual(100,TradeSystem.GetDrams(Actor));Assert.AreEqual(int.MaxValue,coin.GetPart<StackerPart>().StackCount);}
        [TestCase("legacy")] [TestCase("key")] public void EitherLockRefusesPayment(string type)
        {var coin=Coin();if(type=="legacy")Contents.Locked=true;else Box.AddPart(new LockPart{IsLocked=true});Assert.False(Take(coin).Success);Assert.Contains(coin,Contents.Contents);Assert.AreEqual(100,TradeSystem.GetDrams(Actor));}
        [TestCase("taken")] [TestCase("outer")] public void FailedTransactionRestoresExactSourceAndReleasesClaim(string failure)
        {var coin=Coin();var other=Factory.CreateEntity("Starapple");Assert.True(Contents.AddItem(other));var hook=new Hook{Action=e=>{if(failure=="taken")throw new InvalidOperationException("taken failure");}};coin.AddPart(hook);Assert.False(Take(coin,failure=="outer").Success);Assert.AreEqual(100,TradeSystem.GetDrams(Actor));Assert.AreSame(coin,Contents.Contents[0]);Assert.AreSame(other,Contents.Contents[1]);Assert.AreSame(Box,coin.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(3,coin.GetPart<StackerPart>().StackCount);hook.Action=e=>{};Assert.True(Take(coin).Success);Assert.AreEqual(115,TradeSystem.GetDrams(Actor));}
        [TestCase(false)] [TestCase(true)] public void IndependentCreditSurvivesOuterRollback(bool rollback)
        {var outer=Coin();var inner=Factory.CreateEntity("GoldCoin");Assert.True(Zone.AddEntity(inner,10,10));bool nested=false;outer.AddPart(new Hook{Action=e=>nested=InventorySystem.Pickup(Actor,inner,Zone)});Assert.AreEqual(!rollback,Take(outer,rollback).Success);Assert.True(nested);Assert.AreEqual(rollback?105:120,TradeSystem.GetDrams(Actor));Assert.AreEqual(rollback,Contents.Contents.Contains(outer));Assert.Null(Zone.GetEntityCell(inner));}
        [TestCase(1)] [TestCase(2)] public void IndependentCreditRechecksFinalCapacity(int innerCount)
        {var outer=Coin();TradeSystem.SetDrams(Actor,int.MaxValue-20);var inner=Factory.CreateEntity("GoldCoin");inner.GetPart<StackerPart>().StackCount=innerCount;Assert.True(Zone.AddEntity(inner,10,10));bool nested=false;outer.AddPart(new Hook{Action=e=>nested=InventorySystem.Pickup(Actor,inner,Zone)});Assert.AreEqual(innerCount==1,Take(outer).Success);Assert.True(nested);Assert.AreEqual(innerCount==1?int.MaxValue:int.MaxValue-10,TradeSystem.GetDrams(Actor));Assert.AreEqual(innerCount!=1,Contents.Contents.Contains(outer));}
        [Test] public void ReentrantSameSourceCannotDoubleCredit()
        {var coin=Coin();bool nested=true;coin.AddPart(new Hook{Action=e=>nested=Take(coin).Success});Assert.True(Take(coin).Success);Assert.False(nested);Assert.AreEqual(115,TradeSystem.GetDrams(Actor));}
        [TestCase("absent")] [TestCase("null")] [TestCase("not-takeable")] [TestCase("not-carryable")] [TestCase("too-strong")]
        public void InvalidSourceDoesNotChangePurse(string failure)
        {var coin=Coin();if(failure=="absent")Contents.RemoveItem(coin);if(failure=="not-takeable")coin.GetPart<PhysicsPart>().Takeable=false;if(failure=="not-carryable")coin.AddPart(new HandlingPart{Carryable=false});if(failure=="too-strong")coin.AddPart(new HandlingPart{MinLiftStrength=999});Assert.False(Take(failure=="null"?null:coin).Success);Assert.AreEqual(100,TradeSystem.GetDrams(Actor));Assert.AreEqual(3,coin.GetPart<StackerPart>().StackCount);}
        [TestCase("foreign-owner")] [TestCase("ground-alias")] [TestCase("equipped-alias")] [TestCase("duplicate-reference")] [TestCase("carried-alias")]
        public void AmbiguousContainerCoinOwnershipCannotMintCurrency(string change)
        {var coin=Coin();if(change=="foreign-owner")coin.GetPart<PhysicsPart>().InInventory=new Entity();if(change=="ground-alias")Assert.True(Zone.AddEntity(coin,10,10));if(change=="equipped-alias")coin.GetPart<PhysicsPart>().Equipped=Actor;if(change=="duplicate-reference")Contents.Contents.Add(coin);if(change=="carried-alias")Pack.Objects.Add(coin);Assert.False(Take(coin).Success);Assert.AreEqual(100,TradeSystem.GetDrams(Actor));Assert.AreEqual(3,coin.GetPart<StackerPart>().StackCount);Assert.Contains(coin,Contents.Contents);}
        [TestCase("quantity")] [TestCase("readded")] [TestCase("stack")] [TestCase("physics")] [TestCase("blueprint")]
        public void TakenMutationCannotCommitCurrencyWithAnUnspentOrReplacedSource(string change)
        {
            var coin=Coin();var originalStack=coin.GetPart<StackerPart>();var originalPhysics=coin.GetPart<PhysicsPart>();
            coin.AddPart(new Hook{Action=e=>{if(change=="quantity")originalStack.StackCount=3;if(change=="readded"){originalStack.StackCount=3;Assert.True(Contents.AddItem(coin));}if(change=="stack"){coin.RemovePart(originalStack);coin.AddPart(new StackerPart{StackCount=3});}if(change=="physics"){coin.RemovePart(originalPhysics);coin.AddPart(new PhysicsPart{Takeable=true});}if(change=="blueprint")coin.BlueprintName="OtherCoin";}});
            Assert.False(Take(coin).Success);Assert.AreEqual(100,TradeSystem.GetDrams(Actor));Assert.Contains(coin,Contents.Contents);Assert.False(Pack.Objects.Contains(coin));
            Assert.AreEqual(3,originalStack.StackCount,"restore the quantity this transaction actually changed");
        }
        [Test] public void SingletonCoinWithoutStackerCreditsOneUnit()
        {var coin=Coin();coin.RemovePart(coin.GetPart<StackerPart>());Assert.True(Take(coin).Success);Assert.AreEqual(105,TradeSystem.GetDrams(Actor));Assert.False(Take(coin).Success);Assert.AreEqual(105,TradeSystem.GetDrams(Actor));}
        [Test] public void DifferentBlueprintDoesNotConvertByInheritedParts()
        {var coin=Coin();coin.BlueprintName="CustomCoin";Assert.True(Take(coin).Success);Assert.AreEqual(100,TradeSystem.GetDrams(Actor));Assert.Contains(coin,Pack.Objects);Assert.AreEqual(3,coin.GetPart<StackerPart>().StackCount);}
        [Test] public void CurrencyObserverFailureCannotRefundSpentCoins()
        {var coin=Coin();Actor.AddPart(new CurrencyObserver());Assert.True(Take(coin).Success);Assert.AreEqual(115,TradeSystem.GetDrams(Actor));Assert.False(Contents.Contents.Contains(coin));Assert.AreEqual(0,coin.GetPart<StackerPart>().StackCount);}
        sealed class CurrencyObserver:Part {public override bool HandleEvent(GameEvent e){if(e.ID=="IntPropertyChanged"&&e.GetStringParameter("Name")==TradeSystem.CURRENCY_PROP)throw new InvalidOperationException("observer failure");return true;}}
    }
}
