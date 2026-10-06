using System;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>Exact live portable/assembly identity for the regional equipment kit.
    /// Inventory and world callers independently validate the actual owner graph.</summary>
    public static class EquipmentDiscoveryRecipes
    {
        public const string Prefix = "equipment-discovery-";
        public static string HeadForm(string blueprint)
        {
            switch (blueprint) {
                case "PeatMalletHeadComponent": case "TepuiboneHeadComponent": return "peatmallet";
                case "CinderhookAxeHeadComponent": return "cinderhook";
                case "CounterweightLongBladeComponent": return "counterweight";
                default: return null;
            }
        }
        public static bool HandlesBlueprint(string blueprint) => HeadForm(blueprint) != null || blueprint == "GroundwireScreen" || blueprint == "KilnfeltApron";
        public static bool Handles(Entity owner) => owner != null && (HandlesBlueprint(owner.BlueprintName)
            || owner.BlueprintName == "ForgedWeapon" && HeadForm(owner.GetPart<WeaponAssemblyPart>()?.BladeBlueprint) != null);
        public static bool TryRecipe(Entity owner, out string model)
        {
            model = null;
            if (!Handles(owner) || owner.HasTag("Natural") || owner.HasTag("Creature") || !owner.HasTag("Item")) return false;
            var physics = owner.GetPart<PhysicsPart>(); var render = owner.GetPart<RenderPart>();
            if (physics == null || physics.ParentEntity != owner || !physics.Takeable || render == null || render.ParentEntity != owner || !render.Visible
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || (owner.GetPart<StackerPart>()?.StackCount ?? 1) <= 0) return false;
            string head = HeadForm(owner.BlueprintName);
            if (head != null) {
                var component = owner.GetPart<WeaponComponentPart>();
                if (component == null || component.ParentEntity != owner || component.Slot != "Blade") return false;
                model = Prefix + "head-" + head; return true;
            }
            if (owner.BlueprintName == "ForgedWeapon") {
                var assembly = owner.GetPart<WeaponAssemblyPart>(); var weapon = owner.GetPart<MeleeWeaponPart>();
                if (assembly == null || assembly.ParentEntity != owner || weapon == null || weapon.ParentEntity != owner) return false;
                string haft = FiftySecondVisualAliases.HaftForm(assembly.HaftBlueprint);
                string binding = FiftySecondVisualAliases.BindingForm(assembly.BindingBlueprint);
                head = HeadForm(assembly.BladeBlueprint);
                if (head == null || haft == null || binding == null) return false;
                model = Prefix + "forged-" + head + "-" + haft + "-" + binding; return true;
            }
            bool screen = owner.BlueprintName == "GroundwireScreen";
            var armor = owner.GetPart<ArmorPart>(); var equip = owner.GetPart<EquippablePart>();
            if (armor == null || armor.ParentEntity != owner || equip == null || equip.ParentEntity != owner || equip.Slot != (screen ? "Hand" : "Body")) return false;
            model = Prefix + (screen ? "groundwire-screen" : "kilnfelt-apron"); return true;
        }
        public static string WornModel(string blueprint) => blueprint == "KilnfeltApron" ? Prefix + "worn-kilnfelt-apron" : null;
    }
}
