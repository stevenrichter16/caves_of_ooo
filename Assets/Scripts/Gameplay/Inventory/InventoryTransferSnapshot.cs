using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core.Inventory
{
    /// <summary>Short-lived item-list transfer receipt. Apply seals only the entries
    /// actually changed by that immediate mutation, so rollback preserves independent
    /// later work on other items. Item payloads/effects are outside this contract.</summary>
    internal sealed class InventoryTransferSnapshot
    {
        private readonly List<Entity> _list;
        private readonly InventoryPart _inventory;
        private readonly List<ItemState> _before = new List<ItemState>();
        private readonly List<ItemState> _changed = new List<ItemState>();
        internal static InventoryTransferSnapshot Capture(InventoryPart inventory, params Entity[] incoming) =>
            new InventoryTransferSnapshot(inventory.Objects, inventory, incoming);
        internal static InventoryTransferSnapshot Capture(ContainerPart container, params Entity[] incoming) =>
            new InventoryTransferSnapshot(container.Contents, null, incoming);
        private InventoryTransferSnapshot(List<Entity> list, InventoryPart inventory, Entity[] incoming)
        {
            _list = list; _inventory = inventory;
            for (int i = 0; i < list.Count; i++) _before.Add(new ItemState(list[i], i));
            var seen = new HashSet<Entity>(list);
            if (incoming != null)
                foreach (var item in incoming)
                    if (item != null && seen.Add(item)) _before.Add(new ItemState(item, -1));
        }
        internal bool Apply(Func<bool> mutation)
        {
            try { return mutation(); }
            finally
            {
                _changed.Clear();
                foreach (var state in _before) if (state.HasChanged(_list)) _changed.Add(state);
            }
        }
        // A merge can also change preexisting destination stacks. Protect them before
        // exposing callbacks; an independently active transfer causes this mutation to undo.
        internal bool ClaimChanges(InventoryTransaction transaction, Entity actor, string action)
        {
            foreach (var state in _changed)
                if (!transaction.TryClaim(state.Item, actor, action)) return false;
            return true;
        }
        internal void Restore()
        {
            foreach (var state in _changed) if (state.Index < 0) _list.Remove(state.Item);
            foreach (var state in _changed)
            {
                if (state.Index >= 0 && !_list.Contains(state.Item))
                    _list.Insert(Math.Min(state.Index, _list.Count), state.Item);
                state.Restore();
            }
            _inventory?.RefreshHandlingCarryPenalty();
        }
        private readonly struct ItemState
        {
            internal readonly Entity Item;
            internal readonly int Index;
            private readonly StackerPart _stacker;
            private readonly int _count;
            private readonly PhysicsPart _physics;
            private readonly Entity _carriedBy, _equippedBy;
            internal ItemState(Entity item, int index)
            {
                Item = item; Index = index; _stacker = item?.GetPart<StackerPart>(); _count = _stacker?.StackCount ?? 1;
                _physics = item?.GetPart<PhysicsPart>(); _carriedBy = _physics?.InInventory; _equippedBy = _physics?.Equipped;
            }
            internal bool HasChanged(List<Entity> list) => Item != null &&
                ((Index >= 0) != list.Contains(Item) || (_stacker?.StackCount ?? 1) != _count
                || _physics?.InInventory != _carriedBy || _physics?.Equipped != _equippedBy);
            internal void Restore()
            {
                if (_stacker != null) _stacker.StackCount = _count;
                if (_physics != null) { _physics.InInventory = _carriedBy; _physics.Equipped = _equippedBy; }
            }
        }
    }
}
