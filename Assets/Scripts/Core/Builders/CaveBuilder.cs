using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Generates cave terrain using cellular automata + noise.
    /// Faithful to Qud's Cave builder:
    /// 1. Fill zone with walls
    /// 2. Run CellularAutomata
    /// 3. Overlay noise field -- cells where CA is open OR noise less than threshold are cleared
    /// 4. Place floor/rubble entities in open cells
    ///
    /// Priority: NORMAL (2000) -- after borders, before connectivity.
    /// </summary>
    public class CaveBuilder : IZoneBuilder
    {
        public string Name => "CaveBuilder";
        public int Priority => 2000;

        public int SeedChance = 55;
        public int CAPasses = 2;
        public float NoiseThreshold = 0.47f;
        public bool UseNoise = true;
        public string WallBlueprint = "Wall";
        public string FloorBlueprint = "Floor";
        public string RubbleBlueprint = "Rubble";

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            // 1. Fill entire zone with walls
            FillWithWalls(zone, factory);

            // 2. Generate cellular automata
            var ca = new CellularAutomata(Zone.Width, Zone.Height);
            ca.SeedChance = SeedChance;
            ca.SeedBorders = true;
            ca.BorderDepth = 2;
            ca.Passes = CAPasses;
            ca.Generate(rng);

            // 3. Generate noise field
            float[,] noise = null;
            if (UseNoise)
                noise = SimpleNoise.GenerateField(Zone.Width, Zone.Height, rng);

            // 4. Carve open spaces (skip border cells)
            // Faithful to Qud: if CA cell is open OR noise <= threshold, clear
            for (int x = 1; x < Zone.Width - 1; x++)
            {
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    bool caOpen = ca.IsOpen(x, y);
                    bool noiseOpen = noise != null && noise[x, y] <= NoiseThreshold;

                    if (caOpen || noiseOpen)
                    {
                        ClearWalls(zone, x, y);
                        PlaceFloorOrRubble(zone, factory, rng, x, y);
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
                    // Skip if the cell already has a wall (e.g., from BorderBuilder)
                    var cell = zone.GetCell(x, y);
                    if (cell.IsWall()) continue;

                    Entity wall = factory.CreateEntity(WallBlueprint);
                    if (wall != null)
                        zone.AddEntity(wall, x, y);
                }
            }
        }

        private void ClearWalls(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;

            // Remove all wall entities from this cell
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                if (cell.Objects[i].HasTag("Wall"))
                    zone.RemoveEntity(cell.Objects[i]);
            }
        }

        private void PlaceFloorOrRubble(Zone zone, EntityFactory factory, System.Random rng, int x, int y)
        {
            int roll = rng.Next(100);
            string blueprint;
            if (roll < 80)
                blueprint = FloorBlueprint;
            else if (roll < 95)
                blueprint = RubbleBlueprint;
            else
                return; // 5% empty

            Entity terrain = factory.CreateEntity(blueprint);
            if (terrain != null)
                zone.AddEntity(terrain, x, y);
        }
    }
}
