using System;
using CavesOfOoo.Core;

namespace CavesOfOoo.Concerns
{
    /// <summary>
    /// C1: a place's repair sites addressed by the place's authored name rather than by a
    /// conversation speaker, so quest objectives (evaluated on tick, with no speaker) can
    /// observe them. Backs IfPlaceSiteStage / IfPlaceSiteMethod and ContractPlaceRepair.
    /// </summary>
    public static class PlaceSites
    {
        /// <summary>The surface zone id of an authored place, or null.</summary>
        public static string ZoneIdOf(string placeName)
        {
            if (string.IsNullOrEmpty(placeName)) return null;
            foreach (var p in WorldMapAuthoring.Places)
                if (p.Name == placeName) return WorldMap.ToZoneID(p.X, p.Y, 0);
            return null;
        }

        public static RepairableSiteState Site(string placeName, string siteId)
        {
            string zoneId = ZoneIdOf(placeName);
            return zoneId == null || string.IsNullOrEmpty(siteId) || SettlementManager.Current == null
                ? null : SettlementManager.Current.GetSite(zoneId, siteId);
        }

        /// <summary>"Place:SiteId:Stage" — the site has reached at least that stage.</summary>
        public static bool StageAtLeast(string arg)
        {
            var parts = arg?.Split(':');
            if (parts == null || parts.Length != 3 || !TryName(parts[2], out RepairStage stage)) return false;
            var site = Site(parts[0], parts[1]);
            return site != null && site.Stage >= stage;
        }

        /// <summary>"Place:SiteId:Method" — the site's current repair was made this way.</summary>
        public static bool ResolvedBy(string arg)
        {
            var parts = arg?.Split(':');
            if (parts == null || parts.Length != 3 || !TryName(parts[2], out RepairMethodId method)) return false;
            var site = Site(parts[0], parts[1]);
            return site != null && site.ResolvedByMethod == method;
        }

        /// <summary>Only an exact enum name counts: Enum.TryParse would also accept "3",
        /// "-1" or a lowercase spelling, so content typos could silently pass a gate.</summary>
        static bool TryName<T>(string text, out T value) where T : struct, Enum
        {
            value = default;
            return !string.IsNullOrEmpty(text) && char.IsLetter(text[0])
                && Enum.TryParse(text, false, out value) && Enum.IsDefined(typeof(T), value);
        }
    }
}
