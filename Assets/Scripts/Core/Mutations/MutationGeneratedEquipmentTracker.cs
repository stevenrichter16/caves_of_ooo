using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Tracks mutation-generated equipment so it can be cleaned up/rebuilt safely.
    /// </summary>
    public class MutationGeneratedEquipmentTracker
    {
        public Guid ID = Guid.NewGuid();
        public string MutationClassName = "";
        public Entity Item;
        public bool AutoEquip = true;
        public bool AutoRemoveOnMutationLoss = true;
    }
}
