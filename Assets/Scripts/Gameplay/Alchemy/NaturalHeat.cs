namespace CavesOfOoo.Core
{
    /// <summary>
    /// Heat the world gives for nothing (Docs/MACHINE-INVENTED-USES.md, 3p). A terrain that declares a steady heat of its own (<see cref="TileStateSourcePart.Heat"/> above zero, nothing
    /// chilling it, no fuel to burn down) is a source the camp still can use for settling without spending a fire's fuel: a steam vent, an ash bed. The rule is the world's own
    /// declaration, not a list of names, so any terrain that later declares a steady heat joins it and any that stops (heat zero, or chilled) leaves it. It is only low heat: it
    /// settles mire, it does not bake clay (that needs a real fire), and it creates nothing, so the still's four-to-one rule and the feed's finiteness are unchanged.
    /// </summary>
    public static class NaturalHeat
    {
        /// <summary>True for terrain that seeds a steady heat of its own and is not a creature, a carried thing, a chilling source or a fuel-burning fire.</summary>
        public static bool IsSource(Entity e)
        {
            if (e == null || e.HasTag("Creature") || e.GetPart<FuelPart>() != null) return false;
            var physics = e.GetPart<PhysicsPart>(); if (physics != null && (physics.InInventory != null || physics.Equipped != null)) return false;
            var source = e.GetPart<TileStateSourcePart>();
            return source != null && source.Heat > 0 && source.Cold <= 0;
        }

        /// <summary>True when a still could stand within <paramref name="reach"/> cells of <paramref name="at"/>: one already stands there, or the zone takes a set-down kit (open wild ground
        /// in the Spread, Sodden, Beating and Stump; not the Grovelands, towns or any other depth).</summary>
        public static bool StillCanStandNear(Zone zone, Cell at, int reach)
        {
            if (zone == null || at == null) return false;
            if (HeldPlaceZones.RefusalCode(HeldPlaceZones.Classify(WorldLocationContext.For(zone), zone.ZoneID)) == null) return true;
            for (int y = at.Y - reach; y <= at.Y + reach; y++)
                for (int x = at.X - reach; x <= at.X + reach; x++)
                {
                    if (!zone.InBounds(x, y)) continue;
                    var cell = zone.GetCell(x, y); if (cell == null) continue;
                    for (int i = 0; i < cell.Objects.Count; i++)
                    { var e = cell.Objects[i]; if (e != null && e.HasPart<LiquidSettlingPart>() && !PackableFixturePart.IsInertKit(e)) return true; }
                }
            return false;
        }

        /// <summary>The nearest steady heat source within <paramref name="reach"/> cells (Chebyshev) of <paramref name="at"/>, ties by ID, or null.</summary>
        public static Entity Find(Zone zone, Cell at, int reach)
        {
            if (zone == null || at == null) return null;
            Entity best = null; int bestDistance = int.MaxValue;
            for (int y = at.Y - reach; y <= at.Y + reach; y++)
                for (int x = at.X - reach; x <= at.X + reach; x++)
                {
                    if (!zone.InBounds(x, y)) continue;
                    var cell = zone.GetCell(x, y); if (cell == null) continue;
                    int distance = System.Math.Max(System.Math.Abs(x - at.X), System.Math.Abs(y - at.Y));
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        var e = cell.Objects[i]; if (!IsSource(e)) continue;
                        if (distance < bestDistance || (distance == bestDistance && string.CompareOrdinal(e.ID, best.ID) < 0)) { best = e; bestDistance = distance; }
                    }
                }
            return best;
        }
    }
}
