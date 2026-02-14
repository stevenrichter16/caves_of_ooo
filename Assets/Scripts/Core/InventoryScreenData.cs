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

            // Paperdoll layout fields
            public int GridX;
            public int GridY;
            public string ShortLabel;
            public int BodyPartID;
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

            // Build equipment list and compute paperdoll layout
            state.Equipment = BuildEquipmentList(actor, inventory);
            BuildPaperdollLayout(state.Equipment);

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
                            ItemName = part._Equipped != null ? part._Equipped.GetDisplayName() : "(empty)",
                            BodyPartID = part.ID
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

        /// <summary>
        /// Assign grid positions and short labels to equipment slots
        /// for paperdoll-style spatial rendering.
        /// </summary>
        public static void BuildPaperdollLayout(List<EquipmentSlot> slots)
        {
            if (slots == null || slots.Count == 0) return;

            const int BOX_W = 5;
            const int H_GAP = 2;
            const int CENTER_X = 37;
            const int LEFT_X = 18;
            const int RIGHT_X = 56;
            const int ROW0_Y = 9;
            const int ROW1_Y = 16;
            const int ROW2_Y = 23;
            const int ROW3_Y = 30;

            // Classify slots
            EquipmentSlot head = null, body = null, back = null, feet = null;
            EquipmentSlot leftArm = null, rightArm = null;
            EquipmentSlot leftHand = null, rightHand = null;
            var extraArms = new List<EquipmentSlot>();
            var extraHands = new List<EquipmentSlot>();
            var extraFeet = new List<EquipmentSlot>();

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                string name = s.BodyPartName.ToLowerInvariant();
                string type = s.SlotName;

                if (type == "Head") { if (head == null) head = s; continue; }
                if (type == "Body") { if (body == null) body = s; continue; }
                if (type == "Back") { if (back == null) back = s; continue; }
                if (type == "Feet")
                {
                    if (feet == null) feet = s;
                    else extraFeet.Add(s);
                    continue;
                }
                if (type == "Arm")
                {
                    if (name.Contains("left") && leftArm == null) leftArm = s;
                    else if (name.Contains("right") && rightArm == null) rightArm = s;
                    else extraArms.Add(s);
                    continue;
                }
                if (type == "Hand")
                {
                    if (name.Contains("left") && leftHand == null) leftHand = s;
                    else if (name.Contains("right") && rightHand == null) rightHand = s;
                    else extraHands.Add(s);
                    continue;
                }
            }

            // Assign base humanoid positions
            if (head != null) AssignSlot(head, CENTER_X, ROW0_Y, "Head");
            if (leftArm != null) AssignSlot(leftArm, LEFT_X, ROW1_Y, "L.Arm");
            if (back != null) AssignSlot(back, CENTER_X, ROW1_Y, "Back");
            if (rightArm != null) AssignSlot(rightArm, RIGHT_X, ROW1_Y, "R.Arm");
            if (leftHand != null) AssignSlot(leftHand, LEFT_X, ROW2_Y, "L.Hand");
            if (body != null) AssignSlot(body, CENTER_X, ROW2_Y, "Body");
            if (rightHand != null) AssignSlot(rightHand, RIGHT_X, ROW2_Y, "R.Hand");
            if (feet != null) AssignSlot(feet, CENTER_X, ROW3_Y, "Feet");

            // Place extra arms at expanding offsets
            int extraLeftX = LEFT_X - (BOX_W + H_GAP);
            int extraRightX = RIGHT_X + (BOX_W + H_GAP);
            for (int i = 0; i < extraArms.Count; i++)
            {
                bool goLeft = (i % 2 == 0);
                int x = goLeft ? extraLeftX : extraRightX;
                if (x < 0) x = 0;
                if (x + BOX_W > 80) x = 80 - BOX_W;
                string label = "Arm " + (3 + i);
                AssignSlot(extraArms[i], x, ROW1_Y, label);
                if (goLeft) extraLeftX -= (BOX_W + H_GAP);
                else extraRightX += (BOX_W + H_GAP);
            }

            // Place extra hands below their corresponding arm row
            extraLeftX = LEFT_X - (BOX_W + H_GAP);
            extraRightX = RIGHT_X + (BOX_W + H_GAP);
            for (int i = 0; i < extraHands.Count; i++)
            {
                bool goLeft = (i % 2 == 0);
                int x = goLeft ? extraLeftX : extraRightX;
                if (x < 0) x = 0;
                if (x + BOX_W > 80) x = 80 - BOX_W;
                string label = "Hand " + (3 + i);
                AssignSlot(extraHands[i], x, ROW2_Y, label);
                if (goLeft) extraLeftX -= (BOX_W + H_GAP);
                else extraRightX += (BOX_W + H_GAP);
            }

            // Place extra feet in a row
            int feetX = CENTER_X - (BOX_W + H_GAP);
            for (int i = 0; i < extraFeet.Count; i++)
            {
                if (feetX < 0) feetX = 0;
                string label = "Feet " + (2 + i);
                AssignSlot(extraFeet[i], feetX, ROW3_Y, label);
                feetX -= (BOX_W + H_GAP);
            }
        }

        private static void AssignSlot(EquipmentSlot slot, int x, int y, string label)
        {
            slot.GridX = x;
            slot.GridY = y;
            slot.ShortLabel = label;
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
