using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Core.Inventory.Planning;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Inventory facade and compatibility layer.
    /// Query helpers stay here, while all mutating operations route through
    /// ExecuteCommand(...) and transactional command implementations.
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
            var result = ExecuteCommand(new PickupCommand(item), actor, zone);
            return result.Success;
        }

        /// <summary>
        /// Drop an item from inventory onto the zone floor at the actor's position.
        /// Auto-unequips if the item is equipped.
        /// </summary>
        public static bool Drop(Entity actor, Entity item, Zone zone)
        {
            var result = ExecuteCommand(new DropCommand(item), actor, zone);
            return result.Success;
        }

        /// <summary>
        /// Drop a partial stack from inventory onto the zone floor.
        /// Splits the stack and drops the specified count.
        /// </summary>
        public static bool DropPartial(Entity actor, Entity item, int count, Zone zone)
        {
            var result = ExecuteCommand(new DropPartialCommand(item, count), actor, zone);
            return result.Success;
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
            var result = ExecuteCommand(new EquipCommand(item, targetBodyPart), actor);
            return result.Success;
        }

        /// <summary>
        /// Unequip an item from the actor, returning it to carried inventory.
        /// Body-part-aware.
        /// </summary>
        public static bool UnequipItem(Entity actor, Entity item)
        {
            var result = ExecuteCommand(new UnequipCommand(item), actor);
            return result.Success;
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
            var result = ExecuteCommand(new AutoEquipCommand(item), actor);
            return result.Success;
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
            var result = ExecuteCommand(new TakeFromContainerCommand(container, item), actor);
            return result.Success;
        }

        /// <summary>
        /// Take all items from a container into the actor's inventory.
        /// Returns the number of items successfully taken.
        /// </summary>
        public static int TakeAllFromContainer(Entity actor, Entity container)
        {
            if (actor == null || container == null)
                return 0;

            var containerPart = container.GetPart<ContainerPart>();
            if (containerPart == null)
                return 0;

            if (actor.GetPart<InventoryPart>() == null)
                return 0;

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
                var result = ExecuteCommand(new TakeFromContainerCommand(container, item), actor);
                if (!result.Success)
                    break;

                taken++;
            }

            return taken;
        }

        /// <summary>
        /// Put an item from the actor's inventory into a container.
        /// </summary>
        public static bool PutInContainer(Entity actor, Entity container, Entity item)
        {
            var result = ExecuteCommand(new PutInContainerCommand(container, item), actor);
            return result.Success;
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
            var result = ExecuteCommand(
                new PerformInventoryActionCommand(item, command),
                actor,
                zone);
            return result.Success;
        }

        /// <summary>
        /// Craft an item from a tinkering recipe through command execution.
        /// </summary>
        public static bool CraftFromRecipe(Entity actor, EntityFactory factory, string recipeId, Zone zone = null)
        {
            var result = ExecuteCommand(
                new CraftFromRecipeCommand(recipeId, factory),
                actor,
                zone);
            return result.Success;
        }

        /// <summary>
        /// Disassemble an owned item into bits through command execution.
        /// </summary>
        public static bool Disassemble(Entity actor, Entity item, Zone zone = null)
        {
            var result = ExecuteCommand(
                new DisassembleCommand(item),
                actor,
                zone);
            return result.Success;
        }

        /// <summary>
        /// Apply a tinkering modification recipe to an owned target item.
        /// </summary>
        public static bool ApplyModification(Entity actor, string recipeId, Entity targetItem, Zone zone = null)
        {
            var result = ExecuteCommand(
                new ApplyModificationCommand(recipeId, targetItem),
                actor,
                zone);
            return result.Success;
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
    }
}
