using System;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
namespace CavesOfOoo.Web
{
    public partial class BrowserGame
    {
        // These are the native presentation policy's explicit paid branches.
        // Remaining world InventoryAction events (readers, previews, station modes) are free.
        bool DispatchPaidWorld(Entity target,string command)
        {
            bool success;int cost;
            if(EndingSpine.IsWorldCommand(command)){if(EndingSpine.TryWorldAction(target,player,zone,command,out cost)&&cost>0)EndTurn();return true;}
            if(EndingRoutes.IsWorldCommand(command)){if(EndingRoutes.TryWorldAction(target,player,zone,command,out cost)&&cost>0)EndTurn();return true;}
            if(StillleafCustody.IsWorldCommand(command)){if(StillleafCustody.TryWorldAction(target,player,zone,command,out cost)&&cost>0)EndTurn();return true;}
            if(MorrowfastQuests.IsWorldCommand(command)){if(MorrowfastQuests.TryWorldAction(target,player,zone,command,out cost)&&cost>0)EndTurn();return true;}
            if(command==DoorPart.OpenCommand||command==DoorPart.CloseCommand)
            {if(target.GetPart<DoorPart>()?.TrySetOpen(player,zone,command==DoorPart.OpenCommand)==true)EndTurn();return true;}
            if(command==MorrowfastPropPart.ClearCommand){if(target.GetPart<MorrowfastPropPart>()?.TryRemove(player,zone)==true)EndTurn();return true;}
            if(command==MorrowfastPropPart.RoofCommand)
            {var prop=target.GetPart<MorrowfastPropPart>();if(prop!=null&&prop.TrySetRoofLifted(player,zone,!MorrowfastSceneRuntime.IsRoofLifted(zone,prop.ComponentId)))EndTurn();return true;}
            if(command==MorrowfastDoorPart.OpenCommand||command==MorrowfastDoorPart.CloseCommand)
            {if(target.GetPart<MorrowfastDoorPart>()?.TrySetOpen(player,zone,command==MorrowfastDoorPart.OpenCommand)==true)EndTurn();return true;}
            if(command==FellingScenePropPart.ClearCommand){if(target.GetPart<FellingScenePropPart>()?.TryClear(player,zone)==true)EndTurn();return true;}
            if(command==DestructiblePart.BreakCommand){DestructionSystem.StrikeStructure(target,player,zone,rng);EndTurn();return true;}
            if(command=="Take"){if(InventorySystem.Pickup(player,target,zone))EndTurn();return true;}
            bool paid = command==PlayerSeatService.SitCommand||command==PlayerSeatService.StandCommand
                || ((LocalGatheringClaimPart.IsCommand(command)||CookIntroductionPart.IsCommand(command))&&!LocalGatheringClaimPart.IsReadOnlyCommand(command))
                || (CompanionManagementActions.IsCommand(command)&&!CompanionManagementActions.IsReadOnlyCommand(command))
                || command=="Harvest"||command=="HarvestCultivatedCrop"||command==RepairablePart.RepairCommand||command==TrapJammingPart.JamCommand
                || command==BotanicalInkDeskPart.PrepareCommand||command==SoddenPreparationPart.PrepareCommand||command==KitchenBatchPart.StartCommand
                || CropWateringService.IsCommand(command)||WorldResourceActions.IsCommand(command)||PreparationActions.IsCommand(command)
                || SecondExplorationActions.IsCommand(command)||command==CurationIntakePart.CertifyCommand;
            if(paid||command.StartsWith("RegionalRequest:",StringComparison.Ordinal)||LocalGatheringClaimPart.IsCommand(command)||CookIntroductionPart.IsCommand(command)||CompanionManagementActions.IsCommand(command))
            {
                var result=InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(target,command),player,zone);
                if(result.Success&&paid)EndTurn();if(!result.Success)error=result.ErrorMessage;return true;
            }
            return false;
        }
    }
}
