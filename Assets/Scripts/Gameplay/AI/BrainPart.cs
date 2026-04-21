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

        /// <summary>
        /// Passive creatures do not proactively initiate combat. They'll still defend
        /// themselves against entities in <see cref="PersonalEnemies"/> (populated when
        /// they're directly attacked), and they'll still flee when HP drops below
        /// <see cref="FleeThreshold"/> — but a Passive scholar won't chase a snapjaw
        /// across the zone just because it walked into sight.
        /// Mirrors Qud's Brain.Passive flag. Used by non-combat NPCs (scholars, clerics,
        /// civilians, wildlife that doesn't hunt).
        ///
        /// Semantics in <c>BoredGoal.TakeAction</c>:
        ///   <c>canInitiate = !Passive || IsPersonallyHostileTo(hostile)</c>
        /// Engagement happens when <c>canInitiate || ShouldFlee()</c>.
        /// </summary>
        public bool Passive = false;

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

        // --- Debug Introspection (Phase 10) ---

        /// <summary>
        /// Most recent thought string set by a goal handler via <see cref="Think"/>.
        /// Null until first call. Surfaces in the AI goal-stack inspector UI when
        /// <see cref="CavesOfOoo.Diagnostics.AIDebug.AIInspectorEnabled"/> is true.
        /// Mirrors Qud's <c>Brain.LastThought</c> — a single slot, not a history buffer.
        /// </summary>
        public string LastThought;

        /// <summary>
        /// When true, every <see cref="Think"/> call also emits a
        /// <c>[Think:{entityName}] {thought}</c> line to
        /// <see cref="UnityEngine.Debug.Log"/>. Default false so production builds
        /// are silent. Set per-entity (e.g. from a scenario) to stream a specific
        /// NPC's reasoning without spamming every creature's thoughts.
        /// </summary>
        public bool ThinkOutLoud;

        /// <summary>
        /// Record a debug thought for this NPC's current tick. Mirrors Qud's
        /// <c>Brain.Think(string)</c>: single-slot <see cref="LastThought"/>
        /// assignment, plus optional <see cref="UnityEngine.Debug.Log"/> echo
        /// when <see cref="ThinkOutLoud"/> is on.
        ///
        /// Safe to call on every goal tick — the no-echo path is a single
        /// field write with no allocation. The expensive interpolation for
        /// the Debug.Log format sits behind the <see cref="ThinkOutLoud"/>
        /// gate so it costs nothing in the common case.
        ///
        /// Goals should call this at BRANCH POINTS (phase changes, gate passes,
        /// bailouts), not inside tight per-frame loops — that would allocate
        /// every tick if the caller interpolated <c>$"hp is {hp}"</c>.
        /// </summary>
        public void Think(string thought)
        {
            LastThought = thought;
            if (ThinkOutLoud && thought != null)
            {
                string name = ParentEntity?.GetDisplayName() ?? "?";
                UnityEngine.Debug.Log($"[Think:{name}] {thought}");
            }
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

        /// <summary>
        /// Peek at a specific stack index without removing. Index 0 is the
        /// BOTTOM (oldest / root — typically BoredGoal); <c>GoalCount - 1</c>
        /// is the TOP (innermost / currently executing). Returns null if
        /// index is out of range. Used by the Phase 10 goal-stack inspector
        /// UI to render the whole chain without exposing the backing list.
        /// </summary>
        public GoalHandler PeekGoalAt(int index)
        {
            return (index >= 0 && index < _goals.Count) ? _goals[index] : null;
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

        /// <summary>
        /// Check if any goal on the stack has a type whose class name equals typeName.
        /// Mirrors Qud's Brain.HasGoal(string) — used by behavior parts to gate
        /// "am I already doing X?" (e.g. "TurretTinker only places a turret if !HasGoal('PlaceTurretGoal')").
        /// </summary>
        public bool HasGoal(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            for (int i = 0; i < _goals.Count; i++)
            {
                if (_goals[i].GetType().Name == typeName) return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieve the first (topmost) goal of type T on the stack, or null.
        /// Mirrors Qud's Brain.FindGoal pattern. Returns null if no matching goal exists.
        /// Scans top-down so the most recent goal of that type wins.
        /// </summary>
        public T FindGoal<T>() where T : GoalHandler
        {
            for (int i = _goals.Count - 1; i >= 0; i--)
            {
                if (_goals[i] is T typed) return typed;
            }
            return null;
        }

        /// <summary>
        /// Retrieve the first (topmost) goal whose class name equals typeName, or null.
        /// String variant — mirrors Qud's Brain.FindGoal(string) used by ModPsionic
        /// to find the Kill goal and insert Reequip above it.
        /// </summary>
        public GoalHandler FindGoal(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            for (int i = _goals.Count - 1; i >= 0; i--)
            {
                if (_goals[i].GetType().Name == typeName) return _goals[i];
            }
            return null;
        }

        /// <summary>
        /// True if the stack has any goal whose class name is NOT the given typeName.
        /// Useful for "act only if idle" checks that want to exclude a specific
        /// background goal (typically BoredGoal). Mirrors Qud's HasGoalOtherThan(name).
        /// </summary>
        public bool HasGoalOtherThan(string typeName)
        {
            for (int i = 0; i < _goals.Count; i++)
            {
                if (_goals[i].GetType().Name != typeName) return true;
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
