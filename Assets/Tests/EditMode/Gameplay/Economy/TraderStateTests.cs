using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SP.3 tests (Docs/SHOPPING-PARITY.md) for trader-state validation
    /// + the <c>StartTradeEvent</c> session-level hook. Mirrors Qud's
    /// <c>TradeUI.cs:362-379</c> validation set narrowed to states
    /// CoO actually has effects for.
    ///
    /// A trader refuses trade if any of these are true:
    ///   • Hitpoints ≤ 0 (dead)
    ///   • Has BurningEffect (on fire — can't speak)
    ///   • Has StunnedEffect (incapacitated)
    ///   • Has FrozenEffect (frozen solid)
    ///
    /// Counter-check covers the "healthy trader still trades" baseline
    /// to rule out a global-block regression.
    ///
    /// StartTradeEvent fires at the top of OpenTrade (or wherever the
    /// UI launches). v1 has no listeners; the shape is verified so
    /// future content can hook in.
    /// </summary>
    [TestFixture]
    public class TraderStateTests
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
            trader.AddPart(new StatusEffectsPart());
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

        private Entity CreateItem(string name, int value)
        {
            var item = new Entity { BlueprintName = name };
            item.AddPart(new RenderPart { DisplayName = name.ToLower() });
            item.AddPart(new CommercePart { Value = value });
            item.AddPart(new PhysicsPart { Weight = 1, Takeable = true });
            return item;
        }

        // ====================================================================
        // 1-3. Each blocking effect refuses trade
        // ====================================================================

        [Test]
        public void BuyFromTrader_BurningTrader_BlocksTransfer()
        {
            var player = CreatePlayer();
            var trader = CreateTrader();
            trader.GetPart<StatusEffectsPart>().ApplyEffect(new BurningEffect(intensity: 1.0f));
            var item = CreateItem("Apple", value: 10);
            trader.GetPart<InventoryPart>().AddObject(item);

            int playerDramsBefore = TradeSystem.GetDrams(player);
            bool ok = TradeSystem.BuyFromTrader(player, trader, item);

            Assert.IsFalse(ok, "Burning trader must refuse to trade.");
            Assert.AreEqual(playerDramsBefore, TradeSystem.GetDrams(player),
                "Drams must not have been transferred.");
            Assert.IsTrue(trader.GetPart<InventoryPart>().Objects.Contains(item),
                "Item must remain in the trader's inventory.");
        }

        [Test]
        public void BuyFromTrader_StunnedTrader_BlocksTransfer()
        {
            var player = CreatePlayer();
            var trader = CreateTrader();
            trader.GetPart<StatusEffectsPart>().ApplyEffect(new StunnedEffect(duration: 5));
            var item = CreateItem("Apple", value: 10);
            trader.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.BuyFromTrader(player, trader, item);

            Assert.IsFalse(ok, "Stunned trader must refuse to trade.");
        }

        [Test]
        public void BuyFromTrader_DeadTrader_BlocksTransfer()
        {
            var player = CreatePlayer();
            var trader = CreateTrader();
            trader.GetStat("Hitpoints").BaseValue = 0;  // dead
            var item = CreateItem("Apple", value: 10);
            trader.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.BuyFromTrader(player, trader, item);

            Assert.IsFalse(ok, "Dead trader must refuse to trade.");
        }

        // ====================================================================
        // 4. Counter-check — healthy trader trades fine
        // ====================================================================

        [Test]
        public void BuyFromTrader_HealthyTrader_AllowsTransfer()
        {
            var player = CreatePlayer(drams: 1000);
            var trader = CreateTrader();
            // No effects, full HP.
            var item = CreateItem("Apple", value: 10);
            trader.GetPart<InventoryPart>().AddObject(item);

            bool ok = TradeSystem.BuyFromTrader(player, trader, item);

            Assert.IsTrue(ok, "Healthy trader must allow trade.");
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(item),
                "Item must transfer to player.");
        }

        // ====================================================================
        // 5. Sell-side mirror — burning trader refuses sells too
        // ====================================================================

        [Test]
        public void SellToTrader_BurningTrader_BlocksTransfer()
        {
            var player = CreatePlayer(drams: 0);
            var trader = CreateTrader(drams: 1000);
            trader.GetPart<StatusEffectsPart>().ApplyEffect(new BurningEffect(intensity: 1.0f));
            var item = CreateItem("Apple", value: 10);
            player.GetPart<InventoryPart>().AddObject(item);

            int playerDramsBefore = TradeSystem.GetDrams(player);
            bool ok = TradeSystem.SellToTrader(player, trader, item);

            Assert.IsFalse(ok, "Burning trader must refuse sells.");
            Assert.AreEqual(playerDramsBefore, TradeSystem.GetDrams(player),
                "No drams transferred to seller.");
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(item),
                "Item must remain in seller's inventory.");
        }

        // ====================================================================
        // 6. StartTradeEvent fires with correct payload + listener can veto
        // ====================================================================

        [Test]
        public void StartTradeEvent_FiresOnTraderWithCorrectPayload()
        {
            var player = CreatePlayer();
            var trader = CreateTrader();
            var probe = new StartTradeProbePart();
            trader.AddPart(probe);

            bool result = TradeSystem.FireStartTradeEvent(player, trader);

            Assert.IsTrue(result, "Default-allow listener should not veto.");
            Assert.AreEqual(1, probe.FireCount, "Exactly one StartTrade event per call.");
            Assert.AreSame(player, probe.LastBuyer);
            Assert.AreSame(trader, probe.LastTrader);
        }

        [Test]
        public void StartTradeEvent_VetoedByListener_ReturnsFalse()
        {
            var player = CreatePlayer();
            var trader = CreateTrader();
            var probe = new StartTradeProbePart { ReturnValue = false };
            trader.AddPart(probe);

            bool result = TradeSystem.FireStartTradeEvent(player, trader);

            Assert.IsFalse(result,
                "Listener vetoing StartTrade must propagate as false from " +
                "FireStartTradeEvent so callers can abort opening the UI.");
        }

        /// <summary>
        /// Probe Part that captures StartTrade event firings. Used to
        /// verify the event flow shape + the veto path. SP.3 doesn't
        /// have any production listeners — this proves a listener
        /// CAN veto, ready for future content (services / quest gates).
        /// </summary>
        private class StartTradeProbePart : Part
        {
            public override string Name => "StartTradeProbe";
            public int FireCount;
            public Entity LastBuyer;
            public Entity LastTrader;
            public bool ReturnValue = true;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "StartTrade")
                {
                    FireCount++;
                    LastBuyer = e.GetParameter<Entity>("Buyer");
                    LastTrader = e.GetParameter<Entity>("Trader");
                    return ReturnValue;
                }
                return true;
            }
        }
    }
}
