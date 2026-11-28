using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GantryCaravanConsignmentTests : FiftyWorldFixture
    {
        private const string Gantry = GantryCompositionTests.Id;
        private const string Manifest = "Gantry.ManifestEntered";
        private const string ShortCount = "Gantry.ShortCountSigned";
        private const string GrainRequest = "gantry-grain";
        private const string Salt = "PaleSalt";
        private const int FirstTick = 10;
        private const int NextDay = FirstTick + WorldClock.DayLengthTicks;

        private EntityFactory oldTraderFactory, oldRestockFactory;
        private OverworldZoneManager manager;
        private Entity merchant;

        [SetUp]
        public void SetUpGantry()
        {
            oldTraderFactory = TraderPart.Factory;
            oldRestockFactory = TraderRestockSystem.Factory;
            CinderholdCompositionTests.LoadLoot();
            TraderPart.Factory = null;
            TraderRestockSystem.Factory = Factory;

            Assert.That(Zone.RemoveEntity(Actor), Is.True);
            manager = new OverworldZoneManager(Factory, 64);
            Zone = manager.GetZone(Gantry);
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            merchant = Zone.GetAllEntities().Single(e => e.GetPart<RegionalRequestPart>()?.DefinitionId == GrainRequest);
            Assert.That(merchant.GetPart<TraderPart>().StockTable, Is.EqualTo("MerchantStock"));
            var at = Zone.GetEntityCell(merchant);
            var standing = CinderholdCompositionTests.Neighbors(at.X, at.Y).Select(p => Zone.GetCell(p.x, p.y))
                .First(cell => cell != null && !cell.BlocksMovement());
            Assert.That(Zone.AddEntity(Actor, standing.X, standing.Y), Is.True);
            TradeSystem.SetDrams(Actor, 1000);
        }

        [TearDown]
        public void RestoreFactories()
        {
            TraderPart.Factory = oldTraderFactory;
            TraderRestockSystem.Factory = oldRestockFactory;
            MerchantFactoryProbe.Callback = null;
            LootTableRegistry.ResetForTests();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClosedManifestKeepsTheRegularShelfAndWaitsForTheNextDay(bool shortCount)
        {
            if (shortCount) manager.SettlementManager.SetCondition(Gantry, ShortCount, true);
            var shelf = merchant.GetPart<InventoryPart>();
            foreach (var item in shelf.Objects.ToArray()) Assert.That(shelf.RemoveObject(item), Is.True);
            var ordinary = Factory.CreateEntity(Salt);
            Assert.That(shelf.AddObject(ordinary), Is.True);

            TraderRestockSystem.RestockZone(Zone, FirstTick);
            Assert.That(TradeSystem.GetTraderStock(merchant).Any(e => e.BlueprintName == "HealingTonic"), Is.True,
                "The ordinary low-shelf roll remains active at the exchange.");
            Assert.That(merchant.GetPart<MerchantStockPart>()?.LastSpecialTick, Is.EqualTo(FirstTick));
            Assert.That(TradeSystem.GetTraderStock(merchant).Where(e => e.BlueprintName == Salt), Is.EqualTo(new[] { ordinary }));
            Assert.That(MerchantStockService.IsSpecial(merchant, ordinary), Is.False);

            manager.SettlementManager.SetCondition(Gantry, Manifest, true);
            var before = TradeSystem.GetTraderStock(merchant).ToArray();
            Assert.That(MerchantStockService.ProjectBenefit(merchant), Is.Not.Empty);
            Assert.That(MerchantStockService.Reconcile(merchant, Zone, NextDay - 1), Is.False);
            Assert.That(TradeSystem.GetTraderStock(merchant), Is.EqualTo(before));
            Assert.That(merchant.GetPart<MerchantStockPart>().LastSpecialTick, Is.EqualTo(FirstTick));

            TraderRestockSystem.RestockZone(Zone, NextDay);
            Assert.That(OnlyImport(merchant), Is.Not.SameAs(ordinary));
            Assert.That(TradeSystem.GetTraderStock(merchant), Does.Contain(ordinary));
            Assert.That(merchant.GetPart<MerchantStockPart>().LastSpecialTick, Is.EqualTo(NextDay));
        }

        [Test]
        public void CopiedRequestAndReentrantRefreshCannotCreateAnExtraImport()
        {
            manager.SettlementManager.SetCondition(Gantry, Manifest, true);
            var cell = Zone.GetCell(40, 12);
            var open = CinderholdCompositionTests.Neighbors(cell.X, cell.Y).Select(p => Zone.GetCell(p.x, p.y))
                .First(candidate => candidate != null && !candidate.BlocksMovement());
            var decoy = Place("Merchant", open.X, open.Y);
            decoy.AddPart(new RegionalRequestPart
            {
                DefinitionId = GrainRequest,
                InstanceId = merchant.GetPart<RegionalRequestPart>().InstanceId,
                RecipientId = decoy.ID
            });
            Assert.That(decoy.GetPart<TraderPart>().StockTable, Is.EqualTo("MerchantStock"));
            Assert.That(MerchantStockService.Reconcile(decoy, Zone, FirstTick), Is.False);
            Assert.That(MerchantStockService.ProjectBenefit(decoy), Is.Empty);

            int creations = 0;
            bool? reentered = null;
            Factory.RegisterPartType<MerchantFactoryProbe>();
            Factory.Blueprints[Salt].Parts[nameof(MerchantFactoryProbe)] = new Dictionary<string, string>();
            MerchantFactoryProbe.Callback = _ =>
            {
                if (++creations == 1) reentered = MerchantStockService.Reconcile(merchant, Zone, FirstTick);
            };
            Assert.That(MerchantStockService.Reconcile(merchant, Zone, FirstTick), Is.True);
            Assert.That(reentered, Is.False);
            Assert.That(creations, Is.EqualTo(1));
            var import = OnlyImport(merchant);
            TraderRestockSystem.RestockZone(Zone, FirstTick);
            Assert.That(OnlyImport(merchant), Is.SameAs(import));
            Assert.That(creations, Is.EqualTo(1));
            Assert.That(TradeSystem.GetTraderStock(decoy).Any(e => e.BlueprintName == Salt), Is.False);
        }

        [Test]
        public void PaidPurchaseTransfersTheExactImportAndPreservesOrdinarySalt()
        {
            manager.SettlementManager.SetCondition(Gantry, Manifest, true);
            Assert.That(MerchantStockService.Reconcile(merchant, Zone, FirstTick), Is.True);
            var import = OnlyImport(merchant);
            var ordinary = Carry(Salt);
            Assert.That(TradeSystem.SellToTrader(Actor, merchant, ordinary), Is.True);
            Assert.That(TradeSystem.GetTraderStock(merchant), Does.Contain(ordinary));
            Assert.That(MerchantStockService.IsSpecial(merchant, ordinary), Is.False);
            Assert.That(OnlyImport(merchant), Is.SameAs(import));

            int buyerDrams = TradeSystem.GetDrams(Actor), traderDrams = TradeSystem.GetDrams(merchant);
            int price = TradeSystem.GetBuyPrice(import, TradeSystem.GetTradePerformance(Actor), merchant);
            Assert.That(price, Is.GreaterThan(0));
            Assert.That(TradeSystem.BuyFromTrader(Actor, merchant, import), Is.True);
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(buyerDrams - price));
            Assert.That(TradeSystem.GetDrams(merchant), Is.EqualTo(traderDrams + price));
            Assert.That(Pack.Objects, Does.Contain(import));
            Assert.That(merchant.GetPart<MerchantStockPart>().Specials, Is.Empty);
            Assert.That(MerchantStockService.Reconcile(merchant, Zone, NextDay - 1), Is.False);
            Assert.That(TradeSystem.GetTraderStock(merchant).Where(e => e.BlueprintName == Salt), Is.EqualTo(new[] { ordinary }));

            Assert.That(MerchantStockService.Reconcile(merchant, Zone, NextDay), Is.True);
            Assert.That(OnlyImport(merchant), Is.Not.SameAs(import));
            Assert.That(TradeSystem.GetTraderStock(merchant), Does.Contain(ordinary));
            Assert.That(Pack.Objects, Does.Contain(import));
        }

        [Test]
        public void SavedExchangeKeepsItsExactLotUntilTheNextDay()
        {
            manager.SettlementManager.SetCondition(Gantry, Manifest, true);
            Assert.That(MerchantStockService.Reconcile(merchant, Zone, FirstTick), Is.True);
            string merchantId = merchant.ID, importId = OnlyImport(merchant).ID;
            using (var stream = new MemoryStream())
            {
                GameSessionState.Capture("gantry-caravan-stock", "test", manager, null, Actor).Save(new SaveWriter(stream));
                stream.Position = 0;
                var loaded = GameSessionState.Load(new SaveReader(stream, Factory));
                manager = loaded.ZoneManager;
                Zone = manager.CachedZones[Gantry];
            }
            SettlementRuntime.ActiveZone = Zone;
            var restored = Zone.GetAllEntities().Single(e => e.ID == merchantId);
            Assert.That(manager.SettlementManager.HasExistingCondition(Gantry, Manifest), Is.True);
            var import = OnlyImport(restored);
            Assert.That(import.ID, Is.EqualTo(importId));
            Assert.That(MerchantStockService.Reconcile(restored, Zone, NextDay - 1), Is.False);
            Assert.That(OnlyImport(restored), Is.SameAs(import));
            Assert.That(MerchantStockService.Reconcile(restored, Zone, NextDay), Is.True);
            Assert.That(OnlyImport(restored), Is.Not.SameAs(import));
            Assert.IsFalse(TradeSystem.GetTraderStock(restored).Contains(import));
        }

        private static Entity OnlyImport(Entity trader)
        {
            var listed = TradeSystem.GetTraderStock(trader).Where(e => MerchantStockService.IsSpecial(trader, e)).ToArray();
            Assert.That(listed.Select(e => e.BlueprintName), Is.EqualTo(new[] { Salt }));
            var item = listed.Single();
            Assert.That(item.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(item.GetPart<PhysicsPart>().InInventory, Is.SameAs(trader));
            return item;
        }
    }
}
