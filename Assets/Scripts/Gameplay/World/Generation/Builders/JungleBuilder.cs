using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Generates jungle terrain: dense vegetation with organic clearings.
    /// Uses cellular automata like CaveBuilder but with lower seed chance
    /// for more open space. Green palette.
    /// Priority: NORMAL (2000) — after borders, before connectivity.
    /// </summary>
    public class JungleBuilder : IZoneBuilder
    {
        public string Name => "JungleBuilder";
        public int Priority => 2000;

        public string GrassBlueprint = "Grass";
        public string VineWallBlueprint = "VineWall";
        public string TreeBlueprint = "Tree";
        public int SeedChance = 48;
        public int CAPasses = 2;
        public float NoiseThreshold = 0.47f;
        public float TreeChance = 0.10f;

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            // 1. Fill interior with vine walls
            FillWithWalls(zone, factory);

            // 2. Generate cellular automata (lower seed = more open)
            var ca = new CellularAutomata(Zone.Width, Zone.Height);
            ca.SeedChance = SeedChance;
            ca.SeedBorders = true;
            ca.BorderDepth = 2;
            ca.Passes = CAPasses;
            ca.Generate(rng);

            // 3. Generate noise field
            var noise = SimpleNoise.GenerateField(Zone.Width, Zone.Height, rng);

            // 4. Carve open spaces
            // Qud has no border walls — CA SeedBorders creates natural wall tendency near edges
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    bool caOpen = ca.IsOpen(x, y);
                    bool noiseOpen = noise[x, y] <= NoiseThreshold;

                    if (caOpen || noiseOpen)
                    {
                        ClearWalls(zone, x, y);
                        PlaceGrass(zone, factory, x, y);

                        // Scatter trees in some open cells
                        if (rng.NextDouble() < TreeChance)
                        {
                            var tree = factory.CreateEntity(TreeBlueprint);
                            if (tree != null)
                                zone.AddEntity(tree, x, y);
                        }
                    }
                }
            }

            return true;
        }

        private void FillWithWalls(Zone zone, EntityFactory factory)
        {
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell.IsWall()) continue;

                    var wall = factory.CreateEntity(VineWallBlueprint);
                    if (wall != null)
                        zone.AddEntity(wall, x, y);
                }
            }
        }

        private void ClearWalls(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                if (cell.Objects[i].HasTag("Wall"))
                    zone.RemoveEntity(cell.Objects[i]);
            }
        }

        private void PlaceGrass(Zone zone, EntityFactory factory, int x, int y)
        {
            int roll = (int)(System.Math.Abs(x * 31 + y * 17) % 100);
            if (roll < 95)
            {
                var grass = factory.CreateEntity(GrassBlueprint);
                if (grass != null)
                    zone.AddEntity(grass, x, y);
            }
        }
    }
}
