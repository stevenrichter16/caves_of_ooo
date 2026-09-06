using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    // One iteration joins its command's transaction. A normal refusal restores only
    // this iteration; an exception keeps undo armed for the parent's reverse-order
    // rollback, including successful nested commands and earlier batch iterations.
    internal sealed class WeaponCraftingOperation
    {
        private readonly List<Action> _undo = new List<Action>();
        private readonly InventoryTransaction _transaction;
        private readonly InventoryPart _inventory;
        private readonly Entity _actor;
        private readonly string _action;
        private readonly int _weightCeiling;
        private bool _armed;

        internal WeaponCraftingOperation(Entity actor, InventoryTransaction transaction, string action)
        {
            _actor = actor; _inventory = actor.GetPart<InventoryPart>();
            _transaction = transaction; _action = action;
            _weightCeiling = _inventory.MaxWeight < 0 ? int.MaxValue
                : Math.Max(_inventory.MaxWeight, _inventory.GetCarriedWeight());
        }
        internal bool Claim(params Entity[] items)
        {
            foreach (var item in items) if (!_transaction.TryClaim(item, _actor, _action)) return false;
            return true;
        }
        internal InventoryTransferSnapshot Capture(params Entity[] incoming)
        {
            var receipt = InventoryTransferSnapshot.Capture(_inventory, incoming);
            Undo(receipt.Restore);
            return receipt;
        }
        internal void Undo(Action undo)
        {
            // Preparation may complete earlier work in this same transaction.
            // Enroll only now, immediately before our first mutation, so rollback
            // follows actual mutation order rather than operation construction order.
            if (!_armed) { _transaction.Do(null, Restore); _armed = true; }
            _undo.Add(undo);
        }
        internal void CapturePayload(Entity weapon) => Undo(WeaponPayloadUndo.Capture(weapon));
        internal bool Finish(InventoryTransferSnapshot receipt) =>
            receipt.ClaimChanges(_transaction, _actor, _action)
            && _inventory.GetCarriedWeight() <= _weightCeiling;
        internal void Restore()
        {
            try
            {
                for (int i = _undo.Count - 1; i >= 0; i--)
                    try { _undo[i](); }
                    catch (Exception error)
                    {
                        CavesOfOoo.Diagnostics.Diag.Record("event", "WeaponCraftingRollbackFailed", actor: _actor,
                            payload: new { action = _action, error = error.GetType().Name, message = error.Message });
                    }
            }
            finally { _undo.Clear(); }
        }
        internal static bool CanTransform(InventoryPart inventory, Entity weapon) =>
            weapon != null && inventory.Contains(weapon)
            && (weapon.GetPart<StackerPart>()?.StackCount ?? 1) > 0
            && ((weapon.GetPart<StackerPart>()?.StackCount ?? 1) == 1
                || (inventory.Objects.Contains(weapon) && !inventory.GetAllEquipped().Contains(weapon)));

        internal static Entity PrepareUnit(Entity weapon)
        {
            if ((weapon.GetPart<StackerPart>()?.StackCount ?? 1) == 1) return weapon;
            Entity unit = weapon.CloneForStack();
            unit.GetPart<StackerPart>().StackCount = 1;
            return unit;
        }
        internal void MoveMark(Entity source, Entity recipient)
        {
            if (ReferenceEquals(source, recipient) || !CraftingMarkPart.IsMarked(source)) return;
            CaptureMark(source); CaptureMark(recipient);
            source.RemovePart(source.GetPart<CraftingMarkPart>());
            if (!CraftingMarkPart.IsMarked(recipient)) recipient.AddPart(new CraftingMarkPart());
        }
        private void CaptureMark(Entity item)
        {
            var original = item.GetPart<CraftingMarkPart>();
            int index = original == null ? -1 : item.Parts.IndexOf(original);
            Undo(() =>
            {
                var current = item.GetPart<CraftingMarkPart>();
                if (ReferenceEquals(current, original)) return;
                if (current != null) item.RemovePart(current);
                if (original != null)
                {
                    item.AddPart(original);
                    item.Parts.Remove(original);
                    item.Parts.Insert(Math.Min(index, item.Parts.Count), original);
                }
            });
        }
    }
}
