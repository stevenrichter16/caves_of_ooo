using System;

namespace CavesOfOoo.Core
{
    public enum TransitionDirection
    {
        North,
        South,
        East,
        West,
        Up,
        Down
    }

    public struct ZoneTransitionResult
    {
        public bool Success;
        public Zone NewZone;
        public int NewPlayerX;
        public int NewPlayerY;
        public string ErrorReason;
    }

    /// <summary>
    /// Handles zone edge transitions. Pure logic with no Unity dependencies.
    /// Detects when a move would exit the zone, computes arrival position
    /// in the adjacent zone, and executes the player transfer.
    /// </summary>
    public static class ZoneTransitionSystem
    {
        /// <summary>
        /// Check if moving from (x, y) by (dx, dy) would exit the zone bounds.
        /// </summary>
        public static bool IsEdgeTransition(int x, int y, int dx, int dy)
        {
            int nx = x + dx;
            int ny = y + dy;
            return nx < 0 || nx >= Zone.Width || ny < 0 || ny >= Zone.Height;
        }

        /// <summary>
        /// Determine which direction a move exits the zone.
        /// Returns null if the move stays in bounds.
        /// </summary>
        public static TransitionDirection? GetTransitionDirection(int x, int y, int dx, int dy)
        {
            int nx = x + dx;
            int ny = y + dy;

            if (nx < 0) return TransitionDirection.West;
            if (nx >= Zone.Width) return TransitionDirection.East;
            if (ny < 0) return TransitionDirection.North;
            if (ny >= Zone.Height) return TransitionDirection.South;

            return null;
        }

        /// <summary>
        /// Calculate arrival position in the new zone.
        /// Faithful to Qud: wraps to the exact opposite edge.
        /// East from (79, y) arrives at (0, y). West from (0, y) arrives at (79, y).
        /// </summary>
        public static (int x, int y) GetArrivalPosition(TransitionDirection direction, int currentX, int currentY)
        {
            switch (direction)
            {
                case TransitionDirection.East:
                    return (0, currentY);
                case TransitionDirection.West:
                    return (Zone.Width - 1, currentY);
                case TransitionDirection.South:
                    return (currentX, 0);
                case TransitionDirection.North:
                    return (currentX, Zone.Height - 1);
                default:
                    return (currentX, currentY);
            }
        }

        /// <summary>
        /// Execute a full zone transition:
        /// 1. Compute adjacent zone ID
        /// 2. Get/generate zone from ZoneManager
        /// 3. Find passable arrival cell (spiral if needed)
        /// 4. Move player between zones
        /// </summary>
        public static ZoneTransitionResult TransitionPlayer(
            Entity player,
            Zone currentZone,
            TransitionDirection direction,
            int currentX,
            int currentY,
            ZoneManager zoneManager,
            WorldMap worldMap)
        {
            // Compute world direction delta
            int worldDX = 0, worldDY = 0;
            switch (direction)
            {
                case TransitionDirection.East: worldDX = 1; break;
                case TransitionDirection.West: worldDX = -1; break;
                case TransitionDirection.South: worldDY = 1; break;
                case TransitionDirection.North: worldDY = -1; break;
            }

            // Get adjacent zone ID
            string adjacentID = WorldMap.GetAdjacentZoneID(currentZone.ZoneID, worldDX, worldDY);
            if (adjacentID == null)
            {
                return new ZoneTransitionResult
                {
                    Success = false,
                    ErrorReason = "At world edge"
                };
            }

            // Get or generate the new zone
            Zone newZone = zoneManager.GetZone(adjacentID);
            if (newZone == null)
            {
                return new ZoneTransitionResult
                {
                    Success = false,
                    ErrorReason = "Failed to generate zone"
                };
            }

            // Compute ideal arrival position
            var (idealX, idealY) = GetArrivalPosition(direction, currentX, currentY);

            // Find passable cell near ideal position
            var (arriveX, arriveY) = FindPassableCell(newZone, idealX, idealY, direction);
            if (arriveX < 0)
            {
                return new ZoneTransitionResult
                {
                    Success = false,
                    ErrorReason = "No passable arrival cell"
                };
            }

            // Execute the transfer
            currentZone.RemoveEntity(player);
            newZone.AddEntity(player, arriveX, arriveY);

            return new ZoneTransitionResult
            {
                Success = true,
                NewZone = newZone,
                NewPlayerX = arriveX,
                NewPlayerY = arriveY
            };
        }

        /// <summary>
        /// Find a passable cell near the target position.
        /// First tries the exact position, then searches along the edge
        /// and inward in a spiral pattern.
        /// </summary>
        private static (int x, int y) FindPassableCell(Zone zone, int targetX, int targetY, TransitionDirection direction)
        {
            // Try exact position first
            var cell = zone.GetCell(targetX, targetY);
            if (cell != null && cell.IsPassable())
                return (targetX, targetY);

            // Search in expanding radius along the arrival edge
            for (int radius = 1; radius <= 10; radius++)
            {
                // Search along the edge (parallel direction)
                for (int offset = -radius; offset <= radius; offset++)
                {
                    // Also search inward from the edge
                    for (int depth = 0; depth <= radius; depth++)
                    {
                        int x = targetX, y = targetY;

                        switch (direction)
                        {
                            case TransitionDirection.East:
                            case TransitionDirection.West:
                                y = targetY + offset;
                                x = targetX + (direction == TransitionDirection.East ? depth : -depth);
                                break;
                            case TransitionDirection.North:
                            case TransitionDirection.South:
                                x = targetX + offset;
                                y = targetY + (direction == TransitionDirection.South ? depth : -depth);
                                break;
                        }

                        if (!zone.InBounds(x, y)) continue;
                        cell = zone.GetCell(x, y);
                        if (cell != null && cell.IsPassable())
                            return (x, y);
                    }
                }
            }

            return (-1, -1);
        }

        /// <summary>
        /// Execute a vertical zone transition (stairs up/down).
        /// Finds matching stairs in the target zone for arrival position.
        /// </summary>
        public static ZoneTransitionResult TransitionPlayerVertical(
            Entity player,
            Zone currentZone,
            bool goingDown,
            int currentX,
            int currentY,
            ZoneManager zoneManager)
        {
            string targetZoneID = goingDown
                ? WorldMap.GetZoneBelow(currentZone.ZoneID)
                : WorldMap.GetZoneAbove(currentZone.ZoneID);

            if (targetZoneID == null)
            {
                return new ZoneTransitionResult
                {
                    Success = false,
                    ErrorReason = goingDown ? "Cannot go deeper" : "Already at the surface"
                };
            }

            Zone newZone = zoneManager.GetZone(targetZoneID);
            if (newZone == null)
            {
                return new ZoneTransitionResult
                {
                    Success = false,
                    ErrorReason = "Failed to generate zone"
                };
            }

            // Find matching stairs in the target zone
            // Going down: look for StairsUp (the matching pair)
            // Going up: look for StairsDown (the matching pair)
            string searchTag = goingDown ? "StairsUp" : "StairsDown";
            var (arriveX, arriveY) = FindStairsInZone(newZone, searchTag, currentX, currentY);

            if (arriveX < 0)
            {
                // Fallback: arrive at the same position if no matching stairs found
                arriveX = currentX;
                arriveY = currentY;

                // Ensure it's passable
                var cell = newZone.GetCell(arriveX, arriveY);
                if (cell == null || !cell.IsPassable())
                {
                    // Search for any passable cell nearby
                    for (int radius = 1; radius <= 20; radius++)
                    {
                        bool found = false;
                        for (int dx = -radius; dx <= radius && !found; dx++)
                        {
                            for (int dy = -radius; dy <= radius && !found; dy++)
                            {
                                int nx = arriveX + dx;
                                int ny = arriveY + dy;
                                if (!newZone.InBounds(nx, ny)) continue;
                                var c = newZone.GetCell(nx, ny);
                                if (c != null && c.IsPassable())
                                {
                                    arriveX = nx;
                                    arriveY = ny;
                                    found = true;
                                }
                            }
                        }
                        if (found) break;
                    }
                }
            }

            // Execute the transfer
            currentZone.RemoveEntity(player);
            newZone.AddEntity(player, arriveX, arriveY);

            return new ZoneTransitionResult
            {
                Success = true,
                NewZone = newZone,
                NewPlayerX = arriveX,
                NewPlayerY = arriveY
            };
        }

        /// <summary>
        /// Find stairs with the given tag in a zone, preferring position closest to (nearX, nearY).
        /// </summary>
        private static (int x, int y) FindStairsInZone(Zone zone, string stairsTag, int nearX, int nearY)
        {
            int bestX = -1, bestY = -1;
            int bestDist = int.MaxValue;

            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    var cell = zone.GetCell(x, y);
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        if (cell.Objects[i].HasTag(stairsTag))
                        {
                            int dist = Math.Abs(x - nearX) + Math.Abs(y - nearY);
                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                bestX = x;
                                bestY = y;
                            }
                        }
                    }
                }
            }

            return (bestX, bestY);
        }
    }
}
