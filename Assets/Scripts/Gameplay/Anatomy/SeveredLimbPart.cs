namespace CavesOfOoo.Core.Anatomy
{
    /// <summary>
    /// Marker Part on severed limb item entities. Stores metadata about the
    /// original body part for display and potential reattachment.
    /// </summary>
    public class SeveredLimbPart : Part
    {
        public override string Name => "SeveredLimb";

        /// <summary>The body part type that was severed (e.g. "Arm", "Hand").</summary>
        public string PartType;

        /// <summary>The display name of the severed part (e.g. "left arm").</summary>
        public string PartDisplayName;

        /// <summary>The body part category code of the original part.</summary>
        public int Category;

        /// <summary>Whether this was a mortal part (head, body).</summary>
        public bool WasMortal;
    }
}
