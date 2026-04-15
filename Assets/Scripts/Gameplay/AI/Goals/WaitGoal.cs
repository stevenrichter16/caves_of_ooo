namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that idles for a specified number of ticks.
    /// Mirrors Qud's Wait goal handler.
    /// </summary>
    public class WaitGoal : GoalHandler
    {
        public int Duration;

        public WaitGoal(int duration = 1)
        {
            Duration = duration;
        }

        public override bool IsBusy() => false;

        public override bool Finished() => Age >= Duration;

        public override void TakeAction()
        {
            ParentBrain.CurrentState = AIState.Idle;
        }
    }
}
