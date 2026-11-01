using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// EC5 (Docs/ECONOMY-SYSTEM.md §5.2): what a good is worth depends on where it is sold. Canon
    /// (Lore/History/08_MaterialCulture.md §II): salt is "over-abundant near salt-flats; precious far from
    /// them". A trader's price for a salt good scales with its world distance from the nearest salt pan,
    /// from <see cref="Near"/> beside the flats to <see cref="Far"/> far away, which is also the reason the
    /// Concord's caravans exist: carrying salt inland pays.
    ///
    /// <para>The factor scales the good's value itself, so a far trader both charges more for salt and pays
    /// more for it (unlike <see cref="PlaceConsequences.PriceModifier"/>, which moves a place's prices
    /// against the player both ways). Salt goods are blueprints tagged <see cref="SaltTag"/>. The salt-pan
    /// map is a pure function of the authored world, computed once.</para>
    /// </summary>
    public static class RegionalValue
    {
        public const string SaltTag = "TradeSalt";
        public const double Near = 0.5, Far = 2.0, PerZone = 0.15;

        static List<(int x, int y)> pans;

        /// <summary>The value factor for <paramref name="item"/> sold by <paramref name="trader"/>; 1 for
        /// anything that is not salt, or a trader standing nowhere on the surface map.</summary>
        public static double Modifier(Entity item, Entity trader)
        {
            if (item == null || !item.HasTag(SaltTag)) return 1.0;
            int d = DistanceToFlats(trader?.SpatialZone?.ZoneID);
            return d < 0 ? 1.0 : Factor(d);
        }

        /// <summary>The factor at <paramref name="zones"/> from the nearest salt pan.</summary>
        public static double Factor(int zones) => Math.Min(Far, Near + PerZone * Math.Max(0, zones));

        /// <summary>Chebyshev distance in world cells from the zone to the nearest salt pan, or -1 when the
        /// zone is not on the world map (or the world has no salt pan).</summary>
        public static int DistanceToFlats(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || !zoneId.StartsWith("Overworld.", StringComparison.Ordinal)) return -1;
            int x, y;
            try { (x, y, _) = WorldMap.FromZoneID(zoneId); } catch (Exception) { return -1; }
            if (!WorldMapAuthoring.InBounds(x, y)) return -1;
            int best = -1;
            foreach (var (px, py) in Pans())
            {
                int d = Math.Max(Math.Abs(px - x), Math.Abs(py - y));
                if (best < 0 || d < best) best = d;
            }
            return best;
        }

        static List<(int x, int y)> Pans()
        {
            if (pans != null) return pans;
            var found = new List<(int x, int y)>();
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                    if (WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Beating
                        && FormationSelector.For(BiomeType.Beating, WorldMap.ToZoneID(x, y, 0)) == Formation.SaltPan)
                        found.Add((x, y));
            return pans = found;
        }
    }
}
