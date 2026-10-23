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
        public const string CastResourcePath = "Sprites/Live16x24/opening-cast";
        private const int Width = 16, Height = 24, FrameCount = 10;
        private readonly Tile[] _tiles;
        private readonly Sprite[] _sprites;
        private readonly Tile[] _castTiles;
        private readonly Sprite[] _castSprites;
        private bool _disposed;

        public FlatPixelOpeningArtCatalog() : this(Resources.Load<Texture2D>(ResourcePath),
            Resources.Load<Texture2D>(CastResourcePath)) { }

        /// <summary>Creates frames once without changing or taking ownership of
        /// the supplied texture. Null or a non-160x24 atlas leaves art unavailable.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas) : this(atlas, null) { }

        /// <summary>Base and cast resources are independently optional and
        /// borrowed. A missing/malformed atlas cannot disable the other group.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas)
        {
            _tiles = BuildFrames(atlas, FrameCount, "base", out _sprites);
            _castTiles = BuildFrames(castAtlas, 4, "cast", out _castSprites);
        }

        private static Tile[] BuildFrames(Texture2D atlas, int count, string group, out Sprite[] sprites)
        {
            sprites = null;
            if (atlas == null || atlas.width != Width * count || atlas.height != Height) return null;
            var tiles = new Tile[count];
            sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var sprite = Sprite.Create(atlas, new Rect(i * Width, 0, Width, Height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 opening " + group + " " + i;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite;
                tile.flags = TileFlags.None;
                tile.colliderType = Tile.ColliderType.None;
                sprites[i] = sprite;
                tiles[i] = tile;
            }
            return tiles;
        }

        /// <summary>Returns an existing frame for an exact supported owner/role.
        /// Coordinates select stable reed variants; no world state is changed.</summary>
        public bool TryResolve(Entity owner, int x, int y, out Tile tile)
        {
            tile = null;
            if (_disposed || owner == null) return false;
            var render = owner.GetPart<RenderPart>();
            if (render == null || render.ParentEntity != owner || !render.Visible) return false;
            int frame = FrameOf(owner, render.VisualID, x, y);
            if (frame >= 0)
            {
                tile = _tiles == null ? null : _tiles[frame];
                return tile != null;
            }
            frame = CastFrameOf(owner);
            if (frame < 0 || _castTiles == null) return false;
            tile = _castTiles[frame];
            return tile != null;
        }

        private static int CastFrameOf(Entity owner)
        {
            if (!owner.HasTag("Creature")) return -1;
            switch (owner.BlueprintName)
            {
                case "Villager": return 0;
                case "PetDog": return 1;
                case "MarlbackScrabbler": return 2;
                case "MarlbackGleaner": return 3;
                default: return -1;
            }
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
            if (_castTiles != null) foreach (var tile in _castTiles) DestroyOwned(tile);
            if (_castSprites != null) foreach (var sprite in _castSprites) DestroyOwned(sprite);
        }

        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
