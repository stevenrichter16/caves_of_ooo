using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Generates organized inventory data for display.
    /// Pure data model — no Unity dependencies, fully testable.
    /// Mirrors Qud's inventory screen: items grouped by category,
    /// equipment listed by body part, with weight/value summaries.
    /// </summary>
    public static class InventoryScreenData
    {
        /// <summary>
        /// A single item for display.
        /// </summary>
        public class ItemDisplay
        {
            public Entity Item;
            public string Name;
            public string Category;
            public int Weight;
            public int Value;
            public int StackCount;
            public bool IsEquipped;
            public string EquippedSlot;
            public List<InventoryAction> Actions;
        }

        /// <summary>
        /// A group of items in one category.
        /// </summary>
        public class CategoryGroup
        {
            public string CategoryName;
            public int SortOrder;
            public List<ItemDisplay> Items = new List<ItemDisplay>();
        }

        /// <summary>
        /// Equipment slot display info.
        /// </summary>
        public class EquipmentSlot
        {
            public string SlotName;
            public string BodyPartName;
            public Entity EquippedItem;
            public string ItemName;
        }

        /// <summary>
        /// Full inventory screen state.
        /// </summary>
        public class ScreenState
        {
            public List<CategoryGroup> Categories = new List<CategoryGroup>();
            public List<EquipmentSlot> Equipment = new List<EquipmentSlot>();
            public int CarriedWeight;
            public int MaxCarryWeight;
            public int TotalItems;
            public int Drams;
        }

        /// <summary>
        /// Build the full inventory screen data for an entity.
        /// </summary>
        public static ScreenState Build(Entity actor)
        {
            var state = new ScreenState();
            if (actor == null) return state;

            var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null) return state;

            state.CarriedWeight = inventory.GetCarriedWeight();
            state.MaxCarryWeight = inventory.GetMaxCarryWeight();
            state.Drams = TradeSystem.GetDrams(actor);

            // Build category groups from carried items
            var categoryMap = new Dictionary<string, CategoryGroup>();

            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                var item = inventory.Objects[i];
                var display = BuildItemDisplay(actor, item, false, null);
                AddToCategory(categoryMap, display);
                state.TotalItems++;
            }

            // Add equipped items (legacy slots)
            foreach (var kvp in inventory.EquippedItems)
            {
                if (kvp.Value == null) continue;
                var display = BuildItemDisplay(actor, kvp.Value, true, kvp.Key);
                AddToCategory(categoryMap, display);
                state.TotalItems++;
            }

            // Sort categories
            var categories = new List<CategoryGroup>(categoryMap.Values);
            categories.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            state.Categories = categories;

            // Build equipment list
            state.Equipment = BuildEquipmentList(actor, inventory);

            return state;
        }

        /// <summary>
        /// Build a list of all equipment slots and what's in them.
        /// Body-part-aware when available, falls back to legacy slots.
        /// </summary>
        public static List<EquipmentSlot> BuildEquipmentList(Entity actor, InventoryPart inventory)
        {
            var slots = new List<EquipmentSlot>();

            var body = actor.GetPart<Body>();
            if (body != null)
            {
                var parts = body.GetParts();
                for (int i = 0; i < parts.Count; i++)
                {
                    var part = parts[i];
                    if (part.Abstract) continue;

                    // Only show equippable slots
                    string slotType = part.Type;
                    if (slotType == "Body" || slotType == "Head" || slotType == "Hand" ||
                        slotType == "Arm" || slotType == "Feet" || slotType == "Back")
                    {
                        var slot = new EquipmentSlot
                        {
                            SlotName = slotType,
                            BodyPartName = part.GetDisplayName(),
                            EquippedItem = part._Equipped,
                            ItemName = part._Equipped != null ? part._Equipped.GetDisplayName() : "(empty)"
                        };
                        slots.Add(slot);
                    }
                }
            }
            else
            {
                // Legacy slot display
                foreach (var kvp in inventory.EquippedItems)
                {
                    var slot = new EquipmentSlot
                    {
                        SlotName = kvp.Key,
                        BodyPartName = kvp.Key,
                        EquippedItem = kvp.Value,
                        ItemName = kvp.Value != null ? kvp.Value.GetDisplayName() : "(empty)"
                    };
                    slots.Add(slot);
                }
            }

            return slots;
        }

        private static ItemDisplay BuildItemDisplay(Entity actor, Entity item, bool equipped, string slot)
        {
            var stacker = item.GetPart<StackerPart>();
            return new ItemDisplay
            {
                Item = item,
                Name = item.GetDisplayName(),
                Category = ItemCategory.GetCategory(item),
                Weight = InventoryPart.GetItemWeight(item),
                Value = TradeSystem.GetItemValue(item),
                StackCount = stacker != null ? stacker.StackCount : 1,
                IsEquipped = equipped,
                EquippedSlot = slot,
                Actions = InventorySystem.GetActions(actor, item)
            };
        }

        private static void AddToCategory(Dictionary<string, CategoryGroup> map, ItemDisplay display)
        {
            if (!map.TryGetValue(display.Category, out var group))
            {
                group = new CategoryGroup
                {
                    CategoryName = display.Category,
                    SortOrder = ItemCategory.GetSortOrder(display.Category)
                };
                map[display.Category] = group;
            }
            group.Items.Add(display);
        }
    }
}
