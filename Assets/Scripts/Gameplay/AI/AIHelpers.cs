using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static utility class for AI spatial queries and pathfinding.
    /// Provides distance calculations, greedy movement, line-of-sight,
    /// and target finding for BrainPart.
    /// </summary>
    public static class AIHelpers
    {
        /// <summary>
        /// Chebyshev distance (max of |dx|, |dy|). Correct for 8-directional movement.
        /// </summary>
        public static int ChebyshevDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
        }

        /// <summary>
        /// Get the (dx, dy) step that moves from (fromX,fromY) closer to (toX,toY).
        /// Each component is -1, 0, or +1.
        /// </summary>
        public static (int dx, int dy) StepToward(int fromX, int fromY, int toX, int toY)
        {
            int dx = Math.Sign(toX - fromX);
            int dy = Math.Sign(toY - fromY);
            return (dx, dy);
        }

        /// <summary>
        /// Get the (dx, dy) step that moves away from a position.
        /// </summary>
        public static (int dx, int dy) StepAway(int fromX, int fromY, int awayFromX, int awayFromY)
        {
            int dx = Math.Sign(fromX - awayFromX);
            int dy = Math.Sign(fromY - awayFromY);
            return (dx, dy);
        }

        /// <summary>
        /// Check if two positions are adjacent (Chebyshev distance == 1).
        /// </summary>
        public static bool IsAdjacent(int x1, int y1, int x2, int y2)
        {
            return ChebyshevDistance(x1, y1, x2, y2) == 1;
        }

        /// <summary>
        /// Simple line-of-sight check using Bresenham's line algorithm.
        /// Returns true if no solid cells block the path between (x1,y1) and (x2,y2).
        /// The start and end cells are not checked for solidity.
        /// </summary>
        public static bool HasLineOfSight(Zone zone, int x1, int y1, int x2, int y2)
        {
            if (zone == null) return false;

            // Bresenham's line algorithm
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            int cx = x1;
            int cy = y1;

            while (cx != x2 || cy != y2)
            {
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    cx += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    cy += sy;
                }

                // Skip the final cell (target position)
                if (cx == x2 && cy == y2)
                    break;

                // Check intermediate cells for solidity
                var cell = zone.GetCell(cx, cy);
                if (cell != null && cell.IsSolid())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Find the nearest entity hostile to self within the given radius.
        /// Checks faction hostility, distance, and line-of-sight.
        /// Returns null if no hostile entity is found.
        /// </summary>
        public static Entity FindNearestHostile(Entity self, Zone zone, int radius)
        {
            if (self == null || zone == null) return null;

            var selfCell = zone.GetEntityCell(self);
            if (selfCell == null) return null;

            int selfX = selfCell.X;
            int selfY = selfCell.Y;

            Entity nearest = null;
            int nearestDist = int.MaxValue;

            var creatures = zone.GetEntitiesWithTag("Creature");
            for (int i = 0; i < creatures.Count; i++)
            {
                var other = creatures[i];
                if (other == self) continue;

                // Check hostility
                if (!FactionManager.IsHostile(self, other)) continue;

                // Check distance
                var otherCell = zone.GetEntityCell(other);
                if (otherCell == null) continue;

                int dist = ChebyshevDistance(selfX, selfY, otherCell.X, otherCell.Y);
                if (dist > radius) continue;

                // Check line of sight
                if (!HasLineOfSight(zone, selfX, selfY, otherCell.X, otherCell.Y))
                    continue;

                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = other;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Find all entities hostile to self within the given radius (with LoS).
        /// </summary>
        public static List<Entity> FindHostilesInRadius(Entity self, Zone zone, int radius)
        {
            var result = new List<Entity>();
            if (self == null || zone == null) return result;

            var selfCell = zone.GetEntityCell(self);
            if (selfCell == null) return result;

            int selfX = selfCell.X;
            int selfY = selfCell.Y;

            var creatures = zone.GetEntitiesWithTag("Creature");
            for (int i = 0; i < creatures.Count; i++)
            {
                var other = creatures[i];
                if (other == self) continue;
                if (!FactionManager.IsHostile(self, other)) continue;

                var otherCell = zone.GetEntityCell(other);
                if (otherCell == null) continue;

                int dist = ChebyshevDistance(selfX, selfY, otherCell.X, otherCell.Y);
                if (dist > radius) continue;

                if (!HasLineOfSight(zone, selfX, selfY, otherCell.X, otherCell.Y))
                    continue;

                result.Add(other);
            }

            return result;
        }

        /// <summary>
        /// Pick a random passable adjacent direction for wandering.
        /// Tries all 8 directions in random order, returns the first passable one.
        /// Returns (0, 0) if completely surrounded.
        /// </summary>
        public static (int dx, int dy) RandomPassableDirection(Entity self, Zone zone, Random rng)
        {
            if (self == null || zone == null || rng == null) return (0, 0);

            var selfCell = zone.GetEntityCell(self);
            if (selfCell == null) return (0, 0);

            // All 8 directions
            int[] dirs = { 0, 1, 2, 3, 4, 5, 6, 7 };

            // Fisher-Yates shuffle
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int temp = dirs[i];
                dirs[i] = dirs[j];
                dirs[j] = temp;
            }

            for (int i = 0; i < dirs.Length; i++)
            {
                var (dx, dy) = MovementSystem.DirectionToDelta(dirs[i]);
                int nx = selfCell.X + dx;
                int ny = selfCell.Y + dy;

                if (!zone.InBounds(nx, ny)) continue;
                var targetCell = zone.GetCell(nx, ny);
                if (targetCell != null && targetCell.IsPassable())
                    return (dx, dy);
            }

            return (0, 0);
        }
    }
}
