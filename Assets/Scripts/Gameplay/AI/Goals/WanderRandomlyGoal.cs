namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that takes a single random step and finishes.
    /// Preserves the current wander-per-tick behavior: each tick BoredGoal
    /// may push a new WanderRandomlyGoal for one step.
    /// </summary>
    public class WanderRandomlyGoal : GoalHandler
    {
        private bool _acted;

        public override bool IsBusy() => false;

        public override bool Finished() => _acted;

        public override void TakeAction()
        {
            var (dx, dy) = AIHelpers.RandomPassableDirection(ParentEntity, CurrentZone, Rng);
            if (dx != 0 || dy != 0)
                MovementSystem.TryMove(ParentEntity, CurrentZone, dx, dy);

            ParentBrain.CurrentState = AIState.Wander;
            _acted = true;
        }
    }
}
