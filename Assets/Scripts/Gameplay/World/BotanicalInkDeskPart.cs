using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A bound mundane service: two sootroot pulp, one pitchpod resin and
    /// three drams become one actual ink vial. No certification or rental grant.</summary>
    public sealed class BotanicalInkDeskPart:Part
    {
        public override string Name=>"BotanicalInkDesk";
        public const string PrepareCommand="PrepareBotanicalInk";
        public const int Fee=3;
        public Entity Worker;
        public string WorkerID="",ZoneID="",DeskID="";
        public int DeskX,DeskY;
        public bool Configured;
        /// <summary>Call after the desk and its actual assigned worker are placed.
        /// Public fields and entity references are restored by the normal save graph.</summary>
        public bool Configure(Zone zone,Entity worker)
        {
            Worker=worker;WorkerID=worker?.ID??"";ZoneID=zone?.ZoneID??"";DeskID=ParentEntity?.ID??"";
            var at=zone?.GetEntityCell(ParentEntity);DeskX=at?.X??-1;DeskY=at?.Y??-1;
            Configured=at!=null&&worker!=null;
            return Configured;
        }
        public override bool HandleEvent(GameEvent e)
        {
            var actor=e.GetParameter<Entity>("Actor");var zone=e.GetParameter<Zone>("Zone")??ParentEntity?.SpatialZone;
            if(e.ID=="GetInventoryActions")
            {
                if(Context(actor,zone))e.GetParameter<InventoryActionList>("Actions")?.AddAction("PrepareInk",
                    "prepare ink (2 sootroot pulp, 1 pitchpod resin, 3 drams)",PrepareCommand,'i',20);
                return true;
            }
            if(e.ID!="InventoryAction"||e.GetStringParameter("Command")!=PrepareCommand)return true;
            if(e.GetParameter<bool>("BotanicalInkAttempted"))return true;
            e.SetParameter("BotanicalInkAttempted",true);
            if(!Prepare(actor,zone,e.GetParameter<InventoryTransaction>("InventoryTransaction")))return true;
            e.Handled=true;return false;
        }
        bool Context(Entity actor,Zone zone)
        {
            if(actor==null||zone==null||ParentEntity==null||Worker==null)return false;
            var owner=ParentEntity;var at=zone?.GetEntityCell(owner);var physics=owner?.GetPart<PhysicsPart>();
            if(!Configured||ZoneID!=MarrowstyeCompositionPlan.ZoneID||zone?.ZoneID!=ZoneID||owner?.ID!=DeskID
                ||owner.GetPart<BotanicalInkDeskPart>()!=this||owner.Parts.Count(p=>p is BotanicalInkDeskPart)!=1
                ||owner.BlueprintName!="BotanicalInkDesk"||owner.SpatialZone!=zone||at==null||at.X!=DeskX||at.Y!=DeskY
                ||!at.Objects.Contains(owner)||physics?.ParentEntity!=owner||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
                ||owner.HasTag("Creature")||owner.GetPart<RenderPart>()?.Visible!=true
                ||owner.GetPart<DestructiblePart>() is DestructiblePart d&&(d.Gone||d.HP<=0)
                ||!InkActionRules.CurrentActor(actor,zone,zone?.GetEntityCell(actor))||SpatialQuery.Distance(zone,actor,owner)>1)return false;
            var worker=Worker;var brain=worker?.GetPart<BrainPart>();var body=worker?.GetPart<PhysicsPart>();var place=zone.GetEntityCell(worker);
            return worker!=null&&worker!=actor&&worker.ID==WorkerID&&worker.BlueprintName=="CurationJuniorIndexer"
                &&worker.SpatialZone==zone&&place!=null&&place.Objects.Contains(worker)&&worker.HasTag("Creature")
                &&body?.ParentEntity==worker&&body.InInventory==null&&body.Equipped==null&&brain?.ParentEntity==worker
                &&worker.GetStatValue("Hitpoints",0)>0&&!CombatSystem.IsDeathHandled(worker)
                &&worker.GetPart<StatusEffectsPart>()?.IsActionBlocked()!=true&&SpatialQuery.Distance(zone,worker,owner)<=1
                &&!FactionManager.IsHostile(worker,actor)&&!FactionManager.IsHostile(actor,worker)
                &&!brain.IsPersonallyHostileTo(actor)&&actor.GetPart<BrainPart>()?.IsPersonallyHostileTo(worker)!=true;
        }
        bool Prepare(Entity actor,Zone zone,InventoryTransaction transaction)
        {
            if(transaction==null||!Context(actor,zone))return Reject(actor,"unavailable","The ink desk needs its living attendant beside it.");
            var pack=actor.GetPart<InventoryPart>();var factory=SeedPart.Factory;var worker=Worker;var at=zone.GetEntityCell(actor);
            if(pack?.ParentEntity!=actor||factory==null||!factory.Blueprints.ContainsKey("InkVial"))return Reject(actor,"unavailable","The desk cannot prepare usable ink right now.");
            if(TradeSystem.GetDrams(actor)<Fee||(long)TradeSystem.GetDrams(worker)+Fee>int.MaxValue)return Reject(actor,"currency","Preparing ink costs 3 drams; the attendant must be able to receive them.");
            var payment=new List<Entity>();
            foreach(var recipe in new[]{(name:"SootrootPulp",count:2),(name:"PitchpodResin",count:1)})
            {
                int left=recipe.count;
                foreach(var item in pack.Objects.Where(e=>e?.BlueprintName==recipe.name&&InkActionRules.Carried(actor,pack,e)))
                {int use=Math.Min(left,item.GetPart<StackerPart>()?.StackCount??1);for(int i=0;i<use;i++)payment.Add(item);left-=use;if(left==0)break;}
                if(left>0)return Reject(actor,"ingredients","Preparing ink needs 2 sootroot pulp and 1 pitchpod resin.");
            }
            foreach(var item in payment.Concat(new[]{actor,ParentEntity,worker}))
                if(!transaction.TryClaim(item,actor,PrepareCommand))return Reject(actor,"in-progress","Those materials or that service are already in use.");
            var inputState=payment.Distinct().Select(item=>new{item,count=item.GetPart<StackerPart>()?.StackCount??1,
                id=item.ID,blueprint=item.BlueprintName,stack=item.GetPart<StackerPart>(),physical=item.GetPart<PhysicsPart>()}).ToArray();
            var output=factory.CreateEntity("InkVial");
            if(!Context(actor,zone)||Worker!=worker||SeedPart.Factory!=factory||actor.GetPart<InventoryPart>()!=pack
                ||!InkActionRules.CurrentActor(actor,zone,at)||inputState.Any(s=>!InkActionRules.Carried(actor,pack,s.item)
                    ||s.item.ID!=s.id||s.item.BlueprintName!=s.blueprint||s.item.GetPart<StackerPart>()!=s.stack
                    ||s.item.GetPart<PhysicsPart>()!=s.physical||(s.item.GetPart<StackerPart>()?.StackCount??1)!=s.count))
                return Reject(actor,"source-changed","The supplies or attendant changed before the ink could be prepared.");
            if(!FreshInk(output)||pack.Objects.Any(e=>e==output||e?.ID==output.ID)
                ||zone.GetReadOnlyEntities().Any(e=>e==output||e.ID==output.ID)||!transaction.TryClaim(output,actor,PrepareCommand))
                return Reject(actor,"invalid-output","The desk could not produce a usable vial.");
            if(pack.Objects.Any(e=>e?.BlueprintName=="InkVial"&&!InkActionRules.Carried(actor,pack,e)))
                return Reject(actor,"invalid-destination","Your supplies cannot receive that vial.");
            var receipt=InventoryTransferSnapshot.Capture(pack,output);transaction.Do(null,receipt.Restore);
            if(!receipt.Apply(()=>
            {
                foreach(var item in payment)if(!pack.TryConsumeOne(item))return false;
                return pack.AddCraftedUnit(output,out _)&&(pack.MaxWeight<0||pack.GetCarriedWeight()<=pack.MaxWeight);
            })||!receipt.ClaimChanges(transaction,actor,PrepareCommand)||!Context(actor,zone)||Worker!=worker
                ||actor.GetPart<InventoryPart>()!=pack||!InkActionRules.CurrentActor(actor,zone,at))
                return Reject(actor,"transfer-refused","The supplies could not be exchanged; no ink was prepared.");
            transaction.DeferCurrencyTransfer(actor,worker,Fee);
            transaction.AfterCommit(()=>
            {
                MessageLog.Add("The attendant mixes sootroot pigment with pitchpod resin and stopples one vial of ink.");
                Diag.Record("furniture","BotanicalInkPrepared",actor,ParentEntity,new{workerId=worker.ID,fee=Fee,outputId=output.ID});
            });
            return true;
        }
        static bool FreshInk(Entity output)
        {
            var physical=output?.GetPart<PhysicsPart>();var stack=output?.GetPart<StackerPart>();var ink=output?.GetPart<InkVialPart>();
            return output!=null&&output.BlueprintName=="InkVial"&&!string.IsNullOrEmpty(output.ID)&&output.HasTag("Item")
                &&!output.HasTag("Creature")&&!output.HasTag("Terrain")&&output.SpatialZone==null
                &&physical?.ParentEntity==output&&physical.Takeable&&!physical.Solid&&physical.InInventory==null&&physical.Equipped==null
                &&ink?.ParentEntity==output&&ink.InkAmount==25&&(stack==null||stack.ParentEntity==output&&stack.StackCount==1&&stack.MaxStack>=1);
        }
        bool Reject(Entity actor,string reason,string message)
        {MessageLog.Add(message);Diag.Record("furniture","BotanicalInkRejected",actor,ParentEntity,new{reason});return false;}
    }
}
