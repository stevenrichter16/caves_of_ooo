using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Optional visible opening ground, exact East water and Nella soil. Borrows its atlas and strict
    /// cell selector; owns only cached sprites/tiles. Lower terrain is selected
    /// independently of a covering actor. No gameplay or crop time is advanced.</summary>
    public sealed class FlatPixelOpeningGroundCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/opening-ground";
        private const int Width = 16, Height = 24, Frames = 21;
        private const string NellaZoneID = "Overworld.11.8.0";
        private const string EastZoneID = "Overworld.12.10.0";
        private readonly FlatPixelFallbackCatalog _selector;
        private readonly Texture2D _atlas;
        private readonly Tile[] _tiles;
        private readonly Sprite[] _sprites;
        private bool _disposed;

        public FlatPixelOpeningGroundCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath), borrowedSelector) { }

        /// <summary>Missing or malformed injection stays unavailable. Neither
        /// dependency is changed, replaced or disposed by this companion.</summary>
        public FlatPixelOpeningGroundCatalog(Texture2D borrowedAtlas, FlatPixelFallbackCatalog borrowedSelector)
        {
            _selector = borrowedSelector;
            if (_selector == null || borrowedAtlas == null
                || borrowedAtlas.width != Width * Frames || borrowedAtlas.height != Height) return;
            _atlas = borrowedAtlas;
            _tiles = new Tile[Frames];
            _sprites = new Sprite[Frames];
            for (int i = 0; i < Frames; i++)
            {
                var sprite = Sprite.Create(_atlas, new Rect(i * Width, 0, Width, Height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 opening ground " + i;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite;
                tile.flags = TileFlags.None;
                tile.colliderType = Tile.ColliderType.None;
                _sprites[i] = sprite;
                _tiles[i] = tile;
            }
        }

        // Share only the exact material scope with world routing. The selector
        // below still proves the current graph/cell and visible owner authority.
        internal static bool IsSupportedZone(Zone zone)
            => ReferenceGladePlan.IsActive(zone)
                || (zone?.ZoneID == NellaZoneID || zone?.ZoneID == EastZoneID)
                    && FlatPixelGreyboxCatalog.IsAdmitted(zone);

        /// <summary>Returns opaque native ground with an untinted white draw
        /// color for visible opening cells, exact East water or Nella soil. Remembered cells,
        /// unsupported higher-priority ground and mixed coatings retain the
        /// caller's existing color/hazard presentation.</summary>
        public bool TryResolve(Zone zone, int x, int y, out Tile tile, out Color tint)
        {
            tile = null; tint = Color.white;
            if (_disposed || _atlas == null || _tiles == null || !IsSupportedZone(zone)) return false;
            bool opening = ReferenceGladePlan.IsActive(zone);
            bool eastWaterOnly = zone.ZoneID == EastZoneID;
            var cell = zone.GetCell(x, y);
            // Visibility is checked before the selector or any live state read.
            if (!CurrentCell(zone, cell) || !cell.Explored || !cell.IsVisible
                || !_selector.TryResolve(zone, x, y, out var selected) || selected.Remembered) return false;

            var coatings = zone.TileState.Get(x, y)?.Coatings;
            if (coatings != null)
            {
                for (int i = 0; i < coatings.Count; i++)
                {
                    var coating = coatings[i];
                    if (coating == null) return false;
                    if (coating.Turns <= 0) continue;
                    // Match the existing hazard renderer's sole-entry rule.
                    // Rejected live coatings never fall through to dry ground.
                    // Nella borrows soil only; its live coatings retain the
                    // existing generic ground and hazard presentation.
                    if ((!opening && !eastWaterOnly) || coatings.Count != 1 || coating.Id != "water") return false;
                    int mask = 0;
                    if (DisclosedDry(zone, x, y - 1)) mask |= 1;
                    if (DisclosedDry(zone, x + 1, y)) mask |= 2;
                    if (DisclosedDry(zone, x, y + 1)) mask |= 4;
                    if (DisclosedDry(zone, x - 1, y)) mask |= 8;
                    tile = _tiles[mask];
                    return true;
                }
            }

            // East borrows water only. Its dry fields, road and prepared soil
            // retain their existing presentation after expiry, removal or travel.
            if (eastWaterOnly) return false;

            bool field = false, fieldColor = false, road = false, prepared = false;
            int crops = 0;
            bool wet = false;
            bool usableBed = CultivatedSoilPart.IsCultivated(zone, cell);
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var owner = cell.Objects[i];
                if (CurrentCrop(zone, cell, owner, out var crop))
                {
                    // Multiple crops have no single unambiguous moisture cue.
                    if (++crops > 1 || crop.MoistureTicks < 0 || !CurrentPlacement(zone, cell, owner)) return false;
                    wet = crop.MoistureTicks > 0;
                }
                if (CurrentFieldColor(zone, cell, owner)) fieldColor = true;
                if (!CurrentTerrain(zone, cell, owner, out var render)) continue;
                bool oldField = IsField(owner, render);
                var soil = owner.GetPart<CultivatedSoilPart>();
                if (soil != null && !ReferenceEquals(soil.ParentEntity, owner)) continue;
                if (usableBed && soil != null && owner.HasTag("Plantable")
                    && (oldField || IsSupportedGrass(owner, render, opening))) prepared = true;
                if (oldField) field = true;
                else if (owner.BlueprintName == "RoadStone" && PlainVisual(render)) road = true;
            }

            // A prepared harvested RipeCropRow is still the same field owner.
            // Preparation outranks old stubble, while a crop on unprepared soil
            // keeps the caller's existing generic dry/wet crop color.
            if (prepared) tile = _tiles[wet ? 20 : 19];
            else if (crops > 0 || usableBed) return false;
            else if (field) tile = _tiles[18];
            else if (fieldColor) return false;
            else if (opening && road) tile = _tiles[16 + ((x + y) & 1)];
            return tile != null;
        }

        private bool DisclosedDry(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (!CurrentCell(zone, cell) || !cell.Explored || !cell.IsVisible
                || !_selector.TryResolve(zone, x, y, out var selected) || selected.Remembered) return false;
            var coatings = zone.TileState.Get(x, y)?.Coatings;
            // Unknown, mixed, expired-but-retained or other coating records do
            // not establish the disclosed coating-free ground needed for a bank.
            return coatings == null || coatings.Count == 0;
        }

        private static bool CurrentCell(Zone zone, Cell cell)
            => cell != null && ReferenceEquals(cell.ParentZone, zone)
                && ReferenceEquals(zone.GetCell(cell.X, cell.Y), cell);

        private static bool CurrentPlacement(Zone zone, Cell cell, Entity owner)
        {
            if (owner == null || !ReferenceEquals(owner.SpatialZone, zone)
                || !ReferenceEquals(zone.GetEntityCell(owner), cell) || !cell.Objects.Contains(owner)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint == null) return true;
            if (!ReferenceEquals(footprint.ParentEntity, owner) || !zone.IsFootprintCurrent(owner)) return false;
            var offsets = footprint.Offsets;
            return offsets.Length == 1 && offsets[0] == Vector2Int.zero;
        }

        private static bool CurrentTerrain(Zone zone, Cell cell, Entity owner, out RenderPart render)
        {
            render = owner?.GetPart<RenderPart>();
            if (!CurrentPlacement(zone, cell, owner) || !owner.HasTag("Terrain")
                || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item") || owner.HasTag("Solid")
                || render == null || !ReferenceEquals(render.ParentEntity, owner) || !render.Visible) return false;
            var physics = owner.GetPart<PhysicsPart>();
            if (physics == null || !ReferenceEquals(physics.ParentEntity, owner) || physics.Solid || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null) return false;
            var stack = owner.GetPart<StackerPart>();
            return stack == null || ReferenceEquals(stack.ParentEntity, owner) && stack.StackCount > 0;
        }

        private static bool CurrentCrop(Zone zone, Cell cell, Entity owner, out CropPart crop)
        {
            crop = owner?.GetPart<CropPart>();
            var render = owner?.GetPart<RenderPart>();
            return owner != null && ReferenceEquals(owner.SpatialZone, zone)
                && ReferenceEquals(zone.GetEntityCell(owner), cell) && cell.Objects.Contains(owner)
                && crop != null && ReferenceEquals(crop.ParentEntity, owner) && CropTime.IsCurrentOwner(crop, zone)
                && render != null && ReferenceEquals(render.ParentEntity, owner) && render.Visible
                && crop.GrowthStage >= 0 && crop.GrowthStage <= (crop.HarvestAtMaturity ? 2 : 1);
        }

        // Preserve the existing generic finite-field soil priority without
        // granting any unknown owner native field artwork.
        private static bool CurrentFieldColor(Zone zone, Cell cell, Entity owner)
        {
            var field = owner?.GetPart<FieldHarvestPart>();
            var render = owner?.GetPart<RenderPart>();
            var physics = owner?.GetPart<PhysicsPart>();
            return owner != null && field != null && ReferenceEquals(field.ParentEntity, owner)
                && ReferenceEquals(owner.SpatialZone, zone) && ReferenceEquals(zone.GetEntityCell(owner), cell)
                && cell.Objects.Contains(owner) && render != null && ReferenceEquals(render.ParentEntity, owner)
                && render.Visible && physics != null && ReferenceEquals(physics.ParentEntity, owner)
                && !physics.Takeable && !physics.Solid && physics.InInventory == null && physics.Equipped == null;
        }

        private static bool PlainVisual(RenderPart render)
            => string.IsNullOrEmpty(render.VisualID) && string.IsNullOrEmpty(render.VisualVariant)
                && string.IsNullOrEmpty(render.GlyphVariants);

        private static bool IsField(Entity owner, RenderPart render)
        {
            if (!PlainVisual(render)) return false;
            var field = owner.GetPart<FieldHarvestPart>();
            if (field != null && !ReferenceEquals(field.ParentEntity, owner)) return false;
            return owner.BlueprintName == "CropRow"
                || owner.BlueprintName == "RipeCropRow" && field != null;
        }

        private static bool IsSupportedGrass(Entity owner, RenderPart render, bool opening)
            => owner.BlueprintName == "Grass"
                && (opening ? render.VisualID == "reference-glade-ground" : string.IsNullOrEmpty(render.VisualID))
                && string.IsNullOrEmpty(render.VisualVariant) && render.GlyphVariants == "...,.'`";

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_tiles != null) foreach (var tile in _tiles) DestroyOwned(tile);
            if (_sprites != null) foreach (var sprite in _sprites) DestroyOwned(sprite);
        }

        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
