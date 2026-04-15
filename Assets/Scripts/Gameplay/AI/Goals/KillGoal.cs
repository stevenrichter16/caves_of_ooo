using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that tracks and attacks a hostile target.
    /// Mirrors Qud's Kill goal handler.
    /// Finishes when the target dies or leaves the zone.
    /// </summary>
    public class KillGoal : GoalHandler
    {
        public Entity Target;

        public KillGoal(Entity target)
        {
            Target = target;
        }

        public override bool Finished()
        {
            return Target == null || CurrentZone?.GetEntityCell(Target) == null;
        }

        public override void TakeAction()
        {
            ParentBrain.CurrentState = AIState.Chase;
            ParentBrain.Target = Target;

            var myPos = CurrentZone.GetEntityPosition(ParentEntity);
            var targetPos = CurrentZone.GetEntityPosition(Target);

            // Check if we should flee instead
            if (ShouldFlee())
            {
                FailToParent();
                return;
            }

            if (AIHelpers.IsAdjacent(myPos.x, myPos.y, targetPos.x, targetPos.y))
            {
                CombatSystem.PerformMeleeAttack(ParentEntity, Target, CurrentZone, Rng);
            }
            else
            {
                if (!AIHelpers.TryUseRangedAbility(ParentEntity, CurrentZone, Rng, myPos, targetPos))
                    AIHelpers.TryStepToward(ParentEntity, CurrentZone, myPos.x, myPos.y, targetPos.x, targetPos.y);
            }
        }
    }
}
