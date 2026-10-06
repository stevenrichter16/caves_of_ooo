using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Existing post-RED owner, save, rollback, contribution and payload counters.
    /// Extracted without duplicating cases from the primary food acceptance fixture.</summary>
    public class PreparedMealAdversarialTests : PreparedMealFixture
    {
        [Test]
        public void NextPreparedMeal_ReplacesRatherThanCombines_WhileRawFoodPreservesIt()
        {
            Eat("ToastedEmberwheat");
            Eat("Mushroom");
            Assert.AreEqual(20, actor.GetStatValue("HeatResistance"));
            Eat("RoastedHearthbulb");
            Assert.AreEqual(0, actor.GetStatValue("HeatResistance"));
            Assert.AreEqual(20, actor.GetStatValue("ColdResistance"));
            Assert.AreEqual("ColdResistance", actor.GetEffect<PreparedMealEffect>().StatName);
        }
        [Test]
        public void RepeatMeal_RefreshesOnce_ExpiresWithoutRemovingOtherBoosts()
        {
            actor.GetStat("DV").Boost = 3;
            Eat("RoastedStarapple");
            var meal = actor.GetEffect<PreparedMealEffect>();
            meal.OnTurnEnd(actor);
            Assert.AreEqual(99, meal.Duration);
            Eat("RoastedStarapple");
            Assert.AreSame(meal, actor.GetEffect<PreparedMealEffect>());
            Assert.AreEqual(100, meal.Duration);
            Assert.AreEqual(4, actor.GetStatValue("DV"));
            meal.JustApplied = false;
            for (int i = 0; i < 100; i++) actor.FireEventAndRelease(GameEvent.New("EndTurn"));
            Assert.IsNull(actor.GetEffect<PreparedMealEffect>());
            Assert.AreEqual(3, actor.GetStatValue("DV"));
        }
        [TestCase("ToastedEmberwheat", "HeatResistance", 20)]
        [TestCase("RoastedHearthbulb", "ColdResistance", 20)]
        [TestCase("RoastedMushroom", "AcidResistance", 20)]
        [TestCase("CookedMeat", "Toughness", 2)]
        [TestCase("RoastedStarapple", "DV", 1)]
        public void Meal_SaveRoundTrip_PreservesRemainingWindowAndRevertsExactly(string blueprint, string stat, int amount)
        {
            int before = actor.GetStatValue(stat);
            Eat(blueprint);
            actor.GetEffect<PreparedMealEffect>().Duration = 73;
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(before + amount, loaded.GetStatValue(stat));
            Assert.AreEqual(73, loaded.GetEffect<PreparedMealEffect>().Duration);
            Assert.AreEqual(amount, loaded.GetEffect<PreparedMealEffect>().AppliedBonus);
            loaded.RemoveEffect<PreparedMealEffect>();
            Assert.AreEqual(before, loaded.GetStatValue(stat));
            Assert.AreEqual(before + amount, actor.GetStatValue(stat));
        }
        [TestCase("ToastedEmberwheat")]
        [TestCase("RoastedHearthbulb")]
        [TestCase("RoastedMushroom")]
        [TestCase("CookedMeat")]
        [TestCase("RoastedStarapple")]
        public void GroundFood_CannotGrantProtection(string blueprint)
        {
            var food = factory.CreateEntity(blueprint);
            zone.AddEntity(food, 10, 10);
            Assert.IsFalse(InventorySystem.PerformAction(actor, food, "Eat", zone));
            Assert.IsNull(actor.GetEffect<PreparedMealEffect>());
            Assert.AreSame(zone.GetCell(10, 10), zone.GetEntityCell(food));
        }
        [TestCase("ToastedEmberwheat", "Heat")]
        [TestCase("RoastedHearthbulb", "Cold")]
        [TestCase("RoastedMushroom", "Acid")]
        public void ResistanceMeal_ChangesMatchingDamage_NotOrdinaryPhysical(string meal, string attribute)
        {
            Eat(meal);
            actor.GetStat("Hitpoints").BaseValue = 100;
            var elemental = new Damage(20); elemental.AddAttribute(attribute);
            CombatSystem.ApplyDamage(actor, elemental, null, zone);
            Assert.AreEqual(84, actor.GetStatValue("Hitpoints"));
            actor.GetStat("Hitpoints").BaseValue = 100;
            var physical = new Damage(20); physical.AddAttribute("Bludgeoning");
            CombatSystem.ApplyDamage(actor, physical, null, zone);
            Assert.AreEqual(80, actor.GetStatValue("Hitpoints"));
        }
        [Test]
        public void RealPlayer_MissingResistanceStat_GainsAndLosesOnlyMealContribution()
        {
            actor = factory.CreateEntity("Player"); zone.AddEntity(actor, 12, 12);
            int before = actor.GetStatValue("HeatResistance");
            Eat("ToastedEmberwheat");
            Assert.AreEqual(before + 20, actor.GetStatValue("HeatResistance"));
            actor.GetStat("HeatResistance").Boost += 7;
            actor.RemoveEffect<PreparedMealEffect>();
            Assert.AreEqual(before + 7, actor.GetStatValue("HeatResistance"));
        }
        [Test]
        public void EvasionMeal_ChangesActualCombatDV_NotAV()
        {
            int before = CombatSystem.GetDV(actor);
            int av = CombatSystem.GetAV(actor);
            Eat("RoastedStarapple");
            Assert.AreEqual(before + 1, CombatSystem.GetDV(actor));
            Assert.AreEqual(av, CombatSystem.GetAV(actor));
        }
        [Test]
        public void EnduranceMeal_ChangesToughnessModifier_AtFullHealth()
        {
            actor.GetStat("Hitpoints").BaseValue = 100;
            int modifier = StatUtils.GetModifier(actor, "Toughness");
            Eat("CookedMeat");
            Assert.AreEqual(modifier + 1, StatUtils.GetModifier(actor, "Toughness"));
            Assert.AreEqual(100, actor.GetStatValue("Hitpoints"));
            Assert.IsFalse(actor.GetEffect<PreparedMealEffect>().IsOfType(Effect.TYPE_NEGATIVE));
        }
        [Test]
        public void DeadActor_CannotConsumeMealOrGainBoost()
        {
            var food = factory.CreateEntity("CookedMeat");
            actor.GetPart<InventoryPart>().AddObject(food);
            actor.GetStat("Hitpoints").BaseValue = 0;
            Assert.IsFalse(InventorySystem.PerformAction(actor, food, "Eat", zone));
            Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(food));
            Assert.IsNull(actor.GetEffect<PreparedMealEffect>());
        }
        [Test]
        public void FailedOuterAction_RestoresFoodAndCannotLeakOrReplaceMeal()
        {
            Eat("ToastedEmberwheat");
            var food = factory.CreateEntity("RoastedStarapple");
            actor.GetPart<InventoryPart>().AddObject(food);
            var result = InventorySystem.ExecuteCommand(new RejectedMealCommand(food), actor, zone);
            Assert.IsFalse(result.Success);
            Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(food));
            Assert.AreEqual("HeatResistance", actor.GetEffect<PreparedMealEffect>().StatName);
            Assert.AreEqual(20, actor.GetStatValue("HeatResistance"));
            Assert.AreEqual(0, actor.GetStatValue("DV"));
            actor.RemoveEffect<PreparedMealEffect>();
            Assert.AreEqual(0, actor.GetStatValue("HeatResistance"));
            Assert.AreEqual(0, actor.GetStatValue("DV"));
        }
        sealed class RejectedMealCommand : IInventoryCommand
        {
            readonly Entity food;
            public string Name => "RejectMealAfterDispatch";
            public RejectedMealCommand(Entity item) { food = item; }
            public InventoryValidationResult Validate(InventoryContext context) => InventoryValidationResult.Valid();
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var result = new PerformInventoryActionCommand(food, "Eat").Execute(context, transaction);
                Assert.IsTrue(result.Success);
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Fixture declines the outer action.");
            }
        }
        [TestCase("MealStat")]
        [TestCase("MealBonus")]
        [TestCase("MealDuration")]
        [TestCase("Healing")]
        public void DifferentMealPayload_CannotDisappearIntoMatchingBlueprintStack(string changedField)
        {
            var original = factory.CreateEntity("ToastedEmberwheat");
            var different = factory.CreateEntity("ToastedEmberwheat");
            Assert.IsTrue(original.GetPart<StackerPart>().CanStackWith(different));
            var food = different.GetPart<FoodPart>();
            switch (changedField)
            {
                case "MealStat": food.MealStat = ""; break;
                case "MealBonus": food.MealBonus = 10; break;
                case "MealDuration": food.MealDuration = 50; break;
                case "Healing": food.Healing = "1d2"; break;
            }
            Assert.IsFalse(original.GetPart<StackerPart>().CanStackWith(different));
            Assert.IsFalse(different.GetPart<StackerPart>().CanStackWith(original));
        }
    }
}
