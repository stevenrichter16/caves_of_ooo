using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// What counts as a fishable pond (Docs/MACHINE-INVENTED-USES.md 3n). A pond is a connected stretch (eight-way) of at least <see cref="MinCells"/> cells of fresh-water pool owners that carry
    /// <see cref="FishTag"/> (the cascade basins of the Stump today: "things live in it that live nowhere drier"; new water opts in by carrying the tag, and the cascade basins generated before the tag
    /// existed are recognised by name). Its identity is its anchor, the structural pool owner with the lowest (y, x). <b>Identity ignores volume and ice</b>: drawing water from the anchor, or frost
    /// freezing it, must never move the pond's stock or split one pond into two; only a cell that is actually drawable (water left in it, not frozen) can be fished from. The stock lives on the
    /// anchor (<see cref="FishStockPart"/>), so every trap on the same water draws from one place-bound supply. Read-only.
    /// </summary>
    public static class FishPond
    {
        public const int MinCells = 12, MaxCells = 800;
        public const string PouredPool = "PouredLiquidPool", FishTag = "FishWater", LegacyBlueprint = "SprayPool";

        /// <summary>The pool owner of a fishable-water cell, whatever its volume or ice: the pond's structure. Null when the cell holds none.</summary>
        public static Entity StructuralAt(Zone zone, int x, int y)
        {
            if (zone == null || !zone.InBounds(x, y)) return null;
            var cell = zone.GetCell(x, y); if (cell == null) return null;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var e = cell.Objects[i]; var pool = e?.GetPart<LiquidPoolPart>();
                if (pool == null || pool.ParentEntity != e || pool.LiquidId != "water" || e.BlueprintName == PouredPool || e.HasPart<FiniteLiquidFilmPart>()) continue;
                if (e.HasTag(FishTag) || e.BlueprintName == LegacyBlueprint) return e;
            }
            return null;
        }

        /// <summary>The fishable-water owner in the cell that could be fished from or drawn from right now: water left in it and not frozen. Null otherwise.</summary>
        public static Entity PoolAt(Zone zone, int x, int y)
        {
            var e = StructuralAt(zone, x, y);
            return e != null && e.GetPart<LiquidPoolPart>().Volume > 0 && LiquidSourcePhase.CanDrawWater(zone, e) ? e : null;
        }

        /// <summary>The pond beside (x, y): every structural cell of the largest pond that has a drawable cell next to (x, y) (ties go to the lowest anchor), capped at <see cref="MaxCells"/>. Empty when none touches it.</summary>
        public static List<Entity> Component(Zone zone, int x, int y)
        {
            List<Entity> best = null; (int y, int x) bestAnchor = (int.MaxValue, int.MaxValue);
            var claimed = new HashSet<(int, int)>();
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int sx = x + dx, sy = y + dy;
                    if (claimed.Contains((sx, sy)) || PoolAt(zone, sx, sy) == null) continue;
                    var cells = new List<Entity>(); var seen = new HashSet<(int, int)> { (sx, sy) }; var queue = new Queue<(int x, int y)>(); queue.Enqueue((sx, sy));
                    (int y, int x) anchor = (int.MaxValue, int.MaxValue);
                    while (queue.Count > 0 && cells.Count < MaxCells)
                    {
                        var c = queue.Dequeue(); cells.Add(StructuralAt(zone, c.x, c.y)); claimed.Add((c.x, c.y));
                        if ((c.y, c.x).CompareTo(anchor) < 0) anchor = (c.y, c.x);
                        for (int ax = -1; ax <= 1; ax++)
                            for (int ay = -1; ay <= 1; ay++)
                            {
                                int nx = c.x + ax, ny = c.y + ay;
                                if ((ax != 0 || ay != 0) && StructuralAt(zone, nx, ny) != null && seen.Add((nx, ny))) queue.Enqueue((nx, ny));
                            }
                    }
                    if (best == null || cells.Count > best.Count || (cells.Count == best.Count && anchor.CompareTo(bestAnchor) < 0)) { best = cells; bestAnchor = anchor; }
                }
            return best ?? new List<Entity>();
        }

        /// <summary>The pond beside (x, y): its anchor owner, or false when no fishable pond touches the cell.</summary>
        public static bool TryFind(Zone zone, int x, int y, out Entity anchor)
        {
            anchor = null;
            var water = Component(zone, x, y);
            if (water.Count < MinCells) return false;
            (int y, int x) best = (int.MaxValue, int.MaxValue);
            foreach (var e in water)
            {
                var at = zone.GetEntityCell(e); if (at == null) continue;
                if ((at.Y, at.X).CompareTo(best) < 0) { best = (at.Y, at.X); anchor = e; }
            }
            return anchor != null;
        }
    }
}
