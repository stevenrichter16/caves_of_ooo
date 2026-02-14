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

            var validation = command.Validate(context);
            if (validation == null)
            {
                validation = InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.Unknown,
                    $"Command '{command.Name}' returned null validation.");
            }

            if (!validation.IsValid)
                return InventoryCommandResult.ValidationFailure(validation);

            var transaction = new InventoryTransaction();

            try
            {
                var result = command.Execute(context, transaction)
                    ?? InventoryCommandResult.Fail(
                        InventoryCommandErrorCode.ExecutionFailed,
                        $"Command '{command.Name}' returned null result.");

                if (result.Success)
                {
                    transaction.Commit();
                    return result.WithValidation(validation);
                }

                transaction.Rollback();
                return result.WithValidation(validation);
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                return InventoryCommandResult.FromException(exception).WithValidation(validation);
            }
        }
    }
}
