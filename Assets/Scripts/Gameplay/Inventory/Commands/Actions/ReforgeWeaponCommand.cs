using CavesOfOoo.Data;

namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>
    /// Command wrapper for swapping one component of a forged weapon (M3-L3)
    /// — the RPG-identity hook where a favorite weapon evolves instead of
    /// being replaced. Mirrors BrewReagentsCommand's shape; the command-layer
    /// rule is FORGE GATING via <see cref="ForgePart.IsNearForge"/>. The
    /// swap itself (slot matching, stat recompute, temper melt, displaced
    /// component return) is owned by the already-tested
    /// <see cref="WeaponForgingService.TryReforge"/>.
    /// </summary>
    public sealed class ReforgeWeaponCommand : IInventoryCommand
    {
        private readonly Entity _station;
        private readonly Entity _weapon;
        private readonly Entity _newComponent;
        private readonly EntityFactory _factory;

        /// <summary>Actual recipient of the last successfully transformed unit.</summary>
        public Entity ReforgedWeapon { get; private set; }

        public string Name => "ReforgeWeapon";

        public ReforgeWeaponCommand(Entity weapon, Entity newComponent, EntityFactory factory, Entity station = null)
        {
            _station = station;
            _weapon = weapon;
            _newComponent = newComponent;
            _factory = factory;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Re-forging requires a valid actor.");
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
                    "Re-forging requires an EntityFactory.");
            }

            if (_weapon == null || _newComponent == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Re-forging needs a forged weapon and one replacement component.");
            }

            if (_station != null ? !ForgeWorkReceipt.Near(context.Actor, _station, context.Zone) : !ForgePart.IsNearForge(context.Actor, context.Zone))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Weapons are re-forged at a tinker's forge.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            // Direct service-level tests may omit a zone; normal command validation still requires a station.
            if ((context.Zone != null || _station != null) && !ForgeWorkReceipt.Begin(context.Actor, context.Zone, transaction, Name, _station))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The selected workshop is no longer ready.");
            ReforgedWeapon = null;
            if (!WeaponForgingService.TryReforge(
                    context.Actor, _factory, _weapon, _newComponent, transaction,
                    out Entity affected, out Entity _, out string reason, FieldWorkRigRules.IsRig(_station ?? ForgeWorkReceipt.Resolve(context.Actor, context.Zone))))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    reason);
            }

            ReforgedWeapon = affected;
            return InventoryCommandResult.Ok();
        }
    }
}
