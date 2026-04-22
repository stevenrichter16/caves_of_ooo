namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that guards a specific position. Scans for hostiles, returns to post,
    /// and idles when at position.
    /// Mirrors Qud's Guard goal handler.
    ///
    /// At post: does NOT push a child goal — just sets Idle state and returns.
    /// This keeps GuardGoal on top of the stack so it re-scans for hostiles
    /// every tick (no 2-tick reactivity delay from WaitGoal blocking).
    /// </summary>
    public class GuardGoal : GoalHandler
    {
        public int GuardX;
        public int GuardY;

        public GuardGoal(int x, int y)
        {
            GuardX = x;
            GuardY = y;
        }

        public override bool IsBusy() => false;

        public override void TakeAction()
        {
            // Scan for hostiles
            Entity hostile = AIHelpers.FindNearestHostile(ParentEntity, CurrentZone, ParentBrain.SightRadius);
            if (hostile != null)
            {
                ParentBrain.Target = hostile;
                PushChildGoal(new KillGoal(hostile));
                return;
            }

            // Return to guard position if drifted
            var pos = CurrentZone.GetEntityPosition(ParentEntity);
            if (pos.x != GuardX || pos.y != GuardY)
            {
                PushChildGoal(new MoveToGoal(GuardX, GuardY));
                return;
            }

            // At post: idle in place. Don't push a child — re-scan every tick.
            ParentBrain.CurrentState = AIState.Idle;
        }
    }
}
