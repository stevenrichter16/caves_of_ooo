namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that wanders randomly for a fixed number of turns, then pops.
    /// Mirrors Qud's WanderDuration goal handler.
    ///
    /// Contrast with WanderRandomlyGoal (one-shot, single step) and WanderGoal
    /// (walk to one random cell then pop). This one keeps stepping randomly
    /// for Duration ticks — useful for "patrol for 20 turns," "wander while
    /// the alchemist is busy," or "pace during idle conversation."
    ///
    /// Each tick takes one random passable step via WanderRandomlyGoal as a child,
    /// so the stepping logic stays in one place.
    /// </summary>
    public class WanderDurationGoal : GoalHandler
    {
        public int Duration;

        private int _ticksTaken;

        public WanderDurationGoal(int duration)
        {
            Duration = duration;
        }

        public override bool IsBusy() => false;

        public override bool Finished() => _ticksTaken >= Duration;

        public override void TakeAction()
        {
            _ticksTaken++;
            ParentBrain.CurrentState = AIState.Wander;
            PushChildGoal(new WanderRandomlyGoal());
        }
    }
}
