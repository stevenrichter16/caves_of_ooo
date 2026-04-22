using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Popup state for the grimoire picker invoked from the Abilities tab in
    /// <see cref="InventoryUI"/>. Holds the list of learned grimoires, the
    /// currently-highlighted row, and the target slot the player is rebinding.
    /// Rendering and input handling live on <see cref="InventoryUI"/> so the
    /// picker reuses the inventory's drawing primitives and popup style, which
    /// avoids the cross-tilemap z-order complications that would arise from
    /// making this a separate MonoBehaviour the way <c>ContainerPickerUI</c> is.
    ///
    /// The callback model is intentional: when the player picks a grimoire,
    /// <see cref="SelectionCallback"/> is invoked synchronously with the chosen
    /// ability's Guid (or <see cref="Guid.Empty"/> to clear the slot) so
    /// <see cref="InventoryUI"/> can wire the selection to
    /// <see cref="ActivatedAbilitiesPart.AssignAbilityToSlot"/> in one step.
    /// </summary>
    public class GrimoirePickerState
    {
        /// <summary>
        /// Slot index (0..9) the picker is rebinding. Used for the popup title
        /// and passed back via the callback so the caller doesn't have to track
        /// it separately.
        /// </summary>
        public int TargetSlot;

        /// <summary>
        /// Every grimoire-granted ability the player currently has, sorted in a
        /// stable order (matches <see cref="ActivatedAbilitiesPart.AbilityList"/>
        /// order for grimoires). Includes abilities that are currently bound to
        /// another slot — the picker renders an "already bound" marker next to
        /// those rows so the player knows picking one will MOVE it rather than
        /// duplicate it.
        /// </summary>
        public List<ActivatedAbility> Grimoires = new List<ActivatedAbility>();

        /// <summary>
        /// Currently-highlighted row in <see cref="Grimoires"/>. Always valid
        /// when <see cref="Grimoires"/> is non-empty; clamped to [0, count-1]
        /// by navigation helpers.
        /// </summary>
        public int CursorIndex;

        /// <summary>
        /// Scroll offset in units of grimoire blocks (not raw rows). The picker
        /// shows a fixed number of blocks at a time; scrolling advances by whole
        /// blocks so the 4-row layout is never cut mid-entry.
        /// </summary>
        public int ScrollOffset;

        /// <summary>
        /// Invoked when the player confirms a selection, clears the slot, or
        /// cancels. Arguments are (targetSlot, selectedAbilityId). A
        /// <see cref="Guid.Empty"/> id means "clear this slot" (explicit X/Del)
        /// while a cancel (Esc) sets <see cref="CancelWithoutCallback"/> and
        /// the callback is NOT invoked — so the caller can distinguish "leave
        /// binding unchanged" (Esc) from "unbind this slot" (X).
        /// </summary>
        public Action<int, Guid> SelectionCallback;

        /// <summary>
        /// Set to true when the popup was closed via Esc. Rendering and input
        /// paths check this before invoking the callback so cancel becomes a
        /// true no-op.
        /// </summary>
        public bool CancelWithoutCallback;
    }
}
