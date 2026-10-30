using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    public enum FlatPixelGreyboxKind
    {
        Ground, Road, Wall, Player, Actor, Vegetation, Rubble,
        CropSeed, CropSprout, CropRipe, CropSpent, Prop, DamagedProp,
        LooseItem, WaterSource, EmptyWaterSource, Unknown
    }

    public enum FlatPixelGreyboxGround { Ground, Road, DrySoil, WetSoil, Water, StoneFloor }

    /// <summary>Read-only presentation of one currently observed cell. Shapes are
    /// rough layout placeholders, not finished art or promises of new interactions.</summary>
    public readonly struct FlatPixelGreyboxCell
    {
        public readonly Tile GroundTile, ForegroundTile;
        /// <summary>Observed physical ground detail, independent of the top occupant.</summary>
        public readonly Tile GroundDecorationTile;
        public readonly Color GroundColor, ForegroundColor;
        public readonly Entity Owner;
        public readonly FlatPixelGreyboxKind Kind;
        public readonly FlatPixelGreyboxGround GroundKind;
        public readonly bool Remembered, BlocksMovement;
        /// <summary>Live presentation treatment, independent of regional admission.</summary>
        public readonly bool NativeForeground, LocalGround;
        /// <summary>Owner-free disclosed cell canvas. Affects background tint
        /// only; it is not a native floor, material or represented hazard.</summary>
        public readonly bool AmbientBackdrop;
        /// <summary>Exact native foreground represents the sole live brine layer.</summary>
        public readonly bool RepresentedBrine;
        /// <summary>Submitted native MirePool body represents the sole live bog-mire layer; not brine/water.</summary>
        public readonly bool RepresentedMire;
        /// <summary>Opted-in native actors and storage use the selected owner's existing
        /// Render-event color override; their unchanged authored body stays white.</summary>
        public readonly bool PreserveRenderColorCue;

        internal FlatPixelGreyboxCell(Tile ground, Tile foreground, Color groundColor,
            Color foregroundColor, Entity owner, FlatPixelGreyboxKind kind,
            FlatPixelGreyboxGround groundKind, bool remembered, bool blocksMovement, Tile groundDecoration = null,
            bool nativeForeground = false, bool localGround = false, bool representedBrine = false, bool representedMire = false,
            bool preserveRenderColorCue = false, bool ambientBackdrop = false)
        {
            GroundTile = ground; ForegroundTile = foreground; GroundDecorationTile = groundDecoration;
            GroundColor = groundColor; ForegroundColor = foregroundColor;
            Owner = owner; Kind = kind; GroundKind = groundKind;
            Remembered = remembered; BlocksMovement = blocksMovement;
            NativeForeground = nativeForeground; LocalGround = localGround; RepresentedBrine = representedBrine;
            RepresentedMire = representedMire;
            PreserveRenderColorCue = preserveRenderColorCue;
            AmbientBackdrop = ambientBackdrop;
        }
    }

    /// <summary>
    /// Native opening art with a disposable procedural fallback. Cell owners and
    /// states are resolved afresh; the catalog never changes gameplay or approved
    /// source art. One instance owns its shared atlas, sprites and cached tiles.
    /// Admission does not enable the mode: the caller owns the user's opt-in.
    /// </summary>
    public sealed class FlatPixelGreyboxCatalog : IDisposable
    {
        private const int FrameWidth = 16, FrameHeight = 24, AtlasColumns = 8;
        private readonly Tile[] _tiles;
        private readonly Sprite[] _sprites;
        private readonly FlatPixelOpeningArtCatalog _openingArt;
        private FlatPixelFallbackCatalog _glyphFallback;
        private FlatPixelBoundaryCatalog _boundaryArt;
        private FlatPixelOpeningGroundCatalog _groundArt;
        private FlatPixelEastKitchenCatalog _eastKitchenArt;
        private FlatPixelWorkingCourtCatalog _workingCourtArt;
        private FlatPixelCellarFloorCatalog _cellarFloorArt;
        private FlatPixelIncidentalActorCatalog _incidentalActorArt;
        private FlatPixelStumpFaunaCatalog _stumpFaunaArt;
        private FlatPixelGrovelandsActorsCatalog _grovelandsActorsArt;
        private FlatPixelSharedStorageCatalog _sharedStorageArt;
        private FlatPixelSharedFurnitureCatalog _sharedFurnitureArt;
        private FlatPixelSharedSteamVentCatalog _sharedSteamVentArt;
        private FlatPixelSharedResidentsCatalog _sharedResidentsArt;
        private FlatPixelBeatingActorsCatalog _beatingActorsArt;
        private FlatPixelSoddenFrogsCatalog _soddenFrogsArt;
        private FlatPixelGreatdewCatalog _greatdewArt;
        private FlatPixelExpeditionEssentialsCatalog _expeditionEssentialsArt;
        private FlatPixelSharedVineWallCatalog _sharedVineWallArt;
        private FlatPixelSharedCampPropsCatalog _sharedCampPropsArt;
        private FlatPixelSharedCampClothCatalog _sharedCampClothArt;
        private readonly Texture2D _residentsCastAtlas;
        private FlatPixelSharedStonePropsCatalog _sharedStonePropsArt;
        private FlatPixelIncidentalEquipmentCatalog _incidentalEquipmentArt;
        private FlatPixelHarvestProductsCatalog _harvestProductsArt;
        private readonly Texture2D _harvestItemsAtlas, _harvestWorksAtlas, _expeditionPlotProductsAtlas;
        private readonly Texture2D _storageAnchorsAtlas, _storageCellarAtlas;
        private FlatPixelAftermathCatalog _aftermathArt;
        private FlatPixelOrchardCatalog _orchardArt;
        private FlatPixelRiverShrineCatalog _riverShrineArt;
        private FlatPixelNellaCornerCatalog _nellaCornerArt;
        private FlatPixelEastFixtureCatalog _eastFixtureArt;
        private FlatPixelKitchenBrineCatalog _kitchenBrineArt;
        private FlatPixelHedgeCatalog _hedgeArt;
        private FlatPixelSillEnvelopeCatalog _sillEnvelopeArt;
        private FlatPixelSpreadEnvironmentCatalog _spreadEnvironmentArt;
        private FlatPixelSillRiverCatalog _sillRiverArt;
        private FlatPixelSoddenTravelCatalog _soddenTravelArt;
        private FlatPixelBeatingMaterialsCatalog _beatingMaterialsArt;
        private FlatPixelSettlementGroundCatalog _settlementGroundArt;
        private FlatPixelSinkholeLipCatalog _sinkholeLipArt;
        private FlatPixelSpreadEnvironmentCatalog _spreadSiteArt;
        private FlatPixelSoddenTravelCatalog _soddenSiteArt;
        private FlatPixelBeatingMaterialsCatalog _beatingSiteArt;
        private FlatPixelGrovelandsMaterialsCatalog _grovelandsSiteArt;
        private FlatPixelStumpMaterialsCatalog _stumpSiteArt;
        private FlatPixelGrovelandsMaterialsCatalog _grovelandsMaterialsArt;
        private FlatPixelOverwritMaterialsCatalog _overwritMaterialsArt;
        private FlatPixelStumpMaterialsCatalog _stumpMaterialsArt;
        private FlatPixelShallowCavesCatalog _shallowCavesArt;
        private FlatPixelLimestoneCavesCatalog _limestoneCavesArt;
        private FlatPixelShaleCavesCatalog _shaleCavesArt;
        private FlatPixelSlateCavesCatalog _slateCavesArt;
        private FlatPixelQuartziteCavesCatalog _quartziteCavesArt;
        private FlatPixelObsidianCavesCatalog _obsidianCavesArt;
        private readonly bool _loadOptionalResources;
        private Texture2D _atlas;
        private bool _disposed;
        private sealed class SupplyObservation
        {
            internal Cell Cell;
            internal Tile Tile;
            internal bool Covered;
            internal bool Presentation;
        }
        private Zone _observedZone;
        private Zone _reserveZone;
        private IReadOnlyList<Entity> _reserveOwners;
        private int _reserveMembership = -1;
        private Dictionary<int, Entity> _reserveBeds = new Dictionary<int, Entity>();
        private Dictionary<int, Entity> _nextReserveBeds = new Dictionary<int, Entity>();
        private readonly Dictionary<Entity, SupplyObservation> _supplyOwners = new Dictionary<Entity, SupplyObservation>();
        private readonly List<Entity> _releaseSupplyOwners = new List<Entity>();
        private Zone _serviceZone;
        private readonly Dictionary<Entity, SupplyObservation> _serviceOwners = new Dictionary<Entity, SupplyObservation>();
        private readonly List<Entity> _releaseServiceOwners = new List<Entity>();

        public FlatPixelGreyboxCatalog() : this(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CastResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.StatesResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ItemsResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CellarResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.VegetationResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.WorksResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.PlotResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.KeeperResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.PlotProductsResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.WorksProductsResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.LongSwordResourcePath))
        { _loadOptionalResources = true; }

        /// <summary>Injects a borrowed opening atlas. Null deliberately exercises
        /// the procedural fallback without consulting Resources.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas) : this(openingAtlas, null) { }

        /// <summary>Injects independent borrowed base/cast atlases. Neither
        /// missing group causes an implicit resource load or disables the other.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas) : this(openingAtlas, castAtlas, null) { }

        /// <summary>All three native groups are borrowed, optional and independent.
        /// Null injection never consults Resources.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas)
            : this(openingAtlas, castAtlas, anchorsAtlas, null) { }

        /// <summary>Borrowed base/cast/anchor/state atlases remain independent;
        /// omitted injected groups never load their resource implicitly.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas)
            : this(openingAtlas, castAtlas, anchorsAtlas, statesAtlas, null) { }

        /// <summary>Five optional borrowed groups; earlier overloads never load
        /// omitted art. This catalog owns generated frames, not their textures.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas)
            : this(openingAtlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, null) { }

        /// <summary>The sixth optional borrowed group adds current cellar owners.
        /// Earlier overloads retain deterministic null-cellar injection.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas)
            : this(openingAtlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, null) { }

        /// <summary>The seventh optional borrowed atlas adds native vegetation;
        /// earlier injections preserve explicit fallback for omitted groups.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas)
            : this(openingAtlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, null) { }

        /// <summary>The eighth optional borrowed atlas adds works and creatures;
        /// earlier injection overloads never implicitly load it.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas)
            : this(openingAtlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, null) { }

        /// <summary>Independent borrowed crop/ground atlas; keeper remains omitted.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas, Texture2D plotAtlas)
            : this(openingAtlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, plotAtlas, null) { }

        /// <summary>Ten optional borrowed groups. The approved keeper's full cell
        /// is independent of plot art; omitted injections never load Resources.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas, Texture2D plotAtlas, Texture2D keeperAtlas)
            : this(openingAtlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, plotAtlas, keeperAtlas, null) { }

        /// <summary>Eleven optional borrowed groups. Loose plot products retain
        /// the same world/custody authorization as every other native subject.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas, Texture2D plotAtlas, Texture2D keeperAtlas, Texture2D plotProductsAtlas)
            : this(openingAtlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, plotAtlas, keeperAtlas, plotProductsAtlas, null) { }

        /// <summary>Twelve independent borrowed groups. Works goods share the
        /// existing world/custody authorization and exact-subject portrait path.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas, Texture2D plotAtlas, Texture2D keeperAtlas, Texture2D plotProductsAtlas, Texture2D worksProductsAtlas)
            : this(openingAtlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, plotAtlas, keeperAtlas, plotProductsAtlas, worksProductsAtlas, null) { }

        /// <summary>Thirteen independently optional borrowed groups. The exact
        /// blade shares existing regional, world and reader authorization.</summary>
        public FlatPixelGreyboxCatalog(Texture2D openingAtlas, Texture2D castAtlas, Texture2D anchorsAtlas, Texture2D statesAtlas, Texture2D itemsAtlas, Texture2D cellarAtlas, Texture2D vegetationAtlas, Texture2D worksAtlas, Texture2D plotAtlas, Texture2D keeperAtlas, Texture2D plotProductsAtlas, Texture2D worksProductsAtlas, Texture2D longSwordAtlas)
        {
            _openingArt = new FlatPixelOpeningArtCatalog(openingAtlas, castAtlas, anchorsAtlas, statesAtlas, itemsAtlas, cellarAtlas, vegetationAtlas, worksAtlas, plotAtlas, keeperAtlas, plotProductsAtlas, worksProductsAtlas, longSwordAtlas);
            _harvestItemsAtlas = itemsAtlas; _harvestWorksAtlas = worksProductsAtlas; _expeditionPlotProductsAtlas = plotProductsAtlas;
            _storageAnchorsAtlas = anchorsAtlas; _storageCellarAtlas = cellarAtlas;
            _residentsCastAtlas = castAtlas;
            int count = Enum.GetValues(typeof(FlatPixelGreyboxKind)).Length;
            _tiles = new Tile[count];
            _sprites = new Sprite[count];
            BuildAtlas();
        }

        /// <summary>The glade, cellar and four exact connected surface graphs
        /// are admitted. Other regions retain their identity-preserving fallback.
        /// Eligibility reads the current graph; it never generates or repairs it.</summary>
        public static bool IsAdmitted(Zone zone)
        {
            if (ReferenceGladePlan.IsActive(zone)) return true;
            int surfaceX = 11, surfaceY;
            switch (zone?.ZoneID)
            {
                case GleanersCellarBuilder.ZoneID: surfaceY = 10; break;
                case "Overworld.11.9.0": surfaceY = 9; break;
                case "Overworld.11.8.0": surfaceY = 8; break;
                case "Overworld.12.10.0": surfaceX = 12; surfaceY = 10; break;
                case "Overworld.12.11.0": surfaceX = 12; surfaceY = 11; break;
                default: return false;
            }
            if (!SpreadPresentationScope.IsActive(zone)) return false;
            // Broad Spread admission also permits POIs and committed lair floors.
            // Each opening graph retains authority over its own unclaimed address.
            return WorldLocationContext.For(zone).WorldMap.GetPOI(surfaceX, surfaceY) == null;
        }

        /// <summary>Rejects unexplored, foreign and disposed contexts. Remembered
        /// cells expose only low-layer terrain, never current occupants, crop state,
        /// finite supplies, coatings or collision. Visible state is not cached.</summary>
        public bool TryResolve(Zone zone, int x, int y, out FlatPixelGreyboxCell result)
        {
            result = default;
            if (_disposed || !IsAdmitted(zone)) return false;
            Cell cell = zone.GetCell(x, y);
            if (cell == null || cell.ParentZone != zone || !cell.Explored) return false;
            bool remembered = !cell.IsVisible;
            Entity owner = remembered ? RememberedTerrain(zone, cell) : VisibleOwner(zone, cell);
            FlatPixelGreyboxKind kind = Classify(owner);
            FlatPixelGreyboxGround ground = GroundOf(zone, cell, owner, remembered);
            Color groundColor = GroundColor(ground);
            Color foregroundColor = ForegroundColor(kind);
            Tile foreground = kind == FlatPixelGreyboxKind.Ground || kind == FlatPixelGreyboxKind.Road
                ? null : _tiles[(int)kind];
            bool greatdewNative = false;
            bool essentialsNative = false;
            bool sharedVineNative = false;
            bool bucklerNative = false;
            bool portableCropNative = false;
            if (!remembered && SharedVineRole(zone, owner))
            {
                var vines = SharedVineWallArtFor(zone, owner);
                if (vines != null && vines.TryResolve(zone, x, y, out var vineTile))
                { foreground = vineTile; foregroundColor = Color.white; sharedVineNative = true; }
                else if (TryResolveFallback(zone, x, y, out var vineFallback))
                { foreground = vineFallback.ForegroundTile; foregroundColor = vineFallback.ForegroundColor; }
            }
            else if (FlatPixelOpeningArtCatalog.HasPortableCropRole(owner))
            {
                if (_openingArt.TryResolvePortableCrop(owner, out var cropTile))
                { foreground = cropTile; foregroundColor = Color.white; portableCropNative = true; }
                else if (TryResolveFallback(zone, x, y, out var cropFallback))
                { foreground = cropFallback.ForegroundTile; foregroundColor = cropFallback.ForegroundColor; }
            }
            else if (owner?.BlueprintName == "Buckler")
            {
                var equipment = IncidentalEquipmentArtFor(owner);
                if (equipment != null && equipment.TryResolve(owner, out var equipmentTile))
                { foreground = equipmentTile; foregroundColor = Color.white; bucklerNative = true; }
                else if (TryResolveFallback(zone, x, y, out var equipmentFallback))
                { foreground = equipmentFallback.ForegroundTile; foregroundColor = equipmentFallback.ForegroundColor; }
            }
            else if (FlatPixelSharedCampClothCatalog.HasKnownRole(owner))
            {
                var cloth = SharedCampClothArtFor(owner);
                if (cloth != null && cloth.TryResolve(owner, x, y, out var clothTile))
                { foreground = clothTile; foregroundColor = Color.white; }
                else if (TryResolveFallback(zone, x, y, out var clothFallback))
                { foreground = clothFallback.ForegroundTile; foregroundColor = clothFallback.ForegroundColor; }
            }
            else if (FlatPixelSharedCampPropsCatalog.HasKnownRole(owner))
            {
                var camp = SharedCampPropsArtFor(owner);
                if (camp != null && camp.TryResolve(owner, out var campTile))
                { foreground = campTile; foregroundColor = Color.white; }
                else if (TryResolveFallback(zone, x, y, out var campFallback))
                { foreground = campFallback.ForegroundTile; foregroundColor = campFallback.ForegroundColor; }
            }
            else if (FlatPixelExpeditionEssentialsCatalog.HasKnownRole(owner))
            {
                var supplies = ExpeditionEssentialsArtFor(owner);
                if (supplies != null && supplies.TryResolve(owner, out var supplyTile))
                { foreground = supplyTile; foregroundColor = Color.white; essentialsNative = true; }
                else if (TryResolveFallback(zone, x, y, out var supplyFallback))
                { foreground = supplyFallback.ForegroundTile; foregroundColor = supplyFallback.ForegroundColor; }
            }
            else if (FlatPixelGreatdewCatalog.HasKnownRole(owner))
            {
                var plant = GreatdewArtFor(owner);
                if (plant != null && plant.TryResolve(owner, out var plantTile))
                { foreground = plantTile; foregroundColor = Color.white; greatdewNative = true; }
                else if (TryResolveFallback(zone, x, y, out var plantFallback))
                { foreground = plantFallback.ForegroundTile; foregroundColor = plantFallback.ForegroundColor; }
            }
            else if (FlatPixelSoddenFrogsCatalog.HasKnownRole(owner))
            {
                var actors = SoddenFrogsArtFor(owner);
                if (actors != null && actors.TryResolve(owner, out var actorTile))
                { foreground = actorTile; foregroundColor = Color.white; }
                else if (TryResolveFallback(zone, x, y, out var actorFallback))
                { foreground = actorFallback.ForegroundTile; foregroundColor = actorFallback.ForegroundColor; }
            }
            else if (FlatPixelBeatingActorsCatalog.HasKnownRole(owner))
            {
                var actors = BeatingActorsArtFor(owner);
                if (actors != null && actors.TryResolve(owner, out var actorTile))
                { foreground = actorTile; foregroundColor = Color.white; }
                else if (TryResolveFallback(zone, x, y, out var actorFallback))
                { foreground = actorFallback.ForegroundTile; foregroundColor = actorFallback.ForegroundColor; }
            }
            else if (FlatPixelSharedResidentsCatalog.HasKnownRole(owner))
            {
                var residents = SharedResidentsArtFor(owner);
                if (residents.TryResolve(owner, out var residentTile))
                { foreground = residentTile; foregroundColor = Color.white; }
                else if (TryResolveFallback(zone, x, y, out var residentFallback))
                { foreground = residentFallback.ForegroundTile; foregroundColor = residentFallback.ForegroundColor; }
            }
            else if (SharedSteamRole(zone, owner))
            {
                var steam = SharedSteamVentArtFor(zone, owner);
                if (steam != null && steam.TryResolve(owner, out var steamTile))
                { foreground = steamTile; foregroundColor = Color.white; }
                else if (TryResolveFallback(zone, x, y, out var steamFallback))
                { foreground = steamFallback.ForegroundTile; foregroundColor = steamFallback.ForegroundColor; }
            }
            else if (SharedStoneRole(owner))
            {
                var stone = SharedStonePropsArtFor(owner);
                if (stone != null && stone.TryResolve(owner, out var stoneTile))
                { foreground = stoneTile; foregroundColor = Color.white; }
                else if (TryResolveFallback(zone, x, y, out var stoneFallback))
                { foreground = stoneFallback.ForegroundTile; foregroundColor = stoneFallback.ForegroundColor; }
            }
            else if (FlatPixelSharedFurnitureCatalog.HasKnownRole(owner))
            {
                var furniture = SharedFurnitureArtFor(owner);
                if (furniture != null && furniture.TryResolve(owner, out var furnitureTile))
                { foreground = furnitureTile; foregroundColor = Color.white; }
                else if (TryResolveFallback(zone, x, y, out var furnitureFallback))
                { foreground = furnitureFallback.ForegroundTile; foregroundColor = furnitureFallback.ForegroundColor; }
            }
            else if (FlatPixelSharedStorageCatalog.HasKnownRole(owner))
            {
                var storage = SharedStorageArtFor(owner);
                if (storage != null && storage.TryResolve(owner, out var storageTile))
                { foreground = storageTile; foregroundColor = Color.white; }
                else if (TryResolveFallback(zone, x, y, out var storageFallback))
                { foreground = storageFallback.ForegroundTile; foregroundColor = storageFallback.ForegroundColor; }
            }
            else if (FlatPixelHarvestProductsCatalog.HasKnownRole(owner))
            {
                var products = HarvestProductsArtFor(owner);
                if (products != null && products.TryResolve(owner, out var productTile))
                { foreground = productTile; foregroundColor = Color.white; }
                else if (TryResolveFallback(zone, x, y, out var fallback))
                { foreground = fallback.ForegroundTile; foregroundColor = fallback.ForegroundColor; }
            }
            else if (_openingArt.TryResolve(owner, x, y, out var openingTile))
            {
                foreground = openingTile;
                foregroundColor = Color.white;
            }
            if (!remembered && owner?.BlueprintName == "MarlbackPatchbearer" && CurrentOwnerCell(zone, cell, owner))
                ObserveSupply(zone, cell, owner, foreground);
            if (remembered)
            {
                // No independent light source or live-state read through memory.
                groundColor *= 0.35f;
                foregroundColor *= 0.35f;
                groundColor.a = foregroundColor.a = 1f;
            }
            result = new FlatPixelGreyboxCell(_tiles[(int)FlatPixelGreyboxKind.Ground], foreground,
                groundColor, foregroundColor, owner, kind, ground, remembered,
                !remembered && cell.BlocksMovement(), remembered ? null : ReserveCord(zone, cell, out _),
                nativeForeground: sharedVineNative || greatdewNative || essentialsNative || bucklerNative || portableCropNative,
                preserveRenderColorCue: sharedVineNative || greatdewNative || essentialsNative || bucklerNative);
            return true;
        }

        private void ObserveSupply(Zone zone, Cell cell, Entity owner, Tile tile, bool presentation = false)
        {
            if (_observedZone != zone) { ClearSupplyOwners(); _observedZone = zone; }
            if (!_supplyOwners.TryGetValue(owner, out var observation))
            {
                observation = new SupplyObservation();
                _supplyOwners.Add(owner, observation);
            }
            observation.Cell = cell;
            observation.Tile = tile;
            observation.Covered = false;
            observation.Presentation = presentation;
        }

        /// <summary>Only previously painted exact owners are revisited. World
        /// membership and visibility precede the local medicine query. Invalid
        /// semantic parts remain observed so repairing ownership can recover art.
        /// Covered owners retain no supply signature and do not query inventory.</summary>
        internal void ReconcileObservedOwners(Zone zone, Action<int, int, string> dirty)
        {
            if (_disposed || !IsAdmitted(zone) || !FlatPixelFallbackCatalog.IsCurrentGraph(zone)) { ClearObservedOwners(); return; }
            ReconcileReserveBeds(zone, dirty);
            ReconcileServiceOwners(zone, dirty);
            if (zone != _observedZone) { ClearSupplyOwners(); return; }
            _releaseSupplyOwners.Clear();
            foreach (var pair in _supplyOwners)
            {
                Entity owner = pair.Key;
                SupplyObservation previous = pair.Value;
                Cell cell = zone.GetEntityCell(owner);
                if (owner.BlueprintName != "MarlbackPatchbearer" || !CurrentOwnerCell(zone, cell, owner)
                    || !cell.Explored || !cell.IsVisible)
                {
                    dirty(previous.Cell.X, previous.Cell.Y, "flat medicine ownership");
                    _releaseSupplyOwners.Add(owner);
                    continue;
                }
                bool covered = previous.Presentation
                    ? !TryResolveFallback(zone, cell.X, cell.Y, out var selected) || !ReferenceEquals(selected.Owner, owner)
                    : !ReferenceEquals(VisibleOwner(zone, cell), owner);
                Tile tile = null;
                if (!covered && !_openingArt.TryResolve(owner, cell.X, cell.Y, out tile))
                {
                    if (previous.Presentation && TryResolveFallback(zone, cell.X, cell.Y, out var fallback)) tile = fallback.ForegroundTile;
                    else tile = _tiles[(int)Classify(owner)];
                }
                if (previous.Cell != cell || previous.Covered != covered || previous.Tile != tile)
                {
                    dirty(previous.Cell.X, previous.Cell.Y, "flat medicine state");
                    if (previous.Cell != cell) dirty(cell.X, cell.Y, "flat medicine state");
                    previous.Cell = cell;
                    previous.Covered = covered;
                    previous.Tile = tile;
                }
            }
            for (int i = 0; i < _releaseSupplyOwners.Count; i++) _supplyOwners.Remove(_releaseSupplyOwners[i]);
            _releaseSupplyOwners.Clear();
        }

        internal void ClearObservedOwners()
        {
            ClearSupplyOwners();
            ClearServiceOwners();
            _reserveZone = null; _reserveOwners = null; _reserveMembership = -1;
            _reserveBeds.Clear(); _nextReserveBeds.Clear();
        }

        private void ClearSupplyOwners()
        {
            _supplyOwners.Clear();
            _releaseSupplyOwners.Clear();
            _observedZone = null;
        }

        private static bool ServiceSubject(Entity owner) => owner?.BlueprintName == "ConnectedBatchPan"
            || owner?.BlueprintName == "ConnectedKitchenPickup";

        private void ObserveService(Zone zone, Cell cell, Entity owner, Tile tile)
        {
            if (!ServiceSubject(owner) || !CurrentOwnerCell(zone, cell, owner)) return;
            if (_serviceZone != zone) { ClearServiceOwners(); _serviceZone = zone; }
            if (!_serviceOwners.TryGetValue(owner, out var observation))
            { observation = new SupplyObservation(); _serviceOwners.Add(owner, observation); }
            observation.Cell = cell; observation.Tile = tile; observation.Covered = false;
        }

        // Only previously painted exact service owners are observed. Disclosure
        // and top-owner selection precede any job/contents query. Covered records
        // remain suppressed so a quiet uncover can restore the current frame.
        private void ReconcileServiceOwners(Zone zone, Action<int, int, string> dirty)
        {
            if (_serviceZone != zone) { ClearServiceOwners(); return; }
            _releaseServiceOwners.Clear();
            foreach (var pair in _serviceOwners)
            {
                var owner = pair.Key; var previous = pair.Value; var cell = zone.GetEntityCell(owner);
                if (!ServiceSubject(owner) || !CurrentOwnerCell(zone, cell, owner) || !cell.Explored || !cell.IsVisible)
                {
                    dirty(previous.Cell.X, previous.Cell.Y, "flat kitchen ownership");
                    _releaseServiceOwners.Add(owner); continue;
                }
                bool covered = !TryResolveFallback(zone, cell.X, cell.Y, out var selected) || selected.Owner != owner;
                Tile tile = null;
                if (!covered && (_eastKitchenArt == null || !_eastKitchenArt.TryResolvePresentation(zone, owner, out tile)))
                    tile = selected.ForegroundTile;
                if (previous.Cell != cell || previous.Covered != covered || previous.Tile != tile)
                {
                    dirty(previous.Cell.X, previous.Cell.Y, "flat kitchen state");
                    if (previous.Cell != cell) dirty(cell.X, cell.Y, "flat kitchen state");
                    previous.Cell = cell; previous.Tile = tile; previous.Covered = covered;
                }
            }
            for (int i = 0; i < _releaseServiceOwners.Count; i++) _serviceOwners.Remove(_releaseServiceOwners[i]);
            _releaseServiceOwners.Clear();
        }

        private void ClearServiceOwners()
        { _serviceZone = null; _serviceOwners.Clear(); _releaseServiceOwners.Clear(); }

        private IReadOnlyList<Entity> ReserveOwners(Zone zone)
        {
            int version = zone.GetPartMembershipVersion<LocalGatheringClaimPart>();
            if (_reserveZone != zone || version != _reserveMembership)
            {
                if (_reserveZone != zone) { _reserveBeds.Clear(); _nextReserveBeds.Clear(); }
                _reserveZone = zone; _reserveMembership = version;
                _reserveOwners = zone.GetReadOnlyEntitiesWithPart<LocalGatheringClaimPart>();
            }
            return _reserveOwners;
        }

        private static bool CurrentClaim(Zone zone, Entity owner, LocalGatheringClaimPart claim)
            => claim != null && claim.ParentEntity == owner && claim.Bound(zone)
                && CurrentOwnerCell(zone, zone.GetEntityCell(owner), owner)
                && CurrentOwnerCell(zone, zone.GetEntityCell(claim.Tray), claim.Tray)
                && CurrentOwnerCell(zone, zone.GetEntityCell(claim.FirstSoil), claim.FirstSoil)
                && CurrentOwnerCell(zone, zone.GetEntityCell(claim.SecondSoil), claim.SecondSoil);

        private static bool VisibleReserveSoil(Zone zone, Entity soil, Entity keeper, out Cell cell)
        {
            cell = zone.GetEntityCell(soil);
            return CurrentOwnerCell(zone, cell, soil) && cell.Explored && cell.IsVisible
                && soil.GetProperty(SpreadExplorationResidents.ReserveBedKey) == keeper.ID;
        }

        // Direct callers receive fresh authority; a previously painted cord is
        // never evidence that a replacement soil or copied marker owns a bed.
        private Tile ReserveCord(Zone zone, Cell cell, out Entity matched)
        {
            matched = null;
            if (!_openingArt.TryGetReserveCordTile(out var tile)) return null;
            bool marked = false;
            for (int i = 0; i < cell.Objects.Count; i++)
                if (!string.IsNullOrEmpty(cell.Objects[i].GetProperty(SpreadExplorationResidents.ReserveBedKey))) { marked = true; break; }
            if (!marked) return null;
            var owners = ReserveOwners(zone);
            for (int i = 0; i < owners.Count; i++)
            {
                Entity keeper = owners[i]; var claim = keeper.GetPart<LocalGatheringClaimPart>();
                if (!CurrentClaim(zone, keeper, claim)) continue;
                Entity soil = zone.GetEntityCell(claim.FirstSoil) == cell ? claim.FirstSoil
                    : zone.GetEntityCell(claim.SecondSoil) == cell ? claim.SecondSoil : null;
                if (soil == null || !VisibleReserveSoil(zone, soil, keeper, out _)) continue;
                if (matched != null) return null; // Ambiguous graphs do not assert ownership.
                matched = soil;
            }
            return matched == null ? null : tile;
        }

        // Full repaints clear the old observation before submitting tiles. Seed
        // only actually painted cords, so a remote invalidation on the very next
        // quiet frame can retire them. Direct read-only queries never seed this.
        internal void ObservePaintedReserveBed(Zone zone, int x, int y)
        {
            Cell cell = zone?.GetCell(x, y);
            if (cell == null || ReserveCord(zone, cell, out var soil) == null) return;
            _reserveBeds[y * Zone.Width + x] = soil;
        }

        private void ReconcileReserveBeds(Zone zone, Action<int, int, string> dirty)
        {
            if (!_openingArt.TryGetReserveCordTile(out _)) return;
            var owners = ReserveOwners(zone);
            _nextReserveBeds.Clear();
            for (int i = 0; i < owners.Count; i++)
            {
                Entity keeper = owners[i]; var claim = keeper.GetPart<LocalGatheringClaimPart>();
                if (!CurrentClaim(zone, keeper, claim)) continue;
                CollectReserveBed(zone, claim.FirstSoil, keeper);
                CollectReserveBed(zone, claim.SecondSoil, keeper);
            }
            foreach (var pair in _reserveBeds)
                if (!_nextReserveBeds.TryGetValue(pair.Key, out var current) || current != pair.Value)
                    dirty(pair.Key % Zone.Width, pair.Key / Zone.Width, "flat reserve ownership");
            foreach (var pair in _nextReserveBeds)
                if (!_reserveBeds.TryGetValue(pair.Key, out var prior) || prior != pair.Value)
                    dirty(pair.Key % Zone.Width, pair.Key / Zone.Width, "flat reserve ownership");
            var swap = _reserveBeds; _reserveBeds = _nextReserveBeds; _nextReserveBeds = swap;
        }

        private void CollectReserveBed(Zone zone, Entity soil, Entity keeper)
        {
            if (!VisibleReserveSoil(zone, soil, keeper, out var cell)) return;
            int key = cell.Y * Zone.Width + cell.X;
            // A null sentinel keeps ambiguous claims suppressed for this pass.
            if (_nextReserveBeds.ContainsKey(key)) _nextReserveBeds[key] = null;
            else _nextReserveBeds.Add(key, soil);
        }

        private static bool CurrentOwnerCell(Zone zone, Cell cell, Entity owner)
            => cell != null && cell.ParentZone == zone && zone.GetCell(cell.X, cell.Y) == cell
                && owner.SpatialZone == zone && zone.GetEntityCell(owner) == cell && cell.Objects.Contains(owner);

        /// <summary>Borrow the exact subject's current sprite without selecting a
        /// cell occupant. This is semantic artwork only; the caller must authorize
        /// its world/inventory document and presentation context first.</summary>
        public bool TryGetSubjectSprite(Entity owner, int x, int y, out Sprite sprite, out Color tint)
        {
            sprite = null;
            tint = Color.white;
            if (_disposed || owner == null) return false;
            var render = owner.GetPart<RenderPart>();
            if (render == null || render.ParentEntity != owner || !render.Visible) return false;
            if (FlatPixelOpeningArtCatalog.HasPortableCropRole(owner))
            {
                if (_openingArt.TryResolvePortableCrop(owner, out var cropTile))
                { sprite = cropTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (owner.BlueprintName == "Buckler")
            {
                var equipment = IncidentalEquipmentArtFor(owner);
                if (equipment != null && equipment.TryResolve(owner, out var equipmentTile))
                { sprite = equipmentTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (SharedVineRole(owner.SpatialZone, owner))
            {
                var vines = SharedVineWallArtFor(owner.SpatialZone, owner);
                if (vines != null && vines.TryGetSubjectSprite(owner.SpatialZone, owner, x, y, out sprite)) return true;
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (FlatPixelSharedCampClothCatalog.HasKnownRole(owner))
            {
                var cloth = SharedCampClothArtFor(owner);
                if (cloth != null && cloth.TryResolve(owner, x, y, out var clothTile))
                { sprite = clothTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (FlatPixelSharedCampPropsCatalog.HasKnownRole(owner))
            {
                var camp = SharedCampPropsArtFor(owner);
                if (camp != null && camp.TryResolve(owner, out var campTile))
                { sprite = campTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (FlatPixelExpeditionEssentialsCatalog.HasKnownRole(owner))
            {
                var supplies = ExpeditionEssentialsArtFor(owner);
                if (supplies != null && supplies.TryResolve(owner, out var supplyTile))
                { sprite = supplyTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (FlatPixelGreatdewCatalog.HasKnownRole(owner))
            {
                var plant = GreatdewArtFor(owner);
                if (plant != null && plant.TryResolve(owner, out var plantTile))
                { sprite = plantTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (FlatPixelSoddenFrogsCatalog.HasKnownRole(owner))
            {
                var actors = SoddenFrogsArtFor(owner);
                if (actors != null && actors.TryResolve(owner, out var actorTile))
                { sprite = actorTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (FlatPixelBeatingActorsCatalog.HasKnownRole(owner))
            {
                var actors = BeatingActorsArtFor(owner);
                if (actors != null && actors.TryResolve(owner, out var actorTile))
                { sprite = actorTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (FlatPixelSharedResidentsCatalog.HasKnownRole(owner))
            {
                var residents = SharedResidentsArtFor(owner);
                if (residents.TryResolve(owner, out var residentTile))
                { sprite = residentTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (SharedSteamRole(owner.SpatialZone, owner))
            {
                var steam = SharedSteamVentArtFor(owner.SpatialZone, owner);
                if (steam != null && steam.TryResolve(owner, out var steamTile))
                { sprite = steamTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (SharedStoneRole(owner))
            {
                var stone = SharedStonePropsArtFor(owner);
                if (stone != null && stone.TryResolve(owner, out var stoneTile))
                { sprite = stoneTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (FlatPixelSharedFurnitureCatalog.HasKnownRole(owner))
            {
                var furniture = SharedFurnitureArtFor(owner);
                if (furniture != null && furniture.TryResolve(owner, out var furnitureTile))
                { sprite = furnitureTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (FlatPixelSharedStorageCatalog.HasKnownRole(owner))
            {
                var storage = SharedStorageArtFor(owner);
                if (storage != null && storage.TryResolve(owner, out var storageTile))
                { sprite = storageTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            if (FlatPixelHarvestProductsCatalog.HasKnownRole(owner))
            {
                var products = HarvestProductsArtFor(owner);
                if (products != null && products.TryResolve(owner, out var productTile))
                { sprite = productTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(owner, x, y, out sprite, out tint);
            }
            var kind = Classify(owner);
            Tile tile;
            if (_openingArt.TryResolve(owner, x, y, out tile))
            {
                sprite = tile.sprite;
                return sprite != null;
            }
            if (kind == FlatPixelGreyboxKind.Ground || kind == FlatPixelGreyboxKind.Road)
            {
                tile = _tiles[(int)FlatPixelGreyboxKind.Ground];
                tint = GroundColor(kind == FlatPixelGreyboxKind.Road
                    ? FlatPixelGreyboxGround.Road : IsPlainFloor(owner)
                        ? FlatPixelGreyboxGround.StoneFloor : FlatPixelGreyboxGround.Ground);
            }
            else
            {
                tile = _tiles[(int)kind];
                tint = ForegroundColor(kind);
            }
            sprite = tile.sprite;
            return sprite != null;
        }

        private static Entity VisibleOwner(Zone zone, Cell cell)
        {
            Entity result = null;
            int layer = int.MinValue;
            foreach (var owner in cell.Occupants)
            {
                // An old cell list can retain a reference after its indexed owner
                // moved or the cell was replaced. Never repaint that stale body.
                if (owner.BlueprintName == "MarlbackPatchbearer" && !CurrentOwnerCell(zone, cell, owner)) continue;
                var render = owner.GetPart<RenderPart>();
                if (render == null || !render.Visible || render.RenderLayer < layer) continue;
                var physics = owner.GetPart<PhysicsPart>();
                // Preserve existing non-item placeholder semantics. Portable
                // owners must not leak a generic glyph after leaving the ground.
                if (!owner.HasTag("Creature") && !owner.HasTag("Player")
                    && (owner.HasTag("Item") || owner.HasTag("Corpse") || physics?.Takeable == true))
                {
                    var anchor = zone.GetEntityCell(owner);
                    var stack = owner.GetPart<StackerPart>();
                    if (render.ParentEntity != owner || !WorldResourceActions.Ground(owner, zone)
                        || anchor == null || zone.GetCell(anchor.X, anchor.Y) != anchor
                        || SpatialQuery.DistanceToCell(zone, owner, cell.X, cell.Y) != 0
                        || stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0)) continue;
                }
                result = owner;
                layer = render.RenderLayer;
            }
            return result;
        }

        private static Entity RememberedTerrain(Zone zone, Cell cell)
        {
            Entity result = null;
            int layer = int.MinValue;
            foreach (Entity entity in cell.Objects)
            {
                RenderPart render = entity.GetPart<RenderPart>();
                if (render == null || render.ParentEntity != entity || !render.Visible || render.RenderLayer > 1
                    || entity.SpatialZone != zone || zone.GetEntityCell(entity) != cell
                    || entity.HasTag("Creature") || entity.HasTag("Item") || entity.HasTag("Player")
                    || entity.HasPart<CropPart>() || entity.HasPart<FieldHarvestPart>()
                    || entity.HasPart<HarvestablePart>()
                    || entity.HasPart<CampfirePart>() || entity.HasPart<TorchLightPart>()
                    || entity.HasPart<LiquidPoolPart>() || entity.HasPart<RepairablePart>()
                    || entity.GetPart<PhysicsPart>()?.Takeable == true
                    || (!entity.HasTag("Terrain") && !entity.HasTag("Wall"))) continue;
                if (render.RenderLayer >= layer) { result = entity; layer = render.RenderLayer; }
            }
            return result;
        }

        private static FlatPixelGreyboxKind Classify(Entity owner)
        {
            if (owner == null) return FlatPixelGreyboxKind.Ground;
            if (owner.HasTag("Player")) return FlatPixelGreyboxKind.Player;
            if (owner.HasTag("Creature")) return FlatPixelGreyboxKind.Actor;
            var crop = owner.GetPart<CropPart>();
            if (crop != null)
            {
                if (crop.ParentEntity != owner) return FlatPixelGreyboxKind.Unknown;
                switch (crop.GrowthStage)
                {
                    case 0: return FlatPixelGreyboxKind.CropSeed;
                    case 1: return FlatPixelGreyboxKind.CropSprout;
                    case 2: return crop.HarvestAtMaturity ? FlatPixelGreyboxKind.CropRipe : FlatPixelGreyboxKind.Unknown;
                    default: return FlatPixelGreyboxKind.Unknown;
                }
            }
            var field = owner.GetPart<FieldHarvestPart>();
            if (field != null)
                return field.ParentEntity != owner ? FlatPixelGreyboxKind.Unknown
                    : field.Harvested ? FlatPixelGreyboxKind.CropSpent : FlatPixelGreyboxKind.CropRipe;
            var repair = owner.GetPart<RepairablePart>();
            if (repair != null && !repair.PortableEquipment)
            {
                if (repair.ParentEntity != owner) return FlatPixelGreyboxKind.Unknown;
                if (!repair.Repaired) return FlatPixelGreyboxKind.DamagedProp;
            }
            var pool = owner.GetPart<LiquidPoolPart>();
            if (pool != null)
                return pool.ParentEntity != owner || pool.LiquidId != "water" ? FlatPixelGreyboxKind.Unknown
                    : pool.Volume > 0 ? FlatPixelGreyboxKind.WaterSource : FlatPixelGreyboxKind.EmptyWaterSource;
            if (owner.HasTag("Wall")) return FlatPixelGreyboxKind.Wall;
            if (owner.HasTag("Item") || owner.GetPart<PhysicsPart>()?.Takeable == true)
                return FlatPixelGreyboxKind.LooseItem;
            switch (owner.BlueprintName)
            {
                case "Grass": return FlatPixelGreyboxKind.Ground;
                case "Floor": if (IsPlainFloor(owner)) return FlatPixelGreyboxKind.Ground; break;
                case "RoadStone": return FlatPixelGreyboxKind.Road;
                case "CropRow": return FlatPixelGreyboxKind.CropSpent;
                case "Reeds": case "Bush": case "MushroomRing": return FlatPixelGreyboxKind.Vegetation;
                case "Rubble": case "BrokenColumn": return FlatPixelGreyboxKind.Rubble;
            }
            if (owner.HasTag("Vegetation")) return FlatPixelGreyboxKind.Vegetation;
            if (owner.GetPart<PhysicsPart>()?.ParentEntity == owner) return FlatPixelGreyboxKind.Prop;
            return FlatPixelGreyboxKind.Unknown;
        }

        private static bool IsPlainFloor(Entity owner)
        {
            if (owner?.BlueprintName != "Floor" || !owner.HasTag("Terrain")) return false;
            var render = owner.GetPart<RenderPart>();
            var physics = owner.GetPart<PhysicsPart>();
            return render != null && render.ParentEntity == owner && render.Visible
                && string.IsNullOrEmpty(render.VisualID)
                && physics != null && physics.ParentEntity == owner && !physics.Solid && !physics.Takeable;
        }

        private static FlatPixelGreyboxGround GroundOf(Zone zone, Cell cell, Entity owner, bool remembered)
        {
            if (!remembered)
            {
                var coatings = zone.TileState.Get(cell.X, cell.Y)?.Coatings;
                if (coatings != null)
                    foreach (var coating in coatings)
                        if (coating.Id == "water" && coating.Turns > 0) return FlatPixelGreyboxGround.Water;
                // Soil belongs to the real lower crop even while an actor or
                // loose item covers it. This reads state without advancing time.
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    Entity plant = cell.Objects[i]; var crop = plant.GetPart<CropPart>();
                    var render = plant.GetPart<RenderPart>();
                    if (crop != null && crop.ParentEntity == plant
                        && CropTime.IsCurrentOwner(crop, zone) && CurrentOwnerCell(zone, cell, plant)
                        && render != null && render.ParentEntity == plant && render.Visible
                        && crop.GrowthStage >= 0 && crop.GrowthStage <= (crop.HarvestAtMaturity ? 2 : 1))
                        return crop.MoistureTicks > 0 ? FlatPixelGreyboxGround.WetSoil : FlatPixelGreyboxGround.DrySoil;
                }
                // A fixed field row also owns its substrate below an actor or
                // dropped item. Selecting the foreground must not erase soil.
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var row = cell.Objects[i];
                    var field = row?.GetPart<FieldHarvestPart>();
                    var render = row?.GetPart<RenderPart>();
                    var physics = row?.GetPart<PhysicsPart>();
                    var footprint = row?.GetPart<SpatialFootprintPart>();
                    if (row != null && (field != null && field.ParentEntity == row || row.BlueprintName == "CropRow")
                        && CurrentOwnerCell(zone, cell, row)
                        && (footprint == null || footprint.ParentEntity == row && zone.IsFootprintCurrent(row)
                            && SpatialQuery.DistanceToCell(zone, row, cell.X, cell.Y) == 0)
                        && render != null && render.ParentEntity == row && render.Visible
                        && physics != null && physics.ParentEntity == row && !physics.Takeable && !physics.Solid
                        && physics.InInventory == null && physics.Equipped == null)
                        return FlatPixelGreyboxGround.DrySoil;
                }
                if (CultivatedSoilPart.IsCultivated(zone, cell))
                    return FlatPixelGreyboxGround.DrySoil;
            }
            // Roads and ordinary floor are ground geometry. No hidden crop/soil
            // or liquid states are read; roads retain priority over their substrate.
            bool stoneFloor = false;
            foreach (Entity entity in cell.Objects)
            {
                if (entity.SpatialZone != zone || zone.GetEntityCell(entity) != cell) continue;
                if (entity.BlueprintName == "RoadStone" && entity.GetPart<RenderPart>()?.Visible == true)
                    return FlatPixelGreyboxGround.Road;
                if (IsPlainFloor(entity)) stoneFloor = true;
            }
            return stoneFloor ? FlatPixelGreyboxGround.StoneFloor : FlatPixelGreyboxGround.Ground;
        }

        private static Color GroundColor(FlatPixelGreyboxGround kind)
        {
            switch (kind)
            {
                case FlatPixelGreyboxGround.Road: return new Color32(49, 65, 45, 255);
                case FlatPixelGreyboxGround.DrySoil: return new Color32(69, 52, 31, 255);
                case FlatPixelGreyboxGround.WetSoil: return new Color32(37, 35, 25, 255);
                case FlatPixelGreyboxGround.Water: return new Color32(12, 67, 68, 255);
                case FlatPixelGreyboxGround.StoneFloor: return new Color32(24, 44, 41, 255);
                default: return new Color32(3, 47, 35, 255);
            }
        }

        private static Color ForegroundColor(FlatPixelGreyboxKind kind)
        {
            switch (kind)
            {
                case FlatPixelGreyboxKind.Wall: return new Color32(128, 148, 130, 255);
                case FlatPixelGreyboxKind.Player: return new Color32(237, 188, 80, 255);
                case FlatPixelGreyboxKind.Actor: return new Color32(209, 217, 171, 255);
                case FlatPixelGreyboxKind.Vegetation: return new Color32(94, 127, 62, 255);
                case FlatPixelGreyboxKind.CropRipe: return new Color32(210, 168, 73, 255);
                case FlatPixelGreyboxKind.CropSprout: return new Color32(109, 153, 72, 255);
                case FlatPixelGreyboxKind.CropSeed: case FlatPixelGreyboxKind.CropSpent: return new Color32(142, 117, 66, 255);
                case FlatPixelGreyboxKind.WaterSource: return new Color32(90, 172, 173, 255);
                case FlatPixelGreyboxKind.DamagedProp: return new Color32(172, 105, 63, 255);
                case FlatPixelGreyboxKind.LooseItem: return new Color32(221, 203, 153, 255);
                case FlatPixelGreyboxKind.Unknown: return new Color32(199, 128, 183, 255);
                default: return new Color32(150, 137, 100, 255);
            }
        }

        private void BuildAtlas()
        {
            int width = AtlasColumns * FrameWidth;
            int height = ((_tiles.Length + AtlasColumns - 1) / AtlasColumns) * FrameHeight;
            var pixels = new Color32[width * height];
            for (int i = 0; i < _tiles.Length; i++)
            {
                int left = i % AtlasColumns * FrameWidth, bottom = i / AtlasColumns * FrameHeight;
                for (int y = 0; y < FrameHeight; y++) for (int x = 0; x < FrameWidth; x++)
                    if (Filled((FlatPixelGreyboxKind)i, x, y))
                        pixels[(bottom + y) * width + left + x] = new Color32(255, 255, 255, 255);
            }
            _atlas = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Live16x24 Greybox Atlas (placeholder)", filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
            };
            _atlas.SetPixels32(pixels);
            _atlas.Apply(false, false);
            for (int i = 0; i < _tiles.Length; i++)
            {
                var rect = new Rect(i % AtlasColumns * FrameWidth, i / AtlasColumns * FrameHeight, FrameWidth, FrameHeight);
                var sprite = Sprite.Create(_atlas, rect, new Vector2(0.5f, 0.5f), FrameWidth, 0, SpriteMeshType.FullRect);
                sprite.name = "Greybox " + (FlatPixelGreyboxKind)i;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite;
                tile.flags = TileFlags.None;
                tile.colliderType = Tile.ColliderType.None;
                _sprites[i] = sprite;
                _tiles[i] = tile;
            }
        }

        private static bool Filled(FlatPixelGreyboxKind kind, int x, int y)
        {
            switch (kind)
            {
                case FlatPixelGreyboxKind.Ground: case FlatPixelGreyboxKind.Road: return true;
                case FlatPixelGreyboxKind.Wall: return x >= 1 && x <= 14 && y >= 2 && y <= 21 && y != 10 && y != 17;
                case FlatPixelGreyboxKind.Player: case FlatPixelGreyboxKind.Actor:
                    return x >= 5 && x <= 10 && y >= 16 && y <= 20
                        || x >= 4 && x <= 11 && y >= 6 && y <= 14
                        || y >= 2 && y < 6 && (x >= 4 && x <= 6 || x >= 9 && x <= 11);
                case FlatPixelGreyboxKind.Vegetation:
                    return (x == 4 || x == 8 || x == 12) && y >= 3 && y <= 14
                        || y >= 10 && y <= 12 && x >= 2 && x <= 13 && x % 4 != 0;
                case FlatPixelGreyboxKind.Rubble:
                    return y >= 3 && y <= 7 && x >= 2 && x <= 7 || y >= 4 && y <= 10 && x >= 10 && x <= 13;
                case FlatPixelGreyboxKind.CropSeed:
                    return y >= 4 && y <= 5 && (x >= 3 && x <= 5 || x >= 10 && x <= 12);
                case FlatPixelGreyboxKind.CropSprout:
                    return x >= 7 && x <= 8 && y >= 3 && y <= 10 || y >= 8 && y <= 10 && x >= 3 && x <= 12;
                case FlatPixelGreyboxKind.CropRipe:
                    return (x == 4 || x == 8 || x == 12) && y >= 3 && y <= 14
                        || y >= 13 && y <= 17 && x >= 2 && x <= 14 && x % 4 != 2;
                case FlatPixelGreyboxKind.CropSpent:
                    return (x == 3 || x == 7 || x == 11) && y >= 3 && y <= 6;
                case FlatPixelGreyboxKind.LooseItem: return x >= 6 && x <= 10 && y >= 4 && y <= 9;
                case FlatPixelGreyboxKind.Prop: return x >= 3 && x <= 12 && y >= 3 && y <= 13;
                case FlatPixelGreyboxKind.DamagedProp: return x >= 3 && x <= 12 && y >= 3 && y <= 13 && x != y - 1;
                case FlatPixelGreyboxKind.WaterSource: case FlatPixelGreyboxKind.EmptyWaterSource:
                    return x >= 3 && x <= 12 && y >= 3 && y <= 11
                        && (kind == FlatPixelGreyboxKind.WaterSource || x == 3 || x == 12 || y == 3 || y == 11);
                default:
                    return y == 15 && x >= 5 && x <= 10 || x == 10 && y >= 12 && y <= 14
                        || y == 11 && x >= 7 && x <= 9 || x == 7 && y >= 8 && y <= 10
                        || x == 7 && y == 5;
            }
        }

        /// <summary>Continuous flat presentation. G1.1 validates the actual owner;
        /// exact local art/ground stays local, while the four motion identities
        /// may carry their native idle art into other current world graphs.</summary>
        public bool TryResolvePresentation(Zone zone, int x, int y, out FlatPixelGreyboxCell result)
        {
            result = default;
            if (!TryResolveFallback(zone, x, y, out var fallback)) return false;
            bool local = IsAdmitted(zone);
            var owner = fallback.Owner;
            var cell = zone.GetCell(x, y);
            var kind = local ? Classify(owner) : fallback.Kind;
            var ground = local ? GroundOf(zone, cell, owner, fallback.Remembered) : fallback.GroundKind;
            var groundColor = local ? GroundColor(ground) : fallback.GroundColor;
            var groundTile = local ? _tiles[(int)FlatPixelGreyboxKind.Ground] : fallback.GroundTile;
            if (local && !fallback.Remembered && FlatPixelOpeningGroundCatalog.IsSupportedZone(zone))
            {
                if (_groundArt == null && _loadOptionalResources)
                    _groundArt = new FlatPixelOpeningGroundCatalog(_glyphFallback);
                if (_groundArt != null && _groundArt.TryResolve(zone, x, y, out var nativeGround, out var groundTint))
                {
                    groundTile = nativeGround;
                    // These are opaque authored RGB pixels, not a white swatch
                    // to multiply by the existing semantic background color.
                    groundColor = groundTint;
                }
            }
            var spread = SpreadEnvironmentArtFor(zone);
            Tile spreadGroundTile = null;
            Entity spreadFloor = null;
            bool spreadGround = !fallback.Remembered && spread != null
                && spread.TryResolveGround(zone, x, y, out spreadGroundTile, out spreadFloor);
            if (spreadGround)
            {
                groundTile = spreadGroundTile; groundColor = Color.white;
                ground = spreadFloor.BlueprintName == "RoadStone" ? FlatPixelGreyboxGround.Road : FlatPixelGreyboxGround.Ground;
                if (owner == spreadFloor) kind = spreadFloor.BlueprintName == "RoadStone" ? FlatPixelGreyboxKind.Road : FlatPixelGreyboxKind.Ground;
            }
            var cellarFloor = local && !fallback.Remembered && ground == FlatPixelGreyboxGround.StoneFloor
                ? CellarFloorArtFor(zone) : null;
            if (cellarFloor != null && cellarFloor.TryResolve(zone, x, y, out var floorTile, out _))
            {
                groundTile = floorTile; groundColor = Color.white;
            }
            var court = local && !fallback.Remembered ? WorkingCourtArtFor(zone) : null;
            // Opaque material belongs to the real lower floor, independently of
            // the covering foreground. Higher-priority soil/hazards refuse it.
            if (court != null && court.TryResolve(zone, x, y, out var courtTile, out var courtFloor))
            {
                groundTile = courtTile; groundColor = Color.white;
                ground = FlatPixelGreyboxGround.StoneFloor;
                if (owner == courtFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            var sill = !fallback.Remembered ? SillEnvelopeArtFor(zone) : null;
            Tile sillGroundTile = null;
            Entity sillFloor = null;
            bool sillGround = sill != null && sill.TryResolveGround(zone, x, y, out sillGroundTile, out sillFloor);
            if (sillGround)
            {
                groundTile = sillGroundTile; groundColor = Color.white;
                ground = sillFloor.BlueprintName == "StoneFloor" ? FlatPixelGreyboxGround.StoneFloor : FlatPixelGreyboxGround.Ground;
                if (owner == sillFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            var sillRiver = !fallback.Remembered ? SillRiverArtFor(zone) : null;
            Tile sillWaterTile = null; Entity sillWaterSource = null;
            bool sillWater = sillRiver != null
                && sillRiver.TryResolveWaterGround(zone, x, y, out sillWaterTile, out sillWaterSource);
            if (sillWater)
            {
                groundTile = sillWaterTile; groundColor = Color.white;
                ground = FlatPixelGreyboxGround.Water;
                if (owner == sillWaterSource) kind = FlatPixelGreyboxKind.Ground;
            }
            var sodden = !fallback.Remembered ? SoddenTravelArtFor(zone) : null;
            Tile soddenGroundTile = null;
            Entity soddenFloor = null;
            bool soddenGround = sodden != null && sodden.TryResolveGround(zone, x, y, out soddenGroundTile, out soddenFloor);
            if (soddenGround)
            {
                groundTile = soddenGroundTile; groundColor = Color.white;
                ground = soddenFloor.BlueprintName == "StoneFloor" ? FlatPixelGreyboxGround.StoneFloor : FlatPixelGreyboxGround.Ground;
                if (owner == soddenFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            var beating = !fallback.Remembered ? BeatingMaterialsArtFor(zone) : null;
            Tile beatingGroundTile = null;
            Entity beatingFloor = null;
            bool beatingGround = beating != null && beating.TryResolveGround(zone, x, y, out beatingGroundTile, out beatingFloor);
            bool beatingGroundOwner = beatingGround && (owner == beatingFloor
                || beatingFloor.BlueprintName == "SaltCrust" && owner?.BlueprintName == "Sand"
                    && beating.IsRepresentedGroundOwner(zone, x, y, owner));
            if (beatingGround)
            {
                groundTile = beatingGroundTile; groundColor = Color.white;
                ground = beatingFloor.BlueprintName == "SandstoneFloor" ? FlatPixelGreyboxGround.StoneFloor : FlatPixelGreyboxGround.Ground;
                if (beatingGroundOwner) kind = FlatPixelGreyboxKind.Ground;
            }
            var grovelands = !fallback.Remembered ? GrovelandsMaterialsArtFor(zone) : null;
            Tile grovelandsGroundTile = null;
            Entity grovelandsFloor = null;
            bool grovelandsGround = grovelands != null && grovelands.TryResolveGround(zone, x, y, out grovelandsGroundTile, out grovelandsFloor);
            if (grovelandsGround)
            {
                groundTile = grovelandsGroundTile; groundColor = Color.white;
                ground = FlatPixelGreyboxGround.Ground;
                if (owner == grovelandsFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            Tile grovelandsWaterTile = null;
            Entity grovelandsWaterGrass = null;
            bool grovelandsWater = grovelands != null
                && grovelands.TryResolveWaterGround(zone, x, y, out grovelandsWaterTile, out grovelandsWaterGrass);
            if (grovelandsWater)
            {
                groundTile = grovelandsWaterTile; groundColor = Color.white;
                ground = FlatPixelGreyboxGround.Water;
                if (grovelandsWaterGrass != null && owner == grovelandsWaterGrass) kind = FlatPixelGreyboxKind.Ground;
            }
            var overwrit = !fallback.Remembered ? OverwritMaterialsArtFor(zone) : null;
            Tile overwritGroundTile = null;
            Entity overwritFloor = null;
            bool overwritGround = overwrit != null && overwrit.TryResolveGround(zone, x, y, out overwritGroundTile, out overwritFloor);
            if (overwritGround)
            {
                groundTile = overwritGroundTile; groundColor = Color.white;
                ground = FlatPixelGreyboxGround.Ground;
                if (owner == overwritFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            var stump = !fallback.Remembered ? StumpMaterialsArtFor(zone) : null;
            Tile stumpGroundTile = null; Entity stumpFloor = null;
            bool stumpGround = stump != null && stump.TryResolveGround(zone, x, y, out stumpGroundTile, out stumpFloor);
            if (stumpGround)
            {
                groundTile = stumpGroundTile; groundColor = Color.white; ground = FlatPixelGreyboxGround.Ground;
                if (owner == stumpFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            Tile stumpWaterTile = null; Entity stumpWaterSource = null;
            bool stumpWater = stump != null && stump.TryResolveWaterGround(zone, x, y, out stumpWaterTile, out stumpWaterSource);
            if (stumpWater)
            {
                groundTile = stumpWaterTile; groundColor = Color.white; ground = FlatPixelGreyboxGround.Water;
                if (owner == stumpWaterSource) kind = FlatPixelGreyboxKind.Ground;
            }
            // Ordinary authored settlements (2026-10-29): approved biome ground for
            // their own Grass, StoneFloor, Sand and RoadStone.
            var settlement = !fallback.Remembered ? SettlementGroundArtFor(zone) : null;
            Tile settlementGroundTile = null; Entity settlementFloor = null;
            bool settlementGround = settlement != null && settlement.TryResolveGround(zone, x, y, out settlementGroundTile, out settlementFloor);
            if (settlementGround)
            {
                groundTile = settlementGroundTile; groundColor = Color.white; ground = FlatPixelSettlementGroundCatalog.GroundKind(settlementFloor);
                if (owner == settlementFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            // Their permanent pools reuse the Sill river water views.
            Tile settlementWaterTile = null; Entity settlementWaterSource = null;
            bool settlementWater = settlement != null && settlement.TryResolveWaterGround(zone, x, y, out settlementWaterTile, out settlementWaterSource);
            if (settlementWater)
            {
                groundTile = settlementWaterTile; groundColor = Color.white; ground = FlatPixelGreyboxGround.Water;
                if (owner == settlementWaterSource) kind = FlatPixelGreyboxKind.Ground;
            }
            var caves = !fallback.Remembered ? ShallowCavesArtFor(zone) : null;
            Tile caveGroundTile = null; Entity caveFloor = null;
            bool caveGround = caves != null && caves.TryResolveGround(zone, x, y, out caveGroundTile, out caveFloor);
            if (caveGround)
            {
                groundTile = caveGroundTile; groundColor = Color.white; ground = FlatPixelGreyboxGround.StoneFloor;
                if (owner == caveFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            Tile caveBackdropTile = null; Color caveBackdropColor = default;
            bool caveBackdrop = !caveGround && caves != null
                && caves.TryResolveCellBackdrop(zone, x, y, out caveBackdropTile, out caveBackdropColor);
            if (caveBackdrop)
            {
                groundTile = caveBackdropTile; groundColor = caveBackdropColor;
            }
            var limestone = !fallback.Remembered ? LimestoneCavesArtFor(zone) : null;
            Tile limestoneGroundTile = null; Entity limestoneFloor = null;
            bool limestoneGround = limestone != null
                && limestone.TryResolveGround(zone, x, y, out limestoneGroundTile, out limestoneFloor);
            if (limestoneGround)
            {
                groundTile = limestoneGroundTile; groundColor = Color.white; ground = FlatPixelGreyboxGround.StoneFloor;
                if (owner == limestoneFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            Tile limestoneBackdropTile = null; Color limestoneBackdropColor = default;
            bool limestoneBackdrop = !limestoneGround && limestone != null
                && limestone.TryResolveCellBackdrop(zone, x, y, out limestoneBackdropTile, out limestoneBackdropColor);
            if (limestoneBackdrop)
            {
                groundTile = limestoneBackdropTile; groundColor = limestoneBackdropColor;
            }
            var shale = !fallback.Remembered ? ShaleCavesArtFor(zone) : null;
            Tile shaleGroundTile = null; Entity shaleFloor = null;
            bool shaleGround = shale != null
                && shale.TryResolveGround(zone, x, y, out shaleGroundTile, out shaleFloor);
            if (shaleGround)
            {
                groundTile = shaleGroundTile; groundColor = Color.white; ground = FlatPixelGreyboxGround.StoneFloor;
                if (owner == shaleFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            Tile shaleBackdropTile = null; Color shaleBackdropColor = default;
            bool shaleBackdrop = !shaleGround && shale != null
                && shale.TryResolveCellBackdrop(zone, x, y, out shaleBackdropTile, out shaleBackdropColor);
            if (shaleBackdrop)
            {
                groundTile = shaleBackdropTile; groundColor = shaleBackdropColor;
            }
            var slate = !fallback.Remembered ? SlateCavesArtFor(zone) : null;
            Tile slateGroundTile = null; Entity slateFloor = null;
            bool slateGround = slate != null
                && slate.TryResolveGround(zone, x, y, out slateGroundTile, out slateFloor);
            if (slateGround)
            {
                groundTile = slateGroundTile; groundColor = Color.white; ground = FlatPixelGreyboxGround.StoneFloor;
                if (owner == slateFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            Tile slateBackdropTile = null; Color slateBackdropColor = default;
            bool slateBackdrop = !slateGround && slate != null
                && slate.TryResolveCellBackdrop(zone, x, y, out slateBackdropTile, out slateBackdropColor);
            if (slateBackdrop)
            {
                groundTile = slateBackdropTile; groundColor = slateBackdropColor;
            }
            var quartzite = !fallback.Remembered ? QuartziteCavesArtFor(zone) : null;
            Tile quartziteGroundTile = null; Entity quartziteFloor = null;
            bool quartziteGround = quartzite != null
                && quartzite.TryResolveGround(zone, x, y, out quartziteGroundTile, out quartziteFloor);
            if (quartziteGround)
            {
                groundTile = quartziteGroundTile; groundColor = Color.white; ground = FlatPixelGreyboxGround.StoneFloor;
                if (owner == quartziteFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            Tile quartziteBackdropTile = null; Color quartziteBackdropColor = default;
            bool quartziteBackdrop = !quartziteGround && quartzite != null
                && quartzite.TryResolveCellBackdrop(zone, x, y, out quartziteBackdropTile, out quartziteBackdropColor);
            if (quartziteBackdrop)
            {
                groundTile = quartziteBackdropTile; groundColor = quartziteBackdropColor;
            }
            var obsidian = !fallback.Remembered ? ObsidianCavesArtFor(zone) : null;
            Tile obsidianGroundTile = null; Entity obsidianFloor = null;
            bool obsidianGround = obsidian != null
                && obsidian.TryResolveGround(zone, x, y, out obsidianGroundTile, out obsidianFloor);
            if (obsidianGround)
            {
                groundTile = obsidianGroundTile; groundColor = Color.white; ground = FlatPixelGreyboxGround.StoneFloor;
                if (owner == obsidianFloor) kind = FlatPixelGreyboxKind.Ground;
            }
            Tile obsidianBackdropTile = null; Color obsidianBackdropColor = default;
            bool obsidianBackdrop = !obsidianGround && obsidian != null
                && obsidian.TryResolveCellBackdrop(zone, x, y, out obsidianBackdropTile, out obsidianBackdropColor);
            if (obsidianBackdrop)
            {
                groundTile = obsidianBackdropTile; groundColor = obsidianBackdropColor;
            }
            var foreground = fallback.ForegroundTile;
            var foregroundColor = fallback.ForegroundColor;
            Tile art = null;
            Color boundaryTint = Color.white;
            var boundaries = BoundaryArtFor(zone, owner, fallback.Remembered, out bool allowNativeFallback);
            bool boundaryNative = boundaries != null
                && boundaries.TryResolve(zone, x, y, out art, out boundaryTint);
            var eastern = local ? EastKitchenArtFor(zone, owner) : null;
            bool eastNative = eastern != null && (fallback.Remembered
                ? eastern.TryResolve(owner, out art) : eastern.TryResolvePresentation(zone, owner, out art));
            bool farmerKnown = owner?.BlueprintName == "Farmer";
            var incidental = local || farmerKnown ? IncidentalActorArtFor(owner) : null;
            Tile incidentalArt = null;
            bool incidentalNative = incidental != null && incidental.TryResolve(owner, out incidentalArt);
            if (incidentalNative) art = incidentalArt;
            var equipment = IncidentalEquipmentArtFor(owner);
            Tile equipmentArt = null;
            bool equipmentNative = equipment != null && equipment.TryResolve(owner, out equipmentArt);
            if (equipmentNative) art = equipmentArt;
            var aftermath = local ? AftermathArtFor(owner) : null;
            bool aftermathNative = aftermath != null && aftermath.TryResolve(owner, out art);
            var orchard = local ? OrchardArtFor(owner) : null;
            bool orchardNative = orchard != null && orchard.TryResolve(owner, out art);
            var riverShrine = local ? RiverShrineArtFor(owner) : null;
            bool riverShrineNative = riverShrine != null && riverShrine.TryResolve(owner, out art);
            var nellaCorner = !fallback.Remembered ? NellaCornerArtFor(zone, owner) : null;
            // East may already have its approved isolated pipe frame. A missing
            // optional connected atlas must not erase that successful fallback.
            Tile connectedCornerArt = null;
            bool nellaCornerNative = nellaCorner != null && nellaCorner.TryResolve(zone, x, y, out connectedCornerArt);
            if (nellaCornerNative) art = connectedCornerArt;
            var eastFixture = local && !fallback.Remembered ? EastFixtureArtFor(zone, owner) : null;
            bool eastFixtureNative = eastFixture != null && eastFixture.TryResolve(zone, x, y, out art);
            var brine = local && !fallback.Remembered ? KitchenBrineArtFor(zone, owner) : null;
            Tile brineArt = null;
            bool brineNative = brine != null && brine.TryResolve(zone, x, y, out brineArt);
            if (brineNative) art = brineArt;
            bool allowHedgeFallback = true;
            var hedges = local ? HedgeArtFor(owner, fallback.Remembered, out allowHedgeFallback) : null;
            bool hedgeNative = hedges != null && hedges.TryResolve(zone, x, y, out art);
            Tile spreadArt = null;
            bool spreadNative = !fallback.Remembered && spread != null
                && spread.TryResolveForeground(zone, x, y, owner, out spreadArt);
            if (spreadNative) art = spreadArt;
            Tile sillArt = null;
            bool sillNative = sill != null && sill.TryResolveForeground(zone, x, y, owner, out sillArt);
            if (sillNative) art = sillArt;
            // Separate output storage preserves earlier successful portable art
            // if a missing/malformed optional regional group refuses this owner.
            Tile soddenArt = null;
            bool soddenNative = sodden != null && sodden.TryResolveForeground(zone, x, y, owner, out soddenArt);
            if (soddenNative) art = soddenArt;
            Tile beatingArt = null;
            bool beatingNative = beating != null && beating.TryResolveForeground(zone, x, y, owner, out beatingArt);
            if (beatingNative) art = beatingArt;
            Tile settlementArt = null;
            bool settlementNative = settlement != null && settlement.TryResolveForeground(zone, x, y, owner, out settlementArt);
            if (settlementNative) art = settlementArt;
            var sinkholeLip = !fallback.Remembered && owner?.BlueprintName == "SinkholeLip" ? SinkholeLipArtFor(zone) : null;
            Tile lipArt = null;
            bool lipNative = sinkholeLip != null && sinkholeLip.TryResolveForeground(zone, x, y, owner, out lipArt);
            if (lipNative) art = lipArt;
            Tile grovelandsArt = null;
            bool grovelandsNative = grovelands != null && grovelands.TryResolveForeground(zone, x, y, owner, out grovelandsArt);
            if (grovelandsNative) art = grovelandsArt;
            Tile overwritArt = null;
            bool overwritNative = overwrit != null && overwrit.TryResolveForeground(zone, x, y, owner, out overwritArt);
            if (overwritNative) art = overwritArt;
            Tile stumpArt = null;
            bool stumpNative = stump != null && stump.TryResolveForeground(zone, x, y, owner, out stumpArt);
            if (stumpNative) art = stumpArt;
            Tile caveArt = null;
            bool caveNative = caves != null && caves.TryResolveForeground(zone, x, y, owner, out caveArt);
            if (caveNative) art = caveArt;
            Tile limestoneArt = null;
            bool limestoneNative = limestone != null && limestone.TryResolveForeground(zone, x, y, owner, out limestoneArt);
            if (limestoneNative) art = limestoneArt;
            Tile shaleArt = null;
            bool shaleNative = shale != null && shale.TryResolveForeground(zone, x, y, owner, out shaleArt);
            if (shaleNative) art = shaleArt;
            Tile slateArt = null;
            bool slateNative = slate != null && slate.TryResolveForeground(zone, x, y, owner, out slateArt);
            if (slateNative) art = slateArt;
            Tile quartziteArt = null;
            bool quartziteNative = quartzite != null && quartzite.TryResolveForeground(zone, x, y, owner, out quartziteArt);
            if (quartziteNative) art = quartziteArt;
            Tile obsidianArt = null;
            bool obsidianNative = obsidian != null && obsidian.TryResolveForeground(zone, x, y, owner, out obsidianArt);
            if (obsidianNative) art = obsidianArt;
            Tile shaleRecessTile = null; Color shaleRecessColor = default;
            bool shaleRecessBackdrop = shaleNative && !shaleGround
                && shale.TryResolveWallRecessBackdrop(zone, x, y, owner, out shaleRecessTile, out shaleRecessColor);
            if (shaleRecessBackdrop)
            {
                groundTile = shaleRecessTile; groundColor = shaleRecessColor;
            }
            bool beatingBrineNative = beatingNative && owner.BlueprintName == "BrinePool";
            bool mireNative = soddenNative && owner.BlueprintName == "MirePool";
            bool productKnown = FlatPixelHarvestProductsCatalog.HasKnownRole(owner);
            var products = HarvestProductsArtFor(owner);
            Tile productArt = null;
            bool productNative = products != null && products.TryResolve(owner, out productArt);
            if (productKnown) art = productArt;
            bool faunaKnown = FlatPixelStumpFaunaCatalog.HasKnownRole(owner);
            var fauna = !fallback.Remembered ? StumpFaunaArtFor(owner) : null;
            Tile faunaArt = null;
            bool faunaNative = fauna != null && fauna.TryResolve(owner, out faunaArt);
            if (faunaKnown) art = faunaArt;
            bool groveActorKnown = FlatPixelGrovelandsActorsCatalog.HasKnownRole(owner);
            var groveActors = !fallback.Remembered ? GrovelandsActorsArtFor(owner) : null;
            Tile groveActorArt = null;
            bool groveActorNative = groveActors != null && groveActors.TryResolve(owner, out groveActorArt);
            if (groveActorKnown) art = groveActorArt;
            bool steamKnown = SharedSteamRole(zone, owner);
            var steam = !fallback.Remembered ? SharedSteamVentArtFor(zone, owner) : null;
            Tile steamArt = null;
            bool steamNative = steam != null && steam.TryResolve(owner, out steamArt);
            bool stoneKnown = SharedStoneRole(owner);
            var stone = !fallback.Remembered ? SharedStonePropsArtFor(owner) : null;
            Tile stoneArt = null;
            bool stoneNative = stone != null && stone.TryResolve(owner, out stoneArt);
            bool furnitureKnown = FlatPixelSharedFurnitureCatalog.HasKnownRole(owner);
            var furniture = !fallback.Remembered ? SharedFurnitureArtFor(owner) : null;
            Tile furnitureArt = null;
            bool furnitureNative = furniture != null && furniture.TryResolve(owner, out furnitureArt);
            if (furnitureKnown) art = furnitureArt;
            bool storageKnown = FlatPixelSharedStorageCatalog.HasKnownRole(owner);
            var storage = !fallback.Remembered ? SharedStorageArtFor(owner) : null;
            Tile storageArt = null;
            bool storageNative = storage != null && storage.TryResolve(owner, out storageArt);
            if (storageKnown) art = storageArt;
            if (stoneKnown) art = stoneArt;
            if (steamKnown) art = steamArt;
            bool residentKnown = FlatPixelSharedResidentsCatalog.HasKnownRole(owner);
            var residents = !fallback.Remembered ? SharedResidentsArtFor(owner) : null;
            Tile residentArt = null;
            bool residentNative = residents != null && residents.TryResolve(owner, out residentArt);
            if (residentKnown) art = residentArt;
            bool clothKnown = FlatPixelSharedCampClothCatalog.HasKnownRole(owner);
            var cloth = !fallback.Remembered ? SharedCampClothArtFor(owner) : null;
            Tile clothArt = null;
            bool clothNative = cloth != null && cloth.TryResolve(owner, x, y, out clothArt);
            if (clothKnown) art = clothArt;
            bool campKnown = FlatPixelSharedCampPropsCatalog.HasKnownRole(owner);
            var camp = !fallback.Remembered ? SharedCampPropsArtFor(owner) : null;
            Tile campArt = null;
            bool campNative = camp != null && camp.TryResolve(owner, out campArt);
            if (campKnown) art = campArt;
            bool beatingActorKnown = FlatPixelBeatingActorsCatalog.HasKnownRole(owner);
            var beatingActors = !fallback.Remembered ? BeatingActorsArtFor(owner) : null;
            Tile beatingActorArt = null;
            bool beatingActorNative = beatingActors != null && beatingActors.TryResolve(owner, out beatingActorArt);
            if (beatingActorKnown) art = beatingActorArt;
            bool sharedVineKnown = SharedVineRole(zone, owner);
            var vines = sharedVineKnown && !fallback.Remembered ? SharedVineWallArtFor(zone, owner) : null;
            Tile sharedVineArt = null;
            bool sharedVineNative = vines != null && vines.TryResolve(zone, x, y, out sharedVineArt);
            if (sharedVineKnown) art = sharedVineArt;
            bool greatdewKnown = FlatPixelGreatdewCatalog.HasKnownRole(owner);
            var greatdew = !fallback.Remembered ? GreatdewArtFor(owner) : null;
            Tile greatdewArt = null;
            bool greatdewNative = greatdew != null && greatdew.TryResolve(owner, out greatdewArt);
            if (greatdewKnown) art = greatdewArt;
            bool essentialsKnown = FlatPixelExpeditionEssentialsCatalog.HasKnownRole(owner);
            var essentials = !fallback.Remembered ? ExpeditionEssentialsArtFor(owner) : null;
            Tile essentialsArt = null;
            bool essentialsNative = essentials != null && essentials.TryResolve(owner, out essentialsArt);
            if (essentialsKnown) art = essentialsArt;
            bool soddenFrogKnown = FlatPixelSoddenFrogsCatalog.HasKnownRole(owner);
            var soddenFrogs = !fallback.Remembered ? SoddenFrogsArtFor(owner) : null;
            Tile soddenFrogArt = null;
            bool soddenFrogNative = soddenFrogs != null && soddenFrogs.TryResolve(owner, out soddenFrogArt);
            if (soddenFrogKnown) art = soddenFrogArt;
            bool portableCropKnown = FlatPixelOpeningArtCatalog.HasPortableCropRole(owner);
            Tile portableCropArt = null;
            bool portableCropNative = portableCropKnown && !fallback.Remembered
                && _openingArt.TryResolvePortableCrop(owner, out portableCropArt);
            if (portableCropKnown) art = portableCropArt;
            // Known-product refusal is authoritative: older local item groups
            // must not bypass current semantic or optional-resource guards.
            // The same applies to exact fauna; portable species do not admit
            // regional scenery or inherit another body's art after refusal.
            bool portableEquipmentKnown = FlatPixelIncidentalEquipmentCatalog.IsSidearm(owner) || owner?.BlueprintName == "Buckler";
            if (portableEquipmentKnown) art = equipmentArt;
            if (farmerKnown) art = incidentalArt;
            bool native = portableCropKnown ? portableCropNative : essentialsKnown ? essentialsNative : greatdewKnown ? greatdewNative : sharedVineKnown ? sharedVineNative : soddenFrogKnown ? soddenFrogNative : farmerKnown ? incidentalNative : portableEquipmentKnown ? equipmentNative : clothKnown ? clothNative : campKnown ? campNative : beatingActorKnown ? beatingActorNative : residentKnown ? residentNative : steamKnown ? steamNative : stoneKnown ? stoneNative : furnitureKnown ? furnitureNative : storageKnown ? storageNative : groveActorKnown ? groveActorNative : faunaKnown ? faunaNative : productKnown ? productNative : boundaryNative || eastNative || incidentalNative || equipmentNative || aftermathNative || orchardNative || riverShrineNative || nellaCornerNative || eastFixtureNative || brineNative || hedgeNative || spreadNative || sillNative || soddenNative || beatingNative || settlementNative || lipNative || grovelandsNative || overwritNative || stumpNative || caveNative || limestoneNative || shaleNative || slateNative || quartziteNative || obsidianNative
                || allowNativeFallback && allowHedgeFallback
                    && ((!local && _openingArt.TryResolvePortableLongSword(owner, out art))
                        || (local || FlatPixelActorMotionCatalog.IsPortableActor(owner))
                            && _openingArt.TryResolve(owner, x, y, out art));
            if (native) { foreground = art; foregroundColor = boundaryNative ? boundaryTint : Color.white; }
            else if (local && (kind == FlatPixelGreyboxKind.Ground || kind == FlatPixelGreyboxKind.Road)
                || spreadGround && owner == spreadFloor
                || sillGround && owner == sillFloor || soddenGround && owner == soddenFloor || beatingGroundOwner
                || sillWater && owner == sillWaterSource
                || grovelandsGround && owner == grovelandsFloor || overwritGround && owner == overwritFloor
                || grovelandsWater && grovelandsWaterGrass != null && owner == grovelandsWaterGrass
                || stumpGround && owner == stumpFloor || stumpWater && owner == stumpWaterSource
                || settlementGround && owner == settlementFloor || settlementWater && owner == settlementWaterSource
                || caveGround && owner == caveFloor || limestoneGround && owner == limestoneFloor
                || shaleGround && owner == shaleFloor || slateGround && owner == slateFloor
                || quartziteGround && owner == quartziteFloor || obsidianGround && owner == obsidianFloor) foreground = null;
            if (fallback.Remembered)
            {
                if (local) { groundColor *= .35f; groundColor.a = 1; }
                // The boundary adapter already returns its conservative memory
                // tint. Other native groups retain this existing single dim.
                if (native && !boundaryNative) { foregroundColor *= .35f; foregroundColor.a = 1; }
            }
            // Static memory has a separate policy: no live floor/condition or
            // adjacency queries, and no reader/collision authority. The selector
            // has already excluded live families. Keep a wall's fallback ground.
            bool sillMemoryGround = false;
            if (fallback.Remembered)
            {
                var memory = SillEnvelopeArtFor(zone);
                if (memory != null && memory.TryResolveRemembered(zone, x, y, owner,
                    out var memoryGround, out var memoryWall))
                {
                    var tint = new Color(.35f, .35f, .35f, 1f);
                    if (memoryGround != null)
                    {
                        groundTile = memoryGround; groundColor = tint; foreground = null;
                        kind = FlatPixelGreyboxKind.Ground; native = false; sillMemoryGround = true;
                        ground = owner.BlueprintName == "StoneFloor"
                            ? FlatPixelGreyboxGround.StoneFloor : FlatPixelGreyboxGround.Ground;
                    }
                    else
                    {
                        foreground = memoryWall; foregroundColor = tint;
                        kind = FlatPixelGreyboxKind.Wall; native = true;
                    }
                }
                // Ordinary settlements keep the same static memory policy (2026-10-30).
                var settlementMemory = memory == null ? SettlementGroundArtFor(zone) : null;
                if (settlementMemory != null && settlementMemory.TryResolveRemembered(zone, x, y, owner,
                    out var settledGround, out var settledWall))
                {
                    var tint = new Color(.35f, .35f, .35f, 1f);
                    if (settledGround != null)
                    {
                        groundTile = settledGround; groundColor = tint; foreground = null;
                        kind = FlatPixelGreyboxKind.Ground; native = false; sillMemoryGround = true;
                        ground = FlatPixelSettlementGroundCatalog.GroundKind(owner);
                    }
                    else
                    {
                        foreground = settledWall; foregroundColor = tint;
                        kind = FlatPixelGreyboxKind.Wall; native = true;
                    }
                }
            }
            if (local && !fallback.Remembered && owner?.BlueprintName == "MarlbackPatchbearer")
                ObserveSupply(zone, cell, owner, foreground, true);
            if (local && !fallback.Remembered && ServiceSubject(owner))
                ObserveService(zone, cell, owner, foreground);
            result = new FlatPixelGreyboxCell(groundTile,
                foreground, groundColor, foregroundColor, owner, kind, ground, fallback.Remembered,
                fallback.BlocksMovement, local && !fallback.Remembered ? ReserveCord(zone, cell, out _) : null, native, local || spreadGround || sillGround || sillWater || sillMemoryGround || soddenGround || beatingGround || grovelandsGround || grovelandsWater || overwritGround || stumpGround || stumpWater || settlementGround || settlementWater || caveGround || limestoneGround || shaleGround || slateGround || quartziteGround || obsidianGround, brineNative || beatingBrineNative, mireNative, nellaCornerNative && !local || spreadNative || farmerKnown && incidentalNative || faunaNative || groveActorNative || storageNative || furnitureNative || stoneNative || steamNative || residentNative || beatingActorNative || soddenFrogNative || sharedVineNative || greatdewNative || essentialsNative || campNative || clothNative || owner?.BlueprintName == "Buckler" && equipmentNative, caveBackdrop || limestoneBackdrop || shaleBackdrop || shaleRecessBackdrop || slateBackdrop || quartziteBackdrop || obsidianBackdrop);
            return true;
        }

        /// <summary>Shared world/examine art policy. This does not authorize a
        /// subject: callers must validate the current world or carried document.</summary>
        public bool TryGetPresentationSubjectSprite(Zone zone, Entity subject, int x, int y, out Sprite sprite, out Color tint)
        {
            sprite = null; tint = Color.white;
            if (_disposed || !FlatPixelFallbackCatalog.IsCurrentGraph(zone)) return false;
            if (FlatPixelOpeningArtCatalog.HasPortableCropRole(subject))
            {
                if (_openingArt.TryResolvePortableCrop(subject, out var cropTile))
                { sprite = cropTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (SharedVineRole(zone, subject))
            {
                var vines = SharedVineWallArtFor(zone, subject);
                if (vines != null && vines.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            // Farmer stays in its existing optional catalog, portable on the
            // modern routes. Refusal cannot borrow unrelated opening body art.
            if (subject?.BlueprintName == "Farmer")
            {
                var farmers = IncidentalActorArtFor(subject);
                if (farmers != null && farmers.TryResolve(subject, out var farmerTile))
                { sprite = farmerTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            // Exact portable equipment refusal must not fall through to unrelated local art.
            if (FlatPixelIncidentalEquipmentCatalog.IsSidearm(subject) || subject?.BlueprintName == "Buckler")
            {
                var sidearms = IncidentalEquipmentArtFor(subject);
                if (sidearms != null && sidearms.TryResolve(subject, out var sidearmTile))
                { sprite = sidearmTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelSharedCampClothCatalog.HasKnownRole(subject))
            {
                var cloth = SharedCampClothArtFor(subject);
                if (cloth != null && cloth.TryResolve(subject, x, y, out var clothTile))
                { sprite = clothTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelSharedCampPropsCatalog.HasKnownRole(subject))
            {
                var camp = SharedCampPropsArtFor(subject);
                if (camp != null && camp.TryResolve(subject, out var campTile))
                { sprite = campTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelExpeditionEssentialsCatalog.HasKnownRole(subject))
            {
                var supplies = ExpeditionEssentialsArtFor(subject);
                if (supplies != null && supplies.TryResolve(subject, out var supplyTile))
                { sprite = supplyTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelGreatdewCatalog.HasKnownRole(subject))
            {
                var plant = GreatdewArtFor(subject);
                if (plant != null && plant.TryResolve(subject, out var plantTile))
                { sprite = plantTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelSoddenFrogsCatalog.HasKnownRole(subject))
            {
                var actors = SoddenFrogsArtFor(subject);
                if (actors != null && actors.TryResolve(subject, out var beatingBodyTile))
                { sprite = beatingBodyTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelBeatingActorsCatalog.HasKnownRole(subject))
            {
                var actors = BeatingActorsArtFor(subject);
                if (actors != null && actors.TryResolve(subject, out var beatingBodyTile))
                { sprite = beatingBodyTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelSharedResidentsCatalog.HasKnownRole(subject))
            {
                var residents = SharedResidentsArtFor(subject);
                if (residents.TryResolve(subject, out var residentTile))
                { sprite = residentTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (SharedSteamRole(zone, subject))
            {
                var steam = SharedSteamVentArtFor(zone, subject);
                if (steam != null && steam.TryResolve(subject, out var steamTile))
                { sprite = steamTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (SharedStoneRole(subject))
            {
                var stone = SharedStonePropsArtFor(subject);
                if (stone != null && stone.TryResolve(subject, out var stoneTile))
                { sprite = stoneTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelSharedFurnitureCatalog.HasKnownRole(subject))
            {
                var furniture = SharedFurnitureArtFor(subject);
                if (furniture != null && furniture.TryResolve(subject, out var furnitureTile))
                { sprite = furnitureTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelSharedStorageCatalog.HasKnownRole(subject))
            {
                var storage = SharedStorageArtFor(subject);
                if (storage != null && storage.TryResolve(subject, out var storageTile))
                { sprite = storageTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelGrovelandsActorsCatalog.HasKnownRole(subject))
            {
                var actors = GrovelandsActorsArtFor(subject);
                if (actors != null && actors.TryResolve(subject, out var groveActorTile))
                { sprite = groveActorTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelStumpFaunaCatalog.HasKnownRole(subject))
            {
                var fauna = StumpFaunaArtFor(subject);
                if (fauna != null && fauna.TryResolve(subject, out var faunaTile))
                { sprite = faunaTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            if (FlatPixelHarvestProductsCatalog.HasKnownRole(subject))
            {
                var products = HarvestProductsArtFor(subject);
                if (products != null && products.TryResolve(subject, out var productTile))
                { sprite = productTile.sprite; return sprite != null; }
                return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            }
            bool local = IsAdmitted(zone);
            var spread = SpreadEnvironmentArtFor(zone);
            if (spread != null && spread.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var sill = SillEnvelopeArtFor(zone);
            if (sill != null && sill.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var sillRiver = SillRiverArtFor(zone);
            if (sillRiver != null && sillRiver.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var sodden = SoddenTravelArtFor(zone);
            if (sodden != null && sodden.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var beating = BeatingMaterialsArtFor(zone);
            if (beating != null && beating.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var settlementGround = SettlementGroundArtFor(zone);
            if (settlementGround != null && settlementGround.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var lipReader = subject?.BlueprintName == "SinkholeLip" ? SinkholeLipArtFor(zone) : null;
            if (lipReader != null && lipReader.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var grovelands = GrovelandsMaterialsArtFor(zone);
            if (grovelands != null && grovelands.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var overwrit = OverwritMaterialsArtFor(zone);
            if (overwrit != null && overwrit.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var stump = StumpMaterialsArtFor(zone);
            if (stump != null && stump.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var caves = ShallowCavesArtFor(zone);
            if (caves != null && caves.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var limestone = LimestoneCavesArtFor(zone);
            if (limestone != null && limestone.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var shale = ShaleCavesArtFor(zone);
            if (shale != null && shale.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var slate = SlateCavesArtFor(zone);
            if (slate != null && slate.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var quartzite = QuartziteCavesArtFor(zone);
            if (quartzite != null && quartzite.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var obsidian = ObsidianCavesArtFor(zone);
            if (obsidian != null && obsidian.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var boundaries = BoundaryArtFor(zone, subject, false, out bool allowNativeFallback);
            if (!allowNativeFallback) return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            if (boundaries != null && boundaries.TryGetSubjectSprite(subject, out sprite, out tint)) return true;
            bool allowHedgeFallback = true;
            var hedges = local ? HedgeArtFor(subject, false, out allowHedgeFallback) : null;
            if (!allowHedgeFallback) return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
            if (hedges != null && hedges.TryGetSubjectSprite(subject, out sprite)) return true;
            var court = local ? WorkingCourtArtFor(zone) : null;
            if (court != null && court.TryResolve(zone, x, y, out var courtTile, out var courtFloor) && courtFloor == subject)
            { sprite = courtTile.sprite; return sprite != null; }
            var incidental = local ? IncidentalActorArtFor(subject) : null;
            if (incidental != null && incidental.TryResolve(subject, out var actorTile))
            { sprite = actorTile.sprite; return sprite != null; }
            var equipment = IncidentalEquipmentArtFor(subject);
            if (equipment != null && equipment.TryResolve(subject, out var equipmentTile))
            { sprite = equipmentTile.sprite; return sprite != null; }
            var aftermath = local ? AftermathArtFor(subject) : null;
            if (aftermath != null && aftermath.TryResolve(subject, out var aftermathTile))
            { sprite = aftermathTile.sprite; return sprite != null; }
            var orchard = local ? OrchardArtFor(subject) : null;
            if (orchard != null && orchard.TryResolve(subject, out var hiveTile))
            { sprite = hiveTile.sprite; return sprite != null; }
            var riverShrine = local ? RiverShrineArtFor(subject) : null;
            if (riverShrine != null && riverShrine.TryResolve(subject, out var shrineTile))
            { sprite = shrineTile.sprite; return sprite != null; }
            var nellaCorner = NellaCornerArtFor(zone, subject);
            if (nellaCorner != null && nellaCorner.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var eastFixture = local ? EastFixtureArtFor(zone, subject) : null;
            if (eastFixture != null && eastFixture.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var brine = local ? KitchenBrineArtFor(zone, subject) : null;
            if (brine != null && brine.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var eastern = local ? EastKitchenArtFor(zone, subject) : null;
            if (eastern != null && eastern.TryResolvePresentation(zone, subject, out var easternTile))
            { sprite = easternTile.sprite; return sprite != null; }
            if (!local && _openingArt.TryResolvePortableLongSword(subject, out var portableSwordTile))
            { sprite = portableSwordTile.sprite; return sprite != null; }
            if ((local || FlatPixelActorMotionCatalog.IsPortableActor(subject)) && _openingArt.TryResolve(subject, x, y, out var tile))
            { sprite = tile.sprite; return sprite != null; }
            var kind = Classify(subject);
            if (local && subject != null && (kind == FlatPixelGreyboxKind.Ground || kind == FlatPixelGreyboxKind.Road))
                return TryGetSubjectSprite(subject, x, y, out sprite, out tint);
            return TryGetFallbackSubjectSprite(subject, x, y, out sprite, out tint);
        }

        private FlatPixelSpreadEnvironmentCatalog SpreadEnvironmentArtFor(Zone zone)
        {
            if (!FlatPixelSpreadEnvironmentCatalog.IsSupportedZone(zone))
            {
                // Ordinary special sites of this biome use a separate, identically ruled instance.
                if (!FlatPixelSiteAdmission.IsOrdinarySite(zone, BiomeType.Spread)) return null;
                if (_spreadSiteArt == null && _loadOptionalResources)
                {
                    if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                    _spreadSiteArt = new FlatPixelSpreadEnvironmentCatalog(_glyphFallback).AdmittingSites(BiomeType.Spread);
                }
                return _spreadSiteArt;
            }
            if (_spreadEnvironmentArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _spreadEnvironmentArt = new FlatPixelSpreadEnvironmentCatalog(_glyphFallback);
            }
            return _spreadEnvironmentArt;
        }

        private FlatPixelSillEnvelopeCatalog SillEnvelopeArtFor(Zone zone)
        {
            if (!FlatPixelSillEnvelopeCatalog.IsSupportedZone(zone)) return null;
            if (_sillEnvelopeArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _sillEnvelopeArt = new FlatPixelSillEnvelopeCatalog(_glyphFallback);
            }
            return _sillEnvelopeArt;
        }

        private FlatPixelSillRiverCatalog SillRiverArtFor(Zone zone)
        {
            if (!FlatPixelSillRiverCatalog.IsSupportedZone(zone)) return null;
            if (_sillRiverArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _sillRiverArt = new FlatPixelSillRiverCatalog(_glyphFallback);
            }
            return _sillRiverArt;
        }

        private FlatPixelSoddenTravelCatalog SoddenTravelArtFor(Zone zone)
        {
            if (!FlatPixelSoddenTravelCatalog.IsSupportedZone(zone))
            {
                // Ordinary special sites of this biome use a separate, identically ruled instance.
                if (!FlatPixelSiteAdmission.IsOrdinarySite(zone, BiomeType.Sodden)) return null;
                if (_soddenSiteArt == null && _loadOptionalResources)
                {
                    if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                    _soddenSiteArt = new FlatPixelSoddenTravelCatalog(_glyphFallback).AdmittingSites();
                }
                return _soddenSiteArt;
            }
            if (_soddenTravelArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _soddenTravelArt = new FlatPixelSoddenTravelCatalog(_glyphFallback);
            }
            return _soddenTravelArt;
        }

        private FlatPixelSinkholeLipCatalog SinkholeLipArtFor(Zone zone)
        {
            if (!FlatPixelSinkholeLipCatalog.IsSupportedZone(zone)) return null;
            if (_sinkholeLipArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _sinkholeLipArt = new FlatPixelSinkholeLipCatalog(_glyphFallback);
            }
            return _sinkholeLipArt;
        }
        private FlatPixelSettlementGroundCatalog SettlementGroundArtFor(Zone zone)
        {
            if (!FlatPixelSettlementGroundCatalog.IsSupportedZone(zone)) return null;
            if (_settlementGroundArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _settlementGroundArt = new FlatPixelSettlementGroundCatalog(_glyphFallback);
            }
            return _settlementGroundArt;
        }

        private FlatPixelBeatingMaterialsCatalog BeatingMaterialsArtFor(Zone zone)
        {
            if (!FlatPixelBeatingMaterialsCatalog.IsSupportedZone(zone))
            {
                // Ordinary special sites of this biome use a separate, identically ruled instance.
                if (!FlatPixelSiteAdmission.IsOrdinarySite(zone, BiomeType.Beating)) return null;
                if (_beatingSiteArt == null && _loadOptionalResources)
                {
                    if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                    _beatingSiteArt = new FlatPixelBeatingMaterialsCatalog(_glyphFallback).AdmittingSites(BiomeType.Beating);
                }
                return _beatingSiteArt;
            }
            if (_beatingMaterialsArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _beatingMaterialsArt = new FlatPixelBeatingMaterialsCatalog(_glyphFallback);
            }
            return _beatingMaterialsArt;
        }

        private FlatPixelGrovelandsMaterialsCatalog GrovelandsMaterialsArtFor(Zone zone)
        {
            if (!FlatPixelGrovelandsMaterialsCatalog.IsSupportedZone(zone))
            {
                // Ordinary special sites of this biome use a separate, identically ruled instance.
                if (!FlatPixelSiteAdmission.IsOrdinarySite(zone, BiomeType.Grovelands)) return null;
                if (_grovelandsSiteArt == null && _loadOptionalResources)
                {
                    if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                    _grovelandsSiteArt = new FlatPixelGrovelandsMaterialsCatalog(_glyphFallback).AdmittingSites(BiomeType.Grovelands);
                }
                return _grovelandsSiteArt;
            }
            if (_grovelandsMaterialsArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _grovelandsMaterialsArt = new FlatPixelGrovelandsMaterialsCatalog(_glyphFallback);
            }
            return _grovelandsMaterialsArt;
        }

        private FlatPixelOverwritMaterialsCatalog OverwritMaterialsArtFor(Zone zone)
        {
            if (!FlatPixelOverwritMaterialsCatalog.IsSupportedZone(zone)) return null;
            if (_overwritMaterialsArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _overwritMaterialsArt = new FlatPixelOverwritMaterialsCatalog(_glyphFallback);
            }
            return _overwritMaterialsArt;
        }

        private FlatPixelShallowCavesCatalog ShallowCavesArtFor(Zone zone)
        {
            if (!FlatPixelShallowCavesCatalog.IsSupportedZone(zone)) return null;
            if (_shallowCavesArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _shallowCavesArt = new FlatPixelShallowCavesCatalog(_glyphFallback);
            }
            return _shallowCavesArt;
        }

        private FlatPixelLimestoneCavesCatalog LimestoneCavesArtFor(Zone zone)
        {
            if (!FlatPixelLimestoneCavesCatalog.IsSupportedZone(zone)) return null;
            if (_limestoneCavesArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _limestoneCavesArt = new FlatPixelLimestoneCavesCatalog(_glyphFallback);
            }
            return _limestoneCavesArt;
        }

        private FlatPixelShaleCavesCatalog ShaleCavesArtFor(Zone zone)
        {
            if (!FlatPixelShaleCavesCatalog.IsSupportedZone(zone)) return null;
            if (_shaleCavesArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _shaleCavesArt = new FlatPixelShaleCavesCatalog(_glyphFallback);
            }
            return _shaleCavesArt;
        }

        private FlatPixelSlateCavesCatalog SlateCavesArtFor(Zone zone)
        {
            if (!FlatPixelSlateCavesCatalog.IsSupportedZone(zone)) return null;
            if (_slateCavesArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _slateCavesArt = new FlatPixelSlateCavesCatalog(_glyphFallback);
            }
            return _slateCavesArt;
        }

        private FlatPixelQuartziteCavesCatalog QuartziteCavesArtFor(Zone zone)
        {
            if (!FlatPixelQuartziteCavesCatalog.IsSupportedZone(zone)) return null;
            if (_quartziteCavesArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _quartziteCavesArt = new FlatPixelQuartziteCavesCatalog(_glyphFallback);
            }
            return _quartziteCavesArt;
        }

        private FlatPixelObsidianCavesCatalog ObsidianCavesArtFor(Zone zone)
        {
            if (!FlatPixelObsidianCavesCatalog.IsSupportedZone(zone)) return null;
            if (_obsidianCavesArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _obsidianCavesArt = new FlatPixelObsidianCavesCatalog(_glyphFallback);
            }
            return _obsidianCavesArt;
        }

        private FlatPixelStumpMaterialsCatalog StumpMaterialsArtFor(Zone zone)
        {
            if (!FlatPixelStumpMaterialsCatalog.IsSupportedZone(zone))
            {
                // Ordinary special sites of this biome use a separate, identically ruled instance.
                if (!FlatPixelSiteAdmission.IsOrdinarySite(zone, BiomeType.Stump)) return null;
                if (_stumpSiteArt == null && _loadOptionalResources)
                {
                    if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                    _stumpSiteArt = new FlatPixelStumpMaterialsCatalog(_glyphFallback).AdmittingSites(BiomeType.Stump);
                }
                return _stumpSiteArt;
            }
            if (_stumpMaterialsArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _stumpMaterialsArt = new FlatPixelStumpMaterialsCatalog(_glyphFallback);
            }
            return _stumpMaterialsArt;
        }

        private FlatPixelIncidentalActorCatalog IncidentalActorArtFor(Entity owner)
        {
            if (!FlatPixelIncidentalActorCatalog.HasKnownRole(owner)) return null;
            if (_incidentalActorArt == null && _loadOptionalResources)
                _incidentalActorArt = new FlatPixelIncidentalActorCatalog();
            return _incidentalActorArt;
        }

        private FlatPixelStumpFaunaCatalog StumpFaunaArtFor(Entity owner)
        {
            if (!FlatPixelStumpFaunaCatalog.HasKnownRole(owner)) return null;
            if (_stumpFaunaArt == null && _loadOptionalResources)
                _stumpFaunaArt = new FlatPixelStumpFaunaCatalog();
            return _stumpFaunaArt;
        }

        private static bool SharedVineRole(Zone zone, Entity owner)
            => FlatPixelSharedVineWallCatalog.HasKnownRole(owner) && FlatPixelSharedVineWallCatalog.IsSupportedZone(zone);

        private FlatPixelSharedVineWallCatalog SharedVineWallArtFor(Zone zone, Entity owner)
        {
            if (!SharedVineRole(zone, owner)) return null;
            if (_sharedVineWallArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _sharedVineWallArt = new FlatPixelSharedVineWallCatalog(_glyphFallback);
            }
            return _sharedVineWallArt;
        }

        private FlatPixelSharedCampClothCatalog SharedCampClothArtFor(Entity owner)
        {
            if (!FlatPixelSharedCampClothCatalog.HasKnownRole(owner)) return null;
            if (_sharedCampClothArt == null && _loadOptionalResources)
                _sharedCampClothArt = new FlatPixelSharedCampClothCatalog();
            return _sharedCampClothArt;
        }

        private FlatPixelSharedCampPropsCatalog SharedCampPropsArtFor(Entity owner)
        {
            if (!FlatPixelSharedCampPropsCatalog.HasKnownRole(owner)) return null;
            if (_sharedCampPropsArt == null && _loadOptionalResources)
                _sharedCampPropsArt = new FlatPixelSharedCampPropsCatalog();
            return _sharedCampPropsArt;
        }

        private FlatPixelBeatingActorsCatalog BeatingActorsArtFor(Entity owner)
        {
            if (!FlatPixelBeatingActorsCatalog.HasKnownRole(owner)) return null;
            if (_beatingActorsArt == null && _loadOptionalResources)
                _beatingActorsArt = new FlatPixelBeatingActorsCatalog();
            return _beatingActorsArt;
        }

        private FlatPixelExpeditionEssentialsCatalog ExpeditionEssentialsArtFor(Entity owner)
        {
            if (!FlatPixelExpeditionEssentialsCatalog.HasKnownRole(owner)) return null;
            if (_expeditionEssentialsArt == null)
                _expeditionEssentialsArt = new FlatPixelExpeditionEssentialsCatalog(_harvestItemsAtlas,
                    _loadOptionalResources ? Resources.Load<Texture2D>(FlatPixelExpeditionEssentialsCatalog.BookResourcePath) : null,
                    _expeditionPlotProductsAtlas);
            return _expeditionEssentialsArt;
        }

        private FlatPixelGreatdewCatalog GreatdewArtFor(Entity owner)
        {
            if (!FlatPixelGreatdewCatalog.HasKnownRole(owner)) return null;
            if (_greatdewArt == null && _loadOptionalResources)
                _greatdewArt = new FlatPixelGreatdewCatalog();
            return _greatdewArt;
        }

        private FlatPixelSoddenFrogsCatalog SoddenFrogsArtFor(Entity owner)
        {
            if (!FlatPixelSoddenFrogsCatalog.HasKnownRole(owner)) return null;
            if (_soddenFrogsArt == null && _loadOptionalResources)
                _soddenFrogsArt = new FlatPixelSoddenFrogsCatalog();
            return _soddenFrogsArt;
        }

        private FlatPixelSharedResidentsCatalog SharedResidentsArtFor(Entity owner)
        {
            if (!FlatPixelSharedResidentsCatalog.HasKnownRole(owner)) return null;
            if (_sharedResidentsArt == null)
                _sharedResidentsArt = new FlatPixelSharedResidentsCatalog(_residentsCastAtlas,
                    _loadOptionalResources ? Resources.Load<Texture2D>("Sprites/Environment/merchant") : null,
                    _loadOptionalResources ? Resources.Load<Texture2D>("Sprites/Environment/weaponsmith") : null,
                    _loadOptionalResources ? Resources.Load<Texture2D>("Sprites/Live16x24/shared-specialists") : null,
                    _loadOptionalResources ? Resources.Load<Texture2D>("Sprites/Environment/elder") : null,
                    _loadOptionalResources ? Resources.Load<Texture2D>("Sprites/Environment/village_child") : null,
                    _loadOptionalResources ? Resources.Load<Texture2D>(FlatPixelSharedResidentsCatalog.KeepersResourcePath) : null);
            return _sharedResidentsArt;
        }

        // The admitted East kitchens keep their authored manufactured grille,
        // including its missing-resource fallback. Other contexts use the natural body.
        private static bool SharedSteamRole(Zone zone, Entity owner)
            => FlatPixelSharedSteamVentCatalog.HasKnownRole(owner)
                && !(IsAdmitted(zone) && (zone.ZoneID == "Overworld.12.10.0" || zone.ZoneID == "Overworld.12.11.0"));
        private FlatPixelSharedSteamVentCatalog SharedSteamVentArtFor(Zone zone, Entity owner)
        {
            if (!SharedSteamRole(zone, owner)) return null;
            if (_sharedSteamVentArt == null && _loadOptionalResources)
                _sharedSteamVentArt = new FlatPixelSharedSteamVentCatalog();
            return _sharedSteamVentArt;
        }

        private static bool SharedStoneRole(Entity owner)
            => FlatPixelSharedStonePropsCatalog.HasKnownRole(owner)
                && !FlatPixelSharedStonePropsCatalog.UsesAuthoredGladeBody(owner);

        private FlatPixelSharedStonePropsCatalog SharedStonePropsArtFor(Entity owner)
        {
            if (!SharedStoneRole(owner)) return null;
            if (_sharedStonePropsArt == null && _loadOptionalResources)
                _sharedStonePropsArt = new FlatPixelSharedStonePropsCatalog();
            return _sharedStonePropsArt;
        }

        private FlatPixelSharedFurnitureCatalog SharedFurnitureArtFor(Entity owner)
        {
            if (!FlatPixelSharedFurnitureCatalog.HasKnownRole(owner)) return null;
            if (_sharedFurnitureArt == null)
            {
                // Keep the borrowed East group authoritative. A default world
                // may already have warmed it during travel, so load the independent
                // counter group only when optional resources were enabled.
                if (_eastKitchenArt != null)
                    _sharedFurnitureArt = new FlatPixelSharedFurnitureCatalog(_eastKitchenArt.BorrowedFurnitureAtlas,
                        _loadOptionalResources ? Resources.Load<Texture2D>(FlatPixelSharedFurnitureCatalog.MarketStallResourcePath) : null);
                else if (_loadOptionalResources)
                    _sharedFurnitureArt = new FlatPixelSharedFurnitureCatalog();
            }
            return _sharedFurnitureArt;
        }

        private FlatPixelSharedStorageCatalog SharedStorageArtFor(Entity owner)
        {
            if (!FlatPixelSharedStorageCatalog.HasKnownRole(owner)) return null;
            if (_sharedStorageArt == null)
            {
                if (_loadOptionalResources) _sharedStorageArt = new FlatPixelSharedStorageCatalog();
                else if (_storageAnchorsAtlas != null || _storageCellarAtlas != null)
                    _sharedStorageArt = new FlatPixelSharedStorageCatalog(_storageAnchorsAtlas, _storageCellarAtlas, null, null);
            }
            return _sharedStorageArt;
        }

        private FlatPixelGrovelandsActorsCatalog GrovelandsActorsArtFor(Entity owner)
        {
            if (!FlatPixelGrovelandsActorsCatalog.HasKnownRole(owner)) return null;
            if (_grovelandsActorsArt == null && _loadOptionalResources)
                _grovelandsActorsArt = new FlatPixelGrovelandsActorsCatalog();
            return _grovelandsActorsArt;
        }

        private FlatPixelHarvestProductsCatalog HarvestProductsArtFor(Entity owner)
        {
            if (!FlatPixelHarvestProductsCatalog.HasKnownRole(owner)) return null;
            if (_harvestProductsArt == null)
            {
                if (_loadOptionalResources) _harvestProductsArt = new FlatPixelHarvestProductsCatalog();
                else if (_harvestItemsAtlas != null || _harvestWorksAtlas != null)
                    _harvestProductsArt = new FlatPixelHarvestProductsCatalog(null, _harvestItemsAtlas, _harvestWorksAtlas);
            }
            return _harvestProductsArt;
        }

        private FlatPixelIncidentalEquipmentCatalog IncidentalEquipmentArtFor(Entity owner)
        {
            if (!FlatPixelIncidentalEquipmentCatalog.HasKnownRole(owner)) return null;
            if (_incidentalEquipmentArt == null && _loadOptionalResources)
                _incidentalEquipmentArt = new FlatPixelIncidentalEquipmentCatalog();
            return _incidentalEquipmentArt;
        }

        private FlatPixelWorkingCourtCatalog WorkingCourtArtFor(Zone zone)
        {
            if (zone.ZoneID != "Overworld.12.11.0") return null;
            if (_workingCourtArt == null && _loadOptionalResources)
                _workingCourtArt = new FlatPixelWorkingCourtCatalog();
            return _workingCourtArt;
        }

        private FlatPixelAftermathCatalog AftermathArtFor(Entity owner)
        {
            if (!FlatPixelAftermathCatalog.HasKnownRole(owner)) return null;
            if (_aftermathArt == null && _loadOptionalResources)
                _aftermathArt = new FlatPixelAftermathCatalog();
            return _aftermathArt;
        }

        private FlatPixelOrchardCatalog OrchardArtFor(Entity owner)
        {
            if (!FlatPixelOrchardCatalog.HasKnownRole(owner)) return null;
            if (_orchardArt == null && _loadOptionalResources)
                _orchardArt = new FlatPixelOrchardCatalog();
            return _orchardArt;
        }

        private FlatPixelRiverShrineCatalog RiverShrineArtFor(Entity owner)
        {
            if (!FlatPixelRiverShrineCatalog.HasKnownRole(owner)) return null;
            if (_riverShrineArt == null && _loadOptionalResources)
                _riverShrineArt = new FlatPixelRiverShrineCatalog();
            return _riverShrineArt;
        }

        private FlatPixelNellaCornerCatalog NellaCornerArtFor(Zone zone, Entity owner)
        {
            if (!FlatPixelNellaCornerCatalog.SupportsRole(zone, owner)) return null;
            if (_nellaCornerArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _nellaCornerArt = new FlatPixelNellaCornerCatalog(_glyphFallback);
            }
            return _nellaCornerArt;
        }

        private FlatPixelEastFixtureCatalog EastFixtureArtFor(Zone zone, Entity owner)
        {
            if (zone.ZoneID != "Overworld.12.10.0" || !FlatPixelEastFixtureCatalog.HasKnownRole(owner)) return null;
            if (_eastFixtureArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _eastFixtureArt = new FlatPixelEastFixtureCatalog(_glyphFallback);
            }
            return _eastFixtureArt;
        }

        private FlatPixelKitchenBrineCatalog KitchenBrineArtFor(Zone zone, Entity owner)
        {
            if (zone.ZoneID != "Overworld.12.11.0" || owner?.BlueprintName != "BrinePool") return null;
            if (_kitchenBrineArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _kitchenBrineArt = new FlatPixelKitchenBrineCatalog(_glyphFallback);
            }
            return _kitchenBrineArt;
        }

        private FlatPixelHedgeCatalog HedgeArtFor(Entity owner, bool remembered, out bool allowNativeFallback)
        {
            allowNativeFallback = true;
            if (!_loadOptionalResources && _hedgeArt == null || !FlatPixelHedgeCatalog.HasKnownRole(owner)) return null;
            // Reject known invalid semantics even when the optional atlas is
            // missing. Never inspect hidden live condition to update memory.
            allowNativeFallback = !remembered && FlatPixelHedgeCatalog.IsEligible(owner);
            if (!allowNativeFallback) return null;
            if (_hedgeArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _hedgeArt = new FlatPixelHedgeCatalog(_glyphFallback);
            }
            return _hedgeArt;
        }

        private FlatPixelCellarFloorCatalog CellarFloorArtFor(Zone zone)
        {
            if (zone.ZoneID != GleanersCellarBuilder.ZoneID) return null;
            if (_cellarFloorArt == null && _loadOptionalResources)
                _cellarFloorArt = new FlatPixelCellarFloorCatalog();
            return _cellarFloorArt;
        }

        private FlatPixelEastKitchenCatalog EastKitchenArtFor(Zone zone, Entity owner)
        {
            // This companion remains local even when portable actor art travels.
            // The caller has already established current regional admission.
            if (zone.ZoneID != "Overworld.12.10.0" && zone.ZoneID != "Overworld.12.11.0"
                || !FlatPixelEastKitchenCatalog.HasKnownRole(owner)) return null;
            if (_eastKitchenArt == null && _loadOptionalResources)
                _eastKitchenArt = new FlatPixelEastKitchenCatalog();
            return _eastKitchenArt;
        }

        private FlatPixelBoundaryCatalog BoundaryArtFor(Zone zone, Entity owner, bool remembered, out bool allowNativeFallback)
        {
            allowNativeFallback = true;
            if (!_loadOptionalResources && _boundaryArt == null
                || !ReferenceGladePlan.IsActive(zone) || !FlatPixelBoundaryCatalog.HasKnownRole(owner)) return null;
            // A missing optional texture permits old art for a valid owner.
            // Semantic rejection does not: old wall frames have weaker guards
            // and would otherwise conceal authored variants or invalid state.
            allowNativeFallback = FlatPixelBoundaryCatalog.IsEligible(owner, remembered);
            if (!allowNativeFallback) return null;
            if (_boundaryArt == null && _loadOptionalResources)
            {
                // Default construction alone opts into this optional group.
                // Missing Resources are cached as an unavailable adapter, and
                // all older injected constructors remain deterministic.
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _boundaryArt = new FlatPixelBoundaryCatalog(_glyphFallback);
            }
            return _boundaryArt;
        }

        /// <summary>Explicit unskinned-cell query. This does not change native
        /// admission or the existing authored/greybox resolver.</summary>
        public bool TryResolveFallback(Zone zone, int x, int y, out FlatPixelGreyboxCell result)
        {
            result = default;
            if (_disposed) return false;
            if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
            return _glyphFallback.TryResolve(zone, x, y, out result);
        }

        internal Tile PresentationBackgroundTile
        {
            get
            {
                if (_disposed) return null;
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                return _glyphFallback.BackgroundTile;
            }
        }

        /// <summary>Semantic fallback only; existing portrait authorization must
        /// precede this query. The borrowed world bank remains globally owned.</summary>
        public bool TryGetFallbackSubjectSprite(Entity subject, int x, int y, out Sprite sprite, out Color tint)
        {
            sprite = null; tint = Color.white;
            if (_disposed) return false;
            if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
            return _glyphFallback.TryGetSubjectSprite(subject, x, y, out sprite, out tint);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ClearObservedOwners();
            _openingArt.Dispose();
            _boundaryArt?.Dispose();
            _boundaryArt = null;
            _groundArt?.Dispose();
            _groundArt = null;
            _eastKitchenArt?.Dispose();
            _eastKitchenArt = null;
            _workingCourtArt?.Dispose();
            _workingCourtArt = null;
            _cellarFloorArt?.Dispose();
            _cellarFloorArt = null;
            _incidentalActorArt?.Dispose();
            _incidentalActorArt = null;
            _stumpFaunaArt?.Dispose();
            _stumpFaunaArt = null;
            _sharedFurnitureArt?.Dispose();
            _sharedCampClothArt?.Dispose();
            _sharedCampClothArt = null;
            _sharedCampPropsArt?.Dispose();
            _sharedCampPropsArt = null;
            _greatdewArt?.Dispose();
            _greatdewArt = null;
            _expeditionEssentialsArt?.Dispose();
            _expeditionEssentialsArt = null;
            _soddenFrogsArt?.Dispose();
            _soddenFrogsArt = null;
            _sharedVineWallArt?.Dispose();
            _sharedVineWallArt = null;
            _beatingActorsArt?.Dispose();
            _beatingActorsArt = null;
            _sharedResidentsArt?.Dispose();
            _sharedResidentsArt = null;
            _sharedSteamVentArt?.Dispose();
            _sharedSteamVentArt = null;
            _sharedStonePropsArt?.Dispose();
            _sharedFurnitureArt = null;
            _sharedStonePropsArt = null;
            _sharedStorageArt?.Dispose();
            _sharedStorageArt = null;
            _grovelandsActorsArt?.Dispose();
            _grovelandsActorsArt = null;
            _incidentalEquipmentArt?.Dispose();
            _incidentalEquipmentArt = null;
            _harvestProductsArt?.Dispose();
            _harvestProductsArt = null;
            _aftermathArt?.Dispose();
            _aftermathArt = null;
            _orchardArt?.Dispose();
            _orchardArt = null;
            _riverShrineArt?.Dispose();
            _riverShrineArt = null;
            _nellaCornerArt?.Dispose();
            _nellaCornerArt = null;
            _eastFixtureArt?.Dispose();
            _eastFixtureArt = null;
            _kitchenBrineArt?.Dispose();
            _kitchenBrineArt = null;
            _hedgeArt?.Dispose();
            _hedgeArt = null;
            _sillEnvelopeArt?.Dispose();
            _spreadEnvironmentArt?.Dispose();
            _spreadSiteArt?.Dispose();
            _spreadSiteArt = null;
            _sillEnvelopeArt = null;
            _sillRiverArt?.Dispose();
            _sillRiverArt = null;
            _soddenTravelArt?.Dispose();
            _soddenSiteArt?.Dispose();
            _soddenSiteArt = null;
            _soddenTravelArt = null;
            _beatingMaterialsArt?.Dispose();
            _beatingSiteArt?.Dispose();
            _beatingSiteArt = null;
            _beatingMaterialsArt = null;
            _settlementGroundArt?.Dispose();
            _sinkholeLipArt?.Dispose();
            _sinkholeLipArt = null;
            _settlementGroundArt = null;
            _grovelandsMaterialsArt?.Dispose();
            _grovelandsSiteArt?.Dispose();
            _grovelandsSiteArt = null;
            _overwritMaterialsArt?.Dispose();
            _stumpMaterialsArt?.Dispose();
            _stumpSiteArt?.Dispose();
            _stumpSiteArt = null;
            _shallowCavesArt?.Dispose();
            _limestoneCavesArt?.Dispose();
            _shaleCavesArt?.Dispose();
            _slateCavesArt?.Dispose();
            _quartziteCavesArt?.Dispose();
            _obsidianCavesArt?.Dispose();
            _grovelandsMaterialsArt = null;
            _overwritMaterialsArt = null;
            _stumpMaterialsArt = null;
            _shallowCavesArt = null;
            _limestoneCavesArt = null;
            _shaleCavesArt = null;
            _slateCavesArt = null;
            _quartziteCavesArt = null;
            _obsidianCavesArt = null;
            _glyphFallback?.Dispose();
            _glyphFallback = null;
            foreach (Tile tile in _tiles) DestroyOwned(tile);
            foreach (Sprite sprite in _sprites) DestroyOwned(sprite);
            DestroyOwned(_atlas);
            _atlas = null;
        }

        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
