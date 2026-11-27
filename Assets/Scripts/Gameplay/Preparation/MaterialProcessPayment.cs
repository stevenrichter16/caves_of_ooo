using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>One reserved unit, with an inverse limited to its own quantity and removal.</summary>
    internal static class MaterialProcessPayment
    {
        internal static bool Spend(Entity actor,Entity item,Entity holder,Zone zone,InventoryTransaction tx,
            Func<bool> payloadCurrent,Func<bool> clayPaymentCurrent=null,Func<bool> refundAllowed=null)
        {
            if(payloadCurrent==null || !payloadCurrent() || !MaterialProcessService.OwnsConsumption(item,tx))return false;
            var pack=holder.GetPart<InventoryPart>();var container=holder.GetPart<ContainerPart>();
            var list=pack?.Objects??container?.Contents;var physics=item.GetPart<PhysicsPart>();var stack=item.GetPart<StackerPart>();
            if(list==null || list.Count(e=>e==item)!=1 || physics?.ParentEntity!=item || physics.InInventory!=holder || physics.Equipped!=null || item.SpatialZone!=null)return false;
            int count=stack?.StackCount??1;
            if(count<1 || stack!=null && (stack.ParentEntity!=item || count>stack.MaxStack || item.Parts.Count(p=>p is StackerPart)!=1))return false;
            string id=item.ID;bool removed=count==1;int index=list.IndexOf(item);var cell=zone.GetEntityCell(holder)??zone.GetEntityCell(actor);
            var origin=new ConnectedClayProvenance.Snapshot(item);
            if(clayPaymentCurrent!=null)ConnectedClayProvenance.ConsumeOne(item);
            if(removed){if(pack!=null)pack.RemoveObject(item);else container.RemoveItem(item);}
            else {stack.StackCount--;ThrowingRules.OnStackChanged(item);pack?.RefreshHandlingCarryPenalty();}
            tx.Do(null,()=>
            {
                if (refundAllowed?.Invoke() == false) return;
                if(item.ID!=id || item.GetPart<PhysicsPart>()!=physics || physics.ParentEntity!=item
                    || item.GetPart<StackerPart>()!=stack || stack!=null && (stack.ParentEntity!=item || item.Parts.Count(p=>p is StackerPart)!=1))
                {Refused(actor,item,"changed_input_parts");return;}
                if(!(clayPaymentCurrent??payloadCurrent)()){Refused(actor,item,"changed_input_payload");return;}
                if((clayPaymentCurrent!=null || refundAllowed!=null) && zone.GetReadOnlyEntities().Any(other=>other!=item && other?.ID==id))
                {Refused(actor,item,"independent_world_identity");return;}
                if(!removed)
                {
                    if(stack.StackCount<0 || stack.StackCount>=stack.MaxStack || stack.StackCount==int.MaxValue)
                    {Refused(actor,item,"changed_count_bounds");return;}
                    stack.StackCount++;
                    if(clayPaymentCurrent!=null)origin.Restore(item);
                    physics.InInventory?.GetPart<InventoryPart>()?.RefreshHandlingCarryPenalty();return;
                }
                if(item.SpatialZone!=null || physics.InInventory!=null || physics.Equipped!=null)return;
                if(stack!=null && (stack.StackCount<1 || stack.StackCount>stack.MaxStack)){Refused(actor,item,"changed_count_bounds");return;}
                // A forced listener may already have placed the removed identity
                // back in the list without updating Physics. Never insert it twice.
                if(list.Any(e=>e==item || e?.ID==id)){Refused(actor,item,"input_already_listed");return;}
                bool same=pack!=null?holder.GetPart<InventoryPart>()==pack && pack.ParentEntity==holder && ReferenceEquals(pack.Objects,list)
                    :holder.GetPart<ContainerPart>()==container && container.ParentEntity==holder && ReferenceEquals(container.Contents,list);
                bool died=CombatSystem.IsDeathHandled(holder) || holder.GetStat("Hitpoints")!=null && holder.GetStatValue("Hitpoints")<=0;
                same &= holder.GetPart<DestructiblePart>()?.Gone!=true && holder.GetPart<DestructiblePart>()?.HP<=0!=true
                    && !died;
                bool hasSlot = pack != null || container.MaxItems < 0 || list.Count < container.MaxItems;
                if(same && hasSlot && holder.SpatialZone==zone)
                {
                    list.Insert(Math.Min(index,list.Count),item);physics.InInventory=holder;
                    if(clayPaymentCurrent!=null)origin.Restore(item);
                    pack?.RefreshHandlingCarryPenalty();
                }
                else if(cell!=null)
                {
                    if((clayPaymentCurrent!=null || refundAllowed!=null) && pack!=null && died
                        && (holder.HasTag("NoDropOnDeath") || holder.HasTag("Temporary")))return;
                    if(zone.AddEntity(item,cell.X,cell.Y) && clayPaymentCurrent!=null)origin.Restore(item);
                }
            });
            return true;
        }
        static void Refused(Entity actor,Entity item,string reason)
            =>CavesOfOoo.Diagnostics.Diag.Record("event","MaterialWorkRefundRefused",actor:actor,target:item,payload:new{reason});
    }
}
