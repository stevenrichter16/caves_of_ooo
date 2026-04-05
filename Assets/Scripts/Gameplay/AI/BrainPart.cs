using System;
using System.Collections.Generic;

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
        public float FleeThreshold = 0.25f;

        // Runtime state
        public AIState CurrentState = AIState.Idle;
        public Entity Target;
        public bool InConversation;

        /// <summary>
        /// Entities this NPC is personally hostile toward, independent of faction.
        /// Mirrors Qud's per-NPC opinion system (simplified: permanent hostility).
        /// </summary>
        public HashSet<Entity> PersonalEnemies = new HashSet<Entity>();

        public void SetPersonallyHostile(Entity target)
        {
            if (target == null) return;
            bool wasNew = PersonalEnemies.Add(target);
            Target = target;
            InConversation = false;

            if (wasNew && CurrentZone != null)
            {
                var myPos = CurrentZone.GetEntityPosition(ParentEntity);
                if (myPos.x >= 0)
                    AsciiFxBus.EmitParticle(CurrentZone, myPos.x, myPos.y - 1, '!', "&R", 0.25f);
            }
        }

        public bool IsPersonallyHostileTo(Entity target)
        {
            return target != null && PersonalEnemies.Contains(target);
        }

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

            // Skip turn when in conversation
            if (InConversation) return true;

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
            {
                bool firstAggro = Target == null;
                Target = newTarget;

                // Aggro indicator: red ! above creature on first detection
                if (firstAggro)
                {
                    var myPos = CurrentZone.GetEntityPosition(ParentEntity);
                    if (myPos.x >= 0)
                        AsciiFxBus.EmitParticle(CurrentZone, myPos.x, myPos.y - 1, '!', "&R", 0.25f);
                }
            }

            if (Target != null)
            {
                CurrentState = AIState.Chase;

                var myPos = CurrentZone.GetEntityPosition(ParentEntity);
                var targetPos = CurrentZone.GetEntityPosition(Target);

                // Flee if below HP threshold
                int hp = ParentEntity.GetStatValue("Hitpoints", 1);
                int maxHp = ParentEntity.GetStat("Hitpoints")?.Max ?? 1;
                bool shouldFlee = hp > 0 && maxHp > 0 && (float)hp / maxHp < FleeThreshold;

                if (shouldFlee)
                {
                    StepAwayFromTarget(myPos.x, myPos.y, targetPos.x, targetPos.y);
                }
                else if (AIHelpers.IsAdjacent(myPos.x, myPos.y, targetPos.x, targetPos.y))
                {
                    // Adjacent — melee attack
                    CombatSystem.PerformMeleeAttack(ParentEntity, Target, CurrentZone, Rng);
                }
                else
                {
                    // Try ranged ability before chasing
                    if (!TryUseRangedAbility(myPos, targetPos))
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

        private void StepAwayFromTarget(int myX, int myY, int targetX, int targetY)
        {
            var (dx, dy) = AIHelpers.StepAway(myX, myY, targetX, targetY);

            if (MovementSystem.TryMove(ParentEntity, CurrentZone, dx, dy))
                return;

            if (dx != 0 && dy != 0)
            {
                if (MovementSystem.TryMove(ParentEntity, CurrentZone, dx, 0))
                    return;
                if (MovementSystem.TryMove(ParentEntity, CurrentZone, 0, dy))
                    return;
            }

            // Cornered — fight back if adjacent
            if (Target != null && AIHelpers.IsAdjacent(myX, myY, targetX, targetY))
                CombatSystem.PerformMeleeAttack(ParentEntity, Target, CurrentZone, Rng ?? new Random());
        }

        private bool TryUseRangedAbility((int x, int y) myPos, (int x, int y) targetPos)
        {
            var abilities = ParentEntity.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null) return false;

            int dist = AIHelpers.ChebyshevDistance(myPos.x, myPos.y, targetPos.x, targetPos.y);

            for (int i = 0; i < abilities.AbilityList.Count; i++)
            {
                var ability = abilities.AbilityList[i];
                if (!ability.IsUsable) continue;
                if (ability.TargetingMode == AbilityTargetingMode.AdjacentCell) continue;
                if (ability.Range < dist && ability.TargetingMode != AbilityTargetingMode.SelfCentered)
                    continue;

                var cmdEvent = GameEvent.New(ability.Command);
                cmdEvent.SetParameter("Zone", (object)CurrentZone);
                cmdEvent.SetParameter("RNG", (object)(Rng ?? new Random()));

                if (ability.TargetingMode == AbilityTargetingMode.DirectionLine)
                {
                    var sourceCell = CurrentZone.GetCell(myPos.x, myPos.y);
                    var (dx, dy) = AIHelpers.StepToward(myPos.x, myPos.y, targetPos.x, targetPos.y);
                    cmdEvent.SetParameter("SourceCell", (object)sourceCell);
                    cmdEvent.SetParameter("DirectionX", dx);
                    cmdEvent.SetParameter("DirectionY", dy);
                }
                else if (ability.TargetingMode == AbilityTargetingMode.SelfCentered)
                {
                    cmdEvent.SetParameter("SourceCell", (object)CurrentZone.GetCell(myPos.x, myPos.y));
                }

                ParentEntity.FireEvent(cmdEvent);
                if (cmdEvent.Handled) return true;
            }
            return false;
        }
    }
}
