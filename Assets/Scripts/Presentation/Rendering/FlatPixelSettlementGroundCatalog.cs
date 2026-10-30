using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Approved ground for ordinary authored settlements (2026-10-29). A seed-64 census found every
    /// authored Place except Morrowfast (its own 3D scene) and Sill (its own adapter) drawing almost
    /// entirely glyph ground in the flat 16x24 view, because each biome adapter admits wilderness
    /// only. This adapter reuses art that is already approved for the same identities:
    /// Grass → the Sill settlement lawn (Grovelands: the forest floor), StoneFloor → the Sill room
    /// floors, Sand → Beating sand, RoadStone → the opening road (Beating: the Beating road),
    /// and the plain town Floor → the original town-floor packed earth.
    /// Borrows textures and the current-owner selector; owns only cached tiles/sprites. Ground only:
    /// one plain dry owner per cell; wet, mixed, stacked, hidden, remembered or foreign cells keep
    /// their fallback. Also settlement pools (the Sill river water views) and SandstoneWall (the
    /// Beating wall frames, by the Beating's own rule). Never changes geometry, state or collision.
    /// </summary>
    public sealed class FlatPixelSettlementGroundCatalog : IDisposable
    {
        const int Width = 16, Height = 24;
        public const string TownFloorResourcePath = "Sprites/Live16x24/town-floor";
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] lawn, floors, forest, sand, beatingRoad, road, walls, yard;
        readonly FlatPixelSillRiverCatalog water;
        bool disposed;

        public FlatPixelSettlementGroundCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath),
                Resources.Load<Texture2D>(FlatPixelGrovelandsMaterialsCatalog.GroundResourcePath),
                Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath),
                Resources.Load<Texture2D>(TownFloorResourcePath), borrowedSelector) { }

        /// <summary>Each borrowed group is independent: a missing or malformed atlas only leaves
        /// its own identities on the fallback. No Resources reload on failure.</summary>
        public FlatPixelSettlementGroundCatalog(Texture2D sillEnvironment9, Texture2D forestGround4,
            Texture2D beating34, Texture2D openingGround21, Texture2D townFloor4, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null) return;
            lawn = Frames(sillEnvironment9, 9, 0, 4, "lawn");
            floors = Frames(sillEnvironment9, 9, 4, 4, "floor");
            forest = Frames(forestGround4, 4, 0, 4, "forest");
            sand = Frames(beating34, FlatPixelBeatingMaterialsCatalog.FrameCount, 0, 4, "sand");
            beatingRoad = Frames(beating34, FlatPixelBeatingMaterialsCatalog.FrameCount, 32, 2, "beating road");
            road = Frames(openingGround21, 21, 16, 2, "road");
            yard = Frames(townFloor4, 4, 0, 4, "town floor");
            walls = Frames(beating34, FlatPixelBeatingMaterialsCatalog.FrameCount, 16, 2, "sandstone wall");
            water = new FlatPixelSillRiverCatalog(openingGround21, selector, IsSupportedZone, WaterSubstrate);
        }

        /// <summary>The current cached surface graph of an authored Place other than Morrowfast and
        /// Sill, whose map POI is still that Village. Wilderness and special sites are not ours.</summary>
        public static bool IsSupportedZone(Zone zone)
        {
            if (zone?.ZoneID == null) return false;
            var (x, y, z) = WorldMap.FromZoneID(zone.ZoneID);
            if (x < 0 || z != 0) return false;
            var place = WorldMapAuthoring.PlaceAt(x, y);
            if (!place.HasValue || place.Value.Name == "Morrowfast" || place.Value.Name == "Sill") return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            var poi = map.GetPOI(x, y);
            return poi != null && poi.Type == POIType.Village && poi.Name == place.Value.Name
                && !LairStacks.HasSavedColumn(manager, zone.ZoneID);
        }

        /// <summary>Current disclosed dry ground of one plain owner, in its biome's approved art.</summary>
        public bool TryResolveGround(Zone zone, int x, int y, out Tile tile, out Entity floor)
        {
            tile = null; floor = null;
            if (!ReadyCell(zone, x, y, out var cell)) return false;
            var state = zone.TileState.Get(x, y);
            if (state != null && !state.IsEmpty) return false;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                if (IsGroundName(owner.BlueprintName))
                { if (floor != null || !OrdinaryGround(owner)) { floor = null; return false; } floor = owner; }
                else if (CompetingGround(owner)) { floor = null; return false; }
            }
            if (floor == null) return false;
            var (wx, wy, _) = WorldMap.FromZoneID(zone.ZoneID);
            var biome = WorldLocationContext.For(zone).WorldMap.GetBiome(wx, wy);
            switch (floor.BlueprintName)
            {
                case "Grass": tile = biome == BiomeType.Grovelands ? Pick(forest, VariantAt(x, y)) : Pick(lawn, VariantAt(x, y)); break;
                case "StoneFloor": tile = Pick(floors, VariantAt(x, y)); break;
                case "Floor": tile = Pick(yard, VariantAt(x, y)); break;
                case "Sand": tile = Pick(sand, VariantAt(x, y)); break;
                case "RoadStone": tile = biome == BiomeType.Beating ? Pick(beatingRoad, (x + y) & 1) : Pick(road, (x + y) & 1); break;
            }
            if (tile != null) return true;
            floor = null; return false;
        }

        /// <summary>Settlement pools reuse the Sill river's water views and pool rules: one real
        /// permanent-water WaterPuddle on plain Grass, Sand, StoneFloor or Floor. Bank edges follow
        /// disclosed dry neighbours; any second state or other terrain keeps the fallback.</summary>
        public bool TryResolveWaterGround(Zone zone, int x, int y, out Tile tile, out Entity source)
        {
            tile = null; source = null;
            return !disposed && water != null && water.TryResolveWaterGround(zone, x, y, out tile, out source);
        }

        /// <summary>A selected SandstoneWall in the approved Beating wall frames, by exactly the rule
        /// the Beating uses. Collision and the wall itself are unchanged.</summary>
        public bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
        {
            tile = null;
            if (walls == null || !ReadyCell(zone, x, y, out _) || owner == null
                || !selector.TryResolve(zone, x, y, out var picked) || picked.Remembered || !ReferenceEquals(picked.Owner, owner)) return false;
            int frame = FlatPixelBeatingMaterialsCatalog.SandstoneWallFrame(owner, x, y);
            tile = frame < 0 ? null : Pick(walls, frame - 16);
            return tile != null;
        }

        /// <summary>The reader may show the exact current ground owner, even beneath the player.</summary>
        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (owner == null) return false;
            if (owner.BlueprintName == "SandstoneWall")
            {
                if (!TryResolveForeground(zone, x, y, owner, out var wall)) return false;
                sprite = wall.sprite; return sprite != null;
            }
            if (owner.BlueprintName == "WaterPuddle") return water != null && water.TryGetSubjectSprite(zone, owner, x, y, out sprite);
            if (!IsGroundName(owner.BlueprintName)) return false;
            if (!TryResolveGround(zone, x, y, out var tile, out var floor) || !ReferenceEquals(floor, owner)) return false;
            sprite = tile.sprite; return sprite != null;
        }

        public static FlatPixelGreyboxGround GroundKind(Entity floor)
            => floor?.BlueprintName == "RoadStone" ? FlatPixelGreyboxGround.Road
                : floor?.BlueprintName == "StoneFloor" || floor?.BlueprintName == "Floor"
                    ? FlatPixelGreyboxGround.StoneFloor : FlatPixelGreyboxGround.Ground; // plain Floor: the greybox's own floor kind

        bool ReadyCell(Zone zone, int x, int y, out Cell cell)
        {
            cell = null;
            if (disposed || selector == null || !IsSupportedZone(zone)) return false;
            cell = zone.GetCell(x, y);
            return cell != null && cell.ParentZone == zone && cell.X == x && cell.Y == y && cell.Explored && cell.IsVisible
                && selector.TryResolve(zone, x, y, out var selected) && !selected.Remembered;
        }
        static Tile Pick(Tile[] tiles, int i) => tiles == null || i < 0 || i >= tiles.Length ? null : tiles[i];
        static bool IsGroundName(string name) => name == "Grass" || name == "StoneFloor" || name == "Sand" || name == "RoadStone" || name == "Floor";

        static bool OrdinaryGround(Entity owner)
        {
            if (!Plain(owner) || ForeignMeaning(owner) || !owner.HasTag("Terrain") || owner.HasTag("Wall") || owner.HasTag("Solid")
                || owner.HasTag("Barren") || Owned<PhysicsPart>(owner).Solid || owner.HasPart<MaterialPart>()
                || owner.HasPart<ThermalPart>() || owner.HasPart<DestructiblePart>()) return false;
            var r = Owned<RenderPart>(owner);
            if (r.RenderLayer != 0) return false;
            switch (owner.BlueprintName)
            {
                case "Grass":
                    return owner.HasTag("Vegetation") && owner.HasTag("Plantable") && r.RenderString == "." && r.ColorString == "&g"
                        && r.BackgroundColor == "^g" && r.GlyphVariants == "...,.'`";
                case "Sand":
                    return !owner.HasTag("Vegetation") && r.RenderString == "." && r.ColorString == "&W"
                        && r.BackgroundColor == "^w" && r.GlyphVariants == "...,.'`";
                case "StoneFloor":
                    return !owner.HasTag("Vegetation") && r.RenderString == "." && r.ColorString == "&y"
                        && string.IsNullOrEmpty(r.BackgroundColor) && r.GlyphVariants == "...,.'`";
                case "RoadStone":
                    return !owner.HasTag("Vegetation") && r.RenderString == "=" && r.ColorString == "&y"
                        && string.IsNullOrEmpty(r.BackgroundColor) && string.IsNullOrEmpty(r.GlyphVariants);
                case "Floor":
                    return !owner.HasTag("Vegetation") && r.RenderString == "." && r.ColorString == "&K"
                        && string.IsNullOrEmpty(r.BackgroundColor) && r.GlyphVariants == "...,.'`";
                default: return false;
            }
        }
        static bool WaterSubstrate(Entity owner)
            => FlatPixelSillRiverCatalog.OrdinaryGrass(owner)
                || owner.BlueprintName != "Grass" && owner.BlueprintName != "RoadStone" && OrdinaryGround(owner);
        static bool Plain(Entity owner)
        {
            if (owner == null || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.DetailColor) || !string.IsNullOrEmpty(render.TileColor)) return false;
            if (owner.HasPart<StackerPart>()) { var s = Owned<StackerPart>(owner); if (s == null || s.StackCount <= 0) return false; }
            if (owner.HasPart<StatusEffectsPart>())
            { var e = Owned<StatusEffectsPart>(owner); if (e == null || e.GetRenderColorOverride() != null) return false; }
            if (!owner.HasPart<SpatialFootprintPart>()) return true;
            var footprint = Owned<SpatialFootprintPart>(owner);
            return footprint != null && footprint.Offsets.Length == 1 && footprint.Offsets[0] == Vector2Int.zero;
        }
        static bool Placed(Zone zone, Cell cell, Entity owner)
        {
            if (owner == null || owner.SpatialZone != zone) return false;
            var anchor = zone.GetEntityCell(owner);
            if (anchor == null || !anchor.Objects.Contains(owner)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            return footprint == null ? anchor == cell : footprint.ParentEntity == owner && zone.IsFootprintCurrent(owner)
                && SpatialQuery.DistanceToCell(zone, owner, cell.X, cell.Y) == 0;
        }
        static bool VisiblePhysicalOwner(Entity owner)
        {
            var r = Owned<RenderPart>(owner); var p = Owned<PhysicsPart>(owner); var stack = owner.GetPart<StackerPart>();
            return r != null && r.Visible && p != null && p.InInventory == null && p.Equipped == null
                && (stack == null || stack.ParentEntity == owner && stack.StackCount > 0);
        }
        /// <summary>Other terrain, liquids, cultivation, markers and step triggers make the floor
        /// ambiguous; walls, props, actors and items stand on it and do not.</summary>
        static bool CompetingGround(Entity owner)
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasPart<TriggerOnStepPart>() || owner.HasPart<FiniteLiquidFilmPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker")
                || owner.HasTag("Terrain") && !owner.HasTag("Wall");
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>() || owner.HasPart<HarvestablePart>()
                || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>() || owner.HasPart<ContainerPart>() || owner.HasPart<BrainPart>() || owner.HasPart<Body>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<TileStateSourcePart>() || owner.HasPart<TriggerOnStepPart>()
                || owner.HasPart<WellPart>() || owner.HasPart<ForgePart>() || owner.HasPart<AlchemyStillPart>() || owner.HasPart<CampfirePart>() || owner.HasPart<TorchLightPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part) { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        /// <summary>Same quiet-majority variant rule as the Spread, Beating and Sill adapters.</summary>
        static int VariantAt(int x, int y)
        {
            uint value = unchecked((uint)x * 0x9e3779b1u ^ (uint)y * 0x85ebca77u);
            value ^= value >> 16; value = unchecked(value * 0x7feb352du); value ^= value >> 15;
            return (value & 15u) < 12u ? 0 : 1 + (int)((value >> 4) % 3u);
        }
        static Tile[] Frames(Texture2D texture, int atlasCount, int start, int count, string group)
        {
            if (texture == null || texture.width != atlasCount * Width || texture.height != Height) return null;
            var tiles = new Tile[count];
            for (int i = 0; i < count; i++)
            {
                var sprite = Sprite.Create(texture, new Rect((start + i) * Width, 0, Width, Height), new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 Settlement " + group + " " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; tiles[i] = tile;
            }
            return tiles;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            water?.Dispose();
            foreach (var group in new[] { lawn, floors, forest, sand, beatingRoad, road, walls, yard })
            {
                if (group == null) continue;
                foreach (var tile in group) { if (tile == null) continue; DestroyOwned(tile.sprite); DestroyOwned(tile); }
            }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
