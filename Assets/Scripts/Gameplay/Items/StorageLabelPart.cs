using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    // Keep the saved ordinal of the former Icon mode for loading old labels.
    public enum StorageLabelMode { None, ColorMark, Icon, Text }

    /// <summary>Optional cosmetic state on an exact physical item or stack.</summary>
    public sealed class StorageLabelPart : Part
    {
        public override string Name => "StorageLabel";
        public int Mode, ColorIndex, IconIndex;
        public string Text = "";

        public override void OnAfterLoad(SaveReader reader)
        {
            if (Mode != (int)StorageLabelMode.Icon || !StorageLabels.TryLegacyIcon(ColorIndex, IconIndex, out var mark))
                return;
            Mode = (int)mark.Mode;
            IconIndex = mark.IconIndex;
            Text = mark.Text;
        }
    }

    internal readonly struct StorageLabelValue
    {
        internal readonly StorageLabelMode Mode;
        internal readonly int ColorIndex, IconIndex;
        internal readonly string Text;

        internal StorageLabelValue(StorageLabelMode mode, int color, int icon, string text)
        { Mode = mode; ColorIndex = color; IconIndex = icon; Text = text; }

        internal string Display => Mode == StorageLabelMode.ColorMark ? "■"
            : Mode == StorageLabelMode.Text ? Text : "";
    }

    internal static class StorageLabels
    {
        internal const int MaxTextLength = 24;
        internal static readonly IReadOnlyList<string> ColorNames = Array.AsReadOnly(new[]
            { "Ivory", "Amber", "Coral", "Green", "Cyan", "Blue", "Violet" });
        internal static readonly IReadOnlyList<string> IconNames = Array.AsReadOnly(new[]
            { "Tools", "Food", "Materials", "Potions", "Clothing", "Trade", "Important", "Travel" });
        internal static bool TryLegacyIcon(int color, int icon, out StorageLabelValue value)
        {
            value = default;
            return icon >= 0 && icon < IconNames.Count && TryValue(StorageLabelMode.ColorMark, color, 0, "", out value);
        }

        internal static bool CanEdit(Entity actor, Entity item)
            => TryLocation(actor, item, out _) && LabelPartCurrent(item);

        static bool LabelPartCurrent(Entity item)
        {
            var part = item.GetPart<StorageLabelPart>();
            return part == null || part.ParentEntity == item && item.Parts.Count(p => p is StorageLabelPart) == 1;
        }

        internal static bool TryLocation(Entity actor, Entity item,
            out (Entity vessel, ContainerPart container, BodyPart bodySlot, string legacySlot) location)
        {
            location = default;
            var pack = actor?.GetPart<InventoryPart>();
            var physics = item?.GetPart<PhysicsPart>();
            if (actor?.HasTag("Player") != true || pack?.ParentEntity != actor || item == null
                || pack.Objects == null || pack.EquippedItems == null
                || string.IsNullOrEmpty(item.ID) || item.SpatialZone != null || physics?.ParentEntity != item
                || item.GetPart<DestructiblePart>() is { Gone: true }) return false;
            var stack = item.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != item || stack.StackCount <= 0 || stack.StackCount > stack.MaxStack))
                return false;

            if (physics.InInventory == actor)
                return physics.Equipped == null && pack.Objects.Contains(item)
                    && pack.Objects.Count(owner => owner?.ID == item.ID) == 1
                    && !pack.EquippedItems.Values.Any(owner => owner?.ID == item.ID)
                    && pack.FindEquippedBodyPart(item) == null;

            if (pack.Objects.Any(owner => owner?.ID == item.ID)) return false;
            if (physics.Equipped == actor && physics.InInventory == null)
            {
                if (pack.EquippedItems.Values.Any(owner => owner != item && owner?.ID == item.ID)) return false;
                if (actor.GetPart<Body>() != null)
                {
                    var slot = pack.FindEquippedBodyPart(item);
                    if (slot == null || pack.GetEquipped(slot.ID.ToString()) != item) return false;
                    location = (null, null, slot, null);
                }
                else
                {
                    var slots = pack.EquippedItems.Where(entry => entry.Value == item).Select(entry => entry.Key).ToArray();
                    if (slots.Length != 1) return false;
                    location = (null, null, null, slots[0]);
                }
                return true;
            }

            var vessel = physics.InInventory;
            if (physics.Equipped != null || !CarriedStorage.TryBrowse(actor, vessel, out var container)
                || !container.Contents.Contains(item)) return false;
            location = (vessel, container, null, null);
            return true;
        }

        internal static StorageLabelValue Read(Entity item) => TryRead(item, out var value) ? value : default;

        internal static bool TryRead(Entity item, out StorageLabelValue value)
        {
            value = default;
            if (item == null || !LabelPartCurrent(item)) return false;
            var part = item.GetPart<StorageLabelPart>();
            if (part == null) return true;
            return part.Mode == (int)StorageLabelMode.Icon ? TryLegacyIcon(part.ColorIndex, part.IconIndex, out value)
                : TryValue((StorageLabelMode)part.Mode, part.ColorIndex, part.IconIndex, part.Text, out value);
        }

        internal static bool SameStackLabel(Entity first, Entity second)
            => TryRead(first, out var a) && TryRead(second, out var b)
                && a.Mode == b.Mode && a.ColorIndex == b.ColorIndex && a.IconIndex == b.IconIndex && a.Text == b.Text;

        internal static StorageLabelEdit Begin(Entity actor, Entity item) =>
            TryLocation(actor, item, out var location) && LabelPartCurrent(item)
                ? new StorageLabelEdit(actor, item, location) : null;

        internal static bool TryValue(StorageLabelMode mode, int color, int icon, string text, out StorageLabelValue value)
        {
            value = default;
            if (mode == StorageLabelMode.None) return true;
            if (color < 0 || color >= ColorNames.Count) return false;
            switch (mode)
            {
                case StorageLabelMode.ColorMark:
                    icon = 0; text = "";
                    break;
                case StorageLabelMode.Text:
                    if (text == null || text.Any(c => c < ' ' || c > '~' || c == '&')) return false;
                    text = text.Trim();
                    if (text.Length == 0 || text.Length > MaxTextLength) return false;
                    icon = 0;
                    break;
                default:
                    return false;
            }
            value = new StorageLabelValue(mode, color, icon, text);
            return true;
        }
    }

    internal sealed class StorageLabelEdit
    {
        private readonly Entity actor;
        private readonly Zone zone;
        private readonly InventoryPart pack;
        private readonly (Entity vessel, ContainerPart container, BodyPart bodySlot, string legacySlot) location;
        private readonly ContainerPart ownContainer;
        private readonly PhysicsPart physics;
        private readonly StackerPart stack;
        private readonly int quantity;
        private readonly string vesselId;
        private readonly string ownerId;
        private readonly StorageLabelPart originalPart;
        private readonly (int mode, int color, int icon, string text) originalFields;
        private bool completed;
        internal Entity Owner { get; }
        internal StorageLabelValue Original { get; }

        internal StorageLabelEdit(Entity actor, Entity item,
            (Entity vessel, ContainerPart container, BodyPart bodySlot, string legacySlot) location)
        {
            this.actor = actor;
            zone = actor.SpatialZone;
            pack = actor.GetPart<InventoryPart>();
            Owner = item;
            ownerId = item.ID;
            this.location = location;
            vesselId = location.vessel?.ID;
            ownContainer = item.GetPart<ContainerPart>();
            physics = item.GetPart<PhysicsPart>();
            stack = item.GetPart<StackerPart>();
            quantity = stack?.StackCount ?? 1;
            Original = StorageLabels.Read(item);
            originalPart = item.GetPart<StorageLabelPart>();
            if (originalPart != null) originalFields = (originalPart.Mode, originalPart.ColorIndex, originalPart.IconIndex, originalPart.Text);
        }

        internal bool Commit(StorageLabelMode mode, int colorIndex, int iconIndex, string text)
        {
            if (completed || !StorageLabels.TryValue(mode, colorIndex, iconIndex, text, out var value)
                || actor.SpatialZone != zone || actor.GetPart<InventoryPart>() != pack
                || Owner.ID != ownerId || Owner.GetPart<ContainerPart>() != ownContainer
                || Owner.GetPart<PhysicsPart>() != physics || Owner.GetPart<StackerPart>() != stack
                || (stack?.StackCount ?? 1) != quantity
                || location.vessel?.ID != vesselId || !StorageLabels.TryLocation(actor, Owner, out var current)
                || current != location || Owner.GetPart<StorageLabelPart>() != originalPart
                || Owner.Parts.Count(p => p is StorageLabelPart) != (originalPart == null ? 0 : 1)) return false;
            if (originalPart != null && (originalPart.ParentEntity != Owner
                || (originalPart.Mode, originalPart.ColorIndex, originalPart.IconIndex, originalPart.Text) != originalFields)) return false;

            if (originalPart != null) Owner.RemovePart(originalPart);
            if (value.Mode != StorageLabelMode.None)
                Owner.AddPart(new StorageLabelPart
                { Mode = (int)value.Mode, ColorIndex = value.ColorIndex, IconIndex = value.IconIndex, Text = value.Text });
            completed = true;
            return true;
        }
    }
}
