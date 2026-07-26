using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Per-turn crop growth pass. Mirrors <see cref="GasSystem"/>'s
    /// shape exactly: a static system driven from the world entity's
    /// <c>TickEnd</c> event by <see cref="CropSystemPart"/>, snapshotting
    /// via the <c>Crop</c> tag before mutating zone contents.
    /// See <c>Docs/CROPS-WATERING-GRIMOIRE.md §2.3</c>.
    ///
    /// <para><b>Cadence:</b> <c>TickEnd</c> fires once per ACTOR-turn
    /// (N×/round in a zone with N actors) — the same convention the gas
    /// system uses. All thresholds are tick-denominated.</para>
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
                if (crop == null) continue;
                if (crop.MoistureTicks <= 0) continue; // dry = paused

                crop.MoistureTicks--;
                crop.TicksInStage++;

                if (crop.MoistureTicks == 0)
                    crop.OnDriedOut();

                if (crop.TicksInStage >= crop.TicksPerStage)
                    AdvanceStage(zone, entity, crop);
            }
        }

        private static void AdvanceStage(Zone zone, Entity entity, CropPart crop)
        {
            // Completing the sprout stage (stage 1) converts the crop
            // into its produce. Completing the seed stage (stage 0)
            // advances to sprout.
            if (crop.GrowthStage >= 1)
            {
                TryMature(zone, entity, crop);
                return;
            }

            crop.GrowthStage++;
            crop.TicksInStage = 0;

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

            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "StageAdvanced", target: entity,
                    payload: new { stage = crop.GrowthStage, cropBlueprint = entity.BlueprintName });
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

            var pos = zone.GetEntityPosition(entity);
            if (pos.x < 0) return;

            int spawned = 0;
            for (int i = 0; i < crop.YieldCount; i++)
            {
                var produce = Factory.CreateEntity(crop.YieldBlueprint);
                if (produce == null) break; // unknown blueprint: hold, retry next tick
                zone.AddEntity(produce, pos.x, pos.y);
                spawned++;
            }
            if (spawned == 0)
            {
                if (Diag.IsChannelEnabled("crop"))
                    Diag.Record("crop", "MatureBlocked", target: entity,
                        payload: new { reason = "unknown_yield_blueprint" });
                return;
            }

            zone.RemoveEntity(entity);
            ZoneRenderHooks.MarkCellDirty(pos.x, pos.y, "CropMatured");
            MessageLog.Add($"The {entity.GetDisplayName()} is ready — its harvest lies on the ground.");

            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "CropMatured", target: entity,
                    payload: new
                    {
                        yieldBlueprint = crop.YieldBlueprint,
                        yieldCount = spawned,
                        x = pos.x,
                        y = pos.y
                    });
        }

        private static void MarkCellDirty(Zone zone, Entity entity, string source)
        {
            var pos = zone.GetEntityPosition(entity);
            if (pos.x >= 0)
                ZoneRenderHooks.MarkCellDirty(pos.x, pos.y, source);
        }
    }
}
