using CavesOfOoo.Core;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    public sealed partial class FlatPixelGrovelandsMaterialsCatalog
    {
        public const string MucilageResourcePath = "Sprites/Live16x24/mirror-mucilage";
        const string MucilageLiquid = "choir-mirror-mucilage";
        readonly Tile[] mucilage;

        /// <summary>Only the same sole permanent coating can relinquish its generic wave.
        /// Independent thermal, charge, cloud and residue marks remain authoritative.</summary>
        internal static bool IsSoleMucilage(ZoneTileState.TileState state)
            => state?.Coatings != null && state.Coatings.Count == 1 && state.Coatings[0] != null
                && state.Coatings[0].Id == MucilageLiquid && state.Coatings[0].Turns == ZoneTileState.Permanent
                && string.IsNullOrEmpty(state.Coatings[0].ProjectionOwnerId);

        /// <summary>Existing ground decoration keeps a real disclosed pool below
        /// a covering actor/item; it never replaces the selected foreground.</summary>
        public Tile CoveredMucilage(Zone zone, int x, int y)
            => TryMucilageOwner(zone, x, y, out var pool, out _) && !Selected(zone, x, y, pool)
                ? MucilageTile(pool, x, y) : null;

        Tile MucilageTile(Entity pool, int x, int y)
            => mucilage[(Owned<LiquidPoolPart>(pool).Volume == 0 ? 2 : 0) + ((x + y) & 1)];

        bool TryMucilageOwner(Zone zone, int x, int y, out Entity pool, out Entity grass)
        {
            pool = null; grass = null;
            if (mucilage == null || forest == null && sill == null || !IsSupportedZone(zone)
                || !ReadyCell(zone, x, y, out var cell) || !IsSoleMucilage(zone.TileState.Get(x, y))
                || !selector.TryResolve(zone, x, y, out var selected) || selected.Remembered) return false;
            foreach (var other in cell.Occupants)
            {
                if (!Placed(zone, cell, other) || !VisiblePhysicalOwner(other)) continue;
                if (other.BlueprintName == "MirrorMucilagePool")
                { if (pool != null || !CanonicalMucilage(other)) return false; pool = other; }
                else if (other.BlueprintName == "Grass")
                { if (grass != null || !OrdinaryGrass(other)) return false; grass = other; }
                else if (!MucilageCover(other)) return false;
            }
            return pool != null && grass != null && (selected.Owner == pool
                || MucilageCover(selected.Owner) && Placed(zone, cell, selected.Owner) && VisiblePhysicalOwner(selected.Owner));
        }
        static bool MucilageCover(Entity owner)
            => owner != null && (owner.HasTag("Player") || owner.HasTag("Creature") || owner.HasTag("Item"));

        static bool CanonicalMucilage(Entity owner)
        {
            if (!PlainOwner(owner) || !owner.HasTag("Terrain") || owner.HasTag("Solid") || owner.HasTag("Wall")
                || owner.HasTag("Vegetation") || Owned<PhysicsPart>(owner).Solid) return false;
            // Unknown functional extensions must retain their authored presentation.
            if (owner.Parts.Count != 6 || Owned<ExaminablePart>(owner) == null || !Material(owner, "Water")) return false;
            var r = Owned<RenderPart>(owner); var pool = Owned<LiquidPoolPart>(owner); var thermal = Owned<ThermalPart>(owner);
            return r.RenderString == "~" && r.RenderLayer == 1 && r.ColorString == "&C"
                && string.IsNullOrEmpty(r.GlyphVariants) && string.IsNullOrEmpty(r.BackgroundColor)
                && string.IsNullOrEmpty(r.Tile) && string.IsNullOrEmpty(r.TileColor) && string.IsNullOrEmpty(r.DetailColor)
                && pool != null && pool.LiquidId == MucilageLiquid && pool.Volume >= 0
                && NeutralThermal(owner) && thermal != null && float.IsFinite(thermal.VaporTemperature)
                && thermal.Temperature < thermal.VaporTemperature;
        }
    }
}
