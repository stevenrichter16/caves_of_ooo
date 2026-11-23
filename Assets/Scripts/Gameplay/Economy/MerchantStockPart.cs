using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Saved ownership of a small, ordinary consignment. Loading this
    /// record never creates, destroys or restocks goods.</summary>
    public sealed class MerchantStockPart : Part
    {
        public string ProfileId;
        public int Generation;
        public int LastSpecialTick = -1;
        public int LastStapleTick = -1;
        public string SelectionRaw;
        public bool BonusApplied;
        public List<Entity> Specials = new List<Entity>();
        public List<string> SpecialKeys = new List<string>();
    }
}
