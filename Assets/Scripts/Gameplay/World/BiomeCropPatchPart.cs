namespace CavesOfOoo.Core
{
    /// <summary>Saved nonphysical completion receipt. It survives harvesting or
    /// barren-ground removal of the growing bed, preventing finite seed refills.</summary>
    public sealed class BiomeCropPatchPart : Part
    {
        public override string Name => "BiomeCropPatch";
        public string SiteKey = "";
        public string CropBlueprint = "";
    }
}
