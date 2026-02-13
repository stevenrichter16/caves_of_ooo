namespace CavesOfOoo.Core
{
    /// <summary>
    /// Stores the base trade value of an item.
    /// Mirrors Qud's Commerce part. Value is in drams (the universal currency).
    /// </summary>
    public class CommercePart : Part
    {
        public override string Name => "Commerce";

        /// <summary>Base value in drams.</summary>
        public int Value = 1;
    }
}
