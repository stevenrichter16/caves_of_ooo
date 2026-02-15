using System;

namespace CavesOfOoo.Data
{
    [Serializable]
    public class FactionFileData
    {
        public FactionEntry[] Factions;
    }

    [Serializable]
    public class FactionEntry
    {
        public string Name;
        public string DisplayName;
        public bool Visible = true;
        public int InitialPlayerReputation;
        public FactionFeeling[] Feelings;
    }

    [Serializable]
    public class FactionFeeling
    {
        public string Faction;
        public int Value;
    }
}
