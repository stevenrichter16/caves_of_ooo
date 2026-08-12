using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Generates desert terrain: mostly open sand with sparse rock formations.
    /// Uses noise to place scattered wall clusters at high threshold.
    /// Priority: NORMAL (2000) — after borders, before connectivity.
    /// </summary>
    public class DesertBuilder : IZoneBuilder
    {
        public string Name => "DesertBuilder";
        public int Priority => 2000;

        public string SandBlueprint = "Sand";
        public string SandstoneWallBlueprint = "SandstoneWall";
        public string RockBlueprint = "Rock";
        public float WallThreshold = 0.85f;
        public float RockChance = 0.05f;

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            // Generate noise field for wall placement
            var noise = SimpleNoise.GenerateField(Zone.Width, Zone.Height, rng);

            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    // Clear any existing walls placed by BorderBuilder interior
                    ClearCell(zone, x, y);

                    if (noise[x, y] >= WallThreshold)
                    {
                        // High noise peaks become sandstone walls
                        BuilderSpawn.TryPlace(zone, factory, SandstoneWallBlueprint, x, y);
                    }
                    else
                    {
                        // Place sand terrain
                        BuilderSpawn.TryPlace(zone, factory, SandBlueprint, x, y);

                        // Scatter rocks
                        if (rng.NextDouble() < RockChance)
                        {
                            BuilderSpawn.TryPlace(zone, factory, RockBlueprint, x, y);
                        }

                        // Scatter cacti
                        if (rng.NextDouble() < 0.03)
                        {
                            BuilderSpawn.TryPlace(zone, factory, "Cactus", x, y);
                        }
                    }
                }
            }

            return true;
        }

        private void ClearCell(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                if (cell.Objects[i].HasTag("Wall") || cell.Objects[i].HasTag("Terrain"))
                    zone.RemoveEntity(cell.Objects[i]);
            }
        }
    }
}
