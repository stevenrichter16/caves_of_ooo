using System;
using CavesOfOoo.Diagnostics;

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
                RecordFootprintArrival(player, currentZone, null, -1, -1, false, "world-edge");
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
                RecordFootprintArrival(player, currentZone, null, -1, -1, false, "generation-failed");
                return new ZoneTransitionResult
                {
                    Success = false,
                    ErrorReason = "Failed to generate zone"
                };
            }

            // Compute ideal arrival position
            var (idealX, idealY) = GetArrivalPosition(direction, currentX, currentY);

            // Find passable cell near ideal position
            var (arriveX, arriveY) = FindPassableCell(newZone, player, idealX, idealY, direction);
            if (arriveX < 0)
            {
                RecordFootprintArrival(player, currentZone, newZone, arriveX, arriveY, false, "no-eligible-arrival");
                return new ZoneTransitionResult
                {
                    Success = false,
                    ErrorReason = "No passable arrival cell"
                };
            }

            // Validate even exhausted vertical fallbacks before changing
            // source membership. Arrival exclusions do not block walking.
            if (!CanArrive(newZone, player, arriveX, arriveY))
            {
                RecordFootprintArrival(player, currentZone, newZone, arriveX, arriveY, false, "no-eligible-arrival");
                return new ZoneTransitionResult { Success = false, ErrorReason = "No eligible arrival cell" };
            }

            // Execute the transfer
            if (!currentZone.TryTransferEntityTo(player, newZone, arriveX, arriveY))
            {
                RecordFootprintArrival(player, currentZone, newZone, arriveX, arriveY, false, "placement-changed");
                return new ZoneTransitionResult { Success = false, ErrorReason = "Arrival placement changed" };
            }
            RecordFootprintArrival(player, currentZone, newZone, arriveX, arriveY, true, "");

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

                var (mx, my) = FindAdjacentPassableCell(newZone, member, leaderX, leaderY);
                if (mx < 0)
                {
                    RecordFootprintArrival(member, oldZone, newZone, mx, my, false, "no-eligible-arrival");
                    continue;
                }

                if (!oldZone.TryTransferEntityTo(member, newZone, mx, my))
                {
                    RecordFootprintArrival(member, oldZone, newZone, mx, my, false, "placement-changed");
                    continue;
                }
                memberBrain.CurrentZone = newZone;
                RecordFootprintArrival(member, oldZone, newZone, mx, my, true, "");
                // InputHandler.HandleZoneTransition is responsible for
                // re-registering the follower with TurnManager + setting
                // brain.Rng — that loop iterates the NEW zone's creatures
                // (which now includes the transferred follower), so we
                // don't need to mirror that here.
            }
        }

        // Emit only the completed actor-level outcome. Candidate search probes
        // deliberately stay quiet even when a large shape rejects many anchors.
        private static void RecordFootprintArrival(Entity actor, Zone source, Zone destination,
            int x, int y, bool succeeded, string reason)
        {
            if (actor?.GetPart<SpatialFootprintPart>() == null || !Diag.IsChannelEnabled("worldmap")) return;
            Diag.Record("worldmap", succeeded ? "FootprintArrivalSucceeded" : "FootprintArrivalRejected", actor,
                payload: new { fromZone = source?.ZoneID, toZone = destination?.ZoneID, x, y,
                    cells = (succeeded ? destination : source)?.GetOccupiedCells(actor).Count ?? 0, reason });
        }

        // Automatic arrivals must not place actors in a sealed interior.
        // Tags survive save/load and are checked beneath any dropped objects.
        // Ordinary walking and restoration deliberately do not use this rule.
        private static bool CanArrive(Zone zone, Entity actor, int x, int y)
        {
            if (zone == null) return false;
            // Keep the ordinary one-cell arrival contract. Opted-in shapes
            // must fit every physical cell, including non-anchor exclusions.
            if (actor?.GetPart<SpatialFootprintPart>() == null)
            {
                var cell = zone.GetCell(x, y);
                return cell != null && cell.IsPassable()
                    && !cell.HasObjectWithTag("ExcludeZoneArrival");
            }
            if (!zone.CanPlaceFootprint(actor, x, y)) return false;
            var body = zone.GetOccupiedCells(actor, x, y);
            if (body.Count == 0) return false;
            foreach (var cell in body)
                if (cell == null || cell.HasObjectWithTag("ExcludeZoneArrival")) return false;
            return true;
        }

        /// <summary>
        /// F.2.7 — search for a passable cell adjacent to
        /// (<paramref name="cx"/>, <paramref name="cy"/>) in 8-direction
        /// N→NE→E→...→NW order (deterministic for tests). Falls back to
        /// a 3-ring spiral if no adjacent cell works. Returns (-1, -1)
        /// if nothing's passable within radius 4 — defensively rare;
        /// the caller leaves the follower behind in that case.
        /// </summary>
        private static (int x, int y) FindAdjacentPassableCell(Zone zone, Entity actor, int cx, int cy)
        {
            // 8-direction order matches SkillCombatHelpers.FindAdjacentCleaveTarget.
            int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
            int[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };
            for (int i = 0; i < 8; i++)
            {
                int x = cx + dx[i];
                int y = cy + dy[i];
                if (!zone.InBounds(x, y)) continue;
                if (CanArrive(zone, actor, x, y))
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
                        if (CanArrive(zone, actor, x, y))
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
        private static (int x, int y) FindPassableCell(Zone zone, Entity actor, int targetX, int targetY, TransitionDirection direction)
        {
            // Try exact position first
            if (CanArrive(zone, actor, targetX, targetY))
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
                        if (CanArrive(zone, actor, x, y))
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
                RecordFootprintArrival(player, currentZone, null, -1, -1, false, "vertical-limit");
                return new ZoneTransitionResult
                {
                    Success = false,
                    ErrorReason = goingDown ? "Cannot go deeper" : "Already at the surface"
                };
            }

            Zone newZone = zoneManager.GetZone(targetZoneID);
            if (newZone == null)
            {
                RecordFootprintArrival(player, currentZone, null, -1, -1, false, "generation-failed");
                return new ZoneTransitionResult
                {
                    Success = false,
                    ErrorReason = "Failed to generate zone"
                };
            }

            // W6.7: a real stair with a removed return endpoint cannot
            // silently become a one-way teleport through the fallback below.
            // Marker-free relocation callers retain their existing fallback.
            var departure = currentZone.GetCell(currentX, currentY);
            bool physicalStair = false, returnStair = false;
            if (departure != null)
                foreach (var e in departure.Objects)
                    if (goingDown ? e.HasPart<StairsDownPart>() : e.HasPart<StairsUpPart>()) physicalStair = true;
            if (physicalStair)
            {
                foreach (var e in newZone.GetAllEntities())
                    if (goingDown ? e.HasPart<StairsUpPart>() : e.HasPart<StairsDownPart>()) { returnStair = true; break; }
                if (!returnStair)
                {
                    RecordFootprintArrival(player, currentZone, newZone, -1, -1, false, "return-stairs-missing");
                    return new ZoneTransitionResult { Success = false, ErrorReason = "The return stairs are missing" };
                }
            }

            // Find matching stairs in the target zone
            // Going down: look for StairsUp (the matching pair)
            // Going up: look for StairsDown (the matching pair)
            string searchTag = goingDown ? "StairsUp" : "StairsDown";
            var (arriveX, arriveY) = FindStairsInZone(newZone, player, searchTag, currentX, currentY);

            if (arriveX < 0)
            {
                // Fallback: arrive at the same position if no matching stairs found
                arriveX = currentX;
                arriveY = currentY;

                // Ensure it's passable
                if (!CanArrive(newZone, player, arriveX, arriveY))
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
                                if (CanArrive(newZone, player, nx, ny))
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

            // Validate even exhausted vertical fallbacks before changing
            // source membership. Arrival exclusions do not block walking.
            if (!CanArrive(newZone, player, arriveX, arriveY))
            {
                RecordFootprintArrival(player, currentZone, newZone, arriveX, arriveY, false, "no-eligible-arrival");
                return new ZoneTransitionResult { Success = false, ErrorReason = "No eligible arrival cell" };
            }

            // Execute the transfer
            if (!currentZone.TryTransferEntityTo(player, newZone, arriveX, arriveY))
            {
                RecordFootprintArrival(player, currentZone, newZone, arriveX, arriveY, false, "placement-changed");
                return new ZoneTransitionResult { Success = false, ErrorReason = "Arrival placement changed" };
            }
            RecordFootprintArrival(player, currentZone, newZone, arriveX, arriveY, true, "");

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
        private static (int x, int y) FindStairsInZone(Zone zone, Entity actor, string stairsTag, int nearX, int nearY)
        {
            int bestX = -1, bestY = -1;
            int bestDist = int.MaxValue;

            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (!CanArrive(zone, actor, x, y)) continue;
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
