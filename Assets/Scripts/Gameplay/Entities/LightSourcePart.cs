namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an entity as a light source that illuminates nearby cells.
    /// The lighting system uses radius and color to tint visible cells.
    /// </summary>
    public class LightSourcePart : Part
    {
        public override string Name => "LightSource";

        /// <summary>
        /// How far the light reaches in tiles.
        /// </summary>
        public int Radius = 6;

        /// <summary>
        /// Light color code (Qud-style, e.g. "&Y" for warm white, "&R" for red).
        /// </summary>
        public string LightColor = "&Y";

        /// <summary>
        /// Intensity multiplier (0.0 to 1.0). Controls brightness at the source.
        /// </summary>
        public float Intensity = 1.0f;

        public override bool HandleEvent(GameEvent e)
        {
            return true;
        }
    }
}
