namespace CavesOfOoo.Core
{
    /// <summary>
    /// Mutations that stack by rank instead of adding duplicate instances.
    /// </summary>
    public interface IRankedMutation
    {
        int GetRank();
        int AdjustRank(int amount);
    }
}
