namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that takes a single random step and finishes.
    /// Preserves the current wander-per-tick behavior: each tick BoredGoal
    /// may push a new WanderRandomlyGoal for one step.
    /// </summary>
    public class WanderRandomlyGoal : GoalHandler
    {
        // The brain removes completed children at its next turn. Save this
        // flag so Continue cannot spend another turn repeating the step.
        public bool Acted;

        public override bool IsBusy() => false;

        public override bool Finished() => Acted;

        public override void TakeAction()
        {
            // A paired grazer may use this one pacing step to evade its current
            // nearby hunter. Keep the parent duration/effect lifecycle intact.
            if (ParentEntity.GetPart<SpreadGrazerPart>()?.TryWanderFlight(ParentBrain, CurrentZone, this) == true)
            {
                Acted = true;
                return;
            }
            var (dx, dy) = AIHelpers.RandomPassableDirection(ParentEntity, CurrentZone, Rng);
            if (dx != 0 || dy != 0)
                MovementSystem.TryMove(ParentEntity, CurrentZone, dx, dy);

            ParentBrain.CurrentState = AIState.Wander;
            Acted = true;
        }
    }
}
