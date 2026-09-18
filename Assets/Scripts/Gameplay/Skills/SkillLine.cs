using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Shared line-walk for the actives that hit everything in a
    /// direction rather than chaining through conductors.
    ///
    /// <para>Named for the shape, not a tree: it started inside
    /// Galvanism but SM4's <see cref="Pyromancy_EmberSpit"/> needs the
    /// identical walk, and a tree-specific name would have every future
    /// caller look like it was borrowing someone else's code.</para>
    ///
    /// <para>SPELLCRAFT SM3. <see cref="Galvanism_GroundSurge"/> and
    /// <see cref="Galvanism_RailSpike"/> need the identical walk —
    /// stop at stone, collect every creature, skip the caster — and
    /// differ only in what they then DO with the list. Two copies of a
    /// walk is how the two powers would drift apart the first time
    /// either is tuned.</para>
    ///
    /// <para><b>Why collect-then-act.</b> Both callers mutate their
    /// targets (shove, kill). Acting during the walk would let a shoved
    /// target land in a cell the walk has not reached yet and be hit
    /// twice from a single cast. Returning a snapshot makes that class
    /// of bug unrepresentable in the callers.</para>
    ///
    /// <para><b>Not</b> shared with <see cref="Galvanism_Overload"/>,
    /// which walks the same geometry but with different stopping rules:
    /// its chain breaks on the first non-conductor and continues through
    /// empty cells. Merging the two would mean a walk with a behaviour
    /// flag, which is harder to read than two honest walks.</para>
    /// </summary>
    /// <remarks>
    /// Public rather than internal so the walk can be tested directly:
    /// the project has no <c>InternalsVisibleTo</c>, and the stop-order
    /// rule below (collect the cell, THEN check solidity) is exactly the
    /// kind of thing that regresses invisibly through a caller.
    /// </remarks>
    public static class SkillLine
    {
        /// <summary>
        /// Every Creature in the <paramref name="range"/> cells ahead of
        /// (<paramref name="startX"/>, <paramref name="startY"/>) along
        /// (<paramref name="dx"/>, <paramref name="dy"/>), nearest
        /// first. The walk stops at a solid cell and at the zone edge;
        /// <paramref name="actor"/> is never included.
        /// </summary>
        /// <summary>
        /// PALIMPSEST P2 — the CELLS a line passes through, using the
        /// identical stop rules as <see cref="Collect"/>: the zone edge,
        /// a null cell, or stone. Abilities that write to the ground
        /// need coordinates, not occupants.
        /// </summary>
        public static List<Point> CollectCells(
            Zone zone, Entity actor, int startX, int startY,
            int dx, int dy, int range)
        {
            var cells = new List<Point>();
            if (zone == null || range <= 0 || (dx == 0 && dy == 0)) return cells;

            int x = startX, y = startY;
            for (int step = 0; step < range; step++)
            {
                x += dx; y += dy;
                if (!zone.InBounds(x, y)) break;
                var cell = zone.GetCell(x, y);
                if (cell == null) break;
                if (cell.IsSolid()) break;
                cells.Add(new Point(x, y));
                SpellFxCapture.PathCell(zone, x, y);
            }
            return cells;
        }

        public static List<Entity> Collect(
            Zone zone, Entity actor, int startX, int startY,
            int dx, int dy, int range)
        {
            bool ignored;
            return Collect(zone, actor, startX, startY, dx, dy, range, out ignored);
        }

        /// <summary>
        /// As <see cref="Collect(Zone, Entity, int, int, int, int, int)"/>,
        /// but also reports whether the walk was cut short by stone.
        /// Callers use it to tell "nothing was there" from "a wall was in
        /// the way" in their reject diag — two very different answers to
        /// a player asking why a cast did nothing.
        /// </summary>
        public static List<Entity> Collect(
            Zone zone, Entity actor, int startX, int startY,
            int dx, int dy, int range, out bool blockedByWall, List<Point> visitedCells = null)
        {
            blockedByWall = false;
            var found = new List<Entity>();
            var seen = new HashSet<Entity>();
            if (zone == null || range <= 0 || (dx == 0 && dy == 0)) return found;

            int x = startX, y = startY;
            for (int step = 0; step < range; step++)
            {
                x += dx; y += dy;
                if (!zone.InBounds(x, y)) break;

                var cell = zone.GetCell(x, y);
                if (cell == null) break;

                visitedCells?.Add(new Point(x, y));
                SpellFxCapture.PathCell(zone, x, y);

                // Creatures first WITHIN the cell, so a monster standing in
                // a bush is what a single-target power like Ember Spit
                // picks — you aim at the thing that can hit back.
                for (int i = 0; i < cell.Occupants.Count; i++)
                {
                    var e = cell.Occupants[i];
                    if (AbilityTargeting.IsCreatureTarget(e, actor) && seen.Add(e)) found.Add(e);
                }
                for (int i = 0; i < cell.Occupants.Count; i++)
                {
                    var e = cell.Occupants[i];
                    if (e != null && !e.Tags.ContainsKey("Creature")
                        && AbilityTargeting.IsElementalTarget(e, actor) && seen.Add(e))
                        found.Add(e);
                }

                // Stone stops the line — but AFTER the cell's contents have
                // been collected, not before. A tree carries the Solid tag,
                // so the old order meant the walk broke out having never
                // looked at it: a fire bolt aimed point-blank at a tree
                // reported "line_blocked" and left the tree untouched.
                // Now the tree is hit, and THEN the line stops on it.
                if (cell.IsSolid()) { blockedByWall = true; break; }
            }

            return found;
        }
    }
}
