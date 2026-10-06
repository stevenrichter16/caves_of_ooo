using System;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    internal static class CultivationPreparationService
    {
        internal static void AddActions(Entity actor, CropPart crop, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Current(actor, crop, zone)) return;
            var owner = crop.ParentEntity; var cell = zone.GetEntityCell(owner);
            if (FreshCrop(crop)) foreach (var item in actor.GetPart<InventoryPart>().Objects)
                if (item.BlueprintName == "InertSludge" && PreparationActions.Carried(actor, item))
                    actions.AddAction("CompostCrop" + item.ID, "compost: 1 sludge, -25% time (once)", "CompostCrop|" + PreparationActions.Escaped(item), '\0', 17);
            if (crop.GrowthStage == 2 && crop.SeedYieldCount > 0 && !string.IsNullOrEmpty(crop.SeedYieldBlueprint))
                actions.AddAction("HarvestCropSeeds", "harvest: 3x seeds, no produce", "HarvestCropSeeds", '\0', 16);
            if (crop.GrowthStage >= 0 && crop.GrowthStage < 2)
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    var destination = zone.GetCell(cell.X + dx, cell.Y + dy);
                    if (CanTransplant(actor, crop, zone, destination)) actions.AddAction("TransplantCrop" + destination.X + "," + destination.Y,
                        "move to bed (" + destination.X + ", " + destination.Y + "); keep growth",
                        "TransplantCrop|" + destination.X + "|" + destination.Y, '\0', 15);
                }
        }
        internal static bool Handle(GameEvent e, CropPart crop)
        {
            string command = e.GetStringParameter("Command");
            if (command == null || !(command == "HarvestCropSeeds" || command.StartsWith("CompostCrop|", StringComparison.Ordinal) || command.StartsWith("TransplantCrop|", StringComparison.Ordinal))) return false;
            var actor = e.GetParameter<Entity>("Actor"); var zone = PreparationActions.ZoneFor(e, actor); var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (e.GetParameter<bool>("PreparationAttempted")) return false; e.SetParameter("PreparationAttempted", true);
            if (tx == null || !Current(actor, crop, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || !CropTime.Reconcile(crop, zone, WorldClock.CurrentTick) || !Current(actor, crop, zone))
                return PreparationActions.Reject(actor, crop.ParentEntity, command, "crop_unavailable");
            if (command == "HarvestCropSeeds") return CropYieldService.TryRelease(crop, zone, actor, tx, seedsOnly: true);
            if (command.StartsWith("CompostCrop|", StringComparison.Ordinal))
                return Compost(actor, crop, zone, WorldResourceActions.ExactCarried(actor, command.Substring(12)), tx, command);
            var parts = command.Split('|');
            return parts.Length == 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y)
                ? Transplant(actor, crop, zone, zone.GetCell(x, y), tx, command)
                : PreparationActions.Reject(actor, crop.ParentEntity, command, "invalid_destination");
        }
        static bool Current(Entity actor, CropPart crop, Zone zone) => crop?.ParentEntity != null && PreparationActions.CurrentActor(actor, zone)
            && CropTime.IsCurrentOwner(crop, zone) && crop.HarvestAtMaturity && WorldResourceActions.Nearby(actor, crop.ParentEntity, zone)
            && zone.GetEntityCell(crop.ParentEntity).IsVisible && crop.ParentEntity.GetPart<RenderPart>()?.Visible == true
            && CultivatedSoilPart.IsCultivated(zone, zone.GetEntityCell(crop.ParentEntity));
        static bool FreshCrop(CropPart crop) => !crop.Composted && crop.GrowthStage == 0 && crop.TicksInStage == 0
            && crop.GrowthWetTickRemainder == 0 && crop.TicksPerStage > 3;
        static bool Compost(Entity actor, CropPart crop, Zone zone, Entity sludge, InventoryTransaction tx, string command)
        {
            if (!FreshCrop(crop) || sludge?.BlueprintName != "InertSludge" || !PreparationActions.Begin(actor, sludge, zone, tx, command)
                || !tx.TryClaim(crop.ParentEntity, actor, command)) return PreparationActions.Reject(actor, crop.ParentEntity, command, "fresh_crop_or_sludge_required");
            int before = crop.TicksPerStage; bool composted = crop.Composted; var pack = actor.GetPart<InventoryPart>();
            var receipt = InventoryTransferSnapshot.Capture(pack); bool restored = false;
            Action restore = () => { if (!restored) { restored = true; crop.TicksPerStage = before; crop.Composted = composted; receipt.Restore(); } }; tx.Do(null, restore);
            if (!receipt.Apply(() => { if (!pack.TryConsumeOne(sludge)) return false; crop.TicksPerStage = (int)(((long)before * 3 + 3) / 4); crop.Composted = true; return true; })
                || !receipt.ClaimChanges(tx, actor, command)) { restore(); return PreparationActions.Reject(actor, crop.ParentEntity, command, "compost_transfer"); }
            PreparationActions.Complete(tx, actor, crop.ParentEntity, command, "You work inert sludge around the fresh crop. It still needs water."); return true;
        }
        static bool Claimed(Zone zone, Cell cell)
        {
            foreach (var owner in zone.GetReadOnlyEntities())
                if (owner.GetPart<LocalGatheringClaimPart>() is LocalGatheringClaimPart claim && claim.Bound(zone) && claim.OwnsBed(zone, cell)) return true;
            return false;
        }
        static bool CanTransplant(Entity actor, CropPart crop, Zone zone, Cell destination)
        {
            if (!Current(actor, crop, zone) || crop.GrowthStage < 0 || crop.GrowthStage >= 2 || destination == null || destination.ParentZone != zone
                || !destination.IsVisible || !destination.Explored || !CultivatedSoilPart.IsCultivated(zone, destination)
                || destination.HasObjectWithPart<CropPart>() || !destination.IsPassable()) return false;
            var from = zone.GetEntityCell(crop.ParentEntity); var at = zone.GetEntityCell(actor);
            return destination != from && Math.Abs(destination.X - at.X) <= 1 && Math.Abs(destination.Y - at.Y) <= 1
                && Math.Abs(destination.X - from.X) <= 1 && Math.Abs(destination.Y - from.Y) <= 1
                && !Claimed(zone, from) && !Claimed(zone, destination);
        }
        static bool Transplant(Entity actor, CropPart crop, Zone zone, Cell destination, InventoryTransaction tx, string command)
        {
            if (!CanTransplant(actor, crop, zone, destination) || !tx.TryClaim(actor, actor, command) || !tx.TryClaim(crop.ParentEntity, actor, command))
                return PreparationActions.Reject(actor, crop.ParentEntity, command, "empty_nearby_unclaimed_bed_required");
            var owner = crop.ParentEntity; var from = zone.GetEntityCell(owner); int index = from.Objects.IndexOf(owner); bool moved = false;
            Action restore = () =>
            {
                if (!moved) return; moved = false;
                if (owner.SpatialZone == zone) zone.RemoveEntity(owner);
                if (owner.SpatialZone == null && zone.AddEntity(owner, from.X, from.Y))
                { from.Objects.Remove(owner); from.Objects.Insert(Math.Min(index, from.Objects.Count), owner); }
                ZoneRenderHooks.MarkCellDirty(from, "TransplantRollback"); ZoneRenderHooks.MarkCellDirty(destination, "TransplantRollback");
            };
            tx.Do(null, restore);
            if (!zone.RemoveEntity(owner)) return PreparationActions.Reject(actor, owner, command, "source_changed"); moved = true;
            if (!zone.AddEntity(owner, destination.X, destination.Y)) { restore(); return PreparationActions.Reject(actor, owner, command, "destination_changed"); }
            tx.AfterCommit(() => { ZoneRenderHooks.MarkCellDirty(from, "CropTransplanted"); ZoneRenderHooks.MarkCellDirty(destination, "CropTransplanted"); });
            PreparationActions.Complete(tx, actor, owner, command, "You transplant the young crop with its growth and moisture intact."); return true;
        }
    }
}
