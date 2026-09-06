using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core.Inventory
{
    /// <summary>Immediate acquisition step outcomes, before any enclosing transaction
    /// commits. A later outer failure can undo ownership/payment, not arbitrary Taken effects.</summary>
    internal static class AcquisitionDiagnostics
    {
        internal static InventoryCommandResult Refuse(InventoryContext context, Entity item,
            string action, string source, string reason, string message)
        {
            Record(context, item, action, source, item?.GetPart<StackerPart>()?.StackCount ?? 1, reason);
            return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, message);
        }
        internal static void Record(InventoryContext context, Entity item, string action,
            string source, int quantity, string reason = null)
        {
            Diag.Record("event", reason == null ? "ItemAcquisitionApplied" : "ItemAcquisitionRejected",
                actor: context.Actor, target: item,
                payload: new { action, source, quantity, reason });
        }
    }
}
