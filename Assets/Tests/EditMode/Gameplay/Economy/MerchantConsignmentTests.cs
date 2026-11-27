using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    [TestFixture] public sealed class MerchantConsignmentTests : FiftyWorldFixture
    {
        EntityFactory oldTrader, oldRestock;
        [SetUp] public void PrepareMerchants()
        {
            oldTrader = TraderPart.Factory; oldRestock = TraderRestockSystem.Factory;
            TraderPart.Factory = null; TraderRestockSystem.Factory = Factory;
            TradeSystem.SetDrams(Actor, 10000); Pack.MaxWeight = 1000;
        }
        [TearDown] public void RestoreMerchants()
        { MerchantFactoryProbe.Callback = null; TraderPart.Factory = oldTrader; TraderRestockSystem.Factory = oldRestock; }
        Entity Merchant(string bp = "Farmer")
        {
            var e = Place(bp); e.ID = "consignment-" + bp;
            e.GetPart<InventoryPart>().MaxWeight = 1000; TradeSystem.SetDrams(e, 10000); return e;
        }
        MerchantStockPart Start(Entity e, int tick = 10)
        { Assert.True(MerchantStockService.Reconcile(e, Zone, tick)); var s = e.GetPart<MerchantStockPart>(); Assert.NotNull(s); return s; }
        static int Units(Entity e, string bp) => e.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == bp)
            .Sum(i => Math.Max(0, i.GetPart<StackerPart>()?.StackCount ?? 1));
        Entity RoundTrip(Entity e)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(e); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, Factory); var result = reader.ReadEntityReference(); reader.ReadEntityBodies(); return result;
            }
        }
        [TestCase("Farmer", "CandyCarrotSeed", 3)] [TestCase("Farmer", "EmberwheatSeed", 2)]
        [TestCase("Armorer", "LeatherArmor", 1)] [TestCase("Armorer", "LeatherCap", 1)] [TestCase("Armorer", "LeatherBoots", 1)]
        public void DependableStaplesAndTwoRealSpecials(string bp, string staple, int count)
        {
            var e = Merchant(bp); var before = e.GetPart<InventoryPart>().Objects.ToArray(); var s = Start(e);
            Assert.AreEqual(count, Units(e, staple)); Assert.AreEqual(2, s.Specials.Count);
            Assert.AreEqual(2, s.Specials.Distinct().Count()); Assert.AreEqual(2, s.SpecialKeys.Count);
            foreach (var i in s.Specials) { Assert.True(MerchantStockService.IsSpecial(e, i)); Assert.AreSame(e, i.GetPart<PhysicsPart>().InInventory); }
            foreach (var i in before) Assert.Contains(i, e.GetPart<InventoryPart>().Objects);
        }
        [Test] public void GameDayBoundaryAndBoundedLateCatchup()
        {
            var e = Merchant(); var s = Start(e); var first = s.SelectionRaw; var ids = s.Specials.ToArray();
            MerchantStockService.Reconcile(e, Zone, 1209); Assert.AreEqual(first, s.SelectionRaw); CollectionAssert.AreEqual(ids, s.Specials);
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.AreEqual(1, s.Generation); Assert.AreNotEqual(first, s.SelectionRaw);
            Assert.True(MerchantStockService.Reconcile(e, Zone, 900000)); Assert.AreEqual(2, s.Generation); Assert.AreEqual(900000, s.LastSpecialTick);
            MerchantStockService.Reconcile(e, Zone, 900001); Assert.AreEqual(2, s.Generation);
        }
        [Test] public void StaplesWaitStrictlyMoreThanThreeHundredTicks()
        {
            var e = Merchant(); var s = Start(e); var seed = e.GetPart<InventoryPart>().Objects.First(i => i.BlueprintName == "CandyCarrotSeed");
            Assert.True(TradeSystem.BuyFromTrader(Actor, e, seed));
            MerchantStockService.Reconcile(e, Zone, 310); Assert.AreEqual(0, Units(e, "CandyCarrotSeed"));
            MerchantStockService.Reconcile(e, Zone, 311); Assert.AreEqual(3, Units(e, "CandyCarrotSeed")); Assert.AreEqual(311, s.LastStapleTick);
        }
        [Test] public void CurrentLotAndExactSavedMembersRoundTripWithoutReroll()
        {
            var e = Merchant(); var s = Start(e); var saved = RoundTrip(e); var t = saved.GetPart<MerchantStockPart>();
            Assert.NotNull(t); Assert.AreEqual(s.SelectionRaw, t.SelectionRaw); Assert.AreEqual(10, t.LastSpecialTick);
            CollectionAssert.AreEqual(s.Specials.Select(i => i.ID), t.Specials.Select(i => i.ID));
            CollectionAssert.AreEqual(s.SpecialKeys, t.SpecialKeys);
            foreach (var item in t.Specials) { Assert.Contains(item, saved.GetPart<InventoryPart>().Objects); Assert.True(MerchantStockService.IsSpecial(saved, item)); }
        }
        [Test] public void OldSaveLoadsWithoutAddingGoodsOrState()
        { var e = Merchant(); var count = e.GetPart<InventoryPart>().Objects.Count; var saved = RoundTrip(e); Assert.IsNull(saved.GetPart<MerchantStockPart>()); Assert.AreEqual(count, saved.GetPart<InventoryPart>().Objects.Count); }
        [Test] public void ReopenReadsAndBackwardTimeCannotReroll()
        {
            var e = Merchant(); var s = Start(e); var ids = s.Specials.ToArray();
            for (int n = 0; n < 5; n++) { TradeSystem.GetTraderStock(e); MerchantStockService.Reconcile(e, Zone, 10); }
            MerchantStockService.Reconcile(e, Zone, 0); CollectionAssert.AreEqual(ids, s.Specials); Assert.AreEqual(0, s.Generation);
        }
        [Test] public void PurchasedResoldGoodsAreNeverReturnedWithConsignment()
        {
            var e = Merchant(); var s = Start(e); var bought = s.Specials[0];
            Assert.True(TradeSystem.BuyFromTrader(Actor, e, bought)); Assert.False(MerchantStockService.IsSpecial(e, bought));
            Assert.True(TradeSystem.SellToTrader(Actor, e, bought)); Assert.False(MerchantStockService.IsSpecial(e, bought));
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.Contains(bought, e.GetPart<InventoryPart>().Objects);
        }
        [Test] public void OrdinarySaleCannotMergeIntoSpecial()
        {
            var e = Merchant(); var s = Start(e); var special = s.Specials[0]; var sale = Carry(special.BlueprintName);
            Assert.True(TradeSystem.SellToTrader(Actor, e, sale)); Assert.AreEqual(1, BarterDeal.Units(special));
            Assert.Contains(sale, e.GetPart<InventoryPart>().Objects); Assert.True(MerchantStockService.IsSpecial(e, special));
            MerchantStockService.Reconcile(e, Zone, 1210); Assert.Contains(sale, e.GetPart<InventoryPart>().Objects);
        }
        [Test] public void FailedHeavyPurchaseRestoresMembershipAndPurses()
        {
            var e = Merchant("Armorer"); var s = Start(e); var item = s.Specials[0]; Pack.MaxWeight = 0;
            int buyer = TradeSystem.GetDrams(Actor), seller = TradeSystem.GetDrams(e);
            Assert.False(TradeSystem.BuyFromTrader(Actor, e, item)); Assert.True(MerchantStockService.IsSpecial(e, item));
            Assert.Contains(item, e.GetPart<InventoryPart>().Objects); Assert.AreEqual(buyer, TradeSystem.GetDrams(Actor)); Assert.AreEqual(seller, TradeSystem.GetDrams(e));
        }
        [Test] public void BarterPurchaseReleasesMembershipAndClearsOffer()
        {
            var e = Merchant(); var s = Start(e); var item = s.Specials[0]; var deal = new BarterDeal(Actor, e); Assert.True(deal.SetTake(item, 1));
            Assert.True(Barter.TryDeal(deal, out var reason), reason); Assert.False(MerchantStockService.IsSpecial(e, item));
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.AreEqual(1, s.Generation);
        }
        [Test] public void ActiveViewDefersWithdrawalThenAllowsIt()
        {
            var e = Merchant(); var s = Start(e); using (MerchantStockService.BeginView(e))
            { Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.AreEqual(0, s.Generation); }
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.AreEqual(1, s.Generation);
        }
        [Test] public void PendingOfferDefersUntilExplicitlyForgotten()
        {
            var e = Merchant(); var s = Start(e); var deal = new BarterDeal(Actor, e); Assert.True(deal.SetTake(s.Specials[0], 1));
            Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.AreEqual(0, s.Generation);
            MerchantStockService.ForgetOffer(deal); Assert.True(MerchantStockService.Reconcile(e, Zone, 1210));
        }
        [TestCase("changed")] [TestCase("unsupported")] [TestCase("quantity")] [TestCase("escaped")]
        public void ChangedOrEscapedMembersSurviveRotation(string change)
        {
            var e = Merchant(); var s = Start(e); var item = s.Specials[0];
            if (change == "changed") item.SetIntProperty("player-repaired", 1);
            if (change == "unsupported") item.AddPart(new MerchantUnknownPayload());
            if (change == "quantity") item.GetPart<StackerPart>().StackCount = 2;
            if (change == "escaped") { Assert.True(e.GetPart<InventoryPart>().RemoveObject(item)); Assert.True(Pack.AddObject(item)); }
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210));
            Assert.Contains(item, (change == "escaped" ? Pack : e.GetPart<InventoryPart>()).Objects);
            Assert.False(MerchantStockService.IsSpecial(e, item));
        }
        [Test] public void CapacityFailurePreservesOldLotAndStamp()
        {
            var e = Merchant("Armorer"); var s = Start(e); var ids = s.Specials.ToArray(); e.GetPart<InventoryPart>().MaxWeight = 0;
            Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)); CollectionAssert.AreEqual(ids, s.Specials); Assert.AreEqual(10, s.LastSpecialTick);
        }
        [Test] public void MissingFactoryPreservesOldLot()
        {
            var e = Merchant(); var s = Start(e); var ids = s.Specials.ToArray(); TraderRestockSystem.Factory = null;
            Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)); CollectionAssert.AreEqual(ids, s.Specials);
        }
        [Test] public void ForgedStockPropertyDoesNotOptIn()
        {
            var e = Place("Player"); e.Properties[TraderRestockSystem.ShopStockTableProp] = "FarmerStock";
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
        }
        [Test] public void ZoneRestockInstallsProfileWithoutLegacyRandomShelf()
        {
            var e = Merchant(); TraderRestockSystem.RestockZone(Zone, 10); Assert.NotNull(e.GetPart<MerchantStockPart>());
            Assert.AreEqual(3, Units(e, "CandyCarrotSeed")); Assert.AreEqual(2, Units(e, "EmberwheatSeed"));
        }
        [TestCase(RepairStage.Fouled, false)] [TestCase(RepairStage.TemporarilyPurified, false)]
        [TestCase(RepairStage.StableRepair, true)] [TestCase(RepairStage.ImprovedWithCaretaker, true)]
        public void ExistingLocalOvenExpandsOnlyNextLot(RepairStage stage, bool expected)
        {
            Zone.ZoneID = SettlementSiteDefinitions.StartingVillageZoneId;
            var manager = new SettlementManager();
            var settlement = manager.GetOrCreateSettlement(Zone.ZoneID, new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
            var e = Merchant(); var s = Start(e);
            settlement.GetSite(SettlementSiteDefinitions.VillageOvenSiteId).Stage = stage;
            MerchantStockService.Reconcile(e, Zone, 11); Assert.False(s.BonusApplied); Assert.AreEqual(2, s.Specials.Count);
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.AreEqual(expected, s.BonusApplied);
            Assert.AreEqual(expected ? 3 : 2, s.Specials.Count); Assert.AreEqual(expected ? 1 : 0, s.Specials.Count(i => i.BlueprintName == "RoastedHearthbulb"));
            Assert.False(settlement.HasCondition(SettlementSiteDefinitions.ImprovedOvenCondition), "stable repair works without a caretaker-only condition");
        }
        [Test] public void RemoteOvenCannotExpandLocalMerchant()
        {
            var manager = new SettlementManager(); var settlement = manager.GetOrCreateSettlement(SettlementSiteDefinitions.StartingVillageZoneId,
                new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
            settlement.GetSite(SettlementSiteDefinitions.VillageOvenSiteId).Stage = RepairStage.ImprovedWithCaretaker;
            var e = Merchant(); var s = Start(e); Assert.False(s.BonusApplied); Assert.AreEqual(2, s.Specials.Count);
        }
        [Test] public void MissingSiteIsNotCreatedByStockPolicy()
        {
            var manager = new SettlementManager(); var e = Merchant(); Start(e);
            Assert.False(manager.TryGetExistingSite(Zone.ZoneID, SettlementSiteDefinitions.VillageOvenSiteId, out _));
        }
        [TestCase("NoTrade")] [TestCase("modified")] [TestCase("unsupported")] [TestCase("foreign")]
        public void InvalidOrModifiedStaplesDoNotBlockUsableBaseStock(string change)
        {
            var e = Merchant(); Start(e); var seed = e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "CandyCarrotSeed");
            if (change == "NoTrade") seed.SetTag("NoTrade");
            if (change == "modified") seed.GetPart<CommercePart>().Value++;
            if (change == "unsupported") seed.AddPart(new MerchantUnknownPayload());
            if (change == "foreign") seed.GetPart<PhysicsPart>().InInventory = Actor;
            Assert.True(MerchantStockService.Reconcile(e, Zone, 311)); Assert.AreEqual(6, Units(e, "CandyCarrotSeed"));
            Assert.Contains(seed, e.GetPart<InventoryPart>().Objects);
        }
        [TestCase("dead")] [TestCase("removed")] [TestCase("mismatched")]
        public void InvalidMerchantCannotRestock(string change)
        {
            var e = Merchant(); var s = Start(e); var ids = s.Specials.ToArray();
            if (change == "dead") e.GetStat("Hitpoints").BaseValue = 0;
            if (change == "removed") Assert.True(Zone.RemoveEntity(e));
            if (change == "mismatched") e.GetPart<TraderPart>().StockTable = "ArmorerStock";
            Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)); CollectionAssert.AreEqual(ids, s.Specials);
        }
        void Probe(Action<Entity> callback, string bp = "CandyCarrotSeed")
        {
            Factory.RegisterPartType<MerchantFactoryProbe>(); MerchantFactoryProbe.Callback = callback;
            Factory.Blueprints[bp].Parts[nameof(MerchantFactoryProbe)] = new System.Collections.Generic.Dictionary<string, string>();
        }
        [Test] public void FactoryReentryCannotCreateSecondLotOrTradeThroughUpdate()
        {
            var e = Merchant(); var existing = Factory.CreateEntity("Dagger"); Assert.True(e.GetPart<InventoryPart>().AddObject(existing));
            int calls = 0;
            Probe(item => { calls++; Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.False(TradeSystem.BuyFromTrader(Actor, e, existing)); });
            var s = Start(e); Assert.Greater(calls, 0); Assert.AreEqual(2, s.Specials.Count); Assert.Contains(existing, e.GetPart<InventoryPart>().Objects);
        }
        [TestCase("move")] [TestCase("profile")]
        public void FactoryCallbackInvalidatesUpdateWithoutRewindingIndependentChange(string change)
        {
            var e = Merchant(); var before = e.GetPart<InventoryPart>().Objects.ToArray();
            Probe(item => { if (change == "move") Zone.RemoveEntity(e); else e.GetPart<TraderPart>().StockTable = "ArmorerStock"; });
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
            CollectionAssert.AreEqual(before, e.GetPart<InventoryPart>().Objects);
            if (change == "profile") Assert.AreEqual("ArmorerStock", e.GetPart<TraderPart>().StockTable);
        }
        [Test] public void ActiveTradeCallbackCannotRotateTheItemBeingBought()
        {
            var e = Merchant(); var s = Start(e); var item = s.Specials[0];
            var probe = new MerchantBeforeTradeProbe { Callback = () => Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)) }; Actor.AddPart(probe);
            Assert.True(TradeSystem.BuyFromTrader(Actor, e, item)); Assert.AreEqual(0, s.Generation);
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.AreEqual(1, s.Generation);
        }
        [Test] public void PurchaseVetoPreservesItemAndMembership()
        {
            var e = Merchant(); var s = Start(e); var item = s.Specials[0]; Actor.AddPart(new MerchantBeforeTradeProbe { Veto = true });
            Assert.False(TradeSystem.BuyFromTrader(Actor, e, item)); Assert.True(MerchantStockService.IsSpecial(e, item));
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210));
        }
        [Test] public void FullLotSaveUsesSameNextSelectionAsUninterruptedState()
        {
            var e = Merchant(); var s = Start(e); var copy = RoundTrip(e); Assert.True(Zone.RemoveEntity(e)); Assert.True(Zone.AddEntity(copy, 11, 10));
            Assert.True(MerchantStockService.Reconcile(copy, Zone, 1210)); var selection = copy.GetPart<MerchantStockPart>().SelectionRaw;
            Assert.True(Zone.RemoveEntity(copy)); Assert.True(Zone.AddEntity(e, 11, 10)); Assert.True(MerchantStockService.Reconcile(e, Zone, 1210));
            Assert.AreEqual(selection, s.SelectionRaw);
        }
        [Test] public void NearIntegerLimitUsesLongElapsedArithmetic()
        {
            var e = Merchant(); var s = Start(e, int.MaxValue - 1200); Assert.True(MerchantStockService.Reconcile(e, Zone, int.MaxValue));
            Assert.AreEqual(1, s.Generation); Assert.AreEqual(int.MaxValue, s.LastSpecialTick);
            MerchantStockService.Reconcile(e, Zone, 0); Assert.AreEqual(1, s.Generation);
        }
        [Test] public void EmptyAndExplicitlyRemovedOffersReleaseRotation()
        {
            var e = Merchant(); var s = Start(e); var deal = new BarterDeal(Actor, e); Assert.True(deal.SetTake(s.Specials[0], 1));
            Assert.True(deal.SetTake(s.Specials[0], 0)); Assert.True(MerchantStockService.Reconcile(e, Zone, 1210));
        }
        [Test] public void StockExplanationFitsTheActualScreenAndProjectBenefitHasItsOwnLine()
        {
            var e = Merchant(); Start(e); Assert.LessOrEqual(MerchantStockService.Describe(e).Length, 78);
        }
        [Test] public void FactoryEquippedGoodCannotBeAdoptedOrClearedFromAnotherActor()
        {
            var e = Merchant("Armorer"); Entity prepared = null;
            Probe(item => { prepared = item; item.GetPart<PhysicsPart>().Equipped = Actor; Pack.EquippedItems["Head"] = item; }, "LeatherCap");
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.NotNull(prepared);
            Assert.AreSame(Actor, prepared.GetPart<PhysicsPart>().Equipped); Assert.AreSame(prepared, Pack.EquippedItems["Head"]);
            Assert.IsNull(e.GetPart<MerchantStockPart>());
        }
        [Test] public void OvenGateChangedDuringFactoryWorkCannotPublishBonus()
        {
            Zone.ZoneID = SettlementSiteDefinitions.StartingVillageZoneId; var manager = new SettlementManager();
            var settlement = manager.GetOrCreateSettlement(Zone.ZoneID, new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
            var oven = settlement.GetSite(SettlementSiteDefinitions.VillageOvenSiteId); oven.Stage = RepairStage.StableRepair;
            var e = Merchant(); Probe(item => oven.Stage = RepairStage.Fouled, "RoastedHearthbulb");
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
            Assert.AreEqual(RepairStage.Fouled, oven.Stage, "independent deterioration is not rewound");
        }
        [TestCase("view")] [TestCase("offer")]
        public void FactoryOpenedViewOrOfferDefersWithdrawalOfExistingLot(string reservation)
        {
            var e = Merchant(); var s = Start(e); var ids = s.Specials.ToArray(); IDisposable view = null; BarterDeal deal = null;
            Probe(item =>
            {
                if (reservation == "view") view = MerchantStockService.BeginView(e);
                else { deal = new BarterDeal(Actor, e); Assert.True(deal.SetTake(ids[0], 1)); }
            });
            try
            {
                Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)); CollectionAssert.AreEqual(ids, s.Specials);
                Assert.AreEqual(10, s.LastSpecialTick); Assert.Contains(ids[0], e.GetPart<InventoryPart>().Objects);
            }
            finally { view?.Dispose(); MerchantStockService.ForgetOffer(deal); }
        }
        [TestCase("blueprint")] [TestCase("id")] [TestCase("part-owner")]
        public void FactoryCannotSubstituteInvalidFreshGood(string change)
        {
            var e = Merchant(); Probe(item =>
            {
                if (change == "blueprint") item.BlueprintName = "Dagger";
                if (change == "id") item.ID = "";
                if (change == "part-owner") item.GetPart<PhysicsPart>().ParentEntity = Actor;
            });
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
        }
        [TestCase("Farmer", 4)] [TestCase("Armorer", 5)]
        public void EveryAuthoredChoiceCanBeInstalledAcrossACompleteCycle(string bp, int cycle)
        {
            var e = Merchant(bp); var s = Start(e); var seen = new System.Collections.Generic.HashSet<string>();
            string previous = null;
            for (int n = 0; n < cycle; n++)
            {
                if (n > 0) Assert.True(MerchantStockService.Reconcile(e, Zone, 10 + 1200 * n), "lot " + n);
                Assert.AreEqual(n, s.Generation); Assert.AreNotEqual(previous, s.SelectionRaw); previous = s.SelectionRaw;
                foreach (var item in s.Specials) { seen.Add(item.BlueprintName); Assert.True(MerchantStockService.IsSpecial(e, item)); }
            }
            Assert.AreEqual(cycle, seen.Count);
        }
        [Test] public void ArbitraryFactoryFailureIsContainedAndReleasesReentryGuard()
        {
            var e = Merchant(); Probe(item => { throw new Exception("factory probe"); });
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
            MerchantFactoryProbe.Callback = null; Assert.True(MerchantStockService.Reconcile(e, Zone, 10));
        }
        [Test] public void LaterFactoryCallbackCannotPublishStaleStapleDeficitOrRewindInjury()
        {
            var e = Merchant(); int hp = Actor.GetStat("Hitpoints").BaseValue; Entity independent = null;
            Probe(item =>
            {
                independent = Factory.CreateEntity("CandyCarrotSeed"); independent.GetPart<StackerPart>().StackCount = 3;
                Assert.True(e.GetPart<InventoryPart>().AddObject(independent)); Actor.GetStat("Hitpoints").BaseValue--;
            }, "EmberwheatSeed");
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
            Assert.Contains(independent, e.GetPart<InventoryPart>().Objects); Assert.AreEqual(3, Units(e, "CandyCarrotSeed"));
            Assert.AreEqual(hp - 1, Actor.GetStat("Hitpoints").BaseValue);
        }
        [Test] public void MissingPurseCannotOptInThroughShopOpen()
        {
            var e = Merchant(); e.IntProperties.Remove(TradeSystem.CURRENCY_PROP);
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
        }
        [Test] public void OrdinaryStapleTopupMergesIntoTheExistingMatchingStack()
        {
            var e = Merchant(); var seed = Factory.CreateEntity("CandyCarrotSeed"); Assert.True(e.GetPart<InventoryPart>().AddObject(seed));
            Start(e); Assert.AreEqual(3, BarterDeal.Units(seed));
            Assert.AreEqual(1, e.GetPart<InventoryPart>().Objects.Count(i => i.BlueprintName == "CandyCarrotSeed"));
        }
        [Test] public void RepairedOvenExplainsWaitingForNextConsignmentWithoutReroll()
        {
            Zone.ZoneID = SettlementSiteDefinitions.StartingVillageZoneId; var manager = new SettlementManager();
            var settlement = manager.GetOrCreateSettlement(Zone.ZoneID, new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
            var e = Merchant(); var s = Start(e); var ids = s.Specials.ToArray();
            settlement.GetSite(SettlementSiteDefinitions.VillageOvenSiteId).Stage = RepairStage.StableRepair;
            StringAssert.Contains("next consignment", MerchantStockService.ProjectBenefit(e));
            CollectionAssert.AreEqual(ids, s.Specials); Assert.False(s.BonusApplied);
        }
        [Test] public void MissingWithdrawalKeyPreservesGoodAndAllowsFutureLot()
        {
            var e = Merchant(); var s = Start(e); var item = s.Specials[0]; s.SpecialKeys[0] = null;
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.Contains(item, e.GetPart<InventoryPart>().Objects);
            Assert.False(MerchantStockService.IsSpecial(e, item));
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualTradeLedgerShowsWholeProjectBenefitAndClosingReleasesOffers(bool repaired)
        {
            Zone.ZoneID = SettlementSiteDefinitions.StartingVillageZoneId; var manager = new SettlementManager();
            var settlement = manager.GetOrCreateSettlement(Zone.ZoneID, new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
            if (repaired) settlement.GetSite(SettlementSiteDefinitions.VillageOvenSiteId).Stage = RepairStage.StableRepair;
            var e = Merchant(); var state = Start(e);
            var host = new UnityEngine.GameObject("consignment-ui-test");
            var ui = host.AddComponent<CavesOfOoo.Rendering.TradeUI>();
            ui.PlayerEntity = Actor; ui.CurrentZone = Zone;
            try
            {
                ui.Open(e); string text = TradeLedgerTestReader.VisibleText(ui);
                StringAssert.Contains("game day", text);
                StringAssert.Contains(repaired ? "Oven baking included" : "Repair the local oven", text);
                ui.DealKey(UnityEngine.KeyCode.D); Assert.True(ui.Deal.SetTake(state.Specials[0], 1)); ui.Close();
                Assert.True(MerchantStockService.Reconcile(e, Zone, 1210), "closing must release both view and pending offer");
            }
            finally { ui.Close(); UnityEngine.Object.DestroyImmediate(host); }
        }
        [Test] public void FailedBarterKeepsExactMembershipAndOfferUntilCancelled()
        {
            var e = Merchant("Armorer"); var s = Start(e); var item = s.Specials[0]; var deal = new BarterDeal(Actor, e); Assert.True(deal.SetTake(item, 1)); Pack.MaxWeight = 0;
            Assert.False(Barter.TryDeal(deal, out _)); Assert.True(MerchantStockService.IsSpecial(e, item));
            Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)); MerchantStockService.ForgetOffer(deal); Assert.True(MerchantStockService.Reconcile(e, Zone, 1210));
        }
    }
    public sealed class MerchantUnknownPayload : Part { public int Value = 1; }
    public sealed class MerchantFactoryProbe : Part
    {
        public static Action<Entity> Callback;
        public override bool HandleEvent(GameEvent e)
        { if (e.ID == "ObjectCreated") { Callback?.Invoke(ParentEntity); ParentEntity.RemovePart(this); } return true; }
    }
    public sealed class MerchantBeforeTradeProbe : Part
    {
        public Action Callback; public bool Veto;
        public override bool HandleEvent(GameEvent e) { if (e.ID != "BeforeTrade") return true; Callback?.Invoke(); return !Veto; }
    }
}
