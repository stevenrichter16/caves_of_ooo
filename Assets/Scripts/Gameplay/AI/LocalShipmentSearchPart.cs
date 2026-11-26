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
        public bool CheckedPlayerLead;
        public string SeenPorterBodyID;
        public int SeenPorterBodyAtTick = -1;
        public int SeenPorterBodyX;
        public int SeenPorterBodyY;
        public string SeenLivingPorterID;
        public int SeenLivingPorterAtTick = -1;
        private const int SearchDelayTicks = 40;
        internal const int LegAttemptLimit = 24;
        private const int RecognitionRange = 3;
        [NonSerialized] private bool _stepping;

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 25;
        internal bool HasLivingSighting => !string.IsNullOrEmpty(SeenLivingPorterID) && SeenLivingPorterAtTick >= 0;

        public string DescribeState(DescribeContext context) => Phase switch
        {
            LocalShipmentSearchPhase.CheckingLoadingPoint when !CheckedPlayerLead
                && Shelf?.GetPart<LocalShipmentReceiverPart>() is { } receipt
                && receipt.Merchant == ParentEntity && receipt.PlayerReportCell(ParentEntity.SpatialZone) != null
                => receipt.HasLivingWitness ? "Checking where a witness saw the missing porter alive."
                    : "Checking the place a witness reported seeing the missing porter's body.",
            LocalShipmentSearchPhase.CheckingLoadingPoint => "Checking the loading point for news of the overdue clay shipment.",
            LocalShipmentSearchPhase.Returning => "Heading back to the kitchen.",
            LocalShipmentSearchPhase.Finished when SeenPorterBodyID != null => "Found the missing porter dead and returned to the kitchen.",
            LocalShipmentSearchPhase.Finished when LivingReportPublished() => "Saw the missing porter alive while checking the route.",
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
            if (outbound && receipt.ConcernClosedAtTick < 0)
            {
                if (TryObservePorterBody(actor, zone, order))
                {
                    BeginReturn();
                    return true;
                }
                ObserveLivingPorter(actor, zone, order);

                if (!delegateSearch && !CheckedPlayerLead && LegAttempts < LegAttemptLimit
                    && receipt.PlayerReportCell(zone) is { } lead)
                {
                    bool CanCheckFrom(Cell at) => AIHelpers.HasLineOfSight(zone, at.X, at.Y, lead.X, lead.Y);
                    var leadPath = LocalPorterPart.RouteToCell(actor, lead, zone, CanCheckFrom);
                    if (leadPath is { Count: > 0 })
                    {
                        LegAttempts++;
                        return StepToward(leadPath[0], () => receipt.ConcernClosedAtTick < 0
                            && !CheckedPlayerLead && receipt.PlayerReportCell(zone) == lead);
                    }
                    CheckedPlayerLead = true;
                    Announce(zone, leadPath == null
                        ? "cannot reach the place the witness described and goes to check the loading point."
                        : "checks the place the witness described and goes to check the loading point.");
                }
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
                    bool reportsLiving = HasLivingSighting && (!delegateSearch || reporting);
                    Announce(zone, reportsBody ? "returns to the kitchen and reports finding the missing porter dead."
                        : reportsLiving ? "returns to the kitchen and reports seeing the missing porter alive."
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
            return StepToward(path[0]);

            bool StepToward(Cell next, Func<bool> destinationCurrent = null)
            {
                var here = zone.GetEntityCell(actor);
                var phase = Phase;
                return MovementSystem.TryMoveChecked(actor, zone, next.X - here.X, next.Y - here.Y,
                    () => Phase == phase && zone.GetEntityCell(actor) == here && AssignmentCurrent(zone, actor, receipt, order)
                        && order.StaticShelf(zone) && (!outbound || LoadingPointCurrent(zone, order))
                        && LocalPorterPart.CanDoLocalWork(actor, zone) && !PreparingMeal(actor, zone)
                        && LocalPorterPart.ClearStep(actor, zone, next) && destinationCurrent?.Invoke() != false);
            }
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

        private void ObserveLivingPorter(Entity actor, Zone zone, LocalPorterLedgerPart order)
        {
            var porter = order.Worker;
            if (HasLivingSighting || porter == actor || !IsExactLivingPorter(order, porter, zone)
                || !CanSee(actor, zone, porter, RecognitionRange)) return;

            SeenLivingPorterID = porter.ID;
            SeenLivingPorterAtTick = WorldClock.CurrentTick;
        }

        internal static bool IsExactLivingPorter(LocalPorterLedgerPart order, Entity porter, Zone zone) =>
            porter != null && porter == order.Worker && !string.IsNullOrEmpty(porter.ID)
            && LocalGatheringClaims.Alive(porter) && porter.HasTag("Creature") && !porter.HasTag("Corpse")
            && WorldResourceActions.Ground(porter, zone)
            && porter.GetPart<LocalPorterPart>() is { } worker && worker.ParentEntity == porter && worker.Ledger == order.ParentEntity
            && porter.GetPart<RenderPart>() is { Visible: true } render && render.ParentEntity == porter
            && zone.GetReadOnlyEntities().Count(e => e.ID == porter.ID) == 1;

        private bool LivingReportPublished()
        {
            var receipt = Shelf?.GetPart<LocalShipmentReceiverPart>();
            return HasLivingSighting && receipt != null && receipt.ExpectedOrderID == OrderID
                && (receipt.Merchant == ParentEntity || receipt.IsDelegatedSearcher(ParentEntity)
                    && receipt.SearchReportSawLivingPorter && receipt.SearchReportAtTick >= SeenLivingPorterAtTick);
        }

        private bool TryObservePorterBody(Entity actor, Zone zone, LocalPorterLedgerPart order)
        {
            if (SeenPorterBodyID != null || !TryGetDeadPorter(order, out var porter, out var blueprint)) return false;
            var bodies = zone.GetEntitiesWithTag("Corpse").Where(body => MatchesBody(body, porter, blueprint, zone)
                && CanSee(actor, zone, body, RecognitionRange)).Take(2).ToArray();
            if (bodies.Length != 1 || zone.GetReadOnlyEntities().Count(e => e.ID == bodies[0].ID) != 1) return false;

            SeenPorterBodyID = bodies[0].ID;
            SeenPorterBodyAtTick = WorldClock.CurrentTick;
            (SeenPorterBodyX, SeenPorterBodyY) = zone.GetEntityPosition(bodies[0]);
            return true;
        }

        internal static bool IsExactPorterBody(LocalPorterLedgerPart order, Entity body, Zone zone)
            => body?.HasTag("Corpse") == true && TryGetDeadPorter(order, out var porter, out var blueprint)
                && MatchesBody(body, porter, blueprint, zone);

        private static bool TryGetDeadPorter(LocalPorterLedgerPart order, out Entity porter, out string blueprint)
        {
            porter = order.Worker;
            blueprint = porter?.GetPart<CorpsePart>()?.CorpseBlueprint;
            return order.Phase == LocalPorterPhase.Failed && !string.IsNullOrEmpty(porter?.ID)
                && !LocalGatheringClaims.Alive(porter) && porter.GetPart<LocalPorterPart>()?.Ledger == order.ParentEntity
                && !string.IsNullOrEmpty(blueprint);
        }

        private static bool MatchesBody(Entity body, Entity porter, string blueprint, Zone zone)
            => body.BlueprintName == blueprint && body.GetProperty("SourceID") == porter.ID
                && body.GetProperty("SourceBlueprint") == porter.BlueprintName && WorldResourceActions.Ground(body, zone);

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
