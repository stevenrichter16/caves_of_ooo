using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>
    /// Command wrapper for assembling Blade + Haft + Binding into a weapon
    /// through the inventory command pipeline (M3-L3). Mirrors
    /// BrewReagentsCommand: the rule that lives at the command layer (needs
    /// zone context the service deliberately doesn't take) is FORGE GATING —
    /// forging happens at a tinker's forge, checked via
    /// <see cref="ForgePart.IsNearForge"/> in Validate. Batch semantics ride
    /// <see cref="WeaponForgingService.TryForgeBatch"/>: a partial batch
    /// (asked 3, components ran out at 1) is a smaller success.
    /// </summary>
    public sealed class ForgeWeaponCommand : IInventoryCommand
    {
        private readonly Entity _blade;
        private readonly Entity _haft;
        private readonly Entity _binding;
        private readonly EntityFactory _factory;
        private readonly int _requestedCount;

        public string Name => "ForgeWeapon";

        public ForgeWeaponCommand(Entity blade, Entity haft, Entity binding,
            EntityFactory factory, int count = 1)
        {
            _blade = blade;
            _haft = haft;
            _binding = binding;
            _factory = factory;
            _requestedCount = count;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Forging requires a valid actor.");
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
                    "Forging requires an EntityFactory.");
            }

            if (_blade == null || _haft == null || _binding == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Forging needs a blade, a haft, and a binding.");
            }

            if (_requestedCount <= 0)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Requested forge count must be positive.");
            }

            if (!ForgePart.IsNearForge(context.Actor, context.Zone))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Weapons are forged at a tinker's forge.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool any = WeaponForgingService.TryForgeBatch(
                context.Actor,
                _factory,
                _blade,
                _haft,
                _binding,
                _requestedCount,
                out List<Entity> _,
                out int madeCount,
                out string reason);

            if (!any)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    reason);
            }

            if (madeCount > 1)
                MessageLog.Add(context.Actor.GetDisplayName() + " finishes a batch of " + madeCount + ".");

            if (madeCount < _requestedCount)
            {
                MessageLog.Add(
                    "(Requested " + _requestedCount + ", made " + madeCount
                    + " — ran out of components.)");
            }

            return InventoryCommandResult.Ok();
        }
    }
}
