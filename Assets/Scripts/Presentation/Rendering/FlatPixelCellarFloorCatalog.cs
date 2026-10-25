using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Optional quiet material for the actual visible cellar Floor.
    /// Borrows its atlas and owns only cached tiles/sprites. Queries never alter
    /// terrain, crop time, simulation RNG, disclosure or foreground ownership.</summary>
    public sealed class FlatPixelCellarFloorCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/cellar-floor";
        private readonly Texture2D _atlas;
        private readonly Tile[] _tiles;
        private readonly Sprite[] _sprites;
        private bool _disposed;

        public FlatPixelCellarFloorCatalog() : this(Resources.Load<Texture2D>(ResourcePath)) { }

        /// <summary>Missing or malformed borrowed art remains unavailable;
        /// injection never falls back to Resources or changes the texture.</summary>
        public FlatPixelCellarFloorCatalog(Texture2D borrowedAtlas)
        {
            if (borrowedAtlas == null || borrowedAtlas.width != 64 || borrowedAtlas.height != 24) return;
            _atlas = borrowedAtlas;
            _tiles = new Tile[4]; _sprites = new Sprite[4];
            for (int i = 0; i < _tiles.Length; i++)
            {
                var sprite = Sprite.Create(_atlas, new Rect(i * 16, 0, 16, 24),
                    new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 cellar floor " + i;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
                _sprites[i] = sprite; _tiles[i] = tile;
            }
        }

        public bool TryResolve(Zone zone, int x, int y, out Tile tile, out Entity floor)
        {
            tile = null; floor = null;
            if (_disposed || _atlas == null || zone?.ZoneID != GleanersCellarBuilder.ZoneID
                || !FlatPixelGreyboxCatalog.IsAdmitted(zone)) return false;
            var cell = zone.GetCell(x, y);
            // Disclosure precedes every live owner/soil/hazard query.
            if (cell == null || cell.ParentZone != zone || cell.X != x || cell.Y != y
                || !cell.Explored || !cell.IsVisible) return false;
            var state = zone.TileState.Get(x, y);
            if (state != null && !state.IsEmpty || CultivatedSoilPart.IsCultivated(zone, cell)) return false;

            Entity candidate = null;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var owner = cell.Objects[i];
                if (owner == null || owner.SpatialZone != zone || zone.GetEntityCell(owner) != cell) continue;
                if (owner.BlueprintName == "Floor")
                {
                    // A malformed Floor anchored here still makes the material
                    // ambiguous. Only stale entries anchored elsewhere are ignored.
                    if (candidate != null || !CurrentFootprint(zone, cell, owner) || !OrdinaryFloor(owner)) return false;
                    candidate = owner;
                    continue;
                }
                if (!CurrentFootprint(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                // Ground state wins independently of any actor/item covering it.
                // Refuse unknown/malformed values within these state families
                // rather than interpreting them as a clean stone surface.
                if (CompetingGround(owner)) return false;
            }
            if (candidate == null) return false;
            floor = candidate; tile = _tiles[FrameAt(x, y)]; return true;
        }

        private static bool CurrentFootprint(Zone zone, Cell cell, Entity owner)
        {
            var footprint = owner.GetPart<SpatialFootprintPart>();
            return footprint == null || footprint.ParentEntity == owner && zone.IsFootprintCurrent(owner)
                && SpatialQuery.DistanceToCell(zone, owner, cell.X, cell.Y) == 0;
        }

        private static bool VisiblePhysicalOwner(Entity owner)
        {
            var render = owner.GetPart<RenderPart>();
            var physics = owner.GetPart<PhysicsPart>();
            var stack = owner.GetPart<StackerPart>();
            return render != null && render.ParentEntity == owner && render.Visible
                && physics != null && physics.ParentEntity == owner && physics.InInventory == null && physics.Equipped == null
                && (stack == null || stack.ParentEntity == owner && stack.StackCount > 0);
        }

        private static bool CompetingGround(Entity owner)
            => owner.GetPart<CropPart>() != null || owner.GetPart<FieldHarvestPart>() != null
                || owner.GetPart<CultivatedSoilPart>() != null || owner.GetPart<LiquidPoolPart>() != null
                || owner.GetPart<GasPoolPart>() != null || owner.BlueprintName == "CropRow"
                || owner.BlueprintName == "RipeCropRow" || owner.BlueprintName == "RoadStone";

        private static bool OrdinaryFloor(Entity owner)
        {
            if (!VisiblePhysicalOwner(owner) || !owner.HasTag("Terrain") || owner.HasTag("Solid")
                || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")
                || owner.HasTag("Vegetation") || owner.HasTag("Barren") || CompetingGround(owner)) return false;
            var physics = owner.GetPart<PhysicsPart>();
            var render = owner.GetPart<RenderPart>();
            if (physics.Solid || physics.Takeable || !string.IsNullOrEmpty(render.VisualID)
                || !string.IsNullOrEmpty(render.VisualVariant) || render.GlyphVariants != "...,.'\u0060") return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint != null && (footprint.Offsets.Length != 1 || footprint.Offsets[0] != Vector2Int.zero)) return false;
            int renders = 0, physical = 0;
            for (int i = 0; i < owner.Parts.Count; i++)
            {
                if (owner.Parts[i] is RenderPart) renders++;
                if (owner.Parts[i] is PhysicsPart) physical++;
            }
            return renders == 1 && physical == 1;
        }

        private static int FrameAt(int x, int y)
        {
            // Three quarters of coordinate buckets stay unmarked. Mix the
            // coordinates without simulation RNG, IDs, clocks or neighbors.
            uint value = unchecked((uint)x * 0x9e3779b1u ^ (uint)y * 0x85ebca77u);
            value ^= value >> 16;
            value = unchecked(value * 0x7feb352du);
            value ^= value >> 15;
            return (value & 15u) < 12u ? 0 : 1 + (int)((value >> 4) % 3u);
        }

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
