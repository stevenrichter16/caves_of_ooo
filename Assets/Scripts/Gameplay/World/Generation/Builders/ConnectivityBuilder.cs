using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

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
            // W4.1 review: passability here was Cell.IsPassable — a
            // Solid-TAG-only check — while real movement is
            // BlocksMovement (tag OR PhysicsPart.Solid). The gap let
            // connectivity certify corridors THROUGH untagged stationary
            // solids (a seated ChoirTendril), shipping zones that pass
            // the connectivity gate but block the player. Passability is
            // now the same test movement uses.
            // Find first passable cell
            int startX = -1, startY = -1;
            for (int x = 0; x < Zone.Width && startX < 0; x++)
                for (int y = 0; y < Zone.Height && startX < 0; y++)
                    if (!zone.GetCell(x, y).BlocksMovement())
                    { startX = x; startY = y; }

            if (startX < 0) return Reject(zone, "no-passable-cell");

            // Flood fill to find the main reachable region
            var reachable = FloodFill(zone, startX, startY);

            // Connect all disconnected passable regions
            while (true)
            {
                int ux = -1, uy = -1;
                for (int x = 0; x < Zone.Width && ux < 0; x++)
                    for (int y = 0; y < Zone.Height && ux < 0; y++)
                        if (!reachable[x, y] && !zone.GetCell(x, y).BlocksMovement())
                        { ux = x; uy = y; }

                if (ux < 0) break;

                int beforeCount = CountReachable(reachable);
                CarvePath(zone, factory, rng, reachable, ux, uy);
                reachable = FloodFill(zone, startX, startY);
                // A protected owner (for example a creature) may prevent
                // carving. The pipeline can retry; this builder must not spin.
                if (CountReachable(reachable) <= beforeCount)
                    return Reject(zone, "no-progress");
            }

            // Ensure passable edge cells for zone transitions (like Qud's ForceConnections)
            if (!EnsureEdgeConnectivity(zone, factory, rng, reachable, startX, startY))
                return Reject(zone, "edge-blocked");

            Diag.Record("worldgen", "ConnectivityConnected", payload: new { zoneID = zone.ZoneID });
            return true;
        }

        /// <summary>
        /// Ensure at least one passable cell on each zone edge, connected to the main area.
        /// Like Qud's CaveNorthMouth/SouthMouth/EastMouth/WestMouth + ForceConnections.
        /// </summary>
        private bool EnsureEdgeConnectivity(Zone zone, EntityFactory factory, System.Random rng,
            bool[,] reachable, int mainX, int mainY)
        {
            // Keep the existing edge order and RNG draws. Verify each actual
            // requested mouth after carving; protected owners are never erased.
            int edgeX = rng.Next(2, Zone.Width - 2), edgeY = 0;
            for (int edge = 0; edge < 4; edge++)
            {
                if (edge == 1) { edgeX = rng.Next(2, Zone.Width - 2); edgeY = Zone.Height - 1; }
                if (edge == 2) { edgeX = 0; edgeY = rng.Next(2, Zone.Height - 2); }
                if (edge == 3) { edgeX = Zone.Width - 1; edgeY = rng.Next(2, Zone.Height - 2); }
                CarveToEdge(zone, factory, rng, reachable, edgeX, edgeY);
                reachable = FloodFill(zone, mainX, mainY);
                if (!reachable[edgeX, edgeY]) return false;
            }
            return true;
        }

        private static int CountReachable(bool[,] reachable)
        {
            int count = 0;
            foreach (bool value in reachable) if (value) count++;
            return count;
        }

        private static bool Reject(Zone zone, string reason)
        {
            Diag.Record("worldgen", "ConnectivityRejected", payload: new { zoneID = zone.ZoneID, reason });
            return false;
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
                        if (zone.GetCell(nx, ny).BlocksMovement()) continue;

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

        // Physical solidity alone does not make authored gameplay content
        // disposable terrain. A blocked route may be rejected, never "repaired"
        // by removing its people, laws, loot, portable items or transitions.
        private static bool IsProtectedOwner(Entity owner)
        {
            return owner.HasTag("Creature") || owner.HasTag("Furniture") || owner.HasTag("Item")
                || owner.HasPart<ContainerPart>() || owner.HasPart<StairsDownPart>()
                || owner.HasPart<StairsUpPart>() || owner.HasPart<DoorPart>()
                || owner.HasPart<MorrowfastDoorPart>() || owner.HasPart<SealedLibraryBarrierPart>();
        }

        private void ClearAndFloor(Zone zone, EntityFactory factory, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;

            // Match physical occupancy used by FloodFill, including a body's
            // secondary cells. Snapshot before removal since Zone owns all of
            // an entity's occupied cells. Generated creatures are never terrain.
            var occupants = new List<Entity>(cell.Occupants);
            for (int i = occupants.Count - 1; i >= 0; i--)
            {
                var owner = occupants[i];
                if (owner == null || IsProtectedOwner(owner)) continue;
                if (owner.HasTag("Wall") || owner.HasTag("Solid")
                    || owner.GetPart<PhysicsPart>()?.Solid == true)
                    zone.RemoveEntity(owner);
            }

            // Place floor if cell is now empty
            if (cell.IsEmpty())
            {
                BuilderSpawn.TryPlace(zone, factory, FloorBlueprint, x, y);
            }
        }
    }
}
