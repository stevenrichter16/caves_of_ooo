using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Places a StairsDown entity in an underground zone and registers
    /// a zone connection to the level below.
    /// Priority: 3500 (after connectivity, before stair connector).
    /// </summary>
    public class StairsDownBuilder : IZoneBuilder
    {
        public string Name => "StairsDownBuilder";
        public int Priority => 3500;

        private ZoneManager _zoneManager;

        public StairsDownBuilder(ZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            string belowID = WorldMap.GetZoneBelow(zone.ZoneID);
            ZoneConnection pending = null;
            if (_zoneManager != null && belowID != null)
                foreach (var edge in _zoneManager.GetConnections(zone.ZoneID))
                    if (edge.SourceZoneID == zone.ZoneID && edge.TargetZoneID == belowID && edge.Type == "StairsDown")
                    { pending = edge; break; }

            // The lower endpoint may already be player-modified. Pair only
            // with a surviving stair; never replay a builder on that cached zone.
            int targetX = -1, targetY = -1;
            if (_zoneManager != null && belowID != null && _zoneManager.CachedZones.TryGetValue(belowID, out var below))
            {
                if (pending != null && StairsUpBuilder.HasStair(below.GetCell(pending.TargetX, pending.TargetY), true))
                { targetX = pending.TargetX; targetY = pending.TargetY; }
                else
                {
                    var up = StairsUpBuilder.FindStair(below, true);
                    if (up == null)
                    { if (pending != null) _zoneManager.RemoveConnection(pending); return true; }
                    var p = below.GetEntityPosition(up); targetX = p.x; targetY = p.y;
                }
            }
            else if (pending != null)
            { targetX = pending.TargetX; targetY = pending.TargetY; }

            var pos = pending != null && CanPlace(zone.GetCell(pending.SourceX, pending.SourceY))
                ? (x: pending.SourceX, y: pending.SourceY) : FindOpenPosition(zone, rng);
            if (pos.x < 0) return true;
            Entity stairs = factory.CreateEntity("StairsDown");
            if (stairs == null || !zone.AddEntity(stairs, pos.x, pos.y)) return true;
            if (_zoneManager != null && belowID != null)
            {
                if (pending != null) _zoneManager.RemoveConnection(pending);
                _zoneManager.RegisterConnection(new ZoneConnection
                {
                    SourceZoneID = zone.ZoneID, SourceX = pos.x, SourceY = pos.y,
                    TargetZoneID = belowID, TargetX = targetX < 0 ? pos.x : targetX,
                    TargetY = targetY < 0 ? pos.y : targetY, Type = "StairsDown"
                });
            }
            return true;
        }

        // Shared only by cold fresh-generation builders. Keep the original
        // center-biased random search for ordinary top-down generation.
        internal static (int x, int y) FindOpenPosition(Zone zone, System.Random rng)
        {
            int cx = Zone.Width / 2, cy = Zone.Height / 2;
            for (int radius = 0; radius < Zone.Width; radius++)
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    int x = cx + rng.Next(-radius, radius + 1);
                    int y = cy + rng.Next(-radius, radius + 1);
                    if (x < 1 || x >= Zone.Width - 1 || y < 1 || y >= Zone.Height - 1) continue;
                    if (CanPlace(zone.GetCell(x, y))) return (x, y);
                }
            return (-1, -1);
        }

        private static bool CanPlace(Cell cell) => cell != null && cell.IsPassable() && !HasStairs(cell);

        private static bool HasStairs(Cell cell)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (cell.Objects[i].HasTag("StairsDown") || cell.Objects[i].HasTag("StairsUp"))
                    return true;
            }
            return false;
        }
    }
}
