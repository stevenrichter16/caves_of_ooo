using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI state — informational, set by goals for backward compatibility.
    /// </summary>
    public enum AIState
    {
        Idle,
        Wander,
        Chase
    }

    /// <summary>
    /// AI Part that handles the TakeTurn event for NPC creatures.
    /// Mirrors Qud's Brain: owns a goal stack that drives behavior each tick.
    ///
    /// The goal stack is a LIFO list of GoalHandler objects. Each tick:
    /// 1. Finished goals are popped from the top
    /// 2. If the stack is empty, a BoredGoal is pushed as the default
    /// 3. The top goal's TakeAction() is called
    /// 4. If TakeAction pushed a child, the child executes immediately too
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

        // --- Starting Cell / Home ---

        /// <summary>Cell where this NPC was first placed. Used by BoredGoal to return home.</summary>
        public int StartingCellX = -1;
        public int StartingCellY = -1;
        public bool HasStartingCell => StartingCellX >= 0 && StartingCellY >= 0;

        /// <summary>When true, NPC returns to StartingCell when idle instead of wandering.</summary>
        public bool Staying = false;

        /// <summary>Set the NPC's home cell and enable Staying behavior.</summary>
        public void Stay(int x, int y)
        {
            StartingCellX = x;
            StartingCellY = y;
            Staying = true;
        }

        // --- Goal Stack ---

        private List<GoalHandler> _goals = new List<GoalHandler>();

        private const int MaxChildChainDepth = 10;

        /// <summary>Number of goals on the stack.</summary>
        public int GoalCount => _goals.Count;

        /// <summary>Peek at the top goal without removing it. Returns null if empty.</summary>
        public GoalHandler PeekGoal()
        {
            return _goals.Count > 0 ? _goals[_goals.Count - 1] : null;
        }

        /// <summary>Push a goal onto the top of the stack.</summary>
        public void PushGoal(GoalHandler goal)
        {
            goal.ParentBrain = this;
            _goals.Add(goal);
            goal.OnPush();
        }

        /// <summary>Remove a specific goal from the stack.</summary>
        public void RemoveGoal(GoalHandler goal)
        {
            if (_goals.Remove(goal))
                goal.OnPop();
        }

        /// <summary>Clear all goals from the stack.</summary>
        public void ClearGoals()
        {
            for (int i = _goals.Count - 1; i >= 0; i--)
                _goals[i].OnPop();
            _goals.Clear();
        }

        /// <summary>Check if any goal of type T is on the stack.</summary>
        public bool HasGoal<T>() where T : GoalHandler
        {
            for (int i = 0; i < _goals.Count; i++)
            {
                if (_goals[i] is T) return true;
            }
            return false;
        }

        // --- Event Handling ---

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "TakeTurn")
                return HandleTakeTurn();
            return true;
        }

        private bool HandleTakeTurn()
        {
            using (PerformanceMarkers.Turns.AiTakeTurn.Auto())
            {
                // Guard: no zone or not in zone (dead/removed)
                if (CurrentZone == null) return true;
                if (CurrentZone.GetEntityCell(ParentEntity) == null) return true;

                // Skip turn when in conversation
                if (InConversation) return true;

                // Safety: skip player entities
                if (ParentEntity.HasTag("Player")) return true;

                // Ensure RNG exists
                if (Rng == null) Rng = new Random();

                // Clear dead/removed target
                if (Target != null)
                {
                    if (CurrentZone.GetEntityCell(Target) == null)
                        Target = null;
                }

                // Set starting cell on first turn if not already set
                if (!HasStartingCell)
                {
                    var pos = CurrentZone.GetEntityPosition(ParentEntity);
                    if (pos.x >= 0)
                    {
                        StartingCellX = pos.x;
                        StartingCellY = pos.y;
                    }
                }

                // Clean finished goals from top of stack
                while (_goals.Count > 0 && _goals[_goals.Count - 1].Finished())
                {
                    var done = _goals[_goals.Count - 1];
                    _goals.RemoveAt(_goals.Count - 1);
                    done.OnPop();
                }

                // Ensure default goal exists
                if (_goals.Count == 0)
                    PushGoal(new BoredGoal());

                // Increment age on all goals
                for (int i = 0; i < _goals.Count; i++)
                    _goals[i].Age++;

                // Execute top goal
                int stackSize = _goals.Count;
                _goals[stackSize - 1].TakeAction();

                // Child-chain execution: if TakeAction pushed a child, execute it immediately.
                // This ensures BoredGoal -> KillGoal -> attack all happen in one tick.
                int depth = 0;
                while (_goals.Count > stackSize && depth < MaxChildChainDepth)
                {
                    depth++;
                    // Clean any immediately-finished goals
                    while (_goals.Count > 0 && _goals[_goals.Count - 1].Finished())
                    {
                        var done = _goals[_goals.Count - 1];
                        _goals.RemoveAt(_goals.Count - 1);
                        done.OnPop();
                    }

                    if (_goals.Count <= stackSize) break;

                    stackSize = _goals.Count;
                    _goals[stackSize - 1].TakeAction();
                }

                return true;
            }
        }
    }
}
