using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// SP.4 end-to-end verification for <see cref="MerchantShopShowcase"/>.
    /// Drives the trade system the way a player would (find merchant,
    /// pick a stock item, call BuyFromTrader / SellToTrader directly)
    /// and asserts the SP.4 diag substrate captures the expected
    /// `trade/Bought` and `trade/Sold` records.
    ///
    /// Three pinnable contracts:
    ///   1. Buying a stock item records exactly one `trade/Bought`.
    ///   2. Selling a player item records exactly one `trade/Sold`.
    ///   3. SP.2 NoTrade veto: trying to sell the IronKey records
    ///      NO `trade/Sold` (the veto fires before the diag hook).
    ///   4. Counter-check: bumping the merchant without trading
    ///      records 0 trade entries (rules out passive logging).
    ///
    /// Pattern follows the prior 10 scenario diag fixtures shipped
    /// this session (OnHit / Trap / Elemental / CombatHooks /
    /// CombatParity / ThrowableTonics / LockedDoor).
    /// </summary>
    [TestFixture]
    public class MerchantShopShowcaseDiagTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. Buying a stock item records trade/Bought
        // ====================================================================

        [Test]
        public void BuyFirstStockItem_RecordsTradeBought()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new MerchantShopShowcase().Apply(ctx);

            var merchant = FindMerchant(ctx);
            Assert.IsNotNull(merchant, "Showcase must spawn a Merchant.");
            var stock = TradeSystem.GetTraderStock(merchant);
            Assert.IsNotEmpty(stock, "Showcase merchant must have stock.");

            Diag.ResetAll();

            bool ok = TradeSystem.BuyFromTrader(ctx.PlayerEntity, merchant, stock[0]);
            Assert.IsTrue(ok, "Showcase player must afford the first stock item.");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade",
                Kind = "Bought",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count,
                $"Buy must record exactly one trade/Bought entry. Got {records.Count}.");
            Assert.IsTrue(records[0].PayloadJson.Contains("\"price\":"),
                $"Payload must include price. Payload: {records[0].PayloadJson}");
            Assert.IsTrue(records[0].PayloadJson.Contains("\"perf\":"),
                $"Payload must include perf for resistance debugging. " +
                $"Payload: {records[0].PayloadJson}");
        }

        // ====================================================================
        // 2. Selling a player item records trade/Sold
        // ====================================================================

        [Test]
        public void SellInventoryItem_RecordsTradeSold()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new MerchantShopShowcase().Apply(ctx);

            var merchant = FindMerchant(ctx);
            Assert.IsNotNull(merchant);

            // Pick a sellable item from player's inventory.
            // ShortSword is given by the showcase + has no NoTrade tag.
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            var sellable = inv.Objects.FirstOrDefault(e =>
                e != null
                && e.GetPart<CommercePart>() != null
                && !e.HasTag("NoTrade"));
            Assert.IsNotNull(sellable,
                "Showcase must give the player at least one sellable Commerce item.");

            Diag.ResetAll();

            bool ok = TradeSystem.SellToTrader(ctx.PlayerEntity, merchant, sellable);
            Assert.IsTrue(ok, "Showcase merchant has 100 drams; sale must succeed.");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade",
                Kind = "Sold",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count,
                $"Sale must record exactly one trade/Sold. Got {records.Count}.");
        }

        // ====================================================================
        // 3. SP.2 NoTrade veto records NO trade/Sold (veto fires first)
        // ====================================================================

        [Test]
        public void SellNoTradeIronKey_VetoFires_NoTradeSoldRecord()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new MerchantShopShowcase().Apply(ctx);

            var merchant = FindMerchant(ctx);
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            var ironKey = inv.Objects.FirstOrDefault(e =>
                e != null && e.BlueprintName == "IronKey");
            Assert.IsNotNull(ironKey,
                "Showcase must give the player an IronKey.");
            Assert.IsTrue(ironKey.HasTag("NoTrade"),
                "IronKey blueprint must carry NoTrade tag (set in feat/lock-and-key).");

            Diag.ResetAll();

            bool ok = TradeSystem.SellToTrader(ctx.PlayerEntity, merchant, ironKey);
            Assert.IsFalse(ok,
                "NoTrade-tagged IronKey must refuse to be sold (SP.2 veto).");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade",
                Kind = "Sold",
                Limit = 10,
            }).Records;

            Assert.AreEqual(0, records.Count,
                $"SP.2 veto fires BEFORE the SP.4 diag hook — no Sold record " +
                $"should appear for a vetoed sale. Got {records.Count}: " +
                $"[{string.Join(", ", records.Select(r => r.PayloadJson))}]");
        }

        // ====================================================================
        // 4. Counter-check: scenario apply alone produces no trade records
        // ====================================================================

        [Test]
        public void ApplyShowcase_WithoutTrading_ProducesNoTradeDiag()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            Diag.ResetAll();

            new MerchantShopShowcase().Apply(ctx);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade",
                Limit = 10,
            }).Records;

            Assert.AreEqual(0, records.Count,
                $"Building the scenario must NOT produce any trade/* diag " +
                $"records — scenarios shouldn't passively trade. Got {records.Count}.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Finds the first Merchant the showcase spawned (by BlueprintName).
        /// </summary>
        private static Entity FindMerchant(CavesOfOoo.Scenarios.ScenarioContext ctx)
        {
            return ctx.Zone.GetAllEntities()
                .FirstOrDefault(e => e != null
                    && e != ctx.PlayerEntity
                    && e.BlueprintName == "Merchant");
        }
    }
}
