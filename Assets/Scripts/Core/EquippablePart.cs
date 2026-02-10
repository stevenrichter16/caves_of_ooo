namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an item as equippable and defines which equipment slot it uses.
    /// Slot is a string like "Hand", "Body", "Head", "Feet".
    /// Optional stat bonuses applied on equip via EquipBonuses string.
    /// </summary>
    public class EquippablePart : Part
    {
        public override string Name => "Equippable";

        /// <summary>
        /// Which slot this item equips to (e.g. "Hand", "Body", "Head", "Feet").
        /// </summary>
        public string Slot = "Hand";

        /// <summary>
        /// Stat bonuses applied when equipped, as comma-separated "StatName:Amount" pairs.
        /// Example: "Strength:2,Agility:-1" means +2 Str, -1 Agi when equipped.
        /// Parsed at runtime by InventorySystem.
        /// </summary>
        public string EquipBonuses = "";
    }
}