namespace CavesOfOoo.Core
{
    /// <summary>
    /// A* node stored in FindPath's pre-allocated pool.
    /// Struct to avoid heap allocation — lives in a flat array indexed by cell position.
    /// </summary>
    public struct CellNavigationValue
    {
        public int X;
        public int Y;
        public int G;           // cost from start
        public int H;           // heuristic estimate to goal
        public int F;           // G + H
        public int ParentIndex; // index into pool (-1 = start node)
        public bool InOpen;
        public bool InClosed;

        public void Reset()
        {
            G = 0;
            H = 0;
            F = 0;
            ParentIndex = -1;
            InOpen = false;
            InClosed = false;
        }
    }
}
