using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed partial class FiniteCookingSourceTests
    {
        const string Toasted="ToastedEmberwheat";
        Entity Grain(int count)
        {
            var grain=factory.CreateEntity("Emberwheat");Assert.NotNull(grain);
            grain.GetPart<StackerPart>().StackCount=count;
            Assert.True(actor.GetPart<InventoryPart>().AddObject(grain));
            Assert.AreSame(actor,grain.GetPart<PhysicsPart>().InInventory);return grain;
        }
        [Test] public void GrainRecipeKeepsOneForOneFoodWeightAndDeliberateModestValue()
        {
            var grain=factory.CreateEntity("Emberwheat");Assert.NotNull(grain.GetPart<CookablePart>(),"Existing finite field yield needs the explicitly approved new recipe.");
            Assert.AreEqual(Toasted,grain.GetPart<CookablePart>().Into);
            var meal=factory.CreateEntity(Toasted);Assert.NotNull(meal,"Original prepared grain content is required.");
            Assert.AreEqual("2d4",grain.GetPart<FoodPart>().Healing);Assert.AreEqual("3d4",meal.GetPart<FoodPart>().Healing);
            Assert.AreEqual(10,grain.GetPart<CommercePart>().Value);Assert.AreEqual(12,meal.GetPart<CommercePart>().Value);
            Assert.AreEqual(1,grain.GetPart<PhysicsPart>().Weight);Assert.AreEqual(grain.GetPart<PhysicsPart>().Weight,meal.GetPart<PhysicsPart>().Weight);
            Assert.AreEqual(1,meal.GetPart<StackerPart>().StackCount);Assert.IsNull(meal.GetPart<CookablePart>(),"Prepared food must not inherit the raw recipe and toast repeatedly.");
        }
        [Test] public void ActualFiniteRowYieldCanToastWithoutIncreasingTheHarvestBudget()
        {
            var prior=HarvestablePart.Factory;HarvestablePart.Factory=factory;
            try
            {
                var row=factory.CreateEntity("RipeCropRow");Assert.True(zone.AddEntity(row,10,11));var harvest=row.GetPart<FieldHarvestPart>();
                Assert.AreEqual(1,harvest.YieldCount);Assert.AreEqual("Emberwheat",harvest.YieldBlueprint);
                Assert.True(InventorySystem.PerformAction(actor,row,"Harvest",zone));Assert.True(harvest.Harvested);Assert.AreEqual(1,Units("Emberwheat"));
                var grain=actor.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="Emberwheat");Station();
                Assert.True(InventorySystem.PerformAction(actor,grain,"Cook",zone));Assert.AreEqual(1,Units(Toasted));Assert.AreEqual(0,Units("Emberwheat"));
                Assert.False(InventorySystem.PerformAction(actor,row,"Harvest",zone));Assert.AreEqual(1,Units(Toasted));Assert.True(harvest.Harvested);Assert.AreEqual(1,harvest.YieldCount);
                Assert.AreSame(row,zone.GetCell(10,11).Objects.Single(e=>e==row));Assert.AreEqual(40,actor.GetStat("Hitpoints").Value,"Cooking is preparation, not immediate healing.");
            }
            finally{HarvestablePart.Factory=prior;}
        }
        [TestCase(1)][TestCase(3)] public void WholeGrainStackToastsAtExactCapacityAndCannotRepeat(int count)
        {
            var grain=Grain(count);var inv=actor.GetPart<InventoryPart>();inv.MaxWeight=count;Station();
            Assert.True(InventorySystem.GetActions(actor,grain).Any(a=>a.Command=="Cook"));
            Assert.True(InventorySystem.PerformAction(actor,grain,"Cook",zone));Assert.AreEqual(count,Units(Toasted));Assert.AreEqual(0,Units("Emberwheat"));
            Assert.AreEqual(count,inv.GetCarriedWeight());Assert.False(inv.Objects.Contains(grain));Assert.AreEqual(0,grain.GetPart<StackerPart>().StackCount);
            foreach(var meal in inv.Objects){Assert.AreNotEqual(grain.ID,meal.ID);Assert.AreSame(actor,meal.GetPart<PhysicsPart>().InInventory);Assert.IsNull(meal.GetPart<PhysicsPart>().Equipped);Assert.IsNull(zone.GetEntityCell(meal));Assert.False(InventorySystem.GetActions(actor,meal).Any(a=>a.Command=="Cook"));}
            Assert.False(InventorySystem.PerformAction(actor,grain,"Cook",zone));Assert.AreEqual(count,Units(Toasted));
        }
        [Test] public void MissingStationPreservesTheSameCarriedGrainUnits()
        {var grain=Grain(2);Assert.False(InventorySystem.PerformAction(actor,grain,"Cook",zone));Assert.AreEqual(2,Units("Emberwheat"));Assert.AreEqual(0,Units(Toasted));Assert.AreSame(actor,grain.GetPart<PhysicsPart>().InInventory);}
        [Test] public void PreparedGrainRemainsFoodAndConsumesOneWithTheAuthoredHealingRange()
        {
            Assert.True(factory.Blueprints.ContainsKey(Toasted));var meal=factory.CreateEntity(Toasted);meal.GetPart<StackerPart>().StackCount=2;Assert.True(actor.GetPart<InventoryPart>().AddObject(meal));actor.GetStat("Hitpoints").BaseValue=10;
            Assert.False(InventorySystem.PerformAction(actor,meal,"Cook",zone));Assert.AreEqual(2,Units(Toasted));
            Assert.True(InventorySystem.PerformAction(actor,meal,"Eat",zone));Assert.AreEqual(1,Units(Toasted));Assert.That(actor.GetStat("Hitpoints").Value,Is.InRange(13,22));
            Assert.AreSame(actor,meal.GetPart<PhysicsPart>().InInventory);
        }
        [Test] public void SavedPreparedStackRetainsExactIdentityQuantityAndNoRawRecipe()
        {
            var grain=Grain(3);Station();Assert.True(InventorySystem.PerformAction(actor,grain,"Cook",zone));var meal=actor.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName==Toasted);
            var manager=OverworldZoneManager.CreateDetached(factory,1);manager.ReplaceLoadedState(new Dictionary<string,Zone>{{zone.ZoneID,zone}},zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("toasted-grain","core-only",manager,null,actor));Assert.AreNotSame(actor,loaded.Player);
            var saved=loaded.Player.GetPart<InventoryPart>().Objects.Single();Assert.AreNotSame(meal,saved);Assert.AreEqual(meal.ID,saved.ID);Assert.AreEqual(Toasted,saved.BlueprintName);Assert.AreEqual(3,saved.GetPart<StackerPart>().StackCount);
            Assert.AreSame(loaded.Player,saved.GetPart<PhysicsPart>().InInventory);Assert.AreEqual("3d4",saved.GetPart<FoodPart>().Healing);Assert.AreEqual(12,saved.GetPart<CommercePart>().Value);Assert.IsNull(saved.GetPart<CookablePart>());
        }
    }
}
