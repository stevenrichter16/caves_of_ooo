namespace CavesOfOoo.Core
{
    /// <summary>
    /// Consumer of narrative tick events. Implement this to react to changes
    /// in the global narrative state at the end of each turn cycle.
    ///
    /// Register via NarrativeStatePart.RegisterReactor / UnregisterReactor.
    /// Dispatch is polled (fired from TickEnd on the world entity).
    /// </summary>
    public interface INarrativeReactor
    {
        /// <summary>
        /// Called once per TickEnd on the world entity.
        /// Implementations may read or write facts on <paramref name="state"/>.
        /// </summary>
        void OnTickEnd(NarrativeStatePart state);
    }
}
