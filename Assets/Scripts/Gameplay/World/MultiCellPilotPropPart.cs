namespace CavesOfOoo.Core
{
    /// <summary>Saved identity and artwork of a native south-ridge owner.
    /// Geometry lives in SpatialFootprintPart; HP, effects and inventory
    /// remain on this same entity. Visual facing never rotates its body.</summary>
    public sealed class MultiCellPilotPropPart : Part
    {
        public string OwnerId = "";
        public string ModelId = "";
        public string Role = "scenery";

        /// <summary>Only actors and explicitly loose cargo can relocate.</summary>
        public bool IsStationary => Role != "actor" && Role != "movable";
    }
}
