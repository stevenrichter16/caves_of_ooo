using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A receiving shelf that can stock one paid parcel for its assigned trader.</summary>
    public sealed class LocalShipmentReceiverPart : Part, IDescribeState
    {
        public override string Name => "LocalShipmentReceiver";

        public Entity Ledger;
        public Entity Merchant;
        public Entity Sponsor;
        public Entity SearchEmployee;
        public string DelegatedSearcherID;
        public int DelegateDueAtTick = -1;
        public int SearchReportAtTick = -1;
        public bool SearchReportSawBody;
        public Entity FirstMeasure;
        public Entity SecondMeasure;
        public string StockedOrderID;
        public string ExpectedOrderID;
        public int ExpectedByTick = -1;
        public int ArrivedAtTick = -1;
        public int ConcernOpenedAtTick = -1;
        public int ConcernClosedAtTick = -1;
        private const int TicksPerRouteAction = 10;
        private const int GraceTicks = 40;
        private const int SearchWage = 2;

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 25;

        public string DescribeState(DescribeContext context)
        {
            if (Ledger?.GetPart<LocalPorterLedgerPart>() is not { } order || !HasAgreement(order)) return null;
            if (ArrivedAtTick >= 0)
                return order.Phase == LocalPorterPhase.Failed
                    ? $"The original clay shipment was recovered on {Moment(ArrivedAtTick)}."
                    : $"Clay shipment received on {Moment(ArrivedAtTick)}.";
            if (order.Phase == LocalPorterPhase.Cancelled) return "The clay shipment was cancelled.";
            if (ConcernOpenedAtTick >= 0)
            {
                if (ConcernClosedAtTick >= 0) return "The kitchen has closed its overdue shipment notice.";
                if (SearchReportAtTick >= 0)
                    return SearchReportSawBody
                        ? "The kitchen hand reported finding the missing porter dead. Keep the original box packed if you return it."
                        : "The kitchen hand returned from checking the shipment route. Keep the original box packed if you return it.";
                if (Merchant?.GetPart<LocalShipmentSearchPart>() is { Phase: LocalShipmentSearchPhase.Finished, SeenPorterBodyID: not null } search
                    && search.Shelf == ParentEntity && search.OrderID == ExpectedOrderID)
                    return "The cook found the missing porter dead. Keep the original box packed if you return it.";
                if (DelegatedSearcherID != null && !DelegateOverdue)
                    return $"The cook sent the kitchen hand to check the route. The hand is due back on {Moment(DelegateDueAtTick)}.";
                return "The kitchen has posted an overdue clay shipment notice. Keep the original box packed if you return it.";
            }
            return $"Clay shipment due on {Moment(ExpectedByTick)}. Allow {GraceTicks} ticks for delays. The porter works while this area is active.";
        }

        internal bool TryStageAgreement(string orderID, int steps, InventoryTransaction tx)
        {
            int now = WorldClock.CurrentTick;
            long due = now + ((long)steps + 2) * TicksPerRouteAction;
            if (ExpectedOrderID != null || string.IsNullOrEmpty(orderID) || steps < 1
                || now < 0 || due > int.MaxValue - GraceTicks) return false;
            int priorDue = ExpectedByTick;
            tx.Do(() => { ExpectedOrderID = orderID; ExpectedByTick = (int)due; },
                () => { ExpectedOrderID = null; ExpectedByTick = priorDue; });
            return true;
        }

        internal void StageCaseClosure(LocalPorterLedgerPart order, bool received, InventoryTransaction tx)
        {
            if (!HasAgreement(order)) return;
            int now = WorldClock.CurrentTick;
            int priorArrival = ArrivedAtTick, priorClose = ConcernClosedAtTick;
            tx.Do(() =>
            {
                if (received && ArrivedAtTick < 0) ArrivedAtTick = now;
                if (ConcernOpenedAtTick >= 0 && ConcernClosedAtTick < 0) ConcernClosedAtTick = now;
            }, () => { ArrivedAtTick = priorArrival; ConcernClosedAtTick = priorClose; });
        }

        internal void ObserveOverdue(LocalPorterLedgerPart order, Zone zone)
        {
            if (!HasAgreement(order) || order.Phase is LocalPorterPhase.Open or LocalPorterPhase.Delivered or LocalPorterPhase.Cancelled
                || !order.SiteCurrent(zone) || ConcernClosedAtTick >= 0) return;
            int now = WorldClock.CurrentTick;
            if (!LocalGatheringClaims.Alive(Merchant))
            {
                if (ConcernOpenedAtTick >= 0) ConcernClosedAtTick = now;
                return;
            }
            if (ConcernOpenedAtTick >= 0 && Merchant.HasPart<LocalShipmentSearchPart>()) return;
            var shelf = ParentEntity;
            if (ArrivedAtTick >= 0 || ConcernOpenedAtTick < 0 && now <= (long)ExpectedByTick + GraceTicks
                || order.Shelf != shelf || !WorldResourceActions.Nearby(Merchant, shelf, zone)
                || zone.GetEntityPosition(shelf) != (order.ShelfX, order.ShelfY)
                || Merchant.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || TradeSystem.TraderUnableToTrade(Merchant, out _)
                || zone.GetReadOnlyEntities().Count(e => e.ID == Merchant.ID) != 1
                || zone.GetReadOnlyEntities().Count(e => e.ID == shelf.ID) != 1) return;

            if (ConcernOpenedAtTick < 0)
            {
                ConcernOpenedAtTick = now;
                if (zone.GetEntityCell(Merchant)?.IsVisible == true)
                    MessageLog.Add($"{Merchant.GetDisplayName()} marks the clay shipment overdue.");
            }
            if (Merchant.GetPart<LocalShipmentSearchPart>() == null)
                Merchant.AddPart(new LocalShipmentSearchPart { Shelf = shelf, OrderID = ExpectedOrderID });
        }

        private bool HasAgreement(LocalPorterLedgerPart order) => order.ParentEntity == Ledger
            && order.Shelf == ParentEntity && !string.IsNullOrEmpty(ExpectedOrderID)
            && ExpectedOrderID == order.OrderID && ExpectedByTick >= 0;

        internal bool IsDelegatedSearcher(Entity actor) => DelegatedSearcherID != null
            && actor?.ID == DelegatedSearcherID && IsSearchEmployee(actor);

        internal bool DelegateOverdue => DelegatedSearcherID != null && SearchReportAtTick < 0
            && WorldClock.CurrentTick > DelegateDueAtTick;

        private bool IsSearchEmployee(Entity actor) => actor != null && actor == SearchEmployee && actor != Merchant && actor != Sponsor
            && actor.GetPart<LocalShipmentEmploymentPart>() is { } employment
            && employment.ParentEntity == actor && employment.Employer == Merchant
            && actor.Parts.Count(part => part is LocalShipmentEmploymentPart) == 1;

        internal bool CanReceiveSearchReport(Zone zone) => WorldResourceActions.Nearby(Merchant, ParentEntity, zone)
            && LocalPorterPart.CanDoLocalWork(Merchant, zone) && !LocalShipmentSearchPart.PreparingMeal(Merchant, zone)
            && zone.GetReadOnlyEntities().Count(e => e.ID == Merchant.ID) == 1;

        internal bool CanSpeakToCookFrom(Cell cell, Zone zone)
        {
            var cookAt = zone.GetEntityCell(Merchant);
            var shelfAt = zone.GetEntityCell(ParentEntity);
            bool Obscured(Cell at) => at == null || at.IsSolid() || zone.TileState.ObscuresSight(at.X, at.Y);
            return cell != null && cell.ParentZone == zone && cookAt != null && shelfAt != null
                && Math.Max(Math.Abs(cell.X - cookAt.X), Math.Abs(cell.Y - cookAt.Y)) <= 1
                && Math.Max(Math.Abs(cell.X - shelfAt.X), Math.Abs(cell.Y - shelfAt.Y)) <= 1
                && (cell.X == cookAt.X || cell.Y == cookAt.Y
                    || !Obscured(zone.GetCell(cell.X, cookAt.Y)) || !Obscured(zone.GetCell(cookAt.X, cell.Y)))
                && AIHelpers.HasLineOfSight(zone, cell.X, cell.Y, cookAt.X, cookAt.Y);
        }

        internal bool TryDelegateSearch(LocalPorterLedgerPart order, Zone zone)
        {
            var hand = SearchEmployee;
            long due = (long)WorldClock.CurrentTick + (2 * LocalShipmentSearchPart.LegAttemptLimit + 6) * TicksPerRouteAction;
            if (DelegatedSearcherID != null || !IsSearchEmployee(hand) || hand == order.Worker
                || hand.HasPart<LocalShipmentSearchPart>() || !order.StaticShelf(zone)
                || !CanReceiveSearchReport(zone) || !LocalPorterPart.CanDoLocalWork(hand, zone)
                || string.IsNullOrEmpty(hand.ID) || zone.GetReadOnlyEntities().Count(e => e.ID == hand.ID) != 1
                || !CanSpeakToCookFrom(zone.GetEntityCell(hand), zone)
                || LocalPorterPart.Route(hand, order.ParentEntity, zone) is not { Count: <= LocalShipmentSearchPart.LegAttemptLimit }
                || TradeSystem.GetDrams(Merchant) < SearchWage
                || TradeSystem.GetDrams(hand) > int.MaxValue - SearchWage || due > int.MaxValue) return false;

            var search = new LocalShipmentSearchPart
            {
                Shelf = ParentEntity, OrderID = ExpectedOrderID, Phase = LocalShipmentSearchPhase.CheckingLoadingPoint
            };
            var tx = new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, Merchant, Name) || !tx.TryClaim(Merchant, Merchant, Name)
                    || !tx.TryClaim(hand, Merchant, Name)) return false;
                tx.Do(() =>
                {
                    DelegatedSearcherID = hand.ID;
                    DelegateDueAtTick = (int)due;
                    hand.AddPart(search);
                }, () =>
                {
                    hand.RemovePart(search);
                    DelegatedSearcherID = null;
                    DelegateDueAtTick = -1;
                });
                tx.DeferCurrencyTransfer(Merchant, hand, SearchWage);
                tx.AfterCommit(() =>
                {
                    if (zone.GetEntityCell(Merchant)?.IsVisible == true)
                        MessageLog.Add($"{Merchant.GetDisplayName()} pays the kitchen hand to check the overdue shipment. The hand is due back on {Moment((int)due)}.");
                });
                tx.Commit();
                return true;
            }
            finally
            {
                if (!tx.IsCommitted) tx.Rollback();
            }
        }

        internal bool TryReceiveSearchReport(LocalShipmentSearchPart search, Zone zone)
        {
            var actor = search.ParentEntity;
            if (SearchReportAtTick >= 0 || ConcernClosedAtTick >= 0 || DelegateOverdue || !IsDelegatedSearcher(actor)
                || search.OrderID != ExpectedOrderID || !CanReceiveSearchReport(zone)
                || !CanSpeakToCookFrom(zone.GetEntityCell(actor), zone)) return false;
            SearchReportSawBody = search.SeenPorterBodyID != null;
            SearchReportAtTick = WorldClock.CurrentTick;
            return true;
        }

        private static string Moment(int tick) =>
            $"day {tick / WorldClock.DayLengthTicks + 1}, {WorldClock.BandName(WorldClock.GetBand(tick), 0)} + {tick % WorldClock.BandLengthTicks} ticks";

        internal bool MatchesCargo(Entity parcel)
        {
            var contents = parcel?.GetPart<ContainerPart>()?.Contents;
            return StockedOrderID == null && FirstMeasure != null && contents != null
                && contents.Count == (SecondMeasure == null ? 1 : 2) && contents[0] == FirstMeasure
                && (SecondMeasure == null || contents[1] == SecondMeasure);
        }

        public bool TryStock(Zone zone)
        {
            if (StockedOrderID != null || Ledger?.GetPart<LocalPorterLedgerPart>() is not { } order) return false;
            var parcel = order.Box;
            var orderID = order.OrderID;
            var phase = order.Phase;
            bool recovered = phase == LocalPorterPhase.Failed;
            var merchant = Merchant;
            if (recovered && ArrivedAtTick >= 0 || !ReceiptCurrent(zone, order, parcel, orderID, merchant, phase)) return false;
            var reservation = parcel.GetPart<LocalPorterReservationPart>();
            if (recovered ? reservation != null : reservation == null || reservation.Ledger != Ledger
                || reservation.OrderID != orderID || parcel.Parts.Count(p => p is LocalPorterReservationPart) != 1) return false;
            if (DeliveryStoragePart.ParcelWeightOrZero(parcel, zone) == 0) return false;

            var cargo = parcel.GetPart<ContainerPart>();
            var inventory = merchant.GetPart<InventoryPart>();
            var first = FirstMeasure;
            var second = SecondMeasure;
            var items = second == null ? new[] { first } : new[] { first, second };
            if (!MatchesCargo(parcel) || items.Any(item => item?.GetPart<CommercePart>() is not { Value: > 0 }
                || inventory.Objects.Concat(inventory.EquippedItems.Values).Any(held => held?.ID == item.ID))) return false;

            var tx = new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(Ledger, merchant, Name) || !tx.TryClaim(ParentEntity, merchant, Name)
                    || !tx.TryClaim(merchant, merchant, Name) || !tx.TryClaim(parcel, merchant, Name)
                    || items.Any(item => !tx.TryClaim(item, merchant, Name))) return false;

                var source = InventoryTransferSnapshot.Capture(cargo);
                var destination = InventoryTransferSnapshot.Capture(inventory, items);
                tx.Do(null, source.Restore);
                if (!source.Apply(() => items.All(cargo.RemoveItem))) return false;
                tx.Do(null, destination.Restore);
                if (!destination.Apply(() => items.All(inventory.AddRetrievedObject))) return false;
                if (recovered) StageCaseClosure(order, received: true, tx);
                tx.Do(() =>
                {
                    if (reservation != null) parcel.RemovePart(reservation);
                    StockedOrderID = orderID;
                    FirstMeasure = SecondMeasure = null;
                }, () =>
                {
                    if (reservation != null) parcel.AddPart(reservation);
                    StockedOrderID = null;
                    FirstMeasure = first;
                    SecondMeasure = second;
                });
                tx.BeforeCommit(() => ReceiptCurrent(zone, order, parcel, orderID, merchant, phase)
                    && StockedOrderID == orderID && parcel.GetPart<ContainerPart>() == cargo && cargo.Contents.Count == 0
                    && merchant.GetPart<InventoryPart>() == inventory && !LocalPorterReservationPart.Blocks(parcel)
                    && items.All(item => PreparationActions.Carried(merchant, item)));
                if (Diag.IsChannelEnabled("trade"))
                {
                    var evidence = new { orderId = orderID, zoneId = zone.ZoneID, boxId = parcel.ID,
                        shelfId = ParentEntity.ID, receiverId = merchant.ID, recovered,
                        cargo = items.Select(item => new { item.ID, item.BlueprintName,
                            units = item.GetPart<StackerPart>()?.StackCount ?? 1 }).ToArray() };
                    tx.AfterCommit(() => Diag.Record("trade", "ShipmentStocked", merchant, parcel, evidence));
                }
                tx.Commit();
                if (zone.GetEntityCell(ParentEntity)?.IsVisible == true)
                    MessageLog.Add($"{merchant.GetDisplayName()} unpacks the {(recovered ? "returned" : "delivered")} lining clay for sale.");
                return true;
            }
            catch (Exception error)
            {
                Diag.Record("trade", "ShipmentStockInterrupted", merchant, parcel,
                    payload: new { error = error.GetType().Name });
                return tx.IsCommitted;
            }
            finally
            {
                if (!tx.IsCommitted) tx.Rollback();
            }
        }

        private bool ReceiptCurrent(Zone zone, LocalPorterLedgerPart order, Entity parcel, string orderID, Entity merchant, LocalPorterPhase phase)
        {
            var shelf = ParentEntity;
            return zone != null && shelf?.GetPart<LocalShipmentReceiverPart>() == this
                && shelf.Parts.Count(p => p is LocalShipmentReceiverPart) == 1
                && Ledger?.GetPart<LocalPorterLedgerPart>() == order && order.SiteCurrent(zone)
                && order.Shelf == shelf && order.Box == parcel && order.Phase == phase
                && (phase == LocalPorterPhase.Delivered || phase == LocalPorterPhase.Failed && HasAgreement(order))
                && !string.IsNullOrEmpty(orderID) && order.OrderID == orderID && TradeSystem.GetDrams(Ledger) == 0
                && zone.GetEntityPosition(shelf) == (order.ShelfX, order.ShelfY)
                && shelf.GetPart<DeliveryStoragePart>() is { ReceivesParcels: true } storage && storage.CanReach(merchant, zone)
                && merchant == Merchant && merchant.GetPart<TraderPart>()?.ParentEntity == merchant
                && !TradeSystem.TraderUnableToTrade(merchant, out _)
                && zone.GetReadOnlyEntities().Count(e => e.ID == merchant.ID) == 1
                && parcel != null && !string.IsNullOrEmpty(parcel.ID) && parcel.ID != shelf.ID && parcel.ID != merchant.ID && parcel.ID != Ledger.ID
                && parcel.GetPart<DeliveryStoragePart>() is { ReceivesParcels: false, ContentsWithdrawn: false }
                && parcel.SpatialZone == null && parcel.GetPart<PhysicsPart>() is { InInventory: var holder, Equipped: null } physics
                && physics.ParentEntity == parcel && holder == shelf && !zone.GetReadOnlyEntities().Any(e => e.ID == parcel.ID)
                && shelf.GetPart<ContainerPart>().Contents.Count == 1 && shelf.GetPart<ContainerPart>().Contents[0] == parcel;
        }
    }
}
