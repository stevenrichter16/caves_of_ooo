using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CavesOfOoo.Core
{
    /// <summary>Counted presentation only. Each member remains an independent
    /// owner; physical stack quantities, commands and saved payloads are untouched.</summary>
    public sealed class InventoryItemGroup
    {
        public readonly IReadOnlyList<InventoryScreenData.ItemDisplay> Members;
        public readonly InventoryScreenData.ItemDisplay Selected;
        public readonly long TotalUnits, TotalWeight;
        internal InventoryItemGroup(List<InventoryScreenData.ItemDisplay> members, Entity preferred)
        {
            Members = members.AsReadOnly();
            Selected = members.FirstOrDefault(x => x.Item == preferred) ?? members[0];
            foreach (var item in members) { TotalUnits += item.StackCount; TotalWeight += item.Weight; }
        }
        public bool Contains(Entity item) => Members.Any(x => x.Item == item);
        public string Scope => Members.Count <= 1 ? "" : "Group: " + TotalUnits + " items in " + Members.Count
            + " stacks. Actions: selected " + Selected.StackCount + ". G shows individuals.";
    }

    public static class InventoryItemGroups
    {
        // Exact, audited built-ins only. This grants no physical stack/split or
        // processing permission. Unknown extensions stay independently visible.
        static readonly HashSet<Type> Known = new HashSet<Type>
        {
            typeof(RenderPart), typeof(PhysicsPart), typeof(ExaminablePart), typeof(CommercePart),
            typeof(StackerPart), typeof(MaterialPart), typeof(CompositionPart), typeof(HandlingPart),
            typeof(WeaponComponentPart), typeof(PreparationRecipePart), typeof(MaterialProcessInputPart),
            typeof(MeleeWeaponPart), typeof(CuttingToolPart), typeof(EquippablePart), typeof(ArmorPart),
            typeof(ThermalPart), typeof(FoodPart), typeof(TonicPart), typeof(BrewItemPart),
            typeof(StatusTonicPart), typeof(CureTonicPart), typeof(TorchLightPart), typeof(LightSourcePart),
            typeof(FuelPart), typeof(SeedPart), typeof(GrimoirePart), typeof(GrimoireChargePart),
            typeof(WaterskinPart), typeof(LiquidVesselPart), typeof(FlintStrikerPart), typeof(MineralPreparationPart),
            typeof(TorchRefuelPart), typeof(RepairablePart), typeof(WeaponTemperPart),
            typeof(EnhancementLacquered), typeof(EnhancementProofed), typeof(EnhancementSteeped),
            typeof(EnhancementSerrated), typeof(EnhancementGlowQuartz), typeof(EnhancementChoirIron),
            typeof(EnhancementPaleSalt), typeof(EnhancementEngraved)
        };
        static readonly Dictionary<Type, FieldInfo[]> Fields = Known.ToDictionary(t => t,
            t => t.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(f => f.Name, StringComparer.Ordinal).ToArray());
        // Exact merchant-only payloads: no grouping/stacking permission. Reader
        // properties, parsed reagent caches and flicker methods are never invoked.
        static readonly Dictionary<Type, FieldInfo[]> MerchantFields = new[]
        {
            typeof(ReagentPart), typeof(LightSourceFlickerPart), typeof(CookablePart), typeof(CampfirePart),
            typeof(HearthGlowPart), typeof(HearthRestPart), typeof(PackableFixturePart), typeof(GasGrenadePart)
        }.ToDictionary(t => t, t => t.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(f => f.Name, StringComparer.Ordinal).ToArray());

        public static List<InventoryItemGroup> Project(IEnumerable<InventoryScreenData.ItemDisplay> items, Entity preferred, bool grouped)
        {
            var lists = new List<List<InventoryScreenData.ItemDisplay>>();
            var map = new Dictionary<string, List<InventoryScreenData.ItemDisplay>>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (item == null) continue;
                List<InventoryScreenData.ItemDisplay> members;
                if (!grouped || string.IsNullOrEmpty(item.GroupingKey))
                    lists.Add(new List<InventoryScreenData.ItemDisplay> { item });
                else if (map.TryGetValue(item.GroupingKey, out members)) members.Add(item);
                else { members = new List<InventoryScreenData.ItemDisplay> { item }; map.Add(item.GroupingKey, members); lists.Add(members); }
            }
            return lists.Select(m => new InventoryItemGroup(m, preferred)).ToList();
        }

        internal static string CaptureIdentity(Entity actor, InventoryScreenData.ItemDisplay display) => CaptureIdentity(actor, display, false);

        // Consignment withdrawal also audits exact built-in mask/ink/document payloads.
        // Ordinary grouping keeps its original closed whitelist; this grants no
        // merge/split/crafting permission and accepts no payload subclass.
        internal static string CaptureMerchantIdentity(Entity actor, InventoryScreenData.ItemDisplay display) => CaptureIdentity(actor, display, true);

        static string CaptureIdentity(Entity actor, InventoryScreenData.ItemDisplay display, bool merchantExtras)
        {
            var item = display.Item; var pack = actor?.GetPart<InventoryPart>(); var physics = item?.GetPart<PhysicsPart>();
            var render = item?.GetPart<RenderPart>();
            if (display.IsEquipped || item == null || item.HasTag("Creature") || item.HasTag("Terrain")
                || string.IsNullOrEmpty(item.BlueprintName) || pack?.ParentEntity != actor || !pack.Objects.Contains(item)
                || physics?.ParentEntity != item || physics.InInventory != actor || physics.Equipped != null || !physics.Takeable
                || item.SpatialZone != null || render?.ParentEntity != item || !render.Visible || display.StackCount <= 0
                || MaterialProcessService.IsProtected(item)) return null;
            var vessel = item.GetPart<LiquidVesselPart>();
            if (vessel != null && (vessel.Capacity <= 0 || vessel.Volume < 0 || vessel.Volume > vessel.Capacity
                || (vessel.Volume == 0 ? vessel.LiquidId != "" : !LiquidRegistry.IsInitialized || LiquidRegistry.Get(vessel.LiquidId) == null))) return null;
            var skin = item.GetPart<WaterskinPart>();
            if (skin != null && (skin.Capacity <= 0 || skin.Charges < 0 || skin.Charges > skin.Capacity)) return null;
            var striker = item.GetPart<FlintStrikerPart>();
            if (striker != null && (striker.MaxStrikes <= 0 || striker.Strikes < 0 || striker.Strikes > striker.MaxStrikes)) return null;
            var ink = item.GetPart<GrimoireChargePart>();
            if (ink != null && (ink.MaxCharges <= 0 || ink.Charges < 0 || ink.Charges > ink.MaxCharges)) return null;
            var stack = item.GetPart<StackerPart>();
            if (stack != null && (stack.MaxStack <= 0 || stack.StackCount > stack.MaxStack)) return null;
            var key = new StringBuilder();
            Add(key, item.BlueprintName); Add(key, UnitName(display)); Add(key, display.Category); Add(key, display.State);
            if (!Map(key, item.Tags) || !Map(key, item.Properties) || !Map(key, item.IntProperties) || item.Statistics == null) return null;
            Add(key, item.Statistics.Count);
            foreach (var pair in item.Statistics.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var stat = pair.Value; if (stat == null || stat.GetType() != typeof(Stat) || stat.Owner != item) return null;
                Add(key, pair.Key); Add(key, stat.Name); Add(key, stat.sValue); Add(key, stat.BaseValue);
                Add(key, stat.Bonus); Add(key, stat.Penalty); Add(key, stat.Boost); Add(key, stat.Min); Add(key, stat.Max);
            }
            Add(key, "PARTS");
            var types = new HashSet<Type>();
            foreach (var part in item.Parts)
            {
                if (part?.ParentEntity != item) return null;
                if (MaterialProcessPayloadPolicies.IsInertTracking(part)) continue;
                var type = part.GetType();
                bool mask = merchantExtras && type == typeof(GasMaskPart);
                bool vial = merchantExtras && type == typeof(InkVialPart);
                bool document = merchantExtras && type == typeof(ReadableDocumentPart);
                FieldInfo[] extraFields = null;
                bool extra = merchantExtras && MerchantFields.TryGetValue(type, out extraFields);
                if ((!Known.Contains(type) && !mask && !vial && !document && !extra) || !types.Add(type)) return null;
                Add(key, type.FullName);
                if (mask) { Add(key, "Power"); Add(key, ((GasMaskPart)part).Power); continue; }
                if (vial) { Add(key, "InkAmount"); Add(key, ((InkVialPart)part).InkAmount); continue; }
                if (document) { Add(key, "DocumentId"); Add(key, ((ReadableDocumentPart)part).DocumentId); continue; }
                foreach (var field in extra ? extraFields : Fields[type])
                {
                    if (field.Name == "ParentEntity" || type == typeof(PhysicsPart) && (field.Name == "InInventory" || field.Name == "Equipped")
                        || type == typeof(StackerPart) && field.Name == "StackCount") continue;
                    var value = field.GetValue(part);
                    if (type == typeof(RepairablePart) && field.Name == "RepairedBy") { if (value != null) return null; continue; }
                    Add(key, field.Name);
                    if (type == typeof(MaterialPart) && field.Name == "MaterialTags")
                    {
                        var tags = value as HashSet<string>; if (tags == null) return null;
                        Add(key, tags.Count);
                        foreach (var tag in tags.OrderBy(s => s, StringComparer.Ordinal)) Add(key, tag);
                        continue;
                    }
                    if (!Scalar(key, field.FieldType, value)) return null;
                }
                if (key.Length > 16384) return null;
            }
            return key.Length <= 16384 ? key.ToString() : null;
        }
        public static string UnitName(InventoryScreenData.ItemDisplay item)
        {
            string name = item.Name ?? "", suffix = " (x" + item.StackCount + ")";
            return item.StackCount > 1 && name.EndsWith(suffix, StringComparison.Ordinal) ? name.Substring(0, name.Length - suffix.Length) : name;
        }
        static bool Map<T>(StringBuilder key, IDictionary<string, T> values)
        {
            if (values == null) return false;
            Add(key, values.Count);
            foreach (var pair in values.OrderBy(p => p.Key, StringComparer.Ordinal))
            { Add(key, pair.Key); if (!Scalar(key, typeof(T), pair.Value)) return false; }
            return true;
        }
        static bool Scalar(StringBuilder key, Type type, object value)
        {
            if (type == typeof(string)) { Add(key, (string)value); return true; }
            if (type != typeof(bool) && type != typeof(int) && type != typeof(long) && type != typeof(float) && type != typeof(double) && !type.IsEnum) return false;
            if (value is float f && (float.IsNaN(f) || float.IsInfinity(f)) || value is double d && (double.IsNaN(d) || double.IsInfinity(d))) return false;
            if (value is float exactFloat) { Add(key, exactFloat.ToString("R", CultureInfo.InvariantCulture)); return true; }
            if (value is double exactDouble) { Add(key, exactDouble.ToString("R", CultureInfo.InvariantCulture)); return true; }
            Add(key, Convert.ToString(value, CultureInfo.InvariantCulture)); return true;
        }
        static void Add(StringBuilder key, object value)
        {
            if (value == null) { key.Append("-1:"); return; }
            string s = Convert.ToString(value, CultureInfo.InvariantCulture);
            key.Append(s.Length).Append(':').Append(s);
        }
    }
}
