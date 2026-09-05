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

            // W6.7 adversarial: a laterally reached deep floor can precede
            // its parent. Author only this fresh endpoint and an ordinary
            // deferred edge; never recursively generate an underground column.
            string above = WorldMap.GetZoneAbove(zone.ZoneID);
            if (connections.Count == 0 && WorldMap.GetDepth(zone.ZoneID) >= 3 && above != null
                && factory.Blueprints.ContainsKey("StairsUp") && factory.Blueprints.ContainsKey("StairsDown"))
            {
                int sx, sy;
                if (_zoneManager.CachedZones.TryGetValue(above, out var parent))
                {
                    var existing = FindStair(parent, false);
                    if (existing == null) return true; // A cached, removed stair stays removed.
                    var p = parent.GetEntityPosition(existing); sx = p.x; sy = p.y;
                }
                else
                {
                    var p = StairsDownBuilder.FindOpenPosition(zone, rng);
                    if (p.x < 0) return true;
                    sx = p.x; sy = p.y;
                }
                var edge = new ZoneConnection { SourceZoneID = above, SourceX = sx, SourceY = sy,
                    TargetZoneID = zone.ZoneID, TargetX = sx, TargetY = sy, Type = "StairsDown" };
                _zoneManager.RegisterConnection(edge); connections.Add(edge);
            }

            for (int i = 0; i < connections.Count; i++)
            {
                var conn = connections[i];
                if (_zoneManager.CachedZones.TryGetValue(conn.SourceZoneID, out var parent)
                    && !HasStair(parent.GetCell(conn.SourceX, conn.SourceY), false))
                { _zoneManager.RemoveConnection(conn); continue; }
                int x = conn.TargetX;
                int y = conn.TargetY;

                if (HasStair(zone.GetCell(x, y), true)) continue;
                Entity stairs = factory.CreateEntity("StairsUp");
                if (stairs == null) continue;

                // Clear any wall at this position so stairs are accessible
                ClearWalls(zone, x, y);
                zone.AddEntity(stairs, x, y);
            }

            return true;
        }

        internal static bool HasStair(Cell cell, bool up)
        {
            if (cell == null) return false;
            foreach (var e in cell.Objects)
                if (up ? e.HasPart<StairsUpPart>() : e.HasPart<StairsDownPart>()) return true;
            return false;
        }

        internal static Entity FindStair(Zone zone, bool up)
        {
            foreach (var e in zone.GetAllEntities())
                if (up ? e.HasPart<StairsUpPart>() : e.HasPart<StairsDownPart>()) return e;
            return null;
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
