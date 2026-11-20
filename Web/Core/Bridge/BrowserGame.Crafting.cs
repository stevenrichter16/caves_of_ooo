using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
namespace CavesOfOoo.Web
{
    public partial class BrowserGame
    {
        Entity Component(CraftingMarkPart.MarkedSelection selection,string slot)=>selection.Components.FirstOrDefault(e=>e.GetPart<WeaponComponentPart>()?.Slot==slot);
        object CraftingView()
        {
            var marked=CraftingMarkPart.CollectMarked(player);var pack=player.GetPart<InventoryPart>();var bits=player.GetPart<BitLockerPart>();
            var blade=Component(marked,"Blade");var haft=Component(marked,"Haft");var binding=Component(marked,"Binding");
            return new {items=pack.Objects.Where(CraftingMarkPart.IsMarkable).Select(e=>new{id=e.ID,name=e.GetDisplayName(),marked=CraftingMarkPart.IsMarked(e),group=CraftingMarkPart.ExclusiveGroupOf(e)??"Reagent"}).ToArray(),
                forge=WeaponForgingService.PreviewForge(blade?.GetPart<WeaponComponentPart>(),haft?.GetPart<WeaponComponentPart>(),binding?.GetPart<WeaponComponentPart>()),
                brew=BrewingService.PreviewBrew(marked.Reagents),canTemper=marked.Weapons.Count==1&&marked.Coatings.Count==1,
                recipes=TinkerRecipeRegistry.GetAllRecipes().Where(r=>r.Type=="Build"&&bits?.KnowsRecipe(r.ID)==true).Select(r=>new{id=r.ID,name=r.DisplayName,description=r.Description,cost=r.Cost,ingredient=r.Ingredient,affordable=bits.HasBits(r.Cost)}).ToArray()};
        }
        void Craft(Intent a)
        {
            panel="crafting";var marked=CraftingMarkPart.CollectMarked(player);IInventoryCommand command;
            switch(a.command)
            {
                case "mark":var item=Owned(a.id);if(item==null){error="That item is not in your pack.";return;}command=new ToggleCraftMarkCommand(item);break;
                case "forge":command=new ForgeWeaponCommand(Component(marked,"Blade"),Component(marked,"Haft"),Component(marked,"Binding"),factory);break;
                case "brew":command=new BrewReagentsCommand(marked.Reagents,factory);break;
                case "temper":
                    if(marked.Weapons.Count!=1||marked.Coatings.Count!=1){error="Select one weapon and one coating to quench.";return;}
                    command=new TemperWeaponCommand(marked.Weapons[0],marked.Coatings[0]);break;
                case "recipe":command=new CraftFromRecipeCommand(a.id,factory);break;
                default:error="Choose an available crafting action.";return;
            }
            var result=InventorySystem.ExecuteCommand(command,player,zone);if(!result.Success)error=result.ErrorMessage;
            // The native crafting menu is free; its commands enforce ownership, ingredients and stations.
        }
    }
}
