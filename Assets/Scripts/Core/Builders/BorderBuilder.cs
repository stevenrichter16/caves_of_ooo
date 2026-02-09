using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Fills the zone border with Wall entities.
    /// Priority: EARLY (1000) -- borders must be laid down first.
    /// </summary>
    public class BorderBuilder : IZoneBuilder
    {
        public string Name => "BorderBuilder";
        public int Priority => 1000;
        public string WallBlueprint = "Wall";

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    bool isBorder = x == 0 || x == Zone.Width - 1 || y == 0 || y == Zone.Height - 1;
                    if (isBorder)
                    {
                        Entity wall = factory.CreateEntity(WallBlueprint);
                        if (wall != null)
                            zone.AddEntity(wall, x, y);
                    }
                }
            }
            return true;
        }
    }
}
