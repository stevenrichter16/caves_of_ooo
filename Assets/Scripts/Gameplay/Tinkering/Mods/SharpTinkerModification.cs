using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// V1 concrete modification: adds +1 penetration to melee weapons.
    /// </summary>
    public sealed class SharpTinkerModification : ITinkerModification
    {
        private const string ModTag = "ModSharp";

        public string Id => "mod_sharp";

        public string DisplayName => "Sharp";

        public bool CanApply(Entity item, out string reason)
        {
            reason = string.Empty;
            if (item == null)
            {
                reason = "Target item is missing.";
                return false;
            }

            var weapon = item.GetPart<MeleeWeaponPart>();
            if (weapon == null)
            {
                reason = "Sharp can only be applied to melee weapons.";
                return false;
            }

            if (item.HasTag(ModTag))
            {
                reason = "Item is already sharp.";
                return false;
            }

            var stacker = item.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                reason = "Split the stack before applying a modification.";
                return false;
            }

            return true;
        }

        public bool Apply(Entity item, out string reason)
        {
            reason = string.Empty;
            if (!CanApply(item, out reason))
                return false;

            var weapon = item.GetPart<MeleeWeaponPart>();
            weapon.PenBonus += 1;

            // Keep result visible in inventory names.
            var render = item.GetPart<RenderPart>();
            if (render != null && !string.IsNullOrWhiteSpace(render.DisplayName))
            {
                string current = render.DisplayName.Trim();
                if (!current.StartsWith("sharp ", StringComparison.OrdinalIgnoreCase))
                    render.DisplayName = "sharp " + current;
            }

            item.SetTag(ModTag, string.Empty);
            item.ModIntProperty("ModificationCount", 1);
            return true;
        }
    }
}
