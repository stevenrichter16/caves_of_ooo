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
    ///
    /// Self-heal during Recover: the goal heals <see cref="HealPerTick"/> HP per
    /// turn while in the Recover phase (clamped to max HP). This ensures the
    /// goal terminates even for creatures without RegenerationMutation — which
    /// is most of them. Without this self-heal, an NPC without passive regen
    /// would be stuck cycling retreat → MaxTurns timeout → retreat forever.
    /// The heal is scoped to Recover phase only, so it doesn't affect combat
    /// balance outside of retreat scenarios.
    /// </summary>
    public class RetreatGoal : GoalHandler
    {
        public int WaypointX;
        public int WaypointY;

        /// <summary>HP fraction (0..1) at or above which the retreat is considered complete.</summary>
        public float SafeHpFraction;

        /// <summary>Hard turn cap so a stuck NPC eventually gives up and falls back to BoredGoal.</summary>
        public int MaxTurns;

        /// <summary>HP healed per tick while in the Recover phase. Clamps to Max HP.</summary>
        public int HealPerTick;

        private enum Phase { Travel, Recover, Done }
        private Phase _phase = Phase.Travel;

        public RetreatGoal(int waypointX, int waypointY, float safeHpFraction = 0.75f,
            int maxTurns = 200, int healPerTick = 1)
        {
            WaypointX = waypointX;
            WaypointY = waypointY;
            SafeHpFraction = safeHpFraction;
            MaxTurns = maxTurns;
            HealPerTick = healPerTick;
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

            // Heal a small amount each tick. Without this, creatures lacking
            // RegenerationMutation (most of them) would never recover HP and
            // this goal would only terminate via the MaxTurns safety cap.
            // Scoped to the Recover phase so it doesn't affect combat balance
            // outside retreat scenarios.
            var hpStat = ParentEntity.GetStat("Hitpoints");
            if (hpStat == null) return;

            if (HealPerTick > 0 && hpStat.BaseValue < hpStat.Max)
            {
                hpStat.BaseValue += HealPerTick;
                if (hpStat.BaseValue > hpStat.Max)
                    hpStat.BaseValue = hpStat.Max;
            }

            // Recovery complete when BaseValue HP is back above the safe
            // fraction. Using BaseValue (not the computed Stat.Value) is
            // deliberate — the heal above only writes to BaseValue, while
            // Stat.Value subtracts any active Penalty. If this gate compared
            // Value, a debuffed NPC (bleed, poison, exhaustion) could heal
            // BaseValue to full yet never satisfy the gate while Penalty
            // persists, deadlocking Recover until the MaxTurns cap pops the
            // goal. BaseValue-vs-Max keeps the exit condition aligned with
            // the quantity we actually mutate.
            int baseHp = hpStat.BaseValue;
            int maxHp = hpStat.Max;
            if (baseHp > 0 && maxHp > 0 && (float)baseHp / maxHp >= SafeHpFraction)
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
