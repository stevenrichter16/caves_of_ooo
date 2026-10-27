using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Neutral equipment identities. The caller retains current graph,
    /// cell, visibility and reader custody authority. Textures are borrowed;
    /// only the cached sprites and tiles belong to this catalog.</summary>
    public sealed class FlatPixelIncidentalEquipmentCatalog : IDisposable
    {
        public const string LeatherArmorResourcePath = "Sprites/Live16x24/leather-armor";
        public const string HatchetResourcePath = "Sprites/Live16x24/hatchet";
        public const string CudgelResourcePath = "Sprites/Live16x24/cudgel";
        private readonly Tile _leatherArmor, _hatchet, _cudgel;
        private bool _disposed;

        public FlatPixelIncidentalEquipmentCatalog()
            : this(Resources.Load<Texture2D>(LeatherArmorResourcePath), Resources.Load<Texture2D>(HatchetResourcePath),
                   Resources.Load<Texture2D>(CudgelResourcePath)) { }

        /// <summary>Independent optional resources. Null or malformed injected
        /// textures stay unavailable without an implicit Resources lookup.</summary>
        public FlatPixelIncidentalEquipmentCatalog(Texture2D leatherArmor, Texture2D hatchet)
            : this(leatherArmor, hatchet, null) { }

        /// <summary>Independent optional identities; missing injected art stays unavailable.</summary>
        public FlatPixelIncidentalEquipmentCatalog(Texture2D leatherArmor, Texture2D hatchet, Texture2D cudgel)
        {
            _leatherArmor = Build(leatherArmor, "Leather armor");
            _hatchet = Build(hatchet, "Hatchet");
            _cudgel = Build(cudgel, "Cudgel");
        }

        internal static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "LeatherArmor" || owner?.BlueprintName == "Hatchet"
                || owner?.BlueprintName == "Cudgel";

        /// <summary>Resolve semantic art without authorizing world or inventory
        /// disclosure. Neutral frames do not encode condition, stats or provenance.</summary>
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (_disposed || !HasKnownRole(owner) || !owner.HasTag("Item")
                || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Terrain")
                || owner.HasTag("Crop") || owner.HasPart<CropPart>()) return false;
            var render = Owned<RenderPart>(owner);
            var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || !physics.Takeable || physics.Solid
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || !SingleCell(owner)) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0
                || stack.StackCount > stack.MaxStack)) return false;
            var equipment = Owned<EquippablePart>(owner);
            if (owner.BlueprintName == "LeatherArmor")
            {
                if (Owned<ArmorPart>(owner) == null || equipment?.Slot != "Body") return false;
                tile = _leatherArmor;
            }
            else
            {
                if (Owned<MeleeWeaponPart>(owner) == null || equipment?.Slot != "Hand") return false;
                tile = owner.BlueprintName == "Cudgel" ? _cudgel : _hatchet;
            }
            return tile != null;
        }

        private static T Owned<T>(Entity owner) where T : Part
        { var part = owner.GetPart<T>(); return part?.ParentEntity == owner ? part : null; }

        private static bool SingleCell(Entity owner)
        {
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint == null) return true;
            // A raw serialized-field mutation can leave a placed body's index
            // stale. Exact readers must reject the same stale body as world art.
            // Unplaced/carried equipment legitimately has no spatial zone.
            if (footprint.ParentEntity != owner
                || owner.SpatialZone != null && !owner.SpatialZone.IsFootprintCurrent(owner)) return false;
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
            DestroyFrame(_leatherArmor); DestroyFrame(_hatchet); DestroyFrame(_cudgel);
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
