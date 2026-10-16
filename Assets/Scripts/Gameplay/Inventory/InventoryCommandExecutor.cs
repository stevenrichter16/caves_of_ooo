using System;

namespace CavesOfOoo.Core.Inventory
{
    public sealed class InventoryCommandExecutor
    {
        public InventoryCommandResult Execute(IInventoryCommand command, InventoryContext context)
        {
            if (command == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Inventory command is null.");
            }

            if (context == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Inventory context is null.");
            }

            using (ActionResolutionGuard.Enter())
            {
                InventoryValidationResult validation = null;
                var transaction = new InventoryTransaction();
                try
                {
                    validation = command.Validate(context) ?? InventoryValidationResult.Invalid(
                        InventoryValidationErrorCode.Unknown, $"Command '{command.Name}' returned null validation.");
                    if (!validation.IsValid) return InventoryCommandResult.ValidationFailure(validation);
                    var result = command.Execute(context, transaction) ?? InventoryCommandResult.Fail(
                        InventoryCommandErrorCode.ExecutionFailed, $"Command '{command.Name}' returned null result.");
                    if (result.Success || transaction.ActionCommitted)
                    {
                        transaction.Commit();
                        if (transaction.ActionCommitted)
                            result = result.Success ? result.WithActionCommitted()
                                : InventoryCommandResult.CommittedPartial(result.ErrorMessage, "outer-command");
                        return result.WithValidation(validation);
                    }
                    transaction.Rollback();
                    return result.WithValidation(validation);
                }
                catch (Exception exception)
                {
                    if (transaction.ActionCommitted)
                    {
                        transaction.Commit();
                        return InventoryCommandResult.CommittedPartial(exception.Message, "command-exception").WithValidation(validation);
                    }
                    transaction.Rollback();
                    return InventoryCommandResult.FromException(exception).WithValidation(validation);
                }
            }
        }
    }
}
