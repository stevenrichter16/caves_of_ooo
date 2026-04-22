using System.Collections.Generic;
using System;

namespace CavesOfOoo.Core
{
    public class LineTraceResult
    {
        public List<Point> Path = new List<Point>();
        public Cell ImpactCell;
        public Cell LastTraversableCell;
        public Entity HitEntity;
        public bool BlockedBySolid;

        public bool HasImpactCell => ImpactCell != null;

        public Point GetImpactPoint()
        {
            if (ImpactCell != null)
                return new Point(ImpactCell.X, ImpactCell.Y);

            if (Path.Count > 0)
                return Path[Path.Count - 1];

            return new Point(-1, -1);
        }
    }

    /// <summary>
    /// Shared straight-line targeting helper for projectile-style abilities.
    /// </summary>
    public static class LineTargeting
    {
        public static LineTraceResult TraceFirstImpact(
            Zone zone,
            Entity caster,
            int startX,
            int startY,
            int dx,
            int dy,
            int maxRange)
        {
            var result = new LineTraceResult();

            if (zone == null || maxRange <= 0)
                return result;

            if (dx == 0 && dy == 0)
                return result;

            int x = startX;
            int y = startY;

            for (int step = 0; step < maxRange; step++)
            {
                x += dx;
                y += dy;
                if (StepTrace(zone, caster, x, y, result))
                    return result;
            }

            return result;
        }

        public static LineTraceResult TraceFirstImpactToTarget(
            Zone zone,
            Entity caster,
            int startX,
            int startY,
            int targetX,
            int targetY,
            int maxRange)
        {
            var result = new LineTraceResult();

            if (zone == null || maxRange < 0 || !zone.InBounds(startX, startY) || !zone.InBounds(targetX, targetY))
                return result;

            int distance = AIHelpers.ChebyshevDistance(startX, startY, targetX, targetY);
            if (distance > maxRange)
                return result;

            if (startX == targetX && startY == targetY)
            {
                result.ImpactCell = zone.GetCell(startX, startY);
                result.LastTraversableCell = result.ImpactCell;
                return result;
            }

            int x = startX;
            int y = startY;
            int dx = Math.Abs(targetX - startX);
            int dy = Math.Abs(targetY - startY);
            int sx = startX < targetX ? 1 : -1;
            int sy = startY < targetY ? 1 : -1;
            int err = dx - dy;
            int steps = 0;

            while ((x != targetX || y != targetY) && steps < maxRange)
            {
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }

                steps++;
                if (StepTrace(zone, caster, x, y, result))
                    return result;
            }

            return result;
        }

        /// <summary>
        /// Records a single trace step at (x, y) into result.
        /// Returns true if tracing should stop (out-of-bounds, null cell, hit entity, or solid wall).
        /// </summary>
        private static bool StepTrace(Zone zone, Entity caster, int x, int y, LineTraceResult result)
        {
            if (!zone.InBounds(x, y))
                return true;

            Cell cell = zone.GetCell(x, y);
            if (cell == null)
                return true;

            result.Path.Add(new Point(x, y));
            result.ImpactCell = cell;
            if (!cell.IsSolid())
                result.LastTraversableCell = cell;

            Entity hitCreature = GetFirstCreature(cell, caster);
            if (hitCreature != null)
            {
                result.HitEntity = hitCreature;
                return true;
            }

            Entity hitObject = GetFirstTargetableObject(cell, caster);
            if (hitObject != null)
            {
                result.HitEntity = hitObject;
                return true;
            }

            if (cell.IsSolid())
            {
                result.BlockedBySolid = true;
                return true;
            }

            return false;
        }

        private static Entity GetFirstTargetableObject(Cell cell, Entity caster)
        {
            if (cell == null)
                return null;

            for (int i = 0; i < cell.Objects.Count; i++)
            {
                Entity entity = cell.Objects[i];
                if (entity == caster)
                    continue;
                if (entity.HasTag("Creature") || entity.HasTag("Wall") || entity.HasTag("Terrain"))
                    continue;

                if (entity.GetStat("Hitpoints") != null
                    || entity.GetPart<ThermalPart>() != null
                    || entity.GetPart<MaterialPart>() != null
                    || entity.GetPart<PhysicsPart>() != null)
                {
                    return entity;
                }
            }

            return null;
        }

        private static Entity GetFirstCreature(Cell cell, Entity caster)
        {
            if (cell == null)
                return null;

            for (int i = 0; i < cell.Objects.Count; i++)
            {
                Entity entity = cell.Objects[i];
                if (entity == caster)
                    continue;
                if (entity.HasTag("Creature"))
                    return entity;
            }

            return null;
        }
    }
}
