using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    public enum LocalPorterPhase { Open, Booked, Carrying, Returning, Delivered, Cancelled, Failed }

    /// <summary>The fixed loading marker owns the work record and its actual escrow.</summary>
    public sealed class LocalPorterLedgerPart : Part, IInventoryActionReceiptOwner, IDescribeState, IDescribeUse, IMenuStatus
    {
        public override string Name => "LocalPorterLedger";
        public Entity Worker, Box, Shelf, Payer;
        public Entity FirstMeasure, SecondMeasure;
        public LocalPorterPhase Phase;
        public int Fare, OriginX, OriginY, ShelfX, ShelfY;
        public string OrderID, OrderZone, Manifest, Note;
        public const string HirePrefix = "LocalPorterHire:";
        public const string CancelPrefix = "LocalPorterCancel:";
        private const int DepartureFee = 1;
        [NonSerialized] private bool _acting;

        internal bool Active => Phase is LocalPorterPhase.Booked or LocalPorterPhase.Carrying or LocalPorterPhase.Returning;
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 24;
        public bool OwnsInventoryReceipt(string command) => command?.StartsWith(HirePrefix, StringComparison.Ordinal) == true
            || command?.StartsWith(CancelPrefix, StringComparison.Ordinal) == true;

        public string DescribeState(DescribeContext context)
        {
            var shipment = Shipment;
            string agreement = shipment?.DescribeState(context);
            if (agreement != null) return agreement;
            if (Active && context.Viewer != Payer)
                return shipment == null ? "The porter has a delivery booked." : "The porter is carrying out a delivery to the wayside kitchen.";
            if (!Active) return Note ?? (shipment != null ? "A box of lining clay waits for the wayside kitchen."
                : "Pack two measures of clay and set the box at this loading marker. Manual carrying remains free.");
            int held = Phase == LocalPorterPhase.Booked ? Fare : Fare - DepartureFee;
            string status = Phase switch
            {
                LocalPorterPhase.Booked => "The packed box is waiting for pickup.",
                LocalPorterPhase.Returning => "The porter is returning the box.",
                _ => "The porter has the packed box."
            };
            return status + $" {held} drams held here. " + Note;
        }

        public void AppendMenuStatus(DescribeContext context, List<string> lines) => lines.Add(DescribeState(context));

        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            if (context.Owner != ParentEntity || Shipment != null || Role == null || Box == null || Shelf == null) return;
            guide.How.Add("Carry one measure at a time or both loose without a fee. To hire the porter, pack both measures into the box and bring it here.");
            guide.Limits.Add("A shorter clear walk can lower the next fare. The hire quote includes the current route and the weight of the packed box.");
        }

        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
            if (e.ID == "GetInventoryActions" && Shipment == null && AtPost(actor, zone))
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (Active)
                {
                    if (actor == Payer && Phase != LocalPorterPhase.Returning)
                        actions?.AddActionWithHint("Cancel delivery", "cancel delivery", CancelPrefix + OrderID,
                            "Before pickup the whole fare returns. After pickup the porter returns the box and keeps one dram.", 'x', 50, time: ActionTime.Free);
                }
                else if (Offer(actor, zone, out var quote, out var refusal))
                    actions?.AddActionWithHint("Hire porter", $"hire porter ({quote.Fare} drams)", OfferKey(actor, quote, OrderID),
                        $"{quote.LoadedWeight} lb over {quote.Steps.Count} steps. One dram at pickup. The rest on receipt or back to you if the box returns.", 'h', 50, time: ActionTime.OneAction);
                else
                    actions?.AddDisabled("Hire porter", "hire porter", "porter-unavailable", refusal, 'h', 50);
            }
            if (e.ID != "InventoryAction") return true;
            string command = e.GetStringParameter("Command");
            if (!OwnsInventoryReceipt(command)) return true;
            var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            bool accepted = tx != null && AtPost(actor, zone) && (command.StartsWith(CancelPrefix, StringComparison.Ordinal)
                ? Active && actor == Payer && command == CancelPrefix + OrderID && RequestCancel(zone, tx)
                : Shipment == null && Book(actor, zone, command, tx));
            e.Handled = accepted;
            return !accepted;
        }

        internal bool TryBookShipment(LocalShipmentSponsorPart sender, Zone zone)
        {
            var actor = sender?.ParentEntity;
            var shipment = Shipment;
            if (sender?.Ledger != ParentEntity || actor?.GetPart<LocalShipmentSponsorPart>() != sender
                || actor == null || actor.HasTag("Player") || shipment?.Sponsor != actor
                || !Offer(actor, zone, out var quote, out _, shipment)) return false;
            return Execute(zone, (z, tx) => Book(actor, z, OfferKey(actor, quote, OrderID), tx, shipment));
        }

        private bool Book(Entity actor, Zone zone, string command, InventoryTransaction tx, LocalShipmentReceiverPart shipment = null)
        {
            string priorOrderID = OrderID;
            if (!Offer(actor, zone, out var quote, out _, shipment) || command != OfferKey(actor, quote, priorOrderID) || !Claim(tx, actor)) return false;
            var initialMark = shipment == null ? null : Box.GetPart<LocalPorterReservationPart>();
            var mark = initialMark ?? new LocalPorterReservationPart { Ledger = ParentEntity };
            string newOrderID = Guid.NewGuid().ToString("N");
            if (shipment != null && !shipment.TryStageAgreement(newOrderID, quote.Steps.Count, tx)) return false;
            var cargo = Box.GetPart<ContainerPart>().Contents;
            var priorPhase = Phase;
            string priorNote = Note;
            int priorFare = Fare;
            var origin = zone.GetEntityCell(Box);
            var destination = zone.GetEntityCell(Shelf);
            tx.Do(() =>
            {
                OriginX = origin.X; OriginY = origin.Y; ShelfX = destination.X; ShelfY = destination.Y;
                OrderZone = zone.ZoneID; OrderID = newOrderID; Fare = quote.Fare; Payer = actor;
                FirstMeasure = cargo[0]; SecondMeasure = cargo.Count == 2 ? cargo[1] : null;
                Manifest = CargoText();
                Phase = LocalPorterPhase.Booked; Note = null;
                mark.OrderID = newOrderID;
                if (initialMark == null) Box.AddPart(mark);
            }, () =>
            {
                if (initialMark == null && mark.ParentEntity == Box) Box.RemovePart(mark);
                mark.OrderID = null;
                ClearActiveOrder(priorPhase, priorNote);
                OrderID = priorOrderID;
                Fare = priorFare;
            });
            tx.DeferCurrencyTransfer(actor, ParentEntity, quote.Fare);
            tx.BeforeCommit(() => Shipment == shipment && AtPost(actor, zone) && AtSource(zone) && StaticShelf(zone)
                && Reserved(zone) && EmptyShelf() && Role?.TryQuote(actor, Box, Shelf, zone, out var current, out _) == true
                && OfferKey(actor, current, priorOrderID) == command && TradeSystem.GetDrams(ParentEntity) == 0);
            tx.AfterCommit(() =>
            {
                if (shipment == null) MessageLog.Add($"Delivery booked for {quote.Fare} drams. The box stays here until pickup.");
                else if (zone.GetEntityCell(ParentEntity)?.IsVisible == true)
                    MessageLog.Add($"{actor.GetDisplayName()} hires a porter for the kitchen's clay.");
            });
            return true;
        }

        private bool RequestCancel(Zone zone, InventoryTransaction tx)
        {
            if (Phase == LocalPorterPhase.Booked) return Close(zone, tx, LocalPorterPhase.Cancelled, "The booking was cancelled.");
            return RequestReturn(tx, "The porter will return the box to the loading marker.");
        }

        private bool RequestReturn(InventoryTransaction tx, string message)
        {
            if (Phase != LocalPorterPhase.Carrying || !Claim(tx, Payer) || !EscrowCurrent()) return false;
            string prior = Note;
            tx.Do(() => { Phase = LocalPorterPhase.Returning; Note = message; },
                () => { Phase = LocalPorterPhase.Carrying; Note = prior; });
            return true;
        }

        /// <summary>One normal worker action. A pause yields control to the worker's idle and survival behavior.</summary>
        public bool TryAct(Zone zone)
        {
            if (_acting) return false;
            _acting = true;
            try { return ActOnce(zone); }
            finally { _acting = false; }
        }

        private bool ActOnce(Zone zone)
        {
            if (!Active || !SiteCurrent(zone) || !EscrowCurrent()) return false;
            if (Failure(zone))
                return Execute(zone, (z, tx) => Close(z, tx, LocalPorterPhase.Failed, "Delivery stopped. Any surviving box stays where it is."));
            if (Phase == LocalPorterPhase.Carrying && !StaticShelf(zone))
                return Execute(zone, (_, tx) => RequestReturn(tx, "The shelf is gone. The porter is bringing the box back."));
            if (Role?.Available(Worker, zone) != true) { Note = "The porter has paused for safety."; return false; }
            if (Phase == LocalPorterPhase.Booked)
                return Execute(zone, Load);

            var target = Phase == LocalPorterPhase.Returning ? ParentEntity : Shelf;
            if (!Role.Fits(Worker, Box, alreadyCarried: true, out _)) { Note = "The loaded box is too heavy."; return false; }
            if (Phase == LocalPorterPhase.Carrying && !EmptyShelf()) { Note = "The receiving shelf is full or locked."; return false; }
            if (SpatialQuery.Distance(zone, Worker, target) <= 1)
                return Execute(zone, Phase == LocalPorterPhase.Returning ? ReturnBox : Deliver);

            var route = LocalPorterPart.Route(Worker, target, zone);
            if (route == null || route.Count == 0) { Note = "The walking route is blocked."; return false; }
            var next = route[0];
            var here = zone.GetEntityCell(Worker);
            var phase = Phase;
            bool moved = MovementSystem.TryMoveChecked(Worker, zone, next.X - here.X, next.Y - here.Y,
                () => Phase == phase && zone.GetEntityCell(Worker) == here && SiteCurrent(zone)
                    && (phase == LocalPorterPhase.Returning || StaticShelf(zone)) && Reserved(zone) && Carrying(zone)
                    && Role?.Available(Worker, zone) == true && Role.Fits(Worker, Box, alreadyCarried: true, out _)
                    && LocalPorterPart.ClearStep(Worker, zone, next));
            Note = moved ? null : "The porter could not take the next step.";
            return moved;
        }

        private bool Load(Zone zone, InventoryTransaction tx)
        {
            if (!LoadCurrent(zone) || !Claim(tx, Worker)) return false;
            if (!PickupEvent(Worker, "BeforePickup") || !LoadCurrent(zone)
                || !PickupEvent(Box, "BeforeBeingPickedUp") || !LoadCurrent(zone)) return false;

            var box = Box;
            var inventory = Worker.GetPart<InventoryPart>();
            var snapshot = InventoryTransferSnapshot.Capture(inventory, box);
            var source = zone.GetEntityCell(box);
            if (!zone.RemoveEntity(box)) return false;
            tx.Do(null, () =>
            {
                if (box.SpatialZone == null && box.GetPart<PhysicsPart>()?.InInventory == null)
                    zone.AddEntity(box, source.X, source.Y);
            });
            tx.Do(null, snapshot.Restore);
            if (!snapshot.Apply(() => inventory.AddRetrievedObject(box))) return false;
            tx.Do(() => { Phase = LocalPorterPhase.Carrying; Note = null; }, () => Phase = LocalPorterPhase.Booked);
            tx.DeferCurrencyTransfer(ParentEntity, Worker, DepartureFee);
            tx.BeforeCommit(() => SiteCurrent(zone) && Reserved(zone) && Carrying(zone));
            tx.AfterCommit(() =>
            {
                VisibleNote(zone, $"{Worker.GetDisplayName()} takes the packed clay box.");
                PickupEvent(box, "Taken");
                PickupEvent(Worker, "AfterPickup");
            });
            return true;
        }

        private bool Deliver(Zone zone, InventoryTransaction tx)
        {
            if (Phase != LocalPorterPhase.Carrying || !Reserved(zone) || !Carrying(zone)
                || !StaticShelf(zone) || !EmptyShelf() || !Claim(tx, Worker)) return false;
            var mark = Box.GetPart<LocalPorterReservationPart>();
            var shipment = Shipment;
            if (shipment != null && !shipment.MatchesCargo(Box)) return false;
            tx.Do(() => Box.RemovePart(mark), () => Box.AddPart(mark));
            var context = new InventoryContext(Worker, zone);
            var command = new PutInContainerCommand(Shelf, Box);
            if (!command.Validate(context).IsValid || !command.Execute(context, tx).Success) return false;
            if (shipment != null) tx.Do(() => Box.AddPart(mark), () => Box.RemovePart(mark));
            tx.BeforeCommit(() => Shipment == shipment && StaticShelf(zone) && Shelf.GetPart<ContainerPart>().Contents.Count == 1
                && Shelf.GetPart<ContainerPart>().Contents[0] == Box && Box.GetPart<PhysicsPart>()?.InInventory == Shelf);
            return Close(zone, tx, LocalPorterPhase.Delivered, "The box reached the receiving shelf. The porter has been paid.");
        }

        private bool ReturnBox(Zone zone, InventoryTransaction tx)
        {
            if (Phase != LocalPorterPhase.Returning || !Reserved(zone) || !Carrying(zone) || !Claim(tx, Worker)) return false;
            var snapshot = InventoryTransferSnapshot.Capture(Worker.GetPart<InventoryPart>());
            tx.Do(null, snapshot.Restore);
            if (!snapshot.Apply(() => Worker.GetPart<InventoryPart>().RemoveObject(Box))) return false;
            if (!zone.AddEntity(Box, OriginX, OriginY)) return false;
            tx.Do(null, () => { if (Box.SpatialZone == zone) zone.RemoveEntity(Box); });
            tx.BeforeCommit(() => SiteCurrent(zone) && AtSource(zone) && DeliveryStoragePart.ParcelWeightOrZero(Box, zone) > 0);
            return Close(zone, tx, LocalPorterPhase.Cancelled, "The same box returned to the loading marker. One dram was earned at pickup.");
        }

        private bool Close(Zone zone, InventoryTransaction tx, LocalPorterPhase result, string message)
        {
            if (!Active || !SiteCurrent(zone) || !EscrowCurrent() || !Claim(tx, Payer)) return false;
            int refund = TradeSystem.GetDrams(ParentEntity);
            var mark = Box?.GetPart<LocalPorterReservationPart>();
            var shipment = Shipment;
            if (mark?.Ledger == ParentEntity && mark.OrderID == OrderID && (result != LocalPorterPhase.Delivered || shipment == null))
                tx.Do(() => Box.RemovePart(mark), () => Box.AddPart(mark));
            var oldPhase = Phase;
            var oldNote = Note;
            var closingOrder = OrderID;
            var recipient = result == LocalPorterPhase.Delivered ? Worker : Payer;
            bool publicFailureNotice = Payer.HasTag("Player") && result != LocalPorterPhase.Delivered;
            if (result is LocalPorterPhase.Delivered or LocalPorterPhase.Cancelled)
                shipment?.StageCaseClosure(this, result == LocalPorterPhase.Delivered, tx);
            tx.DeferCurrencyTransfer(ParentEntity, recipient, refund);
            tx.Do(() => { Phase = result; Note = message; }, () => { Phase = oldPhase; Note = oldNote; });
            tx.AfterCommit(() =>
            {
                if (OrderID == closingOrder && Phase == result) ClearActiveOrder(result, message);
                if (publicFailureNotice || Visible(zone)) MessageLog.Add(message);
            });
            return true;
        }

        public static void Reconcile(Entity player, Zone zone)
        {
            if (player?.HasTag("Player") != true || zone == null) return;
            foreach (var owner in zone.GetReadOnlyEntitiesWithPart<LocalPorterLedgerPart>())
            {
                var ledger = owner.GetPart<LocalPorterLedgerPart>();
                if (ledger is { Active: true } && ledger.SiteCurrent(zone) && ledger.Failure(zone))
                    ledger.Execute(zone, (z, tx) => ledger.Close(z, tx, LocalPorterPhase.Failed,
                        "Delivery stopped. Any surviving box stays where it is."));
                else if (ledger?.Phase is LocalPorterPhase.Delivered or LocalPorterPhase.Failed)
                    ledger.Shipment?.TryStock(zone);
                ledger?.Shipment?.ObserveOverdue(ledger, zone);
            }
        }

        private bool Offer(Entity actor, Zone zone, out LocalPorterQuote quote, out string refusal, LocalShipmentReceiverPart shipment = null)
        {
            quote = default;
            refusal = "Set the packed clay box at this loading marker.";
            return !Active && AtPost(actor, zone) && TradeSystem.GetDrams(ParentEntity) == 0 && Shipment == shipment
                && (shipment == null ? !LocalPorterReservationPart.Blocks(Box) : CanOfferShipment(actor, zone, shipment))
                && WorldResourceActions.Ground(Box, zone)
                && zone.GetEntityCell(Box) == zone.GetEntityCell(ParentEntity)
                && Role?.TryQuote(actor, Box, Shelf, zone, out quote, out refusal) == true;
        }

        private bool CanOfferShipment(Entity actor, Zone zone, LocalShipmentReceiverPart shipment) =>
            Phase == LocalPorterPhase.Open && OrderID == null && shipment.Sponsor == actor
            && LocalGatheringClaims.Alive(shipment.Merchant) && Exact(shipment.Merchant, zone)
            && shipment.MatchesCargo(Box) && Box.GetPart<LocalPorterReservationPart>() is { OrderID: null } pending
            && pending.Ledger == ParentEntity && Box.Parts.Count(p => p is LocalPorterReservationPart) == 1;

        private LocalShipmentReceiverPart Shipment => Shelf?.GetPart<LocalShipmentReceiverPart>() is { } shipment
            && shipment.Ledger == ParentEntity ? shipment : null;

        private LocalPorterPart Role => Worker?.GetPart<LocalPorterPart>() is LocalPorterPart role && role.Ledger == ParentEntity ? role : null;
        internal bool SiteCurrent(Zone zone) => Exact(ParentEntity, zone) && ParentEntity.GetPart<LocalPorterLedgerPart>() == this
            && !ParentEntity.GetPart<PhysicsPart>().Takeable && (WorldLocationContext.For(zone) is not { } world || world.ActiveZone == zone);
        private bool AtPost(Entity actor, Zone zone) => SiteCurrent(zone) && PreparationActions.CurrentActor(actor, zone)
            && LocalGatheringClaims.Alive(actor) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
            && Exact(actor, zone) && SpatialQuery.Distance(zone, actor, ParentEntity) <= 1;
        private bool AtSource(Zone zone) => Exact(Box, zone) && zone.GetEntityPosition(Box) == (OriginX, OriginY);
        internal bool StaticShelf(Zone zone) => Exact(Shelf, zone) && zone.GetEntityPosition(Shelf) == (ShelfX, ShelfY)
            && Shelf.GetPart<DeliveryStoragePart>() is { ReceivesParcels: true } storage && storage.Intact(Shelf, Shelf.GetPart<ContainerPart>());
        private bool EmptyShelf() => Shelf?.GetPart<ContainerPart>() is { IsLocked: false } container && container.Contents.Count == 0;
        private bool EscrowCurrent() => Payer != null && !string.IsNullOrEmpty(OrderID) && Fare is >= 3 and <= 8
            && TradeSystem.GetDrams(ParentEntity) == (Phase == LocalPorterPhase.Booked ? Fare : Fare - DepartureFee);
        private bool Reserved(Zone zone)
        {
            if (OrderZone != zone?.ZoneID || Box?.GetPart<LocalPorterReservationPart>() is not { } mark
                || mark.ParentEntity != Box || mark.Ledger != ParentEntity || mark.OrderID != OrderID
                || DeliveryStoragePart.ParcelWeightOrZero(Box, zone) <= 0) return false;
            var contents = Box.GetPart<ContainerPart>().Contents;
            return contents[0] == FirstMeasure && (contents.Count == 2 ? contents[1] : null) == SecondMeasure && CargoText() == Manifest;
        }
        private bool Carrying(Zone zone) => Exact(Worker, zone) && PreparationActions.Carried(Worker, Box)
            && !zone.GetReadOnlyEntities().Any(e => e.ID == Box.ID);
        private bool LoadCurrent(Zone zone) => Phase == LocalPorterPhase.Booked && SiteCurrent(zone) && EscrowCurrent()
            && AtSource(zone) && StaticShelf(zone) && Reserved(zone) && Role?.Available(Worker, zone) == true
            && SpatialQuery.Distance(zone, Worker, Box) <= 1 && Role.Fits(Worker, Box, alreadyCarried: false, out _);
        private bool Failure(Zone zone) => !LocalGatheringClaims.Alive(Worker) || !Exact(Worker, zone) || Role == null || !Reserved(zone)
            || zone.GetEntityPosition(ParentEntity) != (OriginX, OriginY)
            || (Phase == LocalPorterPhase.Booked ? !StaticShelf(zone) || !AtSource(zone) : !Carrying(zone));
        private static bool Exact(Entity owner, Zone zone) => WorldResourceActions.Ground(owner, zone)
            && zone.GetReadOnlyEntities().Count(e => e.ID == owner.ID) == 1;
        private string OfferKey(Entity actor, LocalPorterQuote quote, string priorOrderID) =>
            $"{HirePrefix}{priorOrderID}:{actor.ID}:{Worker.ID}:{Shelf.ID}:{Box.ID}:{quote.Fare}:{quote.Steps.Count}:{CargoText()}";
        private string CargoText() => string.Join(",", Box.GetPart<ContainerPart>().Contents.Select(item =>
            $"{item.ID}/{item.BlueprintName}/{item.GetPart<StackerPart>().StackCount}/{item.GetPart<ConnectedClayOriginPart>()?.Units}"));
        private bool Claim(InventoryTransaction tx, Entity actor) => tx.TryClaim(ParentEntity, actor, Name) && tx.TryClaim(Box, actor, Name);
        private void ClearActiveOrder(LocalPorterPhase phase, string note)
        {
            Phase = phase; Note = note; Payer = FirstMeasure = SecondMeasure = null;
            // The terminal ID makes the just-used offer obsolete even if the
            // same box and fare are immediately available again.
            OrderZone = Manifest = null;
        }
        private bool Execute(Zone zone, Func<Zone, InventoryTransaction, bool> action)
        {
            var tx = new InventoryTransaction();
            try
            {
                if (!action(zone, tx)) return false;
                tx.Commit();
                return true;
            }
            catch (Exception error)
            {
                Diag.Record("ai", "PorterActionInterrupted", Worker, Box,
                    payload: new { phase = Phase.ToString(), error = error.GetType().Name });
                return tx.IsCommitted;
            }
            finally { if (!tx.IsCommitted) tx.Rollback(); }
        }
        private bool PickupEvent(Entity target, string name)
        {
            var e = GameEvent.New(name);
            e.SetParameter("Actor", (object)Worker);
            e.SetParameter("Item", (object)Box);
            return target.FireEventAndRelease(e);
        }
        private bool Visible(Zone zone) => zone.GetEntityCell(Worker)?.IsVisible == true || zone.GetEntityCell(Shelf)?.IsVisible == true;
        private void VisibleNote(Zone zone, string message) { if (Visible(zone)) MessageLog.Add(message); }
    }

    /// <summary>A saved guard on the box. The ledger owns the transaction and the fare.</summary>
    public sealed class LocalPorterReservationPart : Part
    {
        public override string Name => "LocalPorterReservation";
        public Entity Ledger;
        public string OrderID;
        public static bool Blocks(Entity item) => item?.HasPart<LocalPorterReservationPart>() == true;
        public override bool HandleEvent(GameEvent e) => e.ID != "CanBeTraded";
    }
}
