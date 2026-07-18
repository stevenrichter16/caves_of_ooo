using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>
    /// Command wrapper for brewing a reagent selection through the inventory
    /// command pipeline. Mirrors CraftFromRecipeCommand's shape and adds the
    /// two rules that belong at the command layer (they need zone context the
    /// service deliberately doesn't take):
    ///
    ///  1. Still gating (§6.2): the brew is resolved (purely) at Validate
    ///     time; any outcome except a pure-Food brew requires standing on or
    ///     adjacent to an alchemy still. Food brews may be made anywhere.
    ///  2. Mishap self-damage (§6.3): a Mishap outcome singes the crafter for
    ///     a SMALL, telegraphed, NON-LETHAL amount — clamped so it can never
    ///     reduce Hitpoints below 1 (RPG, not roguelike: experimenting must
    ///     never kill outright).
    /// </summary>
    public sealed class BrewReagentsCommand : IInventoryCommand
    {
        /// <summary>Cap on mishap self-damage; the non-lethal clamp applies below this.</summary>
        public const int MishapDamageMax = 2;

        private readonly IReadOnlyList<Entity> _reagents;
        private readonly EntityFactory _factory;

        public string Name => "BrewReagents";

        public BrewReagentsCommand(IReadOnlyList<Entity> reagents, EntityFactory factory)
        {
            _reagents = reagents;
            _factory = factory;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Brewing requires a valid actor.");
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
                    "Brewing requires an EntityFactory.");
            }

            if (_reagents == null || _reagents.Count == 0)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "No reagents selected.");
            }

            // Resolve the mix purely (no side effects) to learn its FORM —
            // the still requirement depends on what would be brewed.
            var properties = new List<IReadOnlyList<BrewPropertyAmount>>(_reagents.Count);
            for (int i = 0; i < _reagents.Count; i++)
            {
                ReagentPart reagent = _reagents[i]?.GetPart<ReagentPart>();
                if (reagent == null)
                    continue; // the service rejects precisely; don't duplicate here
                properties.Add(reagent.GetProperties());
            }

            if (properties.Count == _reagents.Count)
            {
                BrewResult preview = BrewResolver.Resolve(properties);
                bool foodOnly = preview.Kind == BrewOutcomeKind.Brew
                    && string.Equals(preview.Form, "Food", StringComparison.OrdinalIgnoreCase);

                if (!foodOnly && !AlchemyStillPart.IsNearStill(context.Actor, context.Zone))
                {
                    return InventoryValidationResult.Invalid(
                        InventoryValidationErrorCode.BlockedByRule,
                        "This mix needs an alchemy still. Only simple foods can be brewed in the field.");
                }
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool ok = BrewingService.TryBrew(
                context.Actor,
                _factory,
                _reagents,
                out _,
                out BrewResult result,
                out string reason);

            if (!ok)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    reason);
            }

            if (result.Kind == BrewOutcomeKind.Mishap)
                ApplyMishapDamage(context.Actor, context.Zone);

            return InventoryCommandResult.Ok();
        }

        /// <summary>
        /// Small telegraphed burn, clamped non-lethal: never reduces
        /// Hitpoints below 1, so damage at 1 HP is zero (the flask still
        /// cracks; the message still lands).
        /// </summary>
        private static void ApplyMishapDamage(Entity actor, Zone zone)
        {
            var hp = actor.GetStat("Hitpoints");
            if (hp == null)
                return;

            int damage = Math.Min(MishapDamageMax, Math.Max(0, hp.Value - 1));
            if (damage <= 0)
                return;

            MessageLog.Add("The mishap singes " + actor.GetDisplayName() + " for " + damage + ".");
            CombatSystem.ApplyDamage(actor, damage, actor, zone);
        }
    }
}
