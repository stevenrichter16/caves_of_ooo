using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core.Inventory
{
    /// <summary>Immediate disposition outcomes. A surrounding transaction may later
    /// roll back a successful placement; these records describe the observed step.</summary>
    internal static class DispositionDiagnostics
    {
        internal static void Record(InventoryContext context, Entity item, string action,
            int quantity, string destination, string reason = null)
        {
            Diag.Record("event", reason == null ? "ItemDispositionApplied" : "ItemDispositionRejected",
                actor: context.Actor, target: item,
                payload: new { action, quantity, source = context.Actor.ID, destination, reason });
        }
    }
}
