using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
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
    }
}
