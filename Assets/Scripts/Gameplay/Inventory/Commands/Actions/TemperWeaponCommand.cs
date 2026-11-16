namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>
    /// Command wrapper for quenching a weapon in a brewed coating (M3-L3).
    /// Mirrors BrewReagentsCommand's shape; the command-layer rule is FORGE
    /// GATING — the quench happens where the metal is worked, checked via
    /// <see cref="ForgePart.IsNearForge"/> in Validate. Everything else
    /// (coating form, temper cap, HP fatigue, spec grammar) is owned by the
    /// already-tested <see cref="WeaponTemperingService.TryTemper"/>, whose
    /// rejections surface as this command's failure reason.
    /// </summary>
    public sealed class TemperWeaponCommand : IInventoryCommand
    {
        private readonly Entity _station;
        private readonly Entity _weapon;
        private readonly Entity _quench;

        /// <summary>Actual recipient of the last successfully transformed unit.</summary>
        public Entity TemperedWeapon { get; private set; }

        public string Name => "TemperWeapon";

        public TemperWeaponCommand(Entity weapon, Entity quench, Entity station = null)
        {
            _station = station;
            _weapon = weapon;
            _quench = quench;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Tempering requires a valid actor.");
            }

            if (context.Inventory == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.MissingInventoryPart,
                    "Actor is missing InventoryPart.");
            }

            if (_weapon == null || _quench == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Tempering needs a weapon and a coating to quench it in.");
            }

            if (_station != null ? !ForgeWorkReceipt.Near(context.Actor, _station, context.Zone) : !ForgePart.IsNearForge(context.Actor, context.Zone))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Quenching happens at a tinker's forge.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            // Direct service-level tests may omit a zone; normal command validation still requires a station.
            if ((context.Zone != null || _station != null) && !ForgeWorkReceipt.Begin(context.Actor, context.Zone, transaction, Name, _station))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The selected workshop is no longer ready.");
            TemperedWeapon = null;
            if (!WeaponTemperingService.TryTemper(
                    context.Actor, _weapon, _quench, transaction, out Entity affected, out string reason, FieldWorkRigRules.IsRig(_station ?? ForgeWorkReceipt.Resolve(context.Actor, context.Zone))))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    reason);
            }

            TemperedWeapon = affected;
            return InventoryCommandResult.Ok();
        }
    }
}
