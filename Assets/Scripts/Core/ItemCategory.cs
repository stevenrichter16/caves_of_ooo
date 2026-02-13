using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Canonical item categories for inventory display and sorting.
    /// Mirrors Qud's Physics.Category values and InventoryCategory display order.
    /// Categories are strings on PhysicsPart.Category, set via blueprints.
    /// </summary>
    public static class ItemCategory
    {
        // Category name constants
        public const string MeleeWeapons = "Melee Weapons";
        public const string MissileWeapons = "Missile Weapons";
        public const string ThrownWeapons = "Thrown Weapons";
        public const string Armor = "Armor";
        public const string Shields = "Shields";
        public const string Clothes = "Clothes";
        public const string Ammo = "Ammo";
        public const string Food = "Food";
        public const string Tonics = "Tonics";
        public const string LightSources = "Light Sources";
        public const string Tools = "Tools";
        public const string Grenades = "Grenades";
        public const string EnergyCells = "Energy Cells";
        public const string Books = "Books";
        public const string Artifacts = "Artifacts";
        public const string TradeGoods = "Trade Goods";
        public const string QuestItems = "Quest Items";
        public const string NaturalWeapons = "Natural Weapons";
        public const string Corpses = "Corpses";
        public const string Miscellaneous = "Miscellaneous";
        public const string Unknown = "Unknown";

        /// <summary>
        /// Sort order for categories. Lower = displayed first.
        /// </summary>
        private static readonly Dictionary<string, int> SortOrder = new Dictionary<string, int>
        {
            { MeleeWeapons, 10 },
            { MissileWeapons, 20 },
            { ThrownWeapons, 25 },
            { Ammo, 30 },
            { Armor, 40 },
            { Shields, 45 },
            { Clothes, 50 },
            { LightSources, 60 },
            { Food, 70 },
            { Tonics, 80 },
            { Grenades, 90 },
            { EnergyCells, 100 },
            { Tools, 110 },
            { Books, 120 },
            { Artifacts, 130 },
            { TradeGoods, 140 },
            { QuestItems, 150 },
            { NaturalWeapons, 160 },
            { Corpses, 170 },
            { Miscellaneous, 900 },
            { Unknown, 999 },
        };

        /// <summary>
        /// Get the sort order for a category name.
        /// Unknown categories sort to the end.
        /// </summary>
        public static int GetSortOrder(string category)
        {
            if (string.IsNullOrEmpty(category)) return 999;
            if (SortOrder.TryGetValue(category, out int order))
                return order;
            return 998;
        }

        /// <summary>
        /// Get the category of an entity from its PhysicsPart.Category.
        /// Falls back to inferring from parts if not set.
        /// </summary>
        public static string GetCategory(Entity entity)
        {
            if (entity == null) return Unknown;

            var physics = entity.GetPart<PhysicsPart>();
            if (physics != null && !string.IsNullOrEmpty(physics.Category))
                return physics.Category;

            // Infer from parts if Category not explicitly set
            if (entity.HasPart<MeleeWeaponPart>())
            {
                if (entity.HasTag("Natural"))
                    return NaturalWeapons;
                return MeleeWeapons;
            }
            if (entity.HasPart<ArmorPart>() && entity.HasPart<EquippablePart>())
            {
                var equip = entity.GetPart<EquippablePart>();
                if (equip.Slot == "Body" || equip.Slot == "Head" || equip.Slot == "Feet")
                    return Armor;
                if (equip.Slot == "Back")
                    return Clothes;
            }

            return Miscellaneous;
        }

        /// <summary>
        /// Compare two category names for sorting.
        /// </summary>
        public static int Compare(string a, string b)
        {
            return GetSortOrder(a).CompareTo(GetSortOrder(b));
        }
    }
}
