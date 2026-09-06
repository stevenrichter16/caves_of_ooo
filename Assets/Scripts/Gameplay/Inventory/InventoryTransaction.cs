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
        private readonly Dictionary<Entity, long> _currencyDeltas = new Dictionary<Entity, long>();

        /// <summary>Queue a positive pickup credit. It is unavailable to independent
        /// spending until commit; rollback discards it without rewinding other payments.</summary>
        internal void DeferCurrencyCredit(Entity actor, int amount) => AddCurrencyDelta(actor, amount);

        /// <summary>Queue both sides of a purchase/sale. Commit publishes all resulting
        /// purses before notifying observers, so no listener sees half a payment.</summary>
        internal void DeferCurrencyTransfer(Entity payer, Entity receiver, int amount)
        {
            AddCurrencyDelta(payer, -(long)amount);
            AddCurrencyDelta(receiver, amount);
        }
        private void AddCurrencyDelta(Entity actor, long amount)
        {
            if (_completed) throw new InvalidOperationException("Transaction is already complete.");
            _currencyDeltas.TryGetValue(actor, out long pending);
            _currencyDeltas[actor] = pending + amount;
        }

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

        /// <summary>Validate queued wallet changes, then commit. A refused wallet
        /// change throws before payment; callers must Rollback (the executor does).
        /// Property notifications occur after commit and cannot undo committed items.</summary>
        public void Commit()
        {
            if (_completed) return;
            List<CurrencyChange> changes = null;
            if (_currencyDeltas.Count > 0)
            {
                changes = new List<CurrencyChange>(_currencyDeltas.Count);
                foreach (var delta in _currencyDeltas)
                {
                    int before = TradeSystem.GetDrams(delta.Key);
                    long after = before + delta.Value;
                    if (after < 0 || after > int.MaxValue)
                    {
                        Diag.Record("event", delta.Value < 0 ? "CurrencyDebitRejected" : "CurrencyCreditRejected",
                            actor: delta.Key, target: delta.Key,
                            payload: new { amount = delta.Value, reason = after < 0 ? "insufficient_currency" : "currency_overflow" });
                        throw new InvalidOperationException(after < 0 ? "There is not enough currency." : "You cannot carry that much currency.");
                    }
                    if (after != before) changes.Add(new CurrencyChange(delta.Key, before, (int)after));
                }
                // SetIntProperty notifies synchronously. Write the validated batch
                // first; preserve its normal notification shape below after commit.
                foreach (var change in changes)
                    change.Actor.IntProperties[TradeSystem.CURRENCY_PROP] = change.After;
            }
            _currencyDeltas.Clear();
            _undoActions.Clear();
            ReleaseClaims();
            _completed = true;
            IsCommitted = true;
            IsRolledBack = false;
            if (changes == null) return;
            foreach (var change in changes)
            {
                Diag.Record("event", change.After > change.Before ? "CurrencyCreditApplied" : "CurrencyDebitApplied",
                    actor: change.Actor, target: change.Actor,
                    payload: new { amount = (long)change.After - change.Before, dramsAfter = change.After });
                var notification = GameEvent.New("IntPropertyChanged");
                notification.SetParameter("Name", TradeSystem.CURRENCY_PROP);
                notification.SetParameter("OldValue", change.Before); notification.SetParameter("NewValue", change.After);
                try { change.Actor.FireEvent(notification); }
                catch (Exception error)
                {
                    // A post-commit observer cannot turn paid goods into a failed
                    // acquisition or stop another recipient receiving its notification.
                    Diag.Record("event", "CurrencyObserverFailed", actor: change.Actor, target: change.Actor,
                        payload: new { property = TradeSystem.CURRENCY_PROP, error = error.GetType().Name, message = error.Message });
                }
                finally { notification.Release(); }
            }
        }
        private readonly struct CurrencyChange
        {
            internal readonly Entity Actor;
            internal readonly int Before, After;
            internal CurrencyChange(Entity actor, int before, int after) { Actor = actor; Before = before; After = after; }
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
            _currencyDeltas.Clear();
            ReleaseClaims();
            _completed = true;
            IsCommitted = false;
            IsRolledBack = true;
        }
    }
}
