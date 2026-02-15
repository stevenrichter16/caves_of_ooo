namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marker part for stairs-up entities.
    /// InputHandler checks for this part when the player presses '&lt;' to ascend.
    /// </summary>
    public class StairsUpPart : Part
    {
        public override string Name => "StairsUp";
    }
}
