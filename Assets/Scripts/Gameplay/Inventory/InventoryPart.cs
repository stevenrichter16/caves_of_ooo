using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Container for carried and equipped items.
    /// Now integrates with the Body part system: when the parent entity has a Body,
    /// equipment is stored on BodyPart nodes. EquippedItems dictionary is maintained
    /// as a legacy/quick-access cache.
    ///
    /// Equipment flow:
    /// 1. With Body: item → BodyPart._Equipped (source of truth) + EquippedItems cache
    /// 2. Without Body: item → EquippedItems dictionary (legacy mode)
    /// </summary>
    public class InventoryPart : Part
    {
        public override string Name => "Inventory";

        /// <summary>
        /// Pounds of carry capacity per point of Strength.
        /// Matches Qud's MAXIMUM_CARRIED_WEIGHT_PER_STRENGTH = 15.
        /// </summary>
        public const int WEIGHT_PER_STRENGTH = 15;

        /// <summary>
        /// Hard weight limit for AddObject. -1 means no hard limit.
        /// Note: overburden is a separate soft limit based on Strength.
        /// </summary>
        public int MaxWeight = -1;

        /// <summary>
        /// Items currently carried (not equipped).
        /// </summary>
        public List<Entity> Objects = new List<Entity>();

        /// <summary>
        /// Items currently equipped, keyed by slot name (legacy) or body part ID string.
        /// When Body part exists, keys are body part IDs as strings.
        /// When no Body part, keys are slot type strings ("Hand", "Body", etc).
        /// </summary>
        public Dictionary<string, Entity> EquippedItems = new Dictionary<string, Entity>();

        /// <summary>
        /// Add an item to the carried list. Sets InInventory back-reference.
        /// Returns false if weight would be exceeded.
        /// If the item is stackable and matches an existing stack, merges automatically.
        /// </summary>
        public bool AddObject(Entity item)
        {
            if (MaxWeight >= 0)
            {
                int itemWeight = GetItemWeight(item);
                if (GetCarriedWeight() + itemWeight > MaxWeight)
                    return false;
            }

            // Try to merge into existing stack
            var itemStacker = item.GetPart<StackerPart>();
            if (itemStacker != null)
            {
                for (int i = 0; i < Objects.Count; i++)
                {
                    var existingStacker = Objects[i].GetPart<StackerPart>();
                    if (existingStacker != null && existingStacker.CanStackWith(item))
                    {
                        existingStacker.MergeFrom(item);
                        if (itemStacker.StackCount <= 0)
                            return true; // Fully merged — item consumed
                    }
                }
            }

            // Add as new entry (or remaining count if partial merge)
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
        /// Equip an item to a body part (body-part-aware mode).
        /// Sets the item on the BodyPart node and updates caches.
        /// For multi-slot items, the same entity ref is set on all occupied parts.
        /// </summary>
        public bool EquipToBodyPart(Entity item, BodyPart bodyPart)
        {
            Objects.Remove(item);
            bodyPart.SetEquipped(item);

            // Cache in EquippedItems by body part ID
            EquippedItems[bodyPart.ID.ToString()] = item;

            var physics = item.GetPart<PhysicsPart>();
            if (physics != null)
            {
                physics.InInventory = null;
                physics.Equipped = ParentEntity;
            }
            return true;
        }

        /// <summary>
        /// Equip an item to multiple body parts (e.g. two-handed weapon).
        /// </summary>
        public bool EquipToBodyParts(Entity item, List<BodyPart> bodyParts)
        {
            Objects.Remove(item);

            for (int i = 0; i < bodyParts.Count; i++)
            {
                bodyParts[i]._Equipped = item;
                // Only mark first as FirstSlotForEquipped
                bodyParts[i].FirstSlotForEquipped = (i == 0);
                EquippedItems[bodyParts[i].ID.ToString()] = item;
            }

            var physics = item.GetPart<PhysicsPart>();
            if (physics != null)
            {
                physics.InInventory = null;
                physics.Equipped = ParentEntity;
            }
            return true;
        }

        /// <summary>
        /// Unequip an item from a body part. Moves to carried list.
        /// Clears from all body parts sharing this item (multi-slot).
        /// </summary>
        public bool UnequipFromBodyPart(BodyPart bodyPart)
        {
            if (bodyPart._Equipped == null) return false;

            var item = bodyPart._Equipped;

            // Clear from all body parts that have this item
            var body = ParentEntity?.GetPart<Body>();
            if (body != null)
            {
                var allParts = body.GetParts();
                for (int i = 0; i < allParts.Count; i++)
                {
                    if (allParts[i]._Equipped == item)
                    {
                        EquippedItems.Remove(allParts[i].ID.ToString());
                        allParts[i].ClearEquipped();
                    }
                }
            }
            else
            {
                bodyPart.ClearEquipped();
            }

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
        /// Legacy equip: item to a string-keyed slot.
        /// Used when entity has no Body part.
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
        /// Legacy unequip: from a string-keyed slot.
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
        /// Get the item equipped in a legacy slot, or null.
        /// </summary>
        public Entity GetEquipped(string slot)
        {
            EquippedItems.TryGetValue(slot, out Entity item);
            return item;
        }

        /// <summary>
        /// Get the first equipped item that has a specific Part type.
        /// Searches both body-part-equipped and legacy-equipped items.
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
        /// Get all equipped items (no duplicates for multi-slot).
        /// </summary>
        public List<Entity> GetAllEquipped()
        {
            var result = new List<Entity>();
            var seen = new HashSet<Entity>();
            foreach (var kvp in EquippedItems)
            {
                if (seen.Add(kvp.Value))
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// Total weight of all carried + equipped items.
        /// Stack-aware: uses StackerPart.GetTotalWeight() when present.
        /// </summary>
        public int GetCarriedWeight()
        {
            int total = 0;
            for (int i = 0; i < Objects.Count; i++)
                total += GetItemWeight(Objects[i]);

            // Deduplicate equipped items for weight counting
            var seen = new HashSet<Entity>();
            foreach (var kvp in EquippedItems)
            {
                if (seen.Add(kvp.Value))
                    total += GetItemWeight(kvp.Value);
            }
            return total;
        }

        /// <summary>
        /// Get the total weight of an item, accounting for stack count.
        /// </summary>
        public static int GetItemWeight(Entity item)
        {
            var stacker = item.GetPart<StackerPart>();
            if (stacker != null)
                return stacker.GetTotalWeight();
            var physics = item.GetPart<PhysicsPart>();
            return physics?.Weight ?? 0;
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

        /// <summary>
        /// Find the body part an item is equipped on (first slot), or null.
        /// Only works when entity has a Body part.
        /// </summary>
        public BodyPart FindEquippedBodyPart(Entity item)
        {
            var body = ParentEntity?.GetPart<Body>();
            if (body == null) return null;

            var parts = body.GetParts();
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i]._Equipped == item && parts[i].FirstSlotForEquipped)
                    return parts[i];
            }
            return null;
        }

        /// <summary>
        /// Find the legacy slot name for an equipped item, or null.
        /// </summary>
        public string FindEquippedSlot(Entity item)
        {
            foreach (var kvp in EquippedItems)
            {
                if (kvp.Value == item)
                    return kvp.Key;
            }
            return null;
        }

        // --- Encumbrance / Overburden ---

        /// <summary>
        /// Maximum carry weight based on Strength stat.
        /// Returns -1 if no Strength stat (unlimited).
        /// </summary>
        public int GetMaxCarryWeight()
        {
            if (ParentEntity == null) return -1;
            var str = ParentEntity.GetStat("Strength");
            if (str == null) return -1;
            return str.Value * WEIGHT_PER_STRENGTH;
        }

        /// <summary>
        /// Check if the entity is carrying more than Strength allows.
        /// Only applies to player-tagged entities (NPCs are exempt per Qud).
        /// </summary>
        public bool IsOverburdened()
        {
            if (ParentEntity == null) return false;
            if (!ParentEntity.HasTag("Player")) return false;

            int maxCarry = GetMaxCarryWeight();
            if (maxCarry < 0) return false;
            return GetCarriedWeight() > maxCarry;
        }

        /// <summary>
        /// Block movement when overburdened.
        /// </summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeMove")
            {
                if (IsOverburdened())
                {
                    e.SetParameter("Blocked", true);
                    MessageLog.Add("You are overburdened and cannot move!");
                    return false;
                }
            }
            return true;
        }
    }
}
