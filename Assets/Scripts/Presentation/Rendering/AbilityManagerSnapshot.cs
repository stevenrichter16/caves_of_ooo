using System.Collections.Generic;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// WSP8.0 — Pure-data snapshot consumed by the
    /// <see cref="UI.AbilityManagerUI"/> renderer. Mirrors the
    /// established CoO modal-UI pattern: state-builder is testable
    /// in EditMode without Tilemap setup, then the MonoBehaviour
    /// renderer consumes the snapshot.
    ///
    /// <para>Qud-parity reference: <c>Qud.UI.AbilityManagerScreen</c>
    /// (Qud.UI/AbilityManagerScreen.cs). Qud's screen has the same
    /// shape — list of ability rows + per-row state (hotkey binding,
    /// cooldown, etc.). CoO's MVP defers Qud's fuzzy search +
    /// custom-sort-mode toggle; sort is always by Class then
    /// DisplayName for deterministic ordering.</para>
    /// </summary>
    public readonly struct AbilityManagerSnapshot
    {
        public readonly IReadOnlyList<AbilityManagerRow> Rows;
        public readonly int RowCount;

        public AbilityManagerSnapshot(IReadOnlyList<AbilityManagerRow> rows)
        {
            Rows = rows ?? System.Array.Empty<AbilityManagerRow>();
            RowCount = Rows.Count;
        }
    }

    /// <summary>
    /// One row in the ability manager. Represents an
    /// <see cref="Core.ActivatedAbility"/> with derived UI state
    /// (hotkey character + cooldown display + usable flag). The
    /// <see cref="AbilityID"/> is the canonical identifier the UI
    /// uses to call back into <see cref="Core.ActivatedAbilitiesPart"/>
    /// for slot-binding mutations or activation.
    /// </summary>
    public readonly struct AbilityManagerRow
    {
        /// <summary>The Guid of the underlying ActivatedAbility — used
        /// by the UI's reorder + activate callbacks. Stable across
        /// snapshots (rebuilt every Open).</summary>
        public readonly System.Guid AbilityID;

        /// <summary>Display name from <see cref="Core.ActivatedAbility.DisplayName"/>
        /// (e.g. "Slam", "Conk", "Fire Bolt").</summary>
        public readonly string DisplayName;

        /// <summary>Source class (e.g. "Skills", "Grimoire Spells",
        /// "Stances"). Used as the sort-group header in the UI.</summary>
        public readonly string SourceClass;

        /// <summary>
        /// Hotkey character bound to this ability. <c>'1'</c>..<c>'9'</c>
        /// for slots 0-8, <c>'0'</c> for slot 9, or <c>'-'</c> when the
        /// ability is owned but unbound (no slot assigned). The UI
        /// renders this in the hotkey column at the start of each row.
        /// </summary>
        public readonly char Hotkey;

        /// <summary>Slot index this ability is bound to: 0-9 or -1
        /// for unbound. Mirrors <see cref="Core.ActivatedAbilitiesPart.GetSlotForAbility"/>.</summary>
        public readonly int SlotIndex;

        /// <summary>Cooldown turns remaining. 0 if usable.</summary>
        public readonly int CooldownRemaining;

        /// <summary>Maximum cooldown for the displayed reference (so
        /// the UI can render "X / Max" if desired).</summary>
        public readonly int MaxCooldown;

        /// <summary>True when <see cref="CooldownRemaining"/> &lt;= 0.</summary>
        public readonly bool IsUsable;

        public AbilityManagerRow(System.Guid abilityID, string displayName,
            string sourceClass, char hotkey, int slotIndex,
            int cooldownRemaining, int maxCooldown, bool isUsable)
        {
            AbilityID = abilityID;
            DisplayName = displayName ?? "";
            SourceClass = sourceClass ?? "";
            Hotkey = hotkey;
            SlotIndex = slotIndex;
            CooldownRemaining = cooldownRemaining;
            MaxCooldown = maxCooldown;
            IsUsable = isUsable;
        }
    }
}
