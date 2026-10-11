using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SelfConsumptionTurnAdversarialTests : SelfConsumptionTurnFixture
    {
        [TestCase("Read")] [TestCase("StudySchematic")] [TestCase("Apply")] [TestCase("Drink")] [TestCase("eat")] [TestCase("")] [TestCase(null)]
        public void UnsupportedCommandsRemainFreeEvenIfSomethingRemovesTheUnit(string command)
        {var item=Food();var proof=SelfConsumptionTurnProof.Capture(Actor,item,command);Assert.True(Pack.TryConsumeOne(item));Assert.False(proof.ShouldSpendTurn(true));}
        [TestCase("null-actor")] [TestCase("null-item")] [TestCase("not-carried")] [TestCase("wrong-owner")] [TestCase("empty")]
        [TestCase("equipped")] [TestCase("ground-alias")] [TestCase("duplicate")]
        public void InvalidStartingOwnershipCannotAcquireAPaymentProof(string invalid)
        {var item=Food();if(invalid=="not-carried")Pack.RemoveObject(item);if(invalid=="wrong-owner")item.GetPart<PhysicsPart>().InInventory=new Entity();if(invalid=="empty")item.GetPart<StackerPart>().StackCount=0;if(invalid=="equipped")item.GetPart<PhysicsPart>().Equipped=Actor;if(invalid=="ground-alias")Assert.True(Zone.AddEntity(item,10,10));if(invalid=="duplicate")Pack.Objects.Add(item);var proof=SelfConsumptionTurnProof.Capture(invalid=="null-actor"?null:Actor,invalid=="null-item"?null:item,"Eat");if(Pack.CanConsumeOne(item))Pack.TryConsumeOne(item);Assert.False(proof.ShouldSpendTurn(true));}
        [TestCase("unchanged")] [TestCase("increased")] [TestCase("two-units")] [TestCase("replacement-pack")] [TestCase("replacement-stack")] [TestCase("replacement-physics")] [TestCase("foreign-owner")]
        public void AChangedSourceMustProveExactlyOneUnitFromTheOriginalOwner(string change)
        {var item=Food("Starapple",3);var proof=SelfConsumptionTurnProof.Capture(Actor,item,"Eat");if(change=="unchanged"){}else if(change=="increased")item.GetPart<StackerPart>().StackCount++;else {Assert.True(Pack.TryConsumeOne(item));if(change=="two-units")Assert.True(Pack.TryConsumeOne(item));if(change=="replacement-pack"){Actor.RemovePart(Pack);Actor.AddPart(new InventoryPart());}if(change=="replacement-stack"){item.RemovePart(item.GetPart<StackerPart>());item.AddPart(new StackerPart{StackCount=2});}if(change=="replacement-physics"){item.RemovePart(item.GetPart<PhysicsPart>());item.AddPart(new PhysicsPart{InInventory=Actor});}if(change=="foreign-owner")item.GetPart<PhysicsPart>().InInventory=new Entity();}Assert.False(proof.ShouldSpendTurn(true));}
        [TestCase("Eat")] [TestCase("ApplyTonic")]
        public void HandledWithoutConsumptionNeverCharges(string command)
        {var item=Food(command=="Eat"?"Starapple":"HealingTonic");var proof=SelfConsumptionTurnProof.Capture(Actor,item,command);Assert.False(proof.ShouldSpendTurn(true));Assert.AreEqual(2,Units(Pack,item));}
        [Test] public void TonicCommandRequiresATonicAndEatingRequiresFood()
        {var item=Food();var proof=SelfConsumptionTurnProof.Capture(Actor,item,"ApplyTonic");Assert.True(Pack.TryConsumeOne(item));Assert.False(proof.ShouldSpendTurn(true));var tonic=Food("HealingTonic");var eat=SelfConsumptionTurnProof.Capture(Actor,tonic,"Eat");Assert.True(Pack.TryConsumeOne(tonic));Assert.False(eat.ShouldSpendTurn(true));}
        [Test] public void SingletonWithNoStackerProvesItsActualRemoval()
        {var item=Food();item.RemovePart(item.GetPart<StackerPart>());var proof=SelfConsumptionTurnProof.Capture(Actor,item,"Eat");Assert.True(Act(item,"Eat"));Assert.True(proof.ShouldSpendTurn(true));}
        [Test] public void DefaultProofAndRepeatedReadNeverCharge()
        {var proof=default(SelfConsumptionTurnProof);Assert.False(proof.ShouldSpendTurn(true));var item=Food();for(int i=0;i<4;i++)Assert.False(SelfConsumptionTurnProof.Capture(Actor,item,"Eat").ShouldSpendTurn(true));Assert.AreEqual(2,Units(Pack,item));}
    }
}
