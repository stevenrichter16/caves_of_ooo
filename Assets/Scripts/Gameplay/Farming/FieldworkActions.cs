using System;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    internal static class FieldworkActions
    {
        internal static bool CanClear(Entity actor, CropPart crop, Zone zone) => crop != null && crop.ParentEntity?.GetPart<CropPart>() == crop
            && WorldResourceActions.Nearby(actor, crop.ParentEntity, zone) && CropTime.IsCurrentOwner(crop, zone)
            && crop.GrowthStage >= 0 && crop.GrowthStage < 2 && CultivatedSoilPart.IsCultivated(zone, zone.GetEntityCell(crop.ParentEntity));
        internal static bool Clear(Entity actor, CropPart crop, Zone zone, InventoryTransaction tx)
        {
            if (!CanClear(actor, crop, zone) || !CropTime.Reconcile(crop, zone, WorldClock.CurrentTick) || !CanClear(actor, crop, zone)) return WorldResourceActions.Reject(actor, crop?.ParentEntity, "ClearCrop", "invalid_or_changed_young_crop");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                var owner = crop.ParentEntity; var cell = zone.GetEntityCell(owner); int index = cell.Objects.IndexOf(owner);
                if (!tx.TryClaim(actor, actor, "ClearCrop") || !tx.TryClaim(owner, actor, "ClearCrop")) return false;
                var gathering = LocalGatheringClaims.CaptureHarvest(actor, owner, zone); bool removed = false;
                tx.Do(null, () =>
                {
                    if (removed && owner.SpatialZone == null && zone.AddEntity(owner, cell.X, cell.Y))
                    { cell.Objects.Remove(owner); cell.Objects.Insert(Math.Min(index, cell.Objects.Count), owner); }
                    ZoneRenderHooks.MarkCellDirty(cell, "CropClearRollback");
                });
                if (!zone.RemoveEntity(owner)) return false; removed = true;
                tx.AfterCommit(() =>
                {
                    if (gathering != null && !gathering.Allowed) gathering.Claim.RecordBreach(actor, zone, cell, "clear");
                    ZoneRenderHooks.MarkCellDirty(cell, "CropCleared"); MessageLog.Add("You clear the young crop. No seed or produce is recovered.");
                });
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }
        internal static bool CanPrepare(Entity actor, FieldHarvestPart row, Zone zone)
        {
            var owner = row?.ParentEntity;
            if (owner == null || zone == null) return false;
            var cell = zone.GetEntityCell(owner);
            return row?.Harvested == true && owner.GetPart<FieldHarvestPart>() == row && WorldResourceActions.Nearby(actor, owner, zone)
                && owner.HasTag("Terrain") && !owner.HasTag("Solid") && !owner.HasTag("Creature") && !owner.HasTag("Item")
                && !owner.GetPart<PhysicsPart>().Takeable && !owner.GetPart<PhysicsPart>().Solid && !owner.HasPart<CultivatedSoilPart>()
                && !BarrenGroundRules.IsBarren(cell) && !cell.HasObjectWithPart<LiquidPoolPart>() && !cell.HasObjectWithPart<CropPart>() && cell.IsPassable();
        }
        internal static bool Prepare(Entity actor, FieldHarvestPart row, Zone zone, InventoryTransaction tx)
        {
            if (!CanPrepare(actor, row, zone)) return WorldResourceActions.Reject(actor, row?.ParentEntity, "PrepareFieldBed", "invalid_or_unavailable_stubble"); bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                var owner = row.ParentEntity;
                if (!tx.TryClaim(actor, actor, "PrepareFieldBed") || !tx.TryClaim(owner, actor, "PrepareFieldBed")) return false;
                var soil = new CultivatedSoilPart(); bool tagged = owner.HasTag("Plantable");
                tx.Do(null, () => { owner.RemovePart(soil); if (!tagged) owner.Tags.Remove("Plantable"); });
                if (!tagged) owner.SetTag("Plantable"); owner.AddPart(soil);
                tx.AfterCommit(() => { ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(owner), "FieldPrepared"); MessageLog.Add("You prepare the cut stubble for planting. Its grain is already spent."); });
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }
    }
}
