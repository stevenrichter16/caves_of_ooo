using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Optional eastern service art. This semantic catalog owns cached
    /// frames, borrows optional textures, and never grants world/examine authority
    /// or advances services. Neutral furniture promises no stock, heat or access.</summary>
    public sealed class FlatPixelEastKitchenCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/east-kitchen";
        public const string CookResourcePath = "Sprites/Live16x24/east-kitchen-cook";
        public const string ServiceResourcePath = "Sprites/Live16x24/kitchen-service";
        private const int Width = 16, Height = 24, Frames = 18;
        private readonly Tile[] _tiles, _cookTiles, _serviceTiles;
        private bool _disposed;

        public FlatPixelEastKitchenCatalog()
            : this(Resources.Load<Texture2D>(ResourcePath), Resources.Load<Texture2D>(CookResourcePath),
                Resources.Load<Texture2D>(ServiceResourcePath)) { }

        /// <summary>Each optional borrowed group is independent. Missing or
        /// malformed injection stays unavailable and never loads Resources.</summary>
        public FlatPixelEastKitchenCatalog(Texture2D borrowedAtlas, Texture2D borrowedCook)
            : this(borrowedAtlas, borrowedCook, null) { }

        /// <summary>The third optional group is independently borrowed. Earlier
        /// injected constructors deliberately retain neutral service artwork.</summary>
        public FlatPixelEastKitchenCatalog(Texture2D borrowedAtlas, Texture2D borrowedCook, Texture2D borrowedService)
        {
            _tiles = Build(borrowedAtlas, Frames, "East kitchen");
            _cookTiles = Build(borrowedCook, 1, "Approved wayside cook");
            _serviceTiles = Build(borrowedService, 2, "Kitchen commission");
        }

        internal static bool HasKnownRole(Entity owner)
        {
            switch (owner?.BlueprintName)
            {
                case "SpreadWaysideCook": case "TinkersForge": case "Oven": case "Bed": case "Chair":
                case "ConnectedBatchPan": case "ConnectedKitchenEscrow": case "ConnectedKitchenPickup":
                case "ClaspbeanCrop": case "ClaspbeanPulp": case "ClaspbeanSeed": case "FieldMeal":
                case "CharmFlowers": case "CopperPipe": case "SteamVent": case "MillStone": return true;
                default: return false;
            }
        }

        /// <summary>Exact subject image only. The world caller must first select
        /// a current disclosed owner; portrait callers retain document/custody
        /// checks. Mutable inventory, mood, heat and job strings are not cues.</summary>
        public bool TryResolve(Entity owner, out Tile tile)
            => Resolve(owner, null, out tile);

        /// <summary>Current disclosed service state may replace neutral art.
        /// Job truth comes from the pure batch query; this never reconciles it.
        /// World top-owner and portrait authorization remain the caller's job.</summary>
        public bool TryResolvePresentation(Zone zone, Entity owner, out Tile tile)
            => Resolve(owner, zone, out tile);

        private bool Resolve(Entity owner, Zone zone, out Tile tile)
        {
            tile = null;
            if (_disposed || !HasKnownRole(owner) || owner.HasTag("Player")) return false;
            var render = Owned<RenderPart>(owner);
            var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || !SingleCell(owner)) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0)) return false;

            bool product = owner.BlueprintName == "ClaspbeanPulp" || owner.BlueprintName == "ClaspbeanSeed" || owner.BlueprintName == "FieldMeal";
            if (product)
            {
                if (!owner.HasTag("Item") || owner.HasTag("Creature") || owner.HasTag("Terrain") || !physics.Takeable || physics.Solid) return false;
                int productFrame = ProductFrame(owner);
                tile = productFrame >= 0 && _tiles != null ? _tiles[productFrame] : null;
                return tile != null;
            }
            if (owner.HasTag("Item") || physics.Takeable || physics.InInventory != null || physics.Equipped != null) return false;
            if (owner.BlueprintName == "SpreadWaysideCook")
            {
                tile = owner.HasTag("Creature") && _cookTiles != null ? _cookTiles[0] : null;
                return tile != null;
            }
            if (owner.HasTag("Creature")) return false;
            int frame = Frame(owner, physics);
            if (frame >= 0 && _serviceTiles != null && zone != null
                && (frame == 5 || frame == 7)
                && KitchenBatchPart.TryGetPresentation(zone, owner, out var state))
            {
                tile = _serviceTiles[state == KitchenBatchPresentation.CoveredWorking ? 0 : 1];
                return true;
            }
            tile = frame >= 0 && _tiles != null ? _tiles[frame] : null;
            return tile != null;
        }

        private static int Frame(Entity owner, PhysicsPart physics)
        {
            switch (owner.BlueprintName)
            {
                case "TinkersForge":
                    return !physics.Solid && owner.HasTag("Furniture") && Owned<ForgePart>(owner) != null ? 0 : -1;
                case "Oven":
                    var oven = Owned<CampfirePart>(owner);
                    return physics.Solid && oven != null && !oven.FiniteCooking ? 1 : -1;
                case "Bed": return !physics.Solid && owner.HasTag("Furniture") && Owned<BedPart>(owner) != null ? 2 : -1;
                case "Chair": return !physics.Solid && owner.HasTag("Furniture") && Owned<ChairPart>(owner) != null ? 3 : -1;
                case "ConnectedBatchPan":
                    var repair = Unique<RepairablePart>(owner);
                    var composition = Unique<CompositionPart>(owner);
                    return physics.Solid && Owned<KitchenBatchPart>(owner) != null
                        && repair != null && !repair.PortableEquipment && repair.RecipeId == "clay-batch-pan"
                        && KitchenBatchPart.HasMasonryComposition(composition) ? repair.Repaired ? 5 : 4 : -1;
                case "ConnectedKitchenEscrow": return !physics.Solid && Owned<ContainerPart>(owner) != null ? 6 : -1;
                case "ConnectedKitchenPickup": return !physics.Solid && Owned<ContainerPart>(owner) != null ? 7 : -1;
                case "ClaspbeanCrop":
                    var crop = Owned<CropPart>(owner);
                    return !physics.Solid && owner.HasTag("Crop") && crop != null && crop.HarvestAtMaturity
                        && crop.GrowthStage >= 0 && crop.GrowthStage <= 2
                        && crop.YieldBlueprint == "ClaspbeanPulp" && crop.YieldCount > 0 && crop.YieldCount <= CropYieldService.MaximumYieldUnits
                        && crop.SeedYieldBlueprint == "ClaspbeanSeed" && crop.SeedYieldCount > 0
                        && crop.SeedYieldCount <= CropYieldService.MaximumYieldUnits - crop.YieldCount ? 8 + crop.GrowthStage : -1;
                case "CharmFlowers": return !physics.Solid && owner.HasTag("Terrain") && owner.HasTag("Vegetation") ? 14 : -1;
                case "CopperPipe":
                    return !physics.Solid && owner.HasTag("Terrain") && Owned<MaterialPart>(owner)?.MaterialID == "Copper" ? 15 : -1;
                case "SteamVent":
                    // This is the fixed vent body, never a claim that it is hot,
                    // emitting steam or currently coating its cell with water.
                    return !physics.Solid && owner.HasTag("Terrain") && Owned<MaterialPart>(owner)?.MaterialID == "Steam"
                        && Owned<ThermalPart>(owner) != null && Owned<TileStateSourcePart>(owner) != null ? 16 : -1;
                case "MillStone":
                    var handling = Owned<HandlingPart>(owner);
                    return physics.Solid && handling != null && !handling.Carryable && !handling.Throwable ? 17 : -1;
                default: return -1;
            }
        }

        private static int ProductFrame(Entity owner)
        {
            switch (owner.BlueprintName)
            {
                case "ClaspbeanPulp":
                    return Owned<TonicPart>(owner)?.Drink == false && Owned<CureTonicPart>(owner)?.CureEffect == nameof(BleedingEffect) ? 11 : -1;
                case "ClaspbeanSeed":
                    var seed = Owned<SeedPart>(owner);
                    return seed != null && seed.CropBlueprint == "ClaspbeanCrop" && seed.RequireCultivatedSoil ? 12 : -1;
                case "FieldMeal": return Owned<FieldMealPart>(owner) != null ? 13 : -1;
                default: return -1;
            }
        }

        private static T Owned<T>(Entity owner) where T : Part
        { var part = owner.GetPart<T>(); return part?.ParentEntity == owner ? part : null; }
        private static T Unique<T>(Entity owner) where T : Part
        {
            T found = null;
            foreach (var part in owner.Parts)
                if (part is T candidate)
                { if (found != null || candidate.ParentEntity != owner) return null; found = candidate; }
            return found;
        }
        private static bool SingleCell(Entity owner)
        {
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint == null) return true;
            if (footprint.ParentEntity != owner) return false;
            var offsets = footprint.Offsets;
            return offsets.Length == 1 && offsets[0] == Vector2Int.zero;
        }
        private static Tile[] Build(Texture2D atlas, int count, string name)
        {
            if (atlas == null || atlas.width != Width * count || atlas.height != Height) return null;
            var tiles = new Tile[count];
            for (int i = 0; i < count; i++)
            {
                var sprite = Sprite.Create(atlas, new Rect(i * Width, 0, Width, Height), new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = name + " " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; tiles[i] = tile;
            }
            return tiles;
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true; DestroyFrames(_tiles); DestroyFrames(_cookTiles); DestroyFrames(_serviceTiles);
        }
        private static void DestroyFrames(Tile[] tiles)
        {
            if (tiles == null) return;
            foreach (var tile in tiles)
            {
                if (tile == null) continue;
                DestroyOwned(tile.sprite); DestroyOwned(tile);
            }
        }
        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
