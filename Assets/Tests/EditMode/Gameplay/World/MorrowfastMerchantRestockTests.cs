using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Purchase real merchandise, leave the native shelf low, and revisit
    /// after the existing interval. Authored stock must opt these two Stillcord
    /// merchants in without changing who qualifies elsewhere.</summary>
    public sealed class MorrowfastMerchantRestockTests
    {
        private EntityFactory factory, previousTraderFactory, previousRestockFactory;
        private System.Random previousRng;
        private static readonly string[] MenderItems = { "Torch", "Dagger", "Spear", "LeatherArmor", "Tepuibone", "FireClay" };
        private static readonly int[] MenderCounts = { 4, 2, 1, 1, 3, 4 };
        private static readonly string[] FoodItems = { "Mushroom", "DriedMeat", "HealingTonic", "BurnSalve", "WaterTonic" };
        private static readonly int[] FoodCounts = { 8, 4, 2, 2, 3 };

        [SetUp] public void SetUp()
        {
            previousTraderFactory = TraderPart.Factory;
            previousRestockFactory = TraderRestockSystem.Factory;
            previousRng = TraderPart.Rng;
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LootTableRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot"), "*.json").OrderBy(p => p).Select(File.ReadAllText));
            TraderPart.Factory = factory; TraderRestockSystem.Factory = factory; TraderPart.Rng = new System.Random(64);
        }

        [TearDown] public void TearDown()
        {
            TraderPart.Factory = previousTraderFactory; TraderRestockSystem.Factory = previousRestockFactory;
            TraderPart.Rng = previousRng; LootTableRegistry.ResetForTests();
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualMerchantDeclaresItsOwnRealNonemptyStockTable(bool food)
        {
            var merchant = Make(food); var trader = merchant.GetPart<TraderPart>();
            Assert.AreEqual("Stillcord", merchant.GetTag("Faction"));
            Assert.NotNull(trader); Assert.IsNotEmpty(trader.StockTable);
            Assert.AreEqual(food ? "MorrowfastProvisionerStock" : "MorrowfastMenderStock", trader.StockTable);
            Assert.AreEqual(trader.StockTable, merchant.GetProperty(TraderRestockSystem.ShopStockTableProp));
            Assert.NotNull(LootTableRegistry.Get(trader.StockTable));
            CollectionAssert.AreEquivalent(ExpectedUnits(food), LootTableRegistry.Roll(trader.StockTable, new System.Random(1729)));
        }

        // A connected factory must not roll the new table AND append the old
        // manual stock. Headless callers still need the same starting goods.
        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void InitialStockKeepsExactExistingQuantitiesWithOrWithoutTraderFactory(bool food, bool wired)
        {
            TraderPart.Factory = wired ? factory : null;
            var merchant = Make(food);
            AssertStock(merchant, food);
            Assert.AreEqual(food ? 200 : 300, TradeSystem.GetDrams(merchant));
            foreach (var item in merchant.GetPart<InventoryPart>().Objects)
            { Assert.AreSame(merchant, item.GetPart<PhysicsPart>().InInventory); Assert.NotNull(item.GetPart<CommercePart>()); }
        }

        [TestCase(false)] [TestCase(true)]
        public void BuyingOutThenRevisitingAfterTheIntervalRefillsTheSameLivingMerchant(bool food)
        {
            var merchant = Make(food); var zone = Put(merchant); var buyer = Buyer();
            BuyEverything(merchant, buyer);
            Assert.IsEmpty(merchant.GetPart<InventoryPart>().Objects);
            TradeSystem.SetDrams(merchant, 0); merchant.SetIntProperty(TraderRestockSystem.LastRestockProp, 100);
            string id = merchant.ID;
            TraderRestockSystem.RestockZone(zone, 401);
            Assert.AreEqual(id, merchant.ID); Assert.AreSame(merchant, zone.GetCell(20, 12).Objects.Single());
            Assert.AreEqual(TraderRestockSystem.DramsFloor, TradeSystem.GetDrams(merchant));
            AssertStock(merchant, food); Assert.AreEqual(401, merchant.GetIntProperty(TraderRestockSystem.LastRestockProp));
            MorrowfastContent.EnsureRegistered(); TraderRestockSystem.RestockZone(zone, 401);
            AssertStock(merchant, food);
        }

        [TestCase(false)] [TestCase(true)]
        public void ExactIntervalDoesNotRefillButNextTurnDoes(bool food)
        {
            var merchant = Make(food); var zone = Put(merchant); BuyEverything(merchant, Buyer());
            merchant.SetIntProperty(TraderRestockSystem.LastRestockProp, 100); TradeSystem.SetDrams(merchant, 0);
            TraderRestockSystem.RestockZone(zone, 400);
            Assert.IsEmpty(merchant.GetPart<InventoryPart>().Objects); Assert.AreEqual(0, TradeSystem.GetDrams(merchant));
            Assert.AreEqual(100, merchant.GetIntProperty(TraderRestockSystem.LastRestockProp));
            TraderRestockSystem.RestockZone(zone, 401); AssertStock(merchant, food);
        }

        [TestCase(false)] [TestCase(true)]
        public void StockAboveTheLowWaterMarkAndRichPurseArePreserved(bool food)
        {
            var merchant = Make(food); var zone = Put(merchant);
            var before = merchant.GetPart<InventoryPart>().Objects.ToArray();
            Assert.GreaterOrEqual(before.Length, TraderRestockSystem.ShelfLowWaterMark);
            TradeSystem.SetDrams(merchant, 700); TraderRestockSystem.RestockZone(zone, 401);
            CollectionAssert.AreEqual(before, merchant.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(700, TradeSystem.GetDrams(merchant));
        }

        [TestCase(false)] [TestCase(true)]
        public void CurrentSaveGraphRetainsDeclarationAndRemainingStockWithoutRefillingOnLoad(bool food)
        {
            var original = Make(food); BuyEverything(original, Buyer());
            original.SetIntProperty(TraderRestockSystem.LastRestockProp, 200); TradeSystem.SetDrams(original, 0);
            var loaded = PartRoundTripHelper.RoundTripEntity(original);
            Assert.IsEmpty(loaded.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(original.GetPart<TraderPart>().StockTable, loaded.GetPart<TraderPart>().StockTable);
            var zone = Put(loaded); TraderRestockSystem.RestockZone(zone, 500);
            Assert.IsEmpty(loaded.GetPart<InventoryPart>().Objects);
            TraderRestockSystem.RestockZone(zone, 501); AssertStock(loaded, food);
        }

        [TestCase(false)] [TestCase(true)]
        public void OrdinaryStillcordAndForgedStockPropertyDoNotBecomeRestockingMerchants(bool forged)
        {
            var person = MorrowfastContent.CreateResident("east-robed-resident", factory);
            Assert.IsNull(person.GetPart<TraderPart>());
            TradeSystem.SetDrams(person, 0);
            if (forged) person.Properties[TraderRestockSystem.ShopStockTableProp] = "MorrowfastProvisionerStock";
            TraderRestockSystem.RestockZone(Put(person), 1000);
            Assert.AreEqual(0, TradeSystem.GetDrams(person)); Assert.IsEmpty(person.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(-1, person.GetIntProperty(TraderRestockSystem.LastRestockProp, -1));
        }

        [Test]
        public void NonVillageTraderWithEmptyDeclarationStillDoesNotQualify()
        {
            var person = factory.CreateEntity("Creature"); person.Tags["Faction"] = "Stillcord";
            person.AddPart(new TraderPart { StockTable = "" }); TradeSystem.SetDrams(person, 0);
            person.Properties[TraderRestockSystem.ShopStockTableProp] = "MorrowfastMenderStock";
            TraderRestockSystem.RestockZone(Put(person), 1000);
            Assert.AreEqual(0, TradeSystem.GetDrams(person)); Assert.IsEmpty(person.GetPart<InventoryPart>().Objects);
        }

        [Test]
        public void LegacyVillagePurseAndExistingDeclaredChoirMerchantRetainTheirEligibility()
        {
            var villager = factory.CreateEntity("Villager");
            // The shipped Villager now inherits Trader. Remove it only in this
            // counterfixture to isolate the retained legacy faction/purse route.
            var declared = villager.GetPart<TraderPart>(); if (declared != null) villager.RemovePart(declared);
            Assert.IsNull(villager.GetPart<TraderPart>());
            TradeSystem.SetDrams(villager, 0); TraderRestockSystem.RestockZone(Put(villager), 1000);
            Assert.AreEqual(TraderRestockSystem.DramsFloor, TradeSystem.GetDrams(villager));
            var choir = factory.CreateEntity("ChoirTendril"); Assert.IsNotEmpty(choir.GetPart<TraderPart>().StockTable);
            foreach (var item in choir.GetPart<InventoryPart>().Objects.ToArray()) choir.GetPart<InventoryPart>().RemoveObject(item);
            TradeSystem.SetDrams(choir, 0); TraderRestockSystem.RestockZone(Put(choir), 1000);
            Assert.AreEqual(TraderRestockSystem.DramsFloor, TradeSystem.GetDrams(choir));
            Assert.IsNotEmpty(choir.GetPart<InventoryPart>().Objects);
        }

        private Entity Make(bool food) => MorrowfastContent.CreateResident(food ? "southern-food-vendor" : "southwest-craftsperson", factory);
        private static Zone Put(Entity person) { var zone = new Zone("Overworld.3.6.0"); Assert.IsTrue(zone.AddEntity(person, 20, 12)); return zone; }
        private Entity Buyer() { var buyer = factory.CreateEntity("Player"); buyer.GetPart<InventoryPart>().MaxWeight = -1; TradeSystem.SetDrams(buyer, 100000); return buyer; }
        private static void BuyEverything(Entity merchant, Entity buyer)
        {
            foreach (var item in merchant.GetPart<InventoryPart>().Objects.ToArray())
                Assert.IsTrue(TradeSystem.BuyFromTrader(buyer, merchant, item), item.BlueprintName);
        }
        private static string[] ExpectedUnits(bool food)
        {
            var names = food ? FoodItems : MenderItems; var counts = food ? FoodCounts : MenderCounts;
            var units = new List<string>(); for (int i = 0; i < names.Length; i++) for (int j = 0; j < counts[i]; j++) units.Add(names[i]);
            return units.ToArray();
        }
        private static void AssertStock(Entity merchant, bool food)
        {
            var units = new List<string>();
            foreach (var item in merchant.GetPart<InventoryPart>().Objects)
                for (int i = 0, n = item.GetPart<StackerPart>()?.StackCount ?? 1; i < n; i++) units.Add(item.BlueprintName);
            CollectionAssert.AreEquivalent(ExpectedUnits(food), units);
        }
    }
}
