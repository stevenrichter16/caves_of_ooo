using System.Linq;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    /// <summary>One poured unit can enter one previously unflooded prepared bed.
    /// Captured before our own pool exists; older pools remain genuine flooding.</summary>
    internal sealed class CropPourIrrigation
    {
        readonly CropPart crop;
        readonly Entity owner, soil;
        readonly Cell cell;
        readonly CultivatedSoilPart marker;
        CropPourIrrigation(CropPart crop, Entity soil, Cell cell) { this.crop = crop; owner = crop.ParentEntity; this.soil = soil; this.cell = cell; marker = soil.GetPart<CultivatedSoilPart>(); }
        internal static CropPourIrrigation Capture(Zone zone, int x, int y, string liquid)
        {
            var cell = zone.GetCell(x, y);
            if (liquid != "water" || !CultivatedSoilPart.IsCultivated(zone, cell)) return null;
            var plants = cell.Objects.Where(e => e.HasPart<CropPart>()).ToArray();
            if (plants.Length != 1) return null;
            var crop = plants[0].GetPart<CropPart>();
            if (!CropTime.Reconcile(crop, zone, WorldClock.CurrentTick) || !CultivatedSoilPart.IsCultivated(zone, cell)
                || !CropTime.IsCurrentOwner(crop, zone) || crop.GrowthStage < 0 || crop.GrowthStage >= 2 || crop.MoistureTicks >= 40) return null;
            var soil = cell.Objects.First(e => e.HasPart<CultivatedSoilPart>() && e.HasTag("Terrain") && e.HasTag("Plantable"));
            return new CropPourIrrigation(crop, soil, cell);
        }
        internal bool Apply(Entity actor, Zone zone, Entity poolOwner, InventoryTransaction tx)
        {
            if (!CropTime.IsCurrentOwner(crop, zone) || zone.GetEntityCell(owner) != cell || crop.GrowthStage < 0 || crop.GrowthStage >= 2
                || crop.MoistureTicks < 0 || crop.MoistureTicks >= 40 || !WorldResourceActions.Ground(soil, zone)
                || zone.GetEntityCell(soil) != cell || soil.GetPart<CultivatedSoilPart>() != marker || !soil.HasTag("Terrain") || !soil.HasTag("Plantable")
                || soil.GetPart<PhysicsPart>().Takeable || soil.GetPart<PhysicsPart>().Solid || BarrenGroundRules.IsBarren(cell)
                || cell.Objects.Any(e => e != poolOwner && e.HasPart<LiquidPoolPart>())) return false;
            var pool = poolOwner.GetPart<LiquidPoolPart>();
            if (pool?.LiquidId != "water" || pool.Volume <= 0 || zone.GetEntityCell(poolOwner) != cell || !LiquidSourceSafety.IsUnmixedPool(zone, poolOwner)
                || !tx.TryClaim(owner, actor, "PourIrrigate") || !tx.TryClaim(soil, actor, "PourIrrigate")) return false;
            int moisture = crop.MoistureTicks, volume = pool.Volume; var render = owner.GetPart<RenderPart>(); string background = render?.BackgroundColor;
            tx.Do(null, () => { crop.MoistureTicks = moisture; pool.Volume = volume; if (render != null) render.BackgroundColor = background; });
            crop.MoistureTicks = 40; pool.Volume--; if (render != null) render.BackgroundColor = CropPart.WET_SOIL_BG;
            tx.AfterCommit(() =>
            {
                LiquidVesselService.PublishCommittedPoolDraw(zone, poolOwner, pool, cell);
                ZoneRenderHooks.MarkCellDirty(cell, "CropPourWatered"); MessageLog.Add("One poured water soaks into the planted bed.");
            });
            return true;
        }
    }
}
