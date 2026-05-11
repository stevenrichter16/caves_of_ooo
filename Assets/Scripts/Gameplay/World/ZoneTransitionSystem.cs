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

            // F.2.7 — bring followers along. Any PartyMember currently
            // in the same zone as the leader gets teleported to a cell
            // adjacent to the leader's arrival. Mirrors Qud's default
            // companion behavior. Followers in OTHER zones (left behind
            // earlier, or recruited elsewhere) are intentionally NOT
            // dragged along — that's a separate scenario.
            TransitPartyMembers(player, currentZone, newZone, arriveX, arriveY);

            return new ZoneTransitionResult
            {
                Success = true,
                NewZone = newZone,
                NewPlayerX = arriveX,
                NewPlayerY = arriveY
            };
        }

        /// <summary>
        /// F.2.7 — move all <see cref="BrainPart.PartyMembers"/> currently
        /// in <paramref name="oldZone"/> to <paramref name="newZone"/>,
        /// placing each at a passable cell adjacent to the leader's
        /// arrival. Public for testability; the production caller is
        /// inside <see cref="TransitionPlayer"/> /
        /// <see cref="TransitionPlayerVertical"/>.
        ///
        /// <para>Iteration uses a snapshot of <c>PartyMembers</c> because
        /// <see cref="Zone.RemoveEntity"/> / <see cref="Zone.AddEntity"/>
        /// may indirectly mutate <c>PartyMembers</c> via downstream
        /// event hooks (no current case but defense-in-depth — the
        /// HashSet would throw if modified during enumeration).</para>
        ///
        /// <para>Followers whose <see cref="BrainPart.CurrentZone"/> is
        /// not the old zone are skipped: they're in some other zone
        /// already and shouldn't be teleport-yanked. (Their continuing
        /// existence is handled separately when the player visits that
        /// zone — the <see cref="HandleZoneTransition"/>-equivalent in
        /// <c>InputHandler</c> rewires their <c>CurrentZone</c> on
        /// arrival.)</para>
        ///
        /// <para>If no adjacent passable cell is available, that
        /// follower is left behind. They'll re-enter the new zone the
        /// next time the player crosses if/when the player passes
        /// through their current zone.</para>
        /// </summary>
        public static void TransitPartyMembers(Entity leader, Zone oldZone, Zone newZone, int leaderX, int leaderY)
        {
            var brain = leader?.GetPart<BrainPart>();
            if (brain?.PartyMembers == null || brain.PartyMembers.Count == 0) return;

            // Snapshot — see method docstring.
            var members = new System.Collections.Generic.List<Entity>(brain.PartyMembers);

            for (int i = 0; i < members.Count; i++)
            {
                var member = members[i];
                if (member == null) continue;
                var memberBrain = member.GetPart<BrainPart>();
                if (memberBrain == null) continue;
                // Only transit followers currently in the OLD zone.
                if (memberBrain.CurrentZone != oldZone) continue;
                if (oldZone.GetEntityCell(member) == null) continue;

                var (mx, my) = FindAdjacentPassableCell(newZone, leaderX, leaderY);
                if (mx < 0) continue;

                oldZone.RemoveEntity(member);
                newZone.AddEntity(member, mx, my);
                memberBrain.CurrentZone = newZone;
                // InputHandler.HandleZoneTransition is responsible for
                // re-registering the follower with TurnManager + setting
                // brain.Rng — that loop iterates the NEW zone's creatures
                // (which now includes the transferred follower), so we
                // don't need to mirror that here.
            }
        }

        /// <summary>
        /// F.2.7 — search for a passable cell adjacent to
        /// (<paramref name="cx"/>, <paramref name="cy"/>) in 8-direction
        /// N→NE→E→...→NW order (deterministic for tests). Falls back to
        /// a 3-ring spiral if no adjacent cell works. Returns (-1, -1)
        /// if nothing's passable within radius 4 — defensively rare;
        /// the caller leaves the follower behind in that case.
        /// </summary>
        private static (int x, int y) FindAdjacentPassableCell(Zone zone, int cx, int cy)
        {
            // 8-direction order matches SkillCombatHelpers.FindAdjacentCleaveTarget.
            int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
            int[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };
            for (int i = 0; i < 8; i++)
            {
                int x = cx + dx[i];
                int y = cy + dy[i];
                if (!zone.InBounds(x, y)) continue;
                var cell = zone.GetCell(x, y);
                if (cell != null && cell.IsPassable())
                    return (x, y);
            }
            // Wider spiral if none of the 8 immediate cells work.
            for (int r = 2; r <= 4; r++)
            {
                for (int ox = -r; ox <= r; ox++)
                {
                    for (int oy = -r; oy <= r; oy++)
                    {
                        // Skip cells already searched (interior to the ring).
                        if (System.Math.Abs(ox) < r && System.Math.Abs(oy) < r) continue;
                        int x = cx + ox;
                        int y = cy + oy;
                        if (!zone.InBounds(x, y)) continue;
                        var cell = zone.GetCell(x, y);
                        if (cell != null && cell.IsPassable())
                            return (x, y);
                    }
                }
            }
            return (-1, -1);
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

            // F.2.7 — bring followers along through stair transitions
            // too. Symmetric with the horizontal path above.
            TransitPartyMembers(player, currentZone, newZone, arriveX, arriveY);

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
