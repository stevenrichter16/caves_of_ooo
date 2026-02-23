namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>
    /// Command wrapper for applying a tinkering modification recipe to an owned target item.
    /// </summary>
    public sealed class ApplyModificationCommand : IInventoryCommand
    {
        private readonly string _recipeId;
        private readonly Entity _targetItem;

        public string Name => "ApplyModification";

        public ApplyModificationCommand(string recipeId, Entity targetItem)
        {
            _recipeId = recipeId;
            _targetItem = targetItem;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Apply modification requires a valid actor.");
            }

            if (context.Inventory == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.MissingInventoryPart,
                    "Actor is missing InventoryPart.");
            }

            if (_targetItem == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidItem,
                    "Modification target is missing.");
            }

            if (string.IsNullOrWhiteSpace(_recipeId))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Modification recipe ID is empty.");
            }

            if (context.Actor.GetPart<BitLockerPart>() == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Actor is missing BitLockerPart.");
            }

            if (!context.Inventory.Contains(_targetItem))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NotOwned,
                    "Actor does not own the target item.");
            }

            if (!TinkerRecipeRegistry.TryGetRecipe(_recipeId, out TinkerRecipe recipe))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Unknown modification recipe.");
            }

            if (!TinkeringService.CanApplyModificationTarget(recipe, _targetItem, out string reason))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    reason);
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = TinkeringService.TryApplyModification(
                context.Actor,
                _recipeId,
                _targetItem,
                out string reason);

            if (!success)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    reason);
            }

            return InventoryCommandResult.Ok();
        }
    }
}
