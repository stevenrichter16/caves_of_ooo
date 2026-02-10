using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Container for carried and equipped items. Mirrors Qud's Inventory part.
    /// Items in Objects have been removed from the zone.
    /// Items in EquippedItems are in a named slot (Hand, Body, etc).
    /// PhysicsPart.InInventory/Equipped on items point back to the owner.
    /// </summary>
    public class InventoryPart : Part
    {
        public override string Name => "Inventory";

        /// <summary>
        /// Maximum carry weight. -1 means no limit.
        /// </summary>
        public int MaxWeight = -1;

        /// <summary>
        /// Items currently carried (not equipped).
        /// </summary>
        public List<Entity> Objects = new List<Entity>();

        /// <summary>
        /// Items currently equipped, keyed by slot name.
        /// </summary>
        public Dictionary<string, Entity> EquippedItems = new Dictionary<string, Entity>();

        /// <summary>
        /// Add an item to the carried list. Sets InInventory back-reference.
        /// Returns false if weight would be exceeded.
        /// </summary>
        public bool AddObject(Entity item)
        {
            if (MaxWeight >= 0)
            {
                var itemPhysics = item.GetPart<PhysicsPart>();
                int itemWeight = itemPhysics?.Weight ?? 0;
                if (GetCarriedWeight() + itemWeight > MaxWeight)
                    return false;
            }
            Objects.Add(item);
            var physics = item.GetPart<PhysicsPart>();
            if (physics != null)
            {
                physics.InInventory = ParentEntity;
                physics.Equipped = null;
            }
            return true;
        }

        /// <summary>
        /// Remove an item from the carried list. Clears InInventory.
        /// </summary>
        public bool RemoveObject(Entity item)
        {
            if (!Objects.Remove(item))
                return false;
            var physics = item.GetPart<PhysicsPart>();
            if (physics != null)
                physics.InInventory = null;
            return true;
        }

        /// <summary>
        /// Equip an item to a slot. Removes from carried list if present.
        /// Sets Equipped back-reference, clears InInventory.
        /// </summary>
        public bool Equip(Entity item, string slot)
        {
            Objects.Remove(item);
            EquippedItems[slot] = item;

            var physics = item.GetPart<PhysicsPart>();
            if (physics != null)
            {
                physics.InInventory = null;
                physics.Equipped = ParentEntity;
            }
            return true;
        }

        /// <summary>
        /// Unequip an item from a slot. Moves back to carried list.
        /// </summary>
        public bool Unequip(string slot)
        {
            if (!EquippedItems.TryGetValue(slot, out Entity item))
                return false;
            EquippedItems.Remove(slot);

            var physics = item.GetPart<PhysicsPart>();
            if (physics != null)
            {
                physics.Equipped = null;
                physics.InInventory = ParentEntity;
            }
            Objects.Add(item);
            return true;
        }

        /// <summary>
        /// Get the item equipped in a slot, or null.
        /// </summary>
        public Entity GetEquipped(string slot)
        {
            EquippedItems.TryGetValue(slot, out Entity item);
            return item;
        }

        /// <summary>
        /// Get the first equipped item that has a specific Part type.
        /// Used by CombatSystem to find equipped MeleeWeaponPart or ArmorPart.
        /// </summary>
        public Entity GetEquippedWithPart<T>() where T : Part
        {
            foreach (var kvp in EquippedItems)
            {
                if (kvp.Value.HasPart<T>())
                    return kvp.Value;
            }
            return null;
        }

        /// <summary>
        /// Total weight of all carried + equipped items.
        /// </summary>
        public int GetCarriedWeight()
        {
            int total = 0;
            for (int i = 0; i < Objects.Count; i++)
            {
                var p = Objects[i].GetPart<PhysicsPart>();
                total += p?.Weight ?? 0;
            }
            foreach (var kvp in EquippedItems)
            {
                var p = kvp.Value.GetPart<PhysicsPart>();
                total += p?.Weight ?? 0;
            }
            return total;
        }

        /// <summary>
        /// Check if an entity is in inventory (carried or equipped).
        /// </summary>
        public bool Contains(Entity item)
        {
            if (Objects.Contains(item)) return true;
            foreach (var kvp in EquippedItems)
            {
                if (kvp.Value == item) return true;
            }
            return false;
        }
    }
}