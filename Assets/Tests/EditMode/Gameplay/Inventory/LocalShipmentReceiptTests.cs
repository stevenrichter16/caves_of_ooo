using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class LocalShipmentReceiptTests : FiftyWorldFixture
    {
        [Test]
        public void AReceivedLotBecomesOrdinaryStockOnceAndKeepsItsOwnIdentity()
        {
            var receipt = Arrange(delivered: true);
            var order = receipt.Ledger.GetPart<LocalPorterLedgerPart>();
            var parcel = order.Box;
            var cargo = parcel.GetPart<ContainerPart>();
            var measure = receipt.FirstMeasure;
            var opening = TradeSystem.GetTraderStock(receipt.Merchant).Single();
            int purse = TradeSystem.GetDrams(receipt.Merchant);

            Assert.That(receipt.TryStock(Zone), Is.True);
            Assert.That(receipt.StockedOrderID, Is.EqualTo(order.OrderID));
            Assert.That(order.Shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(parcel));
            Assert.That(cargo.Contents, Is.Empty);
            Assert.That(LocalPorterReservationPart.Blocks(parcel), Is.False);
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant), Is.EquivalentTo(new[] { opening, measure }));
            Assert.That(opening.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(measure.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(measure.GetPart<PhysicsPart>().InInventory, Is.SameAs(receipt.Merchant));
            Assert.That(MerchantStockService.IsSpecial(receipt.Merchant, measure), Is.False);
            Assert.That(receipt.FirstMeasure, Is.Null);
            Assert.That(TradeSystem.GetDrams(receipt.Merchant), Is.EqualTo(purse));

            TradeSystem.SetDrams(Actor, 50);
            Assert.That(TradeSystem.BuyFromTrader(Actor, receipt.Merchant, measure), Is.True);
            Assert.That(Pack.Objects, Does.Contain(measure));
            Assert.That(receipt.TryStock(Zone), Is.False);
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant), Is.EqualTo(new[] { opening }));
        }

        [Test]
        public void CargoInTransitDoesNotTurnIntoStockEvenIfTheBoxIsOnTheShelf()
        {
            var receipt = Arrange(delivered: false);
            Assert.That(receipt.TryStock(Zone), Is.False);
            Assert.That(receipt.StockedOrderID, Is.Null);
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant).Count, Is.EqualTo(1));
            Assert.That(LocalPorterReservationPart.Blocks(receipt.Ledger.GetPart<LocalPorterLedgerPart>().Box), Is.True);
        }

        [Test]
        public void FullMerchantPackLeavesBothMeasuresInTheBoxAndCanBeRetried()
        {
            var receipt = Arrange(delivered: true, split: true);
            var parcel = receipt.Ledger.GetPart<LocalPorterLedgerPart>().Box;
            var cargo = parcel.GetPart<ContainerPart>();
            var inventory = receipt.Merchant.GetPart<InventoryPart>();
            var opening = TradeSystem.GetTraderStock(receipt.Merchant).Single();
            var measures = new[] { receipt.FirstMeasure, receipt.SecondMeasure };
            inventory.MaxWeight = inventory.GetCarriedWeight() + 1;

            Assert.That(receipt.TryStock(Zone), Is.False);
            Assert.That(cargo.Contents, Is.EqualTo(measures));
            Assert.That(cargo.Contents.All(item => item.GetPart<PhysicsPart>().InInventory == parcel), Is.True);
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant), Is.EqualTo(new[] { opening }));
            Assert.That(receipt.StockedOrderID, Is.Null);
            Assert.That(LocalPorterReservationPart.Blocks(parcel), Is.True);
            inventory.MaxWeight++;
            Assert.That(receipt.TryStock(Zone), Is.True);
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant),
                Is.EquivalentTo(new[] { opening, measures[0], measures[1] }));
        }

        [Test]
        public void StockingTraceWaitsForTheExactDeliveryToReachTheMerchantPack()
        {
            var prior = Diag.Snapshot(Diag.BufferCapacity).Select(e => e.TraceId).ToHashSet();
            bool previous = Diag.IsChannelEnabled("trade");
            Diag.Entry[] Recorded() => Diag.Snapshot(Diag.BufferCapacity)
                .Where(e => !prior.Contains(e.TraceId) && e.Category == "trade" && e.Kind == "ShipmentStocked").ToArray();

            if (!previous) Diag.SetChannel("trade", true);
            try
            {
                var (receipt, order) = BookForDeadline(split: true);
                string orderId = order.OrderID;
                var merchant = receipt.Merchant;
                var shelf = order.Shelf;
                var box = order.Box;
                var cargo = box.GetPart<ContainerPart>();
                var measures = new[] { receipt.FirstMeasure, receipt.SecondMeasure };
                var inventory = merchant.GetPart<InventoryPart>();
                var opening = TradeSystem.GetTraderStock(merchant).Single();
                inventory.MaxWeight = inventory.GetCarriedWeight() + 1;

                FinishDelivery(order);
                Assert.That(orderId, Is.Not.Null.And.Not.Empty);
                Assert.That(shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(box));
                Assert.That(Recorded(), Is.Empty, "Handover to the shelf alone does not stock the shop.");
                Assert.False(receipt.TryStock(Zone));
                Assert.That(Recorded(), Is.Empty, "A rolled-back stock transfer does not publish a receipt.");
                Assert.That(receipt.StockedOrderID, Is.Null);
                Assert.That(cargo.Contents, Is.EqualTo(measures));
                Assert.That(measures.All(item => item.GetPart<PhysicsPart>().InInventory == box), Is.True);
                Assert.That(TradeSystem.GetTraderStock(merchant), Is.EqualTo(new[] { opening }));

                inventory.MaxWeight++;
                Assert.True(receipt.TryStock(Zone));
                Assert.That(receipt.StockedOrderID, Is.EqualTo(orderId));
                Assert.That(cargo.Contents, Is.Empty);
                Assert.That(measures.All(item => item.GetPart<PhysicsPart>().InInventory == merchant), Is.True);
                Assert.That(TradeSystem.GetTraderStock(merchant), Is.EquivalentTo(new[] { opening, measures[0], measures[1] }));
                Assert.False(receipt.TryStock(Zone));
                var entry = Recorded().Single();
                var payload = UnityEngine.JsonUtility.FromJson<ShipmentStockTrace>(entry.PayloadJson);
                Assert.That(payload.orderId, Is.EqualTo(orderId));
                Assert.That(payload.boxId, Is.EqualTo(box.ID));
                Assert.That(payload.shelfId, Is.EqualTo(shelf.ID));
                Assert.That(payload.receiverId, Is.EqualTo(merchant.ID));
                Assert.That(entry.ActorId, Is.EqualTo(merchant.ID));
                Assert.That(entry.TargetId, Is.EqualTo(box.ID));
            }
            finally { if (!previous) Diag.SetChannel("trade", false); }
        }

        [System.Serializable]
        private sealed class ShipmentStockTrace
        {
            public string orderId = "";
            public string boxId = "";
            public string shelfId = "";
            public string receiverId = "";
        }

        [Test]
        public void AReplacementMerchantOrCargoCannotCollectTheReceipt()
        {
            var receipt = Arrange(delivered: true);
            var original = receipt.Merchant;
            var cargo = receipt.Ledger.GetPart<LocalPorterLedgerPart>().Box.GetPart<ContainerPart>();
            var measure = receipt.FirstMeasure;
            Assert.That(Zone.RemoveEntity(original), Is.True);
            var substitute = Place("SpreadWaysideCook", 11, 10);
            substitute.ID = original.ID;
            Assert.That(receipt.TryStock(Zone), Is.False);
            Assert.That(Zone.RemoveEntity(substitute), Is.True);
            Assert.That(Zone.AddEntity(original, 11, 10), Is.True);
            var replacement = Factory.CreateEntity("PreparedLiningClay");
            replacement.GetPart<StackerPart>().StackCount = 2;
            replacement.ID = measure.ID;
            Assert.That(cargo.RemoveItem(measure), Is.True);
            Assert.That(cargo.AddItem(replacement), Is.True);
            Assert.That(receipt.TryStock(Zone), Is.False);
            Assert.That(cargo.Contents.Single(), Is.SameAs(replacement));
            Assert.That(TradeSystem.GetTraderStock(original).Count, Is.EqualTo(1));
        }

        [Test]
        public void AReceiptSurvivesFreshLoadBeforeAndAfterOrdinaryStocking()
        {
            Assert.That(Zone.RemoveEntity(Actor), Is.True);
            Zone = new Zone("Overworld.12.11.0");
            Assert.That(Zone.AddEntity(Actor, 10, 10), Is.True);
            var receipt = Arrange(delivered: true);
            string shelfID = receipt.ParentEntity.ID;
            string measureID = receipt.FirstMeasure.ID;
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.SetActiveZone(Zone);

            receipt = ReloadReceipt(manager, shelfID, out manager);
            Assert.That(receipt.FirstMeasure.ID, Is.EqualTo(measureID));
            Assert.That(receipt.TryStock(Zone), Is.True);
            string orderID = receipt.StockedOrderID;
            receipt = ReloadReceipt(manager, shelfID, out manager);
            Assert.That(receipt.StockedOrderID, Is.EqualTo(orderID));
            var arrived = TradeSystem.GetTraderStock(receipt.Merchant).Single(item => item.ID == measureID);
            Assert.That(receipt.TryStock(Zone), Is.False);
            TradeSystem.SetDrams(Actor, 50);
            Assert.That(TradeSystem.BuyFromTrader(Actor, receipt.Merchant, arrived), Is.True);
            TraderRestockSystem.RestockZone(Zone, TraderRestockSystem.RestockIntervalTurns + 1);
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant).Any(item => item.ID == measureID), Is.False);
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant).Count(item => item.BlueprintName == "PreparedLiningClay"), Is.EqualTo(1));
            Assert.That(receipt.TryStock(Zone), Is.False);
        }

        [Test]
        public void APortersWagesDoNotEnrollThemInLegacyPurseRestocking()
        {
            var worker = Place("GleanersPorter", 12, 10);
            var legacyMerchant = Place("Creature", 12, 12);
            legacyMerchant.SetTag("Faction", "Villagers");
            TradeSystem.SetDrams(legacyMerchant, 1);
            TradeSystem.SetDrams(Actor, 10);
            Assert.That(Ledger.TryTransfer(Actor, worker, 4, "porter wages", out _), Is.True);

            TraderRestockSystem.RestockZone(Zone, TraderRestockSystem.RestockIntervalTurns + 1);
            Assert.That(TradeSystem.GetDrams(legacyMerchant), Is.EqualTo(TraderRestockSystem.DramsFloor));
            Assert.That(TradeSystem.GetDrams(worker), Is.EqualTo(4));
        }

        [Test]
        public void ASelectedSourcePaysTheSameWorkerToMoveAndStockTheExactBatch()
        {
            var receipt = Arrange(delivered: false, atSource: true);
            var order = receipt.Ledger.GetPart<LocalPorterLedgerPart>();
            var source = Sponsor(receipt.Ledger, 8);
            var measure = receipt.FirstMeasure;
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant).Count, Is.EqualTo(1));
            Assert.That(Actions(receipt.Ledger).Any(action => action.Command.StartsWith(LocalPorterLedgerPart.HirePrefix)), Is.False);

            Assert.That(AIBoredEvent.Check(source), Is.False);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Booked));
            Assert.That(TradeSystem.GetDrams(source), Is.EqualTo(8 - order.Fare));
            Assert.That(TradeSystem.GetDrams(receipt.Ledger), Is.EqualTo(order.Fare));
            Assert.That(order.TryAct(Zone), Is.True);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Carrying));
            Assert.That(order.Box.GetPart<PhysicsPart>().InInventory, Is.SameAs(order.Worker));
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant).Count, Is.EqualTo(1));
            for (int i = 0; i < 34 && order.Phase != LocalPorterPhase.Delivered; i++) order.TryAct(Zone);

            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Delivered));
            Assert.That(order.Shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(order.Box));
            Assert.That(LocalPorterReservationPart.Blocks(order.Box), Is.True);
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant).Count, Is.EqualTo(1));
            Assert.That(TradeSystem.GetDrams(order.Worker), Is.EqualTo(order.Fare));
            Assert.That(TradeSystem.GetDrams(receipt.Ledger), Is.Zero);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant), Does.Contain(measure));
            Assert.That(TradeSystem.GetDrams(Actor), Is.Zero);
            TraderRestockSystem.RestockZone(Zone, TraderRestockSystem.RestockIntervalTurns + 1);
            Assert.That(AIBoredEvent.Check(source), Is.True);
            Assert.That(TradeSystem.GetDrams(source), Is.EqualTo(8 - order.Fare));
            Assert.That(TradeSystem.GetDrams(order.Worker), Is.EqualTo(order.Fare));
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Delivered));
        }

        [Test]
        public void AnUnderfundedSourceKeepsItsUnmovedReservedGoods()
        {
            var receipt = Arrange(delivered: false, atSource: true);
            var order = receipt.Ledger.GetPart<LocalPorterLedgerPart>();
            var source = Sponsor(receipt.Ledger, 1);
            Assert.That(AIBoredEvent.Check(source), Is.True);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Open));
            Assert.That(TradeSystem.GetDrams(receipt.Ledger), Is.Zero);
            Assert.That(TradeSystem.GetDrams(source), Is.EqualTo(1));
            Assert.That(Zone.GetEntityPosition(order.Box), Is.EqualTo((10, 11)));
            Assert.That(LocalPorterReservationPart.Blocks(order.Box), Is.True);
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant).Count, Is.EqualTo(1));
            Assert.That(receipt.ExpectedOrderID, Is.Null);
            Assert.That(receipt.ExpectedByTick, Is.EqualTo(-1));
        }

        [Test]
        public void BookingBindsTheQuoteToOneSavedExpectedTime()
        {
            var receipt = Arrange(delivered: false, atSource: true);
            var order = receipt.Ledger.GetPart<LocalPorterLedgerPart>();
            var source = Sponsor(receipt.Ledger, 8);
            Clock.AdvanceClock(1170);
            Assert.That(order.Worker.GetPart<LocalPorterPart>()
                .TryQuote(source, order.Box, order.Shelf, Zone, out var quote, out _), Is.True);

            Assert.That(AIBoredEvent.Check(source), Is.False);
            Assert.That(receipt.ExpectedOrderID, Is.EqualTo(order.OrderID));
            Assert.That(receipt.ExpectedByTick, Is.EqualTo(1170 + (quote.Steps.Count + 2) * 10));
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(-1));
            var lines = new System.Collections.Generic.List<string>();
            StateReader.Collect(order.ParentEntity, true, Zone, Zone.GetEntityCell(order.ParentEntity), Actor, lines);
            Assert.That(string.Join(" ", lines), Does.Contain("day 2").And.Contain("40 ticks"));
            Assert.That(Clock.TickCount, Is.EqualTo(1170), "Reading must be free.");
        }

        [Test]
        public void AConcernOpensOnceAfterGraceAndClosesAtPhysicalHandover()
        {
            var (receipt, order) = BookForDeadline();
            Assert.That(order.TryAct(Zone), Is.True);
            int graceEnd = receipt.ExpectedByTick + 40;
            Clock.AdvanceClock(graceEnd);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(-1));
            Clock.AdvanceClock(1);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(graceEnd + 1));
            Clock.AdvanceClock(9);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(graceEnd + 1));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));

            var inventory = receipt.Merchant.GetPart<InventoryPart>();
            inventory.MaxWeight = inventory.GetCarriedWeight();
            FinishDelivery(order);
            Assert.That(receipt.ArrivedAtTick, Is.EqualTo(graceEnd + 10));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(receipt.ArrivedAtTick));
            Assert.That(receipt.StockedOrderID, Is.Null);
            Assert.That(order.Shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(order.Box));
            Clock.AdvanceClock(10);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(graceEnd + 10));
            Assert.That(receipt.StockedOrderID, Is.Null);
        }

        [Test]
        public void AnOnTimeBoxAwaitingUnpackingNeverCausesConcern()
        {
            var (receipt, order) = BookForDeadline();
            var inventory = receipt.Merchant.GetPart<InventoryPart>();
            inventory.MaxWeight = inventory.GetCarriedWeight();
            Clock.AdvanceClock(10);
            FinishDelivery(order);
            Assert.That(receipt.ArrivedAtTick, Is.EqualTo(10));
            Clock.AdvanceClock(400);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(-1));
            Assert.That(receipt.StockedOrderID, Is.Null);
            Assert.That(order.Shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(order.Box));
        }

        [Test]
        public void ACarrierDeathDoesNotReportTheCauseToTheRecipient()
        {
            var (receipt, order) = BookForDeadline();
            Assert.That(order.TryAct(Zone), Is.True);
            order.Worker.GetStat("Hitpoints").BaseValue = 0;
            CombatSystem.HandleDeath(order.Worker, null, Zone);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Failed));
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(-1));

            Clock.AdvanceClock(receipt.ExpectedByTick + 41);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(Clock.TickCount));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            var lines = new System.Collections.Generic.List<string>();
            StateReader.Collect(receipt.ParentEntity, true, Zone, Zone.GetEntityCell(receipt.ParentEntity), Actor, lines);
            string read = string.Join(" ", lines);
            Assert.That(read, Does.Contain("overdue").And.Not.Contain("dead").And.Not.Contain("killed"));
        }

        [Test]
        public void TheConcernWaitsForActiveLocalPlayAndSurvivesFreshContinue()
        {
            Assert.That(Zone.RemoveEntity(Actor), Is.True);
            Zone = new Zone("Overworld.12.11.0");
            Assert.That(Zone.AddEntity(Actor, 10, 10), Is.True);
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.SetActiveZone(Zone);
            var (receipt, order) = BookForDeadline();
            Assert.That(order.TryAct(Zone), Is.True);
            string shelfID = receipt.ParentEntity.ID;
            int due = receipt.ExpectedByTick;
            var away = new Zone("Overworld.12.10.0");
            manager.SetActiveZone(away);
            Clock.AdvanceClock(due + 41);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(-1));
            Assert.That(order.Box.GetPart<PhysicsPart>().InInventory, Is.SameAs(order.Worker));
            manager.SetActiveZone(Zone);

            receipt = ReloadReceipt(manager, shelfID, out manager, withClock: true);
            Assert.That(receipt.ExpectedByTick, Is.EqualTo(due));
            Assert.That(WorldClock.CurrentTick, Is.EqualTo(due + 41));
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(due + 41));
            receipt = ReloadReceipt(manager, shelfID, out manager, withClock: true);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(due + 41));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            Assert.That(receipt.ExpectedOrderID, Is.EqualTo(receipt.Ledger.GetPart<LocalPorterLedgerPart>().OrderID));
        }

        private (LocalShipmentReceiverPart receipt, LocalPorterLedgerPart order) BookForDeadline(bool split = false)
        {
            var receipt = Arrange(delivered: false, split: split, atSource: true);
            var order = receipt.Ledger.GetPart<LocalPorterLedgerPart>();
            Assert.That(AIBoredEvent.Check(Sponsor(receipt.Ledger, 8)), Is.False);
            return (receipt, order);
        }

        private void FinishDelivery(LocalPorterLedgerPart order)
        {
            for (int i = 0; i < 34 && order.Phase != LocalPorterPhase.Delivered; i++) order.TryAct(Zone);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Delivered));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void AnOverdueCookCanPayOneEmployeeToSearchAndStayAtTheKitchen(bool reload, bool missedReturn)
        {
            Assert.That(Zone.RemoveEntity(Actor), Is.True);
            Zone = new Zone("Overworld.12.11.0");
            Assert.That(Zone.AddEntity(Actor, 10, 10), Is.True);
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.SetActiveZone(Zone);
            var (receipt, order) = BookForDeadline();
            var cook = receipt.Merchant;
            var hand = Place("Creature", 15, 11);
            hand.SetTag("Faction", "Villagers");
            hand.AddPart(new LocalShipmentEmploymentPart { Employer = cook });
            receipt.SearchEmployee = hand;
            int cookDrams = TradeSystem.GetDrams(cook);
            var cookAt = Zone.GetEntityPosition(cook);
            var handAt = Zone.GetEntityPosition(hand);
            Assert.That(order.TryAct(Zone), Is.True);
            Clock.AdvanceClock(receipt.ExpectedByTick + 41);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(Clock.TickCount));
            Assert.That(hand.GetPart<LocalShipmentSearchPart>(), Is.Null);
            Assert.That(TradeSystem.GetDrams(cook), Is.EqualTo(cookDrams));

            Clock.AdvanceClock(40);
            AIBoredEvent.Check(cook);
            var search = hand.GetPart<LocalShipmentSearchPart>();
            Assert.That(search, Is.Not.Null);
            Assert.That(search.OrderID, Is.EqualTo(order.OrderID));
            Assert.That(TradeSystem.GetDrams(cook), Is.EqualTo(cookDrams - 2));
            Assert.That(TradeSystem.GetDrams(hand), Is.EqualTo(2));
            AIBoredEvent.Check(cook);
            Assert.That(hand.GetPart<LocalShipmentSearchPart>(), Is.SameAs(search));
            Assert.That(TradeSystem.GetDrams(hand), Is.EqualTo(2));
            for (int i = 0; i < 4 && Zone.GetEntityPosition(hand) == handAt; i++) AIBoredEvent.Check(hand);
            Assert.That(Zone.GetEntityPosition(hand), Is.Not.EqualTo(handAt));
            Assert.That(Zone.GetEntityPosition(cook), Is.EqualTo(cookAt));
            Assert.That(receipt.SearchReportAtTick, Is.EqualTo(-1));
            int due = receipt.DelegateDueAtTick;
            if (reload)
            {
                var position = Zone.GetEntityPosition(hand);
                receipt = ReloadReceipt(manager, receipt.ParentEntity.ID, out manager, withClock: true);
                cook = receipt.Merchant;
                hand = receipt.SearchEmployee;
                search = hand.GetPart<LocalShipmentSearchPart>();
                Assert.That(receipt.DelegatedSearcherID, Is.EqualTo(hand.ID));
                Assert.That(receipt.DelegateDueAtTick, Is.EqualTo(due));
                Assert.That(hand.GetPart<LocalShipmentEmploymentPart>().Employer, Is.SameAs(cook));
                Assert.That(Zone.GetEntityPosition(hand), Is.EqualTo(position));
                Assert.That(search.Shelf, Is.SameAs(receipt.ParentEntity));
                AIBoredEvent.Check(cook);
                Assert.That(TradeSystem.GetDrams(cook), Is.EqualTo(cookDrams - 2));
                Assert.That(TradeSystem.GetDrams(hand), Is.EqualTo(2));
            }
            if (missedReturn)
            {
                Clock.AdvanceClock(due - Clock.TickCount + 1);
                AIBoredEvent.Check(cook);
                Assert.That(cook.GetPart<LocalShipmentSearchPart>().Phase, Is.EqualTo(LocalShipmentSearchPhase.CheckingLoadingPoint));
                FinishSearch(search, hand);
                Assert.That(receipt.SearchReportAtTick, Is.EqualTo(-1));
                Assert.That(TradeSystem.GetDrams(cook), Is.EqualTo(cookDrams - 2));
                Assert.That(TradeSystem.GetDrams(hand), Is.EqualTo(2));
                return;
            }
            Assert.That(Zone.MoveEntity(cook, 22, 10), Is.True);
            for (int i = 0; i < 54; i++) AIBoredEvent.Check(hand);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.Returning));
            Assert.That(receipt.SearchReportAtTick, Is.EqualTo(-1));
            Assert.That(Zone.MoveEntity(cook, 17, 10), Is.True);
            FinishSearch(search, hand);
            Assert.That(search.CheckedLoadingPoint, Is.True);
            Assert.That(receipt.SearchReportAtTick, Is.EqualTo(WorldClock.CurrentTick));
            Assert.That(receipt.SearchReportSawBody, Is.False);
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Contain("hand returned"));
            TraderRestockSystem.RestockZone(Zone, TraderRestockSystem.RestockIntervalTurns + 1);
            Assert.That(TradeSystem.GetDrams(hand), Is.EqualTo(2));
            Assert.That(Zone.RemoveEntity(hand), Is.True);
            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Contain("hand returned"));
        }

        [TestCase(false, 35)]
        [TestCase(true, 1)]
        public void AnUnavailablePaidSearchLeavesThePersonalSearchAndPursesIntact(bool employed, int cookDrams)
        {
            var (receipt, order) = BookForDeadline();
            var cook = receipt.Merchant;
            var hand = Place("Creature", 15, 11);
            hand.AddPart(new LocalShipmentEmploymentPart { Employer = employed ? cook : Actor });
            receipt.SearchEmployee = hand;
            TradeSystem.SetDrams(cook, cookDrams);
            Assert.That(order.TryAct(Zone), Is.True);
            Clock.AdvanceClock(receipt.ExpectedByTick + 41);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Clock.AdvanceClock(40);
            AIBoredEvent.Check(cook);

            Assert.That(cook.GetPart<LocalShipmentSearchPart>().Phase, Is.EqualTo(LocalShipmentSearchPhase.CheckingLoadingPoint));
            Assert.That(hand.GetPart<LocalShipmentSearchPart>(), Is.Null);
            Assert.That(TradeSystem.GetDrams(cook), Is.EqualTo(cookDrams));
            Assert.That(TradeSystem.GetDrams(hand), Is.Zero);
        }

        [Test]
        public void TheSameRecipientChecksTheMarkerAndReturnsOnceForThisConcern()
        {
            var (receipt, order, search) = PrepareSearch();
            var merchant = receipt.Merchant;
            int purse = TradeSystem.GetDrams(merchant);
            var stock = TradeSystem.GetTraderStock(merchant).ToArray();
            Assert.That(AIBoredEvent.Check(merchant), Is.True);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.Waiting));
            Clock.AdvanceClock(40);
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.CheckingLoadingPoint));

            var start = Zone.GetEntityPosition(merchant);
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            Assert.That(Zone.GetEntityPosition(merchant), Is.Not.EqualTo(start));
            var reader = new System.Collections.Generic.List<string>();
            StateReader.Collect(merchant, true, Zone, Zone.GetEntityCell(merchant), Actor, reader);
            Assert.That(string.Join(" ", reader), Does.Contain("loading point"));
            for (int i = 0; i < 26 && search.Phase == LocalShipmentSearchPhase.CheckingLoadingPoint; i++) AIBoredEvent.Check(merchant);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.Returning));
            Assert.That(search.CheckedLoadingPoint, Is.True);
            Assert.That(SpatialQuery.Distance(Zone, merchant, order.ParentEntity), Is.LessThanOrEqualTo(1));

            FinishSearch(search, merchant);
            Assert.That(SpatialQuery.Distance(Zone, merchant, order.Shelf), Is.LessThanOrEqualTo(1));
            Assert.That(search.OrderID, Is.EqualTo(order.OrderID));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            Assert.That(AIBoredEvent.Check(merchant), Is.True);
            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Not.Contain("found the missing porter"));
            Assert.That(TradeSystem.GetDrams(merchant), Is.EqualTo(purse));
            Assert.That(TradeSystem.GetTraderStock(merchant), Is.EqualTo(stock));
        }

        [Test]
        public void AWalkingRecipientPausesForAnInactiveZoneAndContinuesFromTheSavedPosition()
        {
            Assert.That(Zone.RemoveEntity(Actor), Is.True);
            Zone = new Zone("Overworld.12.11.0");
            Assert.That(Zone.AddEntity(Actor, 10, 10), Is.True);
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.SetActiveZone(Zone);
            var (receipt, _, search) = PrepareSearch();
            var merchant = receipt.Merchant;
            string shelfID = receipt.ParentEntity.ID;
            Clock.AdvanceClock(40);
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            var position = Zone.GetEntityPosition(merchant);
            int attempts = search.LegAttempts;

            manager.SetActiveZone(new Zone("Overworld.12.10.0"));
            Assert.That(AIBoredEvent.Check(merchant), Is.True);
            Assert.That(Zone.GetEntityPosition(merchant), Is.EqualTo(position));
            Assert.That(search.LegAttempts, Is.EqualTo(attempts));
            manager.SetActiveZone(Zone);
            receipt = ReloadReceipt(manager, shelfID, out manager, withClock: true);
            merchant = receipt.Merchant;
            search = merchant.GetPart<LocalShipmentSearchPart>();
            Assert.That(search.Shelf, Is.SameAs(receipt.ParentEntity));
            Assert.That(search.OrderID, Is.EqualTo(receipt.ExpectedOrderID));
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.CheckingLoadingPoint));
            Assert.That(search.LegAttempts, Is.EqualTo(attempts));
            Assert.That(Zone.GetEntityPosition(merchant), Is.EqualTo(position));
            FinishSearch(search, merchant);
            Assert.That(search.CheckedLoadingPoint, Is.True);
        }

        [Test]
        public void AWorkingCookAndARootedSearcherKeepTheirPositions()
        {
            var (receipt, _, search) = PrepareSearch();
            var merchant = receipt.Merchant;
            var pan = Place("ConnectedBatchPan", 17, 11).GetPart<KitchenBatchPart>();
            pan.Worker = merchant;
            pan.Configured = true;
            pan.State = "Working";
            Clock.AdvanceClock(40);
            var origin = Zone.GetEntityPosition(merchant);
            Assert.That(AIBoredEvent.Check(merchant), Is.True);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.Waiting));
            Assert.That(Zone.GetEntityPosition(merchant), Is.EqualTo(origin));
            pan.State = "Idle";
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            Assert.That(merchant.ApplyEffect(new RootedEffect(), Actor, Zone), Is.True);
            Assert.That(AIBoredEvent.Check(merchant), Is.True);
            Assert.That(search.LegAttempts, Is.Zero);
            Assert.That(Zone.GetEntityPosition(merchant), Is.EqualTo(origin));
            merchant.RemoveEffect<RootedEffect>();
            FinishSearch(search, merchant);
        }

        [Test]
        public void AReceiptBeforeDepartureLetsTheCookStayAtWork()
        {
            var (receipt, order, search) = PrepareSearch();
            var merchant = receipt.Merchant;
            var original = Zone.GetEntityPosition(merchant);
            FinishDelivery(order);
            Clock.AdvanceClock(50);
            Assert.That(AIBoredEvent.Check(merchant), Is.True);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.Waiting));
            Assert.That(Zone.GetEntityPosition(merchant), Is.EqualTo(original));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(receipt.ArrivedAtTick));
        }

        [Test]
        public void ABlockedSearchStopsAfterOneBoundedAttemptAndLeavesTheNoticeOpen()
        {
            var (receipt, _, search) = PrepareSearch();
            var merchant = receipt.Merchant;
            var original = Zone.GetEntityPosition(merchant);
            Clock.AdvanceClock(40);
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            Place("StoneWall", 14, 10);
            Place("StoneWall", 16, 10);
            Place("StoneWall", 15, 9);
            Place("StoneWall", 15, 11);
            for (int i = 0; i < 24; i++)
                Assert.That(AIBoredEvent.Check(merchant), Is.True, "No search action is consumed while all approaches are blocked.");
            Assert.That(search.LegAttempts, Is.EqualTo(24));
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.Returning));
            FinishSearch(search, merchant);
            Assert.That(search.CheckedLoadingPoint, Is.False);
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            Assert.That(Zone.GetEntityPosition(merchant), Is.EqualTo(original));
            Assert.That(AIBoredEvent.Check(merchant), Is.True);
        }

        [Test]
        public void ADisplacedLoadingPointTurnsTheSearcherBackToTheOriginalShelf()
        {
            var (receipt, order, search) = PrepareSearch();
            var merchant = receipt.Merchant;
            Clock.AdvanceClock(40);
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            Assert.That(Zone.MoveEntity(order.ParentEntity, 8, 15), Is.True);
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.Returning));
            FinishSearch(search, merchant);
            Assert.That(SpatialQuery.Distance(Zone, merchant, order.Shelf), Is.LessThanOrEqualTo(1));
            Assert.That(search.CheckedLoadingPoint, Is.False);
        }

        [Test]
        public void ARemovedReceivingShelfEndsTheExcursionInPlace()
        {
            var (receipt, order, search) = PrepareSearch();
            var merchant = receipt.Merchant;
            Clock.AdvanceClock(40);
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            Assert.That(AIBoredEvent.Check(merchant), Is.False);
            var position = Zone.GetEntityPosition(merchant);
            Assert.That(Zone.RemoveEntity(order.Shelf), Is.True);
            Assert.That(AIBoredEvent.Check(merchant), Is.True);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.Stopped));
            Assert.That(Zone.GetEntityPosition(merchant), Is.EqualTo(position));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SeeingTheAssignedPorterAliveOnlyChangesTheNoticeAfterReturning(bool delegated, bool reportOverdue)
        {
            Assert.That(Zone.RemoveEntity(Actor), Is.True);
            Zone = new Zone("Overworld.12.11.0");
            Assert.That(Zone.AddEntity(Actor, 10, 10), Is.True);
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.SetActiveZone(Zone);
            var (receipt, order, search) = PrepareSearch();
            var cook = receipt.Merchant;
            var porter = order.Worker;
            var searcher = cook;
            if (delegated)
            {
                searcher = Place("Creature", 15, 11);
                searcher.SetTag("Faction", "Villagers");
                searcher.AddPart(new LocalShipmentEmploymentPart { Employer = cook });
                receipt.SearchEmployee = searcher;
            }

            string shelfID = receipt.ParentEntity.ID;
            string parcelID = order.Box.ID;
            string porterID = porter.ID;
            var opening = TradeSystem.GetTraderStock(cook).Single();
            Clock.AdvanceClock(40);
            Assert.That(AIBoredEvent.Check(cook), Is.False);
            search = searcher.GetPart<LocalShipmentSearchPart>();
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.CheckingLoadingPoint));
            Assert.That(Zone.MoveEntity(porter, 14, 10), Is.True);
            Assert.That(AIBoredEvent.Check(searcher), Is.False);
            Assert.That(search.SeenLivingPorterID, Is.EqualTo(porterID));
            Assert.That(search.SeenLivingPorterAtTick, Is.EqualTo(WorldClock.CurrentTick));
            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Not.Contain("porter alive"));

            // The recorded sighting should survive even after the worker leaves sight.
            Assert.That(Zone.MoveEntity(porter, 28, 10), Is.True);
            receipt = ReloadReceipt(manager, shelfID, out _, withClock: true);
            searcher = delegated ? receipt.SearchEmployee : receipt.Merchant;
            search = searcher.GetPart<LocalShipmentSearchPart>();
            Assert.That(search.SeenLivingPorterID, Is.EqualTo(porterID));
            if (reportOverdue)
            {
                TurnManager.Active.AdvanceClock(receipt.DelegateDueAtTick - WorldClock.CurrentTick + 1);
                Assert.That(WorldClock.CurrentTick, Is.GreaterThan(receipt.DelegateDueAtTick));
            }
            FinishSearch(search, searcher);

            order = receipt.Ledger.GetPart<LocalPorterLedgerPart>();
            Assert.That(ReadCurrentState(receipt.ParentEntity).Contains("porter alive"), Is.EqualTo(!reportOverdue));
            Assert.That(ReadCurrentState(searcher).Contains("porter alive"), Is.EqualTo(!reportOverdue));
            Assert.That(search.CheckedLoadingPoint, Is.EqualTo(!reportOverdue), "the sighting itself does not change the route");
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            Assert.That(receipt.ArrivedAtTick, Is.EqualTo(-1));
            Assert.That(order.Worker.ID, Is.EqualTo(porterID));
            Assert.That(order.Box.ID, Is.EqualTo(parcelID));
            Assert.That(order.Box.GetPart<PhysicsPart>().InInventory, Is.SameAs(order.Worker));
            Assert.That(order.Box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(receipt.FirstMeasure));
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant).Single().ID, Is.EqualTo(opening.ID));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AnUnseenPorterOrAnotherPorterCannotGiveALivingReport(bool exactButInvisible)
        {
            var (receipt, order, search) = PrepareSearch();
            if (exactButInvisible)
                order.Worker.GetPart<RenderPart>().Visible = false;
            else
                Assert.That(Zone.MoveEntity(order.Worker, 28, 10), Is.True);
            Place("GleanersPorter", 13, 10);
            Clock.AdvanceClock(40);
            FinishSearch(search, receipt.Merchant);

            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Not.Contain("porter alive"));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OnlyASeparatelySeenBodyCanSupersedeAnEarlierLivingSighting(bool bodyInSight)
        {
            var (receipt, order, search) = PrepareSearch();
            var searcher = receipt.Merchant;
            var porter = order.Worker;
            Clock.AdvanceClock(40);
            Assert.That(AIBoredEvent.Check(searcher), Is.False);
            Assert.That(Zone.MoveEntity(porter, 14, 10), Is.True);
            Assert.That(AIBoredEvent.Check(searcher), Is.False);
            Assert.That(search.SeenLivingPorterID, Is.EqualTo(porter.ID));
            if (!bodyInSight) Assert.That(Zone.MoveEntity(porter, 28, 10), Is.True);

            KillAndFindBody(porter);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            FinishSearch(search, searcher);

            string report = ReadCurrentState(receipt.ParentEntity);
            Assert.That(report.Contains("porter dead"), Is.EqualTo(bodyInSight));
            Assert.That(report.Contains("porter alive"), Is.EqualTo(!bodyInSight));
            Assert.That(report, Does.Not.Contain("killed"));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
        }

        [Test]
        public void ARealBodyIsReportedOnlyAfterTheSearcherSeesItAndReturns()
        {
            Assert.That(Zone.RemoveEntity(Actor), Is.True);
            Zone = new Zone("Overworld.12.11.0");
            Assert.That(Zone.AddEntity(Actor, 10, 10), Is.True);
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.SetActiveZone(Zone);
            var (receipt, order, search) = PrepareSearch();
            string shelfID = receipt.ParentEntity.ID;
            string parcelID = order.Box.ID;
            var wall = Place("StoneWall", 13, 10);
            Assert.That(Zone.MoveEntity(order.Worker, 12, 10), Is.True);
            var body = KillAndFindBody(order.Worker);
            Assert.That(body.GetProperty("KillerID"), Is.EqualTo(Actor.ID));
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Failed));
            Assert.That(Zone.GetEntityCell(order.Box), Is.Not.Null);
            Assert.That(order.Box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(receipt.FirstMeasure));
            Assert.That(receipt.Merchant.HasEffect<WitnessedEffect>(), Is.False);
            Clock.AdvanceClock(40);
            Assert.That(AIBoredEvent.Check(receipt.Merchant), Is.False);
            Assert.That(AIBoredEvent.Check(receipt.Merchant), Is.False);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.CheckingLoadingPoint));
            Assert.That(search.SeenPorterBodyAtTick, Is.EqualTo(-1));
            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Not.Contain("found the missing porter"));

            Assert.That(Zone.RemoveEntity(wall), Is.True);
            receipt = ReloadReceipt(manager, shelfID, out manager, withClock: true);
            search = receipt.Merchant.GetPart<LocalShipmentSearchPart>();
            Assert.That(AIBoredEvent.Check(receipt.Merchant), Is.False);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.Returning));
            Assert.That(search.CheckedLoadingPoint, Is.False);
            Assert.That(search.SeenPorterBodyID, Is.EqualTo(body.ID));
            Assert.That(search.SeenPorterBodyAtTick, Is.EqualTo(WorldClock.CurrentTick));
            Assert.That((search.SeenPorterBodyX, search.SeenPorterBodyY), Is.EqualTo((12, 10)));
            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Not.Contain("found the missing porter"));

            var loadedBody = Zone.GetReadOnlyEntities().Single(entity => entity.ID == body.ID);
            Assert.That(Zone.RemoveEntity(loadedBody), Is.True);
            receipt = ReloadReceipt(manager, shelfID, out _, withClock: true);
            search = receipt.Merchant.GetPart<LocalShipmentSearchPart>();
            Assert.That(search.SeenPorterBodyID, Is.EqualTo(body.ID));
            FinishSearch(search, receipt.Merchant);
            string report = ReadCurrentState(receipt.ParentEntity);
            Assert.That(report, Does.Contain("found the missing porter dead").And.Not.Contain("killed"));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            var parcel = Zone.GetReadOnlyEntities().Single(item => item.ID == parcelID);
            Assert.That(parcel.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(receipt.FirstMeasure));
            Assert.That(LocalPorterReservationPart.Blocks(parcel), Is.False);
        }

        [Test]
        public void AnotherPortersBodyAndAConcealedBodyGiveNoReport()
        {
            var (receipt, order, search) = PrepareSearch();
            var wall = Place("StoneWall", 13, 10);
            var other = Place("GleanersPorter", 12, 10);
            var wrongBody = KillAndFindBody(other);
            Assert.That(Zone.MoveEntity(order.Worker, 12, 10), Is.True);
            var body = KillAndFindBody(order.Worker);
            Assert.That(Zone.RemoveEntity(wall), Is.True);
            Assert.That(Zone.RemoveEntity(body), Is.True);
            Assert.That(Pack.AddObject(body), Is.True);
            Assert.That(Zone.GetEntityCell(wrongBody), Is.Not.Null);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Failed));
            Clock.AdvanceClock(40);
            FinishSearch(search, receipt.Merchant);
            Assert.That(search.CheckedLoadingPoint, Is.True);
            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Not.Contain("found the missing porter"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void APlayerReportChangesTheExistingSearchRouteWithoutVerifyingTheBody(bool report)
        {
            var (receipt, order, search, body) = PrepareWitnessLead();
            var cook = receipt.Merchant;
            var cargo = order.Box.GetPart<ContainerPart>();
            var original = cargo.Contents.Single();
            var cookAt = Zone.GetEntityPosition(cook);
            int attempts = search.LegAttempts;
            int purse = TradeSystem.GetDrams(cook);

            if (report)
            {
                var pile = Zone.GetEntityCell(body);
                Assert.That(WorldInteractionSystem.IsPileCell(pile, Actor), Is.True);
                var note = WorldInteractionSystem.BuildPileSummaryActions(pile, Actor)
                    .Single(IsWitnessNote);
                Assert.That(note.Via, Is.SameAs(receipt.ParentEntity));
                Assert.That(note.Time, Is.EqualTo(ActionTime.Free));
                Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Not.Contain("reported").IgnoreCase);
                Assert.That(ExecuteWitnessRow(note), Is.True);
                Assert.That(ExecuteWitnessRow(note), Is.False, "The same observation cannot be recorded twice.");
                Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Not.Contain("reported").IgnoreCase);
            }

            Assert.That(Zone.MoveEntity(Actor, 17, 13), Is.True);
            var reportRows = CellMenu(cookAt.x, cookAt.y).Where(IsWitnessReport).ToArray();
            Assert.That(reportRows, Has.Length.EqualTo(report ? 1 : 0));
            if (report)
            {
                var offer = reportRows.Single();
                Assert.That(offer.Via, Is.SameAs(receipt.ParentEntity));
                Assert.That(offer.Time, Is.EqualTo(ActionTime.OneAction));
                Assert.That(ExecuteWitnessRow(offer), Is.True);
                Assert.That(ExecuteWitnessRow(offer), Is.False);
                Assert.That(CellMenu(cookAt.x, cookAt.y).Where(IsWitnessReport), Is.Empty);
                Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Contain("reported").IgnoreCase);
            }

            Assert.That(search.SeenPorterBodyID, Is.Null);
            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Not.Contain("cook found").IgnoreCase);
            Assert.That(search.LegAttempts, Is.EqualTo(attempts));
            Assert.That(AIBoredEvent.Check(cook), Is.False);
            var step = Zone.GetEntityPosition(cook);
            if (report)
            {
                var lead = Zone.GetEntityPosition(body);
                int DistanceToLead((int x, int y) at) => System.Math.Abs(at.x - lead.x) + System.Math.Abs(at.y - lead.y);
                Assert.That(DistanceToLead(step), Is.LessThan(DistanceToLead(cookAt)));
            }
            else Assert.That(step.x, Is.EqualTo(cookAt.x - 1));
            Assert.That(search.LegAttempts, Is.EqualTo(attempts + 1));
            Assert.That(search.SeenPorterBodyID, Is.Null);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Failed));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            Assert.That(receipt.ArrivedAtTick, Is.EqualTo(-1));
            Assert.That(cargo.Contents.Single(), Is.SameAs(original));
            Assert.That(TradeSystem.GetDrams(cook), Is.EqualTo(purse));
        }

        [Test]
        public void AVisibleWrongBodyAndAStaleNoteCannotBecomePlayerTestimony()
        {
            var (receipt, _, _, body) = PrepareWitnessLead();
            var unrelatedPorter = Place("GleanersPorter", 23, 12);
            var wrongBody = KillAndFindBody(unrelatedPorter);
            var wrongCell = Zone.GetEntityCell(wrongBody);
            wrongCell.IsVisible = wrongCell.Explored = true;
            Assert.That(WorldInteractionSystem.GatherCellActions(wrongBody, wrongCell, Actor).Where(IsWitnessNote), Is.Empty);

            var pile = Zone.GetEntityCell(body);
            var note = WorldInteractionSystem.BuildPileSummaryActions(pile, Actor).Single(IsWitnessNote);
            Assert.That(Zone.MoveEntity(body, 28, 10), Is.True);
            Assert.That(ExecuteWitnessRow(note), Is.False);
            Assert.That(WorldInteractionSystem.BuildPileSummaryActions(pile, Actor).Where(IsWitnessNote), Is.Empty);

            Assert.That(Zone.MoveEntity(Actor, 17, 13), Is.True);
            var at = Zone.GetEntityPosition(receipt.Merchant);
            Assert.That(CellMenu(at.x, at.y).Where(IsWitnessReport), Is.Empty);
        }

        [Test]
        public void AStaleReportCannotReachTheMovedCookAndCanBeQuotedAgainInPerson()
        {
            var (receipt, _, search, body) = PrepareWitnessLead();
            var pile = Zone.GetEntityCell(body);
            Assert.That(ExecuteWitnessRow(WorldInteractionSystem.BuildPileSummaryActions(pile, Actor).Single(IsWitnessNote)), Is.True);
            Assert.That(Zone.MoveEntity(Actor, 17, 13), Is.True);
            var offer = CellMenu(17, 12).Single(IsWitnessReport);
            Assert.That(Zone.MoveEntity(receipt.Merchant, 19, 12), Is.True);
            var newCell = Zone.GetCell(19, 12);
            newCell.IsVisible = newCell.Explored = true;
            Assert.That(ExecuteWitnessRow(offer), Is.False);
            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Not.Contain("reported").IgnoreCase);
            Assert.That(search.SeenPorterBodyID, Is.Null);
            Assert.That(CellMenu(19, 12).Where(IsWitnessReport), Is.Empty);

            Assert.That(Zone.MoveEntity(Actor, 19, 13), Is.True);
            Assert.That(ExecuteWitnessRow(CellMenu(19, 12).Single(IsWitnessReport)), Is.True);
            Assert.That(ReadCurrentState(receipt.ParentEntity), Does.Contain("reported").IgnoreCase);
        }

        [Test]
        public void ARecalledReportUsesTheOldLocationAndKeepsItsSearchBudgetOnFallback()
        {
            Assert.That(Zone.RemoveEntity(Actor), Is.True);
            Zone = new Zone("Overworld.12.11.0");
            Assert.That(Zone.AddEntity(Actor, 10, 10), Is.True);
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.SetActiveZone(Zone);
            var (receipt, _, _, body) = PrepareWitnessLead();
            string shelfID = receipt.ParentEntity.ID;
            string bodyID = body.ID;
            var historical = Zone.GetEntityPosition(body);
            var pile = Zone.GetEntityCell(body);
            Assert.That(ExecuteWitnessRow(WorldInteractionSystem.BuildPileSummaryActions(pile, Actor).Single(IsWitnessNote)), Is.True);
            Assert.That(Zone.MoveEntity(body, 39, 23), Is.True);
            Assert.That(Zone.MoveEntity(Actor, 17, 13), Is.True);

            receipt = ReloadReceipt(manager, shelfID, out manager, withClock: true);
            Assert.That(receipt.WitnessBodyID, Is.EqualTo(bodyID));
            Assert.That((receipt.WitnessX, receipt.WitnessY), Is.EqualTo(historical));
            var relocated = Zone.GetReadOnlyEntities().Single(entity => entity.ID == bodyID);
            Assert.That(Zone.GetEntityPosition(relocated), Is.EqualTo((39, 23)));
            Assert.That(receipt.WitnessReportedAtTick, Is.EqualTo(-1));
            var cook = receipt.Merchant;
            var cookCell = Zone.GetEntityCell(cook);
            cookCell.IsVisible = cookCell.Explored = true;
            Assert.That(ExecuteWitnessRow(CellMenu(cookCell.X, cookCell.Y).Single(IsWitnessReport)), Is.True);
            int reported = receipt.WitnessReportedAtTick;
            var search = cook.GetPart<LocalShipmentSearchPart>();

            for (int step = 0; step < 14 && !search.CheckedPlayerLead; step++)
            {
                int before = search.LegAttempts;
                var previous = Zone.GetEntityPosition(cook);
                Assert.That(AIBoredEvent.Check(cook), Is.False);
                Assert.That(search.LegAttempts, Is.EqualTo(before + 1), "The detour and fallback share one outbound budget.");
                Assert.That(search.SeenPorterBodyID, Is.Null);
                if (!search.CheckedPlayerLead) continue;
                Assert.That(System.Math.Max(System.Math.Abs(previous.x - historical.x),
                    System.Math.Abs(previous.y - historical.y)), Is.LessThanOrEqualTo(1));
                Assert.That(Zone.GetEntityPosition(cook).x, Is.LessThan(previous.x), "The cook heads back toward the loading point.");
            }
            Assert.That(search.CheckedPlayerLead, Is.True);
            int attempts = search.LegAttempts;
            var fallback = Zone.GetEntityPosition(cook);

            receipt = ReloadReceipt(manager, shelfID, out _, withClock: true);
            cook = receipt.Merchant;
            search = cook.GetPart<LocalShipmentSearchPart>();
            Assert.That(receipt.WitnessReportedAtTick, Is.EqualTo(reported));
            Assert.That((receipt.WitnessX, receipt.WitnessY), Is.EqualTo(historical));
            Assert.That(search.CheckedPlayerLead, Is.True);
            Assert.That(search.LegAttempts, Is.EqualTo(attempts));
            Assert.That(AIBoredEvent.Check(cook), Is.False);
            Assert.That(Zone.GetEntityPosition(cook).x, Is.LessThan(fallback.x));
            Assert.That(search.LegAttempts, Is.EqualTo(attempts + 1));
            Assert.That(search.SeenPorterBodyID, Is.Null);
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            Assert.That(receipt.ArrivedAtTick, Is.EqualTo(-1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AnUnavailableCookCannotReceiveAReportedLead(bool hidden)
        {
            var (receipt, _, _, body) = PrepareWitnessLead();
            var pile = Zone.GetEntityCell(body);
            Assert.That(ExecuteWitnessRow(WorldInteractionSystem.BuildPileSummaryActions(pile, Actor).Single(IsWitnessNote)), Is.True);
            Assert.That(Zone.MoveEntity(Actor, 17, 13), Is.True);
            var offer = CellMenu(17, 12).Single(IsWitnessReport);
            var cook = receipt.Merchant;
            var render = cook.GetPart<RenderPart>();
            if (hidden) render.Visible = false;
            else Assert.That(cook.ApplyEffect(new StunnedEffect(), Actor, Zone), Is.True);
            Assert.That(ExecuteWitnessRow(offer), Is.False);
            Assert.That(CellMenu(17, 12).Where(IsWitnessReport), Is.Empty);
            Assert.That(receipt.WitnessReportedAtTick, Is.EqualTo(-1));

            if (hidden) render.Visible = true;
            else cook.RemoveEffect<StunnedEffect>();
            Assert.That(ExecuteWitnessRow(CellMenu(17, 12).Single(IsWitnessReport)), Is.True);
        }

        private (LocalShipmentReceiverPart receipt, LocalPorterLedgerPart order,
            LocalShipmentSearchPart search, Entity body) PrepareWitnessLead()
        {
            var (receipt, order, search) = PrepareSearch();
            Assert.That(Zone.MoveEntity(order.Worker, 25, 10), Is.True);
            var body = KillAndFindBody(order.Worker);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Failed));
            Assert.That(Zone.GetEntityCell(order.Box), Is.SameAs(Zone.GetEntityCell(body)));
            Clock.AdvanceClock(40);
            Assert.That(AIBoredEvent.Check(receipt.Merchant), Is.False);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.CheckingLoadingPoint));
            Assert.That(Zone.MoveEntity(receipt.Merchant, 17, 12), Is.True);
            Assert.That(Zone.MoveEntity(Actor, 24, 11), Is.True);
            var bodyCell = Zone.GetEntityCell(body);
            bodyCell.IsVisible = bodyCell.Explored = true;
            var cookCell = Zone.GetEntityCell(receipt.Merchant);
            cookCell.IsVisible = cookCell.Explored = true;
            return (receipt, order, search, body);
        }

        private static bool IsWitnessNote(InventoryAction row)
            => row.Command.StartsWith("ShipmentWitnessNote|", System.StringComparison.Ordinal);

        private static bool IsWitnessReport(InventoryAction row)
            => row.Command.StartsWith("ShipmentWitnessReport|", System.StringComparison.Ordinal);

        private bool ExecuteWitnessRow(InventoryAction row)
            => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(row.Via, row.Command), Actor, Zone).Success;

        [Test]
        public void RecoveringTheSurvivingBoxClosesItsCaseOnceWithoutPayingAnotherFare()
        {
            var (receipt, order, _) = PrepareSearch();
            KillAndFindBody(order.Worker);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Failed));
            var crate = order.Box;
            var measure = receipt.FirstMeasure;
            var opening = TradeSystem.GetTraderStock(receipt.Merchant).Single();
            var holder = receipt.Merchant.GetPart<InventoryPart>();
            int purse = TradeSystem.GetDrams(receipt.Sponsor);
            int earnedFare = TradeSystem.GetDrams(order.Worker);

            Assert.That(InventorySystem.ExecuteCommand(new PickupCommand(crate), Actor, Zone).Success, Is.True);
            Assert.That(Zone.MoveEntity(Actor, 16, 11), Is.True);
            Assert.That(InventorySystem.ExecuteCommand(new PutInContainerCommand(order.Shelf, crate), Actor, Zone).Success, Is.True);
            Assert.That(order.Shelf.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(crate));
            holder.MaxWeight = holder.GetCarriedWeight() + 1;
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            Assert.That(receipt.ArrivedAtTick, Is.EqualTo(-1));
            Assert.That(crate.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(measure));
            holder.MaxWeight++;
            Clock.AdvanceClock(10);

            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ArrivedAtTick, Is.EqualTo(Clock.TickCount));
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(receipt.ArrivedAtTick));
            Assert.That(receipt.StockedOrderID, Is.EqualTo(order.OrderID));
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant), Is.EquivalentTo(new[] { opening, measure }));
            Assert.That(crate.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Failed));
            Assert.That(TradeSystem.GetDrams(receipt.Sponsor), Is.EqualTo(purse));
            Assert.That(TradeSystem.GetDrams(order.Worker), Is.EqualTo(earnedFare));
            Assert.That(TradeSystem.GetDrams(receipt.Ledger), Is.Zero);
            Assert.That(TradeSystem.GetDrams(Actor), Is.Zero);
            Assert.That(LocalPorterReservationPart.Blocks(crate), Is.False);
            Assert.That(ReadCurrentState(order.Shelf), Does.Contain("recovered"));
            Clock.AdvanceClock(10);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(Clock.TickCount - 10));
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant), Is.EquivalentTo(new[] { opening, measure }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnrelatedCompatibleCargoCanUseTheShelfWithoutClosingTheFailedShipment(bool useOriginalCrate)
        {
            var (receipt, order, _) = PrepareSearch();
            KillAndFindBody(order.Worker);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            var crate = useOriginalCrate ? order.Box : Place("LiningClayParcel", 10, 10);
            if (useOriginalCrate)
                Assert.That(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(crate, receipt.FirstMeasure), Actor, Zone).Success, Is.True);
            var substitute = Carry("FireClay");
            substitute.GetPart<StackerPart>().StackCount = 2;
            Assert.That(InventorySystem.ExecuteCommand(new PutInContainerCommand(crate, substitute), Actor, Zone).Success, Is.True);
            Assert.That(InventorySystem.ExecuteCommand(new PickupCommand(crate), Actor, Zone).Success, Is.True);
            Assert.That(Zone.MoveEntity(Actor, 16, 11), Is.True);
            Assert.That(InventorySystem.ExecuteCommand(new PutInContainerCommand(order.Shelf, crate), Actor, Zone).Success, Is.True);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            Assert.That(receipt.StockedOrderID, Is.Null);
            Assert.That(crate.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(substitute));
            if (!useOriginalCrate) Assert.That(Zone.GetEntityCell(order.Box), Is.Not.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ARefilledOriginalStackIsNotTheUnopenedShipment(bool reload)
        {
            Zone.ZoneID = "Overworld.12.11.0";
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.SetActiveZone(Zone);
            var (receipt, order, _) = PrepareSearch();
            KillAndFindBody(order.Worker);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            var crate = order.Box;
            var original = receipt.FirstMeasure;
            string originalID = original.ID;
            var cargo = crate.GetPart<ContainerPart>();
            Assert.That(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(crate, original), Actor, Zone).Success, Is.True);
            Assert.That(InventorySystem.ExecuteCommand(new DropPartialCommand(original, 1), Actor, Zone).Success, Is.True);
            Assert.That(original.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(InventorySystem.ExecuteCommand(new PutInContainerCommand(crate, original), Actor, Zone).Success, Is.True);
            var replacement = Carry("PreparedLiningClay");
            Assert.That(InventorySystem.ExecuteCommand(new PutInContainerCommand(crate, replacement), Actor, Zone).Success, Is.True);
            Assert.That(cargo.Contents.Single(), Is.SameAs(original));
            Assert.That(original.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(InventorySystem.ExecuteCommand(new PickupCommand(crate), Actor, Zone).Success, Is.True);
            if (reload)
            {
                receipt = ReloadReceipt(manager, order.Shelf.ID, out _);
                order = receipt.Ledger.GetPart<LocalPorterLedgerPart>();
                crate = order.Box;
                cargo = crate.GetPart<ContainerPart>();
            }
            Assert.That(Zone.MoveEntity(Actor, 16, 11), Is.True);
            Assert.That(InventorySystem.ExecuteCommand(new PutInContainerCommand(order.Shelf, crate), Actor, Zone).Success, Is.True);

            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernClosedAtTick, Is.EqualTo(-1));
            Assert.That(receipt.ArrivedAtTick, Is.EqualTo(-1));
            Assert.That(receipt.StockedOrderID, Is.Null);
            Assert.That(cargo.Contents.Single().ID, Is.EqualTo(originalID));
            Assert.That(cargo.Contents.Single().GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant), Has.Count.EqualTo(1));
        }

        [Test]
        public void ReadingOrFailingToTakeFromTheBoxAllowsExactRecovery()
        {
            var (receipt, order, _) = PrepareSearch();
            KillAndFindBody(order.Worker);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            var crate = order.Box;
            var cargo = crate.GetPart<ContainerPart>();
            var original = cargo.Contents.Single();
            Assert.True(Act(crate, "OpenContainer"));
            int capacity = Pack.MaxWeight;
            Pack.MaxWeight = Pack.GetCarriedWeight();
            Assert.False(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(crate, original), Actor, Zone).Success);
            Pack.MaxWeight = capacity;
            Assert.That(cargo.Contents.Single(), Is.SameAs(original));

            Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(crate), Actor, Zone).Success);
            Assert.True(Zone.MoveEntity(Actor, 16, 11));
            Assert.True(InventorySystem.ExecuteCommand(new PutInContainerCommand(order.Shelf, crate), Actor, Zone).Success);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.StockedOrderID, Is.EqualTo(order.OrderID));
            Assert.That(cargo.Contents, Is.Empty);
        }

        private Entity KillAndFindBody(Entity worker)
        {
            var originalFactory = CorpsePart.Factory;
            var originalReputation = PlayerReputation.GetAll();
            CorpsePart.Factory = Factory;
            try
            {
                worker.GetStat("Hitpoints").BaseValue = 0;
                CombatSystem.HandleDeath(worker, Actor, Zone);
                return Zone.GetEntitiesWithTag("Corpse").Single(entity => entity.GetProperty("SourceID") == worker.ID);
            }
            finally
            {
                CorpsePart.Factory = originalFactory;
                PlayerReputation.Restore(originalReputation);
            }
        }

        private string ReadCurrentState(Entity entity)
        {
            var lines = new System.Collections.Generic.List<string>();
            StateReader.Collect(entity, true, Zone, Zone.GetEntityCell(entity), Actor, lines);
            return string.Join(" ", lines);
        }

        private (LocalShipmentReceiverPart receipt, LocalPorterLedgerPart order, LocalShipmentSearchPart search) PrepareSearch()
        {
            var (receipt, order) = BookForDeadline();
            Assert.That(order.TryAct(Zone), Is.True);
            Clock.AdvanceClock(receipt.ExpectedByTick + 41);
            LocalPorterLedgerPart.Reconcile(Actor, Zone);
            Assert.That(receipt.ConcernOpenedAtTick, Is.EqualTo(Clock.TickCount));
            var search = receipt.Merchant.GetPart<LocalShipmentSearchPart>();
            Assert.That(search, Is.Not.Null);
            return (receipt, order, search);
        }

        private void FinishSearch(LocalShipmentSearchPart search, Entity merchant)
        {
            for (int i = 0; i < 54 && search.Phase != LocalShipmentSearchPhase.Finished; i++) AIBoredEvent.Check(merchant);
            Assert.That(search.Phase, Is.EqualTo(LocalShipmentSearchPhase.Finished));
        }

        [Test]
        public void TheFreshWaysideKitchenAddsOnePhysicalShipmentAndKeepsItsOpeningStock()
        {
            using var scope = new HaulingContentScope();
            const string id = "Overworld.12.11.0";
            scope.Seed(unchecked(64 ^ FormationSelector.StableIndex(id, int.MaxValue)));
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            var zone = manager.GetZone(id);
            var shelf = zone.GetReadOnlyEntities().Single(e => e.HasPart<LocalShipmentReceiverPart>());
            var receipt = shelf.GetPart<LocalShipmentReceiverPart>();
            var order = receipt.Ledger.GetPart<LocalPorterLedgerPart>();
            var merchant = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SpreadWaysideCook");
            var chair = zone.GetReadOnlyEntities().Single(e => e.GetProperty("SpreadResident.Role") == "chair");
            var opening = TradeSystem.GetTraderStock(merchant).Single(e => e.BlueprintName == "PreparedLiningClay");
            Assert.That(receipt.Merchant, Is.SameAs(merchant));
            Assert.That(receipt.SearchEmployee, Is.Not.Null);
            Assert.That(receipt.SearchEmployee.GetPart<LocalShipmentEmploymentPart>().Employer, Is.SameAs(merchant));
            Assert.That(receipt.SearchEmployee.GetPart<TraderPart>(), Is.Null);
            Assert.That(TradeSystem.GetDrams(receipt.SearchEmployee), Is.Zero);
            var houseBrain = receipt.SearchEmployee.GetPart<BrainPart>();
            Assert.That(houseBrain.Staying, Is.True);
            Assert.That((houseBrain.StartingCellX, houseBrain.StartingCellY), Is.EqualTo(zone.GetEntityPosition(receipt.SearchEmployee)));
            Assert.That(SpatialQuery.Distance(zone, shelf, merchant), Is.LessThanOrEqualTo(1));
            Assert.That(SpatialQuery.Distance(zone, shelf, chair), Is.LessThanOrEqualTo(1));
            Assert.That(opening.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(TradeSystem.GetDrams(merchant), Is.EqualTo(35));
            Assert.That(receipt.FirstMeasure, Is.Not.SameAs(opening));
            Assert.That(receipt.FirstMeasure.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(receipt.FirstMeasure.GetPart<PhysicsPart>().InInventory, Is.SameAs(order.Box));
            Assert.That(LocalPorterReservationPart.Blocks(order.Box), Is.True);
            Assert.That(TradeSystem.GetDrams(receipt.Sponsor), Is.EqualTo(8));
            Assert.That(zone.GetEntityPosition(receipt.Ledger), Is.EqualTo(zone.GetEntityPosition(order.Box)));
            Assert.That(manager.GetZone(id), Is.SameAs(zone));
            Assert.That(zone.GetReadOnlyEntities().Count(e => e.HasPart<LocalShipmentReceiverPart>()), Is.EqualTo(1));
            Assert.That(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "DryClaySpoil"), Is.False);
        }

        [TestCase(23, 64, 9, 16, 6)]
        [TestCase(23, 1, 1, 8, 5)]
        [TestCase(22, 64, 8, 8, 5)]
        public void SeededLoadingApproachesChangeRealQuotesWithoutChangingTheClay(int version, int seed,
            int minSteps, int maxSteps, int fare)
        {
            using var scope = new HaulingContentScope();
            string id = KitchenBatchPart.KitchenZoneID;
            scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(id, int.MaxValue)));
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            if (version < SpreadExplorationPlan.CurrentVersion)
            {
                var world = SpreadExplorationPlan.BindForSave(manager, null);
                var wire = world.GetProperty(SpreadExplorationPlan.PropertyKey);
                world.Properties[SpreadExplorationPlan.PropertyKey] =
                    version.ToString(System.Globalization.CultureInfo.InvariantCulture) + wire.Substring(wire.IndexOf('|'));
                var restore = typeof(SpreadExplorationPlan).GetMethod("Restore",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                var plan = (SpreadExplorationPlan)restore.Invoke(null, new object[] { manager, world });
                typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(manager, plan);
            }
            Assert.That(manager.Exploration.Version, Is.EqualTo(version));
            var zone = manager.GetZone(id);
            manager.SetActiveZone(zone);
            var shelf = zone.GetReadOnlyEntitiesWithPart<LocalShipmentReceiverPart>().Single();
            var receipt = shelf.GetPart<LocalShipmentReceiverPart>();
            var order = receipt.Ledger.GetPart<LocalPorterLedgerPart>();
            var measure = receipt.FirstMeasure;
            var opening = TradeSystem.GetTraderStock(receipt.Merchant).Single(i => i.BlueprintName == "PreparedLiningClay");
            var role = order.Worker.GetPart<LocalPorterPart>();
            Assert.That(role.TryQuote(receipt.Sponsor, order.Box, shelf, zone, out var quote, out string refusal), Is.True, refusal);
            Assert.That(quote.Steps.Count, Is.InRange(minSteps, maxSteps));
            Assert.That(quote.LoadedWeight, Is.EqualTo(5));
            Assert.That(quote.Fare, Is.EqualTo(fare));

            int now = WorldClock.CurrentTick;
            Assert.That(AIBoredEvent.Check(receipt.Sponsor), Is.False);
            Assert.That(receipt.ExpectedByTick, Is.EqualTo(now + (quote.Steps.Count + 2) * 10));
            for (int i = 0; i < 34 && order.Phase != LocalPorterPhase.Delivered; i++) order.TryAct(zone);
            Assert.That(order.Phase, Is.EqualTo(LocalPorterPhase.Delivered));
            Assert.That(receipt.TryStock(zone), Is.True);
            Assert.That(TradeSystem.GetDrams(receipt.Sponsor), Is.EqualTo(8 - fare));
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant), Does.Contain(opening));
            Assert.That(TradeSystem.GetTraderStock(receipt.Merchant), Does.Contain(measure));
            Assert.That(measure.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
        }

        [TestCase(2)]
        [TestCase(1729)]
        public void ASeededWaysideSortingLotKeepsTheSameKitchenAndDoesNotRefill(int seed)
        {
            using var scope = new HaulingContentScope();
            string id = KitchenBatchPart.KitchenZoneID;
            scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(id, int.MaxValue)));
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            var zone = manager.GetZone(id);
            Assert.That(manager.Exploration.DispositionFor(id), Is.EqualTo(2));
            var cook = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SpreadWaysideCook");
            var pan = zone.GetReadOnlyEntities().Single(e => e.GetPart<KitchenBatchPart>() is { Configured: true });
            var lots = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "DryClaySpoil").ToArray();
            Assert.That(lots, Has.Length.EqualTo(1));
            var lot = lots[0];
            Assert.That(zone.GetReadOnlyEntities().Any(e => e.HasPart<LocalShipmentReceiverPart>()), Is.False);
            Assert.That(TradeSystem.GetTraderStock(cook).Single(e => e.BlueprintName == "PreparedLiningClay")
                .GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(pan.GetPart<RepairablePart>().Repaired, Is.False);
            Assert.That(lot.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(lot.GetPart<SiftableStockPart>().Contents, Is.EqualTo("FireClay:2:0;CoarseGrit:1:2"));
            Assert.That(SpatialQuery.Distance(zone, cook, lot), Is.InRange(2, 4));
            Assert.That(zone.GetEntityCell(lot).IsInterior, Is.False);

            var player = scope.Factory.CreateEntity("Player");
            var ground = zone.GetEntityPosition(lot);
            Assert.That(zone.AddEntity(player, ground.x, ground.y), Is.True);
            manager.SetActiveZone(zone);
            Assert.That(InventorySystem.ExecuteCommand(new PickupCommand(lot), player, zone).Success, Is.True);
            using var stream = new MemoryStream();
            GameSessionState.Capture("wayside-local-lot", "receiving", manager, null, player).Save(new SaveWriter(stream));
            stream.Position = 0;
            var restored = GameSessionState.Load(new SaveReader(stream, scope.Factory));
            var returned = restored.ZoneManager.GetZone(id);
            Assert.That(returned.GetReadOnlyEntities().Any(e => e.BlueprintName == "DryClaySpoil"), Is.False);
            Assert.That(restored.Player.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "DryClaySpoil").ID,
                Is.EqualTo(lot.ID));
            restored.ZoneManager.UnloadZone(id);
            Assert.That(restored.ZoneManager.GetZone(id), Is.SameAs(returned));
        }

        [Test]
        public void AVersionEighteenKitchenStillOffersTheShipmentForASortingSeed()
        {
            using var scope = new HaulingContentScope();
            const int seed = 2;
            string id = KitchenBatchPart.KitchenZoneID;
            scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(id, int.MaxValue)));
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            var world = SpreadExplorationPlan.BindForSave(manager, null);
            string manifest = world.GetProperty(SpreadExplorationPlan.PropertyKey);
            world.Properties[SpreadExplorationPlan.PropertyKey] = "18" + manifest.Substring(manifest.IndexOf('|'));
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var legacy = (SpreadExplorationPlan)typeof(SpreadExplorationPlan).GetMethod("Restore", flags)
                .Invoke(null, new object[] { manager, world });
            typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(manager, legacy);
            var zone = manager.GetZone(id);
            Assert.That(legacy.Version, Is.EqualTo(18));
            Assert.That(legacy.DispositionFor(id), Is.EqualTo(2));
            Assert.That(zone.GetReadOnlyEntities().Count(e => e.HasPart<LocalShipmentReceiverPart>()), Is.EqualTo(1));
            Assert.That(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "DryClaySpoil"), Is.False);
        }

        [Test]
        public void MissingOptionalSortingContentKeepsThePanAndOpeningTrade()
        {
            using var scope = new HaulingContentScope();
            const int seed = 2;
            string id = KitchenBatchPart.KitchenZoneID;
            Assert.That(scope.Factory.Blueprints.Remove("DryClaySpoil"), Is.True);
            scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(id, int.MaxValue)));
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
            var zone = manager.GetZone(id);
            Assert.That(manager.Exploration.DispositionFor(id), Is.EqualTo(2));
            var cook = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SpreadWaysideCook");
            Assert.That(zone.GetReadOnlyEntities().Count(e => e.GetPart<KitchenBatchPart>() is { Configured: true }), Is.EqualTo(1));
            Assert.That(TradeSystem.GetTraderStock(cook).Count(e => e.BlueprintName == "PreparedLiningClay"), Is.EqualTo(1));
            Assert.That(zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "DryClaySpoil" || e.HasPart<LocalShipmentReceiverPart>()), Is.False);
        }

        private Entity Sponsor(Entity board, int funds)
        {
            var sender = Place("Creature", 10, 12);
            sender.SetTag("Faction", "Villagers");
            sender.AddPart(new LocalShipmentSponsorPart { Ledger = board });
            board.GetPart<LocalPorterLedgerPart>().Shelf.GetPart<LocalShipmentReceiverPart>().Sponsor = sender;
            TradeSystem.SetDrams(sender, funds);
            TradeSystem.SetDrams(Actor, 0);
            return sender;
        }

        private LocalShipmentReceiverPart ReloadReceipt(OverworldZoneManager manager, string shelfID,
            out OverworldZoneManager loadedManager, bool withClock = false)
        {
            using var stream = new MemoryStream();
            GameSessionState.Capture("local-shipment", "receiving", manager, withClock ? TurnManager.Active : null, Actor).Save(new SaveWriter(stream));
            stream.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(stream, Factory));
            loadedManager = loaded.ZoneManager;
            Zone = loadedManager.ActiveZone;
            Actor = loaded.Player;
            return Zone.GetReadOnlyEntities().Single(e => e.ID == shelfID).GetPart<LocalShipmentReceiverPart>();
        }

        private LocalShipmentReceiverPart Arrange(bool delivered, bool split = false, bool atSource = false)
        {
            var board = atSource ? Place("Signpost", 10, 11) : Place("Signpost", 6, 12);
            var worker = atSource ? Place("GleanersPorter", 11, 11) : Place("GleanersPorter", 12, 11);
            worker.GetPart<LocalPorterPart>().Ledger = board;
            var merchant = atSource ? Place("SpreadWaysideCook", 15, 10) : Place("SpreadWaysideCook", 11, 10);
            var inventory = merchant.GetPart<InventoryPart>();
            TradeSystem.SetDrams(merchant, 35);
            foreach (var stocked in inventory.Objects.ToArray()) inventory.RemoveObject(stocked);
            var opening = Clay(2);
            Assert.That(inventory.AddObject(opening), Is.True);
            var shelf = Place("LiningClayReceivingShelf", atSource ? 16 : 12, 10);
            var box = Factory.CreateEntity("LiningClayParcel");
            var first = Clay(split ? 1 : 2);
            var second = split ? Factory.CreateEntity("FireClay") : null;
            var cargo = box.GetPart<ContainerPart>();
            Assert.That(cargo.AddItem(first), Is.True);
            if (second != null) Assert.That(cargo.AddItem(second), Is.True);
            if (atSource) Assert.That(Zone.AddEntity(box, 10, 11), Is.True);
            else Assert.That(shelf.GetPart<ContainerPart>().AddItem(box), Is.True);
            var order = new LocalPorterLedgerPart
            {
                Box = box, Shelf = shelf, Worker = worker, OrderID = atSource ? null : "delivery-one",
                ShelfX = atSource ? 16 : 12, ShelfY = 10,
                Phase = atSource ? LocalPorterPhase.Open : delivered ? LocalPorterPhase.Delivered : LocalPorterPhase.Carrying
            };
            board.AddPart(order);
            box.AddPart(new LocalPorterReservationPart { Ledger = board, OrderID = order.OrderID });
            var receipt = new LocalShipmentReceiverPart
            {
                Ledger = board, Merchant = merchant, FirstMeasure = first, SecondMeasure = second
            };
            shelf.AddPart(receipt);
            return receipt;
        }

        private Entity Clay(int measures)
        {
            var clay = Factory.CreateEntity("PreparedLiningClay");
            clay.GetPart<StackerPart>().StackCount = measures;
            return clay;
        }
    }
}
