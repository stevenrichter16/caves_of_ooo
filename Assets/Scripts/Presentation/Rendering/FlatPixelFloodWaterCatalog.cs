using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Flood water (2026-10-30): Spread and Sodden grass under a permanent, ownerless water coating draws the
    /// approved water views (opening-ground frames 0–15, banks toward disclosed dry neighbours) by exactly the
    /// rule Grovelands uses for its Fen water. Admitted in ordinary Spread and Sodden wilderness and their
    /// special sites. A substrate only: no portrait, no simulation, no state change; pools, wells, sources and
    /// mixed or passing water keep their own presentation.
    /// </summary>
    public sealed class FlatPixelFloodWaterCatalog : IDisposable
    {
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] water;
        bool disposed;

        public FlatPixelFloodWaterCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath), borrowedSelector) { }

        public FlatPixelFloodWaterCatalog(Texture2D openingGround21, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null || openingGround21 == null || openingGround21.width != 336 || openingGround21.height != 24) return;
            water = new Tile[16];
            for (int i = 0; i < 16; i++)
            {
                var sprite = Sprite.Create(openingGround21, new Rect(i * 16, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 flood water " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; water[i] = tile;
            }
        }

        public static bool IsSupportedZone(Zone zone)
            => FlatPixelSpreadEnvironmentCatalog.IsSupportedZone(zone) || FlatPixelSoddenTravelCatalog.IsSupportedZone(zone)
                || FlatPixelSiteAdmission.IsOrdinarySite(zone, BiomeType.Spread) || FlatPixelSiteAdmission.IsOrdinarySite(zone, BiomeType.Sodden);

        /// <summary>The represented grass is the only owner whose glyph may be suppressed.</summary>
        public bool TryResolveWaterGround(Zone zone, int x, int y, out Tile tile, out Entity representedGrass)
        {
            tile = null; representedGrass = null;
            if (!Ready(zone, x, y, out var cell) || !selector.TryResolve(zone, x, y, out var selected)) return false;
            var coatings = zone.TileState.Get(x, y)?.Coatings;
            if (coatings == null || coatings.Count != 1) return false;
            var coating = coatings[0];
            if (coating == null || coating.Id != "water" || coating.Turns != ZoneTileState.Permanent
                || !string.IsNullOrEmpty(coating.ProjectionOwnerId)) return false;
            Entity grass = null; int grasses = 0;
            foreach (var owner in cell.Occupants)
            {
                if (owner == null || owner.SpatialZone != zone) continue;
                if (owner.HasPart<LiquidPoolPart>() || owner.HasPart<WellPart>() || owner.HasPart<TileStateSourcePart>()
                    || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>()) return false;
                if (owner.BlueprintName == "Grass")
                { grasses++; if (ReferenceEquals(selected.Owner, owner) && FlatPixelSillRiverCatalog.OrdinaryGrass(owner)) grass = owner; }
            }
            int mask = 0;
            if (DryBank(zone, x, y - 1)) mask |= 1;
            if (DryBank(zone, x + 1, y)) mask |= 2;
            if (DryBank(zone, x, y + 1)) mask |= 4;
            if (DryBank(zone, x - 1, y)) mask |= 8;
            tile = water[mask];
            if (grasses == 1) representedGrass = grass;
            return true;
        }

        bool Ready(Zone zone, int x, int y, out Cell cell)
        {
            cell = null;
            if (disposed || water == null || selector == null || !IsSupportedZone(zone)) return false;
            cell = zone.GetCell(x, y);
            return cell != null && cell.ParentZone == zone && cell.X == x && cell.Y == y && cell.Explored && cell.IsVisible
                && selector.TryResolve(zone, x, y, out var picked) && !picked.Remembered;
        }
        bool DryBank(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.ParentZone != zone || !cell.Explored || !cell.IsVisible
                || !selector.TryResolve(zone, x, y, out var picked) || picked.Remembered) return false;
            var coatings = zone.TileState.Get(x, y)?.Coatings;
            return coatings == null || coatings.Count == 0;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (water == null) return;
            foreach (var tile in water) { if (tile == null) continue; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
