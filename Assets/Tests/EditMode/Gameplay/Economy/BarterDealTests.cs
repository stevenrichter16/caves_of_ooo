using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// EC6b (Docs/ECONOMY-SYSTEM.md §12.3): a deal moves goods both ways in one transaction. With a barter
    /// trader no drams move and what you give must be worth what you take; with a dram trader the difference
    /// is paid in drams. Lines may hold part of a stack, so salt can be small change.
    /// </summary>
    public sealed class BarterDealTests
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
            FactionManager.Initialize(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
            oldTrader = TraderPart.Factory; oldLoadout = LoadoutPart.Factory; TraderPart.Factory = null; LoadoutPart.Factory = null;
        }
        [TearDown] public void TearDown()
        {
            TraderPart.Factory = oldTrader; LoadoutPart.Factory = oldLoadout;
            FactionManager.Reset(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
        }

        static (int, int) Cell(System.Func<int, int, bool> where)
        {
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++) if (where(x, y)) return (x, y);
            Assert.Fail("no such cell"); return (-1, -1);
        }
        static (int, int) Deep => Cell((x, y) => Tender.IsDeepWasteland(WorldMap.ToZoneID(x, y, 0)));
        static (int, int) Spread => Cell((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Spread);

        /// <summary>A trader (stock emptied) and the player beside it, both with 100 drams.</summary>
        static (Entity player, Entity trader) Market((int x, int y) cell)
        {
            var zone = new Zone(WorldMap.ToZoneID(cell.x, cell.y, 0));
            var trader = factory.CreateEntity("Merchant"); zone.AddEntity(trader, 10, 10);
            trader.GetPart<InventoryPart>().Objects.Clear();
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 11, 10);
            TradeSystem.SetDrams(player, 100); TradeSystem.SetDrams(trader, 100);
            // A shrewd trader (performance 0.95), so goods-for-goods spreads are narrow enough to reason about.
            var ego = player.GetStat("Ego"); ego.Max = System.Math.Max(ego.Max, 40); ego.BaseValue = 34;
            Assert.AreEqual(0.95, TradeSystem.GetTradePerformance(player), 1e-9, "precondition: performance");
            return (player, trader);
        }
        static Entity Give(Entity holder, string blueprint, int count = 1)
        {
            var item = factory.CreateEntity(blueprint);
            var stacker = item.GetPart<StackerPart>(); if (stacker != null) stacker.StackCount = count;
            Assert.True(holder.GetPart<InventoryPart>().AddObject(item), "precondition: holder carries " + blueprint);
            return item;
        }
        static int Held(Entity holder, string blueprint) =>
            holder.GetPart<InventoryPart>().Objects.Where(o => o.BlueprintName == blueprint).Sum(o => BarterDeal.Units(o));

        // ════════════════ pricing of part of a stack ════════════════

        [Test] public void PartOfAStackIsPricedInProportionAndTheWholeStackAsBefore()
        {
            var (player, trader) = Market(Spread);
            var salt = Give(trader, "PaleSalt", 10);
            double perf = TradeSystem.GetTradePerformance(player);
            Assert.AreEqual(TradeSystem.GetBuyPrice(salt, perf, trader), TradeSystem.GetBuyPrice(salt, perf, trader, 10), "the whole stack");
            Assert.AreEqual(TradeSystem.GetSellPrice(salt, perf, trader), TradeSystem.GetSellPrice(salt, perf, trader, 10));
            Assert.Less(TradeSystem.GetBuyPrice(salt, perf, trader, 3), TradeSystem.GetBuyPrice(salt, perf, trader, 10));
            Assert.AreEqual(0, TradeSystem.GetBuyPrice(salt, perf, trader, 0)); Assert.AreEqual(0, TradeSystem.GetItemValue(salt, -2));
        }

        // ════════════════ the barter tender ════════════════

        [Test] public void ABarterMovesGoodsBothWaysAndNoDrams()
        {
            var (player, trader) = Market(Deep);
            Assert.AreEqual(TenderKind.Barter, Tender.For(trader), "precondition");
            var sword = Give(player, "LongSword"); var tonic = Give(trader, "HealingTonic");
            var deal = new BarterDeal(player, trader);
            Assert.True(deal.SetGive(sword, 1)); Assert.True(deal.SetTake(tonic, 1));
            Assert.GreaterOrEqual(deal.Balance, 0, "precondition: a sword is worth a tonic");
            Assert.AreEqual(deal.Balance, deal.Surplus, "the trader keeps what is over");
            Assert.IsNull(deal.Refusal());
            Assert.True(Barter.TryDeal(deal, out string reason), reason);
            Assert.True(trader.GetPart<InventoryPart>().Contains(sword)); Assert.True(player.GetPart<InventoryPart>().Contains(tonic));
            Assert.AreEqual(100, TradeSystem.GetDrams(player), "no drams move in barter"); Assert.AreEqual(100, TradeSystem.GetDrams(trader));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "Bartered", Limit = 5 }).Records.Count);
        }

        [Test] public void AShortBarterIsRefusedAndChangesNothing()
        {
            var (player, trader) = Market(Deep);
            var apple = Give(player, "Starapple"); var sword = Give(trader, "LongSword");
            var deal = new BarterDeal(player, trader);
            deal.SetGive(apple, 1); deal.SetTake(sword, 1);
            Assert.Less(deal.Balance, 0, "precondition: an apple is not worth a sword");
            Assert.AreEqual("short", deal.Refusal());
            Assert.False(Barter.TryDeal(deal, out string reason)); StringAssert.Contains("Not enough", reason);
            Assert.True(player.GetPart<InventoryPart>().Contains(apple)); Assert.True(trader.GetPart<InventoryPart>().Contains(sword));
            Assert.AreEqual(100, TradeSystem.GetDrams(player));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "BarterRejected", Limit = 5 }).Records.Count);
        }

        [Test] public void PartOfAStackSplitsAndUnitsAreConserved()
        {
            var (player, trader) = Market(Deep);
            var salt = Give(player, "PaleSalt", 10); var apple = Give(trader, "Starapple");
            var deal = new BarterDeal(player, trader);
            Assert.True(deal.SetGive(salt, 4)); deal.SetTake(apple, 1);
            Assume.That(deal.Balance >= 0, "four salt buy an apple");
            Assert.True(Barter.TryDeal(deal, out string reason), reason);
            Assert.AreEqual(6, Held(player, "PaleSalt")); Assert.AreEqual(4, Held(trader, "PaleSalt"));
            Assert.True(player.GetPart<InventoryPart>().Contains(salt), "the remainder stays the same stack");
        }

        [Test] public void AnEmptyDealIsRefused()
        {
            var (player, trader) = Market(Deep);
            var deal = new BarterDeal(player, trader);
            Assert.True(deal.IsEmpty); Assert.AreEqual("empty", deal.Refusal());
            Assert.False(Barter.TryDeal(deal, out _));
        }

        /// <summary>A gift is a deal too: giving something for nothing is accepted (the trader keeps it all).</summary>
        [Test] public void GivingForNothingIsAcceptedAndTakingForNothingIsNot()
        {
            var (player, trader) = Market(Deep);
            var apple = Give(player, "Starapple"); var tonic = Give(trader, "HealingTonic");
            var gift = new BarterDeal(player, trader); gift.SetGive(apple, 1);
            Assert.IsNull(gift.Refusal()); Assert.True(Barter.TryDeal(gift, out _));
            var theft = new BarterDeal(player, trader); theft.SetTake(tonic, 1);
            Assert.AreEqual("short", theft.Refusal()); Assert.False(Barter.TryDeal(theft, out _));
            Assert.True(trader.GetPart<InventoryPart>().Contains(tonic));
        }

        // ════════════════ the drams tender ════════════════

        [Test] public void ADramDealSettlesTheDifferenceEitherWay()
        {
            var (player, trader) = Market(Spread);
            var apple = Give(player, "Starapple"); var sword = Give(trader, "LongSword");
            var deal = new BarterDeal(player, trader); deal.SetGive(apple, 1); deal.SetTake(sword, 1);
            int owed = deal.DramsFromPlayer;
            Assert.Greater(owed, 0); Assert.AreEqual(-deal.Balance, owed); Assert.AreEqual(0, deal.Surplus, "a dram trader keeps nothing over");
            Assert.True(Barter.TryDeal(deal, out string reason), reason);
            Assert.AreEqual(100 - owed, TradeSystem.GetDrams(player)); Assert.AreEqual(100 + owed, TradeSystem.GetDrams(trader));

            var tonic = Give(player, "HealingTonic"); var mine = sword;
            var back = new BarterDeal(player, trader); back.SetGive(mine, 1); back.SetGive(tonic, 1); back.SetTake(apple, 1);
            int paid = back.DramsToPlayer; int before = TradeSystem.GetDrams(player);
            Assert.Greater(paid, 0, "counter-check: the trader pays what is over");
            Assert.True(Barter.TryDeal(back, out reason), reason);
            Assert.AreEqual(before + paid, TradeSystem.GetDrams(player));
        }

        [TestCase(true)] [TestCase(false)]
        public void ADramDealThatCannotBePaidChangesNothing(bool playerPays)
        {
            var (player, trader) = Market(Spread);
            var deal = new BarterDeal(player, trader);
            var placed = new List<(Entity holder, Entity item)>();
            if (playerPays)
            {
                var apple = Give(player, "Starapple"); var sword = Give(trader, "LongSword");
                deal.SetGive(apple, 1); deal.SetTake(sword, 1); TradeSystem.SetDrams(player, 0);
                placed.Add((player, apple)); placed.Add((trader, sword));
            }
            else
            {
                var sword = Give(player, "LongSword");
                deal.SetGive(sword, 1); TradeSystem.SetDrams(trader, 0);
                placed.Add((player, sword));
            }
            Assert.AreEqual(playerPays ? "cannot_afford" : "trader_cannot_afford", deal.Refusal());
            int p = TradeSystem.GetDrams(player), t = TradeSystem.GetDrams(trader);
            Assert.False(Barter.TryDeal(deal, out _));
            Assert.AreEqual(p, TradeSystem.GetDrams(player)); Assert.AreEqual(t, TradeSystem.GetDrams(trader));
            foreach (var (holder, item) in placed) Assert.True(holder.GetPart<InventoryPart>().Contains(item), item.BlueprintName + " stayed put");
        }

        // ════════════════ atomicity ════════════════

        /// <summary>A take the player cannot carry undoes every line already moved, including a split stack.</summary>
        [Test] public void AnOverweightTakeUndoesEveryLine()
        {
            var (player, trader) = Market(Deep);
            var salt = Give(player, "PaleSalt", 10); var sword = Give(player, "LongSword"); var tonic = Give(trader, "HealingTonic");
            var deal = new BarterDeal(player, trader); deal.SetGive(salt, 4); deal.SetGive(sword, 1); deal.SetTake(tonic, 1);
            Assume.That(deal.Refusal() == null, "value is not what refuses this deal");
            player.GetPart<InventoryPart>().MaxWeight = 0;
            Assert.False(Barter.TryDeal(deal, out string reason)); StringAssert.Contains("weight", reason);
            Assert.AreEqual(10, Held(player, "PaleSalt")); Assert.AreEqual(10, BarterDeal.Units(salt), "the split stack is whole again");
            Assert.True(player.GetPart<InventoryPart>().Contains(sword)); Assert.AreEqual(0, Held(trader, "PaleSalt"));
            Assert.True(trader.GetPart<InventoryPart>().Contains(tonic)); Assert.False(trader.GetPart<InventoryPart>().Contains(sword));
        }

        [Test] public void ALineWhoseItemIsGoneRefusesTheDeal()
        {
            var (player, trader) = Market(Deep);
            var sword = Give(player, "LongSword"); var tonic = Give(trader, "HealingTonic");
            var deal = new BarterDeal(player, trader); deal.SetGive(sword, 1); deal.SetTake(tonic, 1);
            trader.GetPart<InventoryPart>().RemoveObject(tonic);
            Assert.False(Barter.TryDeal(deal, out _));
            Assert.True(player.GetPart<InventoryPart>().Contains(sword), "nothing given for a vanished good");
        }

        // ════════════════ what can join a deal ════════════════

        [Test] public void CountsClampToTheStackAndZeroRemovesTheLine()
        {
            var (player, trader) = Market(Deep);
            var salt = Give(player, "PaleSalt", 5);
            var deal = new BarterDeal(player, trader);
            Assert.True(deal.SetGive(salt, 99)); Assert.AreEqual(5, deal.Giving(salt));
            Assert.True(deal.SetGive(salt, 0)); Assert.AreEqual(0, deal.Giving(salt)); Assert.True(deal.IsEmpty);
            Assert.False(deal.SetGive(salt, -3), "nothing changes"); Assert.IsEmpty(deal.Gives);
        }

        [Test] public void OnlyWhatEachSideHoldsAndMayTradeJoinsADeal()
        {
            var (player, trader) = Market(Deep);
            var mine = Give(player, "Starapple"); var theirs = Give(trader, "HealingTonic");
            var coin = factory.CreateEntity("GoldCoin"); player.GetPart<InventoryPart>().AddObject(coin);
            var deal = new BarterDeal(player, trader);
            Assert.False(deal.SetGive(theirs, 1), "you cannot give what the trader holds");
            Assert.False(deal.SetTake(mine, 1), "nor take what you hold");
            Assert.False(deal.SetGive(coin, 1), "a NoTrade item never joins");
            Assert.True(deal.SetGive(mine, 1), "counter-check: your own goods join");
        }

        [Test] public void TheChoirMakesNoDeals()
        {
            var zone = new Zone(WorldMap.ToZoneID(Spread.Item1, Spread.Item2, 0));
            var tendril = factory.CreateEntity("ChoirTendril"); zone.AddEntity(tendril, 10, 10);
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 11, 10);
            var apple = Give(player, "Starapple");
            var deal = new BarterDeal(player, tendril);
            Assert.AreEqual(TenderKind.None, deal.Tender);
            Assert.False(deal.SetGive(apple, 1));
            Assert.AreEqual("no_shop", deal.Refusal()); Assert.False(Barter.TryDeal(deal, out _));
        }

        // ════════════════ salt as change ════════════════

        [Test] public void SaltClosesAShortDealWithTheFewestUnits()
        {
            var (player, trader) = Market(Deep);
            var salt = Give(player, "PaleSalt", 40); var sword = Give(trader, "LongSword");
            var deal = new BarterDeal(player, trader); deal.SetTake(sword, 1);
            Assert.Less(deal.Balance, 0, "precondition");
            int added = deal.BalanceWithSalt();
            Assert.Greater(added, 0); Assert.GreaterOrEqual(deal.Balance, 0, "closed");
            deal.SetGive(salt, deal.Giving(salt) - 1);
            Assert.Less(deal.Balance, 0, "one unit fewer would not do: the fewest units");
        }

        [Test] public void SaltMakesChangeWhenYouGiveTooMuch()
        {
            var (player, trader) = Market(Deep);
            var grimoire = Give(player, "KindleGrimoire"); var apple = Give(trader, "Starapple"); var salt = Give(trader, "PaleSalt", 40);
            var deal = new BarterDeal(player, trader); deal.SetGive(grimoire, 1); deal.SetTake(apple, 1);
            int over = deal.Balance; Assert.Greater(over, 0, "precondition");
            Assert.Greater(deal.BalanceWithSalt(), 0);
            Assert.GreaterOrEqual(deal.Balance, 0, "still acceptable"); Assert.Less(deal.Balance, over, "change was made");
            deal.SetTake(salt, deal.Taking(salt) + 1);
            Assert.Less(deal.Balance, 0, "one more unit would be too many");
        }

        /// <summary>Balancing first returns salt already in the deal on the wrong side.</summary>
        [Test] public void BalancingReturnsSaltBeforeAddingMore()
        {
            var (player, trader) = Market(Deep);
            var mine = Give(player, "PaleSalt", 30); var apple = Give(trader, "Starapple");
            var deal = new BarterDeal(player, trader); deal.SetGive(mine, 30); deal.SetTake(apple, 1);
            Assert.Greater(deal.Balance, 0, "precondition: thirty salt for an apple is far too much");
            deal.BalanceWithSalt();
            Assert.Less(deal.Giving(mine), 30, "your own salt comes back first");
            Assert.GreaterOrEqual(deal.Balance, 0);
        }

        [Test] public void WithoutSaltBalancingChangesNothing()
        {
            var (player, trader) = Market(Deep);
            var sword = Give(trader, "LongSword");
            var deal = new BarterDeal(player, trader); deal.SetTake(sword, 1);
            Assert.AreEqual(0, deal.BalanceWithSalt()); Assert.AreEqual("short", deal.Refusal());
        }
    }
}
