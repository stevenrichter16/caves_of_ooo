using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    public static partial class CompanionManagementActions
    {
        static bool SafeAside(Entity follower, Zone zone, Cell cell) => cell?.ParentZone == zone && cell.IsVisible && cell.Explored
            && !cell.BlocksMovement(follower) && zone.TileState.Get(cell.X, cell.Y)?.IsEmpty != false
            && !cell.Occupants.Any(e => e.HasTag("Creature") || e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>() || e.HasPart<TriggerOnStepPart>());
        static Cell AsideCell(Entity actor, Entity follower, Zone zone)
        {
            if (follower.HasPart<SpatialFootprintPart>() || DragSystem.IsDragging(follower)) return null;
            var from = zone.GetEntityCell(follower); var leader = zone.GetEntityCell(actor);
            if (from == null || leader == null) return null;
            Cell best = null; int score = -1;
            for (int dy=-1;dy<=1;dy++) for (int dx=-1;dx<=1;dx++)
            {
                if (dx==0 && dy==0) continue;
                var cell = zone.GetCell(from.X+dx,from.Y+dy);
                if (!SafeAside(follower,zone,cell)) continue;
                int distance = Math.Max(Math.Abs(cell.X-leader.X),Math.Abs(cell.Y-leader.Y));
                if (distance>score) { score=distance; best=cell; }
            }
            return best;
        }
        static bool StepAside(OwnerScope scope, InventoryTransaction tx)
        {
            var follower = scope.Follower; var zone = scope.Zone;
            var from = zone.GetEntityCell(follower); var destination = AsideCell(scope.Actor,follower,zone);
            if (destination == null) return false;
            // Undo only this landing. Independent relocation/death during a
            // callback remains independent; never pull an owner out of another
            // zone or force it into a newly occupied original cell.
            bool movementAdmitted = false;
            tx.Do(null, () =>
            {
                if (movementAdmitted && zone.GetEntityCell(follower)==destination && WorldResourceActions.ActorCurrent(follower,zone)
                    && !from.BlocksMovement(follower) && !from.Occupants.Any(e => e.HasTag("Creature"))
                    && zone.CanPlaceFootprint(follower,from.X,from.Y))
                    zone.MoveEntity(follower,from.X,from.Y);
            });
            bool CurrentChoice()
            {
                movementAdmitted = scope.Current() && !follower.HasPart<SpatialFootprintPart>()
                    && !DragSystem.IsDragging(follower) && SafeAside(follower,zone,destination);
                return movementAdmitted;
            }
            if (!MovementSystem.TryMoveChecked(follower,zone,destination.X-from.X,destination.Y-from.Y,CurrentChoice)) return false;
            tx.BeforeCommit(() => scope.Current(destination));
            tx.AfterCommit(() => MessageLog.Add(follower.GetDisplayName()+" steps aside."));
            return true;
        }
    }
}
