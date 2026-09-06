using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Application service for craft/disassemble actions.
    /// Keeps UI and input thin while rules stay centralized and testable.
    /// </summary>
    public static class TinkeringService
    {
        private struct ConsumedIngredient
        {
            public Entity Entity;
            public bool ConsumedFromStack;
        }

        /// <summary>Crafted entries count produced units and identify their actual carried
        /// recipients (the same stack may appear repeatedly). Local payment/output commits
        /// before success prose; factory callbacks occur before payment and are not undone.</summary>
        public static bool TryCraft(
            Entity crafter,
            EntityFactory factory,
            string recipeId,
            out List<Entity> crafted,
            out string reason)
        {
            crafted = new List<Entity>();
            reason = string.Empty;

            if (crafter == null)
            {
                reason = "Crafter is missing.";
                return RejectCraft(crafter, recipeId, reason);
            }

            if (factory == null)
            {
                reason = "Entity factory is missing.";
                return RejectCraft(crafter, recipeId, reason);
            }

            InventoryPart inventory = crafter.GetPart<InventoryPart>();
            BitLockerPart bitLocker = crafter.GetPart<BitLockerPart>();
            if (inventory == null || bitLocker == null)
            {
                reason = "Crafter cannot tinker without inventory and bit locker.";
                return RejectCraft(crafter, recipeId, reason);
            }

            if (!TinkerRecipeRegistry.TryGetRecipe(recipeId, out TinkerRecipe recipe))
            {
                reason = "Unknown recipe.";
                return RejectCraft(crafter, recipeId, reason);
            }

            if (!IsBuildRecipe(recipe))
            {
                reason = "Recipe is not a build recipe.";
                return RejectCraft(crafter, recipeId, reason);
            }

            if (!bitLocker.KnowsRecipe(recipe.ID))
            {
                reason = "Recipe is not known.";
                return RejectCraft(crafter, recipeId, reason);
            }

            string cost = BitCost.Normalize(recipe.Cost);
            if (!bitLocker.HasBits(cost))
            {
                reason = "Not enough bits.";
                return RejectCraft(crafter, recipeId, reason);
            }

            // Freeze the selected recipe and exact optional source before callbacks.
            string knownRecipe = recipe.ID;
            string blueprint = recipe.Blueprint;
            int numberMade = Math.Max(1, recipe.NumberMade);
            string ingredientBlueprint = recipe.Ingredient;
            Entity ingredient = null;
            if (!string.IsNullOrWhiteSpace(ingredientBlueprint))
            {
                ingredient = inventory.FindConsumableByBlueprint(ingredientBlueprint);
                if (ingredient == null) { reason = "Required ingredient is missing."; return RejectCraft(crafter, recipeId, reason); }
            }

            var transaction = new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(crafter, crafter, "Craft"))
                { reason = "Crafting is already in progress."; return RejectCraft(crafter, recipeId, reason); }
                var prepared = new Entity[numberMade];
                for (int i = 0; i < numberMade; i++)
                {
                    prepared[i] = factory.CreateEntity(blueprint);
                    if (prepared[i] == null)
                    { reason = "Failed to create crafted item blueprint '" + blueprint + "'."; return RejectCraft(crafter, recipeId, reason); }
                }
                if (!ReferenceEquals(inventory, crafter.GetPart<InventoryPart>())
                    || !ReferenceEquals(bitLocker, crafter.GetPart<BitLockerPart>()))
                { reason = "Crafter payment inventory changed during preparation."; return RejectCraft(crafter, recipeId, reason); }
                if (!bitLocker.KnowsRecipe(knownRecipe) || !bitLocker.HasBits(cost))
                { reason = "Recipe or required bits are no longer available."; return RejectCraft(crafter, recipeId, reason); }
                if (ingredient != null && (!inventory.CanConsumeOne(ingredient)
                    || !string.Equals(ingredient.BlueprintName, ingredientBlueprint, StringComparison.OrdinalIgnoreCase)
                    || !transaction.TryClaim(ingredient, crafter, "Craft")))
                { reason = "Required ingredient is no longer available."; return RejectCraft(crafter, recipeId, reason); }
                foreach (var item in prepared)
                {
                    var physics = item.GetPart<PhysicsPart>();
                    if (inventory.Contains(item) || physics?.InInventory != null || physics?.Equipped != null)
                    { reason = "A prepared output already belongs to an inventory."; return RejectCraft(crafter, recipeId, reason); }
                }

                var receipt = InventoryTransferSnapshot.Capture(inventory, prepared);
                transaction.Do(null, receipt.Restore);
                bool paid = false;
                transaction.Do(null, () => { if (paid) bitLocker.AddBits(cost); });
                var recipients = new List<Entity>(numberMade);
                bool applied = receipt.Apply(() =>
                {
                    if (ingredient != null && !inventory.TryConsumeOne(ingredient)) return false;
                    paid = bitLocker.UseBits(cost);
                    if (!paid) return false;
                    foreach (var item in prepared)
                    {
                        if (!inventory.AddCraftedUnitWithinCapacity(item, out var recipient)) return false;
                        recipients.Add(recipient);
                    }
                    return true;
                });
                if (!applied || !receipt.ClaimChanges(transaction, crafter, "Craft"))
                { reason = "Cannot add crafted item to inventory."; return RejectCraft(crafter, recipeId, reason); }
                transaction.Commit();
                crafted = recipients;
                Diag.Record("event", "CraftCompleted", actor: crafter,
                    payload: new { recipeId = knownRecipe, blueprint, units = recipients.Count, cost });
                MessageLog.Add(crafter.GetDisplayName() + " crafts " + crafted.Count + "x " + blueprint + ".");
                return true;
            }
            finally { transaction.Rollback(); }
        }

        private static bool RejectCraft(Entity crafter, string recipeId, string reason)
        {
            Diag.Record("event", "CraftRejected", actor: crafter, payload: new { recipeId, reason });
            return false;
        }

        public static bool TryApplyModification(
            Entity crafter,
            string recipeId,
            Entity targetItem,
            out string reason)
        {
            reason = string.Empty;
            if (crafter == null)
            {
                reason = "Crafter is missing.";
                return false;
            }

            if (targetItem == null)
            {
                reason = "No target item selected.";
                return false;
            }

            InventoryPart inventory = crafter.GetPart<InventoryPart>();
            BitLockerPart bitLocker = crafter.GetPart<BitLockerPart>();
            if (inventory == null || bitLocker == null)
            {
                reason = "Crafter cannot tinker without inventory and bit locker.";
                return false;
            }

            if (!inventory.Contains(targetItem))
            {
                reason = "You must own the target item.";
                return false;
            }

            if (!TinkerRecipeRegistry.TryGetRecipe(recipeId, out TinkerRecipe recipe))
            {
                reason = "Unknown recipe.";
                return false;
            }

            if (!IsModRecipe(recipe))
            {
                reason = "Recipe is not a modification recipe.";
                return false;
            }

            if (!bitLocker.KnowsRecipe(recipe.ID))
            {
                reason = "Recipe is not known.";
                return false;
            }

            if (!CanApplyModificationTarget(recipe, targetItem, out reason))
                return false;

            string cost = BitCost.Normalize(recipe.Cost);
            if (!bitLocker.HasBits(cost))
            {
                reason = "Not enough bits.";
                return false;
            }

            ConsumedIngredient consumedIngredient = new ConsumedIngredient();
            if (!string.IsNullOrWhiteSpace(recipe.Ingredient)
                && !TryConsumeIngredient(inventory, recipe.Ingredient, out consumedIngredient))
            {
                reason = "Required ingredient is missing.";
                return false;
            }

            if (!bitLocker.UseBits(cost))
            {
                RestoreIngredient(inventory, consumedIngredient);
                reason = "Not enough bits.";
                return false;
            }

            if (!TinkerModificationRegistry.TryCreate(recipe.Blueprint, out ITinkerModification modification))
            {
                bitLocker.AddBits(cost);
                RestoreIngredient(inventory, consumedIngredient);
                reason = "Unknown modification '" + recipe.Blueprint + "'.";
                return false;
            }

            // E.5.1 deep-audit Bug #1 plumbing: set the crafter context
            // for ITinkerModification implementations that need to know
            // who is crafting (MineralInfusionTinkerModification reads
            // this to pass as `wielder` to ItemEnhancing.Apply, so that
            // tinker-on-currently-worn-item fires OnEquipped immediately).
            // try/finally guarantees the thread-static is cleared even
            // if modification.Apply throws.
            MineralInfusionTinkerModification.CurrentCrafter = crafter;
            bool applied;
            try
            {
                applied = modification.Apply(targetItem, out reason);
            }
            finally
            {
                MineralInfusionTinkerModification.CurrentCrafter = null;
            }
            if (!applied)
            {
                bitLocker.AddBits(cost);
                RestoreIngredient(inventory, consumedIngredient);
                if (string.IsNullOrWhiteSpace(reason))
                    reason = "Failed to apply modification.";
                return false;
            }

            MessageLog.Add(
                crafter.GetDisplayName()
                + " applies "
                + modification.DisplayName
                + " to "
                + targetItem.GetDisplayName()
                + ".");
            return true;
        }

        public static bool TryDisassemble(
            Entity crafter,
            Entity item,
            out string yieldedBits,
            out string reason)
        {
            yieldedBits = string.Empty;
            reason = string.Empty;

            if (crafter == null || item == null)
            {
                reason = "Crafter or item is missing.";
                return false;
            }

            InventoryPart inventory = crafter.GetPart<InventoryPart>();
            BitLockerPart bitLocker = crafter.GetPart<BitLockerPart>();
            if (inventory == null || bitLocker == null)
            {
                reason = "Crafter cannot tinker without inventory and bit locker.";
                return false;
            }

            if (!inventory.Contains(item))
            {
                reason = "You must own the item to disassemble it.";
                return false;
            }

            if (!TryResolveDisassemblyBits(item, out string bits, out reason))
            {
                return false;
            }

            if (!TryConsumeItem(inventory, item))
            {
                reason = "Failed to consume item for disassembly.";
                return false;
            }

            bitLocker.AddBits(bits);
            yieldedBits = bits;

            MessageLog.Add(crafter.GetDisplayName() + " disassembles " + item.GetDisplayName() + " for " + bits + ".");
            return true;
        }

        public static bool CanDisassemble(Entity item, out string reason)
        {
            return TryResolveDisassemblyBits(item, out _, out reason);
        }

        public static bool CanApplyModificationTarget(TinkerRecipe recipe, Entity targetItem, out string reason)
        {
            reason = string.Empty;
            if (recipe == null)
            {
                reason = "Recipe is missing.";
                return false;
            }

            if (!IsModRecipe(recipe))
            {
                reason = "Recipe is not a modification recipe.";
                return false;
            }

            if (targetItem == null)
            {
                reason = "No target item selected.";
                return false;
            }

            if ((targetItem.GetPart<StackerPart>()?.StackCount ?? 1) <= 0)
            {
                reason = "The target item is empty.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(recipe.TargetBlueprint)
                && !string.Equals(targetItem.BlueprintName, recipe.TargetBlueprint, StringComparison.OrdinalIgnoreCase))
            {
                reason = "Target item is not compatible with this mod.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(recipe.TargetTag) && !targetItem.HasTag(recipe.TargetTag))
            {
                reason = "Target item is missing required tag '" + recipe.TargetTag + "'.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(recipe.TargetPart) && !targetItem.HasPart(recipe.TargetPart))
            {
                reason = "Target item is missing required part '" + recipe.TargetPart + "'.";
                return false;
            }

            if (!TinkerModificationRegistry.TryCreate(recipe.Blueprint, out ITinkerModification modification))
            {
                reason = "Unknown modification '" + recipe.Blueprint + "'.";
                return false;
            }

            return modification.CanApply(targetItem, out reason);
        }

        private static bool TryResolveDisassemblyBits(Entity item, out string bits, out string reason)
        {
            bits = string.Empty;
            reason = string.Empty;

            if (item == null)
            {
                reason = "Item is missing.";
                return false;
            }

            if ((item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0)
            {
                reason = "The item is empty.";
                return false;
            }

            TinkerItemPart tinkerItem = item.GetPart<TinkerItemPart>();
            if (tinkerItem != null)
            {
                if (!tinkerItem.CanDisassemble)
                {
                    reason = "Item cannot be disassembled.";
                    return false;
                }

                bits = ResolvePartialYield(tinkerItem.BuildCost, tinkerItem.NumberMade);
                if (!string.IsNullOrEmpty(bits))
                    return true;
            }

            // V1 fallback: melee weapons without explicit TinkerItem metadata can
            // still disassemble using their build recipe bit cost.
            if (item.HasPart<MeleeWeaponPart>()
                && TinkerRecipeRegistry.TryGetBuildRecipeForBlueprint(item.BlueprintName, out TinkerRecipe buildRecipe))
            {
                bits = ResolvePartialYield(buildRecipe.Cost, buildRecipe.NumberMade);
                if (!string.IsNullOrEmpty(bits))
                    return true;
            }

            reason = "Item has no disassembly yield.";
            return false;
        }

        /// <summary>
        /// Disassembly yields a strict subset of the build cost so that a
        /// craft->disassemble cycle is always lossy: per-item share of the
        /// cost (for NumberMade>1 recipes), then every other bit of that
        /// share, minimum one. A full refund made crafting reversible for
        /// free and turned any NumberMade>1 recipe into a bit printer.
        /// </summary>
        private static string ResolvePartialYield(string fullCost, int numberMade)
        {
            string normalized = BitCost.Normalize(fullCost);
            if (string.IsNullOrEmpty(normalized))
                return string.Empty;

            int perItem = numberMade > 1
                ? Math.Max(1, normalized.Length / numberMade)
                : normalized.Length;

            var builder = new StringBuilder((perItem + 1) / 2);
            for (int i = 0; i < perItem; i += 2)
                builder.Append(normalized[i]);

            return builder.ToString();
        }

        private static bool TryConsumeIngredient(
            InventoryPart inventory,
            string ingredientBlueprint,
            out ConsumedIngredient consumed)
        {
            consumed = new ConsumedIngredient();
            if (inventory == null || string.IsNullOrWhiteSpace(ingredientBlueprint))
                return false;

            Entity item = inventory.FindConsumableByBlueprint(ingredientBlueprint);
            if (item == null) return false;
            bool fromStack = (item.GetPart<StackerPart>()?.StackCount ?? 1) > 1;
            if (!TryConsumeItem(inventory, item)) return false;
            consumed.Entity = item;
            consumed.ConsumedFromStack = fromStack;
            return true;
        }

        private static void RestoreIngredient(InventoryPart inventory, ConsumedIngredient consumed)
        {
            if (inventory == null || consumed.Entity == null)
                return;

            if (consumed.ConsumedFromStack)
            {
                StackerPart stacker = consumed.Entity.GetPart<StackerPart>();
                if (stacker != null)
                {
                    stacker.StackCount += 1;
                    inventory.RefreshHandlingCarryPenalty();
                    return;
                }
            }

            if (!inventory.Contains(consumed.Entity))
                inventory.AddObject(consumed.Entity);
        }

        private static bool TryConsumeItem(InventoryPart inventory, Entity item)
        {
            if (inventory == null || !inventory.CanConsumeOne(item))
                return false;

            StackerPart stacker = item.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                stacker.StackCount -= 1;
                inventory.RefreshHandlingCarryPenalty();
                return true;
            }

            return inventory.RemoveObject(item);
        }

        private static bool IsBuildRecipe(TinkerRecipe recipe)
        {
            return recipe != null
                && string.Equals(recipe.Type, "Build", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsModRecipe(TinkerRecipe recipe)
        {
            return recipe != null
                && string.Equals(recipe.Type, "Mod", StringComparison.OrdinalIgnoreCase);
        }
    }
}
