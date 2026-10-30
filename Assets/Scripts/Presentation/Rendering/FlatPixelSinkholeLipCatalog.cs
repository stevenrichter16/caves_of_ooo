using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// The solid SinkholeLip ring around an ordinary sinkhole mouth (2026-10-30), drawn with the original
    /// sinkhole-lip rim block (moss cap, flat strata, undercut base; two frames by (x+y)&amp;1). Admitted only
    /// in FlatPixelSiteAdmission sinkhole surfaces of any biome. Borrows the selector; owns only cached
    /// tiles. Collision and the lip itself are unchanged.
    /// </summary>
    public sealed class FlatPixelSinkholeLipCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/sinkhole-lip";
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] lip;
        bool disposed;

        public FlatPixelSinkholeLipCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath), borrowedSelector) { }

        /// <summary>Missing or malformed art leaves the lip on its fallback; no Resources reload.</summary>
        public FlatPixelSinkholeLipCatalog(Texture2D lip2, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null || lip2 == null || lip2.width != 32 || lip2.height != 24) return;
            lip = new Tile[2];
            for (int i = 0; i < 2; i++)
            {
                var sprite = Sprite.Create(lip2, new Rect(i * 16, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 sinkhole lip " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; lip[i] = tile;
            }
        }

        public static bool IsSupportedZone(Zone zone)
        {
            if (zone?.ZoneID == null) return false;
            var (x, y, _) = WorldMap.FromZoneID(zone.ZoneID);
            var map = WorldLocationContext.For(zone)?.WorldMap;
            return x >= 0 && map?.Tiles != null && x < WorldMap.Width && y >= 0 && y < WorldMap.Height
                && FlatPixelSiteAdmission.IsOrdinarySite(zone, map.GetBiome(x, y));
        }

        public bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || lip == null || selector == null || owner == null || !IsSupportedZone(zone)) return false;
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.ParentZone != zone || cell.X != x || cell.Y != y || !cell.Explored || !cell.IsVisible
                || !selector.TryResolve(zone, x, y, out var picked) || picked.Remembered || !ReferenceEquals(picked.Owner, owner)
                || !Canonical(zone, cell, owner)) return false;
            tile = lip[(x + y) & 1]; return true;
        }

        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!TryResolveForeground(zone, x, y, owner, out var tile)) return false;
            sprite = tile.sprite; return sprite != null;
        }

        /// <summary>Canonical placed lip: '#' '&amp;y' layer 2 solid terrain with only its own render,
        /// physics and description; no visual override, damage, state or foreign meaning.</summary>
        static bool Canonical(Zone zone, Cell cell, Entity owner)
        {
            if (owner.BlueprintName != "SinkholeLip" || !owner.HasTag("Terrain") || !owner.HasTag("Solid")
                || owner.HasTag("Creature") || owner.HasTag("Item") || owner.SpatialZone != zone || zone.GetEntityCell(owner) != cell) return false;
            var r = owner.GetPart<RenderPart>(); var p = owner.GetPart<PhysicsPart>();
            if (r == null || r.ParentEntity != owner || !r.Visible || r.RenderString != "#" || r.ColorString != "&y" || r.RenderLayer != 2
                || !string.IsNullOrEmpty(r.BackgroundColor) || !string.IsNullOrEmpty(r.GlyphVariants)
                || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant)
                || p == null || p.ParentEntity != owner || !p.Solid || p.Takeable) return false;
            if (owner.HasPart<StatusEffectsPart>() && owner.GetPart<StatusEffectsPart>().GetRenderColorOverride() != null) return false;
            return !owner.HasPart<DestructiblePart>() && !owner.HasPart<LiquidPoolPart>() && !owner.HasPart<TileStateSourcePart>()
                && !owner.HasPart<DoorPart>() && !owner.HasPart<ContainerPart>() && !owner.HasPart<StairsDownPart>() && !owner.HasPart<StairsUpPart>();
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (lip == null) return;
            foreach (var tile in lip) { if (tile == null) continue; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
