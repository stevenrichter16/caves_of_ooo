using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        private void BuildPackRecipeRows()
        {
            var recipes = new List<CraftRow>();
            if (_state == null) return;
            foreach (var group in _state.Categories)
            foreach (var item in group.Items)
            {
                if (item.Actions == null) continue;
                foreach (var action in item.Actions)
                {
                    if (action.RecipeCategory == PackRecipeCategory.None || string.IsNullOrEmpty(action.RecipeTitle)) continue;
                    recipes.Add(new CraftRow
                    {
                        Text = CapitalizeRecipeText(action.RecipeTitle),
                        Item = item.Item,
                        Count = item.StackCount,
                        IsSelectable = true,
                        RecipeAction = action
                    });
                }
            }
            recipes.Sort((a, b) =>
            {
                int category = a.RecipeAction.RecipeCategory.CompareTo(b.RecipeAction.RecipeCategory);
                if (category != 0) return category;
                int name = string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase);
                if (name != 0) return name;
                int source = string.CompareOrdinal(a.Item.ID, b.Item.ID);
                return source != 0 ? source : string.CompareOrdinal(a.RecipeAction.Display, b.RecipeAction.Display);
            });

            var previous = PackRecipeCategory.None;
            foreach (var row in recipes)
            {
                var category = row.RecipeAction.RecipeCategory;
                if (category != previous)
                    _craftRows.Add(new CraftRow { Text = PackRecipeHeading(category), IsHeader = true });
                _craftRows.Add(row);
                previous = category;
            }
        }

        private static string PackRecipeHeading(PackRecipeCategory category) => category switch
        {
            PackRecipeCategory.WeaponParts => "Weapon parts",
            PackRecipeCategory.MedicalSupplies => "Medical supplies",
            PackRecipeCategory.ToolsAndGear => "Tools & gear",
            PackRecipeCategory.Food => "Food",
            PackRecipeCategory.AlchemySupplies => "Alchemy supplies",
            PackRecipeCategory.FieldSupplies => "Field supplies",
            PackRecipeCategory.Materials => "Materials",
            PackRecipeCategory.Reclamation => "Reclamation",
            PackRecipeCategory.EquipmentWork => "Equipment work",
            _ => "Recipes"
        };

        private CraftRow SelectedPackRecipe => _craftCursorIndex >= 0 && _craftCursorIndex < _craftRows.Count
            ? _craftRows[_craftCursorIndex] : default;

        private bool PackRecipeReady => SelectedPackRecipe.RecipeAction is { IsDisabled: false };

        private string PackRecipeCommitLabel => SelectedPackRecipe.RecipeAction?.RecipeCategory == PackRecipeCategory.Food
            ? "Cook stack" : "Make";

        private string SelectedPackRecipeDetails()
            => SelectedPackRecipe.RecipeAction == null ? PackRecipeDetailsBody()
                : SelectedPackRecipe.Text + "\n\n" + PackRecipeDetailsBody();

        private string PackRecipeDetailsBody()
        {
            var row = SelectedPackRecipe;
            var action = row.RecipeAction;
            if (action == null) return "Carry materials to see their recipes.";

            string effects = string.IsNullOrEmpty(action.RecipeEffects) ? "" : action.RecipeEffects + "\n\n";
            string result = string.IsNullOrEmpty(action.RecipeResult) || action.RecipeResult == action.RecipeTitle
                ? "" : "Makes " + action.RecipeResult + "\n\n";
            string note = action.IsDisabled ? action.DisabledReason : action.Hint;
            string instruction = action.RecipeInstructions ?? action.Display;
            if (!string.IsNullOrEmpty(instruction)) instruction = CapitalizeRecipeText(instruction) + "\n\n";
            return effects + result + instruction + "Carried input: " + row.Item.GetDisplayName()
                + (string.IsNullOrEmpty(note) ? "" : "\n\n" + note);
        }

        private static string CapitalizeRecipeText(string text) => char.ToUpperInvariant(text[0]) + text.Substring(1);

        private void ExecutePackRecipe(KeyCode activationKey)
        {
            var row = SelectedPackRecipe;
            if (row.RecipeAction == null) return;
            ClearActionStatus();
            var action = ItemAction.FromInventoryAction(row.RecipeAction);
            if (ShowDisabledItemAction(action, row.Item)) return;

            // The row keeps the original pack action and any quoted tool or station.
            var consumption = SelfConsumptionTurnProof.Capture(PlayerEntity, row.Item, action.Command);
            if (!TryPerformActionViaCommand(row.Item, action.Command))
            {
                // Keep the refused row selected. A refresh could put a different input under the cursor.
                Render();
                return;
            }
            FinishItemAction(action, row.Item, consumption, true, activationKey);
        }
    }
}
