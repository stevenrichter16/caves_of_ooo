namespace CavesOfOoo.Core
{
    /// <summary>
    /// Cellular automata grid for cave generation.
    /// Port of Qud's CellularGrid: seeds random cells, then applies
    /// birth/survival rules to create organic cave shapes.
    ///
    /// Qud defaults: SeedChance=55%, BornList=[6,7,8], SurviveList=[5,6,7,8], 2 passes.
    /// Result: Cells[x,y] == 1 means wall, 0 means open.
    /// </summary>
    public class CellularAutomata
    {
        public int Width;
        public int Height;
        public bool SeedBorders = true;
        public int BorderDepth = 1;
        public int SeedChance = 55;
        public int Passes = 2;
        public int[] BornList = new int[] { 6, 7, 8 };
        public int[] SurviveList = new int[] { 5, 6, 7, 8 };
        public int[,] Cells;

        public CellularAutomata(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new int[width, height];
        }

        /// <summary>
        /// Seed the grid randomly and run CA passes.
        /// Faithful to Qud's CellularGrid.Generate().
        /// </summary>
        public void Generate(System.Random rng)
        {
            // Seed the grid
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // Border cells
                    if (SeedBorders && (x < BorderDepth || x >= Width - BorderDepth ||
                                        y < BorderDepth || y >= Height - BorderDepth))
                    {
                        Cells[x, y] = 1;
                        continue;
                    }

                    // Interior: random fill
                    Cells[x, y] = rng.Next(1, 101) <= SeedChance ? 1 : 0;
                }
            }

            // Run CA passes
            for (int i = 0; i < Passes; i++)
                ApplyPass();
        }

        /// <summary>
        /// One CA iteration. Faithful to Qud's ApplyCAPass():
        /// uses double-buffering, counts 8 neighbors, applies birth/survive rules.
        /// </summary>
        public void ApplyPass()
        {
            var next = new int[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // Border cells stay as border value
                    if (SeedBorders && (x < BorderDepth || x >= Width - BorderDepth ||
                                        y < BorderDepth || y >= Height - BorderDepth))
                    {
                        next[x, y] = 1;
                        continue;
                    }

                    int neighbors = CountNeighbors(x, y);

                    if (Cells[x, y] == 1)
                    {
                        // Cell is alive — does it survive?
                        next[x, y] = ArrayContains(SurviveList, neighbors) ? 1 : 0;
                    }
                    else
                    {
                        // Cell is dead — is it born?
                        next[x, y] = ArrayContains(BornList, neighbors) ? 1 : 0;
                    }
                }
            }

            Cells = next;
        }

        private int CountNeighbors(int cx, int cy)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx;
                    int ny = cy + dy;
                    // Out of bounds counts as wall
                    if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
                        count++;
                    else if (Cells[nx, ny] == 1)
                        count++;
                }
            }
            return count;
        }

        private static bool ArrayContains(int[] arr, int value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == value) return true;
            }
            return false;
        }

        public bool IsWall(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return true;
            return Cells[x, y] == 1;
        }

        public bool IsOpen(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
            return Cells[x, y] == 0;
        }
    }
}
