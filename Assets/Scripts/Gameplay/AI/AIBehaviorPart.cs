namespace CavesOfOoo.Core
{
    /// <summary>
    /// Abstract base for parts that provide AI idle behavior.
    /// Concrete subclasses override HandleEvent to respond to "AIBored" events
    /// and push goals onto the entity's BrainPart goal stack.
    /// Mirrors Qud's AIBehaviorPart marker class.
    /// </summary>
    public abstract class AIBehaviorPart : Part
    {
    }
}
