using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Optional continuous native hedges in the existing admitted journey.
    /// Borrows its pixels and current-owner selector; owns only cached tiles and
    /// sprites. Connectivity describes disclosed owners, never new geometry.</summary>
    public sealed class FlatPixelHedgeCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/hedge-continuity";
        const int Width = 16, Height = 24, Masks = 16;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] tiles;
        bool disposed;

        public FlatPixelHedgeCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath), borrowedSelector) { }

        /// <summary>Missing or malformed injection remains unavailable. Neither
        /// borrowed dependency is replaced, changed or disposed by this adapter.</summary>
        public FlatPixelHedgeCatalog(Texture2D borrowedAtlas, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null || borrowedAtlas == null
                || borrowedAtlas.width != Width * Masks || borrowedAtlas.height != Height) return;
            tiles = new Tile[Masks];
            for (int mask = 0; mask < Masks; mask++)
            {
                var sprite = Sprite.Create(borrowedAtlas, new Rect(mask * Width, 0, Width, Height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 hedge " + mask;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
                tiles[mask] = tile;
            }
        }

        /// <summary>Uses the selected current owner and four visible, explored
        /// cardinal neighbors. Hedges do not acquire static masonry memory.</summary>
        public bool TryResolve(Zone zone, int x, int y, out Tile tile)
        {
            tile = null;
            if (!Available || !FlatPixelGreyboxCatalog.IsAdmitted(zone) || !Connects(zone, x, y)) return false;
            int mask = 0;
            if (Connects(zone, x, y - 1)) mask |= 1;
            if (Connects(zone, x + 1, y)) mask |= 2;
            if (Connects(zone, x, y + 1)) mask |= 4;
            if (Connects(zone, x - 1, y)) mask |= 8;
            tile = tiles[mask]; return true;
        }

        /// <summary>Isolated semantic portrait only. The reader remains
        /// responsible for document, subject custody and regional authority.</summary>
        public bool TryGetSubjectSprite(Entity owner, out Sprite sprite)
        {
            sprite = null;
            if (!Available || !IsEligible(owner)) return false;
            sprite = tiles[0].sprite; return true;
        }

        bool Available => !disposed && tiles != null;
        bool Connects(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            // Gate disclosure before selecting or inspecting any live condition.
            return cell != null && cell.Explored && cell.IsVisible
                && selector.TryResolve(zone, x, y, out var selected)
                && !selected.Remembered && IsEligible(selected.Owner);
        }

        internal static bool HasKnownRole(Entity owner) => owner?.BlueprintName == "Hedge";

        // Recognition is weaker than eligibility so an invalid known owner
        // cannot escape through the older static vegetation frame.
        internal static bool IsEligible(Entity owner)
        {
            if (!HasKnownRole(owner) || !owner.HasTag("Vegetation") || owner.HasTag("Creature")
                || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var render = owner.GetPart<RenderPart>();
            var physics = owner.GetPart<PhysicsPart>();
            var structure = owner.GetPart<DestructiblePart>();
            if (render == null || !ReferenceEquals(render.ParentEntity, owner) || !render.Visible
                || render.RenderString != "#" || !string.IsNullOrEmpty(render.VisualID)
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || physics == null || !ReferenceEquals(physics.ParentEntity, owner) || !physics.Solid
                || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || structure == null || !ReferenceEquals(structure.ParentEntity, owner)
                || structure.Gone || structure.HP <= 0) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (!ReferenceEquals(stack.ParentEntity, owner) || stack.StackCount <= 0)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint == null) return true;
            if (!ReferenceEquals(footprint.ParentEntity, owner)) return false;
            var offsets = footprint.Offsets;
            return offsets.Length == 1 && offsets[0] == Vector2Int.zero;
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
