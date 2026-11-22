using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core.Inventory
{
    /// <summary>One stackable crafted unit, including a compatible merge. The
    /// immediate placement uses the existing transfer receipt; later rollback
    /// retracts only this unit and never restores a prior quantity or custody.</summary>
    internal sealed class InventoryUnitOutputReceipt
    {
        readonly InventoryPart inventory;
        readonly Entity actor, owner;
        readonly StackerPart stack;
        readonly PhysicsPart physics;
        readonly string id, blueprint;
        readonly int expected;
        InventoryUnitOutputReceipt(InventoryPart inventory, Entity actor, Entity owner, int expected)
        {
            this.inventory=inventory;this.actor=actor;this.owner=owner;this.expected=expected;
            stack=owner.GetPart<StackerPart>();physics=owner.GetPart<PhysicsPart>();id=owner.ID;blueprint=owner.BlueprintName;
        }
        internal static bool TryPlace(InventoryPart inventory, Entity actor, Entity product, InventoryTransaction tx,
            string command, out InventoryUnitOutputReceipt receipt)
        {
            receipt=null;
            var incoming=product?.GetPart<StackerPart>();
            if(actor==null || product==null || incoming==null || tx==null || inventory?.ParentEntity!=actor || incoming.ParentEntity!=product || incoming.StackCount!=1
                || !tx.TryClaim(product,actor,command))return false;
            var before=new Dictionary<Entity,int>();
            foreach(var item in inventory.Objects.ToArray())
            {
                var existing=item.GetPart<StackerPart>();
                before[item]=existing?.StackCount??1;
                if(existing?.CanStackWith(product)==true
                    && (!WorldResourceActions.Carried(actor,item,false) || !tx.TryClaim(item,actor,command)))return false;
            }
            var immediate=InventoryTransferSnapshot.Capture(inventory,product);
            InventoryUnitOutputReceipt paid=null;
            tx.Do(null,()=>{if(paid==null)immediate.Restore();else paid.Undo();});
            Entity recipient=null;
            if(!immediate.Apply(()=>inventory.AddCraftedUnitWithinCapacity(product,out recipient)) || recipient==null
                || !immediate.ClaimChanges(tx,actor,command))return false;
            int count=before.TryGetValue(recipient,out int n)?n:0;
            if(count==int.MaxValue || recipient.GetPart<StackerPart>()?.StackCount!=count+1)return false;
            paid=new InventoryUnitOutputReceipt(inventory,actor,recipient,count+1);
            receipt=paid;return paid.Current();
        }
        internal bool Current()=>actor.GetPart<InventoryPart>()==inventory && owner.ID==id && owner.BlueprintName==blueprint
            && owner.GetPart<StackerPart>()==stack && stack?.ParentEntity==owner && stack.StackCount==expected
            && owner.GetPart<PhysicsPart>()==physics && physics?.ParentEntity==owner
            && WorldResourceActions.Carried(actor,owner,false)
            && (inventory.MaxWeight<0 || inventory.GetCarriedWeight()<=inventory.MaxWeight);
        void Undo()
        {
            if(owner.ID!=id || owner.BlueprintName!=blueprint || owner.GetPart<StackerPart>()!=stack || stack?.ParentEntity!=owner)
            {Diag.Record("event","UnitOutputUndoIdentityChanged",actor,owner);return;}
            stack.StackCount=WaterQuantityUndo.Restore(stack.StackCount,1);
            if(stack.StackCount>0)return; // retain independent later stock, wherever the owner now is
            var current=owner.GetPart<PhysicsPart>();
            if(current!=physics || current?.ParentEntity!=owner)
            {Diag.Record("event","UnitOutputUndoCustodyChanged",actor,owner);return;}
            var holder=current.InInventory;
            if(owner.SpatialZone==null && current.Equipped==null && holder?.GetPart<InventoryPart>() is InventoryPart held
                && held.ParentEntity==holder && held.Objects.Contains(owner))held.RemoveObject(owner);
            else if(owner.SpatialZone is Zone ground && current.InInventory==null && current.Equipped==null
                && ground.GetEntityCell(owner)?.Objects.Contains(owner)==true)ground.RemoveEntity(owner);
            else Diag.Record("event","UnitOutputUndoCustodyChanged",actor,owner);
        }
    }
}
