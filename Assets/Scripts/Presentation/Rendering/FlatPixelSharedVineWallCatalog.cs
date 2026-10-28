using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Approved woven-vine frames for otherwise uncovered surface walls.
    /// Borrows the Sill atlas and current-owner selector. No new geometry, memory,
    /// simulation state, material or regional ground is introduced.</summary>
    public sealed class FlatPixelSharedVineWallCatalog : IDisposable
    {
        const int Width = 16, Height = 24, AtlasFrames = 24, Masks = 16;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] tiles;
        bool disposed;
        static readonly HashSet<string> surfaces = BuildSurfaces();
        static HashSet<string> BuildSurfaces()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    string id = WorldMap.ToZoneID(x, y);
                    // Reserve the addresses, not only currently successful
                    // regional claims: revoked authority must stay revoked.
                    if (id != FlatPixelSillEnvelopeCatalog.ZoneID && !GrovelandsCompositionPlan.IsWildernessZone(id)) ids.Add(id);
                }
            return ids;
        }
        public FlatPixelSharedVineWallCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.ResourcePath), borrowedSelector) { }

        /// <summary>Missing/malformed injection stays unavailable. Only generated
        /// views are owned; the 24-frame source and selector remain borrowed.</summary>
        public FlatPixelSharedVineWallCatalog(Texture2D atlas, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null || atlas == null || atlas.width != Width * AtlasFrames || atlas.height != Height) return;
            tiles = new Tile[Masks];
            for (int mask = 0; mask < Masks; mask++)
            {
                var sprite = Sprite.Create(atlas, new Rect(mask * Width, 0, Width, Height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 shared vine " + mask; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; tiles[mask] = tile;
            }
        }
        public static bool HasKnownRole(Entity owner) => owner?.BlueprintName == "VineWall";

        /// <summary>Only canonical, currently attached surface addresses. Existing
        /// Sill/Grovelands policies remain authoritative, including their refusals.</summary>
        public static bool IsSupportedZone(Zone zone)
        {
            if (zone?.ZoneID == null || !surfaces.Contains(zone.ZoneID)) return false;
            var manager = WorldLocationContext.For(zone);
            var map = manager?.WorldMap;
            return map?.Tiles != null && map.POIs != null
                && map.Tiles.GetLength(0) == WorldMap.Width && map.Tiles.GetLength(1) == WorldMap.Height
                && map.POIs.GetLength(0) == WorldMap.Width && map.POIs.GetLength(1) == WorldMap.Height
                && manager.CachedZones != null && manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                && ReferenceEquals(zone, current);
        }

        /// <summary>Five bounded current selections: center and visible cardinal
        /// neighbors only. Hidden walls retain the existing glyph-memory policy.</summary>
        public bool TryResolve(Zone zone, int x, int y, out Tile tile)
        {
            tile = null;
            if (disposed || tiles == null || !IsSupportedZone(zone) || !Connects(zone, x, y, out _)) return false;
            int mask = 0;
            if (Connects(zone, x, y - 1, out _)) mask |= 1;
            if (Connects(zone, x + 1, y, out _)) mask |= 2;
            if (Connects(zone, x, y + 1, out _)) mask |= 4;
            if (Connects(zone, x - 1, y, out _)) mask |= 8;
            tile = tiles[mask]; return true;
        }

        /// <summary>Isolated frame of the same disclosed selected wall. The caller
        /// still authorizes the examine document; this query never fires Render.</summary>
        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (disposed || tiles == null || !IsSupportedZone(zone)
                || !Connects(zone, x, y, out var selected) || !ReferenceEquals(owner, selected)) return false;
            sprite = tiles[0].sprite; return sprite != null;
        }
        bool Connects(Zone zone, int x, int y, out Entity owner)
        {
            owner = null; var cell = zone.GetCell(x, y);
            if (cell == null || !cell.Explored || !cell.IsVisible
                || !selector.TryResolve(zone, x, y, out var selected) || selected.Remembered || !Eligible(selected.Owner)) return false;
            owner = selected.Owner; return true;
        }
        static bool Eligible(Entity owner)
        {
            if (!HasKnownRole(owner) || !owner.HasTag("Wall") || !owner.HasTag("Solid") || !owner.HasTag("Vegetation")
                || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item") || ForeignMeaning(owner)) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            var structure = Owned<DestructiblePart>(owner); var thermal = Owned<ThermalPart>(owner);
            if (render == null || !render.Visible || render.RenderString != "#" || render.RenderLayer != 0
                || render.ColorString != "&G" || render.BackgroundColor != "^g"
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || physics == null || !physics.Solid || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || structure == null || structure.Gone || structure.HP <= 0 || Owned<MaterialPart>(owner)?.MaterialID != "Plant"
                || Owned<ExaminablePart>(owner) == null || thermal == null
                || !float.IsFinite(thermal.Temperature) || !float.IsFinite(thermal.FreezeTemperature) || !float.IsFinite(thermal.FlameTemperature)
                || thermal.Temperature <= thermal.FreezeTemperature || thermal.Temperature >= thermal.FlameTemperature) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (owner.HasPart<StatusEffectsPart>() && Owned<StatusEffectsPart>(owner) == null) return false;
            if (!owner.HasPart<SpatialFootprintPart>()) return true;
            var footprint = Owned<SpatialFootprintPart>(owner);
            return footprint != null && footprint.Offsets.Length == 1 && footprint.Offsets[0] == Vector2Int.zero;
        }
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<BrainPart>() || owner.HasPart<Body>() || owner.HasPart<ContainerPart>() || owner.HasPart<RepairablePart>()
                || owner.HasPart<DoorPart>() || owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>()
                || owner.HasPart<HarvestablePart>() || owner.HasPart<CultivatedSoilPart>() || owner.HasPart<TriggerOnStepPart>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasPart<ForgePart>() || owner.HasPart<AlchemyStillPart>() || owner.HasPart<WellPart>();
        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part)
                { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (tiles == null) return;
            foreach (var tile in tiles)
                if (tile != null) { DestroyOwned(tile.sprite); DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
