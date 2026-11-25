using System;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>Identifies the actual employer of an eligible local search worker.</summary>
    public sealed class LocalShipmentEmploymentPart : Part
    {
        public override string Name => "LocalShipmentEmployment";
        public Entity Employer;
    }

    public enum LocalShipmentSearchPhase { Waiting, CheckingLoadingPoint, Returning, Finished, Stopped }

    /// <summary>A single searcher's walk assigned to one overdue local order.</summary>
    public sealed class LocalShipmentSearchPart : Part, IDescribeState
    {
        public override string Name => "LocalShipmentSearch";
        public Entity Shelf;
        public string OrderID;
        public LocalShipmentSearchPhase Phase;
        public int LegAttempts;
        public bool CheckedLoadingPoint;
        public string SeenPorterBodyID;
        public int SeenPorterBodyAtTick = -1;
        public int SeenPorterBodyX;
        public int SeenPorterBodyY;
        private const int SearchDelayTicks = 40;
        internal const int LegAttemptLimit = 24;
        private const int RecognitionRange = 3;
        [NonSerialized] private bool _stepping;

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 25;

        public string DescribeState(DescribeContext context) => Phase switch
        {
            LocalShipmentSearchPhase.CheckingLoadingPoint => "Checking the loading point for news of the overdue clay shipment.",
            LocalShipmentSearchPhase.Returning => "Heading back to the kitchen.",
            LocalShipmentSearchPhase.Finished when SeenPorterBodyID != null => "Found the missing porter dead and returned to the kitchen.",
            LocalShipmentSearchPhase.Finished when CheckedLoadingPoint => "Checked the loading point and returned to the kitchen.",
            _ => null
        };

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != AIBoredEvent.ID || _stepping || Phase is LocalShipmentSearchPhase.Finished or LocalShipmentSearchPhase.Stopped) return true;
            _stepping = true;
            try
            {
                if (!TryAct()) return true;
                e.Handled = true;
                return false;
            }
            finally { _stepping = false; }
        }

        private bool TryAct()
        {
            var actor = ParentEntity;
            var zone = actor?.SpatialZone;
            if (!LocalPorterPart.CanDoLocalWork(actor, zone) || PreparingMeal(actor, zone)
                || Shelf?.GetPart<LocalShipmentReceiverPart>() is not { } receipt
                || receipt.Ledger?.GetPart<LocalPorterLedgerPart>() is not { } order
                || !AssignmentCurrent(zone, actor, receipt, order)) return false;

            if (!order.StaticShelf(zone))
            {
                Phase = LocalShipmentSearchPhase.Stopped;
                return false;
            }

            if (Phase == LocalShipmentSearchPhase.Waiting)
            {
                if (receipt.ConcernClosedAtTick >= 0 || receipt.ArrivedAtTick >= 0
                    || receipt.DelegatedSearcherID != null && !receipt.DelegateOverdue
                    || WorldClock.CurrentTick < (long)receipt.ConcernOpenedAtTick + SearchDelayTicks
                    || SpatialQuery.Distance(zone, actor, Shelf) > 1) return false;
                if (!LoadingPointCurrent(zone, order))
                {
                    Phase = LocalShipmentSearchPhase.Stopped;
                    return false;
                }
                if (receipt.TryDelegateSearch(order, zone)) return true;
                Phase = LocalShipmentSearchPhase.CheckingLoadingPoint;
                Announce(zone, "sets out to check the loading point.");
                return true;
            }

            bool outbound = Phase == LocalShipmentSearchPhase.CheckingLoadingPoint;
            bool delegateSearch = receipt.IsDelegatedSearcher(actor);
            bool reporting = !outbound && delegateSearch && !receipt.DelegateOverdue && receipt.ConcernClosedAtTick < 0;
            bool recipientAvailable = reporting && receipt.CanReceiveSearchReport(zone);
            var target = outbound ? receipt.Ledger : recipientAvailable ? receipt.Merchant : Shelf;
            Func<Cell, bool> endpoint = recipientAvailable ? cell => receipt.CanSpeakToCookFrom(cell, zone) : null;
            if (outbound && (!LoadingPointCurrent(zone, order)
                || delegateSearch && receipt.DelegateOverdue
                || receipt.ConcernClosedAtTick >= 0 && CanSee(actor, zone, Shelf)))
            {
                BeginReturn();
                return true;
            }
            if (outbound && receipt.ConcernClosedAtTick < 0 && TryObservePorterBody(actor, zone, order))
            {
                BeginReturn();
                return true;
            }
            if (SpatialQuery.Distance(zone, actor, target) <= 1 && endpoint?.Invoke(zone.GetEntityCell(actor)) != false)
            {
                if (outbound)
                {
                    CheckedLoadingPoint = true;
                    BeginReturn();
                    Announce(zone, "checks the loading point before heading back.");
                }
                else
                {
                    if (reporting && (!recipientAvailable || !receipt.TryReceiveSearchReport(this, zone))) return false;
                    Phase = LocalShipmentSearchPhase.Finished;
                    bool reportsBody = SeenPorterBodyID != null && (!delegateSearch || reporting);
                    Announce(zone, reportsBody ? "returns to the kitchen and reports finding the missing porter dead."
                        : "returns to the kitchen.");
                }
                return true;
            }
            if (LegAttempts >= LegAttemptLimit)
            {
                if (outbound)
                {
                    BeginReturn();
                    return true;
                }
                Phase = LocalShipmentSearchPhase.Stopped;
                return false;
            }

            LegAttempts++;
            if (LocalPorterPart.Route(actor, target, zone, endpoint) is not { Count: > 0 } path) return false;
            var here = zone.GetEntityCell(actor);
            var next = path[0];
            var phase = Phase;
            return MovementSystem.TryMoveChecked(actor, zone, next.X - here.X, next.Y - here.Y,
                () => Phase == phase && zone.GetEntityCell(actor) == here && AssignmentCurrent(zone, actor, receipt, order)
                    && order.StaticShelf(zone) && (!outbound || LoadingPointCurrent(zone, order))
                    && LocalPorterPart.CanDoLocalWork(actor, zone) && !PreparingMeal(actor, zone)
                    && LocalPorterPart.ClearStep(actor, zone, next));
        }

        private bool AssignmentCurrent(Zone zone, Entity actor, LocalShipmentReceiverPart receipt, LocalPorterLedgerPart order) =>
            (receipt.Merchant == actor || receipt.IsDelegatedSearcher(actor))
            && receipt.ParentEntity == Shelf && Shelf.GetPart<LocalShipmentReceiverPart>() == receipt
            && actor.GetPart<LocalShipmentSearchPart>() == this
            && !string.IsNullOrEmpty(OrderID) && receipt.ExpectedOrderID == OrderID && order.OrderID == OrderID
            && receipt.ConcernOpenedAtTick >= 0 && order.Shelf == Shelf && order.ParentEntity == receipt.Ledger
            && zone.GetReadOnlyEntities().Count(e => e.ID == actor.ID) == 1;

        private static bool LoadingPointCurrent(Zone zone, LocalPorterLedgerPart order) =>
            order.SiteCurrent(zone) && zone.GetEntityPosition(order.ParentEntity) == (order.OriginX, order.OriginY);

        internal static bool PreparingMeal(Entity actor, Zone zone) =>
            zone.GetReadOnlyEntitiesWithPart<KitchenBatchPart>().Any(e =>
                e.GetPart<KitchenBatchPart>() is { Configured: true, State: "Working" } batch && batch.Worker == actor);

        private bool TryObservePorterBody(Entity actor, Zone zone, LocalPorterLedgerPart order)
        {
            var porter = order.Worker;
            if (SeenPorterBodyID != null || order.Phase != LocalPorterPhase.Failed || string.IsNullOrEmpty(porter?.ID)
                || LocalGatheringClaims.Alive(porter) || porter.GetPart<LocalPorterPart>()?.Ledger != order.ParentEntity
                || porter.GetPart<CorpsePart>() is not { CorpseBlueprint: { Length: > 0 } corpseBlueprint }) return false;

            var bodies = zone.GetEntitiesWithTag("Corpse").Where(body => body.BlueprintName == corpseBlueprint
                && body.GetProperty("SourceID") == porter.ID && body.GetProperty("SourceBlueprint") == porter.BlueprintName
                && WorldResourceActions.Ground(body, zone) && CanSee(actor, zone, body, RecognitionRange)).Take(2).ToArray();
            if (bodies.Length != 1 || zone.GetReadOnlyEntities().Count(e => e.ID == bodies[0].ID) != 1) return false;

            SeenPorterBodyID = bodies[0].ID;
            SeenPorterBodyAtTick = WorldClock.CurrentTick;
            (SeenPorterBodyX, SeenPorterBodyY) = zone.GetEntityPosition(bodies[0]);
            return true;
        }

        private static bool CanSee(Entity actor, Zone zone, Entity target, int distanceLimit = int.MaxValue)
        {
            var from = zone.GetEntityCell(actor);
            var to = zone.GetEntityCell(target);
            return SpatialQuery.Distance(zone, actor, target) <= Math.Min(distanceLimit, actor.GetPart<BrainPart>().SightRadius)
                && AIHelpers.HasLineOfSight(zone, from.X, from.Y, to.X, to.Y);
        }

        private void BeginReturn()
        {
            Phase = LocalShipmentSearchPhase.Returning;
            LegAttempts = 0;
        }

        private void Announce(Zone zone, string action)
        {
            if (zone.GetEntityCell(ParentEntity)?.IsVisible == true)
                MessageLog.Add($"{ParentEntity.GetDisplayName()} {action}");
        }
    }
}
