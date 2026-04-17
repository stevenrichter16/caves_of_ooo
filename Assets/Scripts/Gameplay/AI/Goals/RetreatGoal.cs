namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that retreats to a known-safe waypoint (typically the NPC's StartingCell),
    /// then holds position until HP recovers.
    /// Mirrors Qud's Retreat goal handler — a structured fallback rather than raw flee.
    ///
    /// Difference from FleeGoal:
    /// - FleeGoal = "run AWAY from entity X" (entity-repulsion)
    /// - FleeLocationGoal = "run TO cell Y" (waypoint, one-shot)
    /// - RetreatGoal = "run TO cell Y AND wait until you recover" (waypoint + recovery hold)
    ///
    /// Difference from GuardGoal:
    /// - GuardGoal actively scans for hostiles at post and engages them
    /// - RetreatGoal ignores hostiles while recovering (CanFight=false); relies
    ///   on distance from the threat + HP regen to recover safely
    ///
    /// Typical usage: pushed by AISelfPreservationPart when HP drops below
    /// RetreatThreshold. Retreats to BrainPart.StartingCell and holds until
    /// HP climbs back above the "safe" threshold.
    /// </summary>
    public class RetreatGoal : GoalHandler
    {
        public int WaypointX;
        public int WaypointY;

        /// <summary>HP fraction (0..1) at or above which the retreat is considered complete.</summary>
        public float SafeHpFraction;

        /// <summary>Hard turn cap so a stuck NPC eventually gives up and falls back to BoredGoal.</summary>
        public int MaxTurns;

        private enum Phase { Travel, Recover, Done }
        private Phase _phase = Phase.Travel;

        public RetreatGoal(int waypointX, int waypointY, float safeHpFraction = 0.75f, int maxTurns = 200)
        {
            WaypointX = waypointX;
            WaypointY = waypointY;
            SafeHpFraction = safeHpFraction;
            MaxTurns = maxTurns;
        }

        public override bool CanFight() => false;

        public override bool Finished()
        {
            if (Age > MaxTurns) return true;
            return _phase == Phase.Done;
        }

        public override void TakeAction()
        {
            if (CurrentZone == null || ParentEntity == null) { Pop(); return; }

            switch (_phase)
            {
                case Phase.Travel:
                    TravelToWaypoint();
                    break;
                case Phase.Recover:
                    RecoverAtWaypoint();
                    break;
            }
        }

        private void TravelToWaypoint()
        {
            var pos = CurrentZone.GetEntityPosition(ParentEntity);
            if (pos.x == WaypointX && pos.y == WaypointY)
            {
                _phase = Phase.Recover;
                RecoverAtWaypoint();
                return;
            }
            PushChildGoal(new MoveToGoal(WaypointX, WaypointY, MaxTurns));
        }

        private void RecoverAtWaypoint()
        {
            if (ParentBrain != null)
                ParentBrain.CurrentState = AIState.Idle;

            // Recovery complete when HP is back above the safe fraction
            int hp = ParentEntity.GetStatValue("Hitpoints", 0);
            int maxHp = ParentEntity.GetStat("Hitpoints")?.Max ?? 0;
            if (hp > 0 && maxHp > 0 && (float)hp / maxHp >= SafeHpFraction)
            {
                _phase = Phase.Done;
            }
            // else: stay idle at waypoint. No WaitGoal push — stays on top so
            // next tick re-checks HP (same pattern as GuardGoal-at-post).
        }

        public override void Failed(GoalHandler child)
        {
            // MoveToGoal couldn't reach the waypoint (path blocked, unreachable).
            // Fail to parent — whoever pushed RetreatGoal can decide what to do
            // (usually BoredGoal, which will push FleeGoal as the fallback).
            FailToParent();
        }
    }
}
