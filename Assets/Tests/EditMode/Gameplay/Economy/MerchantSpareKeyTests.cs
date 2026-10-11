using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    public abstract class MerchantSpareKeyFixture
    {
        internal EntityFactory Factory;
        private EntityFactory oldFactory;
        private System.Random oldRng;
        [SetUp] public void SetUp()
        {
            oldFactory = TraderPart.Factory; oldRng = TraderPart.Rng;
            Factory = new EntityFactory();
            Factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            TraderPart.Factory = Factory; TraderPart.Rng = new System.Random(1); Diag.ResetAll();
        }
        [TearDown] public void TearDown()
        { TraderPart.Factory = oldFactory; TraderPart.Rng = oldRng; LootTableRegistry.ResetForTests(); }
        internal Entity Actor(int drams = 1000)
        {
            var actor = new Entity { ID = Guid.NewGuid().ToString(), BlueprintName = "Player" };
            actor.AddPart(new InventoryPart()); actor.AddPart(new RenderPart { DisplayName = "buyer" });
            TradeSystem.SetDrams(actor, drams); return actor;
        }
        internal Entity Trader(out Entity key, string blueprint = "SpareIronKey")
        {
            var trader = Actor(); key = Factory.CreateEntity(blueprint);
            Assert.NotNull(key, "authored key must exist"); Assert.True(trader.GetPart<InventoryPart>().AddObject(key)); return trader;
        }
    }

    [TestFixture]
    public class MerchantSpareKeyTests : MerchantSpareKeyFixture
    {
        [TestCase("TinkerStock", "Tinker")]
        [TestCase("MerchantStock", "Merchant")]
        public void AuthoredShopRollsSpareThatCanActuallyBeBought(string table, string npc)
        {
            Assert.True(LootTableRegistry.Get(table).Entries.Any(e => e.Blueprint == "SpareIronKey"));
            Assert.False(LootTableRegistry.Get(table).Entries.Any(e => e.Blueprint == "IronKey"));
            Entity merchant = null, key = null;
            for (int seed = 0; seed < 100 && key == null; seed++)
            {
                TraderPart.Rng = new System.Random(seed); merchant = Factory.CreateEntity(npc);
                key = merchant.GetPart<InventoryPart>().Objects.FirstOrDefault(e => e.BlueprintName == "SpareIronKey");
            }
            Assert.NotNull(key, "ordinary trader creation must reach the new stock");
            var buyer = Actor(); int price = TradeSystem.GetBuyPrice(key, TradeSystem.GetTradePerformance(buyer), merchant);
            int purse = TradeSystem.GetDrams(merchant);
            Assert.True(TradeSystem.BuyFromTrader(buyer, merchant, key));
            Assert.AreEqual(1000 - price, TradeSystem.GetDrams(buyer)); Assert.AreEqual(purse + price, TradeSystem.GetDrams(merchant));
            Assert.Contains(key, buyer.GetPart<InventoryPart>().Objects); Assert.False(merchant.GetPart<InventoryPart>().Objects.Contains(key));
            Assert.False(TradeSystem.BuyFromTrader(buyer, merchant, key), "stale second purchase cannot charge again");
            Assert.AreEqual(1000 - price, TradeSystem.GetDrams(buyer));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "Bought", Limit = 10 }).Records.Count);
        }
        [TestCase("iron", true)] [TestCase("coo.sealed-library.stillleaf", false)] [TestCase("wayhouse:bound-door", false)]
        public void PurchasedSpareOnlyUnlocksMatchingLocks(string keyId, bool expected)
        {
            var trader = Trader(out var key); var buyer = Actor(); Assert.True(TradeSystem.BuyFromTrader(buyer, trader, key));
            var door = new Entity(); var part = new LockPart { KeyId = keyId }; door.AddPart(part);
            var attempt = GameEvent.New("AttemptUnlock"); attempt.SetParameter("Actor", (object)buyer); door.FireEventAndRelease(attempt);
            Assert.AreEqual(!expected, part.IsLocked); Assert.Contains(key, buyer.GetPart<InventoryPart>().Objects);
        }
        [Test] public void PurchasedAndProtectedKeysSurviveGraphRoundTrip()
        {
            var actor = Actor(); actor.GetPart<InventoryPart>().AddObject(Factory.CreateEntity("SpareIronKey"));
            actor.GetPart<InventoryPart>().AddObject(Factory.CreateEntity("StillleafKey"));
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(actor); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, Factory); var loaded = reader.ReadEntityReference(); reader.ReadEntityBodies();
                var items = loaded.GetPart<InventoryPart>().Objects;
                Assert.AreEqual(2, items.Count); Assert.AreEqual("iron", items[0].GetPart<KeyPart>().KeyId);
                Assert.False(items[0].HasTag("NoTrade")); Assert.True(items[1].HasTag("NoTrade"));
                Assert.AreEqual("coo.sealed-library.stillleaf", items[1].GetPart<KeyPart>().KeyId);
                Assert.AreSame(loaded, items[0].GetPart<PhysicsPart>().InInventory);
            }
        }
        [Test] public void NativeStumpVisualUsesTheExistingKeyFamily()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Rendering.StumpVoxelLibrary")).FirstOrDefault(t => t != null);
            if (type == null) Assert.Ignore("Native Stump presentation is outside the standalone runner.");
            var family = type.GetMethod("Family");
            Assert.AreEqual("key", family.Invoke(null, new object[] { "IronKey" }), "existing model-family precondition");
            Assert.AreEqual("key", family.Invoke(null, new object[] { "SpareIronKey" }), "a purchased spare retains key art when dropped in Stump");
            Assert.IsNull(family.Invoke(null, new object[] { "UnrelatedSpareIronKey" }), "exact alias only");
            var model = type.GetMethod("ModelId");
            for (int variant = 0; variant < 4; variant++)
                Assert.AreEqual("stump-key-" + variant, model.Invoke(null, new object[] { family.Invoke(null, new object[] { "SpareIronKey" }), variant }));
        }
        [Test] public void NativeVisualRecipeReusesTheExistingKeyModel()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Rendering.SpreadPortableRecipes")).FirstOrDefault(t => t != null);
            if (type == null) Assert.Ignore("Native presentation is outside the standalone runner.");
            var method = type.GetMethod("TryRecipe"); var key = Factory.CreateEntity("SpareIronKey");
            object[] args = { key, null }; Assert.True((bool)method.Invoke(null, args)); Assert.AreEqual("spread-portable-ironkey", args[1]);
            key.GetPart<RenderPart>().Visible = false; args[1] = null; Assert.False((bool)method.Invoke(null, args));
        }
        [Test] public void SpareCanBeSoldBackAtTheQuotedPrice()
        {
            var trader = Trader(out var key); var buyer = Actor(); Assert.True(TradeSystem.BuyFromTrader(buyer, trader, key));
            int a = TradeSystem.GetDrams(buyer), b = TradeSystem.GetDrams(trader);
            int price = TradeSystem.GetSellPrice(key, TradeSystem.GetTradePerformance(buyer), trader);
            Assert.True(TradeSystem.SellToTrader(buyer, trader, key));
            Assert.AreEqual(a + price, TradeSystem.GetDrams(buyer)); Assert.AreEqual(b - price, TradeSystem.GetDrams(trader));
            Assert.Contains(key, trader.GetPart<InventoryPart>().Objects);
        }
    }
}
