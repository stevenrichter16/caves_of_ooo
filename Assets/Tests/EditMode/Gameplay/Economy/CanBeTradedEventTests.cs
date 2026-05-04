using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SP.2 tests (Docs/SHOPPING-PARITY.md) for the
    /// <c>CanBeTradedEvent</c> + <c>"NoTrade"</c> tag — Qud-parity
    /// per-item veto for quest-item / dungeon-key protection.
    ///
    /// Verifies two veto paths:
    ///   1. Tag-based: items with <c>"NoTrade"</c> tag refuse trade
    ///      without consulting any HandleEvent listener.
    ///   2. Event-based: the <c>CanBeTraded</c> event fires on the
    ///      item; any Part returning false from HandleEvent vetoes.
    ///
    /// Counter-checks ensure untagged + listener-allowed items still
    /// trade normally — i.e., the veto isn't accidentally global.
    /// </summary>
    [TestFixture]
    public class CanBeTradedEventTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity CreateTrader(int drams = 1000)
        {
            var trader = new Entity { BlueprintName = "TestTrader" };
            trader.Tags["Creature"] = "";
            trader.Tags["Faction"] = "Villagers";
            trader.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            trader.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            trader.AddPart(new RenderPart { DisplayName = "trader" });
            trader.AddPart(new InventoryPart());
            TradeSystem.SetDrams(trader, drams);
            return trader;
        }

        private Entity CreatePlayer(int drams = 1000)
        {
            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Creature"] = "";
            player.Tags["Player"] = "";
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            player.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 18, Min = 1, Max = 50 };
            player.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            player.AddPart(new RenderPart { DisplayName = "you" });
            player.AddPart(new InventoryPart());
            TradeSystem.SetDrams(player, drams);
            return player;
        }

        private Entity CreateItem(string name, int value, bool noTrade = false)
        {
            var item = new Entity { BlueprintName = name };
            item.AddPart(new RenderPart { DisplayName = name.ToLower() });
            item.AddPart(new CommercePart { Value = value });
            item.AddPart(new PhysicsPart { Weight = 1, Takeable = true });
            if (noTrade) item.Tags["NoTrade"] = "";
            return item;
        }

        /// <summary>
        /// Probe Part that listens for CanBeTraded events on its
        /// parent entity and counts firings + records the last
        /// payload. Used to verify the event flow shape.
        /// </summary>
        private class CanBeTradedProbePart : Part
        {
            public override string Name => "CanBeTradedProbe";
            public int FireCount;
            public string LastDirection;
            public Entity LastActor;
            public Entity LastTrader;
            public bool ReturnValue = true;  // false = veto

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "CanBeTraded")
                {
                    FireCount++;
                    // GameEvent stores string params in StringParameters
                    // (separate dict from Parameters); GetParameter<string>
                    // only reads Parameters and returns null. Use the
                    // type-specific getter for strings.
                    LastDirection = e.GetStringParameter("Direction");
                    LastActor = e.GetParameter<Entity>("Actor");
                    LastTrader = e.GetParameter<Entity>("Trader");
                    return ReturnValue;
                }
                return true;
            }
        }

        // ====================================================================
        // 1. CanBeTradedEvent fires on buy with correct payload
        // ====================================================================

        [Test]
        public void CanBeTradedEvent_FiresOnBuy_WithCorrectPayload()
        {
            var player = CreatePlayer();
            var trader = CreateTrader();
            var item = CreateItem("Apple", value: 10);
            var probe = new CanBeTradedProbePart();
            item.AddPart(probe);
            trader.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.BuyFromTrader(player, trader, item);
            Assert.IsTrue(ok, "Untagged + listener-allowed buy must succeed.");

            Assert.AreEqual(1, probe.FireCount,
                "CanBeTraded event must fire exactly once per buy attempt.");
            Assert.AreEqual("Buy", probe.LastDirection);
            Assert.AreSame(player, probe.LastActor);
            Assert.AreSame(trader, probe.LastTrader);
        }

        // ====================================================================
        // 2. CanBeTradedEvent fires on sell with correct payload
        // ====================================================================

        [Test]
        public void CanBeTradedEvent_FiresOnSell_WithCorrectPayload()
        {
            var player = CreatePlayer();
            var trader = CreateTrader();
            var item = CreateItem("Apple", value: 10);
            var probe = new CanBeTradedProbePart();
            item.AddPart(probe);
            player.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.SellToTrader(player, trader, item);
            Assert.IsTrue(ok, "Untagged + listener-allowed sell must succeed.");

            Assert.AreEqual(1, probe.FireCount);
            Assert.AreEqual("Sell", probe.LastDirection);
            Assert.AreSame(player, probe.LastActor);
            Assert.AreSame(trader, probe.LastTrader);
        }

        // ====================================================================
        // 3. NoTrade-tagged item refuses to be bought
        // ====================================================================

        [Test]
        public void BuyFromTrader_NoTradeTaggedItem_TransferDoesNotHappen()
        {
            var player = CreatePlayer(drams: 1000);
            var trader = CreateTrader(drams: 0);
            var item = CreateItem("QuestKey", value: 5, noTrade: true);
            trader.GetPart<InventoryPart>().AddObject(item);
            int playerDramsBefore = TradeSystem.GetDrams(player);

            bool ok = TradeSystem.BuyFromTrader(player, trader, item);

            Assert.IsFalse(ok, "NoTrade-tagged item must refuse buy.");
            Assert.IsTrue(trader.GetPart<InventoryPart>().Objects.Contains(item),
                "Item must remain in trader's inventory.");
            Assert.IsFalse(player.GetPart<InventoryPart>().Objects.Contains(item),
                "Item must NOT be in player's inventory.");
            Assert.AreEqual(playerDramsBefore, TradeSystem.GetDrams(player),
                "Player's drams must NOT have decreased — no transfer happened.");
        }

        // ====================================================================
        // 4. NoTrade-tagged item refuses to be sold
        // ====================================================================

        [Test]
        public void SellToTrader_NoTradeTaggedItem_TransferDoesNotHappen()
        {
            var player = CreatePlayer(drams: 0);
            var trader = CreateTrader(drams: 1000);
            var item = CreateItem("QuestKey", value: 5, noTrade: true);
            player.GetPart<InventoryPart>().AddObject(item);
            int playerDramsBefore = TradeSystem.GetDrams(player);

            bool ok = TradeSystem.SellToTrader(player, trader, item);

            Assert.IsFalse(ok, "NoTrade-tagged item must refuse sell.");
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(item),
                "Item must remain in player's inventory.");
            Assert.IsFalse(trader.GetPart<InventoryPart>().Objects.Contains(item),
                "Item must NOT be in trader's inventory.");
            Assert.AreEqual(playerDramsBefore, TradeSystem.GetDrams(player),
                "Player's drams must NOT have increased — no sale happened.");
        }

        // ====================================================================
        // 5. Listener-vetoed item refuses trade (event-based veto path)
        // ====================================================================

        [Test]
        public void BuyFromTrader_ListenerVeto_TransferDoesNotHappen()
        {
            var player = CreatePlayer(drams: 1000);
            var trader = CreateTrader(drams: 0);
            var item = CreateItem("Apple", value: 10);  // no NoTrade tag
            // Probe vetoes via ReturnValue=false (instead of tag).
            var probe = new CanBeTradedProbePart { ReturnValue = false };
            item.AddPart(probe);
            trader.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.BuyFromTrader(player, trader, item);

            Assert.IsFalse(ok,
                "Listener-vetoed item must refuse buy even without a tag.");
            Assert.IsTrue(trader.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.AreEqual(1000, TradeSystem.GetDrams(player),
                "Drams must be untouched on listener veto.");
        }

        // ====================================================================
        // 6. Counter-check — untagged item with allowing listener trades fine
        //    (proves the SP.2 hook isn't accidentally a global block)
        // ====================================================================

        [Test]
        public void BuyFromTrader_UntaggedItem_TradesNormally()
        {
            var player = CreatePlayer(drams: 1000);
            var trader = CreateTrader(drams: 0);
            var item = CreateItem("Apple", value: 10);
            trader.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.BuyFromTrader(player, trader, item);

            Assert.IsTrue(ok,
                "Untagged item with no veto-listener must trade normally.");
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(item),
                "Item must transfer to player.");
            Assert.Less(TradeSystem.GetDrams(player), 1000,
                "Player must have paid drams.");
        }

        // ====================================================================
        // 7. Adversarial — null item doesn't crash CanBeTraded helper
        // ====================================================================

        [Test]
        public void CanBeTraded_NullItem_ReturnsFalseWithoutCrash()
        {
            var player = CreatePlayer();
            var trader = CreateTrader();
            // Calling CanBeTraded directly with null
            bool result = TradeSystem.CanBeTraded(null, player, trader, "Buy");
            Assert.IsFalse(result, "Null item must return false (defensive).");
        }
    }
}
