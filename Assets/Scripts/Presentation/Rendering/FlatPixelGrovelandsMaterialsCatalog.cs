using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Static material bodies for current ordinary Grovelands graphs.
    /// Borrows approved source textures and the visible-owner selector; owns only
    /// cached sprites/tiles. Never generates, changes stock or expands opening scope.</summary>
    public sealed class FlatPixelGrovelandsMaterialsCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/grovelands-materials";
        public const string GroundResourcePath = "Sprites/Live16x24/grovelands-ground";
        public const int FrameCount = 14;
        const int Width = 16, Height = 24;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] materials, sill, vegetation, opening, water, forest;
        bool disposed;
        static readonly Dictionary<string, Vector2Int> coordinates = BuildCoordinates();

        static Dictionary<string, Vector2Int> BuildCoordinates()
        {
            var result = new Dictionary<string, Vector2Int>(StringComparer.Ordinal);
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    string id = WorldMap.ToZoneID(x, y);
                    if (GrovelandsCompositionPlan.IsWildernessZone(id)) result.Add(id, new Vector2Int(x, y));
                }
            return result;
        }
        public FlatPixelGrovelandsMaterialsCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath), Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.VegetationResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath), borrowedSelector,
                Resources.Load<Texture2D>(GroundResourcePath)) { }

        /// <summary>Optional groups fail independently. Never reload injected
        /// textures, modify source pixels/settings, or dispose the borrowed selector.</summary>
        public FlatPixelGrovelandsMaterialsCatalog(Texture2D atlas14, Texture2D sill24,
            Texture2D vegetation4, Texture2D opening10, FlatPixelFallbackCatalog borrowedSelector)
            : this(atlas14, sill24, vegetation4, opening10, null, borrowedSelector) { }

        /// <summary>The independently optional opening-ground group supplies only
        /// ownerless permanent water. Older injected material groups do not opt in.</summary>
        public FlatPixelGrovelandsMaterialsCatalog(Texture2D atlas14, Texture2D sill24,
            Texture2D vegetation4, Texture2D opening10, Texture2D water21, FlatPixelFallbackCatalog borrowedSelector)
            : this(atlas14, sill24, vegetation4, opening10, water21, borrowedSelector, null) { }

        /// <summary>Regional forest floor is independently optional. Earlier
        /// injection contracts retain their borrowed Sill grass when omitted.</summary>
        public FlatPixelGrovelandsMaterialsCatalog(Texture2D atlas14, Texture2D sill24,
            Texture2D vegetation4, Texture2D opening10, Texture2D water21, FlatPixelFallbackCatalog borrowedSelector,
            Texture2D forest4)
        {
            selector = borrowedSelector;
            if (selector == null) return;
            materials = Frames(atlas14, FrameCount, "materials"); sill = Frames(sill24, 24, "forest");
            vegetation = Frames(vegetation4, 4, "tree"); opening = Frames(opening10, 10, "bush");
            water = Frames(water21, 21, "water");
            forest = Frames(forest4, 4, "forest floor");
        }
        public static bool IsSupportedZone(Zone zone)
        {
            if (zone?.ZoneID == null || !coordinates.TryGetValue(zone.ZoneID, out var point)) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            return map.GetBiome(point.x, point.y) == BiomeType.Grovelands && map.GetPOI(point.x, point.y) == null
                && !LairStacks.HasSavedColumn(manager, zone.ZoneID);
        }
        public bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
        {
            tile = null;
            if (!ReadyCell(zone, x, y, out var cell) || !Selected(zone, x, y, owner)) return false;
            int frame = ForegroundFrame(owner, zone, cell, x, y);
            if (frame < 0) return false;
            switch (owner.BlueprintName)
            {
                case "VineWall":
                    if (sill == null) return false;
                    frame = 0;
                    if (VineAt(zone, x, y - 1)) frame |= 1;
                    if (VineAt(zone, x + 1, y)) frame |= 2;
                    if (VineAt(zone, x, y + 1)) frame |= 4;
                    if (VineAt(zone, x - 1, y)) frame |= 8;
                    tile = sill[frame]; break;
                case "Tree": tile = vegetation == null ? null : vegetation[0]; break;
                case "Bush": tile = opening == null ? null : opening[5]; break;
                default: tile = materials == null ? null : materials[frame]; break;
            }
            return tile != null;
        }
        /// <summary>Only actual disclosed Grass supplies opaque ground. Exact
        /// neutral foreground plants and audited fixtures retain it. Fixture
        /// hazard marks remain on the independent tile-state layer.</summary>
        public bool TryResolveGround(Zone zone, int x, int y, out Tile tile, out Entity floor)
        {
            tile = null; floor = null;
            if (!ReadyCell(zone, x, y, out var cell)) return false;
            var state = zone.TileState.Get(x, y);
            bool fixture = false;
            Entity grass = null;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                if (owner.BlueprintName == "Grass")
                { if (grass != null || !OrdinaryGrass(owner)) return false; grass = owner; }
                else if (FlatPixelSpreadEnvironmentCatalog.IsEligibleFixture(owner)) fixture = true;
                else if (CompetingGround(owner, zone, cell, x, y)) return false;
            }
            // Represent actual Grass, never Water: live hazard/disclosure marks
            // stay independent, and residual state refuses after source removal.
            if (state != null && !state.IsEmpty && !fixture) return false;
            if (grass == null || forest == null && sill == null) return false;
            floor = grass; tile = forest != null ? forest[VariantAt(x, y)] : sill[20 + VariantAt(x, y)]; return true;
        }
        /// <summary>Represents a disclosed permanent water substrate, never a
        /// physical source or subject portrait. The optional grass output permits
        /// suppressing only that same neutral foreground owner, not a covering body.</summary>
        public bool TryResolveWaterGround(Zone zone, int x, int y, out Tile tile, out Entity representedGrass)
        {
            tile = null; representedGrass = null;
            if (water == null || !ReadyCell(zone, x, y, out var cell)
                || !selector.TryResolve(zone, x, y, out var selected) || selected.Remembered) return false;
            var coatings = zone.TileState.Get(x, y)?.Coatings;
            // Match the painter's exact one-entry rule; an expired extra is not
            // license to hide a mixed/unknown coating behind native water.
            if (coatings == null || coatings.Count != 1) return false;
            var coating = coatings[0];
            if (coating == null || coating.Id != "water" || coating.Turns != ZoneTileState.Permanent
                || !string.IsNullOrEmpty(coating.ProjectionOwnerId)) return false;
            Entity grass = null; int grasses = 0;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                // Ordinary pools also write unowned water. Their bodies and
                // source overlays remain separate from this Fen substrate pass.
                if (owner.HasPart<LiquidPoolPart>() || owner.HasPart<WellPart>() || owner.HasPart<TileStateSourcePart>()
                    || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>()) return false;
                if (owner.BlueprintName == "Grass")
                { grasses++; if (ReferenceEquals(selected.Owner, owner) && OrdinaryGrass(owner)) grass = owner; }
            }
            int mask = 0;
            if (DisclosedDryBank(zone, x, y - 1)) mask |= 1;
            if (DisclosedDryBank(zone, x + 1, y)) mask |= 2;
            if (DisclosedDryBank(zone, x, y + 1)) mask |= 4;
            if (DisclosedDryBank(zone, x - 1, y)) mask |= 8;
            tile = water[mask];
            if (tile == null) return false;
            if (grasses == 1) representedGrass = grass;
            return true;
        }
        bool DisclosedDryBank(Zone zone, int x, int y)
        {
            if (!ReadyCell(zone, x, y, out _) || !selector.TryResolve(zone, x, y, out var selected) || selected.Remembered) return false;
            var coatings = zone.TileState.Get(x, y)?.Coatings;
            return coatings == null || coatings.Count == 0;
        }

        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!ReadyCell(zone, x, y, out var cell) || !Selected(zone, x, y, owner)) return false;
            if (owner.BlueprintName == "VineWall")
            {
                if (sill == null || ForegroundFrame(owner, zone, cell, x, y) < 0) return false;
                sprite = sill[0].sprite; return sprite != null;
            }
            if (TryResolveForeground(zone, x, y, owner, out var foreground))
            { sprite = foreground.sprite; return sprite != null; }
            if (TryResolveGround(zone, x, y, out var ground, out var floor) && ReferenceEquals(floor, owner))
            { sprite = ground.sprite; return sprite != null; }
            return false;
        }
        bool ReadyCell(Zone zone, int x, int y, out Cell cell)
        {
            cell = null;
            if (disposed || selector == null || !IsSupportedZone(zone)) return false;
            cell = zone.GetCell(x, y);
            return cell != null && cell.ParentZone == zone && cell.X == x && cell.Y == y && cell.Explored && cell.IsVisible;
        }
        bool Selected(Zone zone, int x, int y, Entity owner)
            => !disposed && selector != null && owner != null && selector.TryResolve(zone, x, y, out var picked)
                && !picked.Remembered && ReferenceEquals(picked.Owner, owner);
        bool VineAt(Zone zone, int x, int y)
            => ReadyCell(zone, x, y, out var cell) && selector.TryResolve(zone, x, y, out var picked)
                && !picked.Remembered && picked.Owner?.BlueprintName == "VineWall"
                && ForegroundFrame(picked.Owner, zone, cell, x, y) >= 0;

        static int ForegroundFrame(Entity owner, Zone zone, Cell cell, int x, int y)
        {
            if (!PlainOwner(owner) || HasForeignMeaning(owner)) return -1;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (!string.IsNullOrEmpty(render.GlyphVariants)) return -1;
            switch (owner.BlueprintName)
            {
                case "MycelialColumn":
                    return render.RenderString == "O" && render.RenderLayer == 2 && TerrainPlant(owner, true)
                        && Material(owner, "Plant") && Intact(owner) && NeutralThermal(owner)
                        && Owned<LightSourcePart>(owner) != null ? VariantAt(x, y) == 0 ? 0 : 1 : -1;
                case "GroveSeep":
                    var pool = Owned<LiquidPoolPart>(owner); var well = Owned<WellPart>(owner);
                    return render.RenderString == "~" && render.RenderLayer == 1 && owner.HasTag("Terrain")
                        && !owner.HasTag("Vegetation") && !owner.HasTag("Wall") && !owner.HasTag("Solid") && !physics.Solid
                        && NoMaterialState(owner) && pool != null && pool.LiquidId == "water" && pool.Volume >= 0
                        && well != null && well.IsUsable && IsSoleLiveWater(zone.TileState.Get(x, y))
                        && UnmixedCell(cell) ? pool.Volume > 0 ? 2 : 3 : -1;
                case "GroveSign":
                    return render.RenderString == "I" && render.RenderLayer == 2 && physics.Solid && owner.HasTag("Furniture")
                        && !owner.HasTag("Terrain") && !owner.HasTag("Vegetation") && !owner.HasTag("Wall")
                        && NoMaterialState(owner) && Owned<ExaminablePart>(owner) != null ? 4 : -1;
                case "GroveRedGrowth":
                    return render.RenderString == ";" && render.RenderLayer == 1 && LowPlant(owner)
                        && NoMaterialState(owner) && Harvest(owner, "GroveRed", 2, 100) ? 5 : -1;
                case "FruitingBody":
                    return render.RenderString == "%" && render.RenderLayer == 2 && TerrainPlant(owner, true)
                        && Material(owner, "Plant") && Intact(owner) && NeutralThermal(owner) ? 6 + (VariantAt(x, y) == 0 ? 0 : 1) : -1;
                case "CompostRow":
                    return render.RenderString == "%" && render.RenderLayer == 1 && LowRemnant(owner)
                        && NoMaterialState(owner) ? 8 + (VariantAt(x, y) == 0 ? 0 : 1 + ((x + y) & 1)) : -1;
                case "CompostCache":
                    return render.RenderString == "%" && render.RenderLayer == 1 && LowRemnant(owner)
                        && NoMaterialState(owner) && Harvest(owner, "GoldCoin", 3, 60) ? 11 : -1;
                case "ChoirIronVein":
                    return render.RenderString == "*" && render.RenderLayer == 4 && physics.Solid && owner.HasTag("Solid")
                        && owner.HasTag("MineralVein") && !owner.HasTag("Terrain") && !owner.HasTag("Vegetation") && !owner.HasTag("Wall")
                        && NoMaterialState(owner) && Harvest(owner, "ChoirIron", 2, 100) ? 12 : -1;
                case "WineLeafSundew":
                    var snare = Owned<GreatdewSnarePart>(owner);
                    return render.RenderString == "v" && render.RenderLayer == 2 && LowPlant(owner)
                        && Material(owner, "Plant") && Intact(owner) && !owner.HasPart<ThermalPart>()
                        && snare != null && ReferenceEquals(snare, Owned<TriggerOnStepPart>(owner))
                        && !snare.ConsumeOnTrigger && string.IsNullOrEmpty(snare.TriggerFaction)
                        && snare.GrabChance == 45 && snare.HoldTurns == 3 && snare.Corrosion == 1.5f ? 13 : -1;
                case "VineWall":
                    return render.RenderString == "#" && render.RenderLayer == 0 && physics.Solid && owner.HasTag("Solid")
                        && owner.HasTag("Wall") && owner.HasTag("Vegetation") && Material(owner, "Plant")
                        && Intact(owner) && NeutralThermal(owner) ? 0 : -1;
                case "Tree":
                    return render.RenderString == "T" && render.RenderLayer == 1 && physics.Solid && owner.HasTag("Solid")
                        && owner.HasTag("Vegetation") && !owner.HasTag("Terrain") && !owner.HasTag("Wall")
                        && Material(owner, "Wood") && Intact(owner) && NeutralThermal(owner) ? 0 : -1;
                case "Bush":
                    return render.RenderString == ";" && render.RenderLayer == 0 && TerrainPlant(owner, false)
                        && Material(owner, "Plant") && Intact(owner) && NeutralThermal(owner) ? 5 : -1;
                default: return -1;
            }
        }
        static bool TerrainPlant(Entity owner, bool solid)
            => owner.HasTag("Terrain") && owner.HasTag("Vegetation") && !owner.HasTag("Wall")
                && owner.HasTag("Solid") == solid && Owned<PhysicsPart>(owner).Solid == solid;
        static bool LowPlant(Entity owner)
            => owner.HasTag("Vegetation") && !owner.HasTag("Terrain") && !owner.HasTag("Wall")
                && !owner.HasTag("Solid") && !Owned<PhysicsPart>(owner).Solid;
        static bool LowRemnant(Entity owner)
            => !owner.HasTag("Terrain") && !owner.HasTag("Vegetation") && !owner.HasTag("Wall")
                && !owner.HasTag("Solid") && !Owned<PhysicsPart>(owner).Solid;
        static bool OrdinaryGrass(Entity owner)
        {
            if (!PlainOwner(owner) || HasForeignMeaning(owner) || !TerrainPlant(owner, false)
                || !owner.HasTag("Plantable") || owner.HasTag("Barren") || !NoMaterialState(owner)) return false;
            var render = Owned<RenderPart>(owner);
            return render.RenderString == "." && render.RenderLayer == 0 && render.GlyphVariants == "...,.'`";
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
            if (owner.HasPart<StatusEffectsPart>())
            { var effects = Owned<StatusEffectsPart>(owner); if (effects == null || effects.GetRenderColorOverride() != null) return false; }
            return true;
        }
        static bool HasForeignMeaning(Entity owner)
        {
            bool harvest = owner.BlueprintName == "GroveRedGrowth" || owner.BlueprintName == "CompostCache" || owner.BlueprintName == "ChoirIronVein";
            bool seep = owner.BlueprintName == "GroveSeep", column = owner.BlueprintName == "MycelialColumn", sundew = owner.BlueprintName == "WineLeafSundew";
            return owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || !harvest && owner.HasPart<HarvestablePart>() || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>()
                || owner.HasPart<CampfirePart>() || owner.HasPart<TorchLightPart>() || owner.HasPart<AlchemyStillPart>()
                || owner.HasPart<SoddenPreparationPart>() || owner.HasPart<ContainerPart>() || owner.HasPart<RegionalSignpostPart>()
                || !sundew && owner.HasPart<TriggerOnStepPart>() || !column && owner.HasPart<LightSourcePart>()
                || !seep && (owner.HasPart<LiquidPoolPart>() || owner.HasPart<WellPart>())
                || owner.HasPart<TileStateSourcePart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>() || owner.HasPart<BurnOffGasPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
        }
        static bool Harvest(Entity owner, string product, int max, int chance)
        {
            var part = Owned<HarvestablePart>(owner);
            return part != null && !part.Harvested && part.YieldBlueprint == product && part.YieldMin == 1 && part.YieldMax == max && part.YieldChance == chance;
        }
        static bool NoMaterialState(Entity owner)
            => !owner.HasPart<MaterialPart>() && !owner.HasPart<ThermalPart>() && !owner.HasPart<DestructiblePart>();
        static bool Material(Entity owner, string id) => Owned<MaterialPart>(owner)?.MaterialID == id;
        static bool Intact(Entity owner)
        { var part = Owned<DestructiblePart>(owner); return part != null && !part.Gone && part.HP > 0; }
        static bool NeutralThermal(Entity owner)
        {
            var part = Owned<ThermalPart>(owner);
            return part != null && float.IsFinite(part.Temperature) && float.IsFinite(part.FreezeTemperature)
                && float.IsFinite(part.FlameTemperature) && part.Temperature > part.FreezeTemperature && part.Temperature < part.FlameTemperature;
        }
        static bool IsSoleLiveWater(ZoneTileState.TileState state)
        {
            if (state?.Coatings == null) return false;
            int count = 0;
            for (int i = 0; i < state.Coatings.Count; i++)
            {
                var coating = state.Coatings[i]; if (coating.Turns <= 0) continue;
                if (coating.Id != "water" || ++count > 1) return false;
            }
            return count == 1;
        }
        // Mirrors the existing service's material safety without its positive-stock
        // requirement or LINQ closure. An empty source still owns permanent water.
        static bool UnmixedCell(Cell cell)
        {
            foreach (var other in cell.Occupants)
            {
                var pool = other.GetPart<LiquidPoolPart>();
                if (pool != null && (pool.Volume < 0 || pool.Volume > 0 && pool.LiquidId != "water")) return false;
                var source = other.GetPart<TileStateSourcePart>();
                if (source != null && source.CoatingTurns > 0 && !string.IsNullOrEmpty(source.EffectiveCoating) && source.EffectiveCoating != "water") return false;
            }
            return true;
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
        static bool CompetingGround(Entity owner, Zone zone, Cell cell, int x, int y)
        {
            if (owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker")) return true;
            bool namedDryOverlay = ForegroundFrame(owner, zone, cell, x, y) >= 0;
            return owner.HasPart<TriggerOnStepPart>() && !namedDryOverlay
                || owner.HasTag("Terrain") && owner.BlueprintName != "Grass" && !namedDryOverlay;
        }
        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            if (owner == null) return null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part)
                { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        static int VariantAt(int x, int y)
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
                sprite.name = "Live16x24 Grovelands " + group + " " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; tiles[i] = tile;
            }
            return tiles;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrames(materials); DestroyFrames(sill); DestroyFrames(vegetation); DestroyFrames(opening); DestroyFrames(water);
            DestroyFrames(forest);
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
