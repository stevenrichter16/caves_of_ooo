using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Visible ordinary Sill river substrate. Borrows the unchanged
    /// opening-ground atlas and selector; owns only sixteen cached water views.
    /// No remembered pool reads, simulation events or material mutations.</summary>
    public sealed class FlatPixelSillRiverCatalog : IDisposable
    {
        public const string ResourcePath = FlatPixelOpeningGroundCatalog.ResourcePath;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] water;
        bool disposed;

        public FlatPixelSillRiverCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath), borrowedSelector) { }

        /// <summary>Full approved 336x24 atlas; only frames0–15 are borrowed.
        /// Null/malformed input stays unavailable without a resource reload.
        /// Texture and selector remain caller-owned.</summary>
        public FlatPixelSillRiverCatalog(Texture2D openingGround21, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null || openingGround21 == null || openingGround21.width != 336 || openingGround21.height != 24) return;
            water = new Tile[16];
            for (int i = 0; i < water.Length; i++)
            {
                var sprite = Sprite.Create(openingGround21, new Rect(i * 16, 0, 16, 24),
                    new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 Sill river " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; water[i] = tile;
            }
        }

        public static bool IsSupportedZone(Zone zone) => FlatPixelSillEnvelopeCatalog.IsSupportedZone(zone);

        /// <summary>Returns the unique real positive-volume water source beneath
        /// a selected actor/item or ordinary grass. Only its selected glyph and
        /// sole water mark may be suppressed; independent hazards remain visible.</summary>
        public bool TryResolveWaterGround(Zone zone, int x, int y, out Tile tile, out Entity source)
        {
            tile = null; source = null;
            if (water == null || !ReadyCell(zone, x, y, out var cell)
                || !PermanentWater(zone.TileState.Get(x, y))) return false;
            Entity actual = null;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                if (owner.BlueprintName == "WaterPuddle")
                { if (actual != null || !OrdinaryPool(owner)) return false; actual = owner; }
                else if (owner.BlueprintName == "Grass")
                { if (!OrdinaryGrass(owner)) return false; }
                else if (CompetingGround(owner)) return false;
            }
            if (actual == null) return false;
            // The original atlas encodes exposed DRY banks, not water connections.
            // Unknown/remembered neighbors contribute no bank and expose no state.
            int mask = 0;
            if (DisclosedDry(zone, x, y - 1)) mask |= 1;
            if (DisclosedDry(zone, x + 1, y)) mask |= 2;
            if (DisclosedDry(zone, x, y + 1)) mask |= 4;
            if (DisclosedDry(zone, x - 1, y)) mask |= 8;
            source = actual; tile = water[mask]; return true;
        }

        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (owner == null || !ReadyCell(zone, x, y, out _)
                || !selector.TryResolve(zone, x, y, out var picked) || picked.Remembered || !ReferenceEquals(picked.Owner, owner)) return false;
            if (!TryResolveWaterGround(zone, x, y, out var tile, out var source) || !ReferenceEquals(source, owner)) return false;
            sprite = tile.sprite; return sprite != null;
        }

        bool ReadyCell(Zone zone, int x, int y, out Cell cell)
        {
            cell = null;
            if (disposed || selector == null || !IsSupportedZone(zone)) return false;
            cell = zone.GetCell(x, y);
            return cell != null && cell.ParentZone == zone && cell.X == x && cell.Y == y && cell.Explored && cell.IsVisible
                && selector.TryResolve(zone, x, y, out var selected) && !selected.Remembered;
        }
        bool DisclosedDry(Zone zone, int x, int y)
        {
            // The center proved current Sill authority; visibility precedes live state.
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.ParentZone != zone || cell.X != x || cell.Y != y || !cell.Explored || !cell.IsVisible
                || !selector.TryResolve(zone, x, y, out var picked) || picked.Remembered) return false;
            var coatings = zone.TileState.Get(x, y)?.Coatings;
            return coatings == null || coatings.Count == 0;
        }
        static bool PermanentWater(ZoneTileState.TileState state)
        {
            if (state?.Coatings == null || state.Coatings.Count != 1) return false;
            var coating = state.Coatings[0];
            return coating != null && coating.Id == "water" && coating.Turns == ZoneTileState.Permanent
                && string.IsNullOrEmpty(coating.ProjectionOwnerId);
        }
        static bool OrdinaryPool(Entity owner)
        {
            if (!PlainTerrain(owner) || ForeignMeaning(owner) || owner.HasTag("Vegetation") || owner.HasTag("Plantable")
                || owner.HasPart<DestructiblePart>()) return false;
            var render = Owned<RenderPart>(owner); var material = Owned<MaterialPart>(owner);
            var thermal = Owned<ThermalPart>(owner); var pool = Owned<LiquidPoolPart>(owner);
            // WaterPuddle's blueprint is blue; LiquidPool.Initialize applies
            // the registered water definition's cyan during ordinary bootstrap/load.
            return render.RenderString == "~" && (render.ColorString == "&B" || render.ColorString == "&c") && render.RenderLayer == 1
                && string.IsNullOrEmpty(render.BackgroundColor) && string.IsNullOrEmpty(render.GlyphVariants)
                && material != null && material.MaterialID == "Water" && material.Combustibility == 0f
                && material.HasMaterialTag("Liquid") && material.HasMaterialTag("Water") && owner.HasTag("Liquid") && owner.HasTag("Water")
                && pool != null && pool.LiquidId == "water" && pool.Volume > 0
                && thermal != null && float.IsFinite(thermal.Temperature) && float.IsFinite(thermal.FreezeTemperature)
                && float.IsFinite(thermal.FlameTemperature) && float.IsFinite(thermal.VaporTemperature)
                && thermal.Temperature > thermal.FreezeTemperature && thermal.Temperature < thermal.VaporTemperature
                && thermal.Temperature < thermal.FlameTemperature;
        }
        static bool OrdinaryGrass(Entity owner)
        {
            if (!PlainTerrain(owner) || ForeignMeaning(owner) || !owner.HasTag("Vegetation") || !owner.HasTag("Plantable")
                || owner.HasPart<MaterialPart>() || owner.HasPart<ThermalPart>() || owner.HasPart<DestructiblePart>()
                || owner.HasPart<LiquidPoolPart>()) return false;
            var render = Owned<RenderPart>(owner);
            return render.RenderString == "." && render.ColorString == "&g" && render.BackgroundColor == "^g"
                && render.RenderLayer == 0 && render.GlyphVariants == "...,.'`";
        }
        static bool PlainTerrain(Entity owner)
        {
            if (owner == null || !owner.HasTag("Terrain") || owner.HasTag("Creature") || owner.HasTag("Player")
                || owner.HasTag("Item") || owner.HasTag("Solid") || owner.HasTag("Wall") || owner.HasTag("Barren")
                || owner.GetStat("Hitpoints") != null || Owned<ExaminablePart>(owner) == null) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.Solid || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (owner.HasPart<SpatialFootprintPart>())
            { var footprint = Owned<SpatialFootprintPart>(owner); if (footprint == null || footprint.Offsets.Length != 1 || footprint.Offsets[0] != Vector2Int.zero) return false; }
            if (owner.HasPart<StatusEffectsPart>())
            { var effects = Owned<StatusEffectsPart>(owner); if (effects == null || effects.GetRenderColorOverride() != null) return false; }
            return true;
        }
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<Body>() || owner.HasPart<BrainPart>() || owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>()
                || owner.HasPart<CultivatedSoilPart>() || owner.HasPart<HarvestablePart>() || owner.HasPart<WellPart>()
                || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>() || owner.HasPart<ContainerPart>() || owner.HasPart<InventoryPart>()
                || owner.HasPart<ConversationPart>() || owner.HasPart<TraderPart>() || owner.HasPart<LockPart>()
                || owner.HasPart("Restable") || owner.HasTag("Restable") || owner.HasPart<RegionalSignpostPart>()
                || owner.HasPart<CampfirePart>() || owner.HasPart<TorchLightPart>() || owner.HasPart<LightSourcePart>()
                || owner.HasPart<AlchemyStillPart>() || owner.HasPart<SoddenPreparationPart>() || owner.HasPart<TriggerOnStepPart>()
                || owner.HasPart<StairsDownPart>() || owner.HasPart<StairsUpPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>() || owner.HasPart<BurnOffGasPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
        static bool CompetingGround(Entity owner)
            => owner.HasTag("Terrain") || owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasPart<TriggerOnStepPart>() || owner.HasPart<FiniteLiquidFilmPart>() || owner.HasPart<WellPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
        static bool Placed(Zone zone, Cell cell, Entity owner)
        {
            if (owner == null || owner.SpatialZone != zone) return false;
            var anchor = zone.GetEntityCell(owner);
            if (anchor == null || !anchor.Objects.Contains(owner)) return false;
            if (!owner.HasPart<SpatialFootprintPart>()) return anchor == cell;
            var footprint = Owned<SpatialFootprintPart>(owner);
            return footprint != null && zone.IsFootprintCurrent(owner) && SpatialQuery.DistanceToCell(zone, owner, cell.X, cell.Y) == 0;
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
            for (int i = 0; i < owner.Parts.Count; i++) if (owner.Parts[i] is T part)
            { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (water == null) return;
            foreach (var tile in water) if (tile != null) { DestroyOwned(tile.sprite); DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
