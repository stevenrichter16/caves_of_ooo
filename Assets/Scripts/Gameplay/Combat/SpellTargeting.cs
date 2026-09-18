using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    public class BeamTraceResult
    {
        public List<Point> Path = new List<Point>();
        public List<Entity> HitEntities = new List<Entity>();
        public Cell ImpactCell;
        public bool BlockedBySolid;

        public Point GetImpactPoint()
        {
            if (ImpactCell != null)
                return new Point(ImpactCell.X, ImpactCell.Y);

            if (Path.Count > 0)
                return Path[Path.Count - 1];

            return new Point(-1, -1);
        }
    }

    public static class SpellTargeting
    {
        public static BeamTraceResult TraceBeam(
            Zone zone,
            Entity caster,
            int startX,
            int startY,
            int dx,
            int dy,
            int maxRange)
        {
            var result = new BeamTraceResult();
            if (zone == null || maxRange <= 0 || (dx == 0 && dy == 0))
                return result;

            int x = startX;
            int y = startY;
            var seenEntities = new HashSet<Entity>();

            for (int step = 0; step < maxRange; step++)
            {
                x += dx;
                y += dy;

                if (!zone.InBounds(x, y))
                    break;

                Cell cell = zone.GetCell(x, y);
                if (cell == null)
                    break;

                result.Path.Add(new Point(x, y));
                SpellFxCapture.PathCell(zone, x, y);
                SpellFxCapture.AffectCell(zone, x, y);
                result.ImpactCell = cell;

                for (int i = 0; i < cell.Occupants.Count; i++)
                {
                    Entity entity = cell.Occupants[i];
                    if (entity == caster || !entity.HasTag("Creature") || !seenEntities.Add(entity))
                        continue;

                    result.HitEntities.Add(entity);
                }

                if (HasBlockingSolid(cell, caster))
                {
                    result.BlockedBySolid = true;
                    return result;
                }
            }

            return result;
        }

        public static List<Entity> GetCreaturesInRadius(
            Zone zone,
            int centerX,
            int centerY,
            int radius,
            Entity exclude = null)
        {
            var result = new List<Entity>();
            if (zone == null || radius < 0)
                return result;

            var seen = new HashSet<Entity>();
            int minX = Math.Max(0, centerX - radius);
            int maxX = Math.Min(Zone.Width - 1, centerX + radius);
            int minY = Math.Max(0, centerY - radius);
            int maxY = Math.Min(Zone.Height - 1, centerY + radius);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int chebyshev = Math.Max(Math.Abs(x - centerX), Math.Abs(y - centerY));
                    if (chebyshev > radius)
                        continue;

                    Cell cell = zone.GetCell(x, y);
                    if (cell == null)
                        continue;

                    SpellFxCapture.AffectCell(zone, x, y);
                    for (int i = 0; i < cell.Occupants.Count; i++)
                    {
                        Entity entity = cell.Occupants[i];
                        if (entity == exclude || !entity.HasTag("Creature") || !seen.Add(entity))
                            continue;

                        result.Add(entity);
                    }
                }
            }

            return result;
        }

        public static List<Entity> FindChainTargets(
            Zone zone,
            Entity caster,
            Entity firstTarget,
            int maxJumps,
            int searchRadius)
        {
            var result = new List<Entity>();
            if (zone == null || firstTarget == null || maxJumps <= 0 || searchRadius <= 0)
                return result;

            var visited = new HashSet<Entity> { caster, firstTarget };
            Entity current = firstTarget;

            for (int jump = 0; jump < maxJumps; jump++)
            {
                Cell currentCell = zone.GetEntityCell(current);
                if (currentCell == null)
                    break;

                Entity next = FindNearestUntargetedCreature(zone, current, searchRadius, visited);
                if (next == null)
                    break;

                result.Add(next);
                visited.Add(next);
                current = next;
            }

            return result;
        }

        private static Entity FindNearestUntargetedCreature(
            Zone zone,
            Entity current,
            int radius,
            HashSet<Entity> excluded)
        {
            Entity best = null;
            int bestChebyshev = int.MaxValue;
            int bestManhattan = int.MaxValue;
            int bestScanOrder = int.MaxValue;
            int scanOrder = 0;

            // Search the body perimeter, not its canonical save/render anchor.
            // Scanning cells in row order preserves the ordinary single-cell tie rule.
            var body = zone.GetOccupiedCells(current);
            if (body.Count == 0) return null;
            int minX = Zone.Width - 1, maxX = 0, minY = Zone.Height - 1, maxY = 0;
            foreach (var occupied in body)
            {
                minX = Math.Min(minX, occupied.X);
                maxX = Math.Max(maxX, occupied.X);
                minY = Math.Min(minY, occupied.Y);
                maxY = Math.Max(maxY, occupied.Y);
            }
            int boundedRadius = Math.Min(radius, Math.Max(Zone.Width, Zone.Height));
            minX = Math.Max(0, minX - boundedRadius);
            maxX = Math.Min(Zone.Width - 1, maxX + boundedRadius);
            minY = Math.Max(0, minY - boundedRadius);
            maxY = Math.Min(Zone.Height - 1, maxY + boundedRadius);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int chebyshev = SpatialQuery.DistanceToCell(zone, current, x, y);
                    if (chebyshev > radius)
                        continue;

                    Cell cell = zone.GetCell(x, y);
                    if (cell == null)
                        continue;

                    for (int i = 0; i < cell.Occupants.Count; i++)
                    {
                        Entity entity = cell.Occupants[i];
                        if (!entity.HasTag("Creature") || excluded.Contains(entity))
                        {
                            scanOrder++;
                            continue;
                        }

                        int manhattan = int.MaxValue;
                        foreach (var occupied in body)
                            manhattan = Math.Min(manhattan, Math.Abs(x - occupied.X) + Math.Abs(y - occupied.Y));
                        if (chebyshev < bestChebyshev ||
                            (chebyshev == bestChebyshev && manhattan < bestManhattan) ||
                            (chebyshev == bestChebyshev && manhattan == bestManhattan && scanOrder < bestScanOrder))
                        {
                            best = entity;
                            bestChebyshev = chebyshev;
                            bestManhattan = manhattan;
                            bestScanOrder = scanOrder;
                        }

                        scanOrder++;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Every Creature inside a cone that opens from the cell in
        /// front of (<paramref name="startX"/>, <paramref name="startY"/>)
        /// along (<paramref name="dx"/>, <paramref name="dy"/>).
        ///
        /// <para>SPELLCRAFT SM2 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §5,
        /// P2). The fourth targeting shape, alongside
        /// <see cref="TraceBeam"/> (line),
        /// <see cref="GetCreaturesInRadius"/> (nova) and
        /// <see cref="FindChainTargets"/> (chain). Flame Jet, Jet Blast
        /// and Backdraft all need it.</para>
        ///
        /// <para><b>Shape.</b> Step <c>n</c> (1-based) is a band
        /// <c>2n-1</c> cells wide, perpendicular to the facing, so
        /// length 3 covers 1 + 3 + 5 = 9 cells. The caster's own cell is
        /// never included.</para>
        ///
        /// <para><b>Occlusion.</b> A solid cell blocks the cone from
        /// spreading further along <i>that ray</i> only — one pillar
        /// must not cancel the whole spray, or a flamethrower would be
        /// useless in any room with cover. Rays are walked outward from
        /// the centre line so the reachable set stays connected: a cell
        /// is only reachable if the cell one step closer to the caster
        /// on the same ray was itself reachable.</para>
        /// </summary>
        /// <param name="length">Steps forward. Zero or negative returns
        /// an empty list rather than throwing.</param>
        public static List<Entity> GetCreaturesInCone(
            Zone zone,
            Entity caster,
            int startX,
            int startY,
            int dx,
            int dy,
            int length)
        {
            var hits = new List<Entity>();
            if (zone == null || length <= 0 || (dx == 0 && dy == 0))
                return hits;

            // Perpendicular to the facing, for the widening band.
            int px = -dy;
            int py = dx;

            var seen = new HashSet<Entity>();
            // Which lateral offsets were still reachable on the previous
            // step. A cell is reachable only if its inward neighbour was
            // — that is what makes a wall occlude rather than merely
            // skip one cell.
            var openPrev = new HashSet<int> { 0 };

            for (int step = 1; step <= length; step++)
            {
                var openNow = new HashSet<int>();
                int halfWidth = step - 1;

                for (int off = -halfWidth; off <= halfWidth; off++)
                {
                    // Reachable if the ray behind it was open. The
                    // centre-line cell of step 1 seeds from offset 0.
                    bool fedFromBehind =
                        openPrev.Contains(off) ||
                        openPrev.Contains(off - 1) ||
                        openPrev.Contains(off + 1);
                    if (!fedFromBehind) continue;

                    int x = startX + dx * step + px * off;
                    int y = startY + dy * step + py * off;
                    if (!zone.InBounds(x, y)) continue;

                    Cell cell = zone.GetCell(x, y);
                    if (cell == null) continue;

                    // A solid cell is hit-tested for nothing and stops
                    // the spread past it on this ray.
                    if (HasBlockingSolid(cell, caster)) continue;
                    openNow.Add(off);
                    SpellFxCapture.AffectCell(zone, x, y);

                    for (int i = 0; i < cell.Occupants.Count; i++)
                    {
                        Entity entity = cell.Occupants[i];
                        if (entity == caster || !entity.HasTag("Creature"))
                            continue;
                        if (!seen.Add(entity)) continue;
                        hits.Add(entity);
                    }
                }

                if (openNow.Count == 0) break;   // fully walled off
                openPrev = openNow;
            }

            return hits;
        }

        private static bool HasBlockingSolid(Cell cell, Entity caster)
        {
            if (cell == null)
                return false;

            for (int i = 0; i < cell.Occupants.Count; i++)
            {
                Entity entity = cell.Occupants[i];
                if (entity == caster || entity.HasTag("Creature"))
                    continue;
                if (entity.HasTag("Solid"))
                    return true;
            }

            return false;
        }
    }
}
