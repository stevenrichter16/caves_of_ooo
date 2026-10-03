using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Four finite accomplishments bound to this actual new world's authored sources.
    /// Source and player records are ordinary saved Parts; queries never award experience.</summary>
    public static class ConnectedSpreadProgress
    {
        public const int AwardXP = 30;
        const int Stores = 1, Restoration = 2, DryBed = 4, Kitchen = 8;
        const int RepairSource = 1, CropSource = 2, KitchenSource = 4;

        public static bool BindClay(Entity item, string worldKey, int ordinal)
        {
            if (!ValidKey(worldKey) || ordinal < 0 || ordinal > 1 || item?.BlueprintName != "FireClay"
                || string.IsNullOrEmpty(item.ID) || Count(item) != 1 || item.HasPart<ConnectedClayOriginPart>()) return false;
            item.AddPart(new ConnectedClayOriginPart { WorldKey = worldKey, OwnerID = item.ID, Units = 1 << ordinal });
            return true;
        }
        public static bool BindRepair(Entity owner, string worldKey) => owner?.GetPart<RepairablePart>()?.Repaired == false
            && (owner.BlueprintName == "RepairLinedWell" || owner.BlueprintName == "ConnectedBatchPan")
            && Bind(owner, worldKey, RepairSource);
        public static bool BindDryCrop(Entity owner, string worldKey) => owner?.BlueprintName == "SootrootCrop"
            && owner.GetPart<CropPart>() is CropPart crop && crop.GrowthStage == 0 && crop.MoistureTicks == 0
            && crop.HarvestAtMaturity && Bind(owner, worldKey, CropSource);
        public static bool BindKitchen(Entity owner, string worldKey) => owner?.BlueprintName == "ConnectedBatchPan"
            && owner.GetPart<KitchenBatchPart>()?.Configured == true && Bind(owner, worldKey, KitchenSource);
        static bool Bind(Entity owner, string key, int kind)
        {
            if (!ValidKey(key) || string.IsNullOrEmpty(owner?.ID)) return false;
            var stamp = owner.GetPart<ConnectedSpreadSourcePart>();
            if (stamp != null)
            {
                if (stamp.OwnerID != owner.ID || stamp.WorldKey != key || stamp.Blueprint != owner.BlueprintName
                    || (stamp.Kinds & kind) != 0) return false;
                stamp.Kinds |= kind; return true;
            }
            owner.AddPart(new ConnectedSpreadSourcePart { WorldKey = key, OwnerID = owner.ID, Blueprint = owner.BlueprintName, Kinds = kind });
            return true;
        }
        internal static bool ValidKey(string key) => key != null && Guid.TryParseExact(key, "N", out _);
        static int Count(Entity item) => item?.GetPart<StackerPart>()?.StackCount ?? 1;
        static string CurrentWorld(Entity actor, Zone zone)
        {
            var manager = WorldLocationContext.For(zone);
            var at = actor == null ? null : zone?.GetEntityCell(actor);
            string key = manager?.Exploration?.WorldKey;
            return manager?.Exploration != null && actor != null && actor.HasTag("Player") && !string.IsNullOrEmpty(actor.ID)
                && actor.GetStatValue("Hitpoints", 0) > 0 && !CombatSystem.IsDeathHandled(actor)
                && actor.SpatialZone == zone && at != null && at.Objects.Contains(actor)
                && manager.Exploration.Version >= 11 && manager.Exploration.Enabled && ValidKey(key)
                && manager.CachedZones.TryGetValue(zone.ZoneID, out var live) && live == zone ? key : null;
        }
        static bool Bound(Entity owner, string key, int kind) => owner != null && key != null
            && owner.GetPart<ConnectedSpreadSourcePart>() is ConnectedSpreadSourcePart stamp
            && stamp.ParentEntity == owner && stamp.OwnerID == owner.ID && stamp.WorldKey == key
            && stamp.Blueprint == owner.BlueprintName && (stamp.Kinds & kind) != 0
            && owner.Parts.Count(p => p is ConnectedSpreadSourcePart) == 1;
        static bool Carried(Entity actor, Entity item)
        {
            var pack = actor?.GetPart<InventoryPart>(); var physical = item?.GetPart<PhysicsPart>();
            return pack?.ParentEntity == actor && item != null && Count(item) > 0 && item.SpatialZone == null
                && physical?.ParentEntity == item && physical.InInventory == actor && physical.Equipped == null
                && pack.FindEquippedBodyPart(item) == null && !pack.EquippedItems.ContainsValue(item)
                && pack.Objects.Count(e => e == item || e?.ID == item.ID) == 1 && pack.Objects.Contains(item);
        }
        internal sealed class TransferReceipt
        {
            internal Entity Actor, Item, Pan; internal Zone Zone; internal string Key;
            internal int ClayUnits, Quantity; internal Dictionary<Entity, int> Before;
        }
        internal static TransferReceipt CaptureTransfer(Entity actor, Entity item, Zone zone, Entity container = null)
        {
            string key = CurrentWorld(actor, zone);
            var pack = actor?.GetPart<InventoryPart>(); var physics = item?.GetPart<PhysicsPart>();
            if (key == null || pack?.ParentEntity != actor || item == null || Count(item) <= 0
                || physics?.ParentEntity != item || !physics.Takeable || physics.Equipped != null) return null;
            if (container == null)
            {
                if (item.SpatialZone != zone || zone.GetEntityCell(item) == null || physics.InInventory != null
                    || SpatialQuery.Distance(zone, actor, item) > 1) return null;
            }
            else if (container.SpatialZone != zone || zone.GetEntityCell(container) == null
                || SpatialQuery.Distance(zone, actor, container) > 1 || physics.InInventory != container || item.SpatialZone != null
                || container.GetPart<ContainerPart>()?.Contents.Count(e => e == item || e?.ID == item.ID) != 1) return null;
            int units = ConnectedClayProvenance.Mask(item, key);
            Entity pan = null;
            if (item.BlueprintName == "FieldMeal")
                foreach (var owner in zone.GetReadOnlyEntities())
                    if (Bound(owner, key, KitchenSource) && owner.GetPart<KitchenBatchPart>() is KitchenBatchPart batch
                        && batch.Pickup == container && batch.OwnsCommissionedMeal(actor, item, zone))
                    { if (pan != null) return null; pan = owner; }
            if (units == 0 && pan == null) return null;
            var before = new Dictionary<Entity, int>();
            foreach (var held in pack.Objects) if (held != null) before[held] = Count(held);
            return new TransferReceipt { Actor = actor, Item = item, Zone = zone, Key = key,
                ClayUnits = units, Pan = pan, Quantity = Count(item), Before = before };
        }
        internal static void RecordTransfer(TransferReceipt receipt, InventoryTransaction transaction)
        {
            if (receipt == null || transaction == null || CurrentWorld(receipt.Actor, receipt.Zone) != receipt.Key) return;
            var actor = receipt.Actor; var pack = actor.GetPart<InventoryPart>(); int present = 0;
            foreach (var item in pack.Objects)
                if (Carried(actor, item)) present |= ConnectedClayProvenance.Mask(item, receipt.Key);
            int recovered = present & receipt.ClayUnits;
            if (recovered != 0) Stage(actor, receipt.Zone, receipt.Key, Stores, recovered, transaction);
            if (receipt.Pan == null || !Bound(receipt.Pan, receipt.Key, KitchenSource)
                || receipt.Pan.GetPart<KitchenBatchPart>()?.OwnsCommissionedMeal(actor, receipt.Item, receipt.Zone) != true) return;
            bool carried = Carried(actor, receipt.Item);
            // Ordinary native stacking may consume the transferred entity. Require
            // its positive quantity to appear in existing compatible carried stacks.
            if (!carried && Count(receipt.Item) == 0)
            {
                int added = 0;
                foreach (var item in pack.Objects)
                    if (item.BlueprintName == "FieldMeal" && Carried(actor, item) && receipt.Before.TryGetValue(item, out int before))
                        added += Math.Max(0, Count(item) - before);
                carried = added >= receipt.Quantity;
            }
            if (carried) Stage(actor, receipt.Zone, receipt.Key, Kitchen, 0, transaction);
        }
        internal static void RecordRepair(Entity actor, Entity target, Zone zone, InventoryTransaction transaction)
        {
            string key = CurrentWorld(actor, zone);
            bool site = target?.BlueprintName == "RepairLinedWell" && zone?.ZoneID == GleanersDistrict.SurfaceID
                || target?.BlueprintName == "ConnectedBatchPan" && zone?.ZoneID == KitchenBatchPart.KitchenZoneID;
            if (site && Bound(target, key, RepairSource) && target.SpatialZone == zone && zone.GetEntityCell(target) != null
                && target.GetPart<RepairablePart>()?.Repaired == true)
                Stage(actor, zone, key, Restoration, 0, transaction);
        }
        internal static void RecordHarvest(Entity actor, Entity source, Zone zone, InventoryTransaction transaction)
        {
            string key = CurrentWorld(actor, zone);
            if (zone?.ZoneID == GleanersCellarBuilder.ZoneID && Bound(source, key, CropSource)
                && source.BlueprintName == "SootrootCrop" && source.SpatialZone == null
                && source.GetPart<CropPart>() is CropPart crop && crop.GrowthStage == 2 && crop.HarvestAtMaturity)
                Stage(actor, zone, key, DryBed, 0, transaction);
        }
        static void Stage(Entity actor, Zone zone, string key, int award, int units, InventoryTransaction transaction)
        {
            var xp = actor.GetStat("Experience"); var level = actor.GetStat("Level");
            if (transaction == null || xp?.Owner != actor || level?.Owner != actor || xp.BaseValue < 0
                || xp.BaseValue > int.MaxValue - AwardXP || level.Value < 1 || level.Value > 100
                || !transaction.TryClaim(actor, actor, "ConnectedSpreadProgress")) return;
            var ledger = actor.GetPart<ConnectedSpreadProgressPart>();
            if (ledger != null && (ledger.PlayerID != actor.ID || ledger.WorldKey != key
                || (ledger.Awards & ~15) != 0 || (ledger.ClayUnits & ~3) != 0
                || actor.Parts.Count(p => p is ConnectedSpreadProgressPart) != 1)) return;
            if (ledger == null)
            {
                ledger = new ConnectedSpreadProgressPart { WorldKey = key, PlayerID = actor.ID };
                var created = ledger; transaction.Do(() => actor.AddPart(created), () => actor.RemovePart(created));
            }
            int beforeUnits = ledger.ClayUnits, beforeAwards = ledger.Awards;
            int afterUnits = beforeUnits | units;
            bool grant = (beforeAwards & award) == 0 && (award != Stores || afterUnits == 3);
            if (beforeUnits == afterUnits && !grant) return;
            var current = ledger;
            transaction.Do(() => { current.ClayUnits = afterUnits; if (grant) current.Awards |= award; },
                () => { current.ClayUnits = beforeUnits; current.Awards = beforeAwards; });
            if (!grant) return;
            transaction.AfterCommit(() =>
            {
                // Reserve the saved once-bit before any leveling callbacks. This is
                // called only after the physical outcome and all outer hooks commit.
                xp.BaseValue = (int)Math.Min(int.MaxValue, (long)xp.BaseValue + AwardXP);
                MessageLog.Add("You gain 30 XP for " + Description(award) + ".");
                Diag.Record("event", "ConnectedExplorationAccomplished", actor, payload: new { worldKey = key, accomplishment = ID(award), xp = AwardXP });
                LevelingSystem.CheckLevelUp(actor, zone);
            });
        }
        static string ID(int award) => award == Stores ? "cellar-stores" : award == Restoration ? "service-restored"
            : award == DryBed ? "dry-bed-recovered" : "kitchen-working";
        static string Description(int award) => award == Stores ? "recovering the original cellar clay" : award == Restoration ? "restoring a district service"
            : award == DryBed ? "bringing the dry cellar bed to harvest" : "collecting your first commissioned field meal";
    }
    public sealed class ConnectedSpreadSourcePart : Part
    {
        public override string Name => "ConnectedSpreadSource";
        public string WorldKey, OwnerID, Blueprint;
        public int Kinds;
    }
    public sealed class ConnectedSpreadProgressPart : Part
    {
        public override string Name => "ConnectedSpreadProgress";
        public string WorldKey, PlayerID;
        public int Awards, ClayUnits;
    }
}
