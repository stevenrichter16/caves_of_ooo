using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Builds the pure data snapshot consumed by the gameplay hotbar renderer.
    /// </summary>
    public static class HotbarStateBuilder
    {
        public static HotbarSnapshot Build(Entity player, int selectedSlot, ActivatedAbility pendingAbility)
        {
            var abilities = player?.GetPart<ActivatedAbilitiesPart>();
            var slots = new List<HotbarSlotSnapshot>(GameplayHotbarLayout.SlotCount);
            int pendingSlot = pendingAbility != null && abilities != null
                ? abilities.GetSlotForAbility(pendingAbility.ID)
                : -1;

            for (int slot = 0; slot < GameplayHotbarLayout.SlotCount; slot++)
            {
                ActivatedAbility ability = abilities?.GetAbilityBySlot(slot);
                bool occupied = ability != null;
                GrimoireTooltip tooltip = occupied
                    ? GrimoireTooltipData.GetOrDefault(ability.SourceMutationClass)
                    : default;
                string displayName = occupied
                    ? (!string.IsNullOrEmpty(tooltip.DisplayName) ? tooltip.DisplayName : ability.DisplayName)
                    : string.Empty;
                string shortName = occupied
                    ? BuildShortName(displayName)
                    : "empty";

                slots.Add(new HotbarSlotSnapshot(
                    slot,
                    SlotToHotkey(slot),
                    displayName,
                    shortName,
                    tooltip.ColorCode,
                    tooltip.Mechanics,
                    occupied ? BuildGlyph(displayName) : '.',
                    ability?.CooldownRemaining ?? 0,
                    occupied,
                    slot == selectedSlot,
                    slot == pendingSlot,
                    ability?.IsUsable ?? false));
            }

            int summarySlot = pendingSlot >= 0 ? pendingSlot : selectedSlot;
            string summaryText = BuildSummaryText(slots, summarySlot);

            return new HotbarSnapshot(
                "GRIMOIRES",
                summaryText,
                "[] cycle  [Enter] cast",
                slots,
                selectedSlot,
                pendingSlot);
        }

        public static char SlotToHotkey(int slot)
        {
            if (slot >= 0 && slot <= 8)
                return (char)('1' + slot);
            if (slot == 9)
                return '0';
            return '?';
        }

        private static string BuildSummaryText(List<HotbarSlotSnapshot> slots, int summarySlot)
        {
            if (summarySlot < 0 || summarySlot >= slots.Count)
                return "No rite bound. Use the Abilities tab to assign one.";

            HotbarSlotSnapshot slot = slots[summarySlot];
            if (!slot.Occupied)
                return "No rite bound. Use the Abilities tab to assign one.";

            string prefix = "[" + slot.Hotkey + "] " + slot.DisplayName;
            if (slot.Pending)
                return prefix + " - choose a direction.";
            if (!slot.Usable)
                return prefix + " - CD " + slot.CooldownRemaining;
            if (!string.IsNullOrWhiteSpace(slot.MechanicsText))
                return prefix + " - " + slot.MechanicsText;
            return prefix + " - ready.";
        }

        private static string BuildShortName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;

            string[] words = displayName.Split(' ');
            if (words.Length > 1)
            {
                var condensed = new System.Text.StringBuilder();
                for (int i = 0; i < words.Length && condensed.Length < 6; i++)
                {
                    if (string.IsNullOrEmpty(words[i]))
                        continue;
                    condensed.Append(char.ToUpperInvariant(words[i][0]));
                }

                if (condensed.Length > 0)
                    return condensed.ToString();
            }

            string compact = displayName.Replace(" ", string.Empty);
            return compact.Length <= 6 ? compact : compact.Substring(0, 6);
        }

        private static char BuildGlyph(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return '?';

            for (int i = 0; i < displayName.Length; i++)
            {
                char c = displayName[i];
                if (char.IsLetterOrDigit(c))
                    return char.ToUpperInvariant(c);
            }

            return '?';
        }
    }
}
