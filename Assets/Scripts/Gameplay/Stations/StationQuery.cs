using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Finds stations by capability. Replaces the separate 3x3 scans in ForgeWorkReceipt and AlchemyStillPart with one scan order (dx outer, dy inner, then the
    /// cell's object order) so existing tie-breaking is unchanged. Menu/action/generation time only; no per-frame use.</summary>
    public static class StationQuery
    {
        static bool ActorPlaced(Entity actor, Zone zone)
        {
            if (actor == null || zone == null) return false;
            var cell = zone.GetEntityCell(actor);
            return cell != null && actor.SpatialZone == zone && cell.Objects.Contains(actor) && cell.ParentZone == zone && zone.GetCell(cell.X, cell.Y) == cell;
        }

        /// <summary>The first ready provider of <paramref name="kind"/> in the actor's 3x3, in the legacy scan order, or null.</summary>
        public static Entity Resolve(Entity actor, Zone zone, string kind)
        {
            if (actor == null || zone == null) return null;
            var at = zone.GetEntityCell(actor); if (at == null || actor.SpatialZone != zone || !StationReadiness.CurrentZone(zone)) return null;
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
            {
                var cell = zone.GetCell(at.X + dx, at.Y + dy); if (cell == null) continue;
                foreach (var owner in cell.Objects) if (StationReadiness.Evaluate(owner, zone, kind, out _)) return owner;
            }
            return null;
        }

        /// <summary>True when the actor is placed, the station is ready for <paramref name="kind"/> and within the provider's reach.</summary>
        public static bool Near(Entity actor, Entity owner, Zone zone, string kind)
        {
            if (!ActorPlaced(actor, zone) || !StationReadiness.Evaluate(owner, zone, kind, out _)) return false;
            var provider = StationReadiness.Provider(owner, kind, out _);
            return provider != null && SpatialQuery.Distance(zone, actor, owner) <= provider.Reach;
        }

        /// <summary>The older presence-only rule (the alchemy still): the actor's position is read directly and any object in the 3x3 that provides the kind counts.
        /// No zone-current check and no readiness. Kept bit-for-bit so the still's behaviour is unchanged.</summary>
        public static bool NearPresence(Entity actor, Zone zone, string kind)
        {
            if (actor == null || zone == null) return false;
            (int x, int y) = zone.GetEntityPosition(actor);
            if (x < 0 || y < 0) return false;
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
            {
                Cell cell = zone.GetCell(x + dx, y + dy); if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    Entity obj = cell.Objects[i]; if (obj == null) continue;
                    var parts = obj.Parts;
                    for (int p = 0; p < parts.Count; p++)
                        if (parts[p] is IStationCapability capability && capability.Readiness == ReadinessMode.PresenceOnly && HasKind(capability, kind) && !GuardBlocks(obj, zone)) return true;
                }
            }
            return false;
        }

        /// <summary>Presence-only stations still honour a readiness guard (a kit that is not set down is no station). An object with no guard is unaffected, so every authored station behaves as before.</summary>
        static bool GuardBlocks(Entity owner, Zone zone)
        {
            var parts = owner.Parts;
            for (int i = 0; i < parts.Count; i++) if (parts[i] is IStationReadinessGuard guard && guard.TryBlock(owner, zone, out _)) return true;
            return false;
        }

        static bool HasKind(IStationCapability capability, string kind)
        {
            var kinds = capability.Kinds; if (kinds == null) return false;
            for (int i = 0; i < kinds.Count; i++) if (kinds[i] == kind) return true;
            return false;
        }

        /// <summary>Every provider of the kind in the 3x3 with the reason it is not ready (null = ready). Scratch-list API, no LINQ.</summary>
        public static void Collect(Entity actor, Zone zone, string kind, List<StationCandidate> into)
        {
            if (into == null) return; into.Clear();
            if (actor == null || zone == null) return;
            var at = zone.GetEntityCell(actor); if (at == null) return;
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
            {
                var cell = zone.GetCell(at.X + dx, at.Y + dy); if (cell == null) continue;
                foreach (var owner in cell.Objects)
                {
                    if (StationReadiness.Provider(owner, kind, out string providerCode) == null && providerCode == StationReadiness.NoProvider) continue;
                    StationReadiness.Evaluate(owner, zone, kind, out string code); into.Add(new StationCandidate(owner, code));
                }
            }
        }
    }

    /// <summary>The permission seam: loops the <see cref="IStationAccessGuard"/> Parts on a station. No guard means allowed.</summary>
    public static class StationAccess
    {
        public static bool Allowed(Entity actor, Entity station, Zone zone, string kind, string command, out string code, out string reason)
        {
            code = null; reason = null; if (station == null) return true;
            var parts = station.Parts;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] is IStationAccessGuard guard && guard.TryDeny(actor, station, zone, kind, command, out code, out reason)) return false;
            return true;
        }
    }
}
