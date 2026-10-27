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
        public const string VegetationResourcePath = "Sprites/Live16x24/north-vegetation";
        public const string WorksResourcePath = "Sprites/Live16x24/north-works";
        public const string PlotResourcePath = "Sprites/Live16x24/north-plot";
        public const string KeeperResourcePath = "Sprites/Live16x24/seed-keeper-3";
        public const string PlotProductsResourcePath = "Sprites/Live16x24/plot-products";
        public const string WorksProductsResourcePath = "Sprites/Live16x24/north-works-products";
        public const string LongSwordResourcePath = "Sprites/Live16x24/long-sword";
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
        private readonly Tile[] _vegetationTiles;
        private readonly Sprite[] _vegetationSprites;
        private readonly Tile[] _worksTiles;
        private readonly Sprite[] _worksSprites;
        private readonly Tile[] _plotTiles;
        private readonly Sprite[] _plotSprites;
        private readonly Tile[] _keeperTiles;
        private readonly Sprite[] _keeperSprites;
        private readonly Tile[] _plotProductTiles;
        private readonly Sprite[] _plotProductSprites;
        private readonly Tile[] _worksProductTiles;
        private readonly Sprite[] _worksProductSprites;
        private readonly Tile[] _longSwordTiles;
        private readonly Sprite[] _longSwordSprites;
        private bool _disposed;

        public FlatPixelOpeningArtCatalog() : this(Resources.Load<Texture2D>(ResourcePath),
            Resources.Load<Texture2D>(CastResourcePath), Resources.Load<Texture2D>(AnchorsResourcePath),
            Resources.Load<Texture2D>(StatesResourcePath), Resources.Load<Texture2D>(ItemsResourcePath),
            Resources.Load<Texture2D>(CellarResourcePath), Resources.Load<Texture2D>(VegetationResourcePath),
            Resources.Load<Texture2D>(WorksResourcePath), Resources.Load<Texture2D>(PlotResourcePath),
            Resources.Load<Texture2D>(KeeperResourcePath), Resources.Load<Texture2D>(PlotProductsResourcePath),
            Resources.Load<Texture2D>(WorksProductsResourcePath), Resources.Load<Texture2D>(LongSwordResourcePath)) { }

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
            : this(atlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, null) { }

        /// <summary>Independent borrowed vegetation art adds four static source
        /// silhouettes. Earlier injections never load omitted vegetation resources.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas)
            : this(atlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, null) { }

        /// <summary>The eighth borrowed atlas adds exact works and creature
        /// identities. The healing slot follows only actual carried medicine.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas)
            : this(atlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, null) { }

        /// <summary>Plot frames are independent of the approved keeper sprite.
        /// Earlier injection overloads omit both without loading resources.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas, Texture2D plotAtlas)
            : this(atlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, plotAtlas, null) { }

        /// <summary>All ten groups borrow their textures. Keeper art uses the
        /// entire approved 16x24 cell; crop art follows owned functional state.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas, Texture2D plotAtlas, Texture2D keeperAtlas)
            : this(atlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, plotAtlas, keeperAtlas, null) { }

        /// <summary>The independently optional eleventh atlas represents loose
        /// plot goods, not their plants. It borrows its texture and creates no owners.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas, Texture2D plotAtlas, Texture2D keeperAtlas, Texture2D plotProductsAtlas)
            : this(atlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, plotAtlas, keeperAtlas, plotProductsAtlas, null) { }

        /// <summary>The twelfth optional borrowed group adds exact forage and
        /// works outputs. Existing injections never load omitted resource groups.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas, Texture2D plotAtlas, Texture2D keeperAtlas, Texture2D plotProductsAtlas, Texture2D worksProductsAtlas)
            : this(atlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, plotAtlas, keeperAtlas, plotProductsAtlas, worksProductsAtlas, null) { }

        /// <summary>Exact LongSword art borrows one optional native frame. Earlier
        /// injections omit it without loading resources; custody remains caller-owned.</summary>
        public FlatPixelOpeningArtCatalog(Texture2D atlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas, Texture2D plotAtlas, Texture2D keeperAtlas, Texture2D plotProductsAtlas, Texture2D worksProductsAtlas, Texture2D longSwordAtlas)
        {
            _tiles = BuildFrames(atlas, FrameCount, "base", out _sprites);
            _castTiles = BuildFrames(castAtlas, 4, "cast", out _castSprites);
            _anchorTiles = BuildFrames(anchorsAtlas, 12, "anchors", out _anchorSprites);
            _stateTiles = BuildFrames(statesAtlas, 12, "states", out _stateSprites);
            _itemTiles = BuildFrames(itemsAtlas, 12, "items", out _itemSprites);
            _cellarTiles = BuildFrames(cellarAtlas, 9, "cellar", out _cellarSprites);
            _vegetationTiles = BuildFrames(vegetationAtlas, 4, "vegetation", out _vegetationSprites);
            _worksTiles = BuildFrames(worksAtlas, 9, "works", out _worksSprites);
            _plotTiles = BuildFrames(plotAtlas, 8, "plot", out _plotSprites);
            _keeperTiles = BuildFrames(keeperAtlas, 1, "keeper", out _keeperSprites);
            _plotProductTiles = BuildFrames(plotProductsAtlas, 8, "plot-products", out _plotProductSprites);
            _worksProductTiles = BuildFrames(worksProductsAtlas, 7, "works-products", out _worksProductSprites);
            _longSwordTiles = BuildFrames(longSwordAtlas, 1, "long-sword", out _longSwordSprites);
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
            if (LongSwordOwner(owner, render))
            {
                tile = _longSwordTiles == null ? null : _longSwordTiles[0];
                return tile != null;
            }
            frame = CellarFrameOf(owner, render.VisualID);
            if (frame >= 0)
            {
                tile = _cellarTiles == null ? null : _cellarTiles[frame];
                return tile != null;
            }
            frame = VegetationFrameOf(owner, render.VisualID);
            if (frame >= 0)
            {
                tile = _vegetationTiles == null ? null : _vegetationTiles[frame];
                return tile != null;
            }
            frame = WorksFrameOf(owner, render.VisualID);
            if (frame >= 0)
            {
                tile = _worksTiles == null ? null : _worksTiles[frame];
                return tile != null;
            }
            frame = PlotProductFrameOf(owner, render);
            if (frame >= 0)
            {
                tile = _plotProductTiles == null ? null : _plotProductTiles[frame];
                return tile != null;
            }
            frame = WorksProductFrameOf(owner, render);
            if (frame >= 0)
            {
                tile = _worksProductTiles == null ? null : _worksProductTiles[frame];
                return tile != null;
            }
            return TryResolvePlot(owner, render, out tile);
        }

        // Nonlocal callers admit this exact portable identity without opening
        // the regional terrain, service or product selectors in TryResolve.
        internal bool TryResolvePortableLongSword(Entity owner, out Tile tile)
        {
            tile = null;
            if (_disposed || owner == null) return false;
            var render = Owned<RenderPart>(owner);
            if (render == null || !render.Visible || !LongSwordOwner(owner, render)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint != null)
            {
                // The approved portable image represents one current cell.
                // Carried items legitimately have no spatial registration.
                if (footprint.ParentEntity != owner
                    || owner.SpatialZone != null && !owner.SpatialZone.IsFootprintCurrent(owner)) return false;
                var offsets = footprint.Offsets;
                if (offsets.Length != 1 || offsets[0] != Vector2Int.zero) return false;
            }
            tile = _longSwordTiles == null ? null : _longSwordTiles[0];
            return tile != null;
        }

        private static bool LongSwordOwner(Entity owner, RenderPart render)
        {
            // A real quench changes specs, name and condition, not base geometry.
            // Distinct inherited/assembled blades never alias this exact identity.
            return owner.BlueprintName == "LongSword" && PortableNativeProduct(owner, render)
                && Owned<MeleeWeaponPart>(owner) != null && Owned<EquippablePart>(owner)?.Slot == "Hand";
        }

        private static bool PortableNativeProduct(Entity owner, RenderPart render)
        {
            if (!string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || !owner.HasTag("Item")
                || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Terrain")
                || owner.HasTag("Crop") || owner.HasPart<CropPart>()) return false;
            var physics = Owned<PhysicsPart>(owner);
            if (physics == null || !physics.Takeable || physics.Solid) return false;
            var stack = owner.GetPart<StackerPart>();
            return stack == null || (stack.ParentEntity == owner && stack.StackCount > 0
                && stack.StackCount <= stack.MaxStack);
        }

        private static int PlotProductFrameOf(Entity owner, RenderPart render)
        {
            if (!PortableNativeProduct(owner, render)) return -1;
            switch (owner.BlueprintName)
            {
                case "CandyCarrot": return Owned<FoodPart>(owner) != null ? 0 : -1;
                case "CandyCarrotSeed": return PlotSeed(owner, "CandyCarrotCrop", false) ? 1 : -1;
                case "EmberwheatSeed": return PlotSeed(owner, "EmberwheatCrop", false) ? 2 : -1;
                case "MarlrootClod":
                    var recipe = Owned<BotanicalProcessingPart>(owner);
                    return recipe != null && recipe.OutputBlueprint == "FireClay"
                        && recipe.OutputCount >= 1 && recipe.OutputCount <= 8 ? 3 : -1;
                case "MarlrootSeed": return PlotSeed(owner, "MarlrootCrop", true) ? 4 : -1;
                case "PitchpodResin":
                    var reagent = Owned<ReagentPart>(owner);
                    if (reagent == null) return -1;
                    // Reagent owns the parsed cache. Reading it neither brews nor
                    // allocates per refresh after the raw properties settle.
                    var properties = reagent.GetProperties();
                    bool heat = false, combustible = false;
                    for (int i = 0; i < properties.Count; i++)
                    {
                        var property = properties[i];
                        if (property == null || property.Potency <= 0) continue;
                        if (property.Property == BrewProperties.Heat) heat = true;
                        if (property.Property == BrewProperties.Combustible) combustible = true;
                    }
                    return heat && combustible ? 5 : -1;
                case "PitchpodSeed": return PlotSeed(owner, "PitchpodCrop", true) ? 6 : -1;
                case "InertSludge": return 7;
                default: return -1;
            }
        }

        private static int WorksProductFrameOf(Entity owner, RenderPart render)
        {
            if (!PortableNativeProduct(owner, render)) return -1;
            switch (owner.BlueprintName)
            {
                case "StoneburrSeed": return HasReagentProperty(owner, BrewProperties.Binding) ? 0 : -1;
                case "FrostLichen": return HasReagentProperty(owner, BrewProperties.Cold) ? 1 : -1;
                case "MendleafSprig": return HasReagentProperty(owner, BrewProperties.Vital) ? 2 : -1;
                case "WildBerries": return Owned<FoodPart>(owner) != null ? 3 : -1;
                case "GoldCoin": return Owned<CommercePart>(owner) != null ? 4 : -1;
                case "BrewedTonic":
                    if (Owned<TonicPart>(owner) == null) return -1;
                    var brew = owner.GetPart<BrewItemPart>();
                    // Healing-only brews legitimately have no BrewItem. This
                    // opaque vessel promises neither contents nor safe drinking.
                    // Explicit Food and unknown forms retain their fallback.
                    return brew == null || (brew.ParentEntity == owner
                        && (brew.Form == "Tonic" || brew.Form == "Coating" || brew.Form == "Throwable")) ? 5 : -1;
                case "ConcentratedMendleaf": return HasReagentProperty(owner, BrewProperties.Vital) ? 6 : -1;
                default: return -1;
            }
        }

        private static bool HasReagentProperty(Entity owner, string expected)
        {
            var reagent = Owned<ReagentPart>(owner);
            if (reagent == null) return false;
            var properties = reagent.GetProperties(); // Owner caches parsed entries by raw snapshot.
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                if (property != null && property.Potency > 0 && property.Property == expected) return true;
            }
            return false;
        }

        private static bool PlotSeed(Entity owner, string crop, bool cultivated)
        {
            var seed = Owned<SeedPart>(owner);
            return seed != null && seed.CropBlueprint == crop && seed.RequireCultivatedSoil == cultivated;
        }

        /// <summary>Returns only the cached transparent ground cord. This is
        /// not an ownership or permission check: the world caller must validate
        /// the current bound reserve, soil marker and visibility before drawing.</summary>
        public bool TryGetReserveCordTile(out Tile tile)
        {
            tile = _disposed || _plotTiles == null ? null : _plotTiles[7];
            return tile != null;
        }

        private bool TryResolvePlot(Entity owner, RenderPart render, out Tile tile)
        {
            tile = null;
            if (!string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var physics = Owned<PhysicsPart>(owner);
            if (physics == null || physics.Takeable || physics.InInventory != null || physics.Equipped != null) return false;
            if (owner.BlueprintName == "SpreadSeedKeeper")
            {
                // A claim, mood or stock change does not change Nella's body.
                tile = owner.HasTag("Creature") && _keeperTiles != null ? _keeperTiles[0] : null;
                return tile != null;
            }
            if (owner.HasTag("Creature") || physics.Solid) return false;
            if (owner.BlueprintName == "ConnectedReserveTray")
            {
                // Neutral art deliberately promises neither stock nor access.
                tile = Owned<ContainerPart>(owner) != null && _plotTiles != null ? _plotTiles[6] : null;
                return tile != null;
            }

            string produce, seed;
            int sprout, ripe;
            switch (owner.BlueprintName)
            {
                case "CandyCarrotCrop": produce = "CandyCarrot"; seed = null; sprout = 0; ripe = -1; break;
                case "EmberwheatCrop": produce = "Emberwheat"; seed = null; sprout = 1; ripe = -1; break;
                case "MarlrootCrop": produce = "MarlrootClod"; seed = "MarlrootSeed"; sprout = 2; ripe = 3; break;
                case "PitchpodCrop": produce = "PitchpodResin"; seed = "PitchpodSeed"; sprout = 4; ripe = 5; break;
                default: return false;
            }
            var crop = Owned<CropPart>(owner);
            bool cultivated = seed != null;
            if (!owner.HasTag("Crop") || crop == null || crop.HarvestAtMaturity != cultivated
                || crop.GrowthStage < 0 || crop.GrowthStage > (cultivated ? 2 : 1)
                || crop.YieldBlueprint != produce || crop.YieldCount <= 0
                || crop.YieldCount > CropYieldService.MaximumYieldUnits) return false;
            // Automatic maturity does not use the saved-seed fields. Standing
            // reserve crops must retain their real, bounded produce+seed yield.
            if (cultivated && (crop.SeedYieldBlueprint != seed || crop.SeedYieldCount <= 0
                || crop.SeedYieldCount > CropYieldService.MaximumYieldUnits - crop.YieldCount)) return false;
            tile = crop.GrowthStage == 0 ? (_stateTiles == null ? null : _stateTiles[6])
                : _plotTiles == null ? null : _plotTiles[crop.GrowthStage == 1 ? sprout : ripe];
            return tile != null;
        }

        /// <summary>The same finite healing-slot semantics used by static art.
        /// Stateful callers must establish graph, cell and disclosure authority first.</summary>
        internal static bool TryGetPatchbearerIdleFrame(Entity owner, out int frame)
        {
            frame = -1;
            if (owner?.BlueprintName != "MarlbackPatchbearer" || owner.HasTag("Corpse")) return false;
            var render = Owned<RenderPart>(owner);
            if (render == null || !render.Visible) return false;
            frame = WorksFrameOf(owner, render.VisualID);
            return frame == 7 || frame == 8;
        }

        private static int WorksFrameOf(Entity owner, string visual)
        {
            if (!string.IsNullOrEmpty(visual) || owner.HasTag("Player") || owner.HasTag("Item")) return -1;
            var render = Owned<RenderPart>(owner);
            if (render == null || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)) return -1;
            var physics = Owned<PhysicsPart>(owner);
            if (physics == null || physics.Takeable || physics.InInventory != null || physics.Equipped != null) return -1;
            if (owner.HasTag("Creature"))
            {
                switch (owner.BlueprintName)
                {
                    case "Magpie": return 5;
                    case "Viper": return 6;
                    case "MarlbackPatchbearer":
                        var medicine = Owned<FieldMedicinePart>(owner);
                        var inventory = Owned<InventoryPart>(owner);
                        if (medicine == null || medicine.TonicBlueprint != "HealingTonic"
                            || inventory?.Objects == null || Owned<Body>(owner) == null || Owned<BrainPart>(owner) == null) return -1;
                        return medicine.FindCarriedMedicine()?.BlueprintName == "HealingTonic" ? 7 : 8;
                    default: return -1;
                }
            }
            switch (owner.BlueprintName)
            {
                case "AlchemyStill": return !physics.Solid && Owned<AlchemyStillPart>(owner) != null ? 0 : -1;
                case "AlchemyShelf": return physics.Solid && Owned<ContainerPart>(owner) != null ? 1 : -1;
                case "StoneburrPatch": return ValidWorksHarvest(owner, "StoneburrSeed") ? 2 : -1;
                case "FrostLichenPatch": return ValidWorksHarvest(owner, "FrostLichen") ? 3 : -1;
                case "MendleafPlant": return ValidWorksHarvest(owner, "MendleafSprig") ? 4 : -1;
                default: return -1;
            }
        }

        private static bool ValidWorksHarvest(Entity owner, string yield)
        {
            if (!OrdinaryVegetation(owner, "", false)) return false;
            var harvest = Owned<HarvestablePart>(owner);
            return harvest != null && !harvest.Harvested && harvest.YieldBlueprint == yield
                && harvest.YieldMin > 0 && harvest.YieldMax >= harvest.YieldMin
                && harvest.YieldMax <= HarvestablePart.MaximumYieldCount
                && harvest.YieldChance > 0 && harvest.YieldChance <= 100;
        }

        private static bool OrdinaryVegetation(Entity owner, string visual, bool solid)
        {
            if (!string.IsNullOrEmpty(visual) || !owner.HasTag("Vegetation")
                || owner.HasTag("Creature") || owner.HasTag("Player")) return false;
            var physics = Owned<PhysicsPart>(owner);
            return physics != null && !physics.Takeable && physics.Solid == solid;
        }

        private static int VegetationFrameOf(Entity owner, string visual)
        {
            switch (owner.BlueprintName)
            {
                case "Tree": return OrdinaryVegetation(owner, visual, true) ? 0 : -1;
                case "Hedge": return OrdinaryVegetation(owner, visual, true) ? 1 : -1;
                case "BerryBush":
                    if (!OrdinaryVegetation(owner, visual, false)) return -1;
                    var harvest = Owned<HarvestablePart>(owner);
                    return harvest != null && !harvest.Harvested && harvest.YieldBlueprint == "WildBerries"
                        && harvest.YieldMin > 0 && harvest.YieldMax >= harvest.YieldMin
                        && harvest.YieldMax <= HarvestablePart.MaximumYieldCount
                        && harvest.YieldChance > 0 && harvest.YieldChance <= 100 ? 2 : -1;
                case "DryBrush": return OrdinaryVegetation(owner, visual, false) ? 3 : -1;
                default: return -1;
            }
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
                case "Reeds": return visual == "reference-glade-pale-reeds"
                    || OrdinaryVegetation(owner, visual, false) ? 3 + ((x + y) & 1) : -1;
                case "Bush": return visual == "reference-glade-green-grass"
                    || OrdinaryVegetation(owner, visual, false) ? 5 : -1;
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
            if (_vegetationTiles != null) foreach (var tile in _vegetationTiles) DestroyOwned(tile);
            if (_vegetationSprites != null) foreach (var sprite in _vegetationSprites) DestroyOwned(sprite);
            if (_worksTiles != null) foreach (var tile in _worksTiles) DestroyOwned(tile);
            if (_worksSprites != null) foreach (var sprite in _worksSprites) DestroyOwned(sprite);
            if (_plotTiles != null) foreach (var tile in _plotTiles) DestroyOwned(tile);
            if (_plotSprites != null) foreach (var sprite in _plotSprites) DestroyOwned(sprite);
            if (_keeperTiles != null) foreach (var tile in _keeperTiles) DestroyOwned(tile);
            if (_keeperSprites != null) foreach (var sprite in _keeperSprites) DestroyOwned(sprite);
            if (_plotProductTiles != null) foreach (var tile in _plotProductTiles) DestroyOwned(tile);
            if (_plotProductSprites != null) foreach (var sprite in _plotProductSprites) DestroyOwned(sprite);
            if (_worksProductTiles != null) foreach (var tile in _worksProductTiles) DestroyOwned(tile);
            if (_worksProductSprites != null) foreach (var sprite in _worksProductSprites) DestroyOwned(sprite);
            if (_longSwordTiles != null) foreach (var tile in _longSwordTiles) DestroyOwned(tile);
            if (_longSwordSprites != null) foreach (var sprite in _longSwordSprites) DestroyOwned(sprite);
        }

        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
