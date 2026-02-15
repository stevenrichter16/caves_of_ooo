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
            // Find a passable cell, preferring the center region
            int cx = Zone.Width / 2;
            int cy = Zone.Height / 2;

            for (int radius = 0; radius < Zone.Width; radius++)
            {
                // Try random positions within this radius
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    int x = cx + rng.Next(-radius, radius + 1);
                    int y = cy + rng.Next(-radius, radius + 1);

                    if (x < 1 || x >= Zone.Width - 1 || y < 1 || y >= Zone.Height - 1)
                        continue;

                    var cell = zone.GetCell(x, y);
                    if (cell == null || !cell.IsPassable()) continue;

                    // Don't place on a cell that already has stairs
                    if (HasStairs(cell)) continue;

                    Entity stairs = factory.CreateEntity("StairsDown");
                    if (stairs == null) return true; // Blueprint missing, skip silently

                    zone.AddEntity(stairs, x, y);

                    // Register connection to the zone below
                    if (_zoneManager != null)
                    {
                        string belowID = WorldMap.GetZoneBelow(zone.ZoneID);
                        if (belowID != null)
                        {
                            _zoneManager.RegisterConnection(new ZoneConnection
                            {
                                SourceZoneID = zone.ZoneID,
                                SourceX = x,
                                SourceY = y,
                                TargetZoneID = belowID,
                                TargetX = x,
                                TargetY = y,
                                Type = "StairsDown"
                            });
                        }
                    }

                    return true;
                }
            }

            return true; // Not fatal if we can't place stairs
        }

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
