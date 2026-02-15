namespace CavesOfOoo.Core
{
    public enum POIType
    {
        Village,
        Lair,
        MerchantCamp
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

        public PointOfInterest(POIType type, string name, string faction = null, int tier = 1, string bossBlueprint = null)
        {
            Type = type;
            Name = name;
            Faction = faction;
            Tier = tier;
            BossBlueprint = bossBlueprint;
        }
    }
}
