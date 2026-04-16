using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A* pathfinding over the zone grid (80x25).
    /// Pool-based node allocation for zero GC. Chebyshev heuristic for 8-directional movement.
    /// Mirrors Qud's FindPath (simplified: single-zone only, no cross-zone support yet).
    /// </summary>
    public class FindPath
    {
        private const int GridSize = Zone.Width * Zone.Height; // 2000

        // Pre-allocated pools (static, reused across searches)
        private static readonly CellNavigationValue[] Pool = new CellNavigationValue[GridSize];
        private static readonly PathMinHeap OpenSet = new PathMinHeap(GridSize);

        // 8-directional neighbor offsets (N, NE, E, SE, S, SW, W, NW)
        private static readonly int[] DX = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] DY = { -1, -1, 0, 1, 1, 1, 0, -1 };
        // Cost: cardinal=10, diagonal=14 (approximates sqrt(2)*10)
        private static readonly int[] Cost = { 10, 14, 10, 14, 10, 14, 10, 14 };

        /// <summary>Whether a valid path was found.</summary>
        public bool Usable;

        /// <summary>Direction deltas from start to goal (exclusive of start position).</summary>
        public List<(int dx, int dy)> Steps;

        private FindPath() { }

        /// <summary>
        /// Run A* from (startX, startY) to (goalX, goalY) in the given zone.
        /// Returns a FindPath result with Usable and Steps.
        /// </summary>
        /// <param name="zone">The zone to pathfind in.</param>
        /// <param name="startX">Start X coordinate.</param>
        /// <param name="startY">Start Y coordinate.</param>
        /// <param name="goalX">Goal X coordinate.</param>
        /// <param name="goalY">Goal Y coordinate.</param>
        /// <param name="maxNodes">Maximum nodes to expand before giving up.</param>
        public static FindPath Search(Zone zone, int startX, int startY, int goalX, int goalY, int maxNodes = 2000)
        {
            var result = new FindPath { Usable = false, Steps = new List<(int, int)>() };

            if (zone == null) return result;
            if (!zone.InBounds(startX, startY) || !zone.InBounds(goalX, goalY)) return result;

            // Same cell
            if (startX == goalX && startY == goalY)
            {
                result.Usable = true;
                return result;
            }

            // Reset pool and open set
            for (int i = 0; i < GridSize; i++)
            {
                Pool[i].InOpen = false;
                Pool[i].InClosed = false;
            }
            OpenSet.Clear();

            int startIdx = CellIndex(startX, startY);
            int goalIdx = CellIndex(goalX, goalY);

            // Initialize start node
            Pool[startIdx].X = startX;
            Pool[startIdx].Y = startY;
            Pool[startIdx].G = 0;
            Pool[startIdx].H = Heuristic(startX, startY, goalX, goalY);
            Pool[startIdx].F = Pool[startIdx].H;
            Pool[startIdx].ParentIndex = -1;
            Pool[startIdx].InOpen = true;
            Pool[startIdx].InClosed = false;

            OpenSet.Push(startIdx, Pool[startIdx].F);

            int expanded = 0;

            while (OpenSet.Count > 0 && expanded < maxNodes)
            {
                int currentIdx = OpenSet.Pop();

                // Already processed (can happen with duplicate pushes)
                if (Pool[currentIdx].InClosed) continue;

                Pool[currentIdx].InClosed = true;
                Pool[currentIdx].InOpen = false;
                expanded++;

                int cx = Pool[currentIdx].X;
                int cy = Pool[currentIdx].Y;

                // Goal reached
                if (currentIdx == goalIdx)
                {
                    result.Usable = true;
                    ReconstructPath(result, startIdx, goalIdx);
                    return result;
                }

                // Expand neighbors
                for (int dir = 0; dir < 8; dir++)
                {
                    int nx = cx + DX[dir];
                    int ny = cy + DY[dir];

                    if (!zone.InBounds(nx, ny)) continue;

                    int neighborIdx = CellIndex(nx, ny);

                    if (Pool[neighborIdx].InClosed) continue;

                    // Goal cell is always considered passable (we want to path TO it)
                    if (neighborIdx != goalIdx)
                    {
                        var cell = zone.GetCell(nx, ny);
                        if (cell == null || !cell.IsPassable()) continue;
                    }

                    // Check diagonal movement isn't cutting through walls
                    if (DX[dir] != 0 && DY[dir] != 0)
                    {
                        var adjX = zone.GetCell(cx + DX[dir], cy);
                        var adjY = zone.GetCell(cx, cy + DY[dir]);
                        if ((adjX == null || adjX.IsSolid()) && (adjY == null || adjY.IsSolid()))
                            continue; // Can't squeeze diagonally between two walls
                    }

                    int tentativeG = Pool[currentIdx].G + Cost[dir];

                    if (!Pool[neighborIdx].InOpen)
                    {
                        // First visit
                        Pool[neighborIdx].X = nx;
                        Pool[neighborIdx].Y = ny;
                        Pool[neighborIdx].G = tentativeG;
                        Pool[neighborIdx].H = Heuristic(nx, ny, goalX, goalY);
                        Pool[neighborIdx].F = tentativeG + Pool[neighborIdx].H;
                        Pool[neighborIdx].ParentIndex = currentIdx;
                        Pool[neighborIdx].InOpen = true;
                        OpenSet.Push(neighborIdx, Pool[neighborIdx].F);
                    }
                    else if (tentativeG < Pool[neighborIdx].G)
                    {
                        // Better path found — update and re-push (lazy deletion)
                        Pool[neighborIdx].G = tentativeG;
                        Pool[neighborIdx].F = tentativeG + Pool[neighborIdx].H;
                        Pool[neighborIdx].ParentIndex = currentIdx;
                        OpenSet.Push(neighborIdx, Pool[neighborIdx].F);
                    }
                }
            }

            // No path found
            return result;
        }

        /// <summary>Chebyshev distance scaled by 10 (matching cardinal cost).</summary>
        private static int Heuristic(int x1, int y1, int x2, int y2)
        {
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            // Chebyshev with diagonal cost: 10 * max(dx,dy) + 4 * min(dx,dy)
            // This gives exact cost for obstacle-free 8-directional movement
            return dx > dy ? 10 * dx + 4 * dy : 10 * dy + 4 * dx;
        }

        private static int CellIndex(int x, int y)
        {
            return y * Zone.Width + x;
        }

        private static void ReconstructPath(FindPath result, int startIdx, int goalIdx)
        {
            // Walk backwards from goal to start, collecting direction deltas
            var reversedSteps = new List<(int dx, int dy)>();
            int current = goalIdx;

            while (current != startIdx)
            {
                int parent = Pool[current].ParentIndex;
                int dx = Pool[current].X - Pool[parent].X;
                int dy = Pool[current].Y - Pool[parent].Y;
                reversedSteps.Add((dx, dy));
                current = parent;
            }

            // Reverse to get start→goal order
            for (int i = reversedSteps.Count - 1; i >= 0; i--)
                result.Steps.Add(reversedSteps[i]);
        }
    }
}
