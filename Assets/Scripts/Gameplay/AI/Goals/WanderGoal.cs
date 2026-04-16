namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that picks a random reachable cell in the zone and walks to it.
    /// Mirrors Qud's Wander goal handler.
    ///
    /// Reachability is verified via A* before committing to a target — this
    /// prevents NPCs stranded in isolated rooms from repeatedly attempting to
    /// walk to unreachable random cells and silently failing.
    ///
    /// If no reachable cell is found after MaxAttempts random picks, falls back
    /// to WanderRandomlyGoal (a single random step in any passable direction).
    /// </summary>
    public class WanderGoal : GoalHandler
    {
        /// <summary>Maximum random-pick attempts before falling back to random step.</summary>
        private const int MaxAttempts = 10;

        private bool _started;
        private int _targetX;
        private int _targetY;

        public override bool IsBusy() => false;

        public override bool Finished()
        {
            if (!_started) return false;
            var pos = CurrentZone?.GetEntityPosition(ParentEntity) ?? (-1, -1);
            return (pos.x == _targetX && pos.y == _targetY) || Age > 50;
        }

        public override void TakeAction()
        {
            if (!_started)
            {
                _started = true;

                var zone = CurrentZone;
                if (zone == null) { Pop(); return; }

                var myPos = zone.GetEntityPosition(ParentEntity);
                if (myPos.x < 0) { Pop(); return; }

                var rng = Rng;
                for (int attempt = 0; attempt < MaxAttempts; attempt++)
                {
                    int rx = rng.Next(0, Zone.Width);
                    int ry = rng.Next(0, Zone.Height);
                    var cell = zone.GetCell(rx, ry);
                    if (cell == null || !cell.IsPassable())
                        continue;

                    // Verify reachability via A* before committing.
                    // Skip same-cell picks (already there, nothing to walk to).
                    if (rx == myPos.x && ry == myPos.y)
                        continue;

                    var path = FindPath.Search(zone, myPos.x, myPos.y, rx, ry);
                    if (!path.Usable)
                        continue;

                    _targetX = rx;
                    _targetY = ry;
                    PushChildGoal(new MoveToGoal(_targetX, _targetY, 50));
                    return;
                }

                // Every attempt picked an unreachable or same cell.
                // Fall back to a single random step so the NPC still does something.
                PushChildGoal(new WanderRandomlyGoal());
                return;
            }

            // MoveTo child finished or failed — we're done
            Pop();
        }
    }
}
