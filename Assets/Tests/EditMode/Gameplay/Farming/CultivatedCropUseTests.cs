using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Shipped harvest products must pay off through ordinary item commands,
    /// not merely advertise a Food/Reagent tag. These are content integration pins.</summary>
    public sealed class CultivatedCropUseTests : CultivatedCropTestBase
    {
        const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        EntityFactory previousCookingFactory;
        Dictionary<string, BrewRule> previousRules;
        List<BrewRule> previousRuleOrder;
        bool previousRulesInitialized;
        static Dictionary<string, BrewRule> Rules => (Dictionary<string, BrewRule>)typeof(BrewRuleRegistry)
            .GetField("RulesById", PrivateStatic).GetValue(null);
        static List<BrewRule> RuleOrder => (List<BrewRule>)typeof(BrewRuleRegistry)
            .GetField("RulesInOrder", PrivateStatic).GetValue(null);

        [SetUp] public void SetupUses()
        {
            previousCookingFactory = MaterialReactionResolver.Factory;
            MaterialReactionResolver.Factory = Factory;
            previousRules = new Dictionary<string, BrewRule>(Rules);
            previousRuleOrder = new List<BrewRule>(RuleOrder);
            previousRulesInitialized = (bool)typeof(BrewRuleRegistry).GetField("_initialized", PrivateStatic).GetValue(null);
            BrewRuleRegistry.InitializeFromJson(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Alchemy/BrewRules.json")));
        }

        [TearDown] public void CleanupUses()
        {
            MaterialReactionResolver.Factory = previousCookingFactory;
            Rules.Clear(); foreach (var row in previousRules) Rules.Add(row.Key, row.Value);
            RuleOrder.Clear(); RuleOrder.AddRange(previousRuleOrder);
            typeof(BrewRuleRegistry).GetField("_initialized", PrivateStatic).SetValue(null, previousRulesInitialized);
        }

        Entity Give(string blueprint, int count = 1)
        {
            var inventory = Actor.GetPart<InventoryPart>();
            for (int i = 0; i < count; i++) Assert.True(inventory.AddObject(Factory.CreateEntity(blueprint)));
            return inventory.Objects.Single(e => e.BlueprintName == blueprint);
        }
        int Units(string blueprint) => Units(Actor, blueprint);
        static int Units(Entity actor, string blueprint) => actor.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        InventoryCommandResult Use(Entity item, string command) => InventorySystem.ExecuteCommand(
            new PerformInventoryActionCommand(item, command), Actor, Zone);
        InventoryCommandResult Brew(Entity item, int count = 1) => InventorySystem.ExecuteCommand(
            new BrewReagentsCommand(new[] { item }, Factory, count), Actor, Zone);

        [Test] public void HearthbulbCooksAsAWholeStack_ThenEatingOneRoastedBulbHeals()
        {
            var raw = Give("Hearthbulb", 2);
            Assert.True(Zone.AddEntity(Factory.CreateEntity("SpreadCookingCoals"), 6, 5));
            Assert.True(Use(raw, "Cook").Success);
            Assert.Zero(Units("Hearthbulb")); Assert.AreEqual(2, Units("RoastedHearthbulb"));
            var cooked = Actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "RoastedHearthbulb");
            Assert.AreSame(Actor, cooked.GetPart<PhysicsPart>().InInventory);
            Actor.GetStat("Hitpoints").BaseValue = 10;
            Assert.True(Use(cooked, "Eat").Success);
            Assert.That(Actor.GetStatValue("Hitpoints"), Is.InRange(12, 18));
            Assert.AreEqual(1, Units("RoastedHearthbulb"));
        }

        [Test] public void HearthbulbWithoutCookingStationKeepsTheExactRawStack()
        {
            var raw = Give("Hearthbulb", 2);
            Assert.False(Use(raw, "Cook").Success);
            Assert.AreEqual(2, Units("Hearthbulb")); Assert.Zero(Units("RoastedHearthbulb"));
            Assert.AreSame(raw, Actor.GetPart<InventoryPart>().Objects.Single());
            Assert.AreSame(Actor, raw.GetPart<PhysicsPart>().InInventory);
        }

        [Test] public void SeamleafResolvesThroughShippedMendingRule_AndItsActualBrewHeals()
        {
            var sprig = Give("SeamleafSprig");
            var preview = BrewResolver.Resolve(sprig.GetPart<ReagentPart>().GetProperties());
            Assert.AreEqual(BrewOutcomeKind.Brew, preview.Kind); Assert.AreEqual("Tonic", preview.Form);
            Assert.AreEqual("brew_mending", preview.Effects.Single().RuleId);
            Assert.True(Brew(sprig).Success, "A single flask can be brewed in the field.");
            Assert.Zero(Units("SeamleafSprig")); Assert.AreEqual(1, Units("BrewedTonic"));
            var tonic = Actor.GetPart<InventoryPart>().Objects.Single();
            Actor.GetStat("Hitpoints").BaseValue = 10;
            Assert.True(Use(tonic, "ApplyTonic").Success);
            Assert.That(Actor.GetStatValue("Hitpoints"), Is.InRange(12, 18));
            Assert.Zero(Units("BrewedTonic")); Assert.IsEmpty(Actor.GetPart<InventoryPart>().Objects);
        }

        [Test] public void UnsupportedRawBulbCannotBeBrewed_AndNoSuppliesAreSpent()
        {
            var raw = Give("Hearthbulb", 2); var sprig = Give("SeamleafSprig");
            var before = Actor.GetPart<InventoryPart>().Objects.ToArray();
            var command = new BrewReagentsCommand(new[] { raw, sprig }, Factory);
            Assert.False(InventorySystem.ExecuteCommand(command, Actor, Zone).Success);
            CollectionAssert.AreEqual(before, Actor.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(2, Units("Hearthbulb")); Assert.AreEqual(1, Units("SeamleafSprig"));
            Assert.Zero(Units("BrewedTonic")); Assert.Zero(Units("InertSludge"));
        }

        [Test] public void SeamleafBatchNeedsARealAdjacentStill_RefusalPreservesBothSprigs()
        {
            var sprig = Give("SeamleafSprig", 2);
            Assert.False(Brew(sprig, 2).Success);
            Assert.AreEqual(2, Units("SeamleafSprig")); Assert.Zero(Units("BrewedTonic"));
            Assert.AreSame(sprig, Actor.GetPart<InventoryPart>().Objects.Single());
            Assert.True(Zone.AddEntity(Factory.CreateEntity("AlchemyStill"), 6, 5));
            Assert.True(Brew(sprig, 2).Success);
            Assert.Zero(Units("SeamleafSprig")); Assert.AreEqual(2, Units("BrewedTonic"));
        }

        [Test] public void ActualTownProvisionerAndSeedKeeperOpenWithAllThreeNewSeedSpecies()
        {
            using (var stock = new HaulingContentScope())
            {
                stock.Seed(64);
                var town = MorrowfastContent.CreateResident("southern-food-vendor", stock.Factory);
                var keeper = stock.Factory.CreateEntity("SpreadSeedKeeper");
                Assert.AreEqual("MorrowfastProvisionerStock", town.GetPart<TraderPart>().StockTable);
                Assert.AreEqual("SeedKeeperStock", keeper.GetPart<TraderPart>().StockTable);
                foreach (string species in new[] { "Knotflax", "Hearthbulb", "Seamleaf" })
                {
                    Assert.AreEqual(2, Units(town, species + "Seed"), "No double opening-stock application.");
                    Assert.AreEqual(1, Units(keeper, species + "Seed"));
                    foreach (var merchant in new[] { town, keeper })
                    {
                        var seed = merchant.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == species + "Seed");
                        Assert.AreSame(merchant, seed.GetPart<PhysicsPart>().InInventory);
                        Assert.AreEqual(species + "Crop", seed.GetPart<SeedPart>().CropBlueprint);
                    }
                }
                Assert.AreEqual(1, Units(keeper, "WateringGrimoire"), "The established watering source stays obtainable.");
            }
        }
    }
}
