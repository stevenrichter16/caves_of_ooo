using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Fills an underground zone with solid wall entities.
    /// The wall blueprint varies by depth (sandstone, limestone, shale, etc.).
    /// Priority: 500 (very early — before cave carving).
    /// </summary>
    public class SolidEarthBuilder : IZoneBuilder
    {
        public string Name => "SolidEarthBuilder";
        public int Priority => 500;

        public string WallBlueprint;

        public SolidEarthBuilder(string wallBlueprint)
        {
            WallBlueprint = wallBlueprint;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell.IsWall()) continue;

                    Entity wall = factory.CreateEntity(WallBlueprint);
                    if (wall != null)
                        zone.AddEntity(wall, x, y);
                }
            }
            return true;
        }

        /// <summary>
        /// Get wall and floor blueprint names for a given depth.
        /// </summary>
        public static (string wallBP, string floorBP) GetMaterialsForDepth(int depth)
        {
            if (depth <= 2) return ("SandstoneWall", "SandstoneFloor");
            if (depth <= 4) return ("LimestoneWall", "LimestoneFloor");
            if (depth <= 6) return ("ShaleWall", "ShaleFloor");
            if (depth <= 8) return ("SlateWall", "SlateFloor");
            if (depth <= 10) return ("QuartziteWall", "QuartziteFloor");
            return ("ObsidianWall", "ObsidianFloor");
        }
    }
}
