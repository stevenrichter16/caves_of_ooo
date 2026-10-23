using System;
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

    public enum FlatPixelGreyboxGround { Ground, Road, DrySoil, WetSoil, Water }

    /// <summary>Read-only presentation of one currently observed cell. Shapes are
    /// rough layout placeholders, not finished art or promises of new interactions.</summary>
    public readonly struct FlatPixelGreyboxCell
    {
        public readonly Tile GroundTile, ForegroundTile;
        public readonly Color GroundColor, ForegroundColor;
        public readonly Entity Owner;
        public readonly FlatPixelGreyboxKind Kind;
        public readonly FlatPixelGreyboxGround GroundKind;
        public readonly bool Remembered, BlocksMovement;

        internal FlatPixelGreyboxCell(Tile ground, Tile foreground, Color groundColor,
            Color foregroundColor, Entity owner, FlatPixelGreyboxKind kind,
            FlatPixelGreyboxGround groundKind, bool remembered, bool blocksMovement)
        {
            GroundTile = ground; ForegroundTile = foreground;
            GroundColor = groundColor; ForegroundColor = foregroundColor;
            Owner = owner; Kind = kind; GroundKind = groundKind;
            Remembered = remembered; BlocksMovement = blocksMovement;
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
        private Texture2D _atlas;
        private bool _disposed;

        public FlatPixelGreyboxCatalog() : this(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CastResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath),
            Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.StatesResourcePath)) { }

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
        {
            _openingArt = new FlatPixelOpeningArtCatalog(openingAtlas, castAtlas, anchorsAtlas, statesAtlas);
            int count = Enum.GetValues(typeof(FlatPixelGreyboxKind)).Length;
            _tiles = new Tile[count];
            _sprites = new Sprite[count];
            BuildAtlas();
        }

        /// <summary>Exact native opening plus its current content/biome authority.
        /// An unrelated zone at a similar address never acquires this treatment.</summary>
        public static bool IsAdmitted(Zone zone) => ReferenceGladePlan.IsActive(zone);

        /// <summary>Rejects unexplored, foreign and disposed contexts. Remembered
        /// cells expose only low-layer terrain, never current occupants, crop state,
        /// finite supplies, coatings or collision. Visible state is not cached.</summary>
        public bool TryResolve(Zone zone, int x, int y, out FlatPixelGreyboxCell result)
        {
            result = default;
            if (_disposed || !IsAdmitted(zone)) return false;
            Cell cell = zone.GetCell(x, y);
            if (cell == null || !cell.Explored) return false;
            bool remembered = !cell.IsVisible;
            Entity owner = remembered ? RememberedTerrain(zone, cell) : cell.GetTopVisibleObject();
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
            if (remembered)
            {
                // No independent light source or live-state read through memory.
                groundColor *= 0.35f;
                foregroundColor *= 0.35f;
                groundColor.a = foregroundColor.a = 1f;
            }
            result = new FlatPixelGreyboxCell(_tiles[(int)FlatPixelGreyboxKind.Ground], foreground,
                groundColor, foregroundColor, owner, kind, ground, remembered,
                !remembered && cell.BlocksMovement());
            return true;
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
                case "RoadStone": return FlatPixelGreyboxKind.Road;
                case "CropRow": return FlatPixelGreyboxKind.CropSpent;
                case "Reeds": case "Bush": case "MushroomRing": return FlatPixelGreyboxKind.Vegetation;
                case "Rubble": case "BrokenColumn": return FlatPixelGreyboxKind.Rubble;
            }
            if (owner.HasTag("Vegetation")) return FlatPixelGreyboxKind.Vegetation;
            if (owner.GetPart<PhysicsPart>()?.ParentEntity == owner) return FlatPixelGreyboxKind.Prop;
            return FlatPixelGreyboxKind.Unknown;
        }

        private static FlatPixelGreyboxGround GroundOf(Zone zone, Cell cell, Entity owner, bool remembered)
        {
            if (!remembered)
            {
                var coatings = zone.TileState.Get(cell.X, cell.Y)?.Coatings;
                if (coatings != null)
                    foreach (var coating in coatings)
                        if (coating.Id == "water" && coating.Turns > 0) return FlatPixelGreyboxGround.Water;
                var crop = owner?.GetPart<CropPart>();
                if (crop != null && crop.ParentEntity == owner)
                    return crop.MoistureTicks > 0 ? FlatPixelGreyboxGround.WetSoil : FlatPixelGreyboxGround.DrySoil;
                var field = owner?.GetPart<FieldHarvestPart>();
                if (field != null && field.ParentEntity == owner || owner?.BlueprintName == "CropRow"
                    || CultivatedSoilPart.IsCultivated(zone, cell))
                    return FlatPixelGreyboxGround.DrySoil;
            }
            // Road is ground geometry. No hidden crop/soil or liquid states are read.
            foreach (Entity entity in cell.Objects)
                if (entity.BlueprintName == "RoadStone" && entity.SpatialZone == zone
                    && zone.GetEntityCell(entity) == cell && entity.GetPart<RenderPart>()?.Visible == true)
                    return FlatPixelGreyboxGround.Road;
            return FlatPixelGreyboxGround.Ground;
        }

        private static Color GroundColor(FlatPixelGreyboxGround kind)
        {
            switch (kind)
            {
                case FlatPixelGreyboxGround.Road: return new Color32(49, 65, 45, 255);
                case FlatPixelGreyboxGround.DrySoil: return new Color32(69, 52, 31, 255);
                case FlatPixelGreyboxGround.WetSoil: return new Color32(37, 35, 25, 255);
                case FlatPixelGreyboxGround.Water: return new Color32(12, 67, 68, 255);
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _openingArt.Dispose();
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
