using System;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    /// <summary>Only the explicitly authored material/recipe pairs are admitted.</summary>
    public sealed class PreparationRecipePart : Part
    {
        public override string Name => "PreparationRecipe";
        public string RecipeIds = "";
        public override bool HandleEvent(GameEvent e)
        {
            if(!ParentEntity.HasPart<MaterialProcessInputPart>() && !MaterialProcessActions.Handle(ParentEntity,e,MaterialProcessRegistry.ForPreparation(ParentEntity)))return false;
            var actor = e.GetParameter<Entity>("Actor"); var zone = PreparationActions.ZoneFor(e, actor);
            if (e.ID == "GetInventoryActions")
            {
                if (PreparationActions.Carried(actor, ParentEntity))
                    foreach (var recipe in PreparationRecipeService.Recipes)
                        if (Allows(recipe)) AddRecipeAction(e.GetParameter<InventoryActionList>("Actions"), recipe, actor, zone);
                return true;
            }
            string command = e.GetStringParameter("Command");
            if (e.ID != "InventoryAction" || command == null || !command.StartsWith("PrepareRecipe|", StringComparison.Ordinal)) return true;
            if (e.GetParameter<bool>("PreparationAttempted")) return true; e.SetParameter("PreparationAttempted", true);
            if (!PreparationRecipeService.TryPrepare(actor, this, command.Substring(14), zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true; return false;
        }
        internal bool Allows(PreparationRecipeService.Recipe recipe) => recipe != null && ParentEntity?.GetPart<PreparationRecipePart>() == this
            && ParentEntity.BlueprintName == recipe.Input && Array.IndexOf((RecipeIds ?? "").Split(','), recipe.Id) >= 0;

        private void AddRecipeAction(InventoryActionList actions, PreparationRecipeService.Recipe recipe, Entity actor, Zone zone)
        {
            if (actions == null) return;
            InventoryAction row;
            if ((ParentEntity.GetPart<StackerPart>()?.StackCount ?? 1) < recipe.Units)
                row = actions.AddDisabled(recipe.Id, recipe.Text, "short-on-material",
                    "This recipe needs " + recipe.Units + " units in the selected stack.", '\0', 18);
            else if (recipe.Still && !AlchemyStillPart.IsNearStill(actor, zone))
                row = actions.AddDisabled(recipe.Id, recipe.Text, "still-required", "Stand beside a working alchemy still.", '\0', 18);
            else
                row = actions.AddAction(recipe.Id, recipe.Text, "PrepareRecipe|" + recipe.Id, '\0', 18);
            string instructions = "Uses " + recipe.Units + " " + InventoryPart.GetUnitDisplayName(ParentEntity) + "."
                + (recipe.Still && !row.IsDisabled ? " Work beside an alchemy still." : "");
            row.AsRecipe(recipe.Category, MaterialProcessActions.Label(SeedPart.Factory, recipe.Output),
                effects: PackRecipeEffects.FromBlueprint(SeedPart.Factory, recipe.Output), instructions: instructions);
        }
    }
    internal static class PreparationRecipeService
    {
        internal sealed class Recipe
        {
            internal readonly string Id, Input, Output, Text; internal readonly int Units; internal readonly bool Still;
            internal readonly PackRecipeCategory Category;
            internal Recipe(string id, string input, string output, int units, bool still, string text, PackRecipeCategory category)
            { Id = id; Input = input; Output = output; Units = units; Still = still; Text = text; Category = category; }
        }
        internal static readonly Recipe[] Recipes = {
            new Recipe("shape_haft", "SalvagedTimber", "FieldHaftComponent", 1, false, "shape haft: 1 timber, STR cap 1", PackRecipeCategory.WeaponParts),
            new Recipe("braid_binding", "KnotflaxCord", "PlainCordBindingComponent", 1, false, "braid binding: 1 cord, no bonus", PackRecipeCategory.WeaponParts),
            new Recipe("shape_head", "Tepuibone", "TepuiboneHeadComponent", 1, false, "cudgel head: 1 tepuibone, 1d3/-1PV", PackRecipeCategory.WeaponParts),
            new Recipe("bandage", "KnotflaxCord", "KnotflaxBandage", 1, false, "bandage: 1 cord, stops bleeding only", PackRecipeCategory.MedicalSupplies),
            new Recipe("concentrate_mendleaf", "MendleafSprig", "ConcentratedMendleaf", 2, true, "still: 2 mendleaf -> 1 vital:2", PackRecipeCategory.AlchemySupplies),
            new Recipe("detox_grove_red", "GroveRed", "CleansedGrovePulp", 1, true, "still: detox grove-red -> vital:2", PackRecipeCategory.AlchemySupplies),
        };
        internal static bool TryPrepare(Entity actor, PreparationRecipePart part, string id, Zone zone, InventoryTransaction tx)
        {
            Recipe recipe = null; foreach (var row in Recipes) if (row.Id == id) { recipe = row; break; }
            string command = "PrepareRecipe|" + id; var input = part.ParentEntity; var factory = SeedPart.Factory; string authored = part.RecipeIds;
            if (recipe == null || !part.Allows(recipe) || recipe.Still && !AlchemyStillPart.IsNearStill(actor, zone))
                return PreparationActions.Reject(actor, input, command, "recipe_or_station");
            return PreparationActions.Transform(actor, input, zone, factory, tx, command, recipe.Units, new[] { recipe.Output },
                () => SeedPart.Factory == factory && part.RecipeIds == authored && part.Allows(recipe) && (!recipe.Still || AlchemyStillPart.IsNearStill(actor, zone)),
                (product, _) => recipe.Id == "bandage" ? product.GetPart<CureTonicPart>()?.CureEffect == "BleedingEffect"
                    : recipe.Id == "concentrate_mendleaf" || recipe.Id == "detox_grove_red" ? product.GetPart<ReagentPart>()?.PropertiesRaw == "vital:2"
                    : product.HasPart<WeaponComponentPart>(),
                "You prepare " + recipe.Units + " " + InventoryPart.GetUnitDisplayName(input) + ".");
        }
    }
}
