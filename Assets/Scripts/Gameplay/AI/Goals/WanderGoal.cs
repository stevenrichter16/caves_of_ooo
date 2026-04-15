namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that picks a random passable cell in the zone and walks to it.
    /// Mirrors Qud's Wander goal handler.
    /// </summary>
    public class WanderGoal : GoalHandler
    {
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
                // Pick a random passable cell within the zone
                var zone = CurrentZone;
                if (zone == null) { Pop(); return; }

                int attempts = 0;
                var rng = Rng;
                while (attempts < 50)
                {
                    int rx = rng.Next(0, Zone.Width);
                    int ry = rng.Next(0, Zone.Height);
                    var cell = zone.GetCell(rx, ry);
                    if (cell != null && cell.IsPassable())
                    {
                        _targetX = rx;
                        _targetY = ry;
                        PushChildGoal(new MoveToGoal(_targetX, _targetY, 50));
                        return;
                    }
                    attempts++;
                }
                // Couldn't find a valid cell
                Pop();
                return;
            }

            // If we get here, MoveTo child finished or failed — we're done
            Pop();
        }
    }
}
