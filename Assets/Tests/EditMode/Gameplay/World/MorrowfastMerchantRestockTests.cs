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
        private static readonly string[] MenderItems = { "Torch", "Dagger", "Spear", "LeatherArmor", "Tepuibone", "FireClay", "SalvagedTimber", "KnotflaxCord", "QuarryToothTrinket", "TonguelessBellTrinket" };
        private static readonly int[] MenderCounts = { 4, 2, 1, 1, 3, 4, 4, 3, 1, 1 };
        private static readonly string[] FoodItems = { "Mushroom", "DriedMeat", "HealingTonic", "BurnSalve", "WaterTonic", "Waterskin", "LiquidFlask", "KnotflaxSeed", "HearthbulbSeed", "SeamleafSeed", "InertSludge", "FieldSatchel" };
        private static readonly int[] FoodCounts = { 8, 4, 2, 2, 3, 1, 1, 2, 2, 2, 2, 1 };

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
            AssertStock(merchant, food, !food && wired ? 2 : 0);
            Assert.AreEqual(food ? 200 : 300, TradeSystem.GetDrams(merchant));
            foreach (var item in merchant.GetPart<InventoryPart>().Objects)
            { Assert.AreSame(merchant, item.GetPart<PhysicsPart>().InInventory); Assert.NotNull(item.GetPart<CommercePart>()); }
        }

        [Test]
        public void FreshOrritSellsOneAffordableMendedTrapWithOrdinaryFixtureParts()
        {
            var manager = new OverworldZoneManager(factory, 64);
            var zone = manager.GetZone(MorrowfastSceneRuntime.ZoneID);
            var merchant = MorrowfastSceneRuntime.FindOwner(zone, "southwest-craftsperson");
            var items = merchant.GetPart<InventoryPart>().Objects;
            var kits = items.Where(item => item.BlueprintName == "CampFishTrap").ToArray();
            Assert.That(kits, Has.Length.EqualTo(1));
            var kit = kits[0];
            var kitParts = kit.GetPart<PackableFixturePart>();
            var catchState = kit.GetPart<FishTrapPart>();
            Assert.AreEqual("mended fish trap", kit.GetDisplayName());
            Assert.AreEqual(CampKinds.FishTrap, kitParts.Kind);
            Assert.AreEqual(-1, kitParts.SetTick);
            Assert.That(catchState.Held, Is.Zero);
            Assert.That(catchState.Progress, Is.Zero);
            Assert.AreEqual(-1, catchState.LastTick);
            Assert.IsNull(kit.GetPart<InstallablePart>());
            Assert.IsNull(kit.GetPart<StackerPart>());
            Assert.AreSame(merchant, kit.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(14, kit.GetPart<PhysicsPart>().Weight);
            Assert.That(items.Where(item => item.BlueprintName == "CoarseGrit")
                .Sum(item => item.GetPart<StackerPart>()?.StackCount ?? 1), Is.EqualTo(2));

            var buyer = factory.CreateEntity("Player");
            var performance = TradeSystem.GetTradePerformance(buyer);
            int price = TradeSystem.GetBuyPrice(kit, performance, merchant);
            Assert.That(price, Is.InRange(1, TradeSystem.GetDrams(buyer)));
            Assert.That(TradeSystem.GetSellPrice(kit, performance, merchant), Is.LessThan(price));
            int drams = TradeSystem.GetDrams(buyer);
            Assert.IsTrue(TradeSystem.BuyFromTrader(buyer, merchant, kit));
            Assert.AreEqual(drams - price, TradeSystem.GetDrams(buyer));
            Assert.Contains(kit, buyer.GetPart<InventoryPart>().Objects);
            Assert.That(items.Any(item => item.BlueprintName == "CampFishTrap"), Is.False);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(buyer)
                .GetPart<InventoryPart>().Objects.Single(item => item.ID == kit.ID);
            Assert.AreEqual(kit.GetDisplayName(), loaded.GetDisplayName());
            Assert.AreEqual(kit.GetPart<CommercePart>().Value, loaded.GetPart<CommercePart>().Value);
            Assert.AreEqual(55, factory.CreateEntity("CampFishTrap").GetPart<CommercePart>().Value);

            manager.UnloadZone(zone.ZoneID);
            Assert.AreSame(zone, manager.GetZone(zone.ZoneID));
            foreach (var item in items.ToArray()) merchant.GetPart<InventoryPart>().RemoveObject(item);
            TraderRestockSystem.RestockZone(zone, 1800);
            Assert.That(items, Is.Not.Empty);
            Assert.That(items.Any(item => item.BlueprintName == "CampFishTrap"), Is.False);
        }

        private (OverworldZoneManager manager, Zone zone, Entity merchant) FreshOrrit()
        {
            var manager = new OverworldZoneManager(factory, 64);
            var zone = manager.GetZone(MorrowfastSceneRuntime.ZoneID);
            return (manager, zone, MorrowfastSceneRuntime.FindOwner(zone, "southwest-craftsperson"));
        }

        [Test]
        public void FreshOrritOffersOneSoundHandSieveThatTradesAndScreensNormally()
        {
            var (_, zone, merchant) = FreshOrrit();
            AssertStock(merchant, false, 2, "CampFishTrap", "HandSieve");
            var sieve = TradeSystem.GetTraderStock(merchant).Single(item => item.BlueprintName == "HandSieve");
            var physics = sieve.GetPart<PhysicsPart>();
            Assert.AreEqual(4, physics.Weight);
            Assert.AreEqual(12, sieve.GetPart<CommercePart>().Value);
            Assert.IsTrue(physics.Takeable);
            Assert.IsFalse(physics.Solid);
            Assert.AreSame(merchant, physics.InInventory);
            Assert.IsNull(sieve.GetPart<StackerPart>());
            Assert.AreSame(sieve, sieve.GetPart<ScreeningToolPart>()?.ParentEntity);
            var provisioner = MorrowfastSceneRuntime.FindOwner(zone, "southern-food-vendor");
            Assert.False(TradeSystem.GetTraderStock(provisioner).Any(item => item.BlueprintName == "HandSieve"));

            var buyer = MorrowfastTestWorld.Actor(factory, zone);
            var pack = buyer.GetPart<InventoryPart>();
            int packLimit = pack.MaxWeight;
            int funds = TradeSystem.GetDrams(buyer);
            int purse = TradeSystem.GetDrams(merchant);
            var performance = TradeSystem.GetTradePerformance(buyer);
            int price = TradeSystem.GetBuyPrice(sieve, performance, merchant);
            Assert.That(price, Is.InRange(1, funds));
            Assert.That(TradeSystem.GetSellPrice(sieve, performance, merchant), Is.LessThan(price));

            TradeSystem.SetDrams(buyer, price - 1);
            Assert.False(TradeSystem.BuyFromTrader(buyer, merchant, sieve));
            Assert.AreEqual(price - 1, TradeSystem.GetDrams(buyer));
            TradeSystem.SetDrams(buyer, funds);
            pack.MaxWeight = pack.GetCarriedWeight() + 3;
            Assert.False(TradeSystem.BuyFromTrader(buyer, merchant, sieve));
            Assert.AreEqual(funds, TradeSystem.GetDrams(buyer));
            Assert.AreEqual(purse, TradeSystem.GetDrams(merchant));
            Assert.AreSame(merchant, physics.InInventory);
            Assert.AreSame(sieve, TradeSystem.GetTraderStock(merchant).Single(item => item.BlueprintName == "HandSieve"));

            pack.MaxWeight = packLimit;
            Assert.True(TradeSystem.BuyFromTrader(buyer, merchant, sieve));
            Assert.AreEqual(funds - price, TradeSystem.GetDrams(buyer));
            Assert.AreEqual(purse + price, TradeSystem.GetDrams(merchant));
            Assert.AreSame(buyer, physics.InInventory);
            Assert.Contains(sieve, pack.Objects);
            Assert.False(TradeSystem.GetTraderStock(merchant).Any(item => item.BlueprintName == "HandSieve"));
            Assert.True(TradeSystem.GetTraderStock(merchant).Any(item => item.BlueprintName == "CampFishTrap"));

            var oldForge = ForgePart.Factory;
            try
            {
                ForgePart.Factory = factory;
                var packet = factory.CreateEntity("QuarrySweepings");
                Assert.True(pack.AddObject(packet));
                var row = InventorySystem.GetActions(buyer, packet).Single(action => !action.IsDisabled
                    && action.Command.StartsWith("MaterialProcess|screen_quarry_sweepings|", StringComparison.Ordinal));
                Assert.True(InventorySystem.PerformAction(buyer, packet, row.Command, zone));
                Assert.Contains(sieve, pack.Objects);
                Assert.False(pack.Objects.Contains(packet));
                Assert.True(pack.Objects.Any(item => item.BlueprintName == "PaleSalt"));
                Assert.True(pack.Objects.Any(item => item.BlueprintName == "SilverSand"));
            }
            finally { ForgePart.Factory = oldForge; }

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(buyer);
            var savedSieve = loaded.GetPart<InventoryPart>().Objects.Single(item => item.BlueprintName == "HandSieve");
            Assert.AreEqual(sieve.ID, savedSieve.ID);
            Assert.AreSame(loaded, savedSieve.GetPart<PhysicsPart>().InInventory);
            Assert.AreSame(savedSieve, savedSieve.GetPart<ScreeningToolPart>()?.ParentEntity);
        }

        [Test]
        public void BuyingTheSieveThenRevisitingAndRestockingNeverMakesAnother()
        {
            var (manager, zone, merchant) = FreshOrrit();
            var shelf = merchant.GetPart<InventoryPart>();
            var sieve = shelf.Objects.Single(item => item.BlueprintName == "HandSieve");
            Assert.True(TradeSystem.BuyFromTrader(Buyer(), merchant, sieve));
            Assert.False(TradeSystem.BuyFromTrader(Buyer(), merchant, sieve));
            var saved = PartRoundTripHelper.RoundTripEntityViaTokenGraph(merchant);
            AssertStock(saved, false, 2, "CampFishTrap");
            manager.UnloadZone(zone.ZoneID);
            Assert.AreSame(zone, manager.GetZone(zone.ZoneID));
            Assert.AreSame(merchant, MorrowfastSceneRuntime.FindOwner(zone, "southwest-craftsperson"));
            AssertStock(merchant, false, 2, "CampFishTrap");

            foreach (var item in shelf.Objects.ToArray()) Assert.True(shelf.RemoveObject(item));
            TraderRestockSystem.RestockZone(zone, 1800);
            AssertStock(merchant, false);
            TraderRestockSystem.RestockZone(zone, 3600);
            AssertStock(merchant, false);
        }

        [TestCase("missing")] [TestCase("weight")] [TestCase("capability")] [TestCase("destroyed")]
        public void AnUnqualifiedSieveIsSkippedWithoutChangingTheMendedTrapOrOrdinaryGoods(string fault)
        {
            var template = factory.Blueprints["HandSieve"];
            if (fault == "missing") Assert.True(factory.Blueprints.Remove("HandSieve"));
            if (fault == "weight") template.Parts["Physics"]["Weight"] = "5";
            if (fault == "capability") Assert.True(template.Parts.Remove("ScreeningTool"));
            if (fault == "destroyed") template.Parts["Destructible"] = new Dictionary<string, string> { ["HP"] = "0" };

            var (_, zone, merchant) = FreshOrrit();
            AssertStock(merchant, false, 2, "CampFishTrap");
            if (fault != "missing") return;

            factory.Blueprints["HandSieve"] = template;
            Assert.True(MorrowfastSceneRuntime.UpgradeCachedZone(zone, factory));
            Assert.AreSame(merchant, MorrowfastSceneRuntime.FindOwner(zone, "southwest-craftsperson"));
            AssertStock(merchant, false, 2, "CampFishTrap");
        }

        [TestCase(false)] [TestCase(true)]
        public void KeepingAnOlderMorrowfastGraphOrAnInvalidTrapTemplateDoesNotGrantOne(bool malformed)
        {
            var legacy = new Zone(MorrowfastSceneRuntime.ZoneID);
            Assert.IsTrue(MorrowfastSceneRuntime.UpgradeCachedZone(legacy, factory));
            AssertStock(MorrowfastSceneRuntime.FindOwner(legacy, "southwest-craftsperson"), false, 2);
            if (malformed)
                factory.Blueprints["CampFishTrap"].Parts["FishTrap"]["Held"] = "1";
            else
                Assert.IsTrue(factory.Blueprints.Remove("CampFishTrap"));
            var zone = new OverworldZoneManager(factory, 64).GetZone(MorrowfastSceneRuntime.ZoneID);
            AssertStock(MorrowfastSceneRuntime.FindOwner(zone, "southwest-craftsperson"), false, 2, "HandSieve");
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
        private static void AssertStock(Entity merchant, bool food, int openingGrit = 0, params string[] openingItems)
        {
            var units = new List<string>();
            foreach (var item in merchant.GetPart<InventoryPart>().Objects)
                for (int i = 0, n = item.GetPart<StackerPart>()?.StackCount ?? 1; i < n; i++) units.Add(item.BlueprintName);
            CollectionAssert.AreEquivalent(ExpectedUnits(food).Concat(Enumerable.Repeat("CoarseGrit", openingGrit)).Concat(openingItems), units);
        }
    }
}
