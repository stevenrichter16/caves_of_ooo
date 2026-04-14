namespace CavesOfOoo.Core
{
    /// <summary>
    /// Shared handling metadata for carried and throwable items.
    /// GripType describes intended hand usage; other fields gate carry/throw behavior.
    /// </summary>
    public sealed class HandlingPart : Part
    {
        public override string Name => "Handling";

        public GripType GripType = GripType.OneHand;
        public bool Carryable = true;
        public bool Throwable = true;
        public int Weight = 0;
        public string BulkClass = "Light";
        public int MinLiftStrength = 0;
        public int MinThrowStrength = 0;
        public int CarryMovePenalty = 0;
    }
}
