using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;

namespace CavesOfOoo.Web
{
    public partial class BrowserGame
    {
        void Loot(Intent a,bool fromContainer)
        {
            var ids=(a.ids??new[]{a.id}).Where(id=>!string.IsNullOrEmpty(id)).Distinct().Take(100).ToArray();
            if(ids.Length==0){error="Choose something to take.";return;}
            var owner=container;var originalZone=zone;var originalPlayer=player;
            var source=fromContainer?owner?.GetPart<ContainerPart>()?.Contents:InventorySystem.GetTakeableItemsAtFeet(player,zone);
            if(source==null||(fromContainer&&!Adjacent(owner))){error="The container is out of reach.";return;}
            var selectedItems=ids.Select(id=>source.FirstOrDefault(e=>e.ID==id)).ToArray();bool paid=false;
            try
            {
                foreach(var item in selectedItems)
                {
                    if(item==null||player!=originalPlayer||zone!=originalZone||Dead||
                       (fromContainer?(!Adjacent(owner)||owner.GetPart<ContainerPart>()?.Contents.Contains(item)!=true):!InventorySystem.GetTakeableItemsAtFeet(player,zone).Contains(item)))
                    {error="The next item is no longer in reach.";break;}
                    IInventoryCommand command=fromContainer?new TakeFromContainerCommand(owner,item):new PickupCommand(item);
                    var result=InventorySystem.ExecuteCommand(command,player,zone);
                    paid|=result.Success||result.ActionCommitted;
                    if(!result.Success){error=result.ErrorMessage;break;}
                }
            }
            finally {if(paid){panel="";EndTurn();}}
        }
        sealed class PendingThrow
        {
            public Entity Actor,Item;public Zone Zone;public ThrowIntent Intent;
            public ThrowMode Mode;public bool Focused;public string Description;
        }
        PendingThrow pendingThrow;
        string throwingMode="Ordinary";
        Entity ThrowSource(string id)
        {
            var own=Owned(id);if(own!=null)return own;
            var at=zone.GetEntityCell(player);if(at==null)return null;
            foreach(var e in zone.GetAllEntities())
            {
                var cell=zone.GetEntityCell(e);
                if(e.ID==id&&cell!=null&&Math.Abs(cell.X-at.X)+Math.Abs(cell.Y-at.Y)<=1&&ThrowTrajectory.IsKnownOwner(cell,e))return e;
            }
            return null;
        }
        object ThrowingView()
        {
            var recover=throwingMode=="Recovery";
            var items=recover?zone.GetAllEntities().Where(e=>ThrowingRules.CanRecover(player,e,zone,out _)):
                player.GetPart<InventoryPart>().Objects.Concat(player.GetPart<InventoryPart>().GetAllEquipped())
                .Concat(zone.GetAllEntities().Where(e=>ThrowSource(e.ID)==e)).Distinct()
                .Where(e=>HandlingService.CanThrow(player,e,out _)&&(throwingMode=="Ordinary"||ThrowingRules.IsPhysical(e)));
            return new {mode=throwingMode,items=items.Select(e=>new{id=e.ID,name=e.GetDisplayName(),range=HandlingService.GetThrowRange(player,e),focus=ThrowingRules.CanFocus(player,e)}).ToArray()};
        }
        void PreviewThrow(Intent a)
        {
            pendingThrow=null;var item=ThrowSource(a.id);var cell=zone.GetCell(a.x,a.y);
            if(item==null||cell?.IsVisible!=true){error="Choose a visible target and an accessible item.";return;}
            if(!Enum.TryParse<ThrowMode>(a.value??"Ordinary",out var mode)){error="Unknown throwing mode.";return;}
            var intent=a.command=="ground"?ThrowIntent.Ground(a.x,a.y):ThrowTrajectory.ResolveIntent(zone,player,a.x,a.y,knownOnly:true);
            if(!HandlingService.CanThrow(player,item,out string reason)||!ThrowTrajectory.ValidateIntent(zone,player,intent,true,out reason)
                ||!ThrowingRules.ValidateMode(player,item,zone,intent.Target,cell,mode,a.focused,out reason))
            {error=reason;return;}
            var at=zone.GetEntityCell(player);int range=HandlingService.GetThrowRange(player,item);
            if(Math.Max(Math.Abs(at.X-a.x),Math.Abs(at.Y-a.y))>range){error=$"Choose a target within {range} tiles.";return;}
            pendingThrow=new PendingThrow{Actor=player,Zone=zone,Item=item,Intent=intent,Mode=mode,Focused=a.focused,
                Description=ThrowPreview.Build(zone,player,item,intent,range,a.focused,mode==ThrowMode.Driving,mode==ThrowMode.Measured).Description};
            panel="throwPreview";
        }
        void PayThrow(Entity actor)
        {
            ActionResolutionGuard.HoldPendingAction(actor);
            try{EndTurn();}finally{ActionResolutionGuard.SettlePendingAction(actor);}
        }
        void ThrowAction(Intent a)
        {
            var shot=pendingThrow;pendingThrow=null;panel="";
            if(shot==null||shot.Actor!=player||shot.Zone!=zone||ThrowSource(shot.Item.ID)!=shot.Item||!ThrowTrajectory.ValidateIntent(zone,player,shot.Intent,true,out _))
            {error="Review this throw again; its item or target has changed.";return;}
            if(shot.Mode==ThrowMode.Measured)
            {
                if(!ThrowingRules.Prepare(player,shot.Item,zone,shot.Intent.Target,zone.GetCell(shot.Intent.X,shot.Intent.Y),out string reason)){error=reason;return;}
                var prepared=player.GetPart<ThrowingStatePart>();int serial=prepared.PreparationSerial;
                PayThrow(shot.Actor);panel="";
                if(Dead||!prepared.Prepared||prepared.PreparationSerial!=serial||prepared.PreparedSource!=shot.Item||prepared.PreparedTarget!=shot.Intent.Target)return;
                foreach(var contact in zone.GetOccupiedCells(shot.Intent.Target))
                {
                    var origin=zone.GetEntityCell(player);
                    if(!ThrowTrajectory.IsKnownOwner(contact,shot.Intent.Target)||!HandlingService.CanThrow(player,shot.Item,out _)||origin==null||Math.Max(Math.Abs(origin.X-contact.X),Math.Abs(origin.Y-contact.Y))>HandlingService.GetThrowRange(player,shot.Item))continue;
                    var next=ThrowIntent.AtEntity(shot.Intent.Target,contact.X,contact.Y);
                    shot.Mode=ThrowMode.Ordinary;shot.Intent=next;
                    shot.Description="Aim prepared. The throw costs another turn. "+ThrowPreview.Build(zone,player,shot.Item,next,HandlingService.GetThrowRange(player,shot.Item),false,false).Description;
                    pendingThrow=shot;panel="throwPreview";break;
                }
                if(pendingThrow==null)MessageLog.Add("The prepared target is no longer in reach. Choose your next action.");
                return;
            }
            var result=InventorySystem.ExecuteCommand(new ThrowItemCommand(shot.Item,shot.Intent,rng,shot.Mode,shot.Focused),player,zone);
            if(result.ActionCommitted){panel="";PayThrow(shot.Actor);}
            if(!string.IsNullOrEmpty(result.ErrorMessage))error=result.ErrorMessage;
        }
        void RecoverThrow(Intent a)
        {
            var item=zone.GetAllEntities().FirstOrDefault(e=>e.ID==a.id&&ThrowingRules.CanRecover(player,e,zone,out _));
            if(item==null){error="There is no recoverable item there.";return;}
            var actor=player;var result=ThrowingRules.Recover(actor,item,zone);
            if(result.Committed){panel="";PayThrow(actor);}
            if(!string.IsNullOrEmpty(result.Reason))error=result.Reason;
        }
    }
}
