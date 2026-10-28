using System;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
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
        public const string DaggerResourcePath = "Sprites/Environment/item_dagger";
        public const string ShortSwordResourcePath = "Sprites/Environment/item_sword";
        public const string MaceResourcePath = "Sprites/Environment/item_mace";
        public const string BucklerResourcePath = "Sprites/Live16x24/buckler";
        private readonly Tile _leatherArmor, _hatchet, _cudgel, _dagger, _shortSword, _mace, _buckler;
        private bool _disposed;

        public FlatPixelIncidentalEquipmentCatalog()
            : this(Resources.Load<Texture2D>(LeatherArmorResourcePath), Resources.Load<Texture2D>(HatchetResourcePath),
                   Resources.Load<Texture2D>(CudgelResourcePath), Resources.Load<Texture2D>(DaggerResourcePath),
                   Resources.Load<Texture2D>(ShortSwordResourcePath), Resources.Load<Texture2D>(MaceResourcePath),
                   Resources.Load<Texture2D>(BucklerResourcePath)) { }

        /// <summary>Independent optional resources. Null or malformed injected
        /// textures stay unavailable without an implicit Resources lookup.</summary>
        public FlatPixelIncidentalEquipmentCatalog(Texture2D leatherArmor, Texture2D hatchet)
            : this(leatherArmor, hatchet, null) { }

        /// <summary>Independent optional identities; missing injected art stays unavailable.</summary>
        public FlatPixelIncidentalEquipmentCatalog(Texture2D leatherArmor, Texture2D hatchet, Texture2D cudgel)
            : this(leatherArmor, hatchet, cudgel, null, null, null) { }

        /// <summary>Existing tall equipment and independent complete square sidearms.
        /// Injected null/malformed inputs never reload a resource implicitly.</summary>
        public FlatPixelIncidentalEquipmentCatalog(Texture2D leatherArmor, Texture2D hatchet, Texture2D cudgel,
            Texture2D dagger, Texture2D shortSword, Texture2D mace)
            : this(leatherArmor, hatchet, cudgel, dagger, shortSword, mace, null) { }

        /// <summary>Exact Buckler is independent of the existing equipment inputs.</summary>
        public FlatPixelIncidentalEquipmentCatalog(Texture2D leatherArmor, Texture2D hatchet, Texture2D cudgel,
            Texture2D dagger, Texture2D shortSword, Texture2D mace, Texture2D buckler)
        {
            _leatherArmor = Build(leatherArmor, "Leather armor");
            _hatchet = Build(hatchet, "Hatchet");
            _cudgel = Build(cudgel, "Cudgel");
            _dagger = Build(dagger, "Dagger", 16);
            _shortSword = Build(shortSword, "Short sword", 16);
            _mace = Build(mace, "Mace", 16);
            _buckler = Build(buckler, "Buckler");
        }

        internal static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "LeatherArmor" || owner?.BlueprintName == "Hatchet"
                || owner?.BlueprintName == "Cudgel" || owner?.BlueprintName == "Buckler" || IsSidearm(owner);

        internal static bool IsSidearm(Entity owner)
            => owner?.BlueprintName == "Dagger" || owner?.BlueprintName == "ShortSword" || owner?.BlueprintName == "Mace";

        /// <summary>Resolve semantic art without authorizing world or inventory
        /// disclosure. Neutral frames do not encode condition, stats or provenance.</summary>
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (_disposed || !HasKnownRole(owner) || !owner.HasTag("Item")
                || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Terrain")
                || owner.HasTag("Crop") || owner.HasPart<CropPart>()) return false;
            bool sidearm = IsSidearm(owner);
            bool buckler = owner.BlueprintName == "Buckler";
            if (sidearm && (!UniqueOwned<RenderPart>(owner) || !UniqueOwned<PhysicsPart>(owner)
                || !UniqueOwned<MeleeWeaponPart>(owner) || !UniqueOwned<EquippablePart>(owner)
                || !UniqueOwned<ExaminablePart>(owner) || ForeignSidearmMeaning(owner))) return false;
            if (buckler && (owner.HasTag("Corpse") || !UniqueOwned<RenderPart>(owner) || !UniqueOwned<PhysicsPart>(owner)
                || !UniqueOwned<ArmorPart>(owner) || !UniqueOwned<EquippablePart>(owner) || !UniqueOwned<ExaminablePart>(owner)
                || !OptionalUniqueOwned<StackerPart>(owner) || !OptionalUniqueOwned<StatusEffectsPart>(owner)
                || !OptionalUniqueOwned<SpatialFootprintPart>(owner) || ForeignBucklerMeaning(owner))) return false;
            var render = Owned<RenderPart>(owner);
            var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || !physics.Takeable || physics.Solid
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || !SingleCell(owner)) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0
                || stack.StackCount > stack.MaxStack)) return false;
            var equipment = Owned<EquippablePart>(owner);
            if (buckler)
            {
                if (equipment?.Slot != "Hand") return false;
                tile = _buckler;
            }
            else if (owner.BlueprintName == "LeatherArmor")
            {
                if (Owned<ArmorPart>(owner) == null || equipment?.Slot != "Body") return false;
                tile = _leatherArmor;
            }
            else
            {
                if (Owned<MeleeWeaponPart>(owner) == null || equipment?.Slot != "Hand") return false;
                switch (owner.BlueprintName)
                {
                    case "Dagger": tile = _dagger; break;
                    case "ShortSword": tile = _shortSword; break;
                    case "Mace": tile = _mace; break;
                    case "Cudgel": tile = _cudgel; break;
                    default: tile = _hatchet; break;
                }
            }
            return tile != null;
        }

        private static T Owned<T>(Entity owner) where T : Part
        { var part = owner.GetPart<T>(); return part?.ParentEntity == owner ? part : null; }

        private static bool OptionalUniqueOwned<T>(Entity owner) where T : Part
            => !owner.HasPart<T>() || UniqueOwned<T>(owner);

        private static bool UniqueOwned<T>(Entity owner) where T : Part
        {
            T found = null;
            foreach (var part in owner.Parts)
                if (part is T typed)
                {
                    if (found != null || typed.ParentEntity != owner) return false;
                    found = typed;
                }
            return found != null;
        }

        // These roles are portable weapons, never substitute service/actor bodies.
        private static bool ForeignSidearmMeaning(Entity owner)
            => owner.HasPart<ArmorPart>() || owner.HasPart<ContainerPart>() || owner.HasPart<ForgePart>()
                || owner.HasPart<WellPart>() || owner.HasPart<WellSitePart>() || owner.HasPart<CorpsePart>()
                || owner.HasPart<Body>() || owner.HasPart<BrainPart>() || owner.HasPart<DoorPart>();

        // Hand armor has no weapon, actor or world-service meaning. Condition,
        // armor values and price remain mutable and are not encoded in its art.
        private static bool ForeignBucklerMeaning(Entity owner)
            => owner.HasPart<MeleeWeaponPart>() || owner.HasPart<ContainerPart>() || owner.HasPart<ForgePart>()
                || owner.HasPart<WellPart>() || owner.HasPart<WellSitePart>() || owner.HasPart<AlchemyStillPart>()
                || owner.HasPart<CorpsePart>() || owner.HasPart<Body>() || owner.HasPart<BrainPart>()
                || owner.HasPart<DoorPart>() || owner.HasPart<TraderPart>() || owner.HasPart<ConversationPart>()
                || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>();

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

        private static Tile Build(Texture2D texture, string name, int height = 24)
        {
            if (texture == null || texture.width != 16 || texture.height != height) return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, 16, height), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
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
            DestroyFrame(_dagger); DestroyFrame(_shortSword); DestroyFrame(_mace);
            DestroyFrame(_buckler);
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
