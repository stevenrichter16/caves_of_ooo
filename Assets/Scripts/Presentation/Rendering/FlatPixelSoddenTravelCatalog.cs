using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Visible static travel materials for the three actual Sodden
    /// district graphs. Borrows immutable textures and the current-owner selector;
    /// owns cached tiles/sprites only. No generation, state writes or admission expansion.</summary>
    public sealed class FlatPixelSoddenTravelCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/sodden-travel";
        const int Width = 16, Height = 24;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] travel, reeds, grass, stoneFloor, masonry;
        bool disposed;

        public FlatPixelSoddenTravelCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelCellarFloorCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CellarResourcePath), borrowedSelector) { }

        /// <summary>Each injected group is independent. Null/wrong-size groups
        /// never reload Resources. Borrowed dependencies are not changed or disposed.</summary>
        public FlatPixelSoddenTravelCatalog(Texture2D atlas28, Texture2D opening10,
            Texture2D sill24, Texture2D floors4, Texture2D cellar9, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null) return;
            travel = Frames(atlas28, 28, "travel");
            reeds = Frames(opening10, 10, "reeds");
            grass = Frames(sill24, 24, "grass");
            stoneFloor = Frames(floors4, 4, "floor");
            masonry = Frames(cellar9, 9, "stone");
        }

        public static bool IsSupportedZone(Zone zone)
        {
            if (zone == null || !SoddenDistrictPlan.IsSupportedZone(zone.ZoneID)) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            // The finite address whitelist is checked above. Read the current map
            // without repeatedly splitting an address for each rendered cell.
            int worldX = zone.ZoneID == SoddenDistrictPlan.StopZoneID ? 15
                : zone.ZoneID == SoddenDistrictPlan.CrossingZoneID ? 16 : 17;
            return map.GetBiome(worldX, 7) == BiomeType.Sodden && map.GetPOI(worldX, 7) == null;
        }

        public bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
        {
            tile = null;
            if (!ReadyCell(zone, x, y, out _) || !Selected(zone, x, y, owner)) return false;
            int frame = ForegroundFrame(owner, zone.TileState.Get(x, y), x, y);
            if (frame < 0) return false;
            if (owner.BlueprintName == "Reeds") tile = reeds == null ? null : reeds[frame];
            else if (owner.BlueprintName == "StoneWall") tile = masonry == null ? null : masonry[8];
            else if (travel != null)
            {
                if (owner.BlueprintName == "Duckboard")
                {
                    frame = 0;
                    if (BoardAt(zone, x, y - 1)) frame |= 1;
                    if (BoardAt(zone, x + 1, y)) frame |= 2;
                    if (BoardAt(zone, x, y + 1)) frame |= 4;
                    if (BoardAt(zone, x - 1, y)) frame |= 8;
                }
                tile = travel[frame];
            }
            return tile != null;
        }

        /// <summary>Only actual ordinary lower material. Live coating/hazard,
        /// cultivated soil or another disclosed ground owner outranks dry ground.</summary>
        public bool TryResolveGround(Zone zone, int x, int y, out Tile tile, out Entity floor)
        {
            tile = null; floor = null;
            if (!ReadyCell(zone, x, y, out var cell)) return false;
            var state = zone.TileState.Get(x, y);
            if (state != null && !state.IsEmpty) return false;
            // Owner-local cultivation is checked below with disclosed owned parts.
            // The gameplay helper ignores Render visibility and would leak hidden owners.
            Entity grassOwner = null, stoneOwner = null;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner)) continue;
                // Hidden/foreign/custodied owners cannot disclose themselves by
                // erasing visible ground. Independent actual tile state still wins.
                if (VisiblePhysicalOwner(owner) && CompetingGround(owner, state, x, y)) return false;
                if (owner.BlueprintName == "Grass")
                {
                    if (grassOwner != null || !OrdinaryFloor(owner, true)) return false;
                    grassOwner = owner;
                }
                else if (owner.BlueprintName == "StoneFloor")
                {
                    if (stoneOwner != null || !OrdinaryFloor(owner, false)) return false;
                    stoneOwner = owner;
                }
            }
            if (stoneOwner != null)
            { floor = stoneOwner; tile = stoneFloor == null ? null : stoneFloor[GroundVariantAt(x, y)]; }
            else if (grassOwner != null)
            { floor = grassOwner; tile = grass == null ? null : grass[20 + GroundVariantAt(x, y)]; }
            if (tile != null) return true;
            floor = null; return false;
        }

        /// <summary>Caller still authorizes the examine document. Only the same
        /// selected visible world subject can borrow this native image; board
        /// portraits use isolated frame0 rather than revealing neighboring geometry.</summary>
        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!ReadyCell(zone, x, y, out _) || !Selected(zone, x, y, owner)) return false;
            if (owner.BlueprintName == "Duckboard")
            {
                if (travel == null || ForegroundFrame(owner, zone.TileState.Get(x, y), x, y) < 0) return false;
                sprite = travel[0].sprite; return sprite != null;
            }
            if (TryResolveForeground(zone, x, y, owner, out var foreground))
            { sprite = foreground.sprite; return sprite != null; }
            if (TryResolveGround(zone, x, y, out var ground, out var floor) && floor == owner)
            { sprite = ground.sprite; return sprite != null; }
            return false;
        }

        /// <summary>Exact independent coating truth, including a depleted pool
        /// body which still owns a wet film. Mixtures, ice and expired layers refuse.</summary>
        internal static bool IsSoleLiveMire(ZoneTileState.TileState state)
            => state != null && state.Coatings.Count == 1
                && state.Coatings[0].Id == "bog-mire" && state.Coatings[0].Turns > 0;

        bool ReadyCell(Zone zone, int x, int y, out Cell cell)
        {
            cell = null;
            if (disposed || selector == null || !IsSupportedZone(zone)) return false;
            cell = zone.GetCell(x, y);
            return cell != null && cell.ParentZone == zone && cell.X == x && cell.Y == y
                && cell.Explored && cell.IsVisible;
        }
        bool Selected(Zone zone, int x, int y, Entity owner)
            => owner != null && selector.TryResolve(zone, x, y, out var selected)
                && !selected.Remembered && ReferenceEquals(selected.Owner, owner);
        bool BoardAt(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            return cell != null && cell.Explored && cell.IsVisible
                && selector.TryResolve(zone, x, y, out var selected) && !selected.Remembered
                && selected.Owner?.BlueprintName == "Duckboard"
                && ForegroundFrame(selected.Owner, zone.TileState.Get(x, y), x, y) == 0;
        }

        static int ForegroundFrame(Entity owner, ZoneTileState.TileState state, int x, int y)
        {
            if (!PlainOwner(owner) || HasForeignMeaning(owner, owner.BlueprintName == "MirePool")) return -1;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            switch (owner.BlueprintName)
            {
                case "Duckboard":
                    return EmptyVariants(render) && render.RenderString == "=" && owner.HasTag("Terrain")
                        && !owner.HasTag("Wall") && !owner.HasTag("Vegetation") && !owner.HasTag("Solid") && !physics.Solid
                        && Material(owner, "Wood") && Intact(owner) && NeutralThermal(owner) ? 0 : -1;
                case "MirePool":
                    var pool = Owned<LiquidPoolPart>(owner); var source = Owned<TileStateSourcePart>(owner);
                    return EmptyVariants(render) && render.RenderString == "~" && owner.HasTag("Terrain")
                        && !owner.HasTag("Wall") && !owner.HasTag("Vegetation") && !owner.HasTag("Solid") && !physics.Solid
                        && Material(owner, "Peat") && Intact(owner) && NeutralThermal(owner)
                        && pool != null && pool.LiquidId == "bog-mire" && pool.Volume >= 0
                        && source != null && source.EffectiveCoating == "bog-mire" && source.CoatingTurns > 0
                        && Owned<BurnOffGasPart>(owner) != null && IsSoleLiveMire(state)
                        ? MireFrameAt(x, y, pool.Volume == 0) : -1;
                case "PeatBank":
                    return EmptyVariants(render) && render.RenderString == "#" && owner.HasTag("Terrain")
                        && owner.HasTag("Wall") && owner.HasTag("Solid") && !owner.HasTag("Vegetation") && physics.Solid
                        && Material(owner, "Peat") && Intact(owner) && NeutralThermal(owner) ? 18 + ((x + y) & 1) : -1;
                case "DeadTree":
                    return EmptyVariants(render) && render.RenderString == "T" && owner.HasTag("Terrain")
                        && owner.HasTag("Vegetation") && owner.HasTag("Solid") && !owner.HasTag("Wall") && physics.Solid
                        && Material(owner, "Wood") && Intact(owner) && NeutralThermal(owner) ? 20 + ((x + y) & 1) : -1;
                case "Reeds":
                    return render.RenderString == "|" && render.GlyphVariants == "|!" && owner.HasTag("Terrain")
                        && owner.HasTag("Vegetation") && !owner.HasTag("Wall") && !owner.HasTag("Solid") && !physics.Solid
                        && !owner.HasPart<MaterialPart>() && !owner.HasPart<ThermalPart>() && !owner.HasPart<DestructiblePart>()
                        ? 3 + ((x + y) & 1) : -1;
                case "StoneWall":
                    return EmptyVariants(render) && render.RenderString == "#" && owner.HasTag("Wall")
                        && owner.HasTag("Solid") && !owner.HasTag("Vegetation") && physics.Solid
                        && Material(owner, "Stone") && Intact(owner) && NeutralThermal(owner) ? 8 : -1;
                default: return -1;
            }
        }
        static bool OrdinaryFloor(Entity owner, bool isGrass)
        {
            if (!PlainOwner(owner) || HasForeignMeaning(owner, false) || !owner.HasTag("Terrain")
                || owner.HasTag("Barren") || owner.HasTag("Wall") || owner.HasTag("Solid") || Owned<PhysicsPart>(owner).Solid
                || owner.HasPart<MaterialPart>() || owner.HasPart<ThermalPart>() || owner.HasPart<DestructiblePart>()) return false;
            var render = Owned<RenderPart>(owner);
            return render.RenderString == "." && render.GlyphVariants == "...,.'`"
                && (isGrass ? owner.HasTag("Plantable") && owner.HasTag("Vegetation") : !owner.HasTag("Vegetation"));
        }
        static bool PlainOwner(Entity owner)
        {
            if (owner == null || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (owner.HasPart<SpatialFootprintPart>())
            {
                var footprint = Owned<SpatialFootprintPart>(owner);
                if (footprint == null || footprint.Offsets.Length != 1 || footprint.Offsets[0] != Vector2Int.zero) return false;
            }
            // Native RGB is submitted white. Keep the existing event-driven tint
            // for any active effect that overrides its owner's color; no Render event here.
            if (owner.HasPart<StatusEffectsPart>())
            {
                var effects = Owned<StatusEffectsPart>(owner);
                if (effects == null || effects.GetRenderColorOverride() != null) return false;
            }
            return true;
        }
        static bool HasForeignMeaning(Entity owner, bool mire)
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<HarvestablePart>() || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>()
                || owner.HasPart<CampfirePart>() || owner.HasPart<TorchLightPart>() || owner.HasPart<AlchemyStillPart>()
                || owner.HasPart<SoddenPreparationPart>() || owner.HasPart<ContainerPart>()
                || owner.HasPart<TriggerOnStepPart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>()
                || !mire && (owner.HasPart<LiquidPoolPart>() || owner.HasPart<TileStateSourcePart>() || owner.HasPart<BurnOffGasPart>())
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
        static bool EmptyVariants(RenderPart render) => string.IsNullOrEmpty(render.GlyphVariants);
        static bool Material(Entity owner, string id) => Owned<MaterialPart>(owner)?.MaterialID == id;
        static bool Intact(Entity owner)
        { var structure = Owned<DestructiblePart>(owner); return structure != null && !structure.Gone && structure.HP > 0; }
        static bool NeutralThermal(Entity owner)
        {
            var thermal = Owned<ThermalPart>(owner);
            return thermal != null && float.IsFinite(thermal.Temperature) && float.IsFinite(thermal.FreezeTemperature)
                && float.IsFinite(thermal.FlameTemperature) && thermal.Temperature > thermal.FreezeTemperature
                && thermal.Temperature < thermal.FlameTemperature;
        }
        static bool Placed(Zone zone, Cell cell, Entity owner)
        {
            if (owner == null || owner.SpatialZone != zone) return false;
            var anchor = zone.GetEntityCell(owner);
            if (anchor == null || !anchor.Objects.Contains(owner)) return false;
            if (!owner.HasPart<SpatialFootprintPart>()) return anchor == cell;
            var footprint = Owned<SpatialFootprintPart>(owner);
            return footprint != null && zone.IsFootprintCurrent(owner)
                && SpatialQuery.DistanceToCell(zone, owner, cell.X, cell.Y) == 0;
        }
        static bool VisiblePhysicalOwner(Entity owner)
        {
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.InInventory != null || physics.Equipped != null) return false;
            if (!owner.HasPart<StackerPart>()) return true;
            var stack = Owned<StackerPart>(owner); return stack != null && stack.StackCount > 0;
        }
        static bool CompetingGround(Entity owner, ZoneTileState.TileState state, int x, int y)
            => Owned<CropPart>(owner) != null || Owned<FieldHarvestPart>(owner) != null || Owned<CultivatedSoilPart>(owner) != null
                || Owned<LiquidPoolPart>(owner) != null || Owned<GasPoolPart>(owner) != null || Owned<TileStateSourcePart>(owner) != null
                || Owned<TriggerOnStepPart>(owner) != null
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker")
                || owner.HasTag("Terrain") && !owner.HasTag("Wall")
                    && owner.BlueprintName != "Grass" && owner.BlueprintName != "StoneFloor"
                    && !NeutralDryOverlay(owner, state, x, y);
        static bool NeutralDryOverlay(Entity owner, ZoneTileState.TileState state, int x, int y)
            => (owner.BlueprintName == "Reeds" || owner.BlueprintName == "DeadTree" || owner.BlueprintName == "Duckboard")
                && ForegroundFrame(owner, state, x, y) >= 0;
        // Exact sole-owned semantic parts reject both foreign parents and duplicate
        // parts; borrowing the first GetPart result alone would accept ambiguity.
        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            if (owner == null) return null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part)
                { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        static int MireFrameAt(int x, int y, bool depleted)
        {
            int variation = GroundVariantAt(x, y);
            return variation == 0 ? (depleted ? 17 : 16) : (depleted ? 24 : 21) + variation;
        }
        static int GroundVariantAt(int x, int y)
        {
            uint value = unchecked((uint)x * 0x9e3779b1u ^ (uint)y * 0x85ebca77u);
            value ^= value >> 16; value = unchecked(value * 0x7feb352du); value ^= value >> 15;
            return (value & 15u) < 12u ? 0 : 1 + (int)((value >> 4) % 3u);
        }
        static Tile[] Frames(Texture2D texture, int count, string group)
        {
            if (texture == null || texture.width != count * Width || texture.height != Height) return null;
            var tiles = new Tile[count];
            for (int i = 0; i < count; i++)
            {
                var sprite = Sprite.Create(texture, new Rect(i * Width, 0, Width, Height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 Sodden " + group + " " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; tiles[i] = tile;
            }
            return tiles;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrames(travel); DestroyFrames(reeds); DestroyFrames(grass); DestroyFrames(stoneFloor); DestroyFrames(masonry);
        }
        static void DestroyFrames(Tile[] tiles)
        {
            if (tiles == null) return;
            foreach (var tile in tiles)
            { if (tile == null) continue; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
