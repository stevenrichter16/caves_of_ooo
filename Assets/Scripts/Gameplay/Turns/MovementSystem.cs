using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Handles entity movement through a Zone.
    /// Fires BeforeMove, AfterMove, and EntityEnteredCell events so parts
    /// can validate or react.
    ///
    /// Movement flow:
    /// 1. Create BeforeMove event with Actor, TargetCell, Direction
    /// 2. Fire on the actor — PhysicsPart checks for solid blockers
    /// 3. If not blocked, move the entity in the zone
    /// 4. Fire AfterMove event on the mover (post-movement reactions)
    /// 5. Fire EntityEnteredCell on every non-mover occupant of the
    ///    destination cell (M6: rune triggers, future turret proximity
    ///    detection, etc.). Mirrors Qud's <c>ObjectEnteredCellEvent</c>
    ///    fired on mine entities (see Qud's Tinkering_Mine.cs:428).
    /// </summary>
    public static class MovementSystem
    {
        /// <summary>
        /// Attempt to move an entity in a direction (dx, dy).
        /// Returns true if the move succeeded, false if blocked.
        /// </summary>
        public static bool TryMove(Entity entity, Zone zone, int dx, int dy)
        {
            var currentCell = zone.GetEntityCell(entity);
            if (currentCell == null) return false;

            int newX = currentCell.X + dx;
            int newY = currentCell.Y + dy;

            return TryMoveTo(entity, zone, newX, newY);
        }

        /// <summary>
        /// Extended move attempt that also returns what blocked movement.
        /// Returns (moved, blockedBy) where blockedBy is the entity that blocked, or null.
        /// </summary>
        public static (bool moved, Entity blockedBy) TryMoveEx(Entity entity, Zone zone, int dx, int dy)
        {
            var currentCell = zone.GetEntityCell(entity);
            if (currentCell == null) return (false, null);

            int newX = currentCell.X + dx;
            int newY = currentCell.Y + dy;

            if (!zone.InBounds(newX, newY)) return (false, null);

            var targetCell = zone.GetCell(newX, newY);
            if (targetCell == null) return (false, null);

            // Fire BeforeMove event
            var beforeMove = GameEvent.New("BeforeMove");
            beforeMove.SetParameter("Actor", (object)entity);
            beforeMove.SetParameter("TargetCell", (object)targetCell);
            beforeMove.SetParameter("SourceCell", (object)currentCell);
            beforeMove.SetParameter("DX", dx);
            beforeMove.SetParameter("DY", dy);

            bool allowed = entity.FireEvent(beforeMove);
            if (!allowed)
            {
                // Check if something blocked us
                var blocker = beforeMove.GetParameter<Entity>("BlockedBy");
                beforeMove.Release();
                return (false, blocker);
            }
            beforeMove.Release();

            // Perform the move
            int oldX = currentCell.X;
            int oldY = currentCell.Y;
            zone.MoveEntity(entity, newX, newY);

            // Fire AfterMove event
            var afterMove = GameEvent.New("AfterMove");
            afterMove.SetParameter("Actor", (object)entity);
            afterMove.SetParameter("Cell", (object)targetCell);
            afterMove.SetParameter("OldX", oldX);
            afterMove.SetParameter("OldY", oldY);
            afterMove.SetParameter("NewX", newX);
            afterMove.SetParameter("NewY", newY);
            entity.FireEventAndRelease(afterMove);

            FireCellEnteredEvents(entity, targetCell);

            return (true, null);
        }

        /// <summary>
        /// Attempt to move an entity to a specific cell.
        /// Returns true if the move succeeded, false if blocked or out of bounds.
        /// </summary>
        public static bool TryMoveTo(Entity entity, Zone zone, int x, int y)
        {
            if (!zone.InBounds(x, y)) return false;

            var currentCell = zone.GetEntityCell(entity);
            var targetCell = zone.GetCell(x, y);
            if (targetCell == null) return false;

            // Fire BeforeMove event — parts can block this
            var beforeMove = GameEvent.New("BeforeMove");
            beforeMove.SetParameter("Actor", (object)entity);
            beforeMove.SetParameter("TargetCell", (object)targetCell);
            if (currentCell != null)
            {
                beforeMove.SetParameter("SourceCell", (object)currentCell);
                beforeMove.SetParameter("DX", x - currentCell.X);
                beforeMove.SetParameter("DY", y - currentCell.Y);
            }

            bool allowed = entity.FireEvent(beforeMove);
            beforeMove.Release();
            if (!allowed) return false;

            // Perform the move
            int oldX = currentCell?.X ?? -1;
            int oldY = currentCell?.Y ?? -1;
            zone.MoveEntity(entity, x, y);

            // Fire AfterMove event
            var afterMove = GameEvent.New("AfterMove");
            afterMove.SetParameter("Actor", (object)entity);
            afterMove.SetParameter("Cell", (object)targetCell);
            afterMove.SetParameter("OldX", oldX);
            afterMove.SetParameter("OldY", oldY);
            afterMove.SetParameter("NewX", x);
            afterMove.SetParameter("NewY", y);
            entity.FireEventAndRelease(afterMove);

            FireCellEnteredEvents(entity, targetCell);

            return true;
        }

        /// <summary>
        /// Fire <c>EntityEnteredCell</c> on every non-mover occupant of
        /// <paramref name="targetCell"/>. Mirrors Qud's
        /// <c>ObjectEnteredCellEvent</c> dispatched against every object
        /// in the destination cell (see Qud's Tinkering_Mine.cs:428).
        ///
        /// We iterate over a snapshot because a listener may mutate the
        /// cell's Objects list during handling — e.g. a single-use rune
        /// with ConsumeOnTrigger=true removing itself from the zone.
        /// </summary>
        private static void FireCellEnteredEvents(Entity mover, Cell targetCell)
        {
            if (targetCell == null) return;
            // Snapshot: consumers may remove themselves or other entities
            // from the cell during event handling.
            var occupants = targetCell.Objects;
            if (occupants.Count == 0) return;
            var snapshot = new List<Entity>(occupants.Count);
            for (int i = 0; i < occupants.Count; i++)
            {
                var occ = occupants[i];
                if (occ == null || occ == mover) continue;
                snapshot.Add(occ);
            }
            for (int i = 0; i < snapshot.Count; i++)
            {
                var occ = snapshot[i];
                var ev = GameEvent.New("EntityEnteredCell");
                ev.SetParameter("Actor", (object)mover);
                ev.SetParameter("Cell", (object)targetCell);
                occ.FireEventAndRelease(ev);
            }
        }

        /// <summary>
        /// Convert a cardinal/ordinal direction index to dx/dy.
        /// Directions: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        /// </summary>
        public static (int dx, int dy) DirectionToDelta(int direction)
        {
            switch (direction)
            {
                case 0: return (0, -1);   // N
                case 1: return (1, -1);   // NE
                case 2: return (1, 0);    // E
                case 3: return (1, 1);    // SE
                case 4: return (0, 1);    // S
                case 5: return (-1, 1);   // SW
                case 6: return (-1, 0);   // W
                case 7: return (-1, -1);  // NW
                default: return (0, 0);
            }
        }
    }
}
