using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Two exact static bodies. Textures are borrowed; only cached
    /// tiles/sprites are owned. Graph, cell and reader authority stay with the
    /// existing presentation callers. This is not a motion or gameplay service.</summary>
    public sealed class FlatPixelIncidentalActorCatalog : IDisposable
    {
        public const string FarmerResourcePath = "Sprites/Live16x24/farmer";
        public const string GrazerResourcePath = "Sprites/Live16x24/reedback-grazer";
        private readonly Tile _farmer, _grazer;
        private bool _disposed;

        public FlatPixelIncidentalActorCatalog()
            : this(Resources.Load<Texture2D>(FarmerResourcePath), Resources.Load<Texture2D>(GrazerResourcePath)) { }

        /// <summary>Optional groups are independent. Missing or malformed
        /// injected textures remain unavailable; no implicit Resources lookup.</summary>
        public FlatPixelIncidentalActorCatalog(Texture2D farmer, Texture2D grazer)
        {
            _farmer = Build(farmer, "Farmer");
            _grazer = Build(grazer, "Approved reedback grazer");
        }

        internal static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Farmer" || owner?.BlueprintName == "ReedbackGrazer";

        /// <summary>Semantic image, not visibility/custody authorization. Neutral
        /// body art does not encode health, trade stock, mood or forage progress.</summary>
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (_disposed || !HasKnownRole(owner) || !owner.HasTag("Creature")
                || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var render = Owned<RenderPart>(owner);
            var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || !SingleCell(owner)
                || Owned<BrainPart>(owner) == null || Owned<Body>(owner) == null) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0)) return false;
            if (owner.BlueprintName == "Farmer")
            {
                if (render.RenderString != "@") return false;
                tile = _farmer;
            }
            else
            {
                if (render.RenderString != "g" || Owned<SpreadGrazerPart>(owner) == null) return false;
                tile = _grazer;
            }
            return tile != null;
        }

        private static T Owned<T>(Entity owner) where T : Part
        { var part = owner.GetPart<T>(); return part?.ParentEntity == owner ? part : null; }

        private static bool SingleCell(Entity owner)
        {
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint == null) return true;
            if (footprint.ParentEntity != owner) return false;
            var offsets = footprint.Offsets;
            return offsets.Length == 1 && offsets[0] == Vector2Int.zero;
        }

        private static Tile Build(Texture2D texture, string name)
        {
            if (texture == null || texture.width != 16 || texture.height != 24) return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 " + name; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
            tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            DestroyFrame(_farmer); DestroyFrame(_grazer);
        }
        private static void DestroyFrame(Tile tile)
        {
            if (tile == null) return;
            DestroyOwned(tile.sprite); DestroyOwned(tile);
        }
        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
