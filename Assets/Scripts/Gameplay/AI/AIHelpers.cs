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
        // Reusable scratch list for tag queries — avoids per-call allocation.
        [System.ThreadStatic] private static List<Entity> _scratchCreatures;
        private static List<Entity> ScratchCreatures => _scratchCreatures ?? (_scratchCreatures = new List<Entity>());

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
                if (cell != null && (cell.IsSolid() || zone.TileState.ObscuresSight(cx, cy)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Find an actually visible body contact, without treating a large
        /// creature's hidden anchor as observed knowledge. Body enumeration is
        /// allocation-free. Cloud endpoints remain visible like opaque surfaces;
        /// an intervening cloud blocks sight, not adjacent contact or projectiles.
        /// </summary>
        public static bool TryGetVisibleTargetCell(Entity observer, Entity target, Zone zone, int radius, out Cell visibleCell)
        {
            visibleCell = null;
            if (observer == null || target == null || zone == null || radius < 0) return false;
            int nearest = int.MaxValue;
            foreach (var eye in zone.GetOccupiedCells(observer))
            {
                if (eye == null) continue;
                foreach (var contact in zone.GetOccupiedCells(target))
                {
                    if (contact == null) continue;
                    int distance = ChebyshevDistance(eye.X, eye.Y, contact.X, contact.Y);
                    if (distance > radius || distance >= nearest
                        || !HasLineOfSight(zone, eye.X, eye.Y, contact.X, contact.Y)) continue;
                    nearest = distance; visibleCell = contact;
                }
            }
            return visibleCell != null;
        }

        /// <summary>
        /// Cached version of <see cref="FindNearestHostile"/>. Reuses the
        /// per-NPC <see cref="BrainPart"/> hostile cache when it's still
        /// valid (target alive, in zone, in LOS, still hostile, within
        /// radius). Falls back to a full scan on cache miss / failed
        /// validation, and refreshes the cache with the new result.
        ///
        /// <para><b>Why this exists.</b> Every NPC's BoredGoal/GuardGoal/
        /// DormantGoal scans for hostiles every turn. The uncached scan is
        /// O(N · LOS) over every Creature-tagged entity in the zone, even
        /// when the answer didn't change since last turn. With the cache,
        /// the steady-state cost drops to a single O(LOS) validation per
        /// NPC per turn — a 60–80% reduction in populated zones.</para>
        ///
        /// <para><b>TTL semantics.</b> Even when the cached target stays
        /// valid, the TTL counts down so we periodically re-scan to
        /// discover closer hostiles. Without the TTL, an NPC that engaged
        /// a far enemy first would never notice a closer one until the
        /// far one died.</para>
        ///
        /// <para>Falls back to <see cref="FindNearestHostile"/> if
        /// <paramref name="brain"/> is null (e.g. unit tests that skip
        /// BrainPart wiring).</para>
        /// </summary>
        public static Entity FindNearestHostileCached(Entity self, Zone zone, int radius, BrainPart brain)
        {
            if (self == null || zone == null) return null;
            if (brain == null) return FindNearestHostile(self, zone, radius);

            // Validate the cache before trusting it. Cheap O(LOS) check —
            // the win is skipping the O(N · LOS) zone scan that follows a
            // miss.
            if (brain.HasFreshHostileCache)
            {
                var cached = brain.GetCachedHostile();
                if (IsValidHostileTarget(self, cached, zone, radius))
                {
                    brain.TickHostileCacheTtl();
                    return cached;
                }
                // Cached hostile is stale (died, left zone, walked out of
                // LOS, faction shift). Drop it and fall through to a full
                // scan.
                brain.InvalidateHostileCache();
            }

            // Cache miss — pay the full O(N · LOS) scan, then refresh.
            // We deliberately do NOT cache the "no hostile found" answer:
            // an idle NPC would otherwise sleep through the first ~K ticks
            // after a hostile walked into sight (the cached null would
            // shadow the new arrival until TTL expired). Caching only
            // positive results means the savings show up exactly when we
            // need them — during active combat — without delaying AI
            // engagement at the start of an encounter.
            Entity hostile = FindNearestHostile(self, zone, radius);
            if (hostile != null)
                brain.RefreshHostileCache(hostile);
            else
                brain.InvalidateHostileCache();
            return hostile;
        }

        /// <summary>
        /// Cheap O(LOS) check that a previously-cached hostile is still a
        /// valid attack target. False if the target is dead, removed from
        /// the zone, beyond <paramref name="radius"/>, occluded by walls,
        /// or no longer hostile to <paramref name="self"/>.
        /// </summary>
        private static bool IsValidHostileTarget(Entity self, Entity target, Zone zone, int radius)
        {
            if (target == null) return false;

            var selfCell = zone.GetEntityCell(self);
            if (selfCell == null) return false;

            // Removed-from-zone check: GetEntityCell returns null after
            // CombatSystem.HandleDeath -> zone.RemoveEntity(target).
            var targetCell = zone.GetEntityCell(target);
            if (targetCell == null) return false;

            if (!FactionManager.IsHostile(self, target)) return false;

            int dist = ChebyshevDistance(selfCell.X, selfCell.Y, targetCell.X, targetCell.Y);
            if (dist > radius) return false;

            if (!HasLineOfSight(zone, selfCell.X, selfCell.Y, targetCell.X, targetCell.Y))
                return false;

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

            var creatures = ScratchCreatures;
            zone.GetEntitiesWithTagNonAlloc("Creature", creatures);
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

            var creatures = ScratchCreatures;
            zone.GetEntitiesWithTagNonAlloc("Creature", creatures);
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

        /// <summary>
        /// Greedy step toward target with fallback directions.
        /// Tries the ideal diagonal/cardinal direction first, then falls back to alternatives.
        /// Returns true if movement or one stationary door action succeeded.
        ///
        /// This is the fast path — it handles thin obstacles (single-cell walls)
        /// via diagonal/cardinal fallbacks, but CANNOT navigate around larger
        /// obstacles like building walls. Use TryApproachWithPathfinding for
        /// combat/chase logic that must reach moving targets around walls.
        /// </summary>
        private static bool TryStepOrOpenDoor(Entity actor, Zone zone, int dx, int dy)
        {
            var move = MovementSystem.TryMoveDetailed(actor, zone, dx, dy);
            return move.Moved || move.ActionPerformed;
        }

        public static bool TryStepToward(Entity entity, Zone zone, int myX, int myY, int targetX, int targetY)
        {
            var (dx, dy) = StepToward(myX, myY, targetX, targetY);

            if (TryStepOrOpenDoor(entity, zone, dx, dy))
                return true;

            if (dx != 0 && dy != 0)
            {
                if (TryStepOrOpenDoor(entity, zone, dx, 0))
                    return true;
                if (TryStepOrOpenDoor(entity, zone, 0, dy))
                    return true;
            }
            else if (dx != 0)
            {
                if (TryStepOrOpenDoor(entity, zone, dx, 1))
                    return true;
                if (TryStepOrOpenDoor(entity, zone, dx, -1))
                    return true;
            }
            else if (dy != 0)
            {
                if (TryStepOrOpenDoor(entity, zone, 1, dy))
                    return true;
                if (TryStepOrOpenDoor(entity, zone, -1, dy))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Approach a target, preferring the direct greedy step when the ideal
        /// direction is unobstructed and free of known terrain cost, falling back
        /// to actor-aware A* when blocked or dangerous. Returns true for movement
        /// or one stationary door action.
        ///
        /// Use this for KillGoal/chase AI and any goal that needs to "walk toward
        /// an entity that might move" without caching path state. Each call is
        /// stateless — A* is computed fresh when needed, so the NPC adapts
        /// instantly to a moving target.
        ///
        /// Why not TryStepToward's fallbacks? TryStepToward's cardinal fallbacks
        /// can make sideways moves that DON'T reduce distance to target, causing
        /// oscillation when a wall fully blocks the direct route. This helper
        /// only uses greedy when the ideal (single-step toward target) body is
        /// open and has no terrain cost — otherwise it uses A* to make progress
        /// toward a reachable target.
        ///
        /// Performance: A* is pool-based and fast (microseconds per call on a
        /// 2000-cell grid). It also runs when the direct prospective body has
        /// a terrain cost, so ordinary pursuit can use its existing avoidance.
        /// </summary>
        public static bool TryApproachWithPathfinding(
            Entity entity, Zone zone,
            int myX, int myY, int targetX, int targetY)
        {
            // Fast path: take the ideal single-step only when open and harmless.
            // This is a single-direction check, not TryStepToward's multi-fallback,
            // so we never make sideways moves that fail to reduce distance.
            var (dx, dy) = StepToward(myX, myY, targetX, targetY);
            if (dx != 0 || dy != 0)
            {
                int idealX = myX + dx;
                int idealY = myY + dy;
                if (zone.InBounds(idealX, idealY))
                {
                    var idealCell = zone.GetCell(idealX, idealY);
                    if (idealCell != null && idealCell.IsPassable()
                        && TerrainNavigationWeight.ForStep(zone, idealX, idealY, entity) == 0)
                    {
                        if (TryStepOrOpenDoor(entity, zone, dx, dy))
                            return true;
                    }
                }
            }

            // Slow path: weighted A* around obstacles or danger. Only the first step of
            // the returned path is used — the next tick will recompute if still
            // needed, so we naturally handle moving targets.
            // ignoreCreatures=true: combat approach should route around walls but
            // through creature-occupied cells (creatures move, paths recompute).
            var path = FindPath.Search(zone, myX, myY, targetX, targetY, ignoreCreatures: true,
                actor: entity); // G.11: combat approach prefers gas-free cells
            if (path.Usable && path.Steps.Count > 0)
            {
                var (pdx, pdy) = path.Steps[0];
                if (TryStepOrOpenDoor(entity, zone, pdx, pdy))
                    return true;
            }

            // Last resort: full greedy with all fallback directions.
            // Only reached when A* fails (unreachable target) — may oscillate
            // but at least won't crash.
            return TryStepToward(entity, zone, myX, myY, targetX, targetY);
        }

        /// <summary>
        /// Greedy step away from target with fallback directions.
        /// Returns true if movement succeeded. Does NOT handle cornered fight-back.
        /// </summary>
        public static bool TryStepAway(Entity entity, Zone zone, int myX, int myY, int awayFromX, int awayFromY)
        {
            var (dx, dy) = StepAway(myX, myY, awayFromX, awayFromY);

            // Preserve the ordinary clean-ground retreat exactly. Only a
            // dangerous preferred step opens this small outward choice: the
            // two existing axis fallbacks, or adjacent diagonals for a cardinal
            // retreat. Every choice still moves away from the supplied contact.
            if ((dx != 0 || dy != 0)
                && TerrainNavigationWeight.ForStep(zone, myX + dx, myY + dy, entity) > 0)
            {
                var first = (dx: dx, dy: dy, cost: RetreatStepCost(entity, zone, myX, myY, myX + dx, myY + dy, awayFromX, awayFromY));
                var second = dx == 0
                    ? (dx: -1, dy: dy, cost: RetreatStepCost(entity, zone, myX, myY, myX - 1, myY + dy, awayFromX, awayFromY))
                    : dy == 0
                        ? (dx: dx, dy: -1, cost: RetreatStepCost(entity, zone, myX, myY, myX + dx, myY - 1, awayFromX, awayFromY))
                        : (dx: dx, dy: 0, cost: RetreatStepCost(entity, zone, myX, myY, myX + dx, myY, awayFromX, awayFromY));
                var third = dx == 0
                    ? (dx: 1, dy: dy, cost: RetreatStepCost(entity, zone, myX, myY, myX + 1, myY + dy, awayFromX, awayFromY))
                    : dy == 0
                        ? (dx: dx, dy: 1, cost: RetreatStepCost(entity, zone, myX, myY, myX + dx, myY + 1, awayFromX, awayFromY))
                        : (dx: 0, dy: dy, cost: RetreatStepCost(entity, zone, myX, myY, myX, myY + dy, awayFromX, awayFromY));
                // Adjacent swaps keep the preferred/legacy order on equal cost.
                if (first.cost > second.cost) (first, second) = (second, first);
                if (second.cost > third.cost) (second, third) = (third, second);
                if (first.cost > second.cost) (first, second) = (second, first);
                // These are real attempts, not speculative moves. An opening or
                // unlocking action is success and stops this entire opportunity.
                return (first.cost < int.MaxValue && TryStepOrOpenDoor(entity, zone, first.dx, first.dy))
                    || (second.cost < int.MaxValue && TryStepOrOpenDoor(entity, zone, second.dx, second.dy))
                    || (third.cost < int.MaxValue && TryStepOrOpenDoor(entity, zone, third.dx, third.dy));
            }

            if (TryStepOrOpenDoor(entity, zone, dx, dy))
                return true;

            if (dx != 0 && dy != 0)
            {
                if (TryStepOrOpenDoor(entity, zone, dx, 0))
                    return true;
                if (TryStepOrOpenDoor(entity, zone, 0, dy))
                    return true;
            }
            return false;
        }

        // Pure local admission before ranking. An axis fallback from a diagonal
        // bearing can keep the same Chebyshev distance; it is not an escape.
        private static int RetreatStepCost(Entity entity, Zone zone, int fromX, int fromY,
            int x, int y, int awayX, int awayY)
        {
            if (!zone.InBounds(x, y) || ChebyshevDistance(x, y, awayX, awayY)
                <= ChebyshevDistance(fromX, fromY, awayX, awayY)) return int.MaxValue;
            bool legal = zone.CanPlaceFootprint(entity, x, y, allowOperableDoors: true);
            if (!legal)
            {
                // The normal movement hook may spend one stationary key action.
                // Admit that exact existing permission without firing its event.
                foreach (var cell in zone.GetOccupiedCells(entity, x, y))
                {
                    if (cell == null) return int.MaxValue;
                    foreach (var owner in cell.Occupants)
                        if (owner != entity && CanRetreatUnlock(entity, owner, zone)
                            && zone.CanPlaceFootprint(entity, x, y, ignoring: owner, allowOperableDoors: true))
                        { legal = true; break; }
                    if (legal) break;
                }
            }
            if (!legal) return int.MaxValue;
            if (x != fromX && y != fromY)
            {
                if (entity.HasPart<SpatialFootprintPart>())
                {
                    if (!zone.CanPlaceFootprint(entity, x, fromY)
                        && !zone.CanPlaceFootprint(entity, fromX, y)) return int.MaxValue;
                }
            }
            return TerrainNavigationWeight.ForStep(zone, x, y, entity);
        }
        private static bool CanRetreatUnlock(Entity actor, Entity owner, Zone zone)
        {
            var latch = owner?.GetPart<LockPart>();
            if (latch?.IsLocked != true || owner.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true
                || owner.HasPart<MorrowfastDoorPart>()) return false;
            var door = owner.GetPart<DoorPart>();
            if (door != null && !door.CanUnlock(actor, zone)) return false;
            if (string.IsNullOrEmpty(latch.KeyId)) return true;
            var inventory = actor.GetPart<InventoryPart>();
            if (inventory?.Objects == null) return false;
            foreach (var item in inventory.Objects)
                if (item?.GetPart<KeyPart>()?.KeyId == latch.KeyId) return true;
            return false;
        }

        /// <summary>
        /// AI attempts to use a ranged ability against a target.
        /// Returns true if an ability was successfully used.
        /// </summary>
        public static bool TryUseRangedAbility(Entity entity, Zone zone, Random rng,
            (int x, int y) myPos, (int x, int y) targetPos)
        {
            // The physical ray may pass through mist, but an AI cannot choose
            // a hidden live coordinate as its aim. This is a knowledge gate,
            // not a projectile collision rule.
            if (entity == null || zone == null || !zone.InBounds(myPos.x, myPos.y)
                || !zone.InBounds(targetPos.x, targetPos.y)
                || !HasLineOfSight(zone, myPos.x, myPos.y, targetPos.x, targetPos.y)) return false;
            var abilities = entity.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null) return false;

            int dist = ChebyshevDistance(myPos.x, myPos.y, targetPos.x, targetPos.y);
            if (entity.GetPart<BrainPart>() is BrainPart brain && dist > brain.SightRadius) return false;

            for (int i = 0; i < abilities.AbilityList.Count; i++)
            {
                var ability = abilities.AbilityList[i];
                if (!ability.IsUsable) continue;
                if (ability.TargetingMode == AbilityTargetingMode.AdjacentCell) continue;
                if (ability.Range < dist && ability.TargetingMode != AbilityTargetingMode.SelfCentered)
                    continue;

                var cmdEvent = GameEvent.New(ability.Command);
                cmdEvent.SetParameter("Zone", (object)zone);
                cmdEvent.SetParameter("RNG", (object)(rng ?? new Random()));

                if (ability.TargetingMode == AbilityTargetingMode.DirectionLine)
                {
                    var sourceCell = zone.GetCell(myPos.x, myPos.y);
                    var (dx, dy) = StepToward(myPos.x, myPos.y, targetPos.x, targetPos.y);
                    cmdEvent.SetParameter("SourceCell", (object)sourceCell);
                    cmdEvent.SetParameter("DirectionX", dx);
                    cmdEvent.SetParameter("DirectionY", dy);
                }
                else if (ability.TargetingMode == AbilityTargetingMode.SelfCentered)
                {
                    cmdEvent.SetParameter("SourceCell", (object)zone.GetCell(myPos.x, myPos.y));
                }

                entity.FireEvent(cmdEvent);
                bool handled = cmdEvent.Handled;
                cmdEvent.Release();
                if (handled) return true;
            }
            return false;
        }

        /// <summary>
        /// Breadth-first search over passable cells from (fromX, fromY) up
        /// to <paramref name="maxRadius"/> steps (4-neighbor). Returns the
        /// position of the first cell where <paramref name="predicate"/>
        /// is true; null if no match within range. The start cell is
        /// tested too. BFS traverses only passable cells — sealed rooms
        /// (no door reached first) won't be returned even if their
        /// interior cells match the predicate.
        ///
        /// <para>Used by MoveToInterior/ExteriorGoal to find the nearest
        /// reachable indoor/outdoor cell. No Qud analogue (Qud navigates to
        /// specific target GameObjects, not predicate cells) — this is a
        /// CoO-native primitive.</para>
        /// </summary>
        public static (int x, int y)? FindNearestCellWhere(
            Zone zone, int fromX, int fromY,
            System.Predicate<Cell> predicate,
            int maxRadius = 40)
        {
            if (zone == null || predicate == null) return null;

            // Caller precondition: (fromX, fromY) must be a passable cell.
            // NPCs always stand on passable cells, so in practice this is
            // met — but we defend against unusual teleports / pushbacks
            // that could leave an entity on a solid cell. Returning null
            // cleanly rather than silently BFS-ing from inside a wall.
            var startCell = zone.GetCell(fromX, fromY);
            if (startCell == null || !startCell.IsPassable()) return null;

            // Encode (x, y) as a single long for HashSet — avoids per-visit
            // allocation. x, y are in [0, 80) so fitting in 32 bits is safe.
            var visited = new System.Collections.Generic.HashSet<long>();
            var queue = new System.Collections.Generic.Queue<(int x, int y, int dist)>();

            queue.Enqueue((fromX, fromY, 0));
            visited.Add(((long)(uint)fromX << 32) | (uint)fromY);

            while (queue.Count > 0)
            {
                var (x, y, dist) = queue.Dequeue();
                var cell = zone.GetCell(x, y);
                if (cell == null) continue;

                if (predicate(cell)) return (x, y);

                if (dist >= maxRadius) continue;

                // Only expand through passable neighbors. Doors are
                // passable so BFS enters rooms correctly; walls are
                // solid so BFS stops at them.
                foreach (var (dx, dy) in CardinalOffsets)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (!zone.InBounds(nx, ny)) continue;

                    long key = ((long)(uint)nx << 32) | (uint)ny;
                    if (!visited.Add(key)) continue;

                    var nCell = zone.GetCell(nx, ny);
                    if (nCell == null) continue;
                    if (!nCell.IsPassable()) continue;

                    queue.Enqueue((nx, ny, dist + 1));
                }
            }
            return null;
        }

        private static readonly (int dx, int dy)[] CardinalOffsets =
            { (1, 0), (-1, 0), (0, 1), (0, -1) };
    }
}
