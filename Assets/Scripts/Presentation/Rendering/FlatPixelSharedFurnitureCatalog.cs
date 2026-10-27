using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Exact ordinary bed/chair bodies shared across current world graphs.
    /// Borrows the approved East atlas, owns only two cached views. Callers retain
    /// placement, selection and document/disclosure authority.</summary>
    public sealed class FlatPixelSharedFurnitureCatalog : IDisposable
    {
        readonly Tile bed, chair;
        bool disposed;

        public FlatPixelSharedFurnitureCatalog()
            : this(Resources.Load<Texture2D>(FlatPixelEastKitchenCatalog.ResourcePath)) { }

        /// <summary>Explicit null/malformed input remains unavailable; never loads
        /// a replacement. The original 288x24 texture is borrowed without edits.</summary>
        public FlatPixelSharedFurnitureCatalog(Texture2D eastKitchen18)
        {
            bed = Build(eastKitchen18, 2, "bed");
            chair = Build(eastKitchen18, 3, "chair");
        }

        /// <summary>Identity only, not permission to reveal, render or use a body.</summary>
        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Bed" || owner?.BlueprintName == "Chair";

        /// <summary>Pure neutral body lookup. Owner, Occupied and Occupant never
        /// select artwork. In particular, do not dispatch IdleQuery: it reserves
        /// furniture. Existing single Render-event cues belong to the painter.</summary>
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !HasKnownRole(owner) || !owner.HasTag("Furniture")
                || owner.HasTag("Player") || owner.HasTag("Creature") || owner.HasTag("Item")
                || owner.HasTag("Terrain") || owner.HasTag("Crop") || ForeignMeaning(owner)) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || render.RenderLayer != 5 || physics == null
                || physics.Solid || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || Owned<ExaminablePart>(owner) == null || !string.IsNullOrEmpty(render.VisualID)
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || !SingleCell(owner) || !OptionalOwned<StatusEffectsPart>(owner)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (owner.BlueprintName == "Bed")
            {
                if (render.RenderString == "=" && Owned<BedPart>(owner) != null && !owner.HasPart<ChairPart>()) tile = bed;
            }
            else if (render.RenderString == "h" && Owned<ChairPart>(owner) != null && !owner.HasPart<BedPart>()) tile = chair;
            return tile != null;
        }

        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<Body>() || owner.HasPart<BrainPart>() || owner.GetStat("Hitpoints") != null
                || owner.HasPart<MaterialPart>() || owner.HasPart<DestructiblePart>() || owner.HasPart<ThermalPart>()
                || owner.HasPart<ContainerPart>() || owner.HasPart<LockPart>() || owner.HasPart<ForgePart>()
                || owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<HarvestablePart>() || owner.HasPart<LiquidPoolPart>() || owner.HasPart<WellPart>()
                || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>() || owner.HasPart<ConversationPart>()
                || owner.HasPart<TraderPart>() || owner.HasPart<RegionalSignpostPart>() || owner.HasPart<CampfirePart>()
                || owner.HasPart<AlchemyStillPart>() || owner.HasPart<SoddenPreparationPart>() || owner.HasPart<KitchenBatchPart>()
                || owner.HasPart<TriggerOnStepPart>() || owner.HasPart<StairsDownPart>() || owner.HasPart<StairsUpPart>()
                || owner.HasPart<TileStateSourcePart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>()
                || owner.HasPart<BurnOffGasPart>() || owner.HasPart("Restable") || owner.HasTag("Restable")
                || owner.HasTag("Plantable") || owner.HasTag("WellGroundMarker")
                || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
        static bool OptionalOwned<T>(Entity owner) where T : Part => !owner.HasPart<T>() || Owned<T>(owner) != null;
        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part)
                { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        static bool SingleCell(Entity owner)
        {
            if (!owner.HasPart<SpatialFootprintPart>()) return true;
            var part = Owned<SpatialFootprintPart>(owner);
            return part != null && part.Offsets.Length == 1 && part.Offsets[0] == Vector2Int.zero;
        }
        static Tile Build(Texture2D texture, int frame, string label)
        {
            if (texture == null || texture.width != 288 || texture.height != 24) return null;
            var sprite = Sprite.Create(texture, new Rect(frame * 16, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 shared " + label; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrame(bed); DestroyFrame(chair);
        }
        static void DestroyFrame(Tile tile)
        { if (tile == null) return; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
