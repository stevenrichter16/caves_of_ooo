namespace CavesOfOoo.Core
{
    /// <summary>
    /// Tracks where a mutation level modifier came from.
    /// Mirrors Qud's modifier source categories at a high level.
    /// </summary>
    public enum MutationSourceType
    {
        Unknown,
        External,
        StatMod,
        Equipment,
        Cooking,
        Tonic
    }
}
