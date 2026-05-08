using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// WSP8.0 — Builds <see cref="AbilityManagerSnapshot"/> from an
    /// actor's <see cref="ActivatedAbilitiesPart"/>. Pure function
    /// (entity in, snapshot out) — testable in EditMode without
    /// MonoBehaviour setup.
    ///
    /// <para>Sort order: by <see cref="ActivatedAbility.Class"/> first
    /// (so all "Skills" group together, then "Grimoire Spells", etc.),
    /// then by <see cref="ActivatedAbility.DisplayName"/> within the
    /// class for deterministic ordering. Mirrors Qud's
    /// <c>AbilityManagerScreen.SortMode.Class</c> default
    /// (AbilityManager.cs:96-99).</para>
    ///
    /// <para>Hotkey resolution: queries
    /// <see cref="ActivatedAbilitiesPart.GetSlotForAbility"/> for each
    /// ability and translates the slot index (0-9) to the displayed
    /// hotkey character ('1'-'9' for slots 0-8, '0' for slot 9, '-'
    /// for unbound). Mirrors the hotkey logic in
    /// <see cref="HotbarStateBuilder.SlotToHotkey"/>.</para>
    /// </summary>
    public static class AbilityManagerStateBuilder
    {
        /// <summary>
        /// Build a snapshot from the actor's
        /// <see cref="ActivatedAbilitiesPart"/>. Returns an empty
        /// snapshot when the actor lacks the part — callers can
        /// render an "you know no rites" empty state without null
        /// checks.
        /// </summary>
        public static AbilityManagerSnapshot Build(Entity actor)
        {
            if (actor == null)
                return new AbilityManagerSnapshot(null);

            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null || abilities.AbilityList == null)
                return new AbilityManagerSnapshot(null);

            int abilityCount = abilities.AbilityList.Count;
            if (abilityCount == 0)
                return new AbilityManagerSnapshot(null);

            // Snapshot each ability + its derived UI state.
            var rows = new List<AbilityManagerRow>(abilityCount);
            for (int i = 0; i < abilityCount; i++)
            {
                var ability = abilities.AbilityList[i];
                if (ability == null) continue;

                int slot = abilities.GetSlotForAbility(ability.ID);
                char hotkey = SlotToHotkey(slot);

                rows.Add(new AbilityManagerRow(
                    abilityID: ability.ID,
                    displayName: ability.DisplayName,
                    sourceClass: ability.Class,
                    hotkey: hotkey,
                    slotIndex: slot,
                    cooldownRemaining: ability.CooldownRemaining,
                    maxCooldown: ability.MaxCooldown,
                    isUsable: ability.IsUsable));
            }

            // Stable sort: by SourceClass first, then DisplayName.
            // List<T>.Sort uses an unstable QuickSort, so we use a
            // composite comparer to make ordering fully deterministic
            // (otherwise abilities with the same Class+Name could
            // shuffle frame-to-frame in tests).
            rows.Sort((a, b) =>
            {
                int classCmp = string.Compare(a.SourceClass, b.SourceClass,
                    System.StringComparison.OrdinalIgnoreCase);
                if (classCmp != 0) return classCmp;
                return string.Compare(a.DisplayName, b.DisplayName,
                    System.StringComparison.OrdinalIgnoreCase);
            });

            return new AbilityManagerSnapshot(rows);
        }

        /// <summary>Convert a slot index (0-9, or -1 for unbound) to
        /// the displayed hotkey character. Mirrors
        /// <see cref="HotbarStateBuilder.SlotToHotkey"/> with the
        /// extension that -1 → '-' (unbound).</summary>
        public static char SlotToHotkey(int slot)
        {
            if (slot >= 0 && slot <= 8) return (char)('1' + slot);
            if (slot == 9) return '0';
            return '-';
        }

        /// <summary>Convert a hotkey character ('1'-'9', '0') to its
        /// slot index. Returns -1 for unrecognized characters.
        /// Inverse of <see cref="SlotToHotkey"/>.</summary>
        public static int HotkeyToSlot(char hotkey)
        {
            if (hotkey >= '1' && hotkey <= '9') return hotkey - '1';
            if (hotkey == '0') return 9;
            return -1;
        }

        /// <summary>
        /// Synthesize a 1-line description for a row, covering binding +
        /// usability state. <see cref="ActivatedAbility"/> doesn't carry
        /// a Description field today (CoO simplification of Qud's
        /// <c>ActivatedAbilityEntry.Description</c>) so the manager UI
        /// generates a useful status string from the row's
        /// <see cref="AbilityManagerRow.SlotIndex"/> +
        /// <see cref="AbilityManagerRow.IsUsable"/> +
        /// <see cref="AbilityManagerRow.CooldownRemaining"/>.
        ///
        /// <para>Returns:
        /// <list type="bullet">
        ///   <item>Bound + ready: <c>"Bound to [N] - ready to use."</c></item>
        ///   <item>Bound + cooldown: <c>"Bound to [N] - cooldown: NT/MaxT."</c></item>
        ///   <item>Unbound + ready: <c>"Unbound. Press 0-9 to assign a slot, Enter to cast."</c></item>
        ///   <item>Unbound + cooldown: <c>"Unbound - cooldown: NT/MaxT."</c></item>
        /// </list></para>
        ///
        /// <para>Public + static so tests can pin the per-row format
        /// without spinning up the MonoBehaviour. The wording is part of
        /// the player-facing UX contract — if a future contributor
        /// changes it, the test catches the drift.</para>
        /// </summary>
        public static string BuildRowDescription(AbilityManagerRow row)
        {
            bool bound = row.SlotIndex >= 0;
            if (bound && row.IsUsable)
                return "Bound to [" + row.Hotkey + "] - ready to use.";
            if (bound && !row.IsUsable)
                return "Bound to [" + row.Hotkey + "] - cooldown: " +
                       row.CooldownRemaining + "T / " + row.MaxCooldown + "T.";
            if (!bound && row.IsUsable)
                return "Unbound. Press 0-9 to assign a slot, Enter to cast.";
            return "Unbound - cooldown: " +
                   row.CooldownRemaining + "T / " + row.MaxCooldown + "T.";
        }
    }
}
