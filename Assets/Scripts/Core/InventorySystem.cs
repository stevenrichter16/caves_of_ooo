using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static system for inventory operations: pickup, drop, equip, unequip.
    /// Now body-part-aware: when the actor has a Body part, equipment routes
    /// through body part nodes using Qud-style slot queries.
    ///
    /// Flow:
    /// 1. Equip request: get item's EquippablePart.GetSlotArray()
    /// 2. Query actor's Body for matching body parts
    /// 3. Find free slots (or auto-unequip occupied ones)
    /// 4. Place item on body part node(s)
    ///
    /// Falls back to legacy string-slot system when no Body part exists.
    /// </summary>
    public static class InventorySystem
    {
        /// <summary>
        /// Pick up an item from the zone into an actor's inventory.
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

            // If item is equipped, unequip first
            if (IsEquipped(actor, item))
            {
                if (!UnequipItem(actor, item))
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
        /// Equip an item from the actor's inventory.
        /// Body-part-aware: when actor has Body, queries body parts for valid slots.
        /// Falls back to legacy slot system otherwise.
        ///
        /// Mirrors Qud's CommandEquipObject flow:
        /// 1. Get item's slot requirements
        /// 2. Find matching body parts
        /// 3. Auto-unequip existing items in those slots
        /// 4. Place item on body part(s)
        /// </summary>
        public static bool Equip(Entity actor, Entity item, BodyPart targetBodyPart = null)
        {
            if (actor == null || item == null) return false;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            var equippable = item.GetPart<EquippablePart>();
            if (equippable == null) return false;

            var body = actor.GetPart<Body>();

            // Fire BeforeEquip
            var beforeEquip = GameEvent.New("BeforeEquip");
            beforeEquip.SetParameter("Actor", (object)actor);
            beforeEquip.SetParameter("Item", (object)item);
            beforeEquip.SetParameter("Slot", equippable.Slot);
            if (!actor.FireEvent(beforeEquip))
                return false;

            bool result;
            if (body != null)
                result = EquipBodyPartAware(actor, item, equippable, body, inventory, targetBodyPart);
            else
                result = EquipLegacy(actor, item, equippable, inventory);

            if (result)
            {
                ApplyEquipBonuses(actor, equippable, true);
                MessageLog.Add($"{actor.GetDisplayName()} equips {item.GetDisplayName()}.");

                // Fire AfterEquip
                var afterEquip = GameEvent.New("AfterEquip");
                afterEquip.SetParameter("Actor", (object)actor);
                afterEquip.SetParameter("Item", (object)item);
                afterEquip.SetParameter("Slot", equippable.Slot);
                actor.FireEvent(afterEquip);
            }

            return result;
        }

        /// <summary>
        /// Unequip an item from the actor, returning it to carried inventory.
        /// Body-part-aware.
        /// </summary>
        public static bool UnequipItem(Entity actor, Entity item)
        {
            if (actor == null || item == null) return false;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            var equippable = item.GetPart<EquippablePart>();

            // Fire BeforeUnequip
            var beforeUnequip = GameEvent.New("BeforeUnequip");
            beforeUnequip.SetParameter("Actor", (object)actor);
            beforeUnequip.SetParameter("Item", (object)item);
            if (!actor.FireEvent(beforeUnequip))
                return false;

            // Remove stat bonuses
            if (equippable != null)
                ApplyEquipBonuses(actor, equippable, false);

            // Body-part-aware unequip
            var body = actor.GetPart<Body>();
            if (body != null)
            {
                var bodyPart = inventory.FindEquippedBodyPart(item);
                if (bodyPart != null)
                {
                    inventory.UnequipFromBodyPart(bodyPart);
                }
                else
                {
                    // Fallback: search all parts
                    var parts = body.GetParts();
                    for (int i = 0; i < parts.Count; i++)
                    {
                        if (parts[i]._Equipped == item)
                        {
                            inventory.UnequipFromBodyPart(parts[i]);
                            break;
                        }
                    }
                }
            }
            else
            {
                // Legacy unequip
                var slotName = inventory.FindEquippedSlot(item);
                if (slotName != null)
                    inventory.Unequip(slotName);
            }

            MessageLog.Add($"{actor.GetDisplayName()} unequips {item.GetDisplayName()}.");

            // Fire AfterUnequip
            var afterUnequip = GameEvent.New("AfterUnequip");
            afterUnequip.SetParameter("Actor", (object)actor);
            afterUnequip.SetParameter("Item", (object)item);
            actor.FireEvent(afterUnequip);

            return true;
        }

        /// <summary>
        /// Legacy unequip by slot name. Kept for backward compatibility.
        /// </summary>
        public static bool Unequip(Entity actor, string slot)
        {
            if (actor == null || string.IsNullOrEmpty(slot)) return false;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            var item = inventory.GetEquipped(slot);
            if (item == null) return false;

            return UnequipItem(actor, item);
        }

        /// <summary>
        /// Check if an entity is currently equipped on the actor.
        /// </summary>
        public static bool IsEquipped(Entity actor, Entity item)
        {
            if (actor == null || item == null) return false;
            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            foreach (var kvp in inventory.EquippedItems)
            {
                if (kvp.Value == item)
                    return true;
            }
            return false;
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

        // --- Body-part-aware equip ---

        private static bool EquipBodyPartAware(Entity actor, Entity item,
            EquippablePart equippable, Body body, InventoryPart inventory,
            BodyPart targetBodyPart)
        {
            string[] slotTypes = equippable.GetSlotArray();

            // If a specific target body part was given, use it
            if (targetBodyPart != null)
            {
                // Unequip existing item in that slot
                if (targetBodyPart._Equipped != null)
                {
                    if (!UnequipItem(actor, targetBodyPart._Equipped))
                        return false;
                }
                inventory.EquipToBodyPart(item, targetBodyPart);
                return true;
            }

            // Find body parts for each required slot
            // Group slot types and find matching free (or occupiable) body parts
            var occupiedParts = new List<BodyPart>();

            for (int i = 0; i < slotTypes.Length; i++)
            {
                string slotType = slotTypes[i].Trim();
                BodyPart slot = FindBestSlot(body, slotType, occupiedParts);

                if (slot == null)
                {
                    MessageLog.Add($"No available {slotType} slot for {item.GetDisplayName()}.");
                    return false;
                }

                // Unequip existing item if occupied
                if (slot._Equipped != null)
                {
                    if (!UnequipItem(actor, slot._Equipped))
                        return false;
                }

                occupiedParts.Add(slot);
            }

            // Equip to all found parts
            if (occupiedParts.Count == 1)
            {
                inventory.EquipToBodyPart(item, occupiedParts[0]);
            }
            else
            {
                inventory.EquipToBodyParts(item, occupiedParts);
            }

            return true;
        }

        /// <summary>
        /// Find the best body part slot for an item, preferring free slots
        /// and avoiding already-claimed parts.
        /// </summary>
        private static BodyPart FindBestSlot(Body body, string slotType,
            List<BodyPart> alreadyClaimed)
        {
            var candidates = body.GetEquippableSlots(slotType);

            // Prefer free slots first
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c._Equipped == null && !alreadyClaimed.Contains(c))
                    return c;
            }

            // Fall back to occupied slots (will auto-unequip)
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (!alreadyClaimed.Contains(c))
                    return c;
            }

            return null;
        }

        // --- Legacy equip ---

        private static bool EquipLegacy(Entity actor, Entity item,
            EquippablePart equippable, InventoryPart inventory)
        {
            string slot = equippable.Slot;

            // Unequip existing item in that slot
            if (inventory.GetEquipped(slot) != null)
            {
                if (!Unequip(actor, slot))
                    return false;
            }

            inventory.Equip(item, slot);
            return true;
        }

        // --- Stat bonuses ---

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
