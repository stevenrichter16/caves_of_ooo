namespace CavesOfOoo.Core
{
    /// <summary>A currently attached Part may suppress the legacy fallback
    /// snapshot for its exact command family. This grants no action permission:
    /// execution must validate and enlist its complete undo before mutation.
    /// Predicates must be pure and bounded.</summary>
    public interface IInventoryActionReceiptOwner
    {
        bool OwnsInventoryReceipt(string command);
    }
}
