using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Ensures all open areas of the cave are reachable from each other.
    /// Simplified port of Qud's ForceConnections builder.
    ///
    /// Algorithm:
    /// 1. Flood-fill from first passable cell to find reachable area
    /// 2. Find disconnected passable regions
    /// 3. Carve corridors between them (greedy walk + path widening)
    ///
    /// Priority: LATE (3000) -- after terrain, before population.
    /// </summary>
    public class ConnectivityBuilder : IZoneBuilder
    {
        public string Name => "ConnectivityBuilder";
        public int Priority => 3000;
        public bool WidenPaths = true;
        public string FloorBlueprint = "Floor";

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            // Find first passable cell
            int startX = -1, startY = -1;
            for (int x = 0; x < Zone.Width && startX < 0; x++)
                for (int y = 0; y < Zone.Height && startX < 0; y++)
                    if (zone.GetCell(x, y).IsPassable())
                    { startX = x; startY = y; }

            if (startX < 0) return false; // No passable cells at all

            // Flood fill to find the main reachable region
            var reachable = FloodFill(zone, startX, startY);

            // Find all unreachable passable cells, grouped by region
            while (true)
            {
                // Find an unreachable passable cell
                int ux = -1, uy = -1;
                for (int x = 0; x < Zone.Width && ux < 0; x++)
                    for (int y = 0; y < Zone.Height && ux < 0; y++)
                        if (!reachable[x, y] && zone.GetCell(x, y).IsPassable())
                        { ux = x; uy = y; }

                if (ux < 0) break; // All passable cells are reachable

                // Carve a path from the unreachable cell toward the reachable area
                CarvePath(zone, factory, rng, reachable, ux, uy);

                // Re-flood from the original start to update reachability
                reachable = FloodFill(zone, startX, startY);
            }

            return true;
        }

        /// <summary>
        /// Flood fill from a starting point. Returns bool[80,25] of reachable cells.
        /// </summary>
        public static bool[,] FloodFill(Zone zone, int startX, int startY)
        {
            var visited = new bool[Zone.Width, Zone.Height];
            var queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        // Only cardinal directions for connectivity
                        if (dx != 0 && dy != 0) continue;

                        int nx = cx + dx;
                        int ny = cy + dy;

                        if (nx < 0 || nx >= Zone.Width || ny < 0 || ny >= Zone.Height) continue;
                        if (visited[nx, ny]) continue;
                        if (!zone.GetCell(nx, ny).IsPassable()) continue;

                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            return visited;
        }

        /// <summary>
        /// Carve a corridor from an unreachable cell toward the nearest reachable cell.
        /// Greedy walk toward target, clearing walls along the way.
        /// </summary>
        private void CarvePath(Zone zone, EntityFactory factory, System.Random rng,
            bool[,] reachable, int fromX, int fromY)
        {
            // Find nearest reachable cell
            int targetX = -1, targetY = -1;
            int bestDist = int.MaxValue;

            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    if (!reachable[x, y]) continue;
                    int dist = Math.Abs(x - fromX) + Math.Abs(y - fromY);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        targetX = x;
                        targetY = y;
                    }
                }
            }

            if (targetX < 0) return;

            // Walk from source toward target, clearing walls
            int cx = fromX;
            int cy = fromY;
            int maxSteps = Zone.Width + Zone.Height; // safety limit

            for (int step = 0; step < maxSteps; step++)
            {
                if (reachable[cx, cy]) break; // Reached the main region

                ClearAndFloor(zone, factory, cx, cy);

                // Widen path for natural look (Qud does this at 75% chance)
                if (WidenPaths)
                {
                    if (rng.Next(100) < 75)
                    {
                        // Clear an adjacent cell perpendicular to travel direction
                        int dx = targetX - cx;
                        int dy = targetY - cy;

                        // Perpendicular direction
                        int px, py;
                        if (Math.Abs(dx) > Math.Abs(dy))
                        { px = 0; py = rng.Next(2) == 0 ? -1 : 1; }
                        else
                        { px = rng.Next(2) == 0 ? -1 : 1; py = 0; }

                        int wx = cx + px;
                        int wy = cy + py;
                        if (zone.InBounds(wx, wy) && wx > 0 && wx < Zone.Width - 1 &&
                            wy > 0 && wy < Zone.Height - 1)
                        {
                            ClearAndFloor(zone, factory, wx, wy);
                        }
                    }
                }

                // Step toward target
                int ddx = targetX - cx;
                int ddy = targetY - cy;

                // Prefer the longer axis, with some randomness
                if (Math.Abs(ddx) > Math.Abs(ddy) || (Math.Abs(ddx) == Math.Abs(ddy) && rng.Next(2) == 0))
                    cx += ddx > 0 ? 1 : -1;
                else
                    cy += ddy > 0 ? 1 : -1;

                // Stay in bounds (inside border)
                cx = Math.Max(1, Math.Min(cx, Zone.Width - 2));
                cy = Math.Max(1, Math.Min(cy, Zone.Height - 2));
            }
        }

        private void ClearAndFloor(Zone zone, EntityFactory factory, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;

            // Remove wall entities
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                if (cell.Objects[i].HasTag("Wall") || cell.Objects[i].HasTag("Solid"))
                    zone.RemoveEntity(cell.Objects[i]);
            }

            // Place floor if cell is now empty
            if (cell.IsEmpty())
            {
                Entity floor = factory.CreateEntity(FloorBlueprint);
                if (floor != null)
                    zone.AddEntity(floor, x, y);
            }
        }
    }
}
