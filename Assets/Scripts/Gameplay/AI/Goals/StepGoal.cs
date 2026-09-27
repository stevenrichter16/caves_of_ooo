namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that takes a single tile step in a direction.
    /// Mirrors Qud's Step goal handler.
    /// </summary>
    public class StepGoal : GoalHandler
    {
        public int DX;
        public int DY;
        private bool _acted;

        public StepGoal(int dx, int dy)
        {
            DX = dx;
            DY = dy;
        }

        public override bool Finished() => _acted;

        public override void TakeAction()
        {
            var move = MovementSystem.TryMoveDetailed(ParentEntity, CurrentZone, DX, DY);
            if (move.ActionPerformed) return;
            if (!move.Moved)
            {
                FailToParent();
                return;
            }
            _acted = true;
        }
    }
}
