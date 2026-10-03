using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Repairs only the missing family of the original authored steel head.
    /// Saved stats, effects, paid tempering and item identity remain literal;
    /// rebuilding from present-day component blueprints would erase those values.
    /// </summary>
    internal static class SteelBladeFamilyCompatibility
    {
        internal static void RestoreComponent(WeaponComponentPart component)
        {
            if (component?.ParentEntity?.BlueprintName != "SteelBladeComponent"
                || component.Slot != WeaponForgingService.BladeSlot) return;
            component.Attributes = RestoreAttributes(component.Attributes);
        }

        internal static void RestoreAssembly(WeaponAssemblyPart assembly)
        {
            if (assembly?.ParentEntity?.BlueprintName != WeaponForgingService.ForgedWeaponBlueprintName
                || assembly.BladeBlueprint != "SteelBladeComponent") return;
            var melee = assembly.ParentEntity.GetPart<MeleeWeaponPart>();
            if (melee != null) melee.Attributes = RestoreAttributes(melee.Attributes);
        }

        private static string RestoreAttributes(string attributes)
        {
            if (string.IsNullOrWhiteSpace(attributes)) return attributes;
            bool cutting = false;
            foreach (string token in attributes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // An explicitly chosen family belongs to the saved item, even
                // when it is unusual for its recorded component blueprint.
                if (token.Equals("LongBlades", StringComparison.OrdinalIgnoreCase)
                    || token.Equals("Axe", StringComparison.OrdinalIgnoreCase)
                    || token.Equals("Cudgel", StringComparison.OrdinalIgnoreCase)
                    || token.Equals("ShortBlades", StringComparison.OrdinalIgnoreCase)) return attributes;
                if (token.Equals("Cutting", StringComparison.OrdinalIgnoreCase)) cutting = true;
            }
            return cutting ? attributes + " LongBlades" : attributes;
        }
    }
}
