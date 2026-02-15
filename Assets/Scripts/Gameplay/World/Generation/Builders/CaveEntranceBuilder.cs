using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Places a StairsDown (cave entrance) on a surface zone.
    /// Each surface zone has a 50% chance of having a cave entrance.
    /// Priority: 3500 (after connectivity).
    /// </summary>
    public class CaveEntranceBuilder : IZoneBuilder
    {
        public string Name => "CaveEntranceBuilder";
        public int Priority => 3500;

        private ZoneManager _zoneManager;

        public CaveEntranceBuilder(ZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            // Only place on surface zones
            int depth = WorldMap.GetDepth(zone.ZoneID);
            if (depth > 0) return true;

            // 50% chance of having a cave entrance
            if (rng.Next(100) >= 50) return true;

            // Find a passable cell
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int x = rng.Next(3, Zone.Width - 3);
                int y = rng.Next(3, Zone.Height - 3);

                var cell = zone.GetCell(x, y);
                if (cell == null || !cell.IsPassable()) continue;

                Entity stairs = factory.CreateEntity("StairsDown");
                if (stairs == null) return true;

                zone.AddEntity(stairs, x, y);

                // Register connection to z=1
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

            return true; // Not fatal if placement fails
        }
    }
}
