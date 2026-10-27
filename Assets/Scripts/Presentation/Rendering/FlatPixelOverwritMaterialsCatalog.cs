using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Four ordinary Overwrit owners. Borrows texture/selector and owns
    /// cached tiles/sprites only; does not generate, decorate or alter mechanics.</summary>
    public sealed class FlatPixelOverwritMaterialsCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/overwrit-materials";
        public const int FrameCount = 8;
        const int Width = 16, Height = 24;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] tiles;
        bool disposed;
        static readonly Dictionary<string, Vector2Int> coordinates = BuildCoordinates();

        static Dictionary<string, Vector2Int> BuildCoordinates()
        {
            var result = new Dictionary<string, Vector2Int>(StringComparer.Ordinal);
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    string id = WorldMap.ToZoneID(x, y);
                    if (OverwritCompositionPlan.IsWildernessZone(id)) result.Add(id, new Vector2Int(x, y));
                }
            return result;
        }
        public FlatPixelOverwritMaterialsCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath), borrowedSelector) { }

        /// <summary>A missing injected atlas stays missing. Never reload, modify
        /// or dispose borrowed resources.</summary>
        public FlatPixelOverwritMaterialsCatalog(Texture2D atlas, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector != null) tiles = Frames(atlas);
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
            return map.GetBiome(point.x, point.y) == BiomeType.Overwrit && map.GetPOI(point.x, point.y) == null
                && !LairStacks.HasSavedColumn(manager, zone.ZoneID);
        }
        public bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
        {
            tile = null;
            if (!ReadyCell(zone, x, y, out var cell) || !Selected(zone, x, y, owner)
                || !Placed(zone, cell, owner)) return false;
            int frame = ForegroundFrame(owner, x, y);
            if (frame < 0) return false;
            tile = tiles[frame]; return tile != null;
        }
        public bool TryResolveGround(Zone zone, int x, int y, out Tile tile, out Entity floor)
        {
            tile = null; floor = null;
            if (!ReadyCell(zone, x, y, out var cell)) return false;
            var state = zone.TileState.Get(x, y);
            if (state != null && !state.IsEmpty) return false;
            Entity actual = null;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                if (owner.BlueprintName == "OverwritGround")
                { if (actual != null || !OrdinaryGround(owner)) return false; actual = owner; }
                else if (CompetingGround(owner, x, y)) return false;
            }
            if (actual == null) return false;
            floor = actual; tile = tiles[VariantAt(x, y)]; return true;
        }
        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!ReadyCell(zone, x, y, out _) || !Selected(zone, x, y, owner)) return false;
            if (TryResolveForeground(zone, x, y, owner, out var foreground))
            { sprite = foreground.sprite; return sprite != null; }
            if (TryResolveGround(zone, x, y, out var ground, out var floor) && ReferenceEquals(floor, owner))
            { sprite = ground.sprite; return sprite != null; }
            return false;
        }
        bool ReadyCell(Zone zone, int x, int y, out Cell cell)
        {
            cell = null;
            if (disposed || selector == null || tiles == null || !IsSupportedZone(zone)) return false;
            cell = zone.GetCell(x, y);
            return cell != null && cell.ParentZone == zone && cell.X == x && cell.Y == y && cell.Explored && cell.IsVisible;
        }
        bool Selected(Zone zone, int x, int y, Entity owner)
            => owner != null && selector.TryResolve(zone, x, y, out var picked)
                && !picked.Remembered && ReferenceEquals(picked.Owner, owner);

        static bool OrdinaryGround(Entity owner)
        {
            if (!PlainOwner(owner) || HasForeignMeaning(owner) || !LowTerrain(owner)
                || owner.HasTag("Vegetation") || owner.HasTag("Plantable") || owner.HasTag("Barren")
                || owner.HasPart<MaterialPart>() || owner.HasPart<ThermalPart>() || owner.HasPart<DestructiblePart>()) return false;
            var render = Owned<RenderPart>(owner);
            return render.RenderString == "." && render.RenderLayer == 0 && render.ColorString == "&w";
        }
        static int ForegroundFrame(Entity owner, int x, int y)
        {
            if (!PlainOwner(owner) || HasForeignMeaning(owner)) return -1;
            var render = Owned<RenderPart>(owner);
            if (render.RenderLayer != 1) return -1;
            switch (owner.BlueprintName)
            {
                case "OverwritNewGrowth":
                    return render.RenderString == "," && render.ColorString == "&g" && LowTerrain(owner) && owner.HasTag("Vegetation")
                        && Material(owner, "Plant", "Organic", "Plant", .6f) && Intact(owner, 4, "") && NeutralThermal(owner)
                        ? 4 + (VariantAt(x, y) == 0 ? 0 : 1) : -1;
                case "OverwritPilgrimBench":
                    return render.RenderString == "_" && render.ColorString == "&y" && SolidObject(owner)
                        && Material(owner, "Wood", "Organic", "Wood", .6f) && Intact(owner, 12, "") && NeutralThermal(owner) ? 6 : -1;
                case "OverwritWaymarker":
                    return render.RenderString == "|" && render.ColorString == "&w" && SolidObject(owner)
                        && Material(owner, "Stone", "Stone", "Mineral", 0f) && Intact(owner, 24, "Rubble")
                        && !owner.HasPart<ThermalPart>() ? 7 : -1;
                default: return -1;
            }
        }
        static bool LowTerrain(Entity owner)
            => owner.HasTag("Terrain") && !owner.HasTag("Wall") && !owner.HasTag("Solid") && !Owned<PhysicsPart>(owner).Solid;
        static bool SolidObject(Entity owner)
            => !owner.HasTag("Terrain") && !owner.HasTag("Vegetation") && !owner.HasTag("Wall") && Owned<PhysicsPart>(owner).Solid;
        static bool PlainOwner(Entity owner)
        {
            if (owner == null || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")
                || owner.HasPart<BrainPart>() || owner.GetStat("Hitpoints") != null || Owned<ExaminablePart>(owner) == null) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants)) return false;
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
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<HarvestablePart>() || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>()
                || owner.HasPart<ContainerPart>() || owner.HasPart<ConversationPart>() || owner.HasPart<TraderPart>()
                || owner.HasPart("Restable") || owner.HasTag("Restable") || owner.HasPart<RegionalSignpostPart>()
                || owner.HasPart<CampfirePart>() || owner.HasPart<TorchLightPart>() || owner.HasPart<LightSourcePart>()
                || owner.HasPart<AlchemyStillPart>() || owner.HasPart<SoddenPreparationPart>() || owner.HasPart<TriggerOnStepPart>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<WellPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>() || owner.HasPart<BurnOffGasPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
        static bool Material(Entity owner, string id, string tag1, string tag2, float combustibility)
        {
            var part = Owned<MaterialPart>(owner);
            return part != null && part.MaterialID == id && float.IsFinite(part.Combustibility) && part.Combustibility == combustibility
                && part.HasMaterialTag(tag1) && part.HasMaterialTag(tag2) && owner.HasTag(tag1) && owner.HasTag(tag2);
        }
        static bool Intact(Entity owner, int max, string wreckage)
        {
            var part = Owned<DestructiblePart>(owner);
            return part != null && !part.Gone && !part.Indestructible && part.HP > 0 && part.HP <= part.MaxHP
                && part.MaxHP == max && part.Hardness >= 0 && (part.WreckageBlueprint ?? "") == wreckage;
        }
        static bool NeutralThermal(Entity owner)
        {
            var part = Owned<ThermalPart>(owner);
            return part != null && float.IsFinite(part.Temperature) && float.IsFinite(part.FreezeTemperature)
                && float.IsFinite(part.FlameTemperature) && part.Temperature > part.FreezeTemperature && part.Temperature < part.FlameTemperature;
        }
        static bool CompetingGround(Entity owner, int x, int y)
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasPart<TriggerOnStepPart>() || owner.HasPart<FiniteLiquidFilmPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker")
                || owner.HasTag("Terrain") && ForegroundFrame(owner, x, y) < 0;
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
        static Tile[] Frames(Texture2D texture)
        {
            if (texture == null || texture.width != FrameCount * Width || texture.height != Height) return null;
            var result = new Tile[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                var sprite = Sprite.Create(texture, new Rect(i * Width, 0, Width, Height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 Overwrit material " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; result[i] = tile;
            }
            return result;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (tiles == null) return;
            foreach (var tile in tiles)
            { if (tile == null) continue; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
