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
                result.ImpactCell = cell;

                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    Entity entity = cell.Objects[i];
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

                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        Entity entity = cell.Objects[i];
                        if (entity == exclude || !entity.HasTag("Creature"))
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

                Entity next = FindNearestUntargetedCreature(zone, currentCell.X, currentCell.Y, searchRadius, visited);
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
            int centerX,
            int centerY,
            int radius,
            HashSet<Entity> excluded)
        {
            Entity best = null;
            int bestChebyshev = int.MaxValue;
            int bestManhattan = int.MaxValue;
            int bestScanOrder = int.MaxValue;
            int scanOrder = 0;

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

                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        Entity entity = cell.Objects[i];
                        if (!entity.HasTag("Creature") || excluded.Contains(entity))
                        {
                            scanOrder++;
                            continue;
                        }

                        int manhattan = Math.Abs(x - centerX) + Math.Abs(y - centerY);
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

        private static bool HasBlockingSolid(Cell cell, Entity caster)
        {
            if (cell == null)
                return false;

            for (int i = 0; i < cell.Objects.Count; i++)
            {
                Entity entity = cell.Objects[i];
                if (entity == caster || entity.HasTag("Creature"))
                    continue;
                if (entity.HasTag("Solid"))
                    return true;
            }

            return false;
        }
    }
}
