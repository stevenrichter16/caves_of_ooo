using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI state for BrainPart's simple state machine.
    /// </summary>
    public enum AIState
    {
        Idle,
        Wander,
        Chase
    }

    /// <summary>
    /// AI Part that handles the TakeTurn event for NPC creatures.
    /// Mirrors Qud's Brain (simplified: state machine instead of goal stack).
    ///
    /// Behavior flow:
    /// 1. Scan for nearest hostile within SightRadius (with line-of-sight)
    /// 2. If hostile found and adjacent → melee attack
    /// 3. If hostile found and not adjacent → step toward it
    /// 4. If no hostile and Wanders → random movement
    /// 5. If no hostile and !Wanders → idle
    /// </summary>
    public class BrainPart : Part
    {
        public override string Name => "Brain";

        // Configuration (settable from blueprint params)
        public int SightRadius = 10;
        public bool Wanders = true;
        public bool WandersRandomly = true;

        // Runtime state
        public AIState CurrentState = AIState.Idle;
        public Entity Target;

        // Zone reference (set externally by GameBootstrap)
        public Zone CurrentZone;

        // RNG for AI decisions (injectable for deterministic testing)
        public Random Rng;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "TakeTurn")
                return HandleTakeTurn();
            return true;
        }

        private bool HandleTakeTurn()
        {
            // Guard: no zone or not in zone (dead/removed)
            if (CurrentZone == null) return true;
            if (CurrentZone.GetEntityCell(ParentEntity) == null) return true;

            // Safety: skip player entities (TurnManager shouldn't fire TakeTurn on player, but just in case)
            if (ParentEntity.HasTag("Player")) return true;

            // Ensure RNG exists
            if (Rng == null) Rng = new Random();

            // Clear dead/removed target
            if (Target != null)
            {
                if (CurrentZone.GetEntityCell(Target) == null)
                    Target = null;
            }

            // Scan for hostile target
            Entity newTarget = AIHelpers.FindNearestHostile(ParentEntity, CurrentZone, SightRadius);
            if (newTarget != null)
                Target = newTarget;

            if (Target != null)
            {
                CurrentState = AIState.Chase;

                var myPos = CurrentZone.GetEntityPosition(ParentEntity);
                var targetPos = CurrentZone.GetEntityPosition(Target);

                if (AIHelpers.IsAdjacent(myPos.x, myPos.y, targetPos.x, targetPos.y))
                {
                    // Adjacent — attack!
                    CombatSystem.PerformMeleeAttack(ParentEntity, Target, CurrentZone, Rng);
                }
                else
                {
                    // Chase — greedy step toward target with fallback
                    StepTowardTarget(myPos.x, myPos.y, targetPos.x, targetPos.y);
                }
            }
            else if (Wanders && WandersRandomly)
            {
                CurrentState = AIState.Wander;
                var (dx, dy) = AIHelpers.RandomPassableDirection(ParentEntity, CurrentZone, Rng);
                if (dx != 0 || dy != 0)
                    MovementSystem.TryMove(ParentEntity, CurrentZone, dx, dy);
            }
            else
            {
                CurrentState = AIState.Idle;
            }

            return true;
        }

        /// <summary>
        /// Greedy step toward target. Tries the ideal diagonal/cardinal direction first,
        /// then falls back to the two closest alternative directions.
        /// </summary>
        private void StepTowardTarget(int myX, int myY, int targetX, int targetY)
        {
            var (dx, dy) = AIHelpers.StepToward(myX, myY, targetX, targetY);

            // Try ideal direction first
            if (MovementSystem.TryMove(ParentEntity, CurrentZone, dx, dy))
                return;

            // If diagonal, try the two cardinal components
            if (dx != 0 && dy != 0)
            {
                if (MovementSystem.TryMove(ParentEntity, CurrentZone, dx, 0))
                    return;
                if (MovementSystem.TryMove(ParentEntity, CurrentZone, 0, dy))
                    return;
            }
            else if (dx != 0)
            {
                // Horizontal blocked, try diagonals
                if (MovementSystem.TryMove(ParentEntity, CurrentZone, dx, 1))
                    return;
                if (MovementSystem.TryMove(ParentEntity, CurrentZone, dx, -1))
                    return;
            }
            else if (dy != 0)
            {
                // Vertical blocked, try diagonals
                if (MovementSystem.TryMove(ParentEntity, CurrentZone, 1, dy))
                    return;
                if (MovementSystem.TryMove(ParentEntity, CurrentZone, -1, dy))
                    return;
            }
            // All directions blocked — do nothing
        }
    }
}
