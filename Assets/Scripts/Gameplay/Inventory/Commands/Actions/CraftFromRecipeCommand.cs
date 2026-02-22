using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>
    /// Command wrapper for crafting one recipe through the inventory command pipeline.
    /// </summary>
    public sealed class CraftFromRecipeCommand : IInventoryCommand
    {
        private readonly string _recipeId;
        private readonly EntityFactory _factory;

        public string Name => "CraftFromRecipe";

        public CraftFromRecipeCommand(string recipeId, EntityFactory factory)
        {
            _recipeId = recipeId;
            _factory = factory;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Crafting requires a valid actor.");
            }

            if (context.Inventory == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.MissingInventoryPart,
                    "Actor is missing InventoryPart.");
            }

            if (_factory == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Crafting requires an EntityFactory.");
            }

            if (string.IsNullOrWhiteSpace(_recipeId))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Craft recipe ID is empty.");
            }

            if (context.Actor.GetPart<BitLockerPart>() == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Actor is missing BitLockerPart.");
            }

            if (!TinkerRecipeRegistry.TryGetRecipe(_recipeId, out _))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Unknown craft recipe.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = TinkeringService.TryCraft(
                context.Actor,
                _factory,
                _recipeId,
                out List<Entity> crafted,
                out string reason);

            if (!success)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    reason);
            }

            if (crafted == null || crafted.Count == 0)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Craft operation completed without creating any items.");
            }

            return InventoryCommandResult.Ok();
        }
    }
}
