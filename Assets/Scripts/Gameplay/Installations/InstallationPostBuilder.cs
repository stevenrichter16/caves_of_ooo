using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Generic, optional, fresh-only placement of existing machine blueprints into a village (Docs/MACHINE-INSTALLATIONS.md, spec C3).
    /// Runs last (priority 4650, unique in every village pipeline) on the final graph: residents, furniture, stock, doors, stairs,
    /// reserved cells and the public route keep first claim. It never reads or advances the pipeline rng, never returns false (a false
    /// would clear the town and retry), creates each entity exactly once, and omits only itself with a named reason.
    /// Placement is deterministic from (zone id, spec id): candidates are ranked by distance from the anchor, then an FNV hash, then y, x.
    /// </summary>
    public sealed class InstallationPostBuilder : IZoneBuilder
    {
        public string Name => "Installations";
        public int Priority => 4650;
        readonly OverworldZoneManager manager;
        readonly PointOfInterest poi;
        readonly string zoneID;

        public InstallationPostBuilder(OverworldZoneManager manager, PointOfInterest poi, string zoneID)
        { this.manager = manager; this.poi = poi; this.zoneID = zoneID; }

        bool Authority(Zone zone, EntityFactory factory)
        {
            if (zone == null || manager == null || poi == null || factory == null) return false;
            if (zone.ZoneID != zoneID || !ReferenceEquals(factory, manager.Factory)) return false;
            if (manager.CachedZones.ContainsKey(zoneID) || ReferenceEquals(manager.ActiveZone, zone)) return false;
            var (x, y, z) = WorldMap.FromZoneID(zoneID);
            return z == 0 && ReferenceEquals(manager.WorldMap?.GetPOI(x, y), poi) && poi.Type == POIType.Village;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            // A silent no-op, not an omission: a cached, active or foreign zone is a replay, not a failure to place.
            if (!Authority(zone, factory)) return true;
            foreach (var spec in InstallationRegistry.For(poi.Name))
            {
                try { PlaceOne(zone, factory, spec); }
                catch (Exception error) { Omit(spec, "exception", new { error = error.GetType().Name }); }
            }
            return true;
        }

        void Omit(InstallationSpec spec, string reason, object extra = null)
        { if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "InstallationOmitted", payload: new { id = spec.Id, place = spec.Place, zone = zoneID, reason, extra }); }

        static bool BlueprintIsSolid(EntityFactory factory, string blueprint)
            => factory.Blueprints.TryGetValue(blueprint, out var bp) && bp.Parts.TryGetValue("Physics", out var physics) && physics != null
               && physics.TryGetValue("Solid", out var solid) && solid == "true";

        void PlaceOne(Zone zone, EntityFactory factory, InstallationSpec spec)
        {
            // 2. replay guard
            if (zone.GetReadOnlyEntities().Any(e => e.GetPart<InstallationMarkerPart>()?.InstallationId == spec.Id)) { Omit(spec, "already-present"); return; }
            // 3. admission, blueprint level (nothing is created)
            if (InstallationRegistry.AdmissionIssue(factory, spec) != null) { Omit(spec, "content"); return; }
            // 4. anchor: first resolvable owner among Near, lowest (y, x)
            (int x, int y)? anchor = null; string anchorBlueprint = null;
            foreach (var near in spec.Near)
            {
                foreach (var e in zone.GetReadOnlyEntities())
                {
                    if (e.BlueprintName != near) continue;
                    var at = zone.GetEntityPosition(e);
                    if (anchor == null || (at.y, at.x).CompareTo((anchor.Value.y, anchor.Value.x)) < 0) { anchor = (at.x, at.y); anchorBlueprint = near; }
                }
                if (anchor != null) break;
            }
            if (anchor == null) { Omit(spec, "no-anchor"); return; }

            // 5. candidates
            bool solid = BlueprintIsSolid(factory, spec.Blueprint);
            var geometry = new SpreadWildernessSituationBuilder.Geometry(zone, new HashSet<Entity>());
            var candidates = new List<(int distance, int rank, int y, int x)>();
            for (int y = anchor.Value.y - spec.Radius; y <= anchor.Value.y + spec.Radius; y++)
                for (int x = anchor.Value.x - spec.Radius; x <= anchor.Value.x + spec.Radius; x++)
                    if (geometry.In(x, y) && Math.Max(Math.Abs(x - anchor.Value.x), Math.Abs(y - anchor.Value.y)) >= spec.MinRadius)
                        candidates.Add((Math.Max(Math.Abs(x - anchor.Value.x), Math.Abs(y - anchor.Value.y)),
                            FormationSelector.StableIndex(zoneID + ":install:" + spec.Id + ":" + x + "," + y, 1 << 30), y, x));
            candidates.Sort((a, b) => { int c = a.distance.CompareTo(b.distance); if (c != 0) return c; c = a.rank.CompareTo(b.rank); return c != 0 ? c : (a.y, a.x).CompareTo((b.y, b.x)); });

            int probed = 0; (int x, int y)? chosen = null;
            foreach (var cand in candidates)
            {
                if (probed++ >= spec.MaxProbe) break;
                if (Fits(zone, geometry, spec, solid, cand.x, cand.y)) { chosen = (cand.x, cand.y); break; }
            }
            if (chosen == null) { Omit(spec, "no-cell", new { probed = Math.Min(probed, spec.MaxProbe) }); return; }

            // 6. create exactly once
            Entity entity;
            try { entity = factory.CreateEntity(spec.Blueprint); }
            catch (Exception) { Omit(spec, "create-failed"); return; }
            if (entity == null || string.IsNullOrEmpty(entity.ID) || entity.HasPart<InventoryPart>() || entity.HasPart<ContainerPart>() || entity.HasPart<TraderPart>() || entity.HasPart<StackerPart>())
            {
                if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "InstallationRejected", payload: new { id = spec.Id, place = spec.Place, zone = zoneID, reason = "fresh" });
                return;
            }

            // 7. stage and commit as a receipt: nothing that existed may change. Anything that goes wrong after the add (a mismatch or an exception) removes the entity again.
            var originals = zone.GetReadOnlyEntities().ToArray();
            var intact = SpreadGenerationReceipt.CaptureFinalState(zone, originals);
            string tiles = zone.TileState.ToSaveString(); var reserves = new HashSet<(int, int)>(zone.GenReservedCells); int before = zone.EntityCount;
            entity.AddPart(new InstallationMarkerPart { InstallationId = spec.Id, Origin = "Generated", Revision = spec.Revision, RetainZone = spec.Retain });
            entity.Properties["SettlementId"] = zoneID;
            bool added = false, committed = false;
            try
            {
                added = zone.AddEntity(entity, chosen.Value.x, chosen.Value.y);
                committed = added && intact() && tiles == zone.TileState.ToSaveString() && reserves.SetEquals(zone.GenReservedCells)
                            && zone.EntityCount == before + 1 && zone.GetEntityCell(entity) == zone.GetCell(chosen.Value.x, chosen.Value.y);
            }
            finally { if (added && !committed) zone.RemoveEntity(entity); }
            if (!committed) { Omit(spec, "displaced-original"); return; }

            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "InstallationPlaced", target: entity, payload: new { id = spec.Id, place = spec.Place, zone = zoneID, x = chosen.Value.x, y = chosen.Value.y, blueprint = spec.Blueprint, anchor = anchorBlueprint, probed });
        }

        static bool Fits(Zone zone, SpreadWildernessSituationBuilder.Geometry geometry, InstallationSpec spec, bool solid, int x, int y)
        {
            if (!geometry.Place(x, y)) return false;                       // exterior, dry, unreserved bare ground, no stairs
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.Objects.Count != 1 || !spec.Ground.Contains(cell.Objects[0].BlueprintName)) return false;   // never RoadStone, the public route
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var near = zone.GetCell(x + dx, y + dy); if (near == null) continue;
                    foreach (var e in near.Objects)
                        if (e.HasPart<DoorPart>() || e.HasPart<StairsUpPart>() || e.HasPart<StairsDownPart>() || e.BlueprintName == "Well") return false;
                }
            if (!solid) return true;                                       // a forge or still is not solid: the route and standing cell checks are vacuous
            if (!geometry.PreservesRoutes(new[] { (x, y) })) return false;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dy != 0) && geometry.In(x + dx, y + dy) && geometry.BorderReach[x + dx, y + dy]) return true;
            return false;
        }
    }
}
