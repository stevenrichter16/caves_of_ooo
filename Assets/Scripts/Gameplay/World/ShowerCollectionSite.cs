using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Optional fresh-only public catchment beside the existing
    /// allotment. Original beds, materials, paths and cached saves stay literal.</summary>
    public static class ShowerCollectionSite
    {
        internal const string PublicNoticeId = "repair-cultivation:allotment-notice";
        internal const string PublicPlotKey = "morrowfast-public-allotment-v1";
        static readonly HashSet<Zone> Working = new HashSet<Zone>();
        public static bool TryInstall(Zone zone, OverworldZoneManager manager)
        {
            var factory = manager?.Factory;
            // Generation precedes OnZoneAttached: receive its authoritative
            // manager directly, rather than querying the not-yet-live context.
            if (zone?.ZoneID != RepairCultivationSite.ZoneID || manager == null || factory == null
                || string.IsNullOrEmpty(manager.Exploration?.WorldKey)
                || manager.WorldMap.GetBiome(2, 6) != BiomeType.Grovelands || manager.WorldMap.GetPOI(2, 6) != null
                || !factory.Blueprints.ContainsKey("CampAwning") || zone.GetReadOnlyEntitiesWithPart<RainCatchmentPart>().Count != 0) return false;
            var before = zone.GetReadOnlyEntities().ToArray();
            var notices = before.Where(e => e.ID == PublicNoticeId && e.GetProperty(RepairCultivationSite.RoleKey) == "notice").ToArray();
            if (notices.Length != 1) return false;
            var anchor = notices[0]; var cell = zone.GetEntityCell(anchor);
            var ground = cell?.Objects.SingleOrDefault(e => e.HasTag("Terrain"));
            if (cell == null || ground == null || cell.IsInterior || !RainCatchmentPart.FixedOn(anchor, ground, zone)) return false;
            var candidates = (from y in Enumerable.Range(Math.Max(1, cell.Y - 2), Math.Min(Zone.Height - 2, cell.Y + 2) - Math.Max(1, cell.Y - 2) + 1)
                              from x in Enumerable.Range(Math.Max(1, cell.X - 2), Math.Min(Zone.Width - 2, cell.X + 2) - Math.Max(1, cell.X - 2) + 1)
                              let c = zone.GetCell(x, y)
                              where c != cell && Clear(zone, c)
                              orderby Math.Abs(y - (cell.Y - 1)), -Math.Abs(x - cell.X), x
                              select c).Take(2).ToArray();
            if (candidates.Length != 2) return false;
            var source = SpreadGenerationReceipt.CaptureFinalState(zone, before);
            var frames = new List<Entity>(); var checks = new List<Func<bool>>();
            var added = new List<Entity>(); RainCatchmentPart claim = null;
            bool complete = false;
            if (!Working.Add(zone)) return false;
            try
            {
                foreach (var target in candidates)
                {
                    var frame = factory.CreateEntity("CampAwning");
                    if (frame == null || frame.BlueprintName != "CampAwning" || frame.SpatialZone != null || frame.GetPart<PhysicsPart>() is not PhysicsPart physics
                        || physics.ParentEntity != frame || physics.InInventory != null || physics.Equipped != null
                        || frame.GetPart<PackableFixturePart>() is not PackableFixturePart kit || kit.Kind != CampKinds.Awning
                        || frame.GetPart<RenderPart>() is not RenderPart render || frame.GetPart<ExaminablePart>() is not ExaminablePart examine
                        || frame.Parts.Any(p => p.ParentEntity != frame) || !source()) return false;
                    frame.RemovePart(kit); physics.Takeable = false; physics.Solid = false;
                    render.DisplayName = "public catch-cloth frame";
                    examine.Text = "Waxed cloth on a fixed public frame gathers a little clean rainwater. Both frames share the allotment's catchment. During mild showers, press C here and choose a carried flask or waterskin. Ordinary water sources remain useful in fair weather.";
                    var collectorGround = target.Objects.Single(e => e.HasTag("Terrain"));
                    var collector = new RainCollectorPart { Anchor = anchor, Ground = collectorGround, Clean = true };
                    frame.AddPart(collector);
                    var parts = frame.Parts.ToArray(); string identity = frame.ID;
                    string description = examine.Text, label = render.DisplayName;
                    checks.Add(() => frame.ID == identity && frame.BlueprintName == "CampAwning" && frame.Parts.SequenceEqual(parts)
                        && frame.Parts.All(p => p.ParentEntity == frame) && frame.GetPart<PhysicsPart>() == physics && !physics.Takeable && !physics.Solid
                        && frame.GetPart<RenderPart>() == render && render.Visible && render.DisplayName == label
                        && frame.GetPart<ExaminablePart>() == examine && examine.Text == description
                        && frame.GetPart<RainCollectorPart>() == collector && collector.Anchor == anchor && collector.Ground == collectorGround && collector.Clean
                        && physics.InInventory == null && physics.Equipped == null
                        && (frame.SpatialZone == null || frame.SpatialZone == zone && zone.GetEntityCell(frame) == target));
                    frames.Add(frame);
                }
                if (!source() || !checks.All(check => check()) || !candidates.All(c => Clear(zone, c)) || frames.Select(e => e.ID).Distinct().Count() != frames.Count
                    || frames.Any(e => before.Any(old => old.ID == e.ID)) || WorldClock.CurrentTick < 0) return false;
                claim = new RainCatchmentPart { Ground = ground, WorldKey = manager.Exploration.WorldKey,
                    PlotKey = PublicPlotKey, Public = true, PolicyVersion = RegionalWeather.PolicyVersion, ActivatedAt = WorldClock.CurrentTick };
                anchor.AddPart(claim);
                for (int i = 0; i < frames.Count; i++)
                {
                    if (!Clear(zone, candidates[i]) || !zone.AddEntity(frames[i], candidates[i].X, candidates[i].Y)) return false;
                    added.Add(frames[i]);
                }
                if (!checks.All(check => check()) || !RainCatchmentPart.FixedOn(anchor, ground, zone)
                    || anchor.GetPart<RainCatchmentPart>() != claim
                    || frames.Any(frame => !RainCatchmentPart.FixedOn(frame, frame.GetPart<RainCollectorPart>().Ground, zone))
                    || manager.Exploration.WorldKey != claim.WorldKey) return false;
                complete = true;
                CavesOfOoo.Diagnostics.Diag.Record("worldgen", "ShowerCatchmentPlaced", target: anchor,
                    payload: new { plot = claim.PlotKey, frames = frames.Count });
                return true;
            }
            finally
            {
                Working.Remove(zone);
                if (!complete)
                {
                    foreach (var frame in added) if (frame.SpatialZone == zone) zone.RemoveEntity(frame);
                    if (claim?.ParentEntity == anchor && anchor.GetPart<RainCatchmentPart>() == claim) anchor.RemovePart(claim);
                }
            }
        }
        static bool Clear(Zone zone, Cell cell) => cell != null && !cell.IsInterior && !zone.GenReservedCells.Contains((cell.X, cell.Y))
            && !zone.TileState.Has(cell.X, cell.Y) && cell.Objects.Count == 1 && cell.Objects[0].HasTag("Terrain")
            && cell.Objects[0].BlueprintName == "Grass" && !cell.BlocksMovement();
    }
}
