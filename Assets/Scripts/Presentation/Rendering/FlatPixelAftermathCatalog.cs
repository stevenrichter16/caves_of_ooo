using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Neutral remains and their existing venom product. The caller
    /// retains graph, visibility and reader custody authority. Corpse source
    /// properties are saved identity, not a reference to a surviving animal.</summary>
    public sealed class FlatPixelAftermathCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/opening-aftermath";
        const int Width = 16, Height = 24, Frames = 3;
        readonly Tile[] tiles;
        bool disposed;

        public FlatPixelAftermathCatalog() : this(Resources.Load<Texture2D>(ResourcePath)) { }

        /// <summary>Borrow an optional atlas. Null or malformed input stays
        /// unavailable without loading a replacement or changing import settings.</summary>
        public FlatPixelAftermathCatalog(Texture2D borrowedAtlas)
        {
            if (borrowedAtlas == null || borrowedAtlas.width != Width * Frames || borrowedAtlas.height != Height) return;
            tiles = new Tile[Frames];
            for (int i = 0; i < Frames; i++)
            {
                var sprite = Sprite.Create(borrowedAtlas, new Rect(i * Width, 0, Width, Height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 aftermath " + i;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
                tiles[i] = tile;
            }
        }

        internal static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "VenomGland" || owner?.BlueprintName == "CreatureCorpse"
                && (owner.GetProperty("SourceBlueprint") == "Magpie" || owner.GetProperty("SourceBlueprint") == "Viper");

        /// <summary>Semantic artwork only, including carried subjects. Successful
        /// butchery consumes its source even when the yield roll fails; there is
        /// no empty remains stage. Other changes do not promise a particular yield.</summary>
        public bool TryResolve(Entity owner, out Tile result)
        {
            result = null;
            if (disposed || tiles == null || !HasKnownRole(owner)
                || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Terrain")
                || owner.HasTag("Crop") || owner.HasPart<CropPart>()) return false;
            var render = Owned<RenderPart>(owner);
            var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || !string.IsNullOrEmpty(render.VisualID)
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || physics == null || !physics.Takeable || physics.Solid || !SingleCell(owner)) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0
                || stack.StackCount > stack.MaxStack)) return false;

            int frame;
            if (owner.BlueprintName == "CreatureCorpse")
            {
                // Corpse owners inherit PhysicalObject, not Item. Their neutral
                // body does not imply that a harvest action or product is available.
                if (!owner.HasTag("Corpse") || string.IsNullOrWhiteSpace(owner.GetProperty("SourceID"))) return false;
                var harvest = owner.GetPart<HarvestablePart>();
                if (harvest != null && (harvest.ParentEntity != owner || harvest.Harvested)) return false;
                frame = owner.GetProperty("SourceBlueprint") == "Magpie" ? 0 : 1;
            }
            else
            {
                if (!owner.HasTag("Item") || owner.HasTag("Corpse") || !ToxicReagent(owner)) return false;
                frame = 2;
            }
            result = tiles[frame];
            return true;
        }

        static T Owned<T>(Entity owner) where T : Part
        { var part = owner.GetPart<T>(); return part?.ParentEntity == owner ? part : null; }

        static bool ToxicReagent(Entity owner)
        {
            var reagent = Owned<ReagentPart>(owner);
            if (reagent == null) return false;
            var properties = reagent.GetProperties(); // Cached by the reagent's saved raw value.
            for (int i = 0; i < properties.Count; i++)
                if (properties[i] != null && properties[i].Property == BrewProperties.Toxic && properties[i].Potency > 0)
                    return true;
            return false;
        }

        static bool SingleCell(Entity owner)
        {
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint == null) return true;
            if (footprint.ParentEntity != owner
                || owner.SpatialZone != null && !owner.SpatialZone.IsFootprintCurrent(owner)) return false;
            var offsets = footprint.Offsets;
            return offsets.Length == 1 && offsets[0] == Vector2Int.zero;
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (tiles == null) return;
            foreach (var tile in tiles)
            {
                if (tile == null) continue;
                DestroyOwned(tile.sprite); DestroyOwned(tile);
            }
        }

        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
