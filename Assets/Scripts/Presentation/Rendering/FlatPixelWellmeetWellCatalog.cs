using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
    /// <summary>The currently disclosed Wellmeet MainWell. The existing cracked/sealed lining
    /// expresses structural repair, not stock or purification. Queries never advance site state,
    /// dispatch Render, change its aura or backfill a settlement. Text/actions retain authority.</summary>
    public sealed class FlatPixelWellmeetWellCatalog : IDisposable
    {
        readonly Tile cracked, sealedRing;
        bool disposed;
        public FlatPixelWellmeetWellCatalog() : this(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath)) { }
        public FlatPixelWellmeetWellCatalog(Texture2D atlas)
        {
            if (atlas == null || atlas.width != 192 || atlas.height != 24) return;
            cracked = Build(atlas, 0); sealedRing = Build(atlas, 1);
        }
        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Well" && (owner.HasPart<WellSitePart>() || !string.IsNullOrEmpty(owner.GetProperty("SettlementSiteId")));
        public bool TryResolve(Zone zone, Entity owner, int x, int y, out Tile tile, out Color tint)
        {
            tile = null; tint = Color.white;
            if (disposed || !HasKnownRole(owner) || zone?.ZoneID != WellmeetCompositionPlan.ZoneID
                || !FlatPixelSettlementGroundCatalog.IsSupportedZone(zone)) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager.WorldMap.GetBiome(8, 16) != BiomeType.Beating
                || !AreaCompositionScope.IsWellmeetSite(manager.WorldMap.GetPOI(8, 16))) return false;
            var cell = zone.GetCell(x, y);
            if (cell == null || !cell.IsVisible || !cell.Explored || cell.ParentZone != zone
                || owner.SpatialZone != zone || zone.GetEntityCell(owner) != cell || !cell.Objects.Contains(owner)) return false;
            var part = Owned<WellSitePart>(owner); var r = Owned<RenderPart>(owner); var p = Owned<PhysicsPart>(owner);
            if (part == null || part.SettlementId != zone.ZoneID || part.SiteId != SettlementSiteDefinitions.MainWellSiteId
                || owner.GetProperty("SettlementId") != part.SettlementId || owner.GetProperty("SettlementSiteId") != part.SiteId
                || r == null || !r.Visible || r.RenderString != "O" || r.RenderLayer != 1
                || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant)
                || !string.IsNullOrEmpty(r.GlyphVariants) || !string.IsNullOrEmpty(r.BackgroundColor)
                || p == null || !p.Solid || p.Takeable || p.InInventory != null || p.Equipped != null
                || Owned<WellPart>(owner) == null || Owned<ExaminablePart>(owner) == null || ForeignMeaning(owner)) return false;
            if (owner.HasPart<SpatialFootprintPart>())
            { var footprint = Owned<SpatialFootprintPart>(owner); if (footprint == null || !zone.IsFootprintCurrent(owner) || footprint.Offsets.Length != 1 || footprint.Offsets[0] != Vector2Int.zero) return false; }
            if (owner.HasPart<StatusEffectsPart>() && Owned<StatusEffectsPart>(owner) == null) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            foreach (var other in cell.Objects)
                if (other != owner && other.HasPart<WellSitePart>() && other.SpatialZone == zone
                    && zone.GetEntityCell(other) == cell && Owned<RenderPart>(other)?.Visible == true
                    && Owned<PhysicsPart>(other) is PhysicsPart physical && physical.InInventory == null && physical.Equipped == null
                    && CurrentOptionalOccupancy(zone, other))
                    return false;
            if (manager.SettlementManager == null || !manager.SettlementManager.TryGetExistingSite(part.SettlementId, part.SiteId, out var site)
                || site.SiteType != RepairableSiteType.Well || site.ProblemType != RepairProblemType.FouledWater
                || site.Stage != part.VisualStage) return false;
            string color;
            switch (site.Stage)
            {
                case RepairStage.Fouled: color = "&y"; tile = cracked; break;
                case RepairStage.TemporarilyPurified: color = "&W"; tile = cracked; break;
                case RepairStage.StableRepair: color = "&c"; tile = sealedRing; break;
                case RepairStage.ImprovedWithCaretaker: color = "&C"; tile = sealedRing; break;
                default: return false;
            }
            if (r.ColorString != color || tile == null) { tile = null; return false; }
            tint = QudColorParser.Parse(color); return true;
        }
        static bool CurrentOptionalOccupancy(Zone zone, Entity owner)
        {
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            return !owner.HasPart<SpatialFootprintPart>()
                || Owned<SpatialFootprintPart>(owner) != null && zone.IsFootprintCurrent(owner);
        }
        static bool ForeignMeaning(Entity e)
            => e.HasTag("Creature") || e.HasTag("Player") || e.HasTag("Terrain") || e.HasTag("Item") || e.HasTag("Crop")
                || e.HasTag("WellGroundMarker") || e.HasTag("OvenGroundMarker") || e.HasTag("LanternGroundMarker")
                || e.GetStat("Hitpoints") != null || e.HasPart<RepairablePart>() || e.HasPart<LiquidPoolPart>()
                || e.HasPart<ContainerPart>() || e.HasPart<LockPart>() || e.HasPart<DestructiblePart>()
                || e.HasPart<BrainPart>() || e.HasPart<Body>() || e.HasPart<TraderPart>() || e.HasPart<ConversationPart>()
                || e.HasPart<OvenSitePart>() || e.HasPart<LanternSitePart>() || e.HasPart<DoorPart>()
                || e.HasPart<StairsDownPart>() || e.HasPart<StairsUpPart>() || e.HasPart<HarvestablePart>()
                || e.HasPart<TileStateSourcePart>() || e.HasPart<GasPoolPart>() || e.HasPart<FiniteLiquidFilmPart>()
                || e.HasPart<CropPart>() || e.HasPart<CultivatedSoilPart>() || e.HasPart<TriggerOnStepPart>()
                || e.HasPart<ForgePart>() || e.HasPart<CampfirePart>() || e.HasPart<AlchemyStillPart>()
                || e.HasPart<SoddenPreparationPart>() || e.HasPart<FieldHarvestPart>() || e.HasPart<RegionalSignpostPart>()
                || e.HasPart<BurnOffGasPart>() || e.HasPart("Restable") || e.HasTag("Restable") || e.HasTag("Plantable")
                || e.HasPart<RootFacePart>() || e.HasPart<SeventhPositionPart>() || e.HasPart<FoundingPlumePart>() || e.HasPart<HearthPatchPart>();
        static T Owned<T>(Entity owner) where T : Part
        {
            T result = null;
            foreach (var p in owner.Parts) if (p is T part)
            { if (result != null || part.ParentEntity != owner) return null; result = part; }
            return result;
        }
        static Tile Build(Texture2D atlas, int frame)
        {
            var sprite = Sprite.Create(atlas, new Rect(frame * 16, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 Wellmeet " + (frame == 0 ? "cracked lining" : "sealed lining"); sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
            tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; return tile;
        }
        public void Dispose() { if (disposed) return; disposed = true; Release(cracked); Release(sealedRing); }
        static void Release(Tile tile) { if (tile == null) return; Destroy(tile.sprite); Destroy(tile); }
        static void Destroy(Object o) { if (o == null) return; if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }
    }
}
