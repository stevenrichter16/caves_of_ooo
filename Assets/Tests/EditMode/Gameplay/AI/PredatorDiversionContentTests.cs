using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class PredatorDiversionContentTests
    {
        HaulingContentScope scope;
        [SetUp] public void Setup() => scope = new HaulingContentScope();
        [TearDown] public void Cleanup() => scope.Dispose();

        [TestCase("RawMeat", "1d4", "Raw", 3)]
        [TestCase("DriedMeat", "3d4", "Meal", 7)]
        public void ActualMeatSupportsOrdinaryThrowAndRetainsItsFoodCost(string blueprint, string healing, string cooking, int value)
        {
            var meat = scope.Factory.CreateEntity(blueprint);
            Assert.True(HandlingService.IsThrowable(meat), "The ordinary throw picker must accept the real food.");
            Assert.True(HandlingService.IsCarryable(meat));
            Assert.AreEqual(GripType.OneHand, HandlingService.GetGripType(meat));
            Assert.AreEqual(2, HandlingService.GetWeight(meat));
            Assert.True(meat.GetPart<PhysicsPart>().Takeable);
            Assert.NotNull(meat.GetPart<StackerPart>());
            Assert.AreEqual(healing, meat.GetPart<FoodPart>().Healing);
            Assert.AreEqual(cooking, meat.GetPart<FoodPart>().Cooking);
            Assert.AreEqual(value, meat.GetPart<CommercePart>().Value);
            var actions = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions"); e.SetParameter("Actions", actions);
            meat.FireEventAndRelease(e);
            Assert.True(actions.Actions.Any(a => a.Command == "Throw"));
        }

        [TestCase("RawMeat")] [TestCase("DriedMeat")]
        public void MeatExamineExplainsPhysicalPlacementAndRisk(string blueprint)
        {
            var text = scope.Factory.CreateEntity(blueprint).GetPart<ExaminablePart>().BuildExamineLine().ToLowerInvariant();
            StringAssert.Contains("furrowstalker", text);
            StringAssert.Contains("throw", text);
            StringAssert.Contains("out of sight", text);
            StringAssert.Contains("once", text);
        }

        [TestCase("CookedMeat")] [TestCase("Mushroom")]
        public void UnrelatedFoodDoesNotAdvertiseOrAcquireTheNewMeatUse(string blueprint)
        {
            var food = scope.Factory.CreateEntity(blueprint);
            Assert.NotNull(food.GetPart<FoodPart>());
            Assert.False(HandlingService.IsThrowable(food));
            StringAssert.DoesNotContain("furrowstalker", food.GetPart<ExaminablePart>().BuildExamineLine().ToLowerInvariant());
        }
    }
}
