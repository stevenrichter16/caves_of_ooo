using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class ItemUtility35GuidanceTests : FiftyWorldFixture
    {
        [TestCase("HealingTonic")]
        [TestCase("Antidote")]
        [TestCase("BurnSalve")]
        [TestCase("Panacea")]
        [TestCase("KnotflaxBandage")]
        [TestCase("SoddenFieldDressing")]
        [TestCase("SumpsievePad")]
        [TestCase("ClaspbeanPulp")]
        [TestCase("MargincressRibbon")]
        [TestCase("AbsentmintLeaf")]
        [TestCase("KnitmossPad")]
        [TestCase("SootrootPulp")]
        [TestCase("CookedMeat")]
        [TestCase("ToastedEmberwheat")]
        [TestCase("RoastedMushroom")]
        [TestCase("RoastedHearthbulb")]
        [TestCase("RoastedStarapple")]
        [TestCase("FieldMeal")]
        [TestCase("FireMoss")]
        [TestCase("FrostLichen")]
        [TestCase("GlacierSalt")]
        [TestCase("EmberFruit")]
        [TestCase("GlimmerBrine")]
        [TestCase("SparkRoot")]
        [TestCase("PrismreedPith")]
        [TestCase("LampOil")]
        [TestCase("SlipsedgeGel")]
        [TestCase("PitchpodResin")]
        [TestCase("Honeycomb")]
        [TestCase("Torch")]
        [TestCase("LampveinFan")]
        [TestCase("GroundwireScreen")]
        [TestCase("GripfrondWrap")]
        [TestCase("IronshodBoots")]
        [TestCase("GlowQuartz")]
        public void OrdinaryItemInspectionExplainsItsNewTacticalUse(string blueprint)
        {
            var item = Factory.CreateEntity(blueprint);
            Assert.NotNull(item);
            Assert.True(ItemExamineService.TryDescribeDetails(item, out var details));
            StringAssert.Contains("Tactical use:", details);
            StringAssert.Contains("action", details.ToLowerInvariant());
            StringAssert.Contains("Tactical use:", item.GetPart<ExaminablePart>().BuildExamineLine());
        }
        [Test] public void UnrelatedItemDoesNotAdvertiseAnInventedUtility()
        {
            var item = Factory.CreateEntity("Dagger");
            Assert.True(ItemExamineService.TryDescribeDetails(item, out var details));
            StringAssert.DoesNotContain("Tactical use:", details);
        }
        [TestCase("HealingTonic")]
        [TestCase("KnitmossPad")]
        public void HealingGuidanceRequiresTheCurrentHealingPayload(string blueprint)
        {
            var item = Factory.CreateEntity(blueprint);
            var tonic = item.GetPart<TonicPart>();
            Assert.NotNull(tonic);
            string healing = tonic.Healing;
            Assert.IsNotEmpty(healing);
            tonic.Healing = "";
            Assert.IsNull(ItemTacticalUseDescription.Describe(item), "A blueprint name is not a healing payload.");
            tonic.Healing = "   ";
            Assert.IsNull(ItemTacticalUseDescription.Describe(item));
            tonic.Healing = healing;
            StringAssert.Contains("healing payload", ItemTacticalUseDescription.Describe(item));
        }
        [Test] public void TacticalGuidancePreservesActualWeaponArmorAndTonicDetails()
        {
            var boots = Factory.CreateEntity("IronshodBoots");
            Assert.True(ItemExamineService.TryDescribeDetails(boots, out var armor));
            StringAssert.Contains("Speed: -5", armor); StringAssert.Contains("Tactical use:", armor);
            var healing = Factory.CreateEntity("HealingTonic");
            Assert.True(ItemExamineService.TryDescribeDetails(healing, out var medicine));
            StringAssert.Contains("4d6+4", medicine); StringAssert.Contains("Tactical use:", medicine);
        }

        [Test] public void FieldMealGuidancePreservesSharingAndSelfUseWithoutPromisingUnsupportedThrowing()
        {
            var meal = Carry("FieldMeal");
            Assert.False(HandlingService.IsThrowable(meal));
            Assert.False(HandlingService.CanThrow(Actor, meal, out var reason));
            StringAssert.Contains("cannot be thrown", reason);
            Assert.False(Actions(meal).Any(a => a.Command == "Throw"));
            Assert.AreEqual(1, Actions(meal).Count(a => a.Command == "Eat"));

            // A truthful hint must not be achieved by disabling actual throwing
            // or deleting the meal's useful companion/self-use description.
            var throwable = Carry("DriedMeat");
            Assert.True(HandlingService.CanThrow(Actor, throwable, out _));
            Assert.True(Actions(throwable).Any(a => a.Command == "Throw"));
            string hint = ItemTacticalUseDescription.Describe(meal);
            StringAssert.Contains("Share", hint);
            StringAssert.Contains("3d4", hint);
            StringAssert.Contains("one ordinary bleed", hint);
            StringAssert.Contains("no prepared-meal bonus", hint);
            StringAssert.Contains("Self-use", hint);
            StringAssert.DoesNotContain("throw", hint.ToLowerInvariant());
            Assert.True(ItemExamineService.TryDescribeDetails(meal, out var details));
            StringAssert.Contains(hint, details);
            StringAssert.DoesNotContain("existing throwing remain available", details);
            StringAssert.DoesNotContain("existing throwing remain available",
                meal.GetPart<ExaminablePart>().BuildExamineLine());
            Assert.Contains(meal, Pack.Objects);
        }

        [Test] public void FieldMealIsExplicitlyFoodWithoutAddingACompetingGenericEatPart()
        {
            var meal = Carry("FieldMeal");
            var custom = meal.GetPart<FieldMealPart>();
            Assert.NotNull(custom);
            Assert.AreEqual(1, meal.Parts.OfType<FieldMealPart>().Count());
            Assert.IsNull(meal.GetPart<FoodPart>(), "FoodItem inheritance would add a second Eat handler.");
            Assert.AreEqual(1, Actions(meal).Count(a => a.Command == "Eat"));
            meal.RemovePart(custom);
            Assert.False(Actions(meal).Any(a => a.Command == "Eat"),
                "The custom meal part must remain the sole source of its Eat action.");
            meal.AddPart(custom);
            Assert.AreEqual(1, Actions(meal).Count(a => a.Command == "Eat"));

            var ordinaryFood = Carry("ToastedEmberwheat");
            Assert.NotNull(ordinaryFood.GetPart<FoodPart>());
            Assert.AreEqual(ItemCategory.Food, ItemCategory.GetCategory(ordinaryFood));
            var blade = Carry("Dagger");
            Assert.AreEqual(ItemCategory.MeleeWeapons, ItemCategory.GetCategory(blade));

            Assert.AreEqual(ItemCategory.Food, meal.GetPart<PhysicsPart>().Category,
                "The authored meal must declare its display category without changing its eating mechanism.");
            Assert.AreEqual(ItemCategory.Food, ItemCategory.GetCategory(meal));
            var screen = InventoryScreenData.Build(Actor);
            var group = screen.Categories.Single(g => g.Items.Any(row => row.Item == meal));
            Assert.AreEqual(ItemCategory.Food, group.CategoryName);
            Assert.AreEqual(1, group.Items.Count(row => row.Item == meal));
            Assert.AreEqual(ItemCategory.Food, group.Items.Single(row => row.Item == meal).Category);
            Assert.Contains(meal, Pack.Objects);
        }
    }
}
