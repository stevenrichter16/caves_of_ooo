using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Exact ordinary bed/chair and market-counter bodies shared across current world graphs.
    /// Borrows approved textures, owns only cached views. Callers retain
    /// placement, selection and document/disclosure authority.</summary>
    public sealed class FlatPixelSharedFurnitureCatalog : IDisposable
    {
        public const string MarketStallResourcePath = "Sprites/Environment/market_stall";
        readonly Tile bed, chair, marketStall;
        bool disposed;

        public FlatPixelSharedFurnitureCatalog()
            : this(Resources.Load<Texture2D>(FlatPixelEastKitchenCatalog.ResourcePath),
                Resources.Load<Texture2D>(MarketStallResourcePath)) { }

        /// <summary>Explicit null/malformed input remains unavailable; never loads
        /// a replacement. The original 288x24 texture is borrowed without edits.</summary>
        public FlatPixelSharedFurnitureCatalog(Texture2D eastKitchen18)
            : this(eastKitchen18, null) { }

        /// <summary>Independent borrowed inputs: 288x24 Bed/Chair atlas and full
        /// 16x16 counter. Null/malformed inputs stay unavailable without resource
        /// reload; this catalog owns only the resulting sprite/tile views.</summary>
        public FlatPixelSharedFurnitureCatalog(Texture2D eastKitchen18, Texture2D marketStall16)
        {
            bed = Build(eastKitchen18, 2, "bed");
            chair = Build(eastKitchen18, 3, "chair");
            if (marketStall16 != null && marketStall16.width == 16 && marketStall16.height == 16)
                marketStall = BuildFrame(marketStall16, new Rect(0, 0, 16, 16), "market stall");
        }

        /// <summary>Identity only, not permission to reveal, render or use a body.</summary>
        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Bed" || owner?.BlueprintName == "Chair" || owner?.BlueprintName == "MarketStall";

        /// <summary>Pure neutral body lookup. Owner, Occupied and Occupant never
        /// select artwork. In particular, do not dispatch IdleQuery: it reserves
        /// furniture. Existing single Render-event cues belong to the painter.</summary>
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !HasKnownRole(owner)
                || owner.HasTag("Player") || owner.HasTag("Creature") || owner.HasTag("Item")
                || owner.HasTag("Terrain") || owner.HasTag("Crop") || ForeignMeaning(owner)) return false;
            bool counter = owner.BlueprintName == "MarketStall";
            if (counter ? !owner.HasTag("Solid") : !owner.HasTag("Furniture")) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || render.RenderLayer != (counter ? 1 : 5) || physics == null
                || physics.Solid != counter || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || Owned<ExaminablePart>(owner) == null || !string.IsNullOrEmpty(render.VisualID)
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || !SingleCell(owner) || !OptionalOwned<StatusEffectsPart>(owner)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (counter)
            {
                // A solid counter is not a bed, container, merchant or terrain.
                // Existing single Render-event color cues remain painter-owned.
                if (render.RenderString == "=" && string.IsNullOrEmpty(render.BackgroundColor)
                    && !owner.HasPart<BedPart>() && !owner.HasPart<ChairPart>() && !owner.HasPart<InventoryPart>()) tile = marketStall;
            }
            else if (owner.BlueprintName == "Bed")
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
            return BuildFrame(texture, new Rect(frame * 16, 0, 16, 24), label);
        }
        static Tile BuildFrame(Texture2D texture, Rect rect, string label)
        {
            var sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 shared " + label; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrame(bed); DestroyFrame(chair); DestroyFrame(marketStall);
        }
        static void DestroyFrame(Tile tile)
        { if (tile == null) return; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
