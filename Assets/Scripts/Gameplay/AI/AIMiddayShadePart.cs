using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>An idle actor can use nearby physical shade without leaving its ordinary post.</summary>
    public sealed class AIMiddayShadePart : AIBehaviorPart, IDescribeState
    {
        public override string Name => "AIMiddayShade";
        public string HomeZone;

        private const int SearchSteps = 2;
        private const int HomeReach = 4;
        private static readonly (int X, int Y)[] Directions = { (0, -1), (-1, 0), (1, 0), (0, 1) };

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != AIBoredEvent.ID || !TryUseShade()) return true;

            e.Handled = true;
            return false;
        }

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 45;

        public string DescribeState(DescribeContext context)
        {
            var zone = context.Zone ?? ParentEntity?.SpatialZone;
            if (!Active(zone)) return null;
            var here = zone.GetEntityCell(ParentEntity);
            return NearHome(here) && CanRest(zone, here)
                ? "Resting in the shade until the glare eases." : null;
        }

        private bool Active(Zone zone)
        {
            var actor = ParentEntity;
            var brain = actor?.GetPart<BrainPart>();
            return zone != null && zone.ZoneID == HomeZone && brain?.CurrentZone == zone
                && WorldLocationContext.For(zone)?.ActiveZone == zone
                && !actor.HasTag("Player") && !actor.HasPart<SpatialFootprintPart>()
                && brain.PartyLeader == null && brain.Target == null && brain.HasStartingCell
                && brain.PeekGoal() is BoredGoal && !brain.InConversation
                && string.IsNullOrEmpty(actor.GetProperty("WhenBoredReturnToOnce"))
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && !actor.HasEffect<SittingEffect>() && !actor.HasEffect<RootedEffect>()
                && actor.GetEffect<GrappledEffect>() is not { Broken: false }
                && WorldResourceActions.ActorCurrent(actor, zone)
                && BeatingGlareSystem.IsGlareHours(zone, WorldClock.CurrentTick)
                && !BeatingGlareSystem.HasHeadCover(actor);
        }

        private bool NearHome(Cell cell)
        {
            if (cell == null) return false;
            var brain = ParentEntity.GetPart<BrainPart>();
            return Math.Max(Math.Abs(cell.X - brain.StartingCellX), Math.Abs(cell.Y - brain.StartingCellY)) <= HomeReach;
        }

        private bool CanRest(Zone zone, Cell cell) => cell != null
            && (cell.IsInterior || ShadePart.Covers(zone, cell))
            && !PlacementCheck.Hazard(zone, cell, ParentEntity)
            && TerrainNavigationWeight.ForStep(zone, cell.X, cell.Y, ParentEntity) == 0;

        private bool CanStep(Zone zone, Cell cell) => NearHome(cell)
            && zone.CanPlaceFootprint(ParentEntity, cell.X, cell.Y)
            && !PlacementCheck.Hazard(zone, cell, ParentEntity)
            && TerrainNavigationWeight.ForStep(zone, cell.X, cell.Y, ParentEntity) == 0;

        private bool TryUseShade()
        {
            var zone = ParentEntity?.SpatialZone;
            if (!Active(zone)) return false;
            var here = zone.GetEntityCell(ParentEntity);
            var brain = ParentEntity.GetPart<BrainPart>();
            var idle = brain.PeekGoal();
            if (!NearHome(here)) return false;
            if (CanRest(zone, here)) return true;

            var seen = new HashSet<Cell> { here };
            var open = new Queue<(Cell Here, Cell First, int Steps)>();
            open.Enqueue((here, null, 0));
            while (open.Count > 0)
            {
                var (current, first, steps) = open.Dequeue();
                if (first != null && CanRest(zone, current))
                {
                    bool moved = MovementSystem.TryMoveChecked(ParentEntity, zone, first.X - here.X, first.Y - here.Y,
                        () => Active(zone) && ReferenceEquals(brain.PeekGoal(), idle) && zone.GetEntityCell(ParentEntity) == here
                            && CanStep(zone, first) && CanStep(zone, current) && CanRest(zone, current));
                    return moved || !ReferenceEquals(brain.PeekGoal(), idle);
                }
                if (steps >= SearchSteps) continue;

                foreach (var (dx, dy) in Directions)
                {
                    var next = zone.GetCell(current.X + dx, current.Y + dy);
                    if (next != null && seen.Add(next) && CanStep(zone, next))
                        open.Enqueue((next, first ?? next, steps + 1));
                }
            }
            return false;
        }
    }
}
