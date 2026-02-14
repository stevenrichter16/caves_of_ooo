using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core.Inventory
{
    /// <summary>
    /// Minimal transaction primitive for inventory command execution.
    /// Apply changes in order, and rollback in reverse order if needed.
    /// </summary>
    public sealed class InventoryTransaction
    {
        private readonly List<Action> _undoActions = new List<Action>();
        private bool _completed;

        public bool IsCommitted { get; private set; }

        public bool IsRolledBack { get; private set; }

        public int StepCount => _undoActions.Count;

        public void Do(Action apply, Action undo)
        {
            if (_completed)
                throw new InvalidOperationException("Transaction is already complete.");

            apply?.Invoke();

            if (undo != null)
                _undoActions.Add(undo);
        }

        public void Commit()
        {
            if (_completed)
                return;

            _undoActions.Clear();
            _completed = true;
            IsCommitted = true;
            IsRolledBack = false;
        }

        public void Rollback()
        {
            if (_completed)
                return;

            for (int i = _undoActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _undoActions[i]?.Invoke();
                }
                catch
                {
                    // Best-effort rollback. Continue attempting remaining steps.
                }
            }

            _undoActions.Clear();
            _completed = true;
            IsCommitted = false;
            IsRolledBack = true;
        }
    }
}
