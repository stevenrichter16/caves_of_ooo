using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Read-only, one-zone suggestions for controller travel. Every suggested
    /// body cell is known and safe; no movement, events, turns, door operation,
    /// target disclosure or cross-zone state is performed here. Callers re-query
    /// before executing each step and own cancellation and movement vetoes.
    /// </summary>
    public static class ControllerTravel
    {
        private const int CellCount = Zone.Width * Zone.Height;
        private static readonly int[] DX = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] DY = { -1, -1, 0, 1, 1, 1, 0, -1 };
        private static readonly int[] DirectionOrder = { 0, 1, -1, 2, -2, 3, -3, 4 };
        private enum Goal { Point, Edge, Frontier, Reachability }

        /// <summary>
        /// Detached results for one synchronous POI-menu construction. CanReach
        /// never reads the live world. Discard after any actor, world or visibility
        /// mutation; actual movement must use a fresh per-step query. The starting
        /// cell is reachable even though it requires no movement step.
        /// </summary>
        public sealed class ReachabilityMap
        {
            private readonly bool[] reached;
            internal ReachabilityMap(bool[] ownedResults) { reached = ownedResults; }

            public bool CanReach(int x, int y) => reached != null && x >= 0 && x < Zone.Width
                && y >= 0 && y < Zone.Height && reached[y * Zone.Width + x];
        }

        private static readonly ReachabilityMap EmptyReachability = new ReachabilityMap(null);

        /// <summary>
        /// Build one bounded reachability snapshot for all candidate destinations
        /// in a POI menu. Invalid or dangerous contexts return an empty nonnull map.
        /// This uses the same admission rules as the step planners and performs no
        /// gameplay actions. Never retain the map as authority for later movement.
        /// </summary>
        public static ReachabilityMap BuildReachable(Zone zone, Entity actor)
        {
            if (!Ready(zone, actor, out var start)) return EmptyReachability;
            return new Search(zone, actor, start, 1, 0).BuildReachable();
        }

        /// <summary>
        /// Approach the selected boundary without crossing it. Diagonals stop at
        /// either selected boundary. If that edge is not safely reachable through
        /// known cells, approach the furthest reachable known cell in the heading.
        /// Only nonzero unit directions are accepted. False always outputs (0,0).
        /// </summary>
        public static bool TryStepTowardEdge(Zone zone, Entity actor, int dx, int dy,
            out int stepX, out int stepY)
        {
            stepX = stepY = 0;
            if (dx < -1 || dx > 1 || dy < -1 || dy > 1 || (dx == 0 && dy == 0)
                || !Ready(zone, actor, out var start) || AtEdge(zone, actor, start.X, start.Y, dx, dy))
                return false;
            return new Search(zone, actor, start, dx, dy).Run(Goal.Edge, 0, 0, out stepX, out stepY);
        }

        /// <summary>
        /// Approach the nearest reachable known safe cell adjoining unknown space.
        /// Unknown neighbors are inspected only for visibility/exploration flags.
        /// Already standing at a frontier stops; this never steps blindly beyond it.
        /// </summary>
        public static bool TryStepTowardFrontier(Zone zone, Entity actor, out int stepX, out int stepY)
        {
            stepX = stepY = 0;
            if (!Ready(zone, actor, out var start) || IsFrontier(zone, actor, start.X, start.Y)) return false;
            return new Search(zone, actor, start, 1, 0).Run(Goal.Frontier, 0, 0, out stepX, out stepY);
        }

        /// <summary>
        /// Suggest one step toward a known local destination. A blocked, dangerous,
        /// unknown, out-of-bounds or already reached destination returns false and
        /// zero outputs. The returned step is a suggestion, not an executed action.
        /// </summary>
        public static bool TryStepTowardPoint(Zone zone, Entity actor, int x, int y,
            out int stepX, out int stepY)
        {
            stepX = stepY = 0;
            if (!Ready(zone, actor, out var start) || !Known(zone.GetCell(x, y))
                || (start.X == x && start.Y == y)) return false;
            return new Search(zone, actor, start, Math.Sign(x - start.X), Math.Sign(y - start.Y))
                .Run(Goal.Point, x, y, out stepX, out stepY);
        }

        /// <summary>
        /// Invalid local context, hazardous current contact or a currently visible
        /// living hostile stops convenience travel. Hidden enemies do not reveal
        /// themselves through this query. The shared steam contact safety query
        /// can conservatively reject unseen adjacent hot steam without revealing it.
        /// </summary>
        public static bool HasDanger(Zone zone, Entity actor) => !Ready(zone, actor, out _);

        private static bool Ready(Zone zone, Entity actor, out Cell start)
        {
            start = null;
            if (zone == null || actor == null || WorldMap.IsWorldMapZoneID(zone.ZoneID)
                || actor.SpatialZone != zone || CombatSystem.IsDeathHandled(actor)
                || actor.GetStatValue("Hitpoints", 1) <= 0) return false;
            var physics = actor.GetPart<PhysicsPart>();
            if (physics?.InInventory != null || physics?.Equipped != null) return false;
            if (actor.HasPart<SpatialFootprintPart>() && !zone.IsFootprintCurrent(actor)) return false;
            start = zone.GetEntityCell(actor);
            if (start == null || !SafePlacement(zone, actor, start.X, start.Y)) return false;

            // Scan visible cells rather than hidden entity metadata. Occupants
            // includes a wide enemy's visible feet even when its anchor is hidden.
            for (int y = 0; y < Zone.Height; y++)
                for (int x = 0; x < Zone.Width; x++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell?.IsVisible != true) continue;
                    foreach (var other in cell.Occupants)
                    {
                        if (other == null || other == actor || !other.HasTag("Creature")
                            || CombatSystem.IsDeathHandled(other) || other.GetStatValue("Hitpoints", 1) <= 0
                            || other.GetPart<RenderPart>()?.Visible == false) continue;
                        var brain = other.GetPart<BrainPart>();
                        if (brain?.Passive == true && !brain.IsPersonallyHostileTo(actor)) continue;
                        if (FactionManager.IsHostile(other, actor)) return false;
                    }
                }
            return true;
        }

        private static bool Known(Cell cell) => cell != null && (cell.IsVisible || cell.Explored);

        private static bool SafePlacement(Zone zone, Entity actor, int x, int y)
        {
            if (!Known(zone.GetCell(x, y))) return false;
            var cells = zone.GetOccupiedCells(actor, x, y);
            if (cells.Count == 0) return false;
            // Complete this pass before collision or hazard reads, including when
            // the physical body omits its own canonical anchor.
            foreach (var cell in cells) if (!Known(cell)) return false;
            if (!zone.CanPlaceFootprint(actor, x, y)) return false;
            if (TerrainNavigationWeight.ForStep(zone, x, y, actor) > 0) return false;
            foreach (var cell in cells)
            {
                foreach (var other in cell.Occupants)
                {
                    if (other == null) continue;
                    if (other != actor && other.HasTag("Creature")) return false;
                    if (other != actor && TriggersOn(other, actor)) return false;
                }
                // Actual fire emits heat to its perimeter. Known neighboring fire
                // is unsafe too; raw tile energy alone is not invented damage.
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var nearby = zone.GetCell(cell.X + dx, cell.Y + dy);
                        if (!Known(nearby)) continue;
                        foreach (var other in nearby.Occupants)
                            if (IsFire(other)) return false;
                    }
            }
            return true;
        }

        private static bool IsFire(Entity owner)
        {
            if (owner == null) return false;
            if (owner.GetPart<ThermalPart>()?.IsAflame == true) return true;
            var effects = owner.GetPart<StatusEffectsPart>()?.GetAllEffects();
            if (effects != null)
                for (int i = 0; i < effects.Count; i++)
                    if (effects[i] is BurningEffect fire && fire.Owner == owner
                        && (fire.Duration > 0 || fire.Duration == Effect.DURATION_INDEFINITE)) return true;
            return false;
        }

        private static bool TriggersOn(Entity owner, Entity actor)
        {
            if (!owner.HasPart<TriggerOnStepPart>() || TrapJammingPart.IsJammed(owner)) return false;
            string faction = FactionManager.GetFaction(actor);
            foreach (var part in owner.Parts)
                if (part is TriggerOnStepPart trigger &&
                    (string.IsNullOrEmpty(trigger.TriggerFaction) || trigger.TriggerFaction != faction)) return true;
            return false;
        }

        private static bool AtEdge(Zone zone, Entity actor, int x, int y, int dx, int dy)
        {
            foreach (var cell in zone.GetOccupiedCells(actor, x, y))
                if (cell != null && ((dx < 0 && cell.X == 0) || (dx > 0 && cell.X == Zone.Width - 1)
                    || (dy < 0 && cell.Y == 0) || (dy > 0 && cell.Y == Zone.Height - 1))) return true;
            return false;
        }

        private static bool IsFrontier(Zone zone, Entity actor, int x, int y)
        {
            foreach (var cell in zone.GetOccupiedCells(actor, x, y))
            {
                if (cell == null) continue;
                for (int d = 0; d < 8; d++)
                {
                    var next = zone.GetCell(cell.X + DX[d], cell.Y + DY[d]);
                    if (next != null && !Known(next)) return true;
                }
            }
            return false;
        }

        // Scratch is private to one query: at most 2,000 queue entries and one
        // admission computation per anchor. No mutable-world cache survives a call.
        private sealed class Search
        {
            private readonly Zone zone;
            private readonly Entity actor;
            private readonly Cell start;
            private readonly int headingX, headingY, headingIndex;
            private readonly int[] queue = new int[CellCount];
            private readonly byte[] safety = new byte[CellCount];
            private readonly bool[] visited = new bool[CellCount];
            private readonly sbyte[] firstX = new sbyte[CellCount], firstY = new sbyte[CellCount];

            internal Search(Zone zone, Entity actor, Cell start, int dx, int dy)
            {
                this.zone = zone; this.actor = actor; this.start = start;
                headingX = dx; headingY = dy;
                for (int d = 0; d < 8; d++) if (DX[d] == dx && DY[d] == dy) { headingIndex = d; break; }
            }

            private bool Safe(int x, int y)
            {
                if (!zone.InBounds(x, y)) return false;
                int index = y * Zone.Width + x;
                if (safety[index] == 0) safety[index] = (byte)(SafePlacement(zone, actor, x, y) ? 1 : 2);
                return safety[index] == 1;
            }

            internal ReachabilityMap BuildReachable()
            {
                Run(Goal.Reachability, 0, 0, out _, out _);
                // Transfer the query-owned bits; no search or caller mutates them
                // afterward, and no zone/entity reference enters the public map.
                return new ReachabilityMap(visited);
            }

            internal bool Run(Goal goal, int targetX, int targetY, out int dx, out int dy)
            {
                dx = dy = 0;
                if (goal == Goal.Point && !Safe(targetX, targetY)) return false;
                int initial = start.Y * Zone.Width + start.X;
                int read = 0, count = 1, best = -1, bestProgress = 0, bestCross = int.MaxValue;
                queue[0] = initial; visited[initial] = true; safety[initial] = 1;
                while (read < count)
                {
                    int current = queue[read++], x = current % Zone.Width, y = current / Zone.Width;
                    if (current != initial)
                    {
                        bool reached = goal == Goal.Point ? x == targetX && y == targetY
                            : goal == Goal.Frontier ? IsFrontier(zone, actor, x, y)
                            : goal == Goal.Edge && AtEdge(zone, actor, x, y, headingX, headingY);
                        if (reached) { dx = firstX[current]; dy = firstY[current]; return true; }
                        if (goal == Goal.Edge)
                        {
                            int mx = x - start.X, my = y - start.Y;
                            int progress = mx * headingX + my * headingY;
                            int cross = Math.Abs(mx * headingY - my * headingX);
                            if ((headingX == 0 || mx * headingX >= 0) && (headingY == 0 || my * headingY >= 0)
                                && progress > 0 && (progress > bestProgress || (progress == bestProgress && cross < bestCross)))
                            { best = current; bestProgress = progress; bestCross = cross; }
                        }
                    }
                    for (int order = 0; order < 8; order++)
                    {
                        int d = (headingIndex + DirectionOrder[order] + 8) % 8;
                        int nx = x + DX[d], ny = y + DY[d];
                        if (!zone.InBounds(nx, ny)) continue;
                        int next = ny * Zone.Width + nx;
                        if (visited[next] || !Safe(nx, ny)) continue;
                        if (DX[d] != 0 && DY[d] != 0 && !Safe(x + DX[d], y) && !Safe(x, y + DY[d])) continue;
                        visited[next] = true;
                        firstX[next] = current == initial ? (sbyte)DX[d] : firstX[current];
                        firstY[next] = current == initial ? (sbyte)DY[d] : firstY[current];
                        queue[count++] = next;
                    }
                }
                if (best < 0) return false;
                dx = firstX[best]; dy = firstY[best]; return true;
            }
        }
    }
}
