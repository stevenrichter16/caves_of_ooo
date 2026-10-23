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
        public const string AnchorsResourcePath = "Sprites/Live16x24/opening-anchors";
        public const string StatesResourcePath = "Sprites/Live16x24/opening-states";
        public const string ItemsResourcePath = "Sprites/Live16x24/opening-items";
        public const string CellarResourcePath = "Sprites/Live16x24/opening-cellar";
        private const int Width = 16, Height = 24, FrameCount = 10;
        private readonly Tile[] _tiles;
        private readonly Sprite[] _sprites;
        private readonly Tile[] _castTiles;
        private readonly Sprite[] _castSprites;
        private readonly Tile[] _anchorTiles;
        private readonly Sprite[] _anchorSprites;
        private readonly Tile[] _stateTiles;
        private readonly Sprite[] _stateSprites;
        private readonly Tile[] _itemTiles;
        private readonly Sprite[] _itemSprites;
        private readonly Tile[] _cellarTiles;
        private readonly Sprite[] _cellarSprites;
        private bool _disposed;

        public FlatPixelOpeningArtCatalog() : this(Resources.Load<Texture2D>(ResourcePath),
            Resources.Load<Texture2D>(CastResourcePath), Resources.Load<Texture2D>(AnchorsResourcePath),
            Resources.Load<Texture2D>(StatesResourcePath), Resources.Load<Texture2D>(ItemsResourcePath),
            Resources.Load<Texture2D>(CellarResourcePath)) { }

        /// <summary>Creates frames once without changing or taking ownership of
        /// the supplied texture. Null or a non-160x24 atlas leaves art unavailable.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas) : this(atlas, null) { }

        /// <summary>Base and cast resources are independently optional and
        /// borrowed. A missing/malformed atlas cannot disable the other group.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas) : this(atlas, castAtlas, null) { }

        /// <summary>Each optional group owns only its generated frames. Earlier
        /// injection overloads deliberately omit anchors without loading Resources.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas)
            : this(atlas, castAtlas, anchorsAtlas, null) { }

        /// <summary>All resources are borrowed and independently optional. State
        /// frames follow current owned parts; no presentation state is persisted.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas)
            : this(atlas, castAtlas, anchorsAtlas, statesAtlas, null) { }

        /// <summary>Item identity is semantic, independent of ground placement.
        /// World and authorized examine callers enforce their own ownership context.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas)
            : this(atlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, null) { }

        /// <summary>The independently optional cellar atlas follows exact owned
        /// functional parts. It neither authorizes placement nor creates supplies.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas)
        {
            _tiles = BuildFrames(atlas, FrameCount, "base", out _sprites);
            _castTiles = BuildFrames(castAtlas, 4, "cast", out _castSprites);
            _anchorTiles = BuildFrames(anchorsAtlas, 12, "anchors", out _anchorSprites);
            _stateTiles = BuildFrames(statesAtlas, 12, "states", out _stateSprites);
            _itemTiles = BuildFrames(itemsAtlas, 12, "items", out _itemSprites);
            _cellarTiles = BuildFrames(cellarAtlas, 9, "cellar", out _cellarSprites);
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
            if (frame >= 0)
            {
                tile = _castTiles == null ? null : _castTiles[frame];
                return tile != null;
            }
            frame = AnchorFrameOf(owner, render.VisualID);
            if (frame >= 0)
            {
                tile = _anchorTiles == null ? null : _anchorTiles[frame];
                return tile != null;
            }
            frame = StateFrameOf(owner);
            if (frame >= 0)
            {
                tile = _stateTiles == null ? null : _stateTiles[frame];
                return tile != null;
            }
            frame = ItemFrameOf(owner, render.VisualID);
            if (frame >= 0)
            {
                tile = _itemTiles == null ? null : _itemTiles[frame];
                return tile != null;
            }
            frame = CellarFrameOf(owner, render.VisualID);
            if (frame < 0 || _cellarTiles == null) return false;
            tile = _cellarTiles[frame];
            return tile != null;
        }

        private static int CellarFrameOf(Entity owner, string visual)
        {
            if (!string.IsNullOrEmpty(visual) || owner.HasTag("Creature") || owner.HasTag("Player")) return -1;
            switch (owner.BlueprintName)
            {
                case "StairsUp": return Owned<StairsUpPart>(owner) != null && Owned<PhysicsPart>(owner) != null ? 0 : -1;
                case "Crate": return Owned<ContainerPart>(owner) != null ? 1 : -1;
                case "GleanersTimberPallet":
                    var harvest = Owned<HarvestablePart>(owner);
                    return Owned<PhysicsPart>(owner)?.Solid == true && Owned<HandlingPart>(owner) != null
                        && harvest != null && !harvest.Harvested && harvest.YieldBlueprint == "SalvagedTimber"
                        && harvest.YieldMin > 0 && harvest.YieldMax >= harvest.YieldMin
                        && harvest.YieldMax <= HarvestablePart.MaximumYieldCount
                        && harvest.YieldChance > 0 && harvest.YieldChance <= 100 ? 2 : -1;
                case "SootrootCrop":
                    var crop = Owned<CropPart>(owner);
                    return crop != null && crop.GrowthStage >= 0 && crop.GrowthStage <= 2
                        && crop.HarvestAtMaturity && crop.YieldBlueprint == "SootrootPulp" && crop.YieldCount > 0
                        && crop.SeedYieldBlueprint == "SootrootSeed" && crop.SeedYieldCount > 0 ? 3 + crop.GrowthStage : -1;
                case "SootrootPulp":
                    return PortableCellarProduct(owner) && Owned<TonicPart>(owner)?.Drink == false
                        && Owned<CureTonicPart>(owner)?.CureEffect == nameof(BurningEffect) ? 6 : -1;
                case "SootrootSeed":
                    var seed = Owned<SeedPart>(owner);
                    return PortableCellarProduct(owner) && seed != null && seed.CropBlueprint == "SootrootCrop"
                        && seed.RequireCultivatedSoil ? 7 : -1;
                case "StoneWall":
                    var stonePhysics = Owned<PhysicsPart>(owner);
                    return owner.HasTag("Wall") && stonePhysics != null
                        && stonePhysics.Solid && !stonePhysics.Takeable ? 8 : -1;
                default: return -1;
            }
        }

        private static bool PortableCellarProduct(Entity owner)
        {
            if (Owned<PhysicsPart>(owner)?.Takeable != true || owner.HasPart<CropPart>()) return false;
            var stack = owner.GetPart<StackerPart>();
            return stack == null || stack.ParentEntity == owner && stack.StackCount > 0;
        }

        private static int ItemFrameOf(Entity owner, string visual)
        {
            // An explicitly assigned visual is a reskin. Glyph/color changes do
            // not redefine item identity; they are the legacy fallback presentation.
            if (!string.IsNullOrEmpty(visual) || owner.HasTag("Creature") || owner.HasTag("Player")
                || owner.HasPart<CropPart>() || Owned<PhysicsPart>(owner)?.Takeable != true) return -1;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0)) return -1;
            switch (owner.BlueprintName)
            {
                case "Emberwheat": return Owned<FoodPart>(owner) != null && Owned<CookablePart>(owner)?.Into == "ToastedEmberwheat" ? 0 : -1;
                case "ToastedEmberwheat": return Owned<FoodPart>(owner) != null ? 1 : -1;
                case "DrawgourdShell": return Owned<WaterskinPart>(owner) != null ? 2 : -1;
                case "DrawgourdSeed": return Owned<SeedPart>(owner)?.CropBlueprint == "DrawgourdCrop" ? 3 : -1;
                case "Mushroom": return Owned<FoodPart>(owner) != null && Owned<CookablePart>(owner)?.Into == "RoastedMushroom" ? 4 : -1;
                case "RoastedMushroom": return Owned<FoodPart>(owner) != null ? 5 : -1;
                case "GlowQuartz": return Owned<MineralPreparationPart>(owner) != null ? 6 : -1;
                case "HealingTonic": return !string.IsNullOrWhiteSpace(Owned<TonicPart>(owner)?.Healing) ? 7 : -1;
                case "DriedMeat": return Owned<FoodPart>(owner) != null ? 8 : -1;
                case "FireClay": return 9;
                case "SalvagedTimber": return 10;
                case "MarlbackCorpse":
                    if (!owner.HasTag("Corpse") || !owner.Properties.TryGetValue("SourceID", out var sourceId)
                        || string.IsNullOrWhiteSpace(sourceId) || !owner.Properties.TryGetValue("SourceBlueprint", out var species)) return -1;
                    return species == "MarlbackScrabbler" || species == "MarlbackGleaner" ? 11 : -1;
                default: return -1;
            }
        }

        private static int StateFrameOf(Entity owner)
        {
            switch (owner.BlueprintName)
            {
                case "GleanersBuckledWicket":
                    var repair = Owned<RepairablePart>(owner); var door = Owned<DoorPart>(owner);
                    if (repair == null || door == null || repair.PortableEquipment || repair.RecipeId != "timber-wicket-hinge") return -1;
                    return RepairablePart.BlocksFunction(owner) ? 0 : door.IsClosed ? 1 : 2;
                case "Campfire":
                    var fire = Owned<CampfirePart>(owner); var heat = Owned<ThermalPart>(owner);
                    if (fire == null || !fire.FiniteCooking || heat == null || Owned<FuelPart>(owner) == null) return -1;
                    if (OwnedBurning(owner)) return 5;
                    if (!Finite(heat.Temperature)) return -1;
                    return heat.Temperature >= CookingService.MinimumFiniteCookingTemperature ? 4 : 3;
                case "DrawgourdCrop":
                    var crop = Owned<CropPart>(owner);
                    if (crop == null || crop.YieldBlueprint != "DrawgourdShell" || crop.GrowthStage < 0 || crop.GrowthStage > 2
                        || crop.GrowthStage == 2 && !crop.HarvestAtMaturity) return -1;
                    return 6 + crop.GrowthStage;
                case "Torch":
                    var torch = Owned<TorchLightPart>(owner); var light = Owned<LightSourcePart>(owner);
                    var thermal = Owned<ThermalPart>(owner); var fuel = Owned<FuelPart>(owner);
                    if (torch == null || light == null || thermal == null || fuel == null) return -1;
                    return OwnedBurning(owner) || torch.ControlledFlameActive ? 10 : 9;
                case "Waterskin": return Owned<WaterskinPart>(owner) != null ? 11 : -1;
                default: return -1;
            }
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool OwnedBurning(Entity owner)
        {
            var effect = Owned<StatusEffectsPart>(owner)?.GetEffect<BurningEffect>();
            return effect != null && effect.Owner == owner;
        }

        private static int AnchorFrameOf(Entity owner, string visual)
        {
            // Blueprint identity precedes a VisualID: the quartz source and an
            // ordinary masonry wall deliberately share the same lit-wall role.
            switch (owner.BlueprintName)
            {
                case "RepairLinedWell":
                    var repair = Owned<RepairablePart>(owner);
                    return Owned<WellPart>(owner) != null && repair != null
                        && !repair.PortableEquipment && repair.RecipeId == "clay-well-lining"
                        ? repair.Repaired ? 1 : 0 : -1;
                case "SpreadDrawPoint":
                    var pool = Owned<LiquidPoolPart>(owner);
                    return pool != null && pool.LiquidId == "water" ? pool.Volume > 0 ? 2 : 3 : -1;
                case "StairsDown": return Owned<StairsDownPart>(owner) != null ? 4 : -1;
                case "FallenBeam": return Owned<PhysicsPart>(owner) != null && Owned<HandlingPart>(owner) != null ? 5 : -1;
                case "Chest": return Owned<ContainerPart>(owner) != null ? 6 : -1;
                case "WoodenBarrel": return Owned<ContainerPart>(owner) != null ? 7 : -1;
                case "Signpost": return 8;
                case "MushroomRing": return UnspentHarvest(owner, "Mushroom") ? 9 : -1;
                case "GlowQuartzVein": return visual == "reference-glade-lit-wall"
                    && UnspentHarvest(owner, "GlowQuartz") ? 10 : -1;
                case "Wall": return visual == "reference-glade-lit-wall" ? 11 : -1;
                default: return -1;
            }
        }

        private static T Owned<T>(Entity owner) where T : Part
        {
            var part = owner.GetPart<T>();
            return part != null && part.ParentEntity == owner ? part : null;
        }

        private static bool UnspentHarvest(Entity owner, string yield)
        {
            var harvest = Owned<HarvestablePart>(owner);
            return harvest != null && !harvest.Harvested && harvest.YieldBlueprint == yield;
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
            if (_anchorTiles != null) foreach (var tile in _anchorTiles) DestroyOwned(tile);
            if (_anchorSprites != null) foreach (var sprite in _anchorSprites) DestroyOwned(sprite);
            if (_stateTiles != null) foreach (var tile in _stateTiles) DestroyOwned(tile);
            if (_stateSprites != null) foreach (var sprite in _stateSprites) DestroyOwned(sprite);
            if (_itemTiles != null) foreach (var tile in _itemTiles) DestroyOwned(tile);
            if (_itemSprites != null) foreach (var sprite in _itemSprites) DestroyOwned(sprite);
            if (_cellarTiles != null) foreach (var tile in _cellarTiles) DestroyOwned(tile);
            if (_cellarSprites != null) foreach (var sprite in _cellarSprites) DestroyOwned(sprite);
        }

        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
