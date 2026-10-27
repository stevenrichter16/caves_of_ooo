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
        /// <summary>Exact native foreground represents the sole live brine layer.</summary>
        public readonly bool RepresentedBrine;
        /// <summary>Submitted native MirePool body represents the sole live bog-mire layer; not brine/water.</summary>
        public readonly bool RepresentedMire;

        internal FlatPixelGreyboxCell(Tile ground, Tile foreground, Color groundColor,
            Color foregroundColor, Entity owner, FlatPixelGreyboxKind kind,
            FlatPixelGreyboxGround groundKind, bool remembered, bool blocksMovement, Tile groundDecoration = null,
            bool nativeForeground = false, bool localGround = false, bool representedBrine = false, bool representedMire = false)
        {
            GroundTile = ground; ForegroundTile = foreground; GroundDecorationTile = groundDecoration;
            GroundColor = groundColor; ForegroundColor = foregroundColor;
            Owner = owner; Kind = kind; GroundKind = groundKind;
            Remembered = remembered; BlocksMovement = blocksMovement;
            NativeForeground = nativeForeground; LocalGround = localGround; RepresentedBrine = representedBrine;
            RepresentedMire = representedMire;
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
        private FlatPixelIncidentalEquipmentCatalog _incidentalEquipmentArt;
        private FlatPixelAftermathCatalog _aftermathArt;
        private FlatPixelOrchardCatalog _orchardArt;
        private FlatPixelRiverShrineCatalog _riverShrineArt;
        private FlatPixelNellaCornerCatalog _nellaCornerArt;
        private FlatPixelEastFixtureCatalog _eastFixtureArt;
        private FlatPixelKitchenBrineCatalog _kitchenBrineArt;
        private FlatPixelHedgeCatalog _hedgeArt;
        private FlatPixelSillEnvelopeCatalog _sillEnvelopeArt;
        private FlatPixelSoddenTravelCatalog _soddenTravelArt;
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
            if (_openingArt.TryResolve(owner, x, y, out var openingTile))
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
                !remembered && cell.BlocksMovement(), remembered ? null : ReserveCord(zone, cell, out _));
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
            var incidental = local ? IncidentalActorArtFor(owner) : null;
            bool incidentalNative = incidental != null && incidental.TryResolve(owner, out art);
            var equipment = IncidentalEquipmentArtFor(owner);
            bool equipmentNative = equipment != null && equipment.TryResolve(owner, out art);
            var aftermath = local ? AftermathArtFor(owner) : null;
            bool aftermathNative = aftermath != null && aftermath.TryResolve(owner, out art);
            var orchard = local ? OrchardArtFor(owner) : null;
            bool orchardNative = orchard != null && orchard.TryResolve(owner, out art);
            var riverShrine = local ? RiverShrineArtFor(owner) : null;
            bool riverShrineNative = riverShrine != null && riverShrine.TryResolve(owner, out art);
            var nellaCorner = local && !fallback.Remembered ? NellaCornerArtFor(zone, owner) : null;
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
            Tile sillArt = null;
            bool sillNative = sill != null && sill.TryResolveForeground(zone, x, y, owner, out sillArt);
            if (sillNative) art = sillArt;
            // Separate output storage preserves earlier successful portable art
            // if a missing/malformed optional regional group refuses this owner.
            Tile soddenArt = null;
            bool soddenNative = sodden != null && sodden.TryResolveForeground(zone, x, y, owner, out soddenArt);
            if (soddenNative) art = soddenArt;
            bool mireNative = soddenNative && owner.BlueprintName == "MirePool";
            bool native = boundaryNative || eastNative || incidentalNative || equipmentNative || aftermathNative || orchardNative || riverShrineNative || nellaCornerNative || eastFixtureNative || brineNative || hedgeNative || sillNative || soddenNative
                || allowNativeFallback && allowHedgeFallback
                    && ((!local && _openingArt.TryResolvePortableLongSword(owner, out art))
                        || (local || FlatPixelActorMotionCatalog.IsPortableActor(owner))
                            && _openingArt.TryResolve(owner, x, y, out art));
            if (native) { foreground = art; foregroundColor = boundaryNative ? boundaryTint : Color.white; }
            else if (local && (kind == FlatPixelGreyboxKind.Ground || kind == FlatPixelGreyboxKind.Road)
                || sillGround && owner == sillFloor || soddenGround && owner == soddenFloor) foreground = null;
            if (fallback.Remembered)
            {
                if (local) { groundColor *= .35f; groundColor.a = 1; }
                // The boundary adapter already returns its conservative memory
                // tint. Other native groups retain this existing single dim.
                if (native && !boundaryNative) { foregroundColor *= .35f; foregroundColor.a = 1; }
            }
            if (local && !fallback.Remembered && owner?.BlueprintName == "MarlbackPatchbearer")
                ObserveSupply(zone, cell, owner, foreground, true);
            if (local && !fallback.Remembered && ServiceSubject(owner))
                ObserveService(zone, cell, owner, foreground);
            result = new FlatPixelGreyboxCell(groundTile,
                foreground, groundColor, foregroundColor, owner, kind, ground, fallback.Remembered,
                fallback.BlocksMovement, local && !fallback.Remembered ? ReserveCord(zone, cell, out _) : null, native, local || sillGround || soddenGround, brineNative, mireNative);
            return true;
        }

        /// <summary>Shared world/examine art policy. This does not authorize a
        /// subject: callers must validate the current world or carried document.</summary>
        public bool TryGetPresentationSubjectSprite(Zone zone, Entity subject, int x, int y, out Sprite sprite, out Color tint)
        {
            sprite = null; tint = Color.white;
            if (_disposed || !FlatPixelFallbackCatalog.IsCurrentGraph(zone)) return false;
            bool local = IsAdmitted(zone);
            var sill = SillEnvelopeArtFor(zone);
            if (sill != null && sill.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
            var sodden = SoddenTravelArtFor(zone);
            if (sodden != null && sodden.TryGetSubjectSprite(zone, subject, x, y, out sprite)) return true;
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
            var nellaCorner = local ? NellaCornerArtFor(zone, subject) : null;
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

        private FlatPixelSoddenTravelCatalog SoddenTravelArtFor(Zone zone)
        {
            if (!FlatPixelSoddenTravelCatalog.IsSupportedZone(zone)) return null;
            if (_soddenTravelArt == null && _loadOptionalResources)
            {
                if (_glyphFallback == null) _glyphFallback = new FlatPixelFallbackCatalog();
                _soddenTravelArt = new FlatPixelSoddenTravelCatalog(_glyphFallback);
            }
            return _soddenTravelArt;
        }

        private FlatPixelIncidentalActorCatalog IncidentalActorArtFor(Entity owner)
        {
            if (!FlatPixelIncidentalActorCatalog.HasKnownRole(owner)) return null;
            if (_incidentalActorArt == null && _loadOptionalResources)
                _incidentalActorArt = new FlatPixelIncidentalActorCatalog();
            return _incidentalActorArt;
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
            _incidentalEquipmentArt?.Dispose();
            _incidentalEquipmentArt = null;
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
            _sillEnvelopeArt = null;
            _soddenTravelArt?.Dispose();
            _soddenTravelArt = null;
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
