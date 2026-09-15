using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Tracks a connection between two zones (e.g., stairs linking floors).
    /// </summary>
    public class ZoneConnection
    {
        public string SourceZoneID;
        public int SourceX, SourceY;
        public string TargetZoneID;
        public int TargetX, TargetY;
        public string Type; // "StairsDown", "StairsUp"
    }

    /// <summary>
    /// Manages zone lifecycle: generation, caching, and transitions.
    /// Mirrors Qud's ZoneManager: zones are identified by string IDs,
    /// cached after generation, and retrieved on demand.
    /// </summary>
    public class ZoneManager
    {
        public Dictionary<string, Zone> CachedZones = new Dictionary<string, Zone>();
        public Zone ActiveZone { get; private set; }
        public EntityFactory Factory { get; private set; }
        public int WorldSeed { get; private set; }

        private Dictionary<string, List<ZoneConnection>> _connections
            = new Dictionary<string, List<ZoneConnection>>();

        public ZoneManager(EntityFactory factory, int worldSeed = 0)
        {
            Factory = factory;
            WorldSeed = worldSeed == 0 ? Environment.TickCount : worldSeed;
        }

        /// <summary>
        /// Get or generate a zone by ID.
        /// Returns cached if available, otherwise generates and caches.
        /// </summary>
        public Zone GetZone(string zoneID)
        {
            PrepareZoneForAccess(zoneID);

            if (CachedZones.TryGetValue(zoneID, out Zone zone))
            {
                OnZoneAttached(zone);
                return zone;
            }

            zone = GenerateZone(zoneID);
            if (zone != null)
            {
                CachedZones[zoneID] = zone;
                OnZoneAttached(zone);
            }
            return zone;
        }

        public void SetActiveZone(string zoneID)
        {
            ActiveZone = GetZone(zoneID);
        }

        public void SetActiveZone(Zone zone)
        {
            CachedZones[zone.ZoneID] = zone;
            OnZoneAttached(zone);
            ActiveZone = zone;
        }

        /// <summary>Derived, nonpersistent context for generated, accessed or
        /// restored native graphs. This hook must never rebuild their contents.</summary>
        protected virtual void OnZoneAttached(Zone zone) { }

        private Zone GenerateZone(string zoneID)
        {
            var zone = new Zone(zoneID);
            var rng = new System.Random(WorldSeed ^ zoneID.GetHashCode());

            var pipeline = GetPipelineForZone(zoneID);
            bool success = pipeline.Generate(zone, Factory, rng);

            if (success)
                OnZoneGenerated(zone, zoneID);

            return success ? zone : null;
        }

        protected virtual void OnZoneGenerated(Zone zone, string zoneID)
        {
        }

        /// <summary>
        /// Determine which pipeline to use based on zone ID.
        /// All zones currently use the cave pipeline.
        /// Future phases will route by biome/depth.
        /// </summary>
        protected virtual ZoneGenerationPipeline GetPipelineForZone(string zoneID)
        {
            return ZoneGenerationPipeline.CreateCavePipeline(PopulationTable.CaveTier1());
        }

        protected virtual void PrepareZoneForAccess(string zoneID)
        {
        }

        public void UnloadZone(string zoneID)
        {
            CachedZones.Remove(zoneID);
            if (ActiveZone?.ZoneID == zoneID)
                ActiveZone = null;
            // W5.7 close-out 🔵 — an unloaded zone REGENERATES on next
            // access and its builders re-register their connections.
            // Leaving the old ones behind grew the registry by one
            // duplicate per unload/regen cycle (village advances while
            // the player is away), every duplicate was saved forever,
            // and StairsUpBuilder placed one staircase PER duplicate.
            // Drop connections the unloaded zone OWNS (it is the
            // source); connections into it from zones still loaded
            // stay, because those zones will not re-register them.
            if (_connections.TryGetValue(zoneID, out var owned))
            {
                var dropped = new List<ZoneConnection>();
                for (int i = owned.Count - 1; i >= 0; i--)
                    if (owned[i].SourceZoneID == zoneID)
                    { dropped.Add(owned[i]); owned.RemoveAt(i); }
                if (owned.Count == 0) _connections.Remove(zoneID);
                foreach (var conn in dropped)
                    if (_connections.TryGetValue(conn.TargetZoneID, out var tl))
                    {
                        // W6.7 adversarial: save/load creates separate instances in
                        // the two indexes. Identity is the complete route, as at registration.
                        tl.RemoveAll(other => SameConnection(other, conn));
                        if (tl.Count == 0) _connections.Remove(conn.TargetZoneID);
                    }
            }
        }

        private static bool SameConnection(ZoneConnection a, ZoneConnection b)
            => a.SourceZoneID == b.SourceZoneID && a.TargetZoneID == b.TargetZoneID
                && a.SourceX == b.SourceX && a.SourceY == b.SourceY
                && a.TargetX == b.TargetX && a.TargetY == b.TargetY && a.Type == b.Type;

        /// <summary>Remove a complete route from both indexes, including the
        /// distinct reference copies restored by saves. Unrelated routes survive.</summary>
        public void RemoveConnection(ZoneConnection connection)
        {
            if (connection == null) return;
            foreach (string id in new[] { connection.SourceZoneID, connection.TargetZoneID })
                if (_connections.TryGetValue(id, out var list))
                {
                    list.RemoveAll(other => SameConnection(other, connection));
                    if (list.Count == 0) _connections.Remove(id);
                }
        }

        public int CachedZoneCount => CachedZones.Count;

        // --- Zone Connection Registry ---

        /// <summary>
        /// Register a connection between two zones (e.g., stairs).
        /// Indexed by both source and target zone IDs.
        /// </summary>
        public void RegisterConnection(ZoneConnection conn)
        {
            if (!_connections.TryGetValue(conn.SourceZoneID, out var sourceList))
            {
                sourceList = new List<ZoneConnection>();
                _connections[conn.SourceZoneID] = sourceList;
            }
            // W5.7 close-out 🔵 — idempotent by value: a regenerated
            // zone re-registering the same stairs must not stack a
            // duplicate (and a save must not grow monotonically).
            foreach (var existing in sourceList)
                if (SameConnection(existing, conn))
                    return;
            sourceList.Add(conn);

            if (!_connections.TryGetValue(conn.TargetZoneID, out var targetList))
            {
                targetList = new List<ZoneConnection>();
                _connections[conn.TargetZoneID] = targetList;
            }
            targetList.Add(conn);
        }

        /// <summary>
        /// Get all connections involving a zone (as source or target).
        /// </summary>
        public List<ZoneConnection> GetConnections(string zoneID)
        {
            if (_connections.TryGetValue(zoneID, out var list))
                return list;
            return new List<ZoneConnection>();
        }

        public Dictionary<string, List<ZoneConnection>> GetConnectionSnapshot()
        {
            var result = new Dictionary<string, List<ZoneConnection>>();
            foreach (var kvp in _connections)
                result[kvp.Key] = new List<ZoneConnection>(kvp.Value);
            return result;
        }

        public void ReplaceLoadedState(
            Dictionary<string, Zone> cachedZones,
            string activeZoneID,
            Dictionary<string, List<ZoneConnection>> connections)
        {
            CachedZones = cachedZones ?? new Dictionary<string, Zone>();
            _connections = connections ?? new Dictionary<string, List<ZoneConnection>>();
            foreach(var zone in CachedZones.Values) OnZoneAttached(zone);

            if (!string.IsNullOrEmpty(activeZoneID) && CachedZones.TryGetValue(activeZoneID, out Zone active))
                ActiveZone = active;
            else
                ActiveZone = null;
        }

        /// <summary>
        /// Get connections where targetZoneID matches, filtered by type.
        /// Used by StairsUpBuilder to find where stairs from above connect.
        /// </summary>
        public List<ZoneConnection> GetConnectionsTo(string targetZoneID, string type)
        {
            var result = new List<ZoneConnection>();
            if (!_connections.TryGetValue(targetZoneID, out var list))
                return result;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].TargetZoneID == targetZoneID && list[i].Type == type)
                    result.Add(list[i]);
            }
            return result;
        }

        // --- Zone Tier ---

        /// <summary>
        /// Calculate zone tier from depth. Surface = 1, every 3 levels deeper = +1 tier, max 8.
        /// </summary>
        public static int GetZoneTier(string zoneID)
        {
            int z = WorldMap.GetDepth(zoneID);
            if (z <= 0) return 1;
            return Math.Min(z / 3 + 1, 8);
        }
    }
}
