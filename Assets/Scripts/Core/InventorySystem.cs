using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static system for inventory operations: pickup, drop, equip, unequip.
    /// Follows the pattern of MovementSystem and CombatSystem.
    /// All operations fire Before/After events and can be cancelled.
    /// </summary>
    public static class InventorySystem
    {
        /// <summary>
        /// Pick up an item from the zone into an actor's inventory.
        /// Returns true if successful, false if cancelled or failed.
        /// </summary>
        public static bool Pickup(Entity actor, Entity item, Zone zone)
        {
            if (actor == null || item == null || zone == null) return false;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            var itemPhysics = item.GetPart<PhysicsPart>();
            if (itemPhysics == null || !itemPhysics.Takeable) return false;

            // Fire BeforePickup on actor
            var beforePickup = GameEvent.New("BeforePickup");
            beforePickup.SetParameter("Actor", (object)actor);
            beforePickup.SetParameter("Item", (object)item);
            if (!actor.FireEvent(beforePickup))
                return false;

            // Fire BeforeBeingPickedUp on item
            var beforeBeing = GameEvent.New("BeforeBeingPickedUp");
            beforeBeing.SetParameter("Actor", (object)actor);
            beforeBeing.SetParameter("Item", (object)item);
            if (!item.FireEvent(beforeBeing))
                return false;

            // Remove from zone first, then try to add to inventory
            zone.RemoveEntity(item);

            if (!inventory.AddObject(item))
            {
                // Weight limit exceeded — put item back in zone
                var cell = zone.GetEntityCell(actor);
                if (cell != null)
                    zone.AddEntity(item, cell.X, cell.Y);
                MessageLog.Add($"You can't carry {item.GetDisplayName()}: too heavy!");
                return false;
            }

            MessageLog.Add($"{actor.GetDisplayName()} picks up {item.GetDisplayName()}.");

            // Fire AfterPickup
            var afterPickup = GameEvent.New("AfterPickup");
            afterPickup.SetParameter("Actor", (object)actor);
            afterPickup.SetParameter("Item", (object)item);
            actor.FireEvent(afterPickup);

            return true;
        }

        /// <summary>
        /// Drop an item from inventory onto the zone floor at the actor's position.
        /// Auto-unequips if the item is equipped.
        /// </summary>
        public static bool Drop(Entity actor, Entity item, Zone zone)
        {
            if (actor == null || item == null || zone == null) return false;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            // If item is equipped, find its slot and unequip first
            string equippedSlot = null;
            foreach (var kvp in inventory.EquippedItems)
            {
                if (kvp.Value == item)
                {
                    equippedSlot = kvp.Key;
                    break;
                }
            }
            if (equippedSlot != null)
            {
                if (!Unequip(actor, equippedSlot))
                    return false;
            }

            // Fire BeforeDrop
            var beforeDrop = GameEvent.New("BeforeDrop");
            beforeDrop.SetParameter("Actor", (object)actor);
            beforeDrop.SetParameter("Item", (object)item);
            if (!actor.FireEvent(beforeDrop))
                return false;

            if (!inventory.RemoveObject(item))
                return false;

            // Place in zone at actor's position
            var cell = zone.GetEntityCell(actor);
            if (cell != null)
                zone.AddEntity(item, cell.X, cell.Y);

            MessageLog.Add($"{actor.GetDisplayName()} drops {item.GetDisplayName()}.");

            // Fire AfterDrop
            var afterDrop = GameEvent.New("AfterDrop");
            afterDrop.SetParameter("Actor", (object)actor);
            afterDrop.SetParameter("Item", (object)item);
            actor.FireEvent(afterDrop);

            return true;
        }

        /// <summary>
        /// Equip an item from the actor's inventory to its designated slot.
        /// Auto-unequips whatever is currently in that slot.
        /// </summary>
        public static bool Equip(Entity actor, Entity item)
        {
            if (actor == null || item == null) return false;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            var equippable = item.GetPart<EquippablePart>();
            if (equippable == null) return false;

            string slot = equippable.Slot;

            // Fire BeforeEquip
            var beforeEquip = GameEvent.New("BeforeEquip");
            beforeEquip.SetParameter("Actor", (object)actor);
            beforeEquip.SetParameter("Item", (object)item);
            beforeEquip.SetParameter("Slot", slot);
            if (!actor.FireEvent(beforeEquip))
                return false;

            // Unequip existing item in that slot
            if (inventory.GetEquipped(slot) != null)
            {
                if (!Unequip(actor, slot))
                    return false;
            }

            inventory.Equip(item, slot);

            // Apply stat bonuses
            ApplyEquipBonuses(actor, equippable, true);

            MessageLog.Add($"{actor.GetDisplayName()} equips {item.GetDisplayName()}.");

            // Fire AfterEquip
            var afterEquip = GameEvent.New("AfterEquip");
            afterEquip.SetParameter("Actor", (object)actor);
            afterEquip.SetParameter("Item", (object)item);
            afterEquip.SetParameter("Slot", slot);
            actor.FireEvent(afterEquip);

            return true;
        }

        /// <summary>
        /// Unequip an item from a slot back to the actor's carried inventory.
        /// </summary>
        public static bool Unequip(Entity actor, string slot)
        {
            if (actor == null || string.IsNullOrEmpty(slot)) return false;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            var item = inventory.GetEquipped(slot);
            if (item == null) return false;

            var equippable = item.GetPart<EquippablePart>();

            // Fire BeforeUnequip
            var beforeUnequip = GameEvent.New("BeforeUnequip");
            beforeUnequip.SetParameter("Actor", (object)actor);
            beforeUnequip.SetParameter("Item", (object)item);
            beforeUnequip.SetParameter("Slot", slot);
            if (!actor.FireEvent(beforeUnequip))
                return false;

            // Remove stat bonuses
            if (equippable != null)
                ApplyEquipBonuses(actor, equippable, false);

            inventory.Unequip(slot);

            MessageLog.Add($"{actor.GetDisplayName()} unequips {item.GetDisplayName()}.");

            // Fire AfterUnequip
            var afterUnequip = GameEvent.New("AfterUnequip");
            afterUnequip.SetParameter("Actor", (object)actor);
            afterUnequip.SetParameter("Item", (object)item);
            afterUnequip.SetParameter("Slot", slot);
            actor.FireEvent(afterUnequip);

            return true;
        }

        /// <summary>
        /// Find all takeable items at the actor's current cell.
        /// </summary>
        public static List<Entity> GetTakeableItemsAtFeet(Entity actor, Zone zone)
        {
            var result = new List<Entity>();
            var cell = zone.GetEntityCell(actor);
            if (cell == null) return result;

            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var obj = cell.Objects[i];
                if (obj == actor) continue;
                var physics = obj.GetPart<PhysicsPart>();
                if (physics != null && physics.Takeable)
                    result.Add(obj);
            }
            return result;
        }

        /// <summary>
        /// Parse and apply/remove EquipBonuses string ("StatName:Amount,...").
        /// Modifies Stat.Bonus on the actor.
        /// </summary>
        private static void ApplyEquipBonuses(Entity actor, EquippablePart equippable, bool apply)
        {
            if (string.IsNullOrEmpty(equippable.EquipBonuses)) return;

            string[] pairs = equippable.EquipBonuses.Split(',');
            foreach (string pair in pairs)
            {
                string trimmed = pair.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                int colon = trimmed.IndexOf(':');
                if (colon < 0) continue;

                string statName = trimmed.Substring(0, colon);
                if (!int.TryParse(trimmed.Substring(colon + 1), out int amount))
                    continue;

                var stat = actor.GetStat(statName);
                if (stat == null) continue;

                if (apply)
                    stat.Bonus += amount;
                else
                    stat.Bonus -= amount;
            }
        }
    }
}