namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part representing armor/natural defense.
    /// Faithful to Qud's Armor part.
    /// AV (Armor Value) blocks penetration. DV (Dodge Value) avoids hits.
    /// </summary>
    public class ArmorPart : Part
    {
        public override string Name => "Armor";

        /// <summary>
        /// Armor Value — compared against penetration rolls.
        /// </summary>
        public int AV = 0;

        /// <summary>
        /// Dodge Value — added to dodge defense.
        /// </summary>
        public int DV = 0;

        /// <summary>
        /// Speed penalty from wearing this armor.
        /// </summary>
        public int SpeedPenalty = 0;
    }
}