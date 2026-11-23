using System;
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
        public const string Trinkets = "Trinkets";
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
        public const string Seeds = "Seeds";
        public const string Reagents = "Reagents";
        public const string Components = "Components";
        public const string Materials = "Materials";
        public const string Containers = "Containers";
        public const string CampEquipment = "Camp Equipment";
        public const string Documents = "Documents";

        // Named ordinary supplies with no more specific public capability Part.
        // Do not infer materials from arbitrary composition or hidden enhancements.
        static readonly HashSet<string> MaterialItems = new HashSet<string>(StringComparer.Ordinal)
        { "FireClay", "SalvagedTimber", "KnotflaxCord", "Tepuibone", "Bone", "InertSludge", "SilverSand" };
        static readonly HashSet<string> DocumentItems = new HashSet<string>(StringComparer.Ordinal)
        { "WellMaintenanceManual", "OvenBuildersGuide", "LanternOilRecipe" };
        static readonly HashSet<string> PreparedOils = new HashSet<string>(StringComparer.Ordinal)
        { "FrogOil", "WardOil", "LanternOil" };

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
            { Trinkets, 55 },
            { LightSources, 60 },
            { Food, 70 },
            { Seeds, 75 },
            { Tonics, 80 },
            { Reagents, 85 },
            { Grenades, 90 },
            { EnergyCells, 100 },
            { Components, 105 },
            { Materials, 107 },
            { Containers, 109 },
            { Tools, 110 },
            { CampEquipment, 115 },
            { Books, 120 },
            { Documents, 125 },
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
            if (physics != null && !string.IsNullOrEmpty(physics.Category) && physics.Category != Miscellaneous)
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

            // Refine the old empty/Miscellaneous fallback for existing saves as
            // well as new goods. Presence checks never invoke use/state providers
            // or rewrite the entity. Primary use wins over incidental living light.
            if (entity.HasPart<PackableFixturePart>()) return CampEquipment;
            if (entity.HasPart<WeaponComponentPart>()) return Components;
            if (entity.HasPart<LiquidVesselPart>() || entity.HasPart<WaterskinPart>()) return Containers;
            if (entity.HasPart<GasGrenadePart>()) return Grenades;
            if (entity.HasPart<TonicPart>() || entity.HasPart<CureTonicPart>() || entity.HasPart<StatusTonicPart>()) return Tonics;
            if (entity.HasPart<GrimoirePart>()) return Books;
            if (entity.HasPart<ReadableDocumentPart>() || entity.HasPart<SchematicPart>() || DocumentItems.Contains(entity.BlueprintName ?? "")) return Documents;
            if (entity.HasPart<ScraperPart>() || entity.HasPart<SpringTrapKitPart>() || entity.HasPart<SoundingRodPart>()
                || entity.HasPart<FlintStrikerPart>() || entity.HasPart<LayableDevicePart>()) return Tools;
            if (entity.HasPart<SeedPart>()) return Seeds;
            if (entity.HasPart<MaterialProcessInputPart>() || entity.HasPart<MineralPreparationPart>() || entity.HasPart<BotanicalProcessingPart>()
                || MaterialItems.Contains(entity.BlueprintName ?? "")) return Materials;
            if (entity.HasPart<ReagentPart>() || PreparedOils.Contains(entity.BlueprintName ?? "")) return Reagents;
            if (entity.HasPart<LightSourcePart>() || entity.HasPart<TorchLightPart>()) return LightSources;

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
