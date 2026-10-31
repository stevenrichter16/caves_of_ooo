using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// P4 (Docs/PLACE-WORK-AND-PROGRESSION.md §3 rule 3): the one tier a zone plays at.
    /// On the surface it is the authored cell tier (<see cref="WorldMapAuthoring.TierAt"/>);
    /// below ground it is one deeper for floors 1-3 and one more for every three floors
    /// after, capped at <see cref="ProgressionCurve.MaxTier"/>. Zones outside the world
    /// map (tests, benches, interiors) have no tier (0), and nothing filters by it.
    /// </summary>
    public static class ZoneTierPolicy
    {
        public static int TierFor(string zoneId)
        {
            if (zoneId == null || !WorldMap.IsOverworldZoneID(zoneId)) return 0;
            var (x, y, z) = WorldMap.FromZoneID(zoneId);
            if (!WorldMapAuthoring.InBounds(x, y) || z < 0) return 0;
            return TierBelow(WorldMapAuthoring.TierAt(x, y), z);
        }

        /// <summary>The tier of floor <paramref name="depth"/> under a surface of
        /// <paramref name="surfaceTier"/> (depth 0 is the surface itself).</summary>
        public static int TierBelow(int surfaceTier, int depth)
        {
            int tier = surfaceTier + (depth > 0 ? 1 + (depth - 1) / 3 : 0);
            return Math.Max(1, Math.Min(ProgressionCurve.MaxTier, tier));
        }
    }
}
