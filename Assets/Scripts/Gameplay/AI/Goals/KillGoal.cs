using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that tracks and attacks a hostile target.
    /// Mirrors Qud's Kill goal handler.
    /// Finishes when the target dies or leaves the zone.
    ///
    /// Approach strategy:
    /// - Authored tactics → one safe supported ability, when selected
    /// - Adjacent fallback → melee attack
    /// - Not adjacent → try ranged ability, else walk toward target
    /// - Walking uses TryApproachWithPathfinding: greedy-first with A* fallback
    ///   when blocked by walls, so creatures navigate around obstacles to reach
    ///   moving targets (no more marlbacks stuck on building walls).
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

            // A finite carried treatment replaces this whole action, including
            // low-health retreat. Do not grant a second move or attack after use.
            if (ParentEntity.GetPart<FieldMedicinePart>()?.TryUseMedicine(Target, CurrentZone, Rng) == true)
                return;

            if (ParentEntity.GetPart<WeaponRecoveryPart>()?.TryRecover(CurrentZone) == true) return;

            // Check if we should flee instead
            if (ShouldFlee())
            {
                Think("low hp, breaking off attack");
                FailToParent();
                return;
            }

            // Only opted-in actors replace immediate melee with a commitment.
            // A refused adjacent start cannot fall through into an instant swing.
            var commitment = ParentEntity.GetPart<CommittedMeleePart>();
            if (commitment != null && commitment.TryBegin(Target, CurrentZone)) return;

            var tactics = ParentEntity.GetPart<CombatTacticsPart>();
            if (tactics != null && tactics.TryUseAbility(Target, CurrentZone, Rng)) return;
            if (tactics != null && tactics.TryPositionForShot(Target, CurrentZone)) return;

            if (SpatialQuery.Distance(CurrentZone,ParentEntity,Target) == 1)
            {
                if (commitment != null) return;
                Think($"attacking {Target.GetDisplayName()}");
                CombatSystem.PerformMeleeAttack(ParentEntity, Target, CurrentZone, Rng);
            }
            else
            {
                if (tactics != null || !AIHelpers.TryUseRangedAbility(ParentEntity, CurrentZone, Rng, myPos, targetPos))
                {
                    Think($"closing on {Target.GetDisplayName()}");
                    if (ParentEntity.HasPart<SpatialFootprintPart>() || Target.HasPart<SpatialFootprintPart>())
                    {
                        var path=FindPath.ToContact(CurrentZone,ParentEntity,Target);
                        if(path.Usable && path.Steps.Count>0)
                            MovementSystem.TryMove(ParentEntity,CurrentZone,path.Steps[0].dx,path.Steps[0].dy);
                    }
                    else AIHelpers.TryApproachWithPathfinding(ParentEntity, CurrentZone, myPos.x, myPos.y, targetPos.x, targetPos.y);
                }
            }
        }
    }
}
