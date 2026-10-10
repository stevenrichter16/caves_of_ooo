using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Builds the pure data snapshot consumed by the gameplay hotbar renderer.
    /// </summary>
    public static class HotbarStateBuilder
    {
        // Fixed-size presentation caches retain strings only, never players or
        // abilities. Gameplay fields are public/mutable, so validate their exact
        // visible values on every call instead of relying on event invalidation.
        // Like the other HUD builders, this is used on the main thread only.
        private static readonly HotbarSlotSnapshot[] SlotScratch =
            new HotbarSlotSnapshot[GameplayHotbarLayout.SlotCount];
        private static readonly string[] DisplayNames = new string[GameplayHotbarLayout.SlotCount];
        private static readonly string[] ShortNames = new string[GameplayHotbarLayout.SlotCount];
        private static readonly char[] Glyphs = new char[GameplayHotbarLayout.SlotCount];
        private static HotbarSnapshot _cachedSnapshot;

        public static HotbarSnapshot Build(Entity player, int selectedSlot, ActivatedAbility pendingAbility)
        {
            var abilities = player?.GetPart<ActivatedAbilitiesPart>();
            var slots = SlotScratch;
            int pendingSlot = pendingAbility != null && abilities != null
                ? abilities.GetSlotForAbility(pendingAbility.ID)
                : -1;

            for (int slot = 0; slot < GameplayHotbarLayout.SlotCount; slot++)
            {
                ActivatedAbility ability = abilities?.GetAbilityBySlot(slot);
                bool occupied = ability != null;
                GrimoireTooltip tooltip = occupied
                    ? GrimoireTooltipData.GetOrDefault(ability.SourcePowerClass)
                    : default;
                string displayName = occupied
                    ? (!string.IsNullOrEmpty(tooltip.DisplayName) ? tooltip.DisplayName : ability.DisplayName)
                    : string.Empty;
                displayName = displayName ?? string.Empty;
                if (occupied && DisplayNames[slot] != displayName)
                {
                    DisplayNames[slot] = displayName;
                    ShortNames[slot] = BuildShortName(displayName);
                    Glyphs[slot] = BuildGlyph(displayName);
                }

                slots[slot] = new HotbarSlotSnapshot(
                    slot,
                    SlotToHotkey(slot),
                    displayName,
                    occupied ? ShortNames[slot] : "empty",
                    tooltip.ColorCode,
                    tooltip.Mechanics,
                    occupied ? Glyphs[slot] : '.',
                    ability?.CooldownRemaining ?? 0,
                    occupied,
                    slot == selectedSlot,
                    slot == pendingSlot,
                    ability?.IsUsable ?? false);
            }

            if (_cachedSnapshot != null && _cachedSnapshot.SelectedSlot == selectedSlot &&
                _cachedSnapshot.PendingSlot == pendingSlot)
            {
                bool unchanged = true;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (!SlotsEqual(slots[i], _cachedSnapshot.Slots[i]))
                    {
                        unchanged = false;
                        break;
                    }
                }
                if (unchanged)
                    return _cachedSnapshot;
            }

            int summarySlot = pendingSlot >= 0 ? pendingSlot : selectedSlot;
            string summaryText = BuildSummaryText(slots, summarySlot);

            // A retained snapshot must not point at the next call's scratch data.
            // Wrap the copy to prevent callers from downcasting and mutating it.
            _cachedSnapshot = new HotbarSnapshot(
                "GRIMOIRES",
                summaryText,
                "[] cycle  [Enter] cast",
                System.Array.AsReadOnly((HotbarSlotSnapshot[])slots.Clone()),
                selectedSlot,
                pendingSlot);
            return _cachedSnapshot;
        }

        internal static bool SlotsEqual(HotbarSlotSnapshot left, HotbarSlotSnapshot right)
        {
            return left.SlotIndex == right.SlotIndex && left.Hotkey == right.Hotkey &&
                left.DisplayName == right.DisplayName && left.ShortName == right.ShortName &&
                left.AccentColorCode == right.AccentColorCode && left.MechanicsText == right.MechanicsText &&
                left.Glyph == right.Glyph && left.CooldownRemaining == right.CooldownRemaining &&
                left.Occupied == right.Occupied && left.Selected == right.Selected &&
                left.Pending == right.Pending && left.Usable == right.Usable;
        }

        public static char SlotToHotkey(int slot)
        {
            if (slot >= 0 && slot <= 8)
                return (char)('1' + slot);
            if (slot == 9)
                return '0';
            return '?';
        }

        private static string BuildSummaryText(IReadOnlyList<HotbarSlotSnapshot> slots, int summarySlot)
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
