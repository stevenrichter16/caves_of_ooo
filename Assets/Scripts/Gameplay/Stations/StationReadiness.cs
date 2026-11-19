using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// One readiness evaluation for every station kind (Docs/MACHINE-STATIONS.md, spec C5). The strict clauses are the forge's, moved verbatim from
    /// <c>ForgeWorkReceipt.Ready</c>: current zone, a placed owner whose cell round-trips, exactly one parented provider Part of the kind, no repair fault,
    /// not being hauled, not harvested away, not destroyed; then every <see cref="IStationReadinessGuard"/> on the owner (the rig payload).
    /// </summary>
    public static class StationReadiness
    {
        public const string StaleZone = "stale-zone", CellMismatch = "cell-mismatch", NoProvider = "no-provider", TwoProviders = "two-providers",
            Faulted = "faulted", Hauled = "hauled", Harvested = "harvested", Destroyed = "destroyed";

        /// <summary>True when the zone is the manager's current cached instance (or is not managed at all).</summary>
        public static bool CurrentZone(Zone zone)
        {
            if (zone == null) return false;
            var manager = WorldLocationContext.For(zone);
            return manager == null || manager.CachedZones.TryGetValue(zone.ZoneID, out var current) && current == zone;
        }

        /// <summary>The single provider Part of <paramref name="kind"/> on the owner, or null (none, or more than one). Providers are found by capability.</summary>
        public static IStationCapability Provider(Entity owner, string kind, out string code)
        {
            code = null; IStationCapability found = null; int count = 0;
            if (owner == null || kind == null) { code = NoProvider; return null; }
            var parts = owner.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                if (!(parts[i] is IStationCapability capability)) continue;
                var kinds = capability.Kinds; bool has = false;
                for (int k = 0; k < kinds.Count; k++) if (kinds[k] == kind) { has = true; break; }
                if (!has) continue;
                count++; found = capability;
            }
            if (count == 0) { code = NoProvider; return null; }
            if (count > 1) { code = TwoProviders; return null; }
            if (!(found is Part part) || part.ParentEntity != owner) { code = NoProvider; return null; }
            return found;
        }

        /// <summary>Strict readiness. <paramref name="code"/> is null when ready, else a stable token naming the first failed clause.</summary>
        public static bool Evaluate(Entity owner, Zone zone, string kind, out string code)
        {
            code = null;
            if (owner == null || zone == null) { code = CellMismatch; return false; }
            var cell = zone.GetEntityCell(owner);
            if (!CurrentZone(zone)) { code = StaleZone; return false; }
            if (cell == null || owner.SpatialZone != zone || !cell.Objects.Contains(owner) || cell.ParentZone != zone || zone.GetCell(cell.X, cell.Y) != cell) { code = CellMismatch; return false; }
            if (Provider(owner, kind, out code) == null) return false;
            if (RepairablePart.BlocksFunction(owner)) { code = Faulted; return false; }
            if (owner.HasPart<DraggedPart>()) { code = Hauled; return false; }
            if (owner.GetPart<HarvestablePart>()?.Harvested == true) { code = Harvested; return false; }
            if (owner.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP <= 0)) { code = Destroyed; return false; }
            var parts = owner.Parts;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] is IStationReadinessGuard guard && guard.TryBlock(owner, zone, out string guardCode)) { code = guardCode ?? "blocked"; return false; }
            return true;
        }
    }
}
