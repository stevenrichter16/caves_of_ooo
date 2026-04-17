namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that flees toward a specific "safe" cell rather than away from an entity.
    /// Mirrors Qud's FleeLocation goal handler.
    ///
    /// Contrast with FleeGoal (entity-repulsion): FleeLocationGoal is waypoint-based —
    /// "run to this location and call it done." Used when the NPC has a designated
    /// safe spot (home, shrine, faction hall, retreat point) rather than a specific
    /// threat to avoid.
    ///
    /// Finishes when:
    /// - The NPC reaches the safe cell
    /// - Age exceeds MaxTurns (gives up, falls back to parent's decision)
    /// - ShouldFlee() becomes false (HP recovered above FleeThreshold)
    /// </summary>
    public class FleeLocationGoal : GoalHandler
    {
        public int SafeX;
        public int SafeY;
        public int MaxTurns;

        /// <summary>When true, the goal finishes as soon as HP is back above FleeThreshold.</summary>
        public bool EndWhenNotFleeing;

        public FleeLocationGoal(int x, int y, int maxTurns = 30, bool endWhenNotFleeing = true)
        {
            SafeX = x;
            SafeY = y;
            MaxTurns = maxTurns;
            EndWhenNotFleeing = endWhenNotFleeing;
        }

        public override bool CanFight() => false;

        public override bool Finished()
        {
            if (Age > MaxTurns) return true;
            var pos = CurrentZone?.GetEntityPosition(ParentEntity) ?? (-1, -1);
            if (pos.x == SafeX && pos.y == SafeY) return true;
            if (EndWhenNotFleeing && ParentBrain != null && !ShouldFlee()) return true;
            return false;
        }

        public override void TakeAction()
        {
            var pos = CurrentZone.GetEntityPosition(ParentEntity);
            if (pos.x < 0) { FailToParent(); return; }

            // Delegate the actual path-following to MoveToGoal so we get A* + greedy fallback.
            // The child runs immediately via the Brain's child-chain execution.
            PushChildGoal(new MoveToGoal(SafeX, SafeY, MaxTurns));
        }

        public override void Failed(GoalHandler child)
        {
            // MoveToGoal gave up (unreachable) — propagate the failure up.
            FailToParent();
        }
    }
}
