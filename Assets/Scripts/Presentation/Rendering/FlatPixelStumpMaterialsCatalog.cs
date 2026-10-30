using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Current ordinary Stump materials. Owns cached sprites/tiles,
    /// borrows textures and selector, and never generates or changes the world.</summary>
    public sealed class FlatPixelStumpMaterialsCatalog : IDisposable
    {
        // Wilderness by default; a separate instance may admit its biome's ordinary special sites.
        System.Func<Zone, bool> admits = IsSupportedZone;
        /// <summary>Make this instance draw its biome's ordinary sinkhole surfaces
        /// (FlatPixelSiteAdmission) instead of the wilderness, with exactly the same rules and art.</summary>
        internal FlatPixelStumpMaterialsCatalog AdmittingSites(BiomeType biome)
        { admits = zone => FlatPixelSiteAdmission.IsOrdinarySite(zone, biome); return this; }

        public const string ResourcePath = "Sprites/Live16x24/stump-materials";
        public const int FrameCount = 71;
        const int Width = 16, Height = 24;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] materials, vegetation, opening;
        bool disposed;
        static readonly Dictionary<string, Vector2Int> coordinates = BuildCoordinates();

        static Dictionary<string, Vector2Int> BuildCoordinates()
        {
            var result = new Dictionary<string, Vector2Int>(StringComparer.Ordinal);
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    string id = WorldMap.ToZoneID(x, y);
                    if (StumpCompositionPlan.IsWildernessZone(id)) result.Add(id, new Vector2Int(x, y));
                }
            return result;
        }
        public FlatPixelStumpMaterialsCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.VegetationResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath), borrowedSelector) { }

        /// <summary>Optional groups fail independently. Explicit null injection
        /// never reloads a resource or disables another valid borrowed group.</summary>
        public FlatPixelStumpMaterialsCatalog(Texture2D atlas71, Texture2D vegetation4,
            Texture2D opening10, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null) return;
            materials = Frames(atlas71, FrameCount, "materials");
            vegetation = Frames(vegetation4, 4, "tree"); opening = Frames(opening10, 10, "bush");
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
            return map.GetBiome(point.x, point.y) == BiomeType.Stump && map.GetPOI(point.x, point.y) == null
                && !LairStacks.HasSavedColumn(manager, zone.ZoneID);
        }
        public bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
        {
            tile = null;
            if (!ReadyCell(zone, x, y, out var cell) || !Selected(zone, x, y, owner) || !Placed(zone, cell, owner)) return false;
            int frame = DryFrame(owner, x, y);
            if (frame < 0) return false;
            var group = owner.BlueprintName == "Tree" ? vegetation : owner.BlueprintName == "Bush" ? opening : materials;
            if (group == null) return false;
            tile = group[PerimeterFrame(zone, owner.BlueprintName, x, y, frame)]; return true;
        }
        // Exposed edges, not connection bits. Zone north is screen top (y - 1).
        // A fully joined mass retains its original quiet interior variant.
        int PerimeterFrame(Zone zone, string family, int x, int y, int interior)
        {
            int first = family == "TepuiWall" ? 26 : family == "GrainRidge" ? 41 : family == "StoneDome" ? 56 : -1;
            if (first < 0) return interior;
            int mask = 0;
            if (!JoinsSolid(zone, family, x, y - 1)) mask |= 1;
            if (!JoinsSolid(zone, family, x + 1, y)) mask |= 2;
            if (!JoinsSolid(zone, family, x, y + 1)) mask |= 4;
            if (!JoinsSolid(zone, family, x - 1, y)) mask |= 8;
            return mask == 0 ? interior : first + mask - 1;
        }
        bool JoinsSolid(Zone zone, string family, int x, int y)
        {
            // The center already proved current graph/scope. Never inspect a
            // remembered/hidden neighbor's owner or recursively resolve art.
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.ParentZone != zone || cell.X != x || cell.Y != y
                || !cell.Explored || !cell.IsVisible
                || !selector.TryResolve(zone, x, y, out var selected) || selected.Remembered) return false;
            var owner = selected.Owner;
            return owner != null && owner.BlueprintName == family && Placed(zone, cell, owner)
                && DryFrame(owner, x, y) >= 0;
        }
        /// <summary>Actual neutral TepuiStone only. Crops, cultivation, unfamiliar
        /// terrain and independent tile state keep their semantic fallback.</summary>
        public bool TryResolveGround(Zone zone, int x, int y, out Tile tile, out Entity floor)
        {
            tile = null; floor = null;
            if (materials == null || !ReadyCell(zone, x, y, out var cell)) return false;
            var state = zone.TileState.Get(x, y);
            if (state != null && !state.IsEmpty) return false;
            Entity actual = null;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                if (owner.BlueprintName == "TepuiStone")
                { if (actual != null || !OrdinaryFloor(owner)) return false; actual = owner; }
                else if (CompetingGround(owner, x, y)) return false;
            }
            if (actual == null) return false;
            floor = actual; tile = materials[VariantAt(x, y)]; return true;
        }
        /// <summary>Represents the exact disclosed natural pool and its permanent
        /// water, including zero draw stock. A covering actor keeps its own body;
        /// the caller may suppress only the returned source's selected glyph.
        /// Independent heat/charge/cloud/residue marks remain the painter's job.</summary>
        public bool TryResolveWaterGround(Zone zone, int x, int y, out Tile tile, out Entity source)
        {
            tile = null; source = null;
            if (materials == null || !ReadyCell(zone, x, y, out var cell)
                || !selector.TryResolve(zone, x, y, out var selected) || selected.Remembered
                || !PermanentWater(zone.TileState.Get(x, y))) return false;
            Entity actual = null; int frame = -1;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                if (owner.BlueprintName == "SprayPool")
                {
                    if (actual != null) return false;
                    frame = PoolFrame(owner, x, y);
                    if (frame < 0) return false;
                    actual = owner;
                }
                else if (owner.BlueprintName == "TepuiStone")
                { if (!OrdinaryFloor(owner)) return false; }
                else if (owner.HasPart<WellPart>() || CompetingGround(owner, x, y)) return false;
            }
            if (actual == null) return false;
            source = actual; tile = materials[frame]; return true;
        }
        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!ReadyCell(zone, x, y, out _) || !Selected(zone, x, y, owner)) return false;
            if (TryResolveForeground(zone, x, y, owner, out var foreground))
            { sprite = foreground.sprite; return sprite != null; }
            if (owner.BlueprintName == "SprayPool")
            {
                if (TryResolveWaterGround(zone, x, y, out var water, out var source) && ReferenceEquals(source, owner))
                { sprite = water.sprite; return sprite != null; }
                return false;
            }
            if (TryResolveGround(zone, x, y, out var ground, out var floor) && ReferenceEquals(floor, owner))
            { sprite = ground.sprite; return sprite != null; }
            return false;
        }
        bool ReadyCell(Zone zone, int x, int y, out Cell cell)
        {
            cell = null;
            if (disposed || selector == null || !admits(zone)) return false;
            cell = zone.GetCell(x, y);
            return cell != null && cell.ParentZone == zone && cell.X == x && cell.Y == y && cell.Explored && cell.IsVisible;
        }
        bool Selected(Zone zone, int x, int y, Entity owner)
            => owner != null && selector.TryResolve(zone, x, y, out var picked)
                && !picked.Remembered && ReferenceEquals(picked.Owner, owner);

        static bool OrdinaryFloor(Entity owner)
        {
            if (!PlainOwner(owner) || ForeignMeaning(owner) || !Terrain(owner, false, false) || !NoMaterialState(owner)) return false;
            var render = Owned<RenderPart>(owner);
            return render.RenderString == "." && render.ColorString == "&y" && render.RenderLayer == 0
                && render.GlyphVariants == "...,.'`";
        }
        static int DryFrame(Entity owner, int x, int y)
        {
            if (!PlainOwner(owner) || ForeignMeaning(owner)) return -1;
            var render = Owned<RenderPart>(owner);
            if (!string.IsNullOrEmpty(render.GlyphVariants)) return -1;
            int pair = VariantAt(x, y) == 0 ? 0 : 1;
            switch (owner.BlueprintName)
            {
                case "TepuiWall":
                    return Render(render, "#", "&y", 2) && render.BackgroundColor == "^k"
                        && SolidObject(owner) && owner.HasTag("Wall") && Material(owner, "Stone", 0f, "Stone", "Mineral")
                        && Intact(owner, 40, 3, "Rubble") && NeutralThermal(owner) ? 4 + pair : -1;
                case "GrainRidge":
                    return Render(render, "=", "&y", 2) && Terrain(owner, true, false) && NoMaterialState(owner)
                        ? 6 + VariantAt(x, y) : -1;
                case "DescentLedge":
                    return Render(render, "=", "&y", 1) && !owner.HasTag("Terrain") && !owner.HasTag("Vegetation")
                        && !owner.HasTag("Wall") && !owner.HasTag("Solid") && !Owned<PhysicsPart>(owner).Solid
                        && NoMaterialState(owner) ? 10 + pair : -1;
                case "StoneDome":
                    return Render(render, "n", "&y", 2) && Terrain(owner, true, false) && NoMaterialState(owner) ? 20 + pair : -1;
                case "TankBrocchinia":
                    var well = Owned<WellPart>(owner);
                    return Render(render, "u", "&G", 2) && Terrain(owner, false, true) && NoMaterialState(owner)
                        && well != null && well.IsUsable ? 22 + pair : -1;
                case "TepuiboneVein":
                    var harvest = Owned<HarvestablePart>(owner);
                    return Render(render, "*", "&W", 4) && SolidObject(owner) && !owner.HasTag("Wall")
                        && owner.HasTag("MineralVein") && NoMaterialState(owner) && harvest != null && !harvest.Harvested
                        && harvest.YieldBlueprint == "Tepuibone" && harvest.YieldMin == 1 && harvest.YieldMax == 2
                        && harvest.YieldChance == 100 ? 24 + pair : -1;
                case "Tree":
                    return Render(render, "T", "&G", 1) && !owner.HasTag("Terrain") && !owner.HasTag("Wall")
                        && owner.HasTag("Solid") && owner.HasTag("Vegetation") && Owned<PhysicsPart>(owner).Solid
                        && Material(owner, "Wood", .45f, "Organic", "Plant", "Wood")
                        && Intact(owner, 30, 1, "") && NeutralThermal(owner) ? 0 : -1;
                case "Bush":
                    return Render(render, ";", "&G", 0) && Terrain(owner, false, true)
                        && Material(owner, "Plant", .6f, "Organic", "Plant")
                        && Intact(owner, 6, 0, "") && NeutralThermal(owner) ? 5 : -1;
                default: return -1;
            }
        }
        static int PoolFrame(Entity owner, int x, int y)
        {
            if (!PlainOwner(owner) || ForeignMeaning(owner) || !Terrain(owner, false, false)) return -1;
            var render = Owned<RenderPart>(owner); var pool = Owned<LiquidPoolPart>(owner);
            return Render(render, "~", "&c", 1) && string.IsNullOrEmpty(render.GlyphVariants)
                && Material(owner, "Water", 0f) && !owner.HasPart<ThermalPart>() && !owner.HasPart<DestructiblePart>()
                && pool != null && pool.LiquidId == "water" && pool.Volume >= 0
                ? (pool.Volume > 0 ? 12 : 16) + VariantAt(x, y) : -1;
        }
        static bool Render(RenderPart render, string glyph, string color, int layer)
            => render.RenderString == glyph && render.ColorString == color && render.RenderLayer == layer;
        static bool Terrain(Entity owner, bool solid, bool vegetation)
            => owner.HasTag("Terrain") && !owner.HasTag("Wall") && owner.HasTag("Vegetation") == vegetation
                && owner.HasTag("Solid") == solid && Owned<PhysicsPart>(owner).Solid == solid;
        static bool SolidObject(Entity owner)
            => !owner.HasTag("Terrain") && !owner.HasTag("Vegetation") && owner.HasTag("Solid") && Owned<PhysicsPart>(owner).Solid;
        static bool PlainOwner(Entity owner)
        {
            if (owner == null || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")
                || owner.HasTag("Plantable") || owner.HasTag("Barren") || owner.HasPart<BrainPart>()
                || owner.GetStat("Hitpoints") != null || Owned<ExaminablePart>(owner) == null) return false;
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
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.BlueprintName != "TepuiboneVein" && owner.HasPart<HarvestablePart>()
                || owner.BlueprintName != "SprayPool" && owner.HasPart<LiquidPoolPart>()
                || owner.BlueprintName != "TankBrocchinia" && owner.HasPart<WellPart>()
                || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>() || owner.HasPart<ContainerPart>()
                || owner.HasPart<ConversationPart>() || owner.HasPart<TraderPart>()
                || owner.HasPart("Restable") || owner.HasTag("Restable") || owner.HasPart<RegionalSignpostPart>()
                || owner.HasPart<CampfirePart>() || owner.HasPart<TorchLightPart>() || owner.HasPart<LightSourcePart>()
                || owner.HasPart<AlchemyStillPart>() || owner.HasPart<SoddenPreparationPart>() || owner.HasPart<TriggerOnStepPart>()
                || owner.HasPart<StairsDownPart>() || owner.HasPart<StairsUpPart>()
                || owner.HasPart<TileStateSourcePart>() || owner.HasPart<GasPoolPart>()
                || owner.HasPart<FiniteLiquidFilmPart>() || owner.HasPart<BurnOffGasPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
        static bool NoMaterialState(Entity owner)
            => !owner.HasPart<MaterialPart>() && !owner.HasPart<ThermalPart>() && !owner.HasPart<DestructiblePart>();
        static bool Material(Entity owner, string id, float combustibility, string tag1 = null, string tag2 = null, string tag3 = null)
        {
            var part = Owned<MaterialPart>(owner);
            return part != null && part.MaterialID == id && float.IsFinite(part.Combustibility) && part.Combustibility == combustibility
                && (tag1 == null || part.HasMaterialTag(tag1) && owner.HasTag(tag1))
                && (tag2 == null || part.HasMaterialTag(tag2) && owner.HasTag(tag2))
                && (tag3 == null || part.HasMaterialTag(tag3) && owner.HasTag(tag3));
        }
        static bool Intact(Entity owner, int max, int hardness, string wreckage)
        {
            var part = Owned<DestructiblePart>(owner);
            return part != null && !part.Gone && !part.Indestructible && part.HP > 0 && part.HP <= part.MaxHP
                && part.MaxHP == max && part.Hardness == hardness && (part.WreckageBlueprint ?? "") == wreckage;
        }
        static bool NeutralThermal(Entity owner)
        {
            var part = Owned<ThermalPart>(owner);
            return part != null && float.IsFinite(part.Temperature) && float.IsFinite(part.FreezeTemperature)
                && float.IsFinite(part.FlameTemperature) && part.Temperature > part.FreezeTemperature && part.Temperature < part.FlameTemperature;
        }
        static bool PermanentWater(ZoneTileState.TileState state)
        {
            if (state?.Coatings == null || state.Coatings.Count != 1) return false;
            var coating = state.Coatings[0];
            return coating != null && coating.Id == "water" && coating.Turns == ZoneTileState.Permanent
                && string.IsNullOrEmpty(coating.ProjectionOwnerId);
        }
        static bool CompetingGround(Entity owner, int x, int y)
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasPart<TriggerOnStepPart>() || owner.HasPart<FiniteLiquidFilmPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker")
                || owner.HasTag("Terrain") && DryFrame(owner, x, y) < 0;
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
                sprite.name = "Live16x24 Stump " + group + " " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; tiles[i] = tile;
            }
            return tiles;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrames(materials); DestroyFrames(vegetation); DestroyFrames(opening);
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
