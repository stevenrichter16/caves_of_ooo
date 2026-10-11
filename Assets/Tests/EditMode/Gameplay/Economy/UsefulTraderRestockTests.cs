using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public abstract class UsefulTraderRestockFixture
    {
        protected EntityFactory Factory;
        private EntityFactory previous;
        [SetUp] public void SetUp()
        {
            previous = TraderRestockSystem.Factory; Factory = new EntityFactory();
            Factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            TraderRestockSystem.Factory = Factory; Table();
        }
        [TearDown] public void TearDown() { TraderRestockSystem.Factory = previous; LootTableRegistry.ResetForTests(); }
        protected void Table(string entries = "{\"Blueprint\":\"DriedMeat\",\"MinCount\":5,\"MaxCount\":5}", string extra = "", string properties = "")
        { LootTableRegistry.Initialize("{\"Tables\":[{\"Name\":\"Stock\"," + properties + "\"Entries\":[" + entries + "]}" + extra + "]}"); }
        protected Entity Merchant(Zone zone)
        {
            var e = new Entity { ID = "restock-test", BlueprintName = "test-merchant" };
            e.SetTag("Creature"); e.SetTag("Faction", "Villagers"); e.AddPart(new InventoryPart());
            e.Properties[TraderRestockSystem.ShopStockTableProp] = "Stock"; TradeSystem.SetDrams(e, 1000);
            e.SetIntProperty(TraderRestockSystem.LastRestockProp, 100); Assert.True(zone.AddEntity(e, 5, 5)); return e;
        }
        protected Entity Add(Entity merchant, string bp, int count = 1)
        {
            var item = Factory.CreateEntity(bp); Assert.NotNull(item); var stack = item.GetPart<StackerPart>();
            if (stack != null) stack.StackCount = count;
            Assert.True(merchant.GetPart<InventoryPart>().AddObject(item)); return item;
        }
        protected static int Units(Entity merchant, string bp = "DriedMeat") => merchant.GetPart<InventoryPart>().Objects
            .Where(i => i != null && i.BlueprintName == bp).Sum(i => Math.Max(0, i.GetPart<StackerPart>()?.StackCount ?? 1));
    }
    [TestFixture] public class UsefulTraderRestockTests : UsefulTraderRestockFixture
    {
        [Test] public void ActualPlayerSalesDoNotJamUsefulShelfRefill()
        {
            var zone = new Zone("trade"); var merchant = Merchant(zone);
            var player = new Entity(); player.AddPart(new InventoryPart()); TradeSystem.SetDrams(player, 1000);
            var junk = new[] { Add(player, "SpareIronKey"), Add(player, "Dagger"), Add(player, "Torch") };
            int quotes = junk.Sum(i => TradeSystem.GetSellPrice(i, TradeSystem.GetTradePerformance(player), merchant));
            foreach (var item in junk) Assert.True(TradeSystem.SellToTrader(player, merchant, item));
            Assert.AreEqual(1000 + quotes, TradeSystem.GetDrams(player)); Assert.AreEqual(1000 - quotes, TradeSystem.GetDrams(merchant));
            TraderRestockSystem.RestockZone(zone, 401);
            Assert.AreEqual(5, Units(merchant), "one full authored roll reaches a shelf holding only sold junk");
            foreach (var item in junk) Assert.Contains(item, merchant.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(1000 - quotes, TradeSystem.GetDrams(merchant), "refill itself charges no currency");
            TraderRestockSystem.RestockZone(zone, 401); TraderRestockSystem.RestockZone(zone, 702);
            Assert.AreEqual(5, Units(merchant), "same-turn and later visits cannot accumulate the retained stack");
        }
        [TestCase(0, 5)] [TestCase(1, 6)] [TestCase(2, 7)] [TestCase(3, 3)] [TestCase(50, 50)]
        public void PositiveMatchingUnitsDefineLowStock(int held, int expected)
        {
            var zone = new Zone("trade"); var merchant = Merchant(zone); if (held > 0) Add(merchant, "DriedMeat", held);
            TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(expected, Units(merchant));
        }
        [Test] public void NestedStockRecognizesItsExistingGoods()
        {
            Table("{\"TableRef\":\"Inner\"}", ",{\"Name\":\"Inner\",\"Entries\":[{\"Blueprint\":\"DriedMeat\",\"MinCount\":5,\"MaxCount\":5}]}");
            var zone = new Zone("trade"); var merchant = Merchant(zone); Add(merchant, "DriedMeat", 3);
            TraderRestockSystem.RestockZone(zone, 401); Assert.AreEqual(3, Units(merchant));
        }
        [Test] public void StockAndTimestampRoundTripCloseTheGate()
        {
            var zone = new Zone("trade"); var merchant = Merchant(zone); TraderRestockSystem.RestockZone(zone, 401);
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(merchant); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, Factory); var loaded = reader.ReadEntityReference(); reader.ReadEntityBodies();
                Assert.AreEqual(401, loaded.GetIntProperty(TraderRestockSystem.LastRestockProp)); Assert.AreEqual("Stock", loaded.GetProperty(TraderRestockSystem.ShopStockTableProp));
                var restored = new Zone("restored"); Assert.True(restored.AddEntity(loaded, 5, 5));
                TraderRestockSystem.RestockZone(restored, 401); TraderRestockSystem.RestockZone(restored, 702); Assert.AreEqual(5, Units(loaded));
            }
        }
    }
}
