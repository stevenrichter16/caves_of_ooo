using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>One neutral image for the existing river shrine. Donation changes
    /// the visitor, not this image. Textures are borrowed; only the cached tile
    /// and sprite are owned.</summary>
    public sealed class FlatPixelRiverShrineCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/river-shrine";
        readonly Tile tile;
        bool disposed;

        public FlatPixelRiverShrineCatalog() : this(Resources.Load<Texture2D>(ResourcePath)) { }

        /// <summary>Null or malformed borrowed pixels remain unavailable without
        /// an implicit Resources lookup. Import settings are never modified.</summary>
        public FlatPixelRiverShrineCatalog(Texture2D borrowedTexture)
        {
            if (borrowedTexture == null || borrowedTexture.width != 16 || borrowedTexture.height != 24) return;
            var sprite = Sprite.Create(borrowedTexture, new Rect(0, 0, 16, 24), new Vector2(.5f, .5f),
                16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 river shrine"; sprite.hideFlags = HideFlags.HideAndDontSave;
            tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
        }

        internal static bool HasKnownRole(Entity owner) => owner?.BlueprintName == "RiverShrine";

        /// <summary>Semantic identity only: callers retain current graph, owner,
        /// disclosure and reader authorization. Other sanctuary-bearing objects
        /// do not acquire the river shrine's particular shape.</summary>
        public bool TryResolve(Entity owner, out Tile result)
        {
            result = null;
            if (disposed || tile == null || !HasKnownRole(owner) || owner.HasTag("Creature")
                || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var render = Owned<RenderPart>(owner);
            var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || render.RenderString != "_"
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || physics == null || physics.Solid || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null || !SingleCell(owner)
                || Owned<SanctuaryPart>(owner) == null) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0)) return false;
            result = tile; return true;
        }

        static T Owned<T>(Entity owner) where T : Part
        { var part = owner.GetPart<T>(); return part?.ParentEntity == owner ? part : null; }

        static bool SingleCell(Entity owner)
        {
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint == null) return true;
            if (footprint.ParentEntity != owner) return false;
            var offsets = footprint.Offsets;
            return offsets.Length == 1 && offsets[0] == Vector2Int.zero;
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (tile == null) return;
            DestroyOwned(tile.sprite); DestroyOwned(tile);
        }

        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
