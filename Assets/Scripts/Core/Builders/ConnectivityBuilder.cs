using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Ensures all open areas of the zone are reachable and that zone edges
    /// have passable cells for zone transitions.
    /// Port of Qud's ForceConnections builder:
    /// 1. Flood-fill from first passable cell to find reachable area
    /// 2. Carve corridors to connect disconnected passable regions
    /// 3. Ensure at least one passable cell on each zone edge, connected
    ///    to the main area (like Qud's CaveNorthMouth/SouthMouth/etc.)
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

            // Connect all disconnected passable regions
            while (true)
            {
                int ux = -1, uy = -1;
                for (int x = 0; x < Zone.Width && ux < 0; x++)
                    for (int y = 0; y < Zone.Height && ux < 0; y++)
                        if (!reachable[x, y] && zone.GetCell(x, y).IsPassable())
                        { ux = x; uy = y; }

                if (ux < 0) break;

                CarvePath(zone, factory, rng, reachable, ux, uy);
                reachable = FloodFill(zone, startX, startY);
            }

            // Ensure passable edge cells for zone transitions (like Qud's ForceConnections)
            EnsureEdgeConnectivity(zone, factory, rng, reachable, startX, startY);

            return true;
        }

        /// <summary>
        /// Ensure at least one passable cell on each zone edge, connected to the main area.
        /// Like Qud's CaveNorthMouth/SouthMouth/EastMouth/WestMouth + ForceConnections.
        /// </summary>
        private void EnsureEdgeConnectivity(Zone zone, EntityFactory factory, System.Random rng,
            bool[,] reachable, int mainX, int mainY)
        {
            // For each edge, pick a connection point and carve to it if needed
            // North edge (y=0): pick random x
            CarveToEdge(zone, factory, rng, reachable, rng.Next(2, Zone.Width - 2), 0);
            reachable = FloodFill(zone, mainX, mainY);

            // South edge (y=Height-1)
            CarveToEdge(zone, factory, rng, reachable, rng.Next(2, Zone.Width - 2), Zone.Height - 1);
            reachable = FloodFill(zone, mainX, mainY);

            // West edge (x=0)
            CarveToEdge(zone, factory, rng, reachable, 0, rng.Next(2, Zone.Height - 2));
            reachable = FloodFill(zone, mainX, mainY);

            // East edge (x=Width-1)
            CarveToEdge(zone, factory, rng, reachable, Zone.Width - 1, rng.Next(2, Zone.Height - 2));
        }

        /// <summary>
        /// Ensure a specific edge cell is passable and connected to the main area.
        /// If the edge cell is a wall, clear it. Then carve from the nearest
        /// reachable cell to the edge cell.
        /// </summary>
        private void CarveToEdge(Zone zone, EntityFactory factory, System.Random rng,
            bool[,] reachable, int edgeX, int edgeY)
        {
            // Make the edge cell passable
            ClearAndFloor(zone, factory, edgeX, edgeY);

            // If already reachable, done
            if (reachable[edgeX, edgeY]) return;

            // Find nearest reachable cell
            int targetX = -1, targetY = -1;
            int bestDist = int.MaxValue;
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    if (!reachable[x, y]) continue;
                    int dist = Math.Abs(x - edgeX) + Math.Abs(y - edgeY);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        targetX = x;
                        targetY = y;
                    }
                }
            }

            if (targetX < 0) return;

            // Carve from edge toward reachable area
            int cx = edgeX, cy = edgeY;
            int maxSteps = Zone.Width + Zone.Height;
            for (int step = 0; step < maxSteps; step++)
            {
                if (reachable[cx, cy]) break;
                ClearAndFloor(zone, factory, cx, cy);

                int ddx = targetX - cx;
                int ddy = targetY - cy;
                if (Math.Abs(ddx) > Math.Abs(ddy) || (Math.Abs(ddx) == Math.Abs(ddy) && rng.Next(2) == 0))
                    cx += ddx > 0 ? 1 : -1;
                else
                    cy += ddy > 0 ? 1 : -1;

                cx = Math.Max(0, Math.Min(cx, Zone.Width - 1));
                cy = Math.Max(0, Math.Min(cy, Zone.Height - 1));
            }
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
            int maxSteps = Zone.Width + Zone.Height;

            for (int step = 0; step < maxSteps; step++)
            {
                if (reachable[cx, cy]) break;

                ClearAndFloor(zone, factory, cx, cy);

                // Widen path for natural look (Qud does this at 75% chance)
                if (WidenPaths && rng.Next(100) < 75)
                {
                    int dx = targetX - cx;
                    int dy = targetY - cy;
                    int px, py;
                    if (Math.Abs(dx) > Math.Abs(dy))
                    { px = 0; py = rng.Next(2) == 0 ? -1 : 1; }
                    else
                    { px = rng.Next(2) == 0 ? -1 : 1; py = 0; }

                    int wx = cx + px;
                    int wy = cy + py;
                    if (zone.InBounds(wx, wy))
                        ClearAndFloor(zone, factory, wx, wy);
                }

                // Step toward target
                int ddx = targetX - cx;
                int ddy = targetY - cy;

                if (Math.Abs(ddx) > Math.Abs(ddy) || (Math.Abs(ddx) == Math.Abs(ddy) && rng.Next(2) == 0))
                    cx += ddx > 0 ? 1 : -1;
                else
                    cy += ddy > 0 ? 1 : -1;

                cx = Math.Max(0, Math.Min(cx, Zone.Width - 1));
                cy = Math.Max(0, Math.Min(cy, Zone.Height - 1));
            }
        }

        private void ClearAndFloor(Zone zone, EntityFactory factory, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;

            // Remove wall/solid entities
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
