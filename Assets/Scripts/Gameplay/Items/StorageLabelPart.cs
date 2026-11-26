using System;
using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    public enum StorageLabelMode { None, ColorMark, Icon, Text }

    /// <summary>Optional cosmetic state on the exact physical storage vessel.</summary>
    public sealed class StorageLabelPart : Part
    {
        public override string Name => "StorageLabel";
        public int Mode, ColorIndex, IconIndex;
        public string Text = "";
    }

    internal readonly struct StorageLabelValue
    {
        internal readonly StorageLabelMode Mode;
        internal readonly int ColorIndex, IconIndex;
        internal readonly string Text;

        internal StorageLabelValue(StorageLabelMode mode, int color, int icon, string text)
        { Mode = mode; ColorIndex = color; IconIndex = icon; Text = text; }

        internal string Display => Mode == StorageLabelMode.ColorMark ? "■"
            : Mode == StorageLabelMode.Icon ? StorageLabels.IconGlyph(IconIndex)
            : Mode == StorageLabelMode.Text ? Text : "";
    }

    internal static class StorageLabels
    {
        internal const int MaxTextLength = 24;
        internal static readonly IReadOnlyList<string> ColorNames = Array.AsReadOnly(new[]
            { "Ivory", "Amber", "Coral", "Green", "Cyan", "Blue", "Violet" });
        internal static readonly IReadOnlyList<string> IconNames = Array.AsReadOnly(new[]
            { "Tools", "Food", "Materials", "Potions", "Clothing", "Trade", "Important", "Travel" });
        private static readonly string[] Glyphs = { "+", "%", "#", "!", "=", "$", "*", "^" };

        internal static string IconGlyph(int icon) => icon >= 0 && icon < Glyphs.Length ? Glyphs[icon] : "";

        internal static bool CanEdit(Entity actor, Entity vessel) => actor?.HasTag("Player") == true
            && CarriedStorage.IsEligible(vessel) && WorldResourceActions.Carried(actor, vessel)
            && InventoryPart.GetItemWeight(vessel) < int.MaxValue;

        internal static StorageLabelValue Read(Entity vessel)
        {
            if (!CarriedStorage.IsEligible(vessel) || vessel.GetPart<StorageLabelPart>() is not StorageLabelPart part
                || part.ParentEntity != vessel || vessel.Parts.Count(p => p is StorageLabelPart) != 1) return default;
            return TryValue((StorageLabelMode)part.Mode, part.ColorIndex, part.IconIndex, part.Text, out var value)
                ? value : default;
        }

        internal static StorageLabelEdit Begin(Entity actor, Entity vessel) =>
            CanEdit(actor, vessel) && vessel.Parts.Count(p => p is StorageLabelPart) <= 1
                ? new StorageLabelEdit(actor, vessel) : null;

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
                case StorageLabelMode.Icon:
                    if (icon < 0 || icon >= Glyphs.Length) return false;
                    text = "";
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
        private readonly ContainerPart container;
        private readonly string ownerId;
        private readonly StorageLabelPart originalPart;
        private readonly (int mode, int color, int icon, string text) originalFields;
        private bool completed;
        internal Entity Owner { get; }
        internal StorageLabelValue Original { get; }

        internal StorageLabelEdit(Entity actor, Entity vessel)
        {
            this.actor = actor;
            zone = actor.SpatialZone;
            pack = actor.GetPart<InventoryPart>();
            Owner = vessel;
            ownerId = vessel.ID;
            container = vessel.GetPart<ContainerPart>();
            Original = StorageLabels.Read(vessel);
            originalPart = vessel.GetPart<StorageLabelPart>();
            if (originalPart != null) originalFields = (originalPart.Mode, originalPart.ColorIndex, originalPart.IconIndex, originalPart.Text);
        }

        internal bool Commit(StorageLabelMode mode, int colorIndex, int iconIndex, string text)
        {
            if (completed || !StorageLabels.TryValue(mode, colorIndex, iconIndex, text, out var value)
                || actor.SpatialZone != zone || actor.GetPart<InventoryPart>() != pack
                || Owner.ID != ownerId || Owner.GetPart<ContainerPart>() != container
                || Owner.GetPart<StorageLabelPart>() != originalPart || !StorageLabels.CanEdit(actor, Owner)
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
