namespace CavesOfOoo.Core
{
    public enum POIType
    {
        Village,
        Lair,
        MerchantCamp,

        /// <summary>
        /// A zone whose entire 80×25 grid is a faithful port of the
        /// river.ascii demo: a full-width channel of water flanked on
        /// both sides by noise-driven bank vegetation. No village content.
        /// </summary>
        RiverChunk
    }

    /// <summary>
    /// Marks a world map cell as a special location that modifies zone generation.
    /// </summary>
    public class PointOfInterest
    {
        public POIType Type;
        public string Name;
        public string Faction;
        public int Tier;
        public string BossBlueprint; // For lairs only
        /// <summary>W4.6 — the authored Place's village profile,
        /// carried onto the POI so CreateVillagePipeline can switch on
        /// data instead of names. Null for non-village POIs and plain
        /// villages.</summary>
        public string Profile;

        public PointOfInterest(POIType type, string name, string faction = null, int tier = 1, string bossBlueprint = null, string profile = null)
        {
            Type = type;
            Name = name;
            Faction = faction;
            Tier = tier;
            BossBlueprint = bossBlueprint;
            Profile = profile;
        }
    }
}
