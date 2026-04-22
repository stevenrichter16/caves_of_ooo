namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI behavior part that pushes GuardGoal when the NPC is bored.
    /// The NPC guards its StartingCell — scanning for hostiles, engaging
    /// threats, and returning to post after combat.
    ///
    /// Attach this to guard/warden NPCs via blueprint:
    ///   { "Name": "AIGuard", "Params": [] }
    ///
    /// Requires BrainPart.HasStartingCell to be true (set by GameBootstrap
    /// or on first TakeTurn). Without a starting cell, the guard has no
    /// post to guard and the event is not consumed.
    /// </summary>
    public class AIGuardPart : AIBehaviorPart
    {
        public override string Name => "AIGuard";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == AIBoredEvent.ID)
            {
                bool result = HandleBored();
                if (!result) e.Handled = true;
                return result;
            }
            return true;
        }

        private bool HandleBored()
        {
            var brain = ParentEntity.GetPart<BrainPart>();
            if (brain == null || !brain.HasStartingCell)
                return true; // no post to guard — let default behavior proceed

            // TODO: When party/follower system exists (Phase 8), gate this with
            // CanAIDoIndependentBehavior to prevent followers from guarding independently.
            brain.PushGoal(new GuardGoal(brain.StartingCellX, brain.StartingCellY));
            return false; // consumed — BoredGoal will not proceed to wander/idle
        }
    }
}
