namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marker part for stairs-down entities.
    /// InputHandler checks for this part when the player presses '>' to descend.
    /// </summary>
    public class StairsDownPart : Part
    {
        public override string Name => "StairsDown";
    }
}
