using System;
using System.Linq;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>One transaction across ordinary equipment commands. The result asks
    /// the input owner to advance one turn only after every changed slot commits.</summary>
    public sealed class ApplyPlayerLoadoutCommand : IInventoryCommand
    {
        readonly int slot;
        public string Name => "ApplyLoadout";
        public ApplyPlayerLoadoutCommand(int slot) { this.slot = slot; }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Zone != null && context.Actor?.SpatialZone != context.Zone)
                return Invalid("The player is no longer in the selected area.");
            if (PlayerLoadoutService.GetState(context?.Actor)?.Applying == true)
                return Invalid("A loadout change is already in progress.");
            return PlayerLoadoutService.TryResolve(context?.Actor, slot, out _, out _, out string reason)
                ? InventoryValidationResult.Valid() : Invalid(reason);
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var validation = Validate(context);
            if (!validation.IsValid) return InventoryCommandResult.ValidationFailure(validation);
            var actor = context.Actor;
            if (!PlayerLoadoutService.TryResolve(actor, slot, out var saved, out var desired, out string reason)) return Fail(reason);
            var state = PlayerLoadoutService.GetState(actor);
            if (PlayerLoadoutService.MatchesCurrent(actor, saved)) return InventoryCommandResult.Ok();
            if (!transaction.TryClaimAction(actor)) return Fail("You are already resolving another action.");
            transaction.Do(() => state.Applying = true, () => state.Applying = false);
            transaction.AfterCommit(() => state.Applying = false);

            var originalBody = context.Body;
            var originalInventory = context.Inventory;
            var originalZone = actor.SpatialZone;
            var originalCell = originalZone?.GetEntityCell(actor);
            string savedID = saved.ID;
            bool Current()
            {
                return actor.GetPart<PlayerLoadoutsPart>() == state
                    && actor.GetPart<Body>() == originalBody && actor.GetPart<InventoryPart>() == originalInventory
                    && actor.SpatialZone == originalZone && originalZone?.GetEntityCell(actor) == originalCell
                    && PlayerLoadoutService.GetPrepared(actor, slot) == saved && saved.ID == savedID
                    && PlayerLoadoutService.TryResolve(actor, slot, out _, out _, out _);
            }

            // Claim every participant before any hook runs, so a nested command
            // cannot sell a later item or make a second swap partway through.
            var worn = context.Inventory.GetAllEquipped().Distinct().ToList();
            foreach (var item in worn.Concat(desired.Select(value => value.Item)).Distinct())
                if (!transaction.TryClaim(item, actor, Name)) return Fail("One of the loadout's items is already being used.");

            bool InPlace(PlayerLoadoutService.ResolvedItem value) => value.Parts.All(p => p._Equipped == value.Item)
                && value.Parts[0].FirstSlotForEquipped
                && originalBody.GetParts().Count(p => p._Equipped == value.Item) == value.Parts.Count;

            foreach (var item in worn)
            {
                var target = desired.FirstOrDefault(value => value.Item == item);
                if (target != null && InPlace(target)) continue;
                if (!Current()) return Fail("Your equipment changed while switching loadouts.");
                var result = new UnequipCommand(item, emitSuccessMessage: false).Execute(context, transaction);
                if (!result.Success) return result;
            }
            foreach (var target in desired)
            {
                if (!Current()) return Fail("Your equipment changed while switching loadouts.");
                if (InPlace(target)) continue;
                var result = EquipCommand.ExecuteInternal(context, transaction, target.Item, target.Parts[0],
                    allowDisplacements: false, emitPlanFailureMessage: false, emitSuccessMessage: false,
                    validatePreparedChoice: item => item == target.Item && Current(), exactParts: target.Parts);
                if (!result.Success) return result;
            }
            if (!Current() || !PlayerLoadoutService.MatchesCurrent(actor, saved))
                return Fail("The loadout could not be restored exactly.");
            string previous = state.LastAppliedID;
            transaction.Do(() => state.LastAppliedID = saved.ID, () => state.LastAppliedID = previous);
            transaction.BeforeCommit(() => Current() && PlayerLoadoutService.MatchesCurrent(actor, saved));
            transaction.AfterCommit(() => MessageLog.Add("You ready " + saved.Name + "."));
            // Do not SealAction: final validation and every undo must survive
            // until InventoryCommandExecutor commits the complete transaction.
            return InventoryCommandResult.Ok(actionCommitted: true);
        }

        static InventoryValidationResult Invalid(string reason) =>
            InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, reason);
        static InventoryCommandResult Fail(string reason) =>
            InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, reason);
    }
}
