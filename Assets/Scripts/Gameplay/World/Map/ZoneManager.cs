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
            if (CachedZones.TryGetValue(zoneID, out Zone zone))
                return zone;

            zone = GenerateZone(zoneID);
            if (zone != null)
                CachedZones[zoneID] = zone;
            return zone;
        }

        public void SetActiveZone(string zoneID)
        {
            ActiveZone = GetZone(zoneID);
        }

        public void SetActiveZone(Zone zone)
        {
            CachedZones[zone.ZoneID] = zone;
            ActiveZone = zone;
        }

        private Zone GenerateZone(string zoneID)
        {
            var zone = new Zone(zoneID);
            var rng = new System.Random(WorldSeed ^ zoneID.GetHashCode());

            var pipeline = GetPipelineForZone(zoneID);
            bool success = pipeline.Generate(zone, Factory, rng);

            return success ? zone : null;
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

        public void UnloadZone(string zoneID)
        {
            CachedZones.Remove(zoneID);
            if (ActiveZone?.ZoneID == zoneID)
                ActiveZone = null;
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
