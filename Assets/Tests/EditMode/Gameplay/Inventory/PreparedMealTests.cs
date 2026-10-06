using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public abstract class PreparedMealFixture
    {
        protected EntityFactory factory;
        protected Entity actor;
        protected Zone zone;
        [OneTimeSetUp] public void Load()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void SetUp()
        {
            MessageLog.Clear();
            actor = new Entity { BlueprintName = "MealTester" };
            actor.AddPart(new InventoryPart()); actor.AddPart(new PhysicsPart());
            foreach (string stat in new[] { "Hitpoints", "Toughness", "DV", "AV", "HeatResistance", "ColdResistance", "AcidResistance" })
                actor.Statistics[stat] = new Stat { Name = stat, Owner = actor,
                    BaseValue = stat == "Hitpoints" ? 50 : stat == "Toughness" ? 16 : 0, Min = -100, Max = 100 };
            zone = new Zone("MealTest"); zone.AddEntity(actor, 10, 10);
        }
        protected Entity Eat(string blueprint)
        {
            var food = factory.CreateEntity(blueprint);
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(food));
            Assert.IsTrue(InventorySystem.PerformAction(actor, food, "Eat", zone));
            return food;
        }
    }

    public class PreparedMealTests : PreparedMealFixture
    {
        [TestCase("ToastedEmberwheat", "HeatResistance", 20)]
        [TestCase("RoastedHearthbulb", "ColdResistance", 20)]
        [TestCase("RoastedMushroom", "AcidResistance", 20)]
        [TestCase("CookedMeat", "Toughness", 2)]
        [TestCase("RoastedStarapple", "DV", 1)]
        public void PreparedDish_ConsumesOneAndProvidesDistinctPreparation(string blueprint, string stat, int bonus)
        {
            int before = actor.GetStatValue(stat);
            var food = factory.CreateEntity(blueprint);
            food.GetPart<StackerPart>().StackCount = 2;
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(food));
            Assert.IsTrue(InventorySystem.PerformAction(actor, food, "Eat", zone));
            Assert.AreEqual(1, food.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(before + bonus, actor.GetStatValue(stat));
        }
        [TestCase("Emberwheat", "HeatResistance")]
        [TestCase("Hearthbulb", "ColdResistance")]
        [TestCase("Mushroom", "AcidResistance")]
        [TestCase("RawMeat", "Toughness")]
        [TestCase("Starapple", "DV")]
        public void RawCounterpart_HealsWithoutPreparedProtection(string blueprint, string stat)
        {
            int before = actor.GetStatValue(stat);
            var food = factory.CreateEntity(blueprint);
            actor.GetPart<InventoryPart>().AddObject(food);
            Assert.IsTrue(InventorySystem.PerformAction(actor, food, "Eat", zone));
            Assert.AreEqual(before, actor.GetStatValue(stat));
            Assert.Greater(actor.GetStatValue("Hitpoints"), 50);
        }
        [TestCase("Emberwheat", "ToastedEmberwheat", "HeatResistance")]
        [TestCase("Hearthbulb", "RoastedHearthbulb", "ColdResistance")]
        [TestCase("Mushroom", "RoastedMushroom", "AcidResistance")]
        [TestCase("RawMeat", "CookedMeat", "Toughness")]
        [TestCase("Starapple", "RoastedStarapple", "DV")]
        public void ActualRawStack_CooksIntoMealBesideStation(string raw, string cooked, string stat)
        {
            var food = factory.CreateEntity(raw);
            actor.GetPart<InventoryPart>().AddObject(food);
            Assert.IsFalse(CookingService.TryCook(actor, food, zone, factory));
            var station = new Entity(); station.AddPart(new PhysicsPart()); station.AddPart(new CampfirePart());
            zone.AddEntity(station, 11, 10);
            Assert.IsTrue(CookingService.TryCook(actor, food, zone, factory));
            var result = actor.GetPart<InventoryPart>().Objects.Find(x => x.BlueprintName == cooked);
            Assert.IsNotNull(result);
            Assert.AreEqual(stat, result.GetPart<FoodPart>().MealStat);
            Assert.IsTrue(InventorySystem.PerformAction(actor, result, "Eat", zone));
            Assert.AreEqual(stat, actor.GetEffect<PreparedMealEffect>().StatName);
        }
        [Test]
        public void Inspection_UsesExactAuthoredAndRemainingFields()
        {
            var food = factory.CreateEntity("ToastedEmberwheat").GetPart<FoodPart>();
            StringAssert.Contains("+20 heat resistance", FoodPart.DescribeMeal(food));
            StringAssert.Contains("100", FoodPart.DescribeMeal(food));
            StringAssert.Contains("Replaces", FoodPart.DescribeMeal(food));
            Assert.AreEqual("", FoodPart.DescribeMeal(factory.CreateEntity("Emberwheat").GetPart<FoodPart>()));
            Eat("ToastedEmberwheat"); actor.GetEffect<PreparedMealEffect>().Duration = 23;
            StringAssert.Contains("23", EffectDescriber.Describe(actor.GetEffect<PreparedMealEffect>()));
        }
        [Test]
        public void NativeMealBench_ExercisesAllFiveAndPreservesItsCallersContext()
        {
            var previousTurns = TurnManager.Active;
            var previousWorld = TurnManager.World;
            var previousFactory = MaterialReactionResolver.Factory;
            // ScenarioContext requires a real caller clock, even when the bench
            // creates its own isolated clocks. Never depend on another fixture.
            var callerTurns = new TurnManager();
            try
            {
                var bench = new CavesOfOoo.Scenarios.Custom.MealPreparationBench();
                bench.Apply(new CavesOfOoo.Scenarios.ScenarioContext(zone, factory, actor, callerTurns));
                Assert.AreEqual(20, bench.Cases);
                Assert.AreEqual(0, bench.Failures, string.Join("\n", bench.Audit));
                Assert.AreSame(callerTurns, TurnManager.Active);
                Assert.AreSame(previousWorld, TurnManager.World);
                Assert.AreSame(previousFactory, MaterialReactionResolver.Factory);
                Assert.AreEqual(50, actor.GetStatValue("Hitpoints"));
                Assert.IsNull(actor.GetEffect<PreparedMealEffect>());
                Assert.AreSame(zone.GetCell(10, 10), zone.GetEntityCell(actor));
            }
            finally { typeof(TurnManager).GetProperty("Active").SetValue(null, previousTurns); }
        }
    }
}
