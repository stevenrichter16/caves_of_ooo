using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios
{
    /// <summary>
    /// Pure-function position helpers used by <see cref="Builders.EntityBuilder"/>
    /// to resolve scenario positioning parameters to concrete cell coordinates.
    ///
    /// Kept as a separate static class rather than private helpers on EntityBuilder
    /// so the logic is unit-testable without a live game context — just pass a
    /// <see cref="Zone"/> with the desired terrain configuration.
    ///
    /// All distances use Chebyshev metric (max of |dx|, |dy|) to match Caves of Ooo's
    /// 8-directional movement / sight model.
    ///
    /// Public rather than internal so scenario authors can call these helpers
    /// directly when the fluent EntityBuilder doesn't cover an edge case, and so
    /// PlayMode / EditMode tests can reach them from the tests assembly.
    /// </summary>
    public static class PositionResolver
    {
        /// <summary>
        /// Collect every passable cell in <paramref name="zone"/> whose Chebyshev
        /// distance from (<paramref name="centerX"/>, <paramref name="centerY"/>) is
        /// in the inclusive range [<paramref name="minRadius"/>, <paramref name="maxRadius"/>].
        ///
        /// The center cell itself is excluded even if minRadius=0 (a scenario spawning
        /// "near the player" shouldn't land on the player). Off-map candidates are
        /// silently filtered.
        ///
        /// Returns an empty list if no passable cell exists in the band.
        /// </summary>
        public static List<(int x, int y)> CollectCellsInRadiusBand(
            Zone zone, int centerX, int centerY, int minRadius, int maxRadius)
        {
            var result = new List<(int x, int y)>();
            if (zone == null || maxRadius < minRadius) return result;
            if (minRadius < 0) minRadius = 0;

            for (int dy = -maxRadius; dy <= maxRadius; dy++)
            {
                for (int dx = -maxRadius; dx <= maxRadius; dx++)
                {
                    if (dx == 0 && dy == 0) continue; // always skip center
                    int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    if (d < minRadius || d > maxRadius) continue;

                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (!zone.InBounds(x, y)) continue;

                    var cell = zone.GetCell(x, y);
                    if (cell != null && cell.IsPassable())
                        result.Add((x, y));
                }
            }
            return result;
        }

        /// <summary>
        /// Compute the grid cell for ring position <paramref name="indexOf"/> of
        /// <paramref name="totalOfN"/> evenly-distributed points at the given
        /// <paramref name="radius"/> around (<paramref name="centerX"/>, <paramref name="centerY"/>).
        ///
        /// Uses standard trig: angle = 2π · i / N, then rounds x/y to nearest int.
        /// At small radii, adjacent indices may round to the same cell — expected.
        /// Authors needing fine control should use explicit <c>.AtPlayerOffset</c>.
        ///
        /// No bounds or passability check here; this is pure math. The caller is
        /// responsible for validating the result.
        /// </summary>
        public static (int x, int y) ComputeRingPosition(
            int centerX, int centerY, int radius, int indexOf, int totalOfN)
        {
            if (totalOfN <= 0) totalOfN = 1;
            if (radius < 0) radius = 0;

            double angle = 2.0 * Math.PI * indexOf / totalOfN;
            int x = centerX + (int)Math.Round(Math.Cos(angle) * radius);
            int y = centerY + (int)Math.Round(Math.Sin(angle) * radius);
            return (x, y);
        }

        /// <summary>
        /// Scan the zone in row-major order (x=0..Width-1 for each y=0..Height-1)
        /// and return the first passable cell matching <paramref name="predicate"/>.
        /// Returns null if no match exists.
        ///
        /// Row-major is chosen so scenarios with "find a cell that…" semantics get
        /// deterministic results across runs — the same zone yields the same first
        /// match.
        /// </summary>
        public static (int x, int y)? FindFirstPassableCell(Zone zone, Func<Cell, bool> predicate)
        {
            if (zone == null || predicate == null) return null;

            for (int y = 0; y < Zone.Height; y++)
            {
                for (int x = 0; x < Zone.Width; x++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell == null) continue;
                    if (!cell.IsPassable()) continue;
                    if (predicate(cell)) return (x, y);
                }
            }
            return null;
        }
    }
}
