using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Cached native frames for known opening owners. The texture is
    /// borrowed from Resources or the caller; this catalog owns only its generated
    /// sprites and tiles. Zone admission, visibility and background remain with
    /// FlatPixelGreyboxCatalog. Missing/malformed art resolves to its fallback.</summary>
    public sealed class FlatPixelOpeningArtCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/opening-base";
        private const int Width = 16, Height = 24, FrameCount = 10;
        private readonly Tile[] _tiles;
        private readonly Sprite[] _sprites;
        private bool _disposed;

        public FlatPixelOpeningArtCatalog() : this(Resources.Load<Texture2D>(ResourcePath)) { }

        /// <summary>Creates frames once without changing or taking ownership of
        /// the supplied texture. Null or a non-160x24 atlas leaves art unavailable.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas)
        {
            if (atlas == null || atlas.width != Width * FrameCount || atlas.height != Height) return;
            _tiles = new Tile[FrameCount];
            _sprites = new Sprite[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                var sprite = Sprite.Create(atlas, new Rect(i * Width, 0, Width, Height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 opening " + i;
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

        /// <summary>Returns an existing frame for an exact supported owner/role.
        /// Coordinates select stable reed variants; no world state is changed.</summary>
        public bool TryResolve(Entity owner, int x, int y, out Tile tile)
        {
            tile = null;
            if (_disposed || _tiles == null || owner == null) return false;
            var render = owner.GetPart<RenderPart>();
            if (render == null || render.ParentEntity != owner || !render.Visible) return false;
            int frame = FrameOf(owner, render.VisualID, x, y);
            if (frame < 0) return false;
            tile = _tiles[frame];
            return tile != null;
        }

        private static int FrameOf(Entity owner, string visual, int x, int y)
        {
            if (owner.HasTag("Player")) return 0;
            switch (owner.BlueprintName)
            {
                case "Warden": return owner.HasTag("Creature") ? 9 : -1;
                case "Wall":
                    if (visual == "reference-glade-dark-ruin") return 1;
                    // A worn crossing keeps this identity after its Wall tag is removed.
                    return visual == "reference-glade-low-wall" ? 2 : -1;
                case "Reeds": return visual == "reference-glade-pale-reeds" ? 3 + ((x + y) & 1) : -1;
                case "Bush": return visual == "reference-glade-green-grass" ? 5 : -1;
                case "Rubble": return visual == "reference-glade-gravel" ? 6 : -1;
                case "BrokenColumn": return visual == "reference-glade-dark-ruin" ? 6 : -1;
                case "CropRow": return 8;
                case "RipeCropRow":
                    var field = owner.GetPart<FieldHarvestPart>();
                    return field != null && field.ParentEntity == owner ? field.Harvested ? 8 : 7 : -1;
                default: return -1;
            }
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
