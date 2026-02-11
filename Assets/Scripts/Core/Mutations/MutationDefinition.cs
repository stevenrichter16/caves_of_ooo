using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Metadata for a mutation class loaded from data.
    /// </summary>
    [Serializable]
    public class MutationDefinition
    {
        public string Name = "";
        public string ClassName = "";
        public string DisplayName = "";
        public string Category = "";
        public string Stat = "";
        public int Cost = 1;
        public int MaxLevel = 10;
        public bool Defect = false;
        public bool Ranked = false;
        public bool ExcludeFromPool = false;
        public string Variant = "";
        public string[] Exclusions = new string[0];
    }
}
