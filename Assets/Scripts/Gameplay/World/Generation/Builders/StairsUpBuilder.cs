using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Places StairsUp entities by reading zone connections from the level above.
    /// If the zone above has registered a StairsDown connection targeting this zone,
    /// StairsUp is placed at the matching coordinates.
    /// Priority: 3500 (after connectivity, before stair connector).
    /// </summary>
    public class StairsUpBuilder : IZoneBuilder
    {
        public string Name => "StairsUpBuilder";
        public int Priority => 3500;

        private ZoneManager _zoneManager;

        public StairsUpBuilder(ZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (_zoneManager == null) return true;

            // Find connections from the zone above that target this zone
            List<ZoneConnection> connections = _zoneManager.GetConnectionsTo(zone.ZoneID, "StairsDown");

            for (int i = 0; i < connections.Count; i++)
            {
                var conn = connections[i];
                int x = conn.TargetX;
                int y = conn.TargetY;

                // Clear any wall at this position so stairs are accessible
                ClearWalls(zone, x, y);

                Entity stairs = factory.CreateEntity("StairsUp");
                if (stairs == null) continue;

                zone.AddEntity(stairs, x, y);
            }

            return true;
        }

        private void ClearWalls(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;

            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                if (cell.Objects[i].HasTag("Wall") || cell.Objects[i].HasTag("Solid"))
                    zone.RemoveEntity(cell.Objects[i]);
            }
        }
    }
}
