using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Carves caves in underground zones using cellular automata + noise.
    /// Parameters vary by depth: shallow caves are open, deep caves are tight.
    /// Mirrors CaveBuilder but with depth-driven parameterization.
    /// Priority: 1000 (after SolidEarthBuilder, before connectivity).
    /// </summary>
    public class StrataBuilder : IZoneBuilder
    {
        public string Name => "StrataBuilder";
        public int Priority => 1000;

        private int _depth;
        private string _wallBlueprint;
        private string _floorBlueprint;

        public StrataBuilder(int depth, string wallBlueprint, string floorBlueprint)
        {
            _depth = depth;
            _wallBlueprint = wallBlueprint;
            _floorBlueprint = floorBlueprint;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            // Depth-driven parameters
            int seedChance;
            int passes;
            float noiseThreshold;

            if (_depth <= 4)
            {
                // Shallow: open caves
                seedChance = 55;
                passes = 2;
                noiseThreshold = 0.47f;
            }
            else if (_depth <= 8)
            {
                // Mid: tighter caves
                seedChance = 60;
                passes = 3;
                noiseThreshold = 0.42f;
            }
            else
            {
                // Deep: narrow tunnels
                seedChance = 65;
                passes = 3;
                noiseThreshold = 0.38f;
            }

            // Generate cellular automata
            var ca = new CellularAutomata(Zone.Width, Zone.Height);
            ca.SeedChance = seedChance;
            ca.SeedBorders = true;
            ca.BorderDepth = 2;
            ca.Passes = passes;
            ca.Generate(rng);

            // Generate noise field
            float[,] noise = SimpleNoise.GenerateField(Zone.Width, Zone.Height, rng);

            // Carve open spaces (same logic as CaveBuilder)
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    bool caOpen = ca.IsOpen(x, y);
                    bool noiseOpen = noise[x, y] <= noiseThreshold;

                    if (caOpen || noiseOpen)
                    {
                        ClearWalls(zone, x, y);
                        PlaceFloor(zone, factory, rng, x, y);
                    }
                }
            }

            return true;
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

        private void PlaceFloor(Zone zone, EntityFactory factory, System.Random rng, int x, int y)
        {
            int roll = rng.Next(100);
            if (roll < 85)
            {
                Entity floor = factory.CreateEntity(_floorBlueprint);
                if (floor != null)
                    zone.AddEntity(floor, x, y);
            }
            else if (roll < 95)
            {
                Entity rubble = factory.CreateEntity("Rubble");
                if (rubble != null)
                    zone.AddEntity(rubble, x, y);
            }
            // 5% empty
        }
    }
}
