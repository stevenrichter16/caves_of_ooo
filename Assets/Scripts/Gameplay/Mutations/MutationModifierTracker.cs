using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Represents a single modifier applied to one mutation's effective level.
    /// </summary>
    [Serializable]
    public class MutationModifierTracker
    {
        public Guid ID = Guid.NewGuid();
        public int Bonus = 0;
        public string MutationClassName = "";
        public MutationSourceType SourceType = MutationSourceType.Unknown;
        public string SourceName = "";
    }
}
