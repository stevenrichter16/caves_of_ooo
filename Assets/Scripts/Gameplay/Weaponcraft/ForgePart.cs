namespace CavesOfOoo.Core
{
    /// <summary>
    /// Furniture part marking a tinker's forge — the weaponcraft station.
    /// Per M3-L3 (Docs/CRAFTING-ALCHEMY-SYSTEM.md), forging, re-forging and
    /// quenching all happen where the metal is worked: the forge/temper
    /// commands gate on this adjacency the same way brewing gates on
    /// <see cref="AlchemyStillPart"/>. Same furniture shape as
    /// ChairPart/BedPart: a marker part on a PhysicalObject blueprint.
    /// </summary>
    public class ForgePart : Part
    {
        public override string Name => "Forge";

        /// <summary>
        /// True when <paramref name="actor"/> stands on or orthogonally/
        /// diagonally adjacent to (3×3 box) a cell containing an entity
        /// with a ForgePart. False when the zone is null or the actor
        /// isn't placed in it.
        /// </summary>
        public static bool IsNearForge(Entity actor, Zone zone)
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
                        if (obj != null && obj.HasPart<ForgePart>())
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
