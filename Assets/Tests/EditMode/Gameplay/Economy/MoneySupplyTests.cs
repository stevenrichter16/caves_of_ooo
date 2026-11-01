using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// EC2 (Docs/ECONOMY-SYSTEM.md §5): money only enters the world through named faucets, and no
    /// round trip a player can repeat mints it. Pins for the sweep's findings E9 (the coin round trip)
    /// and E2 (a reward near the purse's ceiling).
    /// </summary>
    public sealed class MoneySupplyTests
    {
        static EntityFactory factory;
        EntityFactory oldTrader, oldLoadout;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            FactionManager.Initialize(); PlayerReputation.Reset(); MessageLog.Clear();
            oldTrader = TraderPart.Factory; oldLoadout = LoadoutPart.Factory; TraderPart.Factory = null; LoadoutPart.Factory = null;
        }
        [TearDown] public void TearDown() { TraderPart.Factory = oldTrader; LoadoutPart.Factory = oldLoadout; FactionManager.Reset(); PlayerReputation.Reset(); MessageLog.Clear(); }

        (Zone zone, Entity player, Entity trader) Market()
        {
            var zone = new Zone("Overworld.10.11.0");
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) zone.AddEntity(factory.CreateEntity("StoneFloor"), x, y);
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 20, 12);
            var trader = factory.CreateEntity("Merchant"); zone.AddEntity(trader, 21, 12);
            TradeSystem.SetDrams(trader, 1000);
            return (zone, player, trader);
        }

        /// <summary>E9. Hypothesis: a coin bought from a trader, dropped and picked up again pays out its
        /// pickup credit (5 drams) for less than that (its trade value is 1). Repeated while the trader
        /// restocks, it mints money. Coins are money, not merchandise: the purchase is refused.</summary>
        [Test] public void BuyingACoinAndPickingItUpNeverMintsDrams()
        {
            var (zone, player, trader) = Market();
            TradeSystem.SetDrams(player, 100);
            var coin = factory.CreateEntity("GoldCoin"); trader.GetPart<InventoryPart>().AddObject(coin);
            if (TradeSystem.BuyFromTrader(player, trader, coin, out _))
            {
                player.GetPart<InventoryPart>().RemoveObject(coin); zone.AddEntity(coin, 20, 12);
                InventorySystem.ExecuteCommand(new PickupCommand(coin), player, zone);
            }
            Assert.LessOrEqual(TradeSystem.GetDrams(player), 100, "a coin bought and picked up again paid out more than it cost");
        }

        /// <summary>Counter-check: a coin found on the ground still pays its credit.</summary>
        [Test] public void AFoundCoinStillPaysItsCredit()
        {
            var (zone, player, _) = Market();
            TradeSystem.SetDrams(player, 100);
            var coin = factory.CreateEntity("GoldCoin"); zone.AddEntity(coin, 20, 12);
            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(coin), player, zone).Success);
            Assert.Greater(TradeSystem.GetDrams(player), 100);
        }

        /// <summary>A coin cannot be bought or sold, and no trader's stock table rolls one; the veto
        /// also covers coins a trader carries some other way (a loadout).</summary>
        [Test] public void ACoinIsMoneyNotMerchandise()
        {
            var (_, player, trader) = Market();
            var coin = factory.CreateEntity("GoldCoin");
            Assert.False(TradeSystem.CanBeTraded(coin, player, trader, "Buy"));
            Assert.False(TradeSystem.CanBeTraded(coin, player, trader, "Sell"));
            Assert.True(TradeSystem.CanBeTraded(factory.CreateEntity("Starapple"), player, trader, "Buy"), "counter-check: goods trade");
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(t => t.text).ToList());
            var stock = factory.Blueprints.Values.Where(b => b.Parts.ContainsKey("Trader")).Select(b => factory.CreateEntity(b.Name)?.GetPart<TraderPart>()?.StockTable)
                .Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
            bool Rolls(string table, System.Collections.Generic.HashSet<string> seen)
            {
                var t = seen.Add(table) ? LootTableRegistry.Get(table) : null;
                return t != null && t.Entries.Any(e => e.Blueprint == "GoldCoin" || e.TableRef != null && Rolls(e.TableRef, seen));
            }
            Assert.Greater(stock.Count, 10, "precondition: the traders' stock tables were found");
            CollectionAssert.IsEmpty(stock.Where(t => Rolls(t, new System.Collections.Generic.HashSet<string>())).ToList(), "a shop shelf offers coins");
        }

        /// <summary>EC2b: drams move only through the ledger, a trade or an inventory transaction. A purse is
        /// otherwise written only where it is first set up (a trader's opening purse, a generated indexer's empty
        /// one, the bootstrap elder). A new direct write fails here, naming its file.</summary>
        [Test] public void NoCodeWritesAPurseOutsideTheMoneyPaths()
        {
            var allowed = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Gameplay/Economy/TradeSystem.cs", "defines SetDrams; buy and sell" },
                { "Gameplay/Economy/Ledger.cs", "faucets, sinks and payments" },
                { "Gameplay/Inventory/InventoryTransaction.cs", "transactional currency deltas" },
                { "Gameplay/Economy/TraderPart.cs", "a trader's opening purse" },
                { "Gameplay/World/Generation/Builders/CurationReceivingBuilder.cs", "a generated indexer's empty purse" },
                { "Presentation/Bootstrap/GameBootstrap.cs", "the bootstrap elder's purse" },
            };
            var write = new System.Text.RegularExpressions.Regex(@"SetDrams\s*\(|SetIntProperty\s*\(\s*(TradeSystem\.CURRENCY_PROP|""Drams"")|IntProperties\s*\[\s*(TradeSystem\.CURRENCY_PROP|""Drams"")\s*\]\s*=[^=]");
            string root = Path.Combine(Application.dataPath, "Scripts");
            var writers = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Select(f => f.Substring(root.Length + 1).Replace('\\', '/'))
                .Where(f => !f.StartsWith("Scenarios/") && write.IsMatch(File.ReadAllText(Path.Combine(root, f)))).ToList();
            CollectionAssert.IsSubsetOf(writers, allowed.Keys.ToList(), "a purse is written outside the money paths");
            CollectionAssert.Contains(writers, "Gameplay/Economy/Ledger.cs", "counter-check: the scan finds real writers");
        }

        /// <summary>E2. Hypothesis: a conversation reward near the purse's ceiling overflows and the clamp
        /// turns the wrapped negative into zero, wiping the purse.</summary>
        [Test] public void ARewardNearTheCeilingNeverWipesThePurse()
        {
            var player = factory.CreateEntity("Player");
            TradeSystem.SetDrams(player, int.MaxValue - 2);
            ConversationActions.Execute("GiveDrams", null, player, "5");
            Assert.AreEqual(int.MaxValue - 2, TradeSystem.GetDrams(player), "an unpayable reward leaves the purse as it was");
            TradeSystem.SetDrams(player, 10);
            ConversationActions.Execute("GiveDrams", null, player, "5");
            Assert.AreEqual(15, TradeSystem.GetDrams(player), "counter-check: an ordinary reward is paid");
        }
    }
}
