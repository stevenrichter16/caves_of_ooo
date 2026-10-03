using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Re-ink one exact carried book inside the native inventory receipt.
    /// Rental ink and the existing any-inked-book casting authority are unchanged.</summary>
    public static class GrimoireInkService
    {
        public static int PreviewGain(Entity actor,GrimoireChargePart charge)
        {
            var book=charge?.ParentEntity;var pack=actor?.GetPart<InventoryPart>();
            return charge!=null&&book!=null&&book.GetPart<GrimoireChargePart>()==charge
                &&book.Parts.Count(p=>p is GrimoireChargePart)==1&&book.GetPart<GrimoirePart>()?.ParentEntity==book
                &&InkActionRules.Carried(actor,pack,book)&&(book.GetPart<StackerPart>()?.StackCount??1)==1
                &&charge.Charges>=0&&charge.MaxCharges>0&&charge.Charges<charge.MaxCharges&&charge.ChargesPerVial>0
                ?(int)Math.Min((long)charge.ChargesPerVial,(long)charge.MaxCharges-charge.Charges):0;
        }
        public static bool TryReink(Entity actor,GrimoireChargePart charge,Zone zone,InventoryTransaction transaction)
        {
            var book=charge?.ParentEntity;var pack=actor?.GetPart<InventoryPart>();
            int gain=PreviewGain(actor,charge);var at=zone?.GetEntityCell(actor);
            if(transaction==null||gain<=0||!InkActionRules.CurrentActor(actor,zone,at))
                return Reject(actor,book,"unavailable","That book cannot be re-inked right now.");
            var vial=pack.Objects.FirstOrDefault(e=>e?.BlueprintName=="InkVial"&&InkActionRules.Carried(actor,pack,e)
                &&e.GetPart<InkVialPart>()?.ParentEntity==e);
            if(vial==null)return Reject(actor,book,"missing-ink","Re-inking needs one carried ink vial.");
            if(!transaction.TryClaim(actor,actor,GrimoireChargePart.ReinkCommand)
                ||!transaction.TryClaim(book,actor,GrimoireChargePart.ReinkCommand)
                ||!transaction.TryClaim(vial,actor,GrimoireChargePart.ReinkCommand))
                return Reject(actor,book,"in-progress","That book or ink is already in use.");
            int before=charge.Charges,maximum=charge.MaxCharges,perVial=charge.ChargesPerVial;
            var receipt=InventoryTransferSnapshot.Capture(pack);transaction.Do(null,receipt.Restore);
            if(!receipt.Apply(()=>pack.TryConsumeOne(vial))||!receipt.ClaimChanges(transaction,actor,GrimoireChargePart.ReinkCommand)
                ||!InkActionRules.CurrentActor(actor,zone,at)||actor.GetPart<InventoryPart>()!=pack
                ||PreviewGain(actor,charge)!=gain||charge.Charges!=before||charge.MaxCharges!=maximum||charge.ChargesPerVial!=perVial)
                return Reject(actor,book,"state-changed","The book or ink is no longer available.");
            transaction.Do(()=>charge.Charges=before+gain,()=>charge.Charges=before);
            transaction.AfterCommit(()=>
            {
                MessageLog.Add("You re-ink "+book.GetDisplayName()+". (+"+gain+" charges, "+charge.Charges+"/"+maximum+")");
                Diag.Record("event","GrimoireReinked",actor,book,new{vialId=vial.ID,amount=gain,charges=charge.Charges,maximum});
            });
            return true;
        }
        static bool Reject(Entity actor,Entity book,string reason,string message)
        {MessageLog.Add(message);Diag.Record("event","GrimoireReinkRejected",actor,book,new{reason});return false;}
    }

    // Local physical-input rules shared by the two connected ink actions, not a recipe engine.
    internal static class InkActionRules
    {
        internal static bool Carried(Entity actor,InventoryPart pack,Entity item)
        {
            var physics=item?.GetPart<PhysicsPart>();var stack=item?.GetPart<StackerPart>();
            return actor!=null&&pack?.ParentEntity==actor&&actor.GetPart<InventoryPart>()==pack&&item!=null
                &&!string.IsNullOrEmpty(item.ID)&&item.HasTag("Item")&&!item.HasTag("Creature")&&!item.HasTag("Terrain")
                &&item.SpatialZone==null&&physics?.ParentEntity==item&&physics.Takeable&&!physics.Solid
                &&physics.InInventory==actor&&physics.Equipped==null&&pack.FindEquippedBodyPart(item)==null
                &&!pack.EquippedItems.ContainsValue(item)&&pack.Objects.Count(e=>e==item||e?.ID==item.ID)==1
                &&pack.Objects.Contains(item)&&(stack==null||stack.ParentEntity==item&&stack.StackCount>0&&stack.StackCount<=stack.MaxStack);
        }
        internal static bool CurrentActor(Entity actor,Zone zone,Cell anchor)
        {
            if(actor==null||zone==null||anchor==null||!actor.HasTag("Player")||actor.SpatialZone!=zone
                ||zone.GetEntityCell(actor)!=anchor||!anchor.Objects.Contains(actor)||actor.GetStatValue("Hitpoints",0)<=0
                ||CombatSystem.IsDeathHandled(actor)||actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()==true)return false;
            var manager=WorldLocationContext.For(zone);
            return manager==null||manager.CachedZones.TryGetValue(zone.ZoneID,out var cached)&&ReferenceEquals(cached,zone);
        }
    }
}
