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
        RiverChunk,

        /// <summary>W5.1 — a hole in the world. Claims a world cell and
        /// routes ALL of its z-levels to bespoke pipelines: the Mouth
        /// (z=0) you find, the Descent (z=1) you survive, the Floor
        /// (z=2) that is a different place per sinkhole. Unlike every
        /// other POI type, this one means something below z=0 — see
        /// OverworldZoneManager's depth routing.</summary>
        Sinkhole,

        /// <summary>The authored surface circle; append-only save ordinal5.</summary>
        FellingSite
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
