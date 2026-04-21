using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that tracks and attacks a hostile target.
    /// Mirrors Qud's Kill goal handler.
    /// Finishes when the target dies or leaves the zone.
    ///
    /// Approach strategy:
    /// - Adjacent → melee attack
    /// - Not adjacent → try ranged ability, else walk toward target
    /// - Walking uses TryApproachWithPathfinding: greedy-first with A* fallback
    ///   when blocked by walls, so creatures navigate around obstacles to reach
    ///   moving targets (no more snapjaws stuck on building walls).
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

        public override string GetDetails()
        {
            if (Target == null) return null;
            string name = Target.GetDisplayName();
            return string.IsNullOrEmpty(name) ? null : $"target={name}";
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
                Think("low hp, breaking off attack");
                FailToParent();
                return;
            }

            if (AIHelpers.IsAdjacent(myPos.x, myPos.y, targetPos.x, targetPos.y))
            {
                Think($"attacking {Target.GetDisplayName()}");
                CombatSystem.PerformMeleeAttack(ParentEntity, Target, CurrentZone, Rng);
            }
            else
            {
                if (!AIHelpers.TryUseRangedAbility(ParentEntity, CurrentZone, Rng, myPos, targetPos))
                {
                    Think($"closing on {Target.GetDisplayName()}");
                    AIHelpers.TryApproachWithPathfinding(ParentEntity, CurrentZone, myPos.x, myPos.y, targetPos.x, targetPos.y);
                }
            }
        }
    }
}
