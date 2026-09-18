using CavesOfOoo.Core;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// SPELLCRAFT SM8 — the small amount of targeting the rites share.
    ///
    /// <para>Rites live in the mutation namespace and so cannot reach
    /// <c>SkillLine</c>, which is internal to the skills assembly
    /// namespace. Rather than make that internal helper public for one
    /// caller, the line walk a rite needs is spelled out once here.</para>
    /// </summary>
    internal static class RiteTargeting
    {
        /// <summary>Nearest creature along a direction, stopping at
        /// stone and at the zone edge. Null when the line is empty.</summary>
        internal static Entity FirstCreatureInLine(
            Zone zone, Entity caster, int startX, int startY, int dx, int dy, int range)
        {
            if (zone == null || range <= 0 || (dx == 0 && dy == 0)) return null;

            int x = startX, y = startY;
            for (int step = 0; step < range; step++)
            {
                x += dx; y += dy;
                if (!zone.InBounds(x, y)) break;
                var cell = zone.GetCell(x, y);
                if (cell == null || cell.IsSolid()) break;

                SpellFxCapture.PathCell(zone, x, y);
                for (int i = 0; i < cell.Occupants.Count; i++)
                {
                    var e = cell.Occupants[i];
                    if (e == null || e == caster) continue;
                    if (e.Tags.ContainsKey("Creature")) return e;
                }
            }
            return null;
        }
    }
}
