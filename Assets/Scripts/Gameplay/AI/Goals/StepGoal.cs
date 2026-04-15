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
            if (!MovementSystem.TryMove(ParentEntity, CurrentZone, DX, DY))
            {
                FailToParent();
                return;
            }
            _acted = true;
        }
    }
}
