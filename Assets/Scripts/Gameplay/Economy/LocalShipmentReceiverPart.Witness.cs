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
        public int WitnessX;
        public int WitnessY;
        public int WitnessNotedAtTick = -1;
        public int WitnessReportedAtTick = -1;

        private bool HasPlayerReport => !string.IsNullOrEmpty(WitnessOrderID) && WitnessOrderID == ExpectedOrderID
            && !string.IsNullOrEmpty(WitnessActorID) && !string.IsNullOrEmpty(WitnessBodyID)
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
            string bodyID = report ? WitnessBodyID : subject.ID;
            int x = report ? WitnessX : cell.X, y = report ? WitnessY : cell.Y;
            int noted = WitnessNotedAtTick, applied = -1;

            bool Current() => HasAgreement(order) && order.OrderID == orderID && order.SiteCurrent(zone) && order.StaticShelf(zone)
                && ParentEntity.GetPart<LocalShipmentReceiverPart>() == this
                && ParentEntity.Parts.Count(part => part is LocalShipmentReceiverPart) == 1
                && ConcernOpenedAtTick >= 0 && ConcernClosedAtTick < 0 && ArrivedAtTick < 0
                && order.Phase == LocalPorterPhase.Failed && SearchReportAtTick < 0
                && Merchant == cook && LocalGatheringClaims.Alive(cook) && (DelegatedSearcherID == null || DelegateOverdue)
                && search.Shelf == ParentEntity && search.OrderID == orderID && cook.GetPart<LocalShipmentSearchPart>() == search
                && search.Phase == LocalShipmentSearchPhase.CheckingLoadingPoint && search.SeenPorterBodyID == null
                && RouteWorkMenu.ActorReady(actor, zone) && actor.HasTag("Player") && !string.IsNullOrEmpty(actorID) && actor.ID == actorID
                && RouteWorkMenu.Visible(cell) && CanSpeakFrom(zone.GetEntityCell(actor), cell, zone)
                && WorldResourceActions.Ground(subject, zone) && zone.GetEntityCell(subject) == cell
                && subject.GetPart<RenderPart>() is { Visible: true } render && render.ParentEntity == subject
                && zone.GetReadOnlyEntities().Count(e => e.ID == subject.ID) == 1;

            bool ExactNote(int tick) => WitnessOrderID == orderID && WitnessActorID == actorID
                && WitnessBodyID == bodyID && WitnessX == x && WitnessY == y && WitnessNotedAtTick == tick;

            bool EvidenceCurrent() => report
                ? subject == cook && LocalPorterPart.CanDoLocalWork(cook, zone) && !LocalShipmentSearchPart.PreparingMeal(cook, zone)
                    && !string.IsNullOrEmpty(bodyID) && noted >= ConcernOpenedAtTick && noted <= WorldClock.CurrentTick
                    && zone.InBounds(x, y) && ExactNote(noted)
                : subject.ID == bodyID && LocalShipmentSearchPart.IsExactPorterBody(order, subject, zone)
                    && cell.Occupants.Count(other => LocalShipmentSearchPart.IsExactPorterBody(order, other, zone)) == 1;

            return new RouteWorkMenu.Entry
            {
                Actor = actor, Zone = zone, Cell = cell,
                Key = report ? ReportKey : NoteKey,
                Label = report ? "Tell the cook where you saw the porter's body" : "Note the porter's body",
                Hint = report ? "Give the cook the place you saw. He will check it himself." : "Remember this place so you can tell the cook.",
                Time = report ? ActionTime.OneAction : ActionTime.Free,
                Claims = new[] { subject, order.ParentEntity },
                Before = () => Current() && EvidenceCurrent() && WitnessReportedAtTick < 0
                    && (report || WitnessOrderID == null && WitnessNotedAtTick < 0),
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
                        var prior = (WitnessOrderID, WitnessActorID, WitnessBodyID, WitnessX, WitnessY, WitnessNotedAtTick);
                        tx.Do(() => (WitnessOrderID, WitnessActorID, WitnessBodyID, WitnessX, WitnessY, WitnessNotedAtTick) =
                            (orderID, actorID, bodyID, x, y, applied),
                            () => (WitnessOrderID, WitnessActorID, WitnessBodyID, WitnessX, WitnessY, WitnessNotedAtTick) = prior);
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
