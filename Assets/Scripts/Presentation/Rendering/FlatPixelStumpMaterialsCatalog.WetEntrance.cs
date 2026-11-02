using CavesOfOoo.Core;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    public sealed partial class FlatPixelStumpMaterialsCatalog
    {
        readonly Tile[] wetEntrance;
        /// <summary>One observed water/entrance overlap. The stair remains its
        /// real owner; this lower cue never replaces the selected pool/actor.</summary>
        public Tile WetEntrance(Zone zone, int x, int y)
            => TryWetEntranceOwners(zone, x, y, out _, out _) ? wetEntrance[0] : null;

        bool TryWetEntranceOwners(Zone zone, int x, int y, out Entity stair, out Entity pool)
        {
            stair = null; pool = null;
            if (wetEntrance == null || materials == null || fellingAuthored || siteLedgesOnly
                || zone?.ZoneID != "Overworld.4.4.0" || !IsSupportedZone(zone)
                || !ReadyCell(zone, x, y, out var cell) || !PermanentWater(zone.TileState.Get(x, y))
                || !selector.TryResolve(zone, x, y, out var selected) || selected.Remembered) return false;
            Entity floor = null;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                if (owner.BlueprintName == "StairsDown")
                { if (stair != null || !OrdinaryWetStair(owner)) return false; stair = owner; }
                else if (owner.BlueprintName == "SprayPool")
                { if (pool != null || PoolFrame(owner, x, y) < 0) return false; pool = owner; }
                else if (owner.BlueprintName == "TepuiStone")
                { if (floor != null || !OrdinaryFloor(owner)) return false; floor = owner; }
                else if (!WetEntranceCover(owner)) return false;
            }
            return stair != null && pool != null && floor != null && (selected.Owner == pool
                || WetEntranceCover(selected.Owner) && Placed(zone, cell, selected.Owner) && VisiblePhysicalOwner(selected.Owner));
        }
        static bool WetEntranceCover(Entity owner)
            => owner != null && (owner.HasTag("Player") || owner.HasTag("Creature") || owner.HasTag("Item"))
                && !owner.HasPart<LiquidPoolPart>() && !owner.HasPart<TileStateSourcePart>() && !owner.HasPart<GasPoolPart>();
        static bool OrdinaryWetStair(Entity owner)
        {
            if (!PlainOwner(owner) || owner.Parts.Count != 4 || !Terrain(owner, false, false)
                || !NoMaterialState(owner) || !owner.HasTag("StairsDown") || owner.HasTag("StairsUp")
                || Owned<StairsDownPart>(owner) == null || Owned<ExaminablePart>(owner) == null) return false;
            var r = Owned<RenderPart>(owner);
            return Render(r, ">", "&W", 0) && string.IsNullOrEmpty(r.GlyphVariants)
                && string.IsNullOrEmpty(r.BackgroundColor) && string.IsNullOrEmpty(r.Tile)
                && string.IsNullOrEmpty(r.TileColor) && string.IsNullOrEmpty(r.DetailColor);
        }
    }
}
