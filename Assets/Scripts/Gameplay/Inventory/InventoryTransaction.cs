using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CavesOfOoo.Diagnostics;

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
        private static readonly ConditionalWeakTable<Entity, InventoryTransaction> ActiveTransfers
            = new ConditionalWeakTable<Entity, InventoryTransaction>();
        private readonly List<Entity> _claimedItems = new List<Entity>();

        /// <summary>Protect a participating item until commit/rollback. Direct nested
        /// commands sharing this transaction remain allowed; separate reentrant commands
        /// on that item refuse. Independent items may still transfer.</summary>
        internal bool TryClaim(Entity item, Entity actor, string action)
        {
            if (item == null || _completed) return false;
            if (ActiveTransfers.TryGetValue(item, out var owner))
            {
                if (ReferenceEquals(owner, this)) return true;
                Diag.Record("event", "InventoryTransferRejected", actor: actor, target: item,
                    payload: new { action, reason = "transfer_in_progress" });
                return false;
            }
            ActiveTransfers.Add(item, this); _claimedItems.Add(item); return true;
        }
        private void ReleaseClaims()
        {
            foreach (var item in _claimedItems) ActiveTransfers.Remove(item);
            _claimedItems.Clear();
        }

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
            ReleaseClaims();
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
            ReleaseClaims();
            _completed = true;
            IsCommitted = false;
            IsRolledBack = true;
        }
    }
}
