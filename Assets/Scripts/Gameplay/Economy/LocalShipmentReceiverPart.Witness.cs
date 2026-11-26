using System;
using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    public sealed partial class LocalShipmentReceiverPart
    {
        private const string NoteKey = "ShipmentWitnessNote";
        private const string ReportKey = "ShipmentWitnessReport";

        public string WitnessOrderID;
        public string WitnessActorID;
        public string WitnessBodyID;
        public string WitnessLivingPorterID;
        public int WitnessX;
        public int WitnessY;
        public int WitnessNotedAtTick = -1;
        public int WitnessReportedAtTick = -1;

        internal bool HasLivingWitness => !string.IsNullOrEmpty(WitnessLivingPorterID) && WitnessBodyID == null
            && Ledger?.GetPart<LocalPorterLedgerPart>()?.Worker?.ID == WitnessLivingPorterID;

        private bool HasPlayerReport => !string.IsNullOrEmpty(WitnessOrderID) && WitnessOrderID == ExpectedOrderID
            && !string.IsNullOrEmpty(WitnessActorID)
            && (HasLivingWitness || !string.IsNullOrEmpty(WitnessBodyID) && WitnessLivingPorterID == null)
            && WitnessNotedAtTick >= ConcernOpenedAtTick && WitnessNotedAtTick >= 0
            && WitnessReportedAtTick >= WitnessNotedAtTick && WitnessReportedAtTick <= WorldClock.CurrentTick;

        internal Cell PlayerReportCell(Zone zone) => HasPlayerReport && zone != null && zone.InBounds(WitnessX, WitnessY)
            ? zone.GetCell(WitnessX, WitnessY) : null;

        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<LocalShipmentReceiverPart>() == this
            && (command?.StartsWith(NoteKey + "|", StringComparison.Ordinal) == true
                || command?.StartsWith(ReportKey + "|", StringComparison.Ordinal) == true);

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "InventoryAction" || !OwnsInventoryReceipt(e.GetStringParameter("Command"))
                || !RouteWorkMenu.Act(ParentEntity, e)) return true;
            e.Handled = true;
            return false;
        }

        internal static void AppendWitnessActions(List<InventoryAction> rows, Cell cell, Entity actor, Entity target)
        {
            var zone = cell?.ParentZone;
            if (actor?.HasTag("Player") != true || !RouteWorkMenu.Visible(cell) || !RouteWorkMenu.ActorReady(actor, zone)
                || !CanSpeakFrom(zone.GetEntityCell(actor), cell, zone)) return;

            if (target != null)
                AddFor(target);
            else
                foreach (var occupant in cell.Occupants) AddFor(occupant);

            void AddFor(Entity subject)
            {
                if (subject?.HasTag("Corpse") == true)
                {
                    foreach (var shelf in zone.GetReadOnlyEntitiesWithPart<LocalShipmentReceiverPart>())
                        Add(shelf.GetPart<LocalShipmentReceiverPart>(), subject, report: false);
                }
                else if (subject?.GetPart<LocalShipmentSearchPart>() is { } search)
                    Add(search.Shelf?.GetPart<LocalShipmentReceiverPart>(), subject, report: true);
                else if (subject?.GetPart<LocalPorterPart>()?.Ledger?.GetPart<LocalPorterLedgerPart>() is { } order)
                    Add(order.Shelf?.GetPart<LocalShipmentReceiverPart>(), subject, report: false);
            }

            void Add(LocalShipmentReceiverPart receipt, Entity subject, bool report)
            {
                var entry = receipt?.WitnessChoice(actor, zone, cell, subject, report);
                if (entry == null || !entry.Before()) return;
                var options = new InventoryActionList();
                RouteWorkMenu.Rows(receipt.ParentEntity, actor, zone, options, new[] { entry }, retain: true);
                foreach (var action in options.Actions)
                {
                    action.Via = receipt.ParentEntity;
                    rows.Add(action);
                }
            }
        }

        private RouteWorkMenu.Entry WitnessChoice(Entity actor, Zone zone, Cell cell, Entity subject, bool report)
        {
            var order = Ledger?.GetPart<LocalPorterLedgerPart>();
            var cook = Merchant;
            var search = cook?.GetPart<LocalShipmentSearchPart>();
            if (order == null || search == null) return null;

            string actorID = actor.ID, orderID = order.OrderID;
            bool living = report ? HasLivingWitness : !subject.HasTag("Corpse");
            string bodyID = report ? WitnessBodyID : living ? null : subject.ID;
            string livingID = report ? WitnessLivingPorterID : living ? subject.ID : null;
            if (living ? string.IsNullOrEmpty(livingID) || bodyID != null
                : string.IsNullOrEmpty(bodyID) || livingID != null) return null;

            int x = report ? WitnessX : cell.X, y = report ? WitnessY : cell.Y;
            int noted = WitnessNotedAtTick, applied = -1;

            bool OrderStillOpen() => order.Phase == LocalPorterPhase.Failed
                || living && (order.Phase is LocalPorterPhase.Booked or LocalPorterPhase.Carrying);
            bool SearchInProgress() => search.Phase == LocalShipmentSearchPhase.CheckingLoadingPoint
                || living && !report && search.Phase == LocalShipmentSearchPhase.Waiting;

            bool Current() => HasAgreement(order) && order.OrderID == orderID && order.SiteCurrent(zone) && order.StaticShelf(zone)
                && ParentEntity.GetPart<LocalShipmentReceiverPart>() == this
                && ParentEntity.Parts.Count(part => part is LocalShipmentReceiverPart) == 1
                && ConcernOpenedAtTick >= 0 && ConcernClosedAtTick < 0 && ArrivedAtTick < 0
                && OrderStillOpen() && SearchReportAtTick < 0
                && Merchant == cook && LocalGatheringClaims.Alive(cook) && (DelegatedSearcherID == null || DelegateOverdue)
                && search.Shelf == ParentEntity && search.OrderID == orderID && cook.GetPart<LocalShipmentSearchPart>() == search
                && SearchInProgress() && search.SeenPorterBodyID == null
                && RouteWorkMenu.ActorReady(actor, zone) && actor.HasTag("Player") && !string.IsNullOrEmpty(actorID) && actor.ID == actorID
                && RouteWorkMenu.Visible(cell) && CanSpeakFrom(zone.GetEntityCell(actor), cell, zone)
                && WorldResourceActions.Ground(subject, zone) && zone.GetEntityCell(subject) == cell
                && subject.GetPart<RenderPart>() is { Visible: true } render && render.ParentEntity == subject
                && zone.GetReadOnlyEntities().Count(e => e.ID == subject.ID) == 1;

            bool ExactNote(int tick) => WitnessOrderID == orderID && WitnessActorID == actorID
                && WitnessBodyID == bodyID && WitnessLivingPorterID == livingID
                && WitnessX == x && WitnessY == y && WitnessNotedAtTick == tick;

            bool CanRecordNote() => WitnessOrderID == null && WitnessNotedAtTick < 0
                || !living && HasLivingWitness && WitnessOrderID == orderID && WitnessActorID == actorID
                    && WitnessNotedAtTick == noted && noted >= ConcernOpenedAtTick;

            bool EvidenceCurrent() => report
                ? subject == cook && LocalPorterPart.CanDoLocalWork(cook, zone) && !LocalShipmentSearchPart.PreparingMeal(cook, zone)
                    && (!living || order.Worker?.ID == livingID)
                    && noted >= ConcernOpenedAtTick && noted <= WorldClock.CurrentTick
                    && zone.InBounds(x, y) && ExactNote(noted)
                : living
                    ? subject.ID == livingID && order.Phase != LocalPorterPhase.Failed
                        && LocalShipmentSearchPart.IsExactLivingPorter(order, subject, zone)
                    : subject.ID == bodyID && LocalShipmentSearchPart.IsExactPorterBody(order, subject, zone)
                        && cell.Occupants.Count(other => LocalShipmentSearchPart.IsExactPorterBody(order, other, zone)) == 1;

            string label = living
                ? report ? "Tell the cook where you saw the porter alive" : "Note where you saw the porter alive"
                : report ? "Tell the cook where you saw the porter's body" : "Note the porter's body";

            return new RouteWorkMenu.Entry
            {
                Actor = actor, Zone = zone, Cell = cell,
                Key = report ? ReportKey : NoteKey,
                Label = label,
                Hint = report ? "Give the cook the place you saw. He will check it himself." : "Remember this place so you can tell the cook.",
                Time = report ? ActionTime.OneAction : ActionTime.Free,
                Claims = new[] { subject, order.ParentEntity },
                Before = () => Current() && EvidenceCurrent() && WitnessReportedAtTick < 0
                    && (report || CanRecordNote()),
                After = () => Current() && EvidenceCurrent() && (report
                    ? WitnessReportedAtTick == applied && HasPlayerReport
                    : ExactNote(applied) && WitnessReportedAtTick < 0),
                Apply = tx =>
                {
                    applied = WorldClock.CurrentTick;
                    if (report)
                    {
                        int prior = WitnessReportedAtTick;
                        tx.Do(() => WitnessReportedAtTick = applied, () => WitnessReportedAtTick = prior);
                    }
                    else
                    {
                        var prior = (WitnessOrderID, WitnessActorID, WitnessBodyID, WitnessLivingPorterID, WitnessX, WitnessY, WitnessNotedAtTick);
                        tx.Do(() => (WitnessOrderID, WitnessActorID, WitnessBodyID, WitnessLivingPorterID, WitnessX, WitnessY, WitnessNotedAtTick) =
                            (orderID, actorID, bodyID, livingID, x, y, applied),
                            () => (WitnessOrderID, WitnessActorID, WitnessBodyID, WitnessLivingPorterID, WitnessX, WitnessY, WitnessNotedAtTick) = prior);
                    }
                    return true;
                },
                Publish = () => MessageLog.Add(report
                    ? $"{cook.GetDisplayName()} takes your report and will check the place you saw."
                    : "You remember where you saw the missing porter. You can tell the cook if you meet him.")
            };
        }
    }
}
