namespace CavesOfOoo.Core
{
    /// <summary>
    /// Minimal runtime contract for a tinkering item modification.
    /// Mirrors Qud's "mod as behavior object" pattern in a lightweight V1 form.
    /// </summary>
    public interface ITinkerModification
    {
        string Id { get; }

        string DisplayName { get; }

        bool CanApply(Entity item, out string reason);

        bool Apply(Entity item, out string reason);
    }
}
