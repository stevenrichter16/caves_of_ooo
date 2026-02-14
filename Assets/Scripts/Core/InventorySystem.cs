using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Planning;
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
        private static readonly InventoryCommandExecutor CommandExecutor = new InventoryCommandExecutor();
        private static readonly EquipPlanner BodyEquipPlanner = new EquipPlanner();

        /// <summary>
        /// Refactor seam: execute an inventory command through the new
        /// validation/execution/rollback pipeline.
        /// </summary>
        public static InventoryCommandResult ExecuteCommand(IInventoryCommand command, Entity actor, Zone zone = null)
        {
            var context = new InventoryContext(actor, zone);
            return CommandExecutor.Execute(command, context);
        }

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

            // Auto-equip if slot is free
            AutoEquip(actor, item);

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
        /// Drop a partial stack from inventory onto the zone floor.
        /// Splits the stack and drops the specified count.
        /// </summary>
        public static bool DropPartial(Entity actor, Entity item, int count, Zone zone)
        {
            if (actor == null || item == null || zone == null) return false;
            if (count <= 0) return false;

            var stacker = item.GetPart<StackerPart>();
            if (stacker == null) return Drop(actor, item, zone);
            if (count >= stacker.StackCount) return Drop(actor, item, zone);

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            // Split off the requested count
            var split = stacker.SplitStack(count);
            if (split == null) return false;

            // Place split stack in zone at actor's position
            var cell = zone.GetEntityCell(actor);
            if (cell != null)
                zone.AddEntity(split, cell.X, cell.Y);

            MessageLog.Add($"{actor.GetDisplayName()} drops {split.GetDisplayName()}.");
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

            // If stacked, split off one item to equip
            var stacker = item.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                item = stacker.RemoveOne();
                equippable = item.GetPart<EquippablePart>();
                // Original stack stays in inventory; we equip the clone
            }

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
                result = EquipBodyPartAware(actor, item, inventory, targetBodyPart);
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

        // --- Auto-Equip ---

        /// <summary>
        /// Auto-equip an item if it has an EquippablePart and the target slot is free.
        /// Only equips into empty slots — never displaces existing equipment.
        /// Mirrors Qud's auto-equip on pickup behavior.
        /// </summary>
        public static bool AutoEquip(Entity actor, Entity item)
        {
            if (actor == null || item == null) return false;

            var equippable = item.GetPart<EquippablePart>();
            if (equippable == null) return false;

            // Don't auto-equip stacked items (stacks stay in inventory)
            var stacker = item.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1) return false;

            var body = actor.GetPart<Body>();

            if (body != null)
                return AutoEquipBodyPartAware(actor, item);
            else
                return AutoEquipLegacy(actor, item, equippable);
        }

        private static bool AutoEquipBodyPartAware(Entity actor, Entity item)
        {
            var plan = BodyEquipPlanner.Build(actor, item);
            if (!plan.IsValid)
                return false;

            // Auto-equip never displaces existing equipment.
            if (plan.Displacements.Count > 0)
                return false;

            return Equip(actor, item);
        }

        private static bool AutoEquipLegacy(Entity actor, Entity item,
            EquippablePart equippable)
        {
            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            // Only auto-equip if the slot is empty
            if (inventory.GetEquipped(equippable.Slot) != null)
                return false;

            return Equip(actor, item);
        }

        // --- Containers ---

        /// <summary>
        /// Find all containers at the actor's current cell (chests, corpses, etc.).
        /// </summary>
        public static List<Entity> GetContainersAtFeet(Entity actor, Zone zone)
        {
            var result = new List<Entity>();
            if (actor == null || zone == null) return result;

            var cell = zone.GetEntityCell(actor);
            if (cell == null) return result;

            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var obj = cell.Objects[i];
                if (obj == actor) continue;
                if (obj.GetPart<ContainerPart>() != null)
                    result.Add(obj);
            }
            return result;
        }

        /// <summary>
        /// Take a specific item from a container into the actor's inventory.
        /// </summary>
        public static bool TakeFromContainer(Entity actor, Entity container, Entity item)
        {
            if (actor == null || container == null || item == null) return false;

            var containerPart = container.GetPart<ContainerPart>();
            if (containerPart == null) return false;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            if (containerPart.Locked)
            {
                MessageLog.Add($"The {container.GetDisplayName()} is locked.");
                return false;
            }

            if (!containerPart.RemoveItem(item)) return false;

            if (!inventory.AddObject(item))
            {
                // Too heavy — put it back
                containerPart.AddItem(item);
                MessageLog.Add($"You can't carry {item.GetDisplayName()}: too heavy!");
                return false;
            }

            MessageLog.Add($"You take {item.GetDisplayName()} from the {container.GetDisplayName()}.");
            return true;
        }

        /// <summary>
        /// Take all items from a container into the actor's inventory.
        /// Returns the number of items successfully taken.
        /// </summary>
        public static int TakeAllFromContainer(Entity actor, Entity container)
        {
            if (actor == null || container == null) return 0;

            var containerPart = container.GetPart<ContainerPart>();
            if (containerPart == null) return 0;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return 0;

            if (containerPart.Locked)
            {
                MessageLog.Add($"The {container.GetDisplayName()} is locked.");
                return 0;
            }

            int taken = 0;
            var items = new List<Entity>(containerPart.Contents);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!containerPart.RemoveItem(item)) continue;

                if (!inventory.AddObject(item))
                {
                    containerPart.AddItem(item);
                    MessageLog.Add($"You can't carry {item.GetDisplayName()}: too heavy!");
                    break;
                }

                MessageLog.Add($"You take {item.GetDisplayName()} from the {container.GetDisplayName()}.");
                taken++;
            }
            return taken;
        }

        /// <summary>
        /// Put an item from the actor's inventory into a container.
        /// </summary>
        public static bool PutInContainer(Entity actor, Entity container, Entity item)
        {
            if (actor == null || container == null || item == null) return false;

            var containerPart = container.GetPart<ContainerPart>();
            if (containerPart == null) return false;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return false;

            if (containerPart.Locked)
            {
                MessageLog.Add($"The {container.GetDisplayName()} is locked.");
                return false;
            }

            // If equipped, unequip first
            if (IsEquipped(actor, item))
            {
                if (!UnequipItem(actor, item))
                    return false;
            }

            if (!inventory.RemoveObject(item)) return false;

            if (!containerPart.AddItem(item))
            {
                // Container full — put item back
                inventory.AddObject(item);
                MessageLog.Add($"The {container.GetDisplayName()} is full.");
                return false;
            }

            MessageLog.Add($"You put {item.GetDisplayName()} {containerPart.Preposition} the {container.GetDisplayName()}.");
            return true;
        }

        // --- Item Actions ---

        /// <summary>
        /// Get available inventory actions for an item.
        /// Fires GetInventoryActions event on the item; parts respond by adding actions.
        /// Mirrors Qud's GetInventoryActionsEvent flow.
        /// </summary>
        public static List<InventoryAction> GetActions(Entity actor, Entity item)
        {
            var actionList = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", (object)actionList);
            e.SetParameter("Actor", (object)actor);
            item.FireEvent(e);
            actionList.Sort();
            return actionList.Actions;
        }

        /// <summary>
        /// Perform an inventory action on an item by command string.
        /// Fires InventoryAction event on the item (or actor if FireOnActor).
        /// Returns true if the action was handled.
        /// </summary>
        public static bool PerformAction(Entity actor, Entity item, string command, Zone zone = null)
        {
            if (actor == null || item == null || string.IsNullOrEmpty(command))
                return false;

            // Fire BeforeInventoryAction on actor (can veto)
            var before = GameEvent.New("BeforeInventoryAction");
            before.SetParameter("Actor", (object)actor);
            before.SetParameter("Item", (object)item);
            before.SetParameter("Command", command);
            if (!actor.FireEvent(before))
                return false;

            // Fire InventoryAction on the item
            var actionEvent = GameEvent.New("InventoryAction");
            actionEvent.SetParameter("Actor", (object)actor);
            actionEvent.SetParameter("Item", (object)item);
            actionEvent.SetParameter("Command", command);
            if (zone != null)
                actionEvent.SetParameter("Zone", (object)zone);

            bool handled = !item.FireEvent(actionEvent) || actionEvent.Handled;

            if (handled)
            {
                // Fire AfterInventoryAction on actor
                var after = GameEvent.New("AfterInventoryAction");
                after.SetParameter("Actor", (object)actor);
                after.SetParameter("Item", (object)item);
                after.SetParameter("Command", command);
                actor.FireEvent(after);
            }

            return handled;
        }

        // --- Displacement preview ---

        /// <summary>
        /// Describes a single item that would be displaced by an equip operation.
        /// </summary>
        public struct Displacement
        {
            public Entity Item;
            public BodyPart BodyPart;
        }

        /// <summary>
        /// Preview what items would be displaced when equipping an item, without mutating state.
        /// Returns a list of (item, body part) pairs that would need to be unequipped.
        /// Multi-slot items list every body part they occupy (e.g. a two-handed weapon
        /// shows both "greatsword in Right Hand" and "greatsword in Left Hand").
        /// </summary>
        public static List<Displacement> PreviewDisplacements(Entity actor, Entity item,
            BodyPart targetBodyPart = null)
        {
            var result = new List<Displacement>();
            if (actor == null || item == null) return result;

            var plan = BodyEquipPlanner.Build(actor, item, targetBodyPart);
            if (!plan.IsValid)
                return result;

            for (int i = 0; i < plan.Displacements.Count; i++)
            {
                var displacement = plan.Displacements[i];
                result.Add(new Displacement
                {
                    Item = displacement.Item,
                    BodyPart = displacement.BodyPart
                });
            }

            return result;
        }

        // --- Body-part-aware equip ---

        private static bool EquipBodyPartAware(Entity actor, Entity item,
            InventoryPart inventory, BodyPart targetBodyPart)
        {
            var plan = BodyEquipPlanner.Build(actor, item, targetBodyPart);
            if (!plan.IsValid)
            {
                if (!string.IsNullOrEmpty(plan.FailureReason))
                    MessageLog.Add(plan.FailureReason);
                return false;
            }

            var occupiedParts = plan.ClaimedParts;
            if (occupiedParts.Count == 0)
                return false;

            // Unequip existing items in all claimed parts (deduplicate multi-slot)
            var unequipped = new HashSet<Entity>();
            for (int i = 0; i < occupiedParts.Count; i++)
            {
                var existing = occupiedParts[i]._Equipped;
                if (existing != null && existing != item && unequipped.Add(existing))
                {
                    if (!UnequipItem(actor, existing))
                        return false;
                }
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
        /// Also applies ArmorPart.SpeedPenalty as a penalty to the Speed stat.
        /// Modifies Stat.Bonus/Penalty on the actor.
        /// </summary>
        private static void ApplyEquipBonuses(Entity actor, EquippablePart equippable, bool apply)
        {
            var item = equippable.ParentEntity;

            // Apply EquipBonuses string
            if (!string.IsNullOrEmpty(equippable.EquipBonuses))
            {
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

            // Apply ArmorPart.SpeedPenalty as a penalty on the Speed stat
            if (item != null)
            {
                var armor = item.GetPart<ArmorPart>();
                if (armor != null && armor.SpeedPenalty != 0)
                {
                    var speedStat = actor.GetStat("Speed");
                    if (speedStat != null)
                    {
                        if (apply)
                            speedStat.Penalty += armor.SpeedPenalty;
                        else
                            speedStat.Penalty -= armor.SpeedPenalty;
                    }
                }
            }
        }
    }
}
