using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    public class LineTraceResult
    {
        public List<Point> Path = new List<Point>();
        public Cell ImpactCell;
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

                if (!zone.InBounds(x, y))
                    break;

                Cell cell = zone.GetCell(x, y);
                if (cell == null)
                    break;

                result.Path.Add(new Point(x, y));
                result.ImpactCell = cell;

                Entity hitCreature = GetFirstCreature(cell, caster);
                if (hitCreature != null)
                {
                    result.HitEntity = hitCreature;
                    return result;
                }

                if (cell.IsSolid())
                {
                    result.BlockedBySolid = true;
                    return result;
                }
            }

            return result;
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
