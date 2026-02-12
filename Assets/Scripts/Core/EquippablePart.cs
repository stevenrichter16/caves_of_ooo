namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an item as equippable and defines which body part slot(s) it uses.
    /// Mirrors Qud's equipment slot system: items declare which body part types
    /// they can equip to via UsesSlots (comma-separated body part type names).
    ///
    /// When an entity has a Body part, equipment routes through body part nodes.
    /// When no Body part exists, falls back to legacy string-keyed slot system.
    /// </summary>
    public class EquippablePart : Part
    {
        public override string Name => "Equippable";

        /// <summary>
        /// Primary slot type this item equips to (e.g. "Hand", "Body", "Head", "Feet").
        /// Used for both legacy string-slot and body-part-aware equip.
        /// </summary>
        public string Slot = "Hand";

        /// <summary>
        /// Comma-separated list of body part types this item occupies.
        /// Mirrors Qud's Physics.UsesSlots.
        ///
        /// Examples:
        /// - "Hand" = one-handed weapon/shield (occupies 1 Hand)
        /// - "Hand,Hand" = two-handed weapon (occupies 2 Hands)
        /// - "Body" = body armor
        /// - "Head" = helmet
        /// - "Back" = cloak
        /// - "Feet" = boots
        /// - "Floating Nearby" = floating item
        ///
        /// If null or empty, falls back to Slot field for a single slot.
        /// </summary>
        public string UsesSlots;

        /// <summary>
        /// Stat bonuses applied when equipped, as comma-separated "StatName:Amount" pairs.
        /// Example: "Strength:2,Agility:-1" means +2 Str, -1 Agi when equipped.
        /// Parsed at runtime by InventorySystem.
        /// </summary>
        public string EquipBonuses = "";

        /// <summary>
        /// Get the effective slots list. Returns UsesSlots if set, otherwise Slot.
        /// </summary>
        public string GetEffectiveSlots()
        {
            if (!string.IsNullOrEmpty(UsesSlots))
                return UsesSlots;
            return Slot;
        }

        /// <summary>
        /// Get the slot types as an array. Handles comma-separated values.
        /// </summary>
        public string[] GetSlotArray()
        {
            string slots = GetEffectiveSlots();
            if (string.IsNullOrEmpty(slots))
                return new string[] { "Hand" };
            return slots.Split(',');
        }

        /// <summary>
        /// How many slots of the primary type does this item require?
        /// (e.g. "Hand,Hand" = 2 Hand slots)
        /// </summary>
        public int GetSlotsRequired(string type)
        {
            string[] arr = GetSlotArray();
            int count = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].Trim() == type)
                    count++;
            }
            return count;
        }
    }
}
