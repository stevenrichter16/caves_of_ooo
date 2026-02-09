namespace CavesOfOoo.Core
{
    /// <summary>
    /// Handles entity movement through a Zone.
    /// Fires BeforeMove and AfterMove events so parts can validate or react.
    ///
    /// Movement flow:
    /// 1. Create BeforeMove event with Actor, TargetCell, Direction
    /// 2. Fire on the actor — PhysicsPart checks for solid blockers
    /// 3. If not blocked, move the entity in the zone
    /// 4. Fire AfterMove event for post-movement reactions
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
            entity.FireEvent(afterMove);

            return true;
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
