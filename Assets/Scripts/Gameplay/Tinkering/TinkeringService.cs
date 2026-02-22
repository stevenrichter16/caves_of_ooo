using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

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
                return false;
            }

            if (factory == null)
            {
                reason = "Entity factory is missing.";
                return false;
            }

            InventoryPart inventory = crafter.GetPart<InventoryPart>();
            BitLockerPart bitLocker = crafter.GetPart<BitLockerPart>();
            if (inventory == null || bitLocker == null)
            {
                reason = "Crafter cannot tinker without inventory and bit locker.";
                return false;
            }

            if (!TinkerRecipeRegistry.TryGetRecipe(recipeId, out TinkerRecipe recipe))
            {
                reason = "Unknown recipe.";
                return false;
            }

            if (!bitLocker.KnowsRecipe(recipe.ID))
            {
                reason = "Recipe is not known.";
                return false;
            }

            string cost = BitCost.Normalize(recipe.Cost);
            if (!bitLocker.HasBits(cost))
            {
                reason = "Not enough bits.";
                return false;
            }

            ConsumedIngredient consumedIngredient = new ConsumedIngredient();
            if (!string.IsNullOrWhiteSpace(recipe.Ingredient))
            {
                if (!TryConsumeIngredient(inventory, recipe.Ingredient, out consumedIngredient))
                {
                    reason = "Required ingredient is missing.";
                    return false;
                }
            }

            if (!bitLocker.UseBits(cost))
            {
                RestoreIngredient(inventory, consumedIngredient);
                reason = "Not enough bits.";
                return false;
            }

            int numberMade = Math.Max(1, recipe.NumberMade);
            for (int i = 0; i < numberMade; i++)
            {
                Entity created = factory.CreateEntity(recipe.Blueprint);
                if (created == null)
                {
                    bitLocker.AddBits(cost);
                    RestoreIngredient(inventory, consumedIngredient);
                    reason = "Failed to create crafted item blueprint '" + recipe.Blueprint + "'.";
                    return false;
                }

                if (!inventory.AddObject(created))
                {
                    bitLocker.AddBits(cost);
                    RestoreIngredient(inventory, consumedIngredient);
                    reason = "Cannot add crafted item to inventory.";
                    return false;
                }

                crafted.Add(created);
            }

            MessageLog.Add(crafter.GetDisplayName() + " crafts " + crafted.Count + "x " + recipe.Blueprint + ".");
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

        private static bool TryResolveDisassemblyBits(Entity item, out string bits, out string reason)
        {
            bits = string.Empty;
            reason = string.Empty;

            if (item == null)
            {
                reason = "Item is missing.";
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

                bits = BitCost.Normalize(tinkerItem.BuildCost);
                if (!string.IsNullOrEmpty(bits))
                    return true;
            }

            // V1 fallback: melee weapons without explicit TinkerItem metadata can
            // still disassemble using their build recipe bit cost.
            if (item.HasPart<MeleeWeaponPart>()
                && TinkerRecipeRegistry.TryGetBuildRecipeForBlueprint(item.BlueprintName, out TinkerRecipe buildRecipe))
            {
                bits = BitCost.Normalize(buildRecipe.Cost);
                if (!string.IsNullOrEmpty(bits))
                    return true;
            }

            reason = "Item has no disassembly yield.";
            return false;
        }

        private static bool TryConsumeIngredient(
            InventoryPart inventory,
            string ingredientBlueprint,
            out ConsumedIngredient consumed)
        {
            consumed = new ConsumedIngredient();
            if (inventory == null || string.IsNullOrWhiteSpace(ingredientBlueprint))
                return false;

            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                Entity item = inventory.Objects[i];
                if (!string.Equals(item.BlueprintName, ingredientBlueprint, StringComparison.OrdinalIgnoreCase))
                    continue;

                StackerPart stacker = item.GetPart<StackerPart>();
                if (stacker != null && stacker.StackCount > 1)
                {
                    stacker.StackCount -= 1;
                    consumed.Entity = item;
                    consumed.ConsumedFromStack = true;
                    return true;
                }

                if (inventory.RemoveObject(item))
                {
                    consumed.Entity = item;
                    consumed.ConsumedFromStack = false;
                    return true;
                }
            }

            return false;
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
                    return;
                }
            }

            if (!inventory.Contains(consumed.Entity))
                inventory.AddObject(consumed.Entity);
        }

        private static bool TryConsumeItem(InventoryPart inventory, Entity item)
        {
            if (inventory == null || item == null)
                return false;

            StackerPart stacker = item.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                stacker.StackCount -= 1;
                return true;
            }

            return inventory.RemoveObject(item);
        }
    }
}
