using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// TRADE STOCK FIX — user-reported: "merchants and saccharine
    /// envoys out in the world do not have inventory when you trade."
    ///
    /// <para>Root cause: <c>ConversationManager</c> injected the
    /// "[Let's trade.]" choice for any speaker with an
    /// <see cref="InventoryPart"/> — which EVERY creature inherits from
    /// the <c>Creature</c> blueprint — so all 31 talkable NPCs offered
    /// trade while only the five town shopkeepers (stocked by the
    /// <c>shop:</c> stamp marker) ever had goods.</para>
    /// </summary>
    [TestFixture]
    public class TraderStockTests
    {
        private const string Blueprints = @"
        {
          ""Objects"": [
            { ""Name"": ""PhysicalObject"", ""Parts"": [] },
            { ""Name"": ""Item"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" }, { ""Key"": ""Weight"", ""Value"": ""1"" } ] } ] },
            { ""Name"": ""HealingTonic"", ""Inherits"": ""Item"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""healing tonic"" }, { ""Key"": ""RenderString"", ""Value"": ""!"" } ] } ] },
            { ""Name"": ""Creature"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                { ""Name"": ""Inventory"", ""Params"": [ { ""Key"": ""MaxWeight"", ""Value"": ""150"" } ] } ] },
            { ""Name"": ""Merchant"", ""Inherits"": ""Creature"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""merchant"" }, { ""Key"": ""RenderString"", ""Value"": ""@"" } ] },
                { ""Name"": ""Trader"", ""Params"": [
                    { ""Key"": ""StockTable"", ""Value"": ""MerchantStock"" },
                    { ""Key"": ""Drams"", ""Value"": ""150"" } ] } ] },
            { ""Name"": ""Beast"", ""Inherits"": ""Creature"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""beast"" }, { ""Key"": ""RenderString"", ""Value"": ""b"" } ] } ] }
          ]
        }";

        private const string Tables = @"
        { ""Tables"": [
            { ""Name"": ""MerchantStock"", ""Entries"": [ { ""Blueprint"": ""HealingTonic"", ""Chance"": 100, ""MinCount"": 2, ""MaxCount"": 2 } ] }
        ] }";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(Blueprints);
            LootTableRegistry.Initialize(Tables);
            TraderPart.Factory = _factory;
            TraderPart.Rng = new System.Random(5);
        }

        [TearDown]
        public void TearDown()
        {
            TraderPart.Factory = null;
            TraderPart.Rng = null;
        }

        [Test]
        public void Merchant_SpawnsWithStockOnTheShelf()
        {
            // The reported bug, inverted into an invariant.
            var merchant = _factory.CreateEntity("Merchant");
            var inv = merchant.GetPart<InventoryPart>();

            Assert.AreEqual(2, inv.Objects.Count,
                "a merchant opens the trade window with goods, not an empty list");
            Assert.AreEqual("HealingTonic", inv.Objects[0].BlueprintName);
        }

        [Test]
        public void Merchant_GetsAPurse_SoRestockAndSellingBothWork()
        {
            // TraderRestockSystem SKIPS any entity whose drams property
            // is missing — a trader without a purse would never restock,
            // and the player could never sell to them.
            var merchant = _factory.CreateEntity("Merchant");
            Assert.AreEqual(150, TradeSystem.GetDrams(merchant));
        }

        [Test]
        public void Merchant_CarriesTheStockTableProperty_ForRestocking()
        {
            var merchant = _factory.CreateEntity("Merchant");
            Assert.AreEqual("MerchantStock",
                merchant.GetProperty(TraderRestockSystem.ShopStockTableProp),
                "the existing restock system re-rolls this table when the shelf runs low");
        }

        [Test]
        public void AlreadyStockedTrader_IsNotDoubleStocked()
        {
            // The five town shopkeepers are stocked by their shop:
            // stamp. Double-stocking would quietly double the town
            // economy.
            var merchant = _factory.CreateEntity("Merchant");
            int after = merchant.GetPart<InventoryPart>().Objects.Count;

            // Re-fire the spawn hook as a stamp-then-part ordering would.
            merchant.FireEventAndRelease(GameEvent.New("ObjectCreated"));
            Assert.AreEqual(after, merchant.GetPart<InventoryPart>().Objects.Count,
                "a shelf that already has goods is left alone");
        }

        // ── The choice must never lie ────────────────────────────

        [Test]
        public void CanTrade_TrueForATrader()
        {
            Assert.IsTrue(ConversationManager.CanTrade(_factory.CreateEntity("Merchant")));
        }

        [Test]
        public void CanTrade_FalseForAnEmptyHandedNPC()
        {
            // Counter-check + the actual regression: before the fix,
            // ANY creature passed this because Creature grants an
            // InventoryPart, so the trade option appeared everywhere.
            var beast = _factory.CreateEntity("Beast");
            Assert.IsFalse(ConversationManager.CanTrade(beast),
                "an NPC with no stock, no table and no coin must not offer trade");
        }

        [Test]
        public void CanTrade_TrueForASoldOutMerchantWithCoin()
        {
            // A merchant who has sold everything can still BUY from the
            // player — refusing trade there would strand the player's
            // loot.
            var merchant = _factory.CreateEntity("Merchant");
            merchant.GetPart<InventoryPart>().Objects.Clear();
            merchant.Properties[TraderRestockSystem.ShopStockTableProp] = "";
            var stripped = new Entity { ID = "s", BlueprintName = "Stripped" };
            stripped.AddPart(new InventoryPart { MaxWeight = 100 });
            TradeSystem.SetDrams(stripped, 80);

            Assert.IsTrue(ConversationManager.CanTrade(stripped),
                "coin alone is reason enough to open the window");
        }

        [Test]
        public void CanTrade_FalseForNullOrInventorylessSpeaker()
        {
            Assert.IsFalse(ConversationManager.CanTrade(null));
            var rock = new Entity { ID = "r", BlueprintName = "Rock" };
            Assert.IsFalse(ConversationManager.CanTrade(rock));
        }

        [Test]
        public void NullFactory_IsAGracefulNoOp_ButThePurseStillLands()
        {
            // Headless contexts keep the purse (so restock can run
            // later) even when the stock roll can't.
            TraderPart.Factory = null;
            var merchant = _factory.CreateEntity("Merchant");
            Assert.AreEqual(0, merchant.GetPart<InventoryPart>().Objects.Count);
            Assert.AreEqual(150, TradeSystem.GetDrams(merchant));
        }
    }
}
