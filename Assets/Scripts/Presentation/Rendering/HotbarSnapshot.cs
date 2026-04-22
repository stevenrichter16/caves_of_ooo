using System.Collections.Generic;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Read-only presentation snapshot for the bottom gameplay hotbar.
    /// </summary>
    public sealed class HotbarSnapshot
    {
        public HotbarSnapshot(
            string title,
            string summaryText,
            string hintText,
            IReadOnlyList<HotbarSlotSnapshot> slots,
            int selectedSlot,
            int pendingSlot)
        {
            Title = title ?? string.Empty;
            SummaryText = summaryText ?? string.Empty;
            HintText = hintText ?? string.Empty;
            Slots = slots ?? new List<HotbarSlotSnapshot>();
            SelectedSlot = selectedSlot;
            PendingSlot = pendingSlot;
        }

        public string Title { get; }
        public string SummaryText { get; }
        public string HintText { get; }
        public IReadOnlyList<HotbarSlotSnapshot> Slots { get; }
        public int SelectedSlot { get; }
        public int PendingSlot { get; }
    }

    public readonly struct HotbarSlotSnapshot
    {
        public HotbarSlotSnapshot(
            int slotIndex,
            char hotkey,
            string displayName,
            string shortName,
            string accentColorCode,
            string mechanicsText,
            char glyph,
            int cooldownRemaining,
            bool occupied,
            bool selected,
            bool pending,
            bool usable)
        {
            SlotIndex = slotIndex;
            Hotkey = hotkey;
            DisplayName = displayName ?? string.Empty;
            ShortName = shortName ?? string.Empty;
            AccentColorCode = accentColorCode ?? string.Empty;
            MechanicsText = mechanicsText ?? string.Empty;
            Glyph = glyph;
            CooldownRemaining = cooldownRemaining;
            Occupied = occupied;
            Selected = selected;
            Pending = pending;
            Usable = usable;
        }

        public int SlotIndex { get; }
        public char Hotkey { get; }
        public string DisplayName { get; }
        public string ShortName { get; }
        public string AccentColorCode { get; }
        public string MechanicsText { get; }
        public char Glyph { get; }
        public int CooldownRemaining { get; }
        public bool Occupied { get; }
        public bool Selected { get; }
        public bool Pending { get; }
        public bool Usable { get; }
    }
}
