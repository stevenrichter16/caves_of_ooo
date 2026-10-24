using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Optional connected masonry for the opening's exact Wall owners.
    /// Borrows the source texture and strict current-owner selector; owns only
    /// cached sprites and tiles. It never changes geometry or gameplay state.</summary>
    public sealed class FlatPixelBoundaryCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/opening-boundaries";
        private const int Width = 16, Height = 24, Masks = 16, Materials = 3;
        private readonly FlatPixelFallbackCatalog _selector;
        private readonly Texture2D _atlas;
        private readonly Tile[] _tiles;
        private readonly Sprite[] _sprites;
        private bool _disposed;

        public FlatPixelBoundaryCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath), borrowedSelector) { }

        /// <summary>Null or malformed injection stays unavailable. Neither
        /// dependency is replaced, mutated or disposed by this adapter.</summary>
        public FlatPixelBoundaryCatalog(Texture2D borrowedAtlas, FlatPixelFallbackCatalog borrowedSelector)
        {
            _selector = borrowedSelector;
            if (_selector == null || borrowedAtlas == null
                || borrowedAtlas.width != Width * Masks || borrowedAtlas.height != Height * Materials) return;
            _atlas = borrowedAtlas;
            _tiles = new Tile[Masks * Materials];
            _sprites = new Sprite[_tiles.Length];
            for (int material = 0; material < Materials; material++)
                for (int mask = 0; mask < Masks; mask++)
                {
                    int index = material * Masks + mask;
                    var sprite = Sprite.Create(_atlas,
                        new Rect(mask * Width, (Materials - 1 - material) * Height, Width, Height),
                        new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                    sprite.name = "Live16x24 opening boundary " + material + " " + mask;
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.name = sprite.name;
                    tile.hideFlags = HideFlags.HideAndDontSave;
                    tile.sprite = sprite;
                    tile.flags = TileFlags.None;
                    tile.colliderType = Tile.ColliderType.None;
                    _sprites[index] = sprite;
                    _tiles[index] = tile;
                }
        }

        /// <summary>Uses only the selected current cell and four disclosed
        /// cardinal neighbors. Remembered static masonry is an isolated cap;
        /// its live condition and hidden adjacency are never queried.</summary>
        public bool TryResolve(Zone zone, int x, int y, out Tile tile, out Color tint)
        {
            tile = null; tint = Color.white;
            if (!Available || !ReferenceGladePlan.IsActive(zone)
                || !_selector.TryResolve(zone, x, y, out var selected)
                || !TryMaterial(selected.Owner, selected.Remembered, out int material)) return false;

            int mask = 0;
            if (selected.Remembered) tint = new Color(.35f, .35f, .35f, 1);
            else
            {
                if (Connects(zone, x, y - 1)) mask |= 1;
                if (Connects(zone, x + 1, y)) mask |= 2;
                if (Connects(zone, x, y + 1)) mask |= 4;
                if (Connects(zone, x - 1, y)) mask |= 8;
            }
            tile = _tiles[material * Masks + mask];
            return true;
        }

        /// <summary>Semantic isolated material portrait only. The caller must
        /// authorize the current document, subject custody and opening scope.</summary>
        public bool TryGetSubjectSprite(Entity owner, out Sprite sprite, out Color tint)
        {
            sprite = null; tint = Color.white;
            if (!Available || !TryMaterial(owner, false, out int material)) return false;
            sprite = _sprites[material * Masks];
            return true;
        }

        private bool Available => !_disposed && _atlas != null && _tiles != null;

        private bool Connects(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            // Do not even select remembered neighbors: they cannot disclose a
            // live connection to the currently visible center.
            return cell != null && cell.Explored && cell.IsVisible
                && _selector.TryResolve(zone, x, y, out var selected)
                && !selected.Remembered && TryMaterial(selected.Owner, false, out _);
        }

        // Recognition is deliberately weaker than eligibility. A customized or
        // invalid known wall must not escape rejection through older wall art.
        internal static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Wall" && MaterialIndex(owner.GetPart<RenderPart>()?.VisualID) >= 0;

        internal static bool IsEligible(Entity owner, bool remembered)
            => TryMaterial(owner, remembered, out _);

        private static int MaterialOf(Entity owner)
        {
            if (owner?.BlueprintName != "Wall") return -1;
            var render = owner.GetPart<RenderPart>();
            if (render == null || !ReferenceEquals(render.ParentEntity, owner) || !render.Visible
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)) return -1;
            return MaterialIndex(render.VisualID);
        }

        private static int MaterialIndex(string visualID)
        {
            switch (visualID)
            {
                case "reference-glade-low-wall": return 0;
                case "reference-glade-dark-ruin": return 1;
                case "reference-glade-lit-wall": return 2;
                default: return -1;
            }
        }

        private static bool TryMaterial(Entity owner, bool remembered, out int material)
        {
            material = MaterialOf(owner);
            if (material < 0) return false;
            var physics = owner.GetPart<PhysicsPart>();
            var structure = owner.GetPart<DestructiblePart>();
            if (physics == null || !ReferenceEquals(physics.ParentEntity, owner) || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || structure == null || !ReferenceEquals(structure.ParentEntity, owner)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint != null)
            {
                if (!ReferenceEquals(footprint.ParentEntity, owner)) return false;
                var offsets = footprint.Offsets;
                if (offsets.Length != 1 || offsets[0] != Vector2Int.zero) return false;
            }
            // The low crossing deliberately has no Wall tag, but remains an
            // obstacle. Never use this live condition to update hidden memory.
            return remembered || !structure.Gone && structure.HP > 0
                && (physics.Solid || owner.HasTag("Solid"));
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
