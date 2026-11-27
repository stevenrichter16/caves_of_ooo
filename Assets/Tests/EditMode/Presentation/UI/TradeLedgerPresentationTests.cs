using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CavesOfOoo.Tests
{
    internal static class TradeLedgerTestReader
    {
        internal static Text[] VisibleLabels(TradeUI trade) => trade.GetComponentsInChildren<Text>(false)
            .Where(label => label.isActiveAndEnabled && !string.IsNullOrWhiteSpace(label.text)).ToArray();

        internal static string VisibleText(TradeUI trade) => string.Join("\n", VisibleLabels(trade).Select(label => label.text));
    }

    public sealed class TradeLedgerPresentationTests : FiftyWorldFixture
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject host;
        TradeUI trade;
        Entity merchant;

        [SetUp]
        public void SetupTrade()
        {
            host = new GameObject("Trade ledger test host");
            trade = host.AddComponent<TradeUI>();
            trade.PlayerEntity = Actor;
            trade.CurrentZone = Zone;
            merchant = new Entity { ID = "ledger-merchant-test", BlueprintName = "TestMerchant" };
            merchant.AddPart(new RenderPart { DisplayName = "Tessa, test trader" });
            merchant.AddPart(new InventoryPart());
            merchant.AddPart(new PhysicsPart());
            Assert.True(Zone.AddEntity(merchant, 11, 10));
            TradeSystem.SetDrams(Actor, 137);
            TradeSystem.SetDrams(merchant, 283);
        }

        [TearDown]
        public void CleanupTrade()
        {
            trade?.Close();
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
        }

        private Entity Stock(string blueprint, string name, int units = 1)
        {
            var item = Factory.CreateEntity(blueprint);
            item.GetPart<RenderPart>().DisplayName = name;
            if (units > 1) item.GetPart<StackerPart>().StackCount = units;
            Assert.True(merchant.GetPart<InventoryPart>().AddObject(item));
            return item;
        }

        private static string NormalizeLine(string text) => string.Join(" ", text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

        [Test]
        public void TradeShowsLiveCanvasStockPursesAndActualSpecialsWithoutATilemap()
        {
            var staple = Stock("HealingTonic", "Field remedy");
            var special = Stock("LongSword", "Tessa's silver-edged sword");
            var stock = new MerchantStockPart();
            stock.Specials.Add(special);
            merchant.AddPart(stock);
            Assert.False(MerchantStockService.IsSpecial(merchant, staple));
            Assert.True(MerchantStockService.IsSpecial(merchant, special));

            trade.Open(merchant);

            Assert.True(trade.GetComponentsInChildren<RectTransform>(false).Any(rect => rect.name == "MerchantTrade"));
            var labels = TradeLedgerTestReader.VisibleLabels(trade);
            Assert.That(labels, Is.Not.Empty);
            Assert.True(labels.Any(label => label.text.Contains("Tessa, test trader") && label.font != null && label.font.dynamic));
            string text = TradeLedgerTestReader.VisibleText(trade);
            Assert.That(text, Does.Contain("137").And.Contain("283").And.Contain("Drams").IgnoreCase);
            Assert.That(text, Does.Contain("Field remedy").And.Contain("Tessa's silver-edged sword"));
            Assert.That(text, Does.Contain("Special stock").IgnoreCase);
            trade.Close();
            Assert.That(TradeLedgerTestReader.VisibleLabels(trade), Is.Empty);
        }

        [Test]
        public void DealViewCountsSelectedUnitsWithoutTurningADealIntoABarterOnlyShop()
        {
            var item = Stock("HealingTonic", "Field remedy", 5);
            trade.Open(merchant);
            Assert.False(trade.IsDealMode);
            Assert.True(trade.DealKey(KeyCode.D));
            Assert.True(trade.DealKey(KeyCode.Return));
            Assert.True(trade.DealKey(KeyCode.RightArrow));

            Assert.AreEqual(2, trade.Deal.Taking(item));
            string text = TradeLedgerTestReader.VisibleText(trade);
            Assert.That(text, Does.Match(@"(?i)\b2\s*(?:/|of)\s*5\s+in offer\b"));
            Assert.That(text, Does.Contain("drams").IgnoreCase);
            Assert.AreEqual(137, TradeSystem.GetDrams(Actor));
            Assert.AreEqual(5, item.GetPart<StackerPart>().StackCount);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConfirmationKeepsItsExactRowAndRejectsAChangedQuantity(bool changedQuantity)
        {
            var shown = Stock("HealingTonic", "Field remedy", 3);
            var other = Stock("Dagger", "Unrelated blade");
            trade.Open(merchant);
            typeof(TradeUI).GetMethod("ShowConfirmation", Private).Invoke(trade, null);

            typeof(TradeUI).GetField("_leftCursor", Private).SetValue(trade, 1);
            if (changedQuantity) shown.GetPart<StackerPart>().StackCount = 2;
            typeof(TradeUI).GetMethod("ConfirmTrade", Private).Invoke(trade, null);

            Assert.True(merchant.GetPart<InventoryPart>().Contains(other));
            Assert.That(merchant.GetPart<InventoryPart>().Contains(shown), Is.EqualTo(changedQuantity));
            Assert.That(Actor.GetPart<InventoryPart>().Contains(shown), Is.EqualTo(!changedQuantity));
            if (changedQuantity)
            {
                Assert.AreEqual(137, TradeSystem.GetDrams(Actor));
                StringAssert.Contains("quantity or price changed", TradeLedgerTestReader.VisibleText(trade));
            }
        }

        [Test]
        public void CountedReaderShowsTheMarkedLinePriceAndRestoresTheOpenOffer()
        {
            var item = Stock("HealingTonic", "Field remedy", 5);
            trade.Open(merchant);
            trade.DealKey(KeyCode.D);
            trade.DealKey(KeyCode.Return);
            trade.DealKey(KeyCode.Return);
            var deal = trade.Deal;
            int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(Actor), merchant, 2);
            string text = trade.SelectedDecisionDetails();
            StringAssert.Contains("In offer: 2 of 5", text);
            StringAssert.Contains("Marked line: " + price + " drams", text);
            trade.HideForDecisionReader();
            Assert.That(TradeLedgerTestReader.VisibleLabels(trade), Is.Empty);
            trade.RestoreAfterDecisionReader();
            Assert.That(trade.Deal, Is.SameAs(deal));
            Assert.AreEqual(2, deal.Taking(item));
            Assert.AreEqual(137, TradeSystem.GetDrams(Actor));
        }

        [Test]
        public void WholeStackConfirmationShowsTheFullNameAndCanonicalPriceInSeparateLabels()
        {
            const string name = "A very long field remedy from the western edge of the peat works";
            var item = Stock("HealingTonic", name, 5);
            trade.Open(merchant);
            int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(Actor), merchant);

            typeof(TradeUI).GetMethod("ShowConfirmation", Private).Invoke(trade, null);

            var labels = TradeLedgerTestReader.VisibleLabels(trade);
            Assert.True(labels.Any(label => label.text.IndexOf("Buy item", StringComparison.OrdinalIgnoreCase) >= 0));
            var nameLabel = labels.FirstOrDefault(label => NormalizeLine(label.text).Contains(name));
            Assert.NotNull(nameLabel, "The complete item name must survive the confirmation layout.");
            var priceLabel = labels.FirstOrDefault(label => label.text.IndexOf(price + " drams", StringComparison.OrdinalIgnoreCase) >= 0
                && label.text.IndexOf("whole stack", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.NotNull(priceLabel);
            Assert.AreNotSame(nameLabel, priceLabel);
            Assert.AreEqual(137, TradeSystem.GetDrams(Actor), "The preview takes no payment.");
        }
    }
}
