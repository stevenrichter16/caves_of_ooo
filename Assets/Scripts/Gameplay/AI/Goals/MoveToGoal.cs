namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that moves to a specific cell using greedy step-toward logic.
    /// Mirrors Qud's MoveTo goal handler (simplified: no A* pathfinding yet).
    /// Each tick, takes one step toward the destination.
    /// </summary>
    public class MoveToGoal : GoalHandler
    {
        public int TargetX;
        public int TargetY;
        public int MaxTurns;

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
            if (!AIHelpers.TryStepToward(ParentEntity, CurrentZone, pos.x, pos.y, TargetX, TargetY))
            {
                FailToParent();
            }
        }
    }
}
