namespace CavesOfOoo.Core
{
    /// <summary>
    /// Furniture part marking an alchemy still — the brewing station.
    /// Per the M1 design lockdown (Docs/CRAFTING-ALCHEMY-SYSTEM.md §6.2),
    /// tonics/coatings/throwables require a still; only simple Foods may be
    /// field-brewed. Same furniture shape as ChairPart/BedPart: a marker
    /// part on a PhysicalObject blueprint.
    /// </summary>
    public class AlchemyStillPart : Part
    {
        public override string Name => "AlchemyStill";

        /// <summary>
        /// True when <paramref name="actor"/> stands on or orthogonally/
        /// diagonally adjacent to (3×3 box) a cell containing an entity
        /// with an AlchemyStillPart. False when the zone is null or the
        /// actor isn't placed in it.
        /// </summary>
        public static bool IsNearStill(Entity actor, Zone zone)
        {
            if (actor == null || zone == null)
                return false;

            (int x, int y) = zone.GetEntityPosition(actor);
            if (x < 0 || y < 0)
                return false;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Cell cell = zone.GetCell(x + dx, y + dy);
                    if (cell == null)
                        continue;

                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        Entity obj = cell.Objects[i];
                        if (obj != null && obj.HasPart<AlchemyStillPart>())
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
