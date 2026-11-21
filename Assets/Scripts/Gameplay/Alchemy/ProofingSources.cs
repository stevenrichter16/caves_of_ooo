using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Where an element's harm settles into something you can catch (Docs/MACHINE-PROOFING.md, the settling tray): a terrain that declares a steady heat or cold of its own (a steam vent, an ash bed, a frost vent,
    /// an ice sheet) or a pool of a liquid the element's table row names (brine for shock, acid and bog-mire for acid). It reads declared facts, never names, so any terrain that later declares a steady heat
    /// or cold, or a new liquid added to a row, joins with no list to edit.
    /// </summary>
    public static class ProofingSources
    {
        /// <summary>How far from a tray (Chebyshev) a source can be. The placement rules refuse a cell beside a hazard (and, for terrain that warms or chills what it touches, the cell in line beyond it), so two cells off is the nearest a tray usually stands.</summary>
        public const int Reach = 2;

        /// <summary>The element a thing is a source of right now, or null: not a creature, not carried, and a declared tile heat or cold, or a pool with liquid left in it of a liquid the element table names.</summary>
        public static ProofingElement ElementOf(Entity e) => KindOf(e, true);

        /// <summary>The element a thing is a source of by what it is made of, whether or not it has anything left to give (a pool drawn dry is still the same structure). Identity of a source must not move when a flask draws a cell dry.</summary>
        static ProofingElement KindOf(Entity e, bool live)
        {
            if (e == null || e.HasTag("Creature")) return null;
            var physics = e.GetPart<PhysicsPart>(); if (physics != null && (physics.InInventory != null || physics.Equipped != null)) return null;
            var tile = e.GetPart<TileStateSourcePart>();
            if (tile != null && e.GetPart<FuelPart>() == null)
            {
                string kind = tile.Heat > 0 && tile.Cold <= 0 ? "heat" : tile.Cold > 0 && tile.Heat <= 0 ? "cold" : "";
                if (kind.Length > 0) foreach (var element in ProofingRegistry.All) if (element.SourceTile == kind) return element;
            }
            // A pool the world made, not one a flask made: pouring brine or mire onto the ground must not conjure a fresh source (and a fresh stock) from a carried liquid, and a thin film is not a source.
            var pool = e.GetPart<LiquidPoolPart>();
            if (pool != null && pool.ParentEntity == e && e.BlueprintName != FishPond.PouredPool && !e.HasPart<FiniteLiquidFilmPart>() && (!live || pool.Volume > 0) && !string.IsNullOrEmpty(pool.LiquidId))
                foreach (var element in ProofingRegistry.All)
                    foreach (var liquid in (element.SourceLiquids ?? "").Split(','))
                        if (liquid.Trim().Length > 0 && string.Equals(liquid.Trim(), pool.LiquidId, StringComparison.OrdinalIgnoreCase)) return element;
            return null;
        }

        /// <summary>Visit the whole connected source within the bounded native zone.
        /// A smaller, origin-relative prefix can elect different stock holders at
        /// opposite edges of one pool and create independent resource budgets.</summary>
        public const int MaxComponent = Zone.Width * Zone.Height;

        /// <summary>
        /// Where the stock of the source <paramref name="source"/> lives. Hazards grow in blobs of one entity a cell, and a blob is one source: the stock is kept on the lowest (y, x) cell of the eight-way connected stretch of cells
        /// that are sources of the same element, as a pond's is on its anchor. Existing stock holders take precedence so a saved source first drawn under the old partial-search rule keeps its budget.
        /// Multiple legacy holders elect the lowest (y, x, ID), never sum their budgets; the others remain dormant while connected. Lookup does not migrate or mutate saved state.
        /// </summary>
        public static Entity StockHolder(Zone zone, Entity source, ProofingElement element)
        {
            var start = zone?.GetEntityCell(source); if (start == null || element == null) return source;
            var seen = new HashSet<(int, int)> { (start.X, start.Y) }; var queue = new Queue<(int x, int y)>(); queue.Enqueue((start.X, start.Y));
            Entity best = null, stocked = null; (int y, int x) bestAt = (int.MaxValue, int.MaxValue), stockedAt = (int.MaxValue, int.MaxValue); int visited = 0;
            while (queue.Count > 0 && visited < MaxComponent)
            {
                var c = queue.Dequeue(); var cell = zone.GetCell(c.x, c.y); if (cell == null) continue;
                Entity here = null;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i]; var kind = KindOf(e, false);
                    if (kind == null || !string.Equals(kind.Id, element.Id, StringComparison.OrdinalIgnoreCase)) continue;
                    if (here == null || string.CompareOrdinal(e.ID, here.ID) < 0) here = e;
                    var stock = e.GetPart<SourceStockPart>();
                    if (stock != null && stock.ParentEntity == e
                        && ((c.y, c.x).CompareTo(stockedAt) < 0
                            || (c.y, c.x) == stockedAt && (stocked == null || string.CompareOrdinal(e.ID, stocked.ID) < 0)))
                    { stocked = e; stockedAt = (c.y, c.x); }
                }
                if (here == null) continue;
                visited++;
                if ((c.y, c.x).CompareTo(bestAt) < 0) { best = here; bestAt = (c.y, c.x); }
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = c.x + dx, ny = c.y + dy;
                        if ((dx != 0 || dy != 0) && zone.InBounds(nx, ny) && seen.Add((nx, ny))) queue.Enqueue((nx, ny));
                    }
            }
            return stocked ?? best ?? source;
        }

        /// <summary>The nearest source within <see cref="Reach"/> cells of <paramref name="at"/> (ties by ID), with the element it is a source of; null when there is none.</summary>
        public static Entity Find(Zone zone, Cell at, out ProofingElement element)
        {
            element = null; if (zone == null || at == null) return null;
            Entity best = null; int bestDistance = int.MaxValue; ProofingElement bestElement = null;
            for (int y = at.Y - Reach; y <= at.Y + Reach; y++)
                for (int x = at.X - Reach; x <= at.X + Reach; x++)
                {
                    if (!zone.InBounds(x, y)) continue; var cell = zone.GetCell(x, y); if (cell == null) continue;
                    int distance = Math.Max(Math.Abs(x - at.X), Math.Abs(y - at.Y));
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        var e = cell.Objects[i]; var kind = ElementOf(e); if (kind == null) continue;
                        if (distance < bestDistance || (distance == bestDistance && string.CompareOrdinal(e.ID, best.ID) < 0)) { best = e; bestDistance = distance; bestElement = kind; }
                    }
                }
            element = bestElement; return best;
        }
    }

    /// <summary>
    /// What a source gives up (Docs/MACHINE-PROOFING.md, the settling tray), kept on the source and shared by every tray that catches from it. Like a pond's fish: a source starts full at a capacity set by where it
    /// is (<see cref="MinCapacity"/> to <see cref="MaxCapacity"/>, stable for a given zone, cell and blueprint) and refills one catch a day, up to its capacity. Time is reconciled when a tray is read or emptied.
    /// </summary>
    public sealed class SourceStockPart : Part
    {
        public override string Name => "SourceStock";
        public const int MinCapacity = 3, MaxCapacity = 5, RegenTicks = WorldClock.DayLengthTicks;

        public int Capacity, Available, LastTick;

        /// <summary>The stock on a source, created full on first use. Null when there is no source.</summary>
        public static SourceStockPart For(Zone zone, Entity source, int now)
        {
            if (zone == null || source == null) return null;
            var existing = source.GetPart<SourceStockPart>(); if (existing != null && existing.ParentEntity == source) return existing;
            int capacity = CapacityFor(zone, source);
            var made = new SourceStockPart { Capacity = capacity, Available = capacity, LastTick = now }; source.AddPart(made); return made;
        }

        /// <summary>What a source's capacity is (or will be, once someone draws on it): stable for a zone, blueprint and cell, so a reader can say what an untouched source holds without making its stock.</summary>
        public static int CapacityFor(Zone zone, Entity source)
        {
            if (zone == null || source == null) return MaxCapacity;
            var at = zone.GetEntityCell(source); int span = MaxCapacity - MinCapacity + 1;
            return MinCapacity + FormationSelector.StableIndex(zone.ZoneID + ":source:" + source.BlueprintName + ":" + (at?.X ?? 0) + "," + (at?.Y ?? 0), span);
        }

        public int AvailableAt(int now)
        {
            if (Available >= Capacity) return Capacity;
            int regen = now > LastTick ? (now - LastTick) / RegenTicks : 0;
            return Math.Min(Capacity, Available + Math.Max(0, regen));
        }

        public void Reconcile(int now)
        {
            if (Available >= Capacity) { Available = Capacity; LastTick = now; return; }
            if (now <= LastTick) return;
            int regen = (now - LastTick) / RegenTicks; if (regen <= 0) return;
            Available = Math.Min(Capacity, Available + regen);
            LastTick = Available >= Capacity ? now : LastTick + regen * RegenTicks;
        }

        /// <summary>How a source looks to someone beside it: a hint, never a count.</summary>
        public static string Look(int available, int capacity)
            => available <= 0 ? "spent for now" : available * 3 <= capacity ? "thin" : available >= capacity ? "plentiful" : "well drawn but not spent";
    }
}
