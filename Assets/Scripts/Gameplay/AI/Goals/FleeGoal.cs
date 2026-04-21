using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that runs away from a threat entity.
    /// Mirrors Qud's Flee goal handler.
    /// If cornered and adjacent, fights back as a last resort.
    /// </summary>
    public class FleeGoal : GoalHandler
    {
        public Entity FleeFrom;
        public int MaxTurns;

        public FleeGoal(Entity fleeFrom, int maxTurns = 20)
        {
            FleeFrom = fleeFrom;
            MaxTurns = maxTurns;
        }

        public override bool CanFight() => false;

        public override bool Finished()
        {
            if (FleeFrom == null) return true;
            if (CurrentZone?.GetEntityCell(FleeFrom) == null) return true;
            if (Age > MaxTurns) return true;
            if (!ShouldFlee()) return true;
            return false;
        }

        public override string GetDetails()
        {
            string name = FleeFrom?.GetDisplayName() ?? "null";
            return $"from={name} | age={Age}/{MaxTurns}";
        }

        public override void TakeAction()
        {
            var myPos = CurrentZone.GetEntityPosition(ParentEntity);
            var threatPos = CurrentZone.GetEntityPosition(FleeFrom);

            if (!AIHelpers.TryStepAway(ParentEntity, CurrentZone, myPos.x, myPos.y, threatPos.x, threatPos.y))
            {
                // Cornered: fight back if adjacent
                if (AIHelpers.IsAdjacent(myPos.x, myPos.y, threatPos.x, threatPos.y))
                {
                    Think($"cornered by {FleeFrom?.GetDisplayName()}, fighting back");
                    CombatSystem.PerformMeleeAttack(ParentEntity, FleeFrom, CurrentZone, Rng);
                }
            }
            else
            {
                Think($"fleeing from {FleeFrom?.GetDisplayName()}");
            }
        }
    }
}
