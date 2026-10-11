using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public abstract class SelfConsumptionTurnFixture : FiftyWorldFixture
    {
        protected Entity Food(string name="Starapple",int count=2){var item=Carry(name);if(item.GetPart<StackerPart>() is StackerPart stack)stack.StackCount=count;return item;}
        protected string Command(Entity item)=>item.HasPart<TonicPart>()?"ApplyTonic":"Eat";
        protected static int Units(InventoryPart pack,Entity item)=>pack.Objects.Contains(item)?item.GetPart<StackerPart>()?.StackCount??1:0;
    }
    public sealed class SelfConsumptionTurnTests : SelfConsumptionTurnFixture
    {
        [TestCase("Starapple",1)] [TestCase("Starapple",3)] [TestCase("ToastedEmberwheat",1)] [TestCase("ToastedEmberwheat",3)]
        [TestCase("HealingTonic",1)] [TestCase("HealingTonic",3)] [TestCase("FieldMeal",1)] [TestCase("FieldMeal",3)]
        public void OnlySuccessfulActualUnitConsumptionEarnsOneTurn(string blueprint,int count)
        {var item=Food(blueprint,count);var proof=SelfConsumptionTurnProof.Capture(Actor,item,Command(item));int before=Units(Pack,item);Assert.False(proof.ShouldSpendTurn(true),"opening/reading is free");bool success=Act(item,Command(item));Assert.True(success);Assert.AreEqual(before-1,Units(Pack,item));Assert.True(proof.ShouldSpendTurn(success));Assert.False(proof.ShouldSpendTurn(false),"an unsuccessful command never charges");}
        [Test] public void FoodAtFullHealthStillPaysForTheUnitActuallyConsumed()
        {Actor.GetStat("Hitpoints").BaseValue=Actor.GetStat("Hitpoints").Max;var item=Food();var proof=SelfConsumptionTurnProof.Capture(Actor,item,"Eat");Assert.True(Act(item,"Eat"));Assert.True(proof.ShouldSpendTurn(true));}
        [Test] public void RefundedOuterFailureCannotChargeForRestoredFood()
        {var item=Food();var proof=SelfConsumptionTurnProof.Capture(Actor,item,"Eat");FailAfter();Assert.False(Act(item,"Eat"));Assert.AreEqual(2,Units(Pack,item));Assert.False(proof.ShouldSpendTurn(false));Assert.False(proof.ShouldSpendTurn(true));}
    }
}
