using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Saved player equipment recipes. Item IDs are deliberately not entity
    /// references: a remembered item must not keep sold or destroyed equipment alive.</summary>
    public sealed class PlayerLoadoutsPart : Part
    {
        public override string Name => "PlayerLoadouts";
        public List<SavedEquipmentLoadout> Saved = new List<SavedEquipmentLoadout>();
        public string PreparedA = "";
        public string PreparedB = "";
        public string LastAppliedID = "";
        public int InitialPreparations;
        [NonSerialized] internal bool Applying;
    }

    [Serializable]
    public sealed class SavedEquipmentLoadout
    {
        public string ID;
        public string Name;
        public List<SavedLoadoutItem> Items = new List<SavedLoadoutItem>();
        public List<SavedLoadoutSlot> Slots = new List<SavedLoadoutSlot>();
    }

    [Serializable]
    public sealed class SavedLoadoutItem
    {
        public string ItemID;
        public string ItemName;
        // First entry is the primary slot, preserving multi-handed weapon handling.
        public List<int> BodyPartIDs = new List<int>();
        public List<string> BodyPartTypes = new List<string>();
    }

    [Serializable]
    public sealed class SavedLoadoutSlot
    {
        public int BodyPartID;
        public string Type;
        public string Name;
    }
}
