using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Web
{
    public partial class BrowserGame
    {
        sealed class EquipmentReview
        {
            public string Token;public Entity Actor,Item;public Zone Zone;public Body Body;public InventoryPart Inventory;
            public BodyPart Target;public BodyPart[] Parts,Claims;public Entity[] Occupants;public InventoryDisplacement[] Displaced;public int Count;
        }
        EquipmentReview pendingEquipment;
        string equipmentItem;
        static readonly EquipPlanner BrowserEquipPlanner=new EquipPlanner();
        static object[] Displacements(InventoryDisplacement[] displaced) => displaced.Select(d=>(object)new{id=d.Item.ID,name=d.Item.GetDisplayName(),slot=d.BodyPart.GetDisplayName()}).ToArray();
        object EquipmentView()
        {
            var item=Owned(equipmentItem);var body=player.GetPart<Body>();if(item==null||body==null)return null;
            EquipmentComparisonService.TryDescribe(player,item,out var comparison,out _);
            var slots=InventoryScreenData.BuildEquipmentList(player,player.GetPart<InventoryPart>()).Select(slot=>
            {
                var part=body.GetParts().Single(p=>p.ID==slot.BodyPartID);var plan=BrowserEquipPlanner.Build(player,item,part);
                return new{id=part.ID,name=part.GetDisplayName(),primary=part.Primary||part.DefaultPrimary,equippedName=part._Equipped?.GetDisplayName(),naturalName=part._DefaultBehavior?.GetDisplayName(),
                    valid=plan.IsValid,reason=plan.FailureReason,claimed=plan.ClaimedParts.Select(p=>p.GetDisplayName()).ToArray(),displaced=Displacements(plan.Displacements.ToArray())};
            }).ToArray();
            return new{itemId=item.ID,name=item.GetDisplayName(),slots,comparison};
        }
        object EquipmentReviewView()
        {
            var p=pendingEquipment;if(p==null)return null;
            return new{token=p.Token,itemId=p.Item.ID,name=p.Item.GetDisplayName(),claimed=p.Claims.Select(part=>part.GetDisplayName()).ToArray(),displaced=Displacements(p.Displaced)};
        }
        void Equipment(Intent a)
        {
            if(a.command=="confirm"){ConfirmEquipment(a);return;}
            pendingEquipment=null;
            if(a.command=="cancel"){panel="inventory";return;}
            if(a.command!="choose"&&a.command!="review"){error="That equipment control is not available.";return;}
            var item=Owned(a.id);var body=player.GetPart<Body>();var inventory=player.GetPart<InventoryPart>();
            if(item==null||item.GetPart<EquippablePart>()==null||body==null||!inventory.Objects.Contains(item)||InventorySystem.IsEquipped(player,item))
            {error="Choose equippable gear in your pack. Unequip it first to move it.";return;}
            var parts=body.GetParts().ToArray();
            if(parts.Select(p=>p.ID).Distinct().Count()!=parts.Length){error="Your equipment slots could not be resolved.";return;}
            equipmentItem=item.ID;
            if(a.command=="choose"){panel="equipment";return;}
            var target=parts.SingleOrDefault(p=>p.ID==a.index);
            if(target==null){error="That body part is no longer attached.";return;}
            var plan=BrowserEquipPlanner.Build(player,item,target);
            if(!plan.IsValid){error=plan.FailureReason;return;}
            pendingEquipment=new EquipmentReview{Token=Guid.NewGuid().ToString("N"),Actor=player,Item=item,Zone=zone,Body=body,Inventory=inventory,Target=target,
                Parts=parts,Occupants=parts.Select(p=>p._Equipped).ToArray(),Claims=plan.ClaimedParts.ToArray(),Displaced=plan.Displacements.ToArray(),Count=item.GetPart<StackerPart>()?.StackCount??1};
            panel="equipReview";
        }
        void ConfirmEquipment(Intent a)
        {
            var p=pendingEquipment;pendingEquipment=null;panel="inventory";
            if(p==null||string.IsNullOrEmpty(a.token)||p.Token!=a.token||p.Actor!=player||p.Zone!=zone||p.Body!=player.GetPart<Body>()||p.Inventory!=player.GetPart<InventoryPart>()
                ||Owned(p.Item.ID)!=p.Item||!p.Inventory.Objects.Contains(p.Item)||InventorySystem.IsEquipped(player,p.Item)||(p.Item.GetPart<StackerPart>()?.StackCount??1)!=p.Count
                ||!p.Body.GetParts().SequenceEqual(p.Parts)||!p.Parts.Select(part=>part._Equipped).SequenceEqual(p.Occupants))
            {error="Review the equipment again; your gear or body has changed.";return;}
            var current=BrowserEquipPlanner.Build(player,p.Item,p.Target);
            if(!current.IsValid||!current.ClaimedParts.SequenceEqual(p.Claims)||current.Displacements.Count!=p.Displaced.Length||!current.Displacements.All(d=>p.Displaced.Any(old=>old.Item==d.Item&&old.BodyPart==d.BodyPart)))
            {error="Review the equipment again; the placement has changed.";return;}
            // Keep the native inventory transaction, event vetoes, stack splitting and fallback behavior.
            var result=InventorySystem.ExecuteCommand(new EquipCommand(p.Item,p.Target),player,zone);
            if(!result.Success)error=result.ErrorMessage??"That equipment could not be placed.";
        }
    }
}
