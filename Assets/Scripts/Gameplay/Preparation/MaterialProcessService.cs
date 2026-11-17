using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Supervised finite conversion. Definitions and Part policies own
    /// content/behavior; this service owns payment and participant custody.</summary>
    internal static class MaterialProcessService
    {
        static readonly ConditionalWeakTable<Entity,InventoryTransaction> Custody=new ConditionalWeakTable<Entity,InventoryTransaction>();
        internal static bool IsProtected(Entity item)=>item!=null&&Custody.TryGetValue(item,out _);
        internal static bool OwnsConsumption(Entity item,InventoryTransaction tx)
            => item!=null && tx!=null && Custody.TryGetValue(item,out var owner) && owner==tx;
        static bool Guard(Entity item,InventoryTransaction tx)
        {
            if(Custody.TryGetValue(item,out var owner))return owner==tx;
            Custody.Add(item,tx);Action release=()=>{if(Custody.TryGetValue(item,out var current)&&current==tx)Custody.Remove(item);};
            tx.Do(null,release);tx.AfterCommit(release);return true;
        }
        internal sealed class Quote
        {
            internal readonly MaterialProcessPlan Plan;
            internal readonly MaterialProcessTemplate SourceTemplate;
            internal readonly MaterialProcessItemState SourceState;
            internal readonly MaterialProcessTemplate[] Outputs;
            internal Quote(MaterialProcessPlan plan,MaterialProcessTemplate source,MaterialProcessItemState state,MaterialProcessTemplate[] outputs)
            {Plan=plan;SourceTemplate=source;SourceState=state;Outputs=outputs;}
            internal bool Current()=>Plan.Current() && SourceState.Matches() && Outputs.All(t=>t.Current());
        }
        internal static Quote TryQuote(MaterialProcessPlan plan)
        {
            if(plan==null || !plan.Current())return null;
            var source=MaterialProcessTemplate.Capture(plan.Factory,plan.Source.BlueprintName);
            var state=source?.Snapshot(plan.Source,true);
            var outputs=plan.Definition.OutputBlueprints.Select(bp=>MaterialProcessTemplate.Capture(plan.Factory,bp,true)).ToArray();
            return state!=null && outputs.All(t=>t!=null)?new Quote(plan,source,state,outputs):null;
        }
        static bool Emit(Entity owner,string id,MaterialProcessPlan plan)
        {
            var e=GameEvent.New(id);e.SetParameter("Actor",plan.Actor);e.SetParameter("Source",plan.Source);
            e.SetParameter("Station",plan.Definition.WorkKind=="ForgeCold"?plan.Station:null);
            e.SetParameter("Tool",plan.Definition.WorkKind=="HandCutting"?plan.Station:null);e.SetParameter("WorkProvider",plan.Station);e.SetParameter("Process",plan.Definition);
            e.SetParameter("Inputs",Array.AsReadOnly(new[]{plan.Source}.Concat(plan.ExtraInputs.Select(i=>i.Owner)).ToArray()));
            return owner.FireEventAndRelease(e);
        }
        static int Count(Entity item)=>item.GetPart<StackerPart>()?.StackCount??1;
        static bool Loose(Entity item)=>item.SpatialZone==null && item.GetPart<PhysicsPart>().InInventory==null && item.GetPart<PhysicsPart>().Equipped==null;
        static void Detach(Entity item)
        {
            item?.SpatialZone?.RemoveEntity(item);var physics=item?.GetPart<PhysicsPart>();if(physics==null)return;
            var carrier=physics.InInventory;
            if(carrier!=null){carrier.GetPart<InventoryPart>()?.RemoveObject(item);carrier.GetPart<ContainerPart>()?.RemoveItem(item);}
            var wearer=physics.Equipped;
            if(wearer!=null)
            {
                var pack=wearer.GetPart<InventoryPart>();
                if(pack!=null){foreach(var key in pack.EquippedItems.Where(p=>p.Value==item).Select(p=>p.Key).ToArray())pack.EquippedItems.Remove(key);pack.Objects.Remove(item);}
                var body=wearer.GetPart<Body>();if(body!=null)foreach(var limb in body.GetParts())if(limb._Equipped==item)limb.ClearEquipped();
                physics.Equipped=null;
            }
        }
        internal static bool TryExecute(Quote quote,InventoryTransaction tx,string command)
        {
            var plan=quote?.Plan;
            bool Refuse()=>PreparationActions.Reject(plan?.Actor,plan?.Source,command,"process_unavailable");
            if(tx==null || quote==null || !quote.Current())return Refuse();
            var actor=plan.Actor;var input=plan.Source;var pack=actor.GetPart<InventoryPart>();var zone=plan.Zone;
            var inputs=new[]{input}.Concat(plan.ExtraInputs.Select(i=>i.Owner)).ToArray();
            var participants=new[]{actor,plan.Station}.Concat(inputs).ToArray();
            if(!tx.TryClaimAction(actor) || inputs.Any(i=>!PreparationActions.Begin(actor,i,zone,tx,command))
                || !plan.BeginWork(tx,command) || !Guard(plan.Station,tx) || inputs.Any(i=>!Guard(i,tx)))return Refuse();
            var sourceTemplate=quote.SourceTemplate;var sourceState=quote.SourceState;
            if(sourceState==null)return Refuse();
            int sourceCount=Count(input),units=plan.Definition.InputCount;
            foreach(var owner in participants)if(!Emit(owner,"BeforeMaterialProcess",plan))return Refuse();
            if(!quote.Current())return Refuse();
            var templates=quote.Outputs;
            if(templates.Any(t=>t==null))return Refuse();
            var products=new List<Entity>();var states=new Dictionary<Entity,MaterialProcessItemState>();
            var ids=new HashSet<string>(participants.Select(e=>e.ID),StringComparer.Ordinal);
            for(int kind=0;kind<templates.Length;kind++)for(int n=0;n<plan.Definition.OutputCounts[kind];n++)
            {
                if(!templates.All(t=>t.Current()) || !plan.Current() || !sourceState.Matches())return Refuse();
                var product=plan.Factory.CreateEntity(plan.Definition.OutputBlueprints[kind]);products.Add(product);
                tx.Do(null,()=>Detach(product));
                if(product==null || !PreparationActions.Fresh(product,plan.Definition.OutputBlueprints[kind])
                    || !ids.Add(product.ID) || pack.Objects.Concat(pack.EquippedItems.Values).Concat(zone.GetReadOnlyEntities()).Any(e=>e==product||e?.ID==product.ID)
                    || !tx.TryClaim(product,actor,command))return Refuse();
                var state=templates[kind].Snapshot(product);if(state==null)return Refuse();states.Add(product,state);
            }
            if(!quote.Current())return Refuse();
            var residents=pack.Objects.Where(o=>plan.Definition.OutputBlueprints.Contains(o.BlueprintName)).ToArray();
            foreach(var resident in residents)
            {
                var template=templates[plan.Definition.OutputBlueprints.ToList().IndexOf(resident.BlueprintName)];var state=template.Snapshot(resident);
                if(state==null || !PreparationActions.Carried(actor,resident) || !tx.TryClaim(resident,actor,command))return Refuse();
                states.Add(resident,state);
            }
            int ceiling=pack.MaxWeight<0?int.MaxValue:Math.Max(pack.MaxWeight,pack.GetCarriedWeight());
            var expected=residents.ToDictionary(e=>e,Count);var received=new HashSet<Entity>();
            var receipt=InventoryTransferSnapshot.Capture(pack,products.ToArray());bool restored=false;
            tx.Do(null,()=>
            {
                if(restored)return;restored=true;
                foreach(var owner in products.Concat(received).Concat(inputs).Distinct())Detach(owner);
                receipt.Restore();
            });
            if(!receipt.Apply(()=>
            {
                for(int n=0;n<units;n++)if(!pack.TryConsumeMaterialProcessUnit(input,tx))return false;
                foreach(var ingredient in plan.ExtraInputs)
                    for(int n=0;n<ingredient.Units;n++)if(!pack.TryConsumeMaterialProcessUnit(ingredient.Owner,tx))return false;
                foreach(var product in products)
                {
                    if(!pack.AddCraftedUnit(product,out var recipient) || recipient==null)return false;
                    received.Add(recipient);expected.TryGetValue(recipient,out int prior);expected[recipient]=prior+1;
                }
                return true;
            }) || !receipt.ClaimChanges(tx,actor,command))return Refuse();
            bool Final()
            {
                if(!plan.ContextCurrent() || !sourceState.Matches() || !templates.All(t=>t.Current()) || Count(input)!=Math.Max(1,sourceCount-units)
                    || (sourceCount>units?!PreparationActions.Carried(actor,input):!Loose(input)||pack.Objects.Contains(input))
                    || pack.GetCarriedWeight()>ceiling || states.Values.Any(s=>!s.Matches()) || plan.ExtraInputs.Any(i=>!i.After(actor)))return false;
                foreach(var pair in expected)if(!PreparationActions.Carried(actor,pair.Key) || Count(pair.Key)!=pair.Value)return false;
                foreach(var product in products)
                    if(!received.Contains(product) && (!Loose(product)||Count(product)!=0||pack.Objects.Contains(product)))return false;
                return true;
            }
            if(!Final())return Refuse();
            foreach(var owner in products.Concat(received).Concat(inputs).Distinct())if(!Guard(owner,tx))return Refuse();
            tx.BeforeCommit(Final);
            PreparationActions.Complete(tx,actor,input,command,"You complete the quoted "+plan.Definition.Mode.ToLowerInvariant()+" work. The declared inputs are spent; the products are in your pack.");
            foreach(var owner in participants)
            {var captured=owner;tx.AfterCommit(()=>Emit(captured,"MaterialProcessCompleted",plan));}
            return true;
        }
    }
}
