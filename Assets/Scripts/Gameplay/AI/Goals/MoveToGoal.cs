using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that moves to a specific cell using A* pathfinding.
    /// Falls back to greedy step-toward if A* fails or path is blocked.
    /// Mirrors Qud's MoveTo goal handler.
    /// </summary>
    public class MoveToGoal : GoalHandler
    {
        public int TargetX;
        public int TargetY;
        public int MaxTurns;

        // Cached A* path
        private List<(int dx, int dy)> _path;
        private int _pathStep;
        private int _pathZoneVersion = -1;

        public MoveToGoal(int x, int y, int maxTurns = 100)
        {
            TargetX = x;
            TargetY = y;
            MaxTurns = maxTurns;
        }

        public override bool Finished()
        {
            if (Age > MaxTurns) return true;
            var pos = CurrentZone?.GetEntityPosition(ParentEntity) ?? (-1, -1);
            return pos.x == TargetX && pos.y == TargetY;
        }

        public override void TakeAction()
        {
            var pos = CurrentZone.GetEntityPosition(ParentEntity);

            // Compute or revalidate path
            if (_path == null || _pathStep >= _path.Count || _pathZoneVersion != CurrentZone.EntityVersion)
            {
                if (!ComputePath(pos.x, pos.y))
                {
                    // A* failed — try greedy fallback
                    if (!AIHelpers.TryStepToward(ParentEntity, CurrentZone, pos.x, pos.y, TargetX, TargetY))
                        FailToParent();
                    return;
                }
            }

            // Follow cached path
            if (_pathStep < _path.Count)
            {
                var (dx, dy) = _path[_pathStep];
                if (MovementSystem.TryMove(ParentEntity, CurrentZone, dx, dy))
                {
                    _pathStep++;
                }
                else
                {
                    // Step blocked — recompute from current position
                    pos = CurrentZone.GetEntityPosition(ParentEntity);
                    if (!ComputePath(pos.x, pos.y))
                    {
                        // Recompute failed — greedy fallback
                        if (!AIHelpers.TryStepToward(ParentEntity, CurrentZone, pos.x, pos.y, TargetX, TargetY))
                            FailToParent();
                    }
                    else if (_pathStep < _path.Count)
                    {
                        var (rdx, rdy) = _path[_pathStep];
                        if (MovementSystem.TryMove(ParentEntity, CurrentZone, rdx, rdy))
                            _pathStep++;
                        else
                            FailToParent();
                    }
                }
            }
        }

        private bool ComputePath(int fromX, int fromY)
        {
            var result = FindPath.Search(CurrentZone, fromX, fromY, TargetX, TargetY);
            if (!result.Usable || result.Steps.Count == 0)
            {
                _path = null;
                return false;
            }
            _path = result.Steps;
            _pathStep = 0;
            _pathZoneVersion = CurrentZone.EntityVersion;
            return true;
        }
    }
}
