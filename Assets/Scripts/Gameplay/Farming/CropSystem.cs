using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Crop transitions and yield publication. Runtime cultivation uses
    /// CropTime's saved elapsed-world-time policy. OnTickEnd remains an
    /// explicit one-authored-unit driver for legacy fixtures and actorless
    /// benchmark events; stamped gameplay TickEnd does not invoke it.
    ///
    /// <para><b>Cadence:</b> Ten elapsed world ticks consume one moisture
    /// unit. NPC counts and repeated observation cannot accelerate growth.</para>
    ///
    /// <para><b>Single decrement path:</b> moisture and growth advance
    /// ONLY here. <see cref="CropPart"/> holds state and the two visual
    /// transitions (wet on <see cref="CropPart.Water"/>, dry via
    /// <see cref="CropPart.OnDriedOut"/> called from this tick).</para>
    /// </summary>
    public static class CropSystem
    {
        /// <summary>
        /// Global factory for spawning produce at maturity — set once by
        /// GameBootstrap (CorpsePart.Factory convention). When null, a
        /// matured crop is NOT deleted: it holds at the completion
        /// boundary and converts on the next MOIST tick after the
        /// factory is back (retry ticks still consume moisture, and a
        /// crop that dries during the hold sits paused until re-watered
        /// — produce is never lost, but recovery is not instantaneous;
        /// SM7d doc-drift fix).
        /// </summary>
        public static EntityFactory Factory;

        public static void OnTickEnd(Zone zone)
        {
            if (zone == null) return;

            // Snapshot: the loop mutates zone contents (maturity removes
            // the crop and adds produce) — never iterate the live list.
            List<Entity> crops = zone.GetEntitiesWithTag("Crop");
            for (int i = 0; i < crops.Count; i++)
            {
                var entity = crops[i];
                var crop = entity.GetPart<CropPart>();
                if (crop == null || crop.ParentEntity != entity || entity.SpatialZone != zone
                    || zone.GetEntityCell(entity) == null) continue;
                if (crop.GrowthStage < 0 || crop.GrowthStage > (crop.HarvestAtMaturity ? 2 : 1)) continue;
                if (crop.MoistureTicks <= 0) continue; // dry = paused

                crop.MoistureTicks--;
                bool ripe = crop.HarvestAtMaturity && crop.GrowthStage == 2;
                if (!ripe && crop.TicksInStage < int.MaxValue) crop.TicksInStage++;

                if (crop.MoistureTicks == 0)
                    crop.OnDriedOut();

                if (!ripe && crop.TicksInStage >= crop.TicksPerStage)
                    AdvanceStage(zone, entity, crop);
            }
        }

        /// <summary>Advance a validated owner's bounded wet interval. Work is
        /// bounded by the two growth transitions, never elapsed duration. A
        /// blocked legacy yield is attempted once per reconciliation; remaining
        /// wet time still dries the held crop, as with ordinary local retries.</summary>
        internal static void AdvanceWetUnits(Zone zone, Entity owner, CropPart crop, int units)
        {
            int remaining = units;
            while (remaining > 0 && CropTime.IsCurrentOwner(crop, zone))
            {
                bool ripe = crop.HarvestAtMaturity && crop.GrowthStage == 2;
                int toBoundary = ripe ? remaining : (int)System.Math.Max(1L, (long)crop.TicksPerStage - crop.TicksInStage);
                int step = System.Math.Min(remaining, toBoundary);
                ConsumeWetUnits(crop, step, !ripe);
                remaining -= step;
                if (ripe || crop.TicksInStage < crop.TicksPerStage) continue;

                // Stage painting is local; legacy yield can invoke factories.
                // Remember the expected held state before that callback, so
                // a replaced/moved/mutated crop is never advanced further.
                bool legacyMaturity = !crop.HarvestAtMaturity && crop.GrowthStage >= 1;
                int heldProgress = crop.TicksInStage, heldMoisture = crop.MoistureTicks;
                string yieldBlueprint = crop.YieldBlueprint;
                int yieldCount = crop.YieldCount, stageLength = crop.TicksPerStage;
                AdvanceStage(zone, owner, crop);
                if (!legacyMaturity) continue;
                if (!CropTime.IsCurrentOwner(crop, zone) || crop.GrowthStage != 1
                    || crop.TicksInStage != heldProgress || crop.MoistureTicks != heldMoisture
                    || crop.HarvestAtMaturity || crop.YieldBlueprint != yieldBlueprint
                    || crop.YieldCount != yieldCount || crop.TicksPerStage != stageLength) return;
                if (remaining > 0) ConsumeWetUnits(crop, remaining, true);
                return;
            }
        }

        private static void ConsumeWetUnits(CropPart crop, int units, bool grow)
        {
            crop.MoistureTicks -= units;
            if (grow) crop.TicksInStage = (int)System.Math.Min(int.MaxValue, (long)crop.TicksInStage + units);
            if (crop.MoistureTicks == 0) crop.OnDriedOut();
        }

        private static void AdvanceStage(Zone zone, Entity entity, CropPart crop)
        {
            // Completing the sprout stage (stage 1) converts the crop
            // into its produce. Completing the seed stage (stage 0)
            // advances to sprout.
            if (crop.GrowthStage >= 1)
            {
                if (crop.HarvestAtMaturity)
                {
                    crop.GrowthStage = 2;
                    crop.TicksInStage = 0;
                    PaintStage(zone, entity, crop);
                    if (Diag.IsChannelEnabled("crop")) Diag.Record("crop", "CropReady", target: entity,
                        payload: new { cropBlueprint = entity.BlueprintName, stage = 2 });
                }
                else TryMature(zone, entity, crop);
                return;
            }

            crop.GrowthStage++;
            crop.TicksInStage = 0;

            PaintStage(zone, entity, crop);

            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "StageAdvanced", target: entity,
                    payload: new { stage = crop.GrowthStage, cropBlueprint = entity.BlueprintName });
        }

        private static void PaintStage(Zone zone, Entity entity, CropPart crop)
        {
            var render = entity.GetPart<RenderPart>();
            if (render != null)
            {
                char glyph = crop.GlyphForStage(crop.GrowthStage);
                if (glyph != '\0')
                    render.RenderString = glyph.ToString();
                string color = crop.ColorForStage(crop.GrowthStage);
                if (color != null)
                    render.ColorString = color;
            }
            MarkCellDirty(zone, entity, "CropStageAdvanced");

        }

        private static void TryMature(Zone zone, Entity entity, CropPart crop)
        {
            // Hold at the boundary when the factory is unavailable —
            // deleting the crop without spawning produce would silently
            // destroy the player's harvest. TicksInStage stays >=
            // TicksPerStage so the next moist tick retries.
            if (Factory == null || string.IsNullOrEmpty(crop.YieldBlueprint))
            {
                if (Diag.IsChannelEnabled("crop"))
                    Diag.Record("crop", "MatureBlocked", target: entity,
                        payload: new { reason = Factory == null ? "no_factory" : "no_yield_blueprint" });
                return;
            }

            // Distinct reason for a zero/negative yield COUNT: the spawn
            // loop below never runs, so without this pre-check the
            // spawned==0 branch misattributed it as unknown_yield_blueprint
            // and sent debuggers hunting a blueprint that resolves fine
            // (SM7d, audit note).
            if (crop.YieldCount <= 0)
            {
                if (Diag.IsChannelEnabled("crop"))
                    Diag.Record("crop", "MatureBlocked", target: entity,
                        payload: new { reason = "no_yield_count" });
                return;
            }

            CropYieldService.TryRelease(crop, zone);
        }

        private static void MarkCellDirty(Zone zone, Entity entity, string source)
        {
            var pos = zone.GetEntityPosition(entity);
            if (pos.x >= 0)
                ZoneRenderHooks.MarkCellDirty(pos.x, pos.y, source);
        }
    }
}
