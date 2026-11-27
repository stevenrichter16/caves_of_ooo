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
        // A zone is already finite. A smaller traversal cap can assign one
        // connected pond different anchors when approached from opposite shores.
        public const int MinCells = 12, MaxCells = Zone.Width * Zone.Height;
        public const string PouredPool = "PouredLiquidPool", FishTag = "FishWater", LegacyBlueprint = "SprayPool";
        internal enum Activity { None, Quiet, Thin, Active }

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

        /// <summary>The pond beside (x, y): retain its lowest existing stock
        /// holder, or use the structural minimum when it has never been fished.
        /// This preserves pre-existing budgets after expanding a legacy search;
        /// multiple old records are never added together or rewritten.</summary>
        public static bool TryFind(Zone zone, int x, int y, out Entity anchor)
        {
            anchor = null;
            var water = Component(zone, x, y);
            if (water.Count < MinCells) return false;
            anchor = StockHolder(zone, water);
            return anchor != null;
        }

        static Entity StockHolder(Zone zone, List<Entity> water)
        {
            (int y, int x) best = (int.MaxValue, int.MaxValue);
            Entity anchor = null, stocked = null;
            (int y, int x) stockedAt = (int.MaxValue, int.MaxValue);
            foreach (var e in water)
            {
                var at = zone.GetEntityCell(e); if (at == null) continue;
                if ((at.Y, at.X).CompareTo(best) < 0) { best = (at.Y, at.X); anchor = e; }
                var stock = e.GetPart<FishStockPart>();
                if (stock?.ParentEntity == e && (at.Y, at.X).CompareTo(stockedAt) < 0)
                { stocked = e; stockedAt = (at.Y, at.X); }
            }
            return stocked ?? anchor;
        }

        static bool Disclosed(Cell cell) => cell is { Explored: true, IsVisible: true };

        static bool ShowsSurface(Zone zone, Cell cell, Entity owner)
            => Disclosed(cell) && owner != null && owner.GetPart<RenderPart>() is RenderPart render
                && render.ParentEntity == owner && render.Visible && PoolAt(zone, cell.X, cell.Y) == owner;

        static Activity ReadActivity(Entity holder, int now)
        {
            var stock = holder?.GetPart<FishStockPart>();
            if (holder == null) return Activity.None;
            if (stock == null || stock.ParentEntity != holder) return Activity.Active;
            int available = stock.AvailableAt(now);
            if (available <= 0) return Activity.Quiet;
            return available * 3 <= stock.Capacity ? Activity.Thin : Activity.Active;
        }

        /// <summary>A cue on the visible, drawable surface of this exact basin.
        /// Only the actual pond ledger is projected. Traps have committed no
        /// catch merely because their readers forecast one.</summary>
        internal static Activity VisibleActivity(Zone zone, Cell cell, int now)
        {
            if (zone == null || !Disclosed(cell) || cell.ParentZone != zone || zone.GetCell(cell.X, cell.Y) != cell)
                return Activity.None;
            var owner = StructuralAt(zone, cell.X, cell.Y);
            return ShowsSurface(zone, cell, owner) && TryFind(zone, cell.X, cell.Y, out var holder)
                ? ReadActivity(holder, now) : Activity.None;
        }

        /// <summary>Choose one visible shore tile per active basin. Traverse each
        /// touched component once per paint refresh, never once per water tile.</summary>
        internal static void FillShoreMarks(Zone zone, Cell observer, int now, Dictionary<Cell, Activity> marks)
        {
            marks.Clear();
            if (zone == null) return;
            HashSet<Entity> seen = null;
            foreach (var owner in zone.GetReadOnlyEntitiesWithPart<LiquidPoolPart>())
            {
                if (seen?.Contains(owner) == true) continue;
                var at = zone.GetEntityCell(owner);
                if (!ShowsSurface(zone, at, owner)) continue;
                var water = Component(zone, at.X, at.Y);
                (seen ??= new HashSet<Entity>()).UnionWith(water);
                if (water.Count < MinCells) continue;
                var activity = ReadActivity(StockHolder(zone, water), now);
                if (activity != Activity.Active && activity != Activity.Thin) continue;

                Cell best = null;
                (int distance, int y, int x) bestRank = (int.MaxValue, int.MaxValue, int.MaxValue);
                foreach (var member in water)
                {
                    var candidate = zone.GetEntityCell(member);
                    if (!Disclosed(candidate) || candidate.GetTopVisibleObject() != member || !OnBank(zone, candidate)
                        || !ShowsSurface(zone, candidate, member)) continue;
                    int distance = observer == null ? 0 : Math.Abs(observer.X - candidate.X) + Math.Abs(observer.Y - candidate.Y);
                    var rank = (distance, candidate.Y, candidate.X);
                    if (rank.CompareTo(bestRank) < 0) { best = candidate; bestRank = rank; }
                }
                if (best != null) marks.Add(best, activity);
            }
        }

        static bool OnBank(Zone zone, Cell cell)
            => StructuralAt(zone, cell.X - 1, cell.Y) == null || StructuralAt(zone, cell.X + 1, cell.Y) == null
                || StructuralAt(zone, cell.X, cell.Y - 1) == null || StructuralAt(zone, cell.X, cell.Y + 1) == null;
    }
}
