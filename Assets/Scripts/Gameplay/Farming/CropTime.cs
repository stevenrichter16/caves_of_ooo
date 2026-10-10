using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Reconciles actual planted owners against the one saved world
    /// clock. Ten world ticks consume one authored moisture/growth unit. A
    /// remainder represents wet time only; dry absence never earns credit.
    /// Call after the player scheduler, at real arrival, and before crop
    /// mutation. Generation queries and partial save hydration must not call it.</summary>
    public static class CropTime
    {
        public const int WorldTicksPerUnit = 10;
        public const int CurrentVersion = 1;
        static readonly HashSet<CropPart> Updating = new HashSet<CropPart>();
        static readonly Stack<List<Entity>> Scratch = new Stack<List<Entity>>();

        /// <summary>Initializes only legacy stamps at the restored clock. This
        /// does not advance already-stamped crops in inactive saved graphs.</summary>
        public static void InitializeLegacyZone(Zone zone, int currentTick) => Visit(zone, currentTick, true);

        /// <summary>Applies elapsed time to a snapshot of physical crop owners.
        /// A pooled list keeps active turn work proportional to crop count.</summary>
        public static void ReconcileZone(Zone zone, int currentTick) => Visit(zone, currentTick, false);

        static void Visit(Zone zone, int currentTick, bool legacyOnly)
        {
            if (zone == null || currentTick < 0) return;
            var owners = Scratch.Count == 0 ? new List<Entity>() : Scratch.Pop();
            try
            {
                zone.GetEntitiesWithTagNonAlloc("Crop", owners);
                foreach (var owner in owners)
                {
                    var crop = owner.GetPart<CropPart>();
                    if (crop != null && (!legacyOnly || crop.GrowthTimingVersion == 0))
                        Reconcile(crop, zone, currentTick);
                }
            }
            finally { owners.Clear(); Scratch.Push(owners); }
        }

        /// <summary>Returns false for stale/malformed/reentrant owners. A true
        /// result includes no elapsed time, legacy initialization, and successful
        /// legacy maturity that removes the owner. Repeat calls at the same
        /// tick never consume more water or publish another yield.</summary>
        public static bool Reconcile(CropPart crop, Zone zone, int currentTick)
        {
            if (!IsCurrentOwner(crop, zone)) return Reject(crop, "invalid-owner");
            if (currentTick < 0 || crop.TicksPerStage <= 0 || crop.TicksInStage < 0 || crop.MoistureTicks < 0
                || crop.GrowthStage < 0 || crop.GrowthStage > (crop.HarvestAtMaturity ? 2 : 1))
                return Reject(crop, "invalid-state");
            if (!Updating.Add(crop)) return false;
            try
            {
                if (crop.GrowthTimingVersion == 0)
                {
                    if (crop.LastGrowthWorldTick != -1 || crop.GrowthWetTickRemainder != 0)
                        return Reject(crop, "invalid-legacy-stamp");
                    crop.GrowthTimingVersion = CurrentVersion;
                    crop.LastGrowthWorldTick = currentTick;
                    return true;
                }
                if (crop.GrowthTimingVersion != CurrentVersion || crop.LastGrowthWorldTick < 0
                    || crop.LastGrowthWorldTick > currentTick || crop.GrowthWetTickRemainder < 0
                    || crop.GrowthWetTickRemainder >= WorldTicksPerUnit
                    || (crop.MoistureTicks == 0 && crop.GrowthWetTickRemainder != 0))
                    return Reject(crop, "invalid-clock-stamp");

                // Widen before arithmetic; even a near-int-max valid clock and
                // nine carried wet ticks cannot overflow into free growth.
                long elapsed = (long)currentTick - crop.LastGrowthWorldTick;
                if (elapsed == 0) return true;
                int beforeMoisture = crop.MoistureTicks;
                long wetTicks = elapsed + crop.GrowthWetTickRemainder;
                int units = (int)System.Math.Min(wetTicks / WorldTicksPerUnit, (long)beforeMoisture);
                int remainder = units >= beforeMoisture ? 0 : (int)(wetTicks % WorldTicksPerUnit);

                // Reserve the interval before yield factories/notifications can
                // reenter. The guard also prevents callbacks from watering or
                // harvesting a crop while its maturity publication is pending.
                crop.LastGrowthWorldTick = currentTick;
                crop.GrowthWetTickRemainder = remainder;
                if (units > 0) CropSystem.AdvanceWetUnits(zone, crop.ParentEntity, crop, units);
                if (Diag.IsRecordEnabled("crop", "CropTimeReconciled")) Diag.Record("crop", "CropTimeReconciled", target: crop.ParentEntity,
                    payload: new { elapsedWorldTicks = elapsed, growthUnits = units, moistureBefore = beforeMoisture,
                        moistureAfter = crop.MoistureTicks, remainder, currentTick, timingVersion = CurrentVersion });
                return true;
            }
            finally { Updating.Remove(crop); }
        }

        internal static bool IsCurrentOwner(CropPart crop, Zone zone)
        {
            var owner = crop?.ParentEntity;
            var physics = owner?.GetPart<PhysicsPart>();
            var cell = owner == null ? null : zone?.GetEntityCell(owner);
            return owner != null && zone != null && cell != null && cell.ParentZone == zone
                && owner.SpatialZone == zone && cell.Objects.Contains(owner) && !string.IsNullOrEmpty(owner.ID)
                && owner.HasTag("Crop") && owner.GetPart<CropPart>() == crop
                && physics != null && physics.ParentEntity == owner && physics.InInventory == null
                && physics.Equipped == null && !physics.Takeable && !physics.Solid;
        }

        static bool Reject(CropPart crop, string reason)
        {
            if (Diag.IsChannelEnabled("crop")) Diag.Record("crop", "CropTimeRejected", target: crop?.ParentEntity,
                payload: new { reason });
            return false;
        }
    }
}
