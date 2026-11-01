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
    /// Adversarial sweep for EC6 (barter) and EC7 (Concord credit), Docs/ECONOMY-SYSTEM.md §12. Surfaces:
    /// state atomicity and rollback (merges, splits, trader capacity), stale deals (prices, stacks, places that
    /// changed after the deal was composed), boundary inputs, currency overflow, parser inputs, anti-exploit
    /// rates, save/load reach and diag invariants.
    /// </summary>
    public sealed class BarterCreditAdversarialTests
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

        static (Zone zone, Entity player, Entity trader) Market((int x, int y) cell)
        {
            var zone = new Zone(WorldMap.ToZoneID(cell.x, cell.y, 0));
            var trader = factory.CreateEntity("Merchant"); zone.AddEntity(trader, 10, 10);
            trader.GetPart<InventoryPart>().Objects.Clear();
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 11, 10);
            var ego = player.GetStat("Ego"); ego.Max = System.Math.Max(ego.Max, 40); ego.BaseValue = 34;
            TradeSystem.SetDrams(player, 100); TradeSystem.SetDrams(trader, 100);
            return (zone, player, trader);
        }
        static Entity Give(Entity holder, string blueprint, int count = 1)
        {
            var item = factory.CreateEntity(blueprint);
            var stacker = item.GetPart<StackerPart>(); if (stacker != null) stacker.StackCount = count;
            Assert.True(holder.GetPart<InventoryPart>().AddObject(item));
            return item;
        }
        static int Held(Entity holder, string blueprint) =>
            holder.GetPart<InventoryPart>().Objects.Where(o => o.BlueprintName == blueprint).Sum(o => BarterDeal.Units(o));
        static int Count(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = kind, Limit = 50 }).Records.Count;

        // ════════════════ atomicity and rollback ════════════════

        /// <summary>A trader too laden to take the player's goods undoes the split and the merge.</summary>
        [Test] public void Adversarial_TraderCapacityUndoesASplitThatWouldHaveMerged()
        {
            var (_, player, trader) = Market(Deep);
            var mine = Give(player, "PaleSalt", 8); var theirs = Give(trader, "PaleSalt", 5); var sword = Give(player, "LongSword");
            var deal = new BarterDeal(player, trader); deal.SetGive(mine, 3); deal.SetGive(sword, 1);
            trader.GetPart<InventoryPart>().MaxWeight = 0;
            Assert.False(Barter.TryDeal(deal, out string reason)); StringAssert.Contains("weight", reason);
            Assert.AreEqual(8, BarterDeal.Units(mine)); Assert.AreEqual(5, BarterDeal.Units(theirs));
            Assert.AreEqual(8, Held(player, "PaleSalt")); Assert.AreEqual(5, Held(trader, "PaleSalt"));
            Assert.True(player.GetPart<InventoryPart>().Contains(sword));
        }

        /// <summary>A successful partial give merges into the trader's existing salt; units are conserved.</summary>
        [Test] public void Adversarial_APartialGiveMergesAndConservesUnits()
        {
            var (_, player, trader) = Market(Deep);
            var mine = Give(player, "PaleSalt", 8); Give(trader, "PaleSalt", 5);
            var deal = new BarterDeal(player, trader); deal.SetGive(mine, 3);
            Assert.True(Barter.TryDeal(deal, out string reason), reason);
            Assert.AreEqual(5, Held(player, "PaleSalt")); Assert.AreEqual(8, Held(trader, "PaleSalt"));
            Assert.AreEqual(13, Held(player, "PaleSalt") + Held(trader, "PaleSalt"));
        }

        [Test] public void Adversarial_ADramDealThatWouldOverflowThePurseChangesNothing()
        {
            var (_, player, trader) = Market(Spread);
            var grimoire = Give(player, "KindleGrimoire");
            TradeSystem.SetDrams(player, int.MaxValue - 1); TradeSystem.SetDrams(trader, 1000);
            var deal = new BarterDeal(player, trader); deal.SetGive(grimoire, 1);
            Assert.False(Barter.TryDeal(deal, out string reason)); StringAssert.Contains("currency", reason);
            Assert.AreEqual(int.MaxValue - 1, TradeSystem.GetDrams(player)); Assert.AreEqual(1000, TradeSystem.GetDrams(trader));
            Assert.True(player.GetPart<InventoryPart>().Contains(grimoire));
        }

        // ════════════════ stale deals ════════════════

        /// <summary>Prices are read at the moment of offer: a deal composed while shrewd is short once the player
        /// is not, and nothing moves.</summary>
        [Test] public void Adversarial_ADealIsRepricedWhenItIsOffered()
        {
            var (_, player, trader) = Market(Deep);
            var sword = Give(player, "LongSword"); var tonic = Give(trader, "HealingTonic");
            var deal = new BarterDeal(player, trader); deal.SetGive(sword, 1); deal.SetTake(tonic, 1);
            Assert.GreaterOrEqual(deal.Balance, 0, "precondition: acceptable while shrewd");
            player.GetStat("Ego").BaseValue = 10;
            Assert.False(Barter.TryDeal(deal, out string reason)); StringAssert.Contains("Not enough", reason);
            Assert.True(player.GetPart<InventoryPart>().Contains(sword)); Assert.True(trader.GetPart<InventoryPart>().Contains(tonic));
        }

        /// <summary>A trader who moved into the deep wasteland after the deal was composed takes no drams for it.</summary>
        [Test] public void Adversarial_ATenderChangeIsReadWhenTheDealIsOffered()
        {
            var (zone, player, trader) = Market(Spread);
            var apple = Give(player, "Starapple"); var sword = Give(trader, "LongSword");
            var deal = new BarterDeal(player, trader); deal.SetGive(apple, 1); deal.SetTake(sword, 1);
            Assert.Greater(deal.DramsFromPlayer, 0, "precondition: composed as a dram deal");
            zone.RemoveEntity(trader); zone.RemoveEntity(player);
            var deep = new Zone(WorldMap.ToZoneID(Deep.Item1, Deep.Item2, 0)); deep.AddEntity(trader, 10, 10); deep.AddEntity(player, 11, 10);
            Assert.False(Barter.TryDeal(deal, out _));
            Assert.AreEqual(100, TradeSystem.GetDrams(player), "no drams change hands in the deep wasteland");
            Assert.True(trader.GetPart<InventoryPart>().Contains(sword));
        }

        [Test] public void Adversarial_AStackThatShrankAfterMarkingRefusesTheDeal()
        {
            var (_, player, trader) = Market(Deep);
            var salt = Give(player, "PaleSalt", 10); var apple = Give(trader, "Starapple");
            var deal = new BarterDeal(player, trader); deal.SetGive(salt, 10); deal.SetTake(apple, 1);
            salt.GetPart<StackerPart>().StackCount = 4;
            Assert.False(Barter.TryDeal(deal, out _));
            Assert.AreEqual(4, Held(player, "PaleSalt")); Assert.True(trader.GetPart<InventoryPart>().Contains(apple));
        }

        [Test] public void Adversarial_AGiveThatLeftThePackRefusesTheDeal()
        {
            var (zone, player, trader) = Market(Deep);
            var sword = Give(player, "LongSword"); var tonic = Give(trader, "HealingTonic");
            var deal = new BarterDeal(player, trader); deal.SetGive(sword, 1); deal.SetTake(tonic, 1);
            player.GetPart<InventoryPart>().RemoveObject(sword); zone.AddEntity(sword, 11, 11);
            Assert.False(Barter.TryDeal(deal, out _));
            Assert.True(trader.GetPart<InventoryPart>().Contains(tonic), "nothing taken for a good no longer offered");
        }

        [Test] public void Adversarial_ADeadTraderMakesNoDeal()
        {
            var (_, player, trader) = Market(Deep);
            var sword = Give(player, "LongSword");
            var deal = new BarterDeal(player, trader); deal.SetGive(sword, 1);
            trader.GetStat("Hitpoints").BaseValue = 0;
            Assert.False(Barter.TryDeal(deal, out string reason)); StringAssert.Contains("dead", reason);
            Assert.True(player.GetPart<InventoryPart>().Contains(sword));
        }

        // ════════════════ boundary inputs ════════════════

        [Test] public void Adversarial_NullsNeverThrow()
        {
            Assert.False(Barter.TryDeal(null, out _));
            var deal = new BarterDeal(null, null);
            Assert.AreEqual(TenderKind.None, deal.Tender); Assert.False(deal.SetGive(null, 1)); Assert.AreEqual(0, deal.BalanceWithSalt());
            Assert.False(Barter.TryDeal(deal, out _));
            var (_, player, trader) = Market(Deep);
            Assert.False(new BarterDeal(player, trader).SetTake(null, 3));
            Assert.AreEqual("missing_participant", ConcordAccount.Borrow(null, player, 10, 0));
            Assert.AreEqual("missing_participant", ConcordAccount.Repay(factory.CreateEntity("ConcordFactor"), null, 10, 0));
            Assert.False(ConcordAccount.Review(null, 0)); Assert.AreEqual(0, ConcordAccount.Line(null));
            ConcordAccount.AppendChoices(null, null, null, "Start");
            Assert.AreEqual("x", ConcordAccount.AppendText("x", null, null, "Start", 0));
        }

        [Test] public void Adversarial_EachRefusalRecordsExactlyOneRejection()
        {
            var (_, player, trader) = Market(Deep);
            var sword = Give(trader, "LongSword");
            var deal = new BarterDeal(player, trader);
            Barter.TryDeal(deal, out _);                    // empty
            deal.SetTake(sword, 1); Barter.TryDeal(deal, out _); // short
            Assert.AreEqual(2, Count("BarterRejected")); Assert.AreEqual(0, Count("Bartered"));
            var mine = Give(player, "KindleGrimoire"); deal.SetGive(mine, 1);
            Assert.True(Barter.TryDeal(deal, out string reason), reason);
            Assert.AreEqual(1, Count("Bartered")); Assert.AreEqual(2, Count("BarterRejected"), "success adds no rejection");
        }

        // ════════════════ salt balancing ════════════════

        /// <summary>Balancing moves only salt, and never turns an acceptable deal into an unacceptable one.</summary>
        [Test] public void Adversarial_BalancingTouchesOnlySaltAndKeepsAnAcceptableDealAcceptable()
        {
            var (_, player, trader) = Market(Deep);
            var grimoire = Give(player, "KindleGrimoire"); var apple = Give(trader, "Starapple"); var tonic = Give(trader, "HealingTonic");
            Give(trader, "PaleSalt", 60); Give(player, "PaleSalt", 60);
            var deal = new BarterDeal(player, trader); deal.SetGive(grimoire, 1); deal.SetTake(apple, 1);
            deal.BalanceWithSalt();
            Assert.AreEqual(1, deal.Giving(grimoire)); Assert.AreEqual(1, deal.Taking(apple)); Assert.AreEqual(0, deal.Taking(tonic));
            Assert.GreaterOrEqual(deal.Balance, 0);
            Assert.True(deal.Gives.Concat(deal.Takes).All(l => l.Item == grimoire || l.Item == apple || l.Item.HasTag(RegionalValue.SaltTag)));
            int moved = deal.BalanceWithSalt();
            Assert.GreaterOrEqual(deal.Balance, 0, "a second press keeps it acceptable");
        }

        // ════════════════ the Concord's account ════════════════

        [Test] public void Adversarial_BorrowingIntoAFullPurseOpensNoAccount()
        {
            var player = factory.CreateEntity("Player"); TradeSystem.SetDrams(player, int.MaxValue - 5);
            Assert.NotNull(ConcordAccount.Borrow(factory.CreateEntity("ConcordFactor"), player, 20, 0));
            Assert.AreEqual(0, ConcordAccount.Debt(player)); Assert.AreEqual(int.MaxValue - 5, TradeSystem.GetDrams(player));
        }

        [Test] public void Adversarial_RepayingMoreThanOwedTakesOnlyTheDebt()
        {
            var player = factory.CreateEntity("Player"); TradeSystem.SetDrams(player, 500);
            var factor = factory.CreateEntity("ConcordFactor");
            ConcordAccount.Borrow(factor, player, 20, 0);
            Assert.IsNull(ConcordAccount.Repay(factor, player, 10000, 1));
            Assert.AreEqual(500 + 20 - 24, TradeSystem.GetDrams(player));
            Assert.AreEqual("no_debt", ConcordAccount.Repay(factor, player, 5, 2));
        }

        /// <summary>Anti-farm: standing from honoured debts is bounded by one account at a time and a day carried.</summary>
        [Test] public void Adversarial_HonourCannotBeFarmedFasterThanOncePerDay()
        {
            var player = factory.CreateEntity("Player"); TradeSystem.SetDrams(player, 10000);
            var factor = factory.CreateEntity("ConcordFactor");
            for (int t = 0; t < 50; t++) { ConcordAccount.Borrow(factor, player, 10, t * 10); ConcordAccount.Repay(factor, player, 100, t * 10 + 5); }
            Assert.AreEqual(0, ConcordAccount.Standing, "fifty quick loops in one day earn nothing");
            int day = WorldClock.DayLengthTicks;
            for (int d = 1; d <= 3; d++) { ConcordAccount.Borrow(factor, player, 10, d * 2 * day); ConcordAccount.Repay(factor, player, 100, d * 2 * day + day); }
            Assert.AreEqual(3 * ConcordAccount.Honor, ConcordAccount.Standing, "counter-check: a debt carried a day is honoured each time");
        }

        [Test] public void Adversarial_DefaultingRepeatedlyKeepsCostingStanding()
        {
            var player = factory.CreateEntity("Player"); TradeSystem.SetDrams(player, 10000);
            var factor = factory.CreateEntity("ConcordFactor");
            int now = 0;
            for (int i = 0; i < 2; i++)
            {
                Assert.IsNull(ConcordAccount.Borrow(factor, player, 5, now));
                now += ConcordAccount.TermTicks + 1;
                Assert.IsNull(ConcordAccount.Repay(factor, player, 100, now));
            }
            Assert.AreEqual(-2 * ConcordAccount.DefaultPenalty, ConcordAccount.Standing);
        }

        [TestCase(" 10 ", true)] [TestCase("10.5", false)] [TestCase("99999999999", false)] [TestCase("0x10", false)]
        public void Adversarial_TheAmountParserAcceptsOnlyWholeNumbers(string argument, bool parses)
        {
            ConversationActions.Reset();
            var player = factory.CreateEntity("Player");
            string result = ConversationActions.ExecuteRequired(ConcordAccount.BorrowAction, factory.CreateEntity("ConcordFactor"), player, argument);
            if (parses) Assert.IsNull(result, "a padded whole number is a number");
            else { Assert.AreEqual("invalid_argument", result); Assert.AreEqual(0, ConcordAccount.Debt(player)); }
        }

        [Test] public void Adversarial_TheStatementNeverThrowsOnAStrangeAccount()
        {
            var player = factory.CreateEntity("Player");
            player.SetIntProperty(ConcordAccount.DebtProp, 7); player.SetIntProperty(ConcordAccount.DueProp, -999999);
            StringAssert.Contains("overdue", ConcordAccount.Statement(player, 0));
            player.SetIntProperty(ConcordAccount.DueProp, int.MaxValue);
            Assert.DoesNotThrow(() => ConcordAccount.Statement(player, int.MinValue + 1));
        }

        /// <summary>Credit is a net money sink: what is borrowed and repaid on time leaves the Concord its fee.</summary>
        [Test] public void Adversarial_CreditIsANetSinkThroughTheLedger()
        {
            var player = factory.CreateEntity("Player"); TradeSystem.SetDrams(player, 100);
            var factor = factory.CreateEntity("ConcordFactor");
            ConcordAccount.Borrow(factor, player, 20, 0); ConcordAccount.Repay(factor, player, 24, WorldClock.DayLengthTicks + 1);
            var credited = DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "Credited", Limit = 10 }).Records;
            var debited = DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "Debited", Limit = 10 }).Records;
            Assert.True(credited.Any(r => r.PayloadJson.Contains("concord-credit")));
            Assert.True(debited.Any(r => r.PayloadJson.Contains("concord-repayment")));
            Assert.AreEqual(96, TradeSystem.GetDrams(player));
        }
    }
}
