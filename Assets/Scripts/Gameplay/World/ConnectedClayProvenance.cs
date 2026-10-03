using System;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>Only the two original district clay units carry this bounded provenance.
    /// Ordinary items stay unchanged. Primitive moves conserve bits; receipts restore them.</summary>
    public sealed class ConnectedClayOriginPart : Part
    {
        public override string Name => "ConnectedClayOrigin";
        public string WorldKey, OwnerID;
        public int Units;
    }
    internal static class ConnectedClayProvenance
    {
        static int Bits(int mask) => (mask & 1) + ((mask >> 1) & 1);
        static int Count(Entity item) => item?.GetPart<StackerPart>()?.StackCount ?? 1;
        internal static int Mask(Entity item, string key) => Valid(item, Count(item))
            && item.GetPart<ConnectedClayOriginPart>().WorldKey == key ? item.GetPart<ConnectedClayOriginPart>().Units : 0;
        static bool Valid(Entity item, int count)
        {
            var part = item?.GetPart<ConnectedClayOriginPart>();
            return part != null && part.ParentEntity == item && item.BlueprintName == "FireClay" && part.OwnerID == item.ID
                && ConnectedSpreadProgress.ValidKey(part.WorldKey) && (part.Units & ~3) == 0 && part.Units >= 0
                && Bits(part.Units) <= count && item.Parts.Count(p => p is ConnectedClayOriginPart) == 1;
        }
        internal static bool CanMerge(Entity first, Entity second)
        {
            var a = first?.GetPart<ConnectedClayOriginPart>(); var b = second?.GetPart<ConnectedClayOriginPart>();
            if (a != null && a.Units != 0 && !Valid(first, Count(first)) || b != null && b.Units != 0 && !Valid(second, Count(second))) return false;
            return a == null || b == null || a.Units == 0 || b.Units == 0 || a.WorldKey == b.WorldKey && (a.Units & b.Units) == 0;
        }
        internal static bool Merge(Entity target, Entity source, int quantity)
        {
            if (!CanMerge(target, source)) return false;
            var origin = source.GetPart<ConnectedClayOriginPart>();
            if (origin == null || origin.Units == 0) return true;
            int moved = First(origin.Units, quantity); if (moved == 0) return true;
            var destination = target.GetPart<ConnectedClayOriginPart>();
            if (destination == null) { destination = new ConnectedClayOriginPart(); target.AddPart(destination); }
            destination.WorldKey = origin.WorldKey; destination.OwnerID = target.ID; destination.Units |= moved; origin.Units &= ~moved;
            return true;
        }
        internal static void Split(Entity source, Entity clone, int quantity)
        {
            var origin = source.GetPart<ConnectedClayOriginPart>(); var child = clone.GetPart<ConnectedClayOriginPart>();
            if (origin == null || child == null) return;
            child.OwnerID = clone.ID; child.Units = 0;
            if (!Valid(source, Count(source) + quantity)) return;
            int moved = First(origin.Units, quantity); child.WorldKey = origin.WorldKey; child.Units = moved; origin.Units &= ~moved;
        }
        internal static void ConsumeOne(Entity item)
        {
            if (!Valid(item, Count(item))) return;
            var part = item.GetPart<ConnectedClayOriginPart>(); part.Units &= ~First(part.Units, 1);
        }
        static int First(int mask, int count)
        { int result = 0; for (int bit = 1; bit <= 2 && count > 0; bit <<= 1) if ((mask & bit) != 0) { result |= bit; count--; } return result; }
        internal readonly struct Snapshot
        {
            readonly ConnectedClayOriginPart part; readonly string key, owner; readonly int units;
            internal Snapshot(Entity item)
            { part = item?.GetPart<ConnectedClayOriginPart>(); key = part?.WorldKey; owner = part?.OwnerID; units = part?.Units ?? 0; }
            internal bool Changed(Entity item)
            { var now = item?.GetPart<ConnectedClayOriginPart>(); return now != part || now != null && (now.WorldKey != key || now.OwnerID != owner || now.Units != units); }
            internal void Restore(Entity item)
            {
                if (item == null) return;
                var now = item.GetPart<ConnectedClayOriginPart>();
                if (part == null) { if (now != null) item.RemovePart(now); return; }
                if (now != part) { if (now != null) item.RemovePart(now); item.AddPart(part); }
                part.WorldKey = key; part.OwnerID = owner; part.Units = units;
            }
        }
    }
}
