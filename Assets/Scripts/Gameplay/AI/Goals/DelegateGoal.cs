using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that executes an arbitrary callback once, then finishes.
    /// Used for "do X when you arrive" patterns (sit in chair, lie in bed).
    /// Mirrors Qud's DelegateGoal.
    ///
    /// Supports two modes:
    /// 1. Unconditional: action runs whenever the goal is executed.
    /// 2. Position-gated: action only runs if the entity is at (RequireX, RequireY).
    ///    If not, the optional Cleanup callback runs instead (used to roll back
    ///    reservations like a chair's Occupied flag when a sibling MoveToGoal fails).
    ///
    /// Cleanup also runs via OnPop if the goal is popped without ever executing
    /// (e.g., ClearGoals, entity death), ensuring reservations are always released.
    /// </summary>
    public class DelegateGoal : GoalHandler
    {
        private readonly Action<GoalHandler> _action;
        private readonly Action<GoalHandler> _cleanup;
        private readonly int _requireX;
        private readonly int _requireY;
        private readonly bool _hasRequirement;
        private bool _executed;

        /// <summary>
        /// Unconditional delegate — runs whenever the goal is executed.
        /// </summary>
        public DelegateGoal(Action<GoalHandler> action)
        {
            _action = action;
            _hasRequirement = false;
        }

        /// <summary>
        /// Position-gated delegate with cleanup.
        /// If the entity is at (requireX, requireY) when the goal executes, action runs.
        /// Otherwise (e.g., sibling MoveToGoal failed), cleanup runs and action is skipped.
        /// Cleanup also runs on OnPop if the goal is abandoned without executing.
        /// </summary>
        public DelegateGoal(Action<GoalHandler> action, Action<GoalHandler> cleanup, int requireX, int requireY)
        {
            _action = action;
            _cleanup = cleanup;
            _requireX = requireX;
            _requireY = requireY;
            _hasRequirement = true;
        }

        public override bool Finished() => _executed;

        public override void TakeAction()
        {
            if (_hasRequirement)
            {
                var pos = CurrentZone?.GetEntityPosition(ParentEntity) ?? (-1, -1);
                if (pos.x != _requireX || pos.y != _requireY)
                {
                    // Wrong position — sibling move failed. Run cleanup, don't execute action.
                    _cleanup?.Invoke(this);
                    _executed = true;
                    return;
                }
            }

            _action?.Invoke(this);
            _executed = true;
        }

        public override void OnPop()
        {
            // Safety net: if popped without ever executing (stack cleared, entity died),
            // run cleanup to release any reservations.
            if (!_executed && _cleanup != null)
            {
                _cleanup.Invoke(this);
                _executed = true;
            }
        }
    }
}
