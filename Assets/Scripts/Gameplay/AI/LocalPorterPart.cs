using System;
using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>A local worker who may quote for a physical handover.</summary>
    public sealed class LocalPorterPart : Part
    {
        public override string Name => "LocalPorter";
        public Entity Ledger;
        private const int MaximumSteps = 32;
        private static readonly (int dx, int dy)[] Directions = { (0, -1), (1, 0), (0, 1), (-1, 0) };

        public bool TryQuote(Entity payer, Entity parcel, Entity shelf, Zone zone, out LocalPorterQuote quote, out string refusal)
        {
            quote = default;
            var worker = ParentEntity;
            if (!LocalGatheringClaims.Alive(payer) || !PreparationActions.CurrentActor(payer, zone)
                || payer.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true || !Available(worker, zone)
                || payer == worker || SpatialQuery.Distance(zone, payer, worker) > 1 || FactionManager.IsHostile(worker, payer))
                return Refuse("Speak to a porter who is free to work.", out refusal);
            if (!WorldResourceActions.Ground(parcel, zone) || parcel.GetPart<PhysicsPart>()?.Takeable != true
                || SpatialQuery.Distance(zone, worker, parcel) > 1 || SpatialQuery.Distance(zone, payer, parcel) > 1)
                return Refuse("Set a box with two measures of lining clay beside you and the porter.", out refusal);
            int weight = DeliveryStoragePart.ParcelWeightOrZero(parcel, zone);
            if (weight == 0)
                return Refuse("The shelf takes one box packed with two measures of lining clay.", out refusal);
            long looseWeight = parcel.GetPart<ContainerPart>().Contents.Sum(item => (long)InventoryPart.GetItemWeight(item));
            if (looseWeight < 0 || looseWeight > weight)
                return Refuse("The contents do not match the weight of this box.", out refusal);
            if (!LocalGatheringClaims.Ground(shelf, zone) || shelf.GetPart<DeliveryStoragePart>() is not { ReceivesParcels: true } storage
                || !storage.Intact(shelf, shelf.GetPart<ContainerPart>()) || shelf.GetPart<ContainerPart>().IsLocked
                || shelf.GetPart<ContainerPart>().Contents.Count != 0)
                return Refuse("The receiving shelf needs an open place for the box.", out refusal);
            if (new[] { payer, worker, parcel, shelf }.Any(owner => zone.GetReadOnlyEntities().Count(e => e.ID == owner.ID) != 1))
                return Refuse("The people, loading point and shelf must still be here.", out refusal);

            if (!Fits(worker, parcel, alreadyCarried: false, out int freeBefore))
                return Refuse("The porter cannot carry this box with their current gear.", out refusal);

            var steps = Route(worker, shelf, zone);
            if (steps == null || steps.Count == 0)
                return Refuse("The porter needs a short, clear route to the receiving shelf.", out refusal);
            int fare = 2 + (steps.Count + 7) / 8 + (weight + 3) / 4;
            if (TradeSystem.GetDrams(payer) < fare)
                return Refuse("The fare is " + fare + " drams. You can still carry the box yourself.", out refusal);

            quote = new LocalPorterQuote(fare, weight, (int)looseWeight, freeBefore, steps.AsReadOnly());
            refusal = null;
            return true;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != AIBoredEvent.ID || Ledger?.GetPart<LocalPorterLedgerPart>() is not { } order || order.Worker != ParentEntity)
                return true;
            var zone = ParentEntity?.SpatialZone;
            if (order.TryAct(zone) || !order.Active && ReturnToPost(zone, order)) { e.Handled = true; return false; }
            return true;
        }

        private bool ReturnToPost(Zone zone, LocalPorterLedgerPart order)
        {
            var worker = ParentEntity;
            if (!Available(worker, zone) || !order.SiteCurrent(zone)) return false;
            var route = Route(worker, Ledger, zone);
            if (route == null || route.Count == 0) return false;
            var here = zone.GetEntityCell(worker);
            var next = route[0];
            return MovementSystem.TryMoveChecked(worker, zone, next.X - here.X, next.Y - here.Y,
                () => zone.GetEntityCell(worker) == here && Available(worker, zone)
                    && order.SiteCurrent(zone) && !order.Active && ClearStep(worker, zone, next));
        }

        internal bool Fits(Entity worker, Entity parcel, bool alreadyCarried, out int freeBefore)
        {
            freeBefore = 0;
            if (worker?.GetPart<InventoryPart>() is not { } pack || worker.SpatialZone == null) return false;
            int weight = DeliveryStoragePart.ParcelWeightOrZero(parcel, worker.SpatialZone);
            if (weight == 0 || !HandlingService.CanLift(worker, parcel, out _)) return false;
            int capacity = pack.GetMaxCarryWeight();
            if (pack.MaxWeight >= 0) capacity = Math.Min(capacity, pack.MaxWeight);
            int carried = pack.GetCarriedWeight();
            if (carried < 0 || capacity < (long)carried + (alreadyCarried ? 0 : weight)) return false;
            freeBefore = capacity - carried;
            return true;
        }

        internal bool Available(Entity worker, Zone zone) =>
            worker?.GetPart<LocalPorterPart>() == this && worker.GetStatValue("Strength") > 0
            && CanDoLocalWork(worker, zone);

        internal static bool CanDoLocalWork(Entity worker, Zone zone)
        {
            if (!LocalGatheringClaims.Alive(worker) || !PreparationActions.CurrentActor(worker, zone)
                || !worker.HasTag("Creature") || worker.HasTag("Player") || worker.HasPart<SpatialFootprintPart>()
                || worker.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || worker.HasEffect<RootedEffect>() || worker.GetEffect<GrappledEffect>() is { Broken: false }
                || worker.GetPart<BrainPart>() is not BrainPart brain || brain.Target != null || brain.PartyLeader != null
                || brain.PeekGoal()?.IsBusy() == true
                || TradeSystem.TraderUnableToTrade(worker, out _)) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager != null && manager.ActiveZone != zone) return false;
            var from = zone.GetEntityCell(worker);
            return !zone.GetReadOnlyEntities().Any(other => other != worker && other.HasTag("Creature")
                && LocalGatheringClaims.Alive(other) && FactionManager.IsHostile(other, worker)
                && zone.GetEntityCell(other) is Cell at && Distance(from, at) <= Math.Min(8, brain.SightRadius)
                && AIHelpers.HasLineOfSight(zone, from.X, from.Y, at.X, at.Y));
        }

        internal static List<Cell> Route(Entity worker, Entity shelf, Zone zone, Func<Cell, bool> suitableEndpoint = null)
            => RouteToCell(worker, zone.GetEntityCell(shelf), zone, suitableEndpoint);

        internal static List<Cell> RouteToCell(Entity worker, Cell target, Zone zone, Func<Cell, bool> suitableEndpoint = null)
        {
            var start = zone.GetEntityCell(worker);
            if (start == null || target?.ParentZone != zone || Distance(start, target) > MaximumSteps + 1) return null;
            bool CanStopAt(Cell cell) => Distance(cell, target) <= 1 && suitableEndpoint?.Invoke(cell) != false;
            if (CanStopAt(start)) return new List<Cell>();
            var visited = new Dictionary<Cell, (Cell prior, int steps)> { [start] = (null, 0) };
            var pending = new Queue<Cell>();
            pending.Enqueue(start);
            while (pending.Count > 0)
            {
                var at = pending.Dequeue();
                if (CanStopAt(at))
                {
                    var result = new List<Cell>();
                    for (var next = at; next != start; next = visited[next].prior) result.Add(next);
                    result.Reverse();
                    return result;
                }
                int steps = visited[at].steps + 1;
                if (steps > MaximumSteps) continue;
                foreach (var (dx, dy) in Directions)
                {
                    var next = zone.GetCell(at.X + dx, at.Y + dy);
                    if (next == null || next == target || visited.ContainsKey(next) || !ClearStep(worker, zone, next)) continue;
                    visited.Add(next, (at, steps));
                    pending.Enqueue(next);
                }
            }
            return null;
        }

        internal static bool ClearStep(Entity worker, Zone zone, Cell step) => zone.CanPlaceFootprint(worker, step.X, step.Y)
            && zone.TileState.Get(step.X, step.Y)?.IsEmpty != false
            && TerrainNavigationWeight.ForStep(zone, step.X, step.Y, worker) == 0
            && !step.Objects.Any(e => e.HasPart<LiquidPoolPart>() || e.HasPart<TriggerOnStepPart>() || e.HasTag("Creature"));

        private static int Distance(Cell a, Cell b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        private static bool Refuse(string reason, out string refusal)
        {
            refusal = reason;
            return false;
        }
    }

    public readonly struct LocalPorterQuote
    {
        public readonly int Fare;
        public readonly int LoadedWeight;
        public readonly int LooseWeight;
        public readonly int FreeBefore;
        public int FreeAfter => FreeBefore - LoadedWeight;
        public readonly IReadOnlyList<Cell> Steps;

        internal LocalPorterQuote(int fare, int loadedWeight, int looseWeight, int freeBefore, IReadOnlyList<Cell> steps)
        {
            Fare = fare;
            LoadedWeight = loadedWeight;
            LooseWeight = looseWeight;
            FreeBefore = freeBefore;
            Steps = steps;
        }
    }
}
