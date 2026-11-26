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
        internal static bool ReserveInput(Entity item,InventoryTransaction tx)=>item!=null && tx!=null && Guard(item,tx);
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
            internal readonly MaterialProcessDefinition Result;
            internal readonly bool RetainsChaff;
            internal readonly Cell GritGround;
            internal string ActionId => GritGround == null ? Plan.Definition.Id : SiftableStockPart.LeaveGritId;
            internal Quote(MaterialProcessPlan plan,MaterialProcessTemplate source,MaterialProcessItemState state,MaterialProcessTemplate[] outputs,
                MaterialProcessTemplate retainedChaff, Cell gritGround)
            {
                Plan=plan;SourceTemplate=source;SourceState=state;
                RetainsChaff=retainedChaff!=null;
                GritGround=gritGround;
                Result=RetainsChaff?plan.Definition.RetainOne(SeedFramePart.Chaff,"retain dry chaff for fire fuel"):plan.Definition;
                Outputs=RetainsChaff?outputs.Append(retainedChaff).ToArray():outputs;
            }
            internal bool GroundCurrent()=>GritGround==null || SiftableStockPart.GritDestination(Plan)==GritGround;
            internal bool OutputsCurrent()=>Outputs.All(t=>t.Current()) && RetainsChaff==(SeedFramePart.RetainedChaff(Plan)!=null) && GroundCurrent();
            internal bool Current()=>Plan.Current() && Plan.RoomForStoredScreening() && SourceState.Matches() && OutputsCurrent();
        }
        internal static Quote TryQuote(MaterialProcessPlan plan, Cell gritGround = null)
            => TryQuote(plan, out _, gritGround);

        internal static Quote TryQuote(MaterialProcessPlan plan, out bool reservedForCrafting, Cell gritGround = null)
        {
            reservedForCrafting = false;
            if(plan==null || !plan.Current() || !plan.RoomForStoredScreening()
                || gritGround!=null && SiftableStockPart.GritDestination(plan)!=gritGround)return null;
            var source=MaterialProcessTemplate.Capture(plan.Factory,plan.Source.BlueprintName);
            var state=source?.Snapshot(plan.Source,true);
            var outputs=plan.Definition.OutputBlueprints.Select(bp=>MaterialProcessTemplate.Capture(plan.Factory,bp,true)).ToArray();
            if (outputs.Any(template => template == null)) return null;
            if (state == null)
            {
                reservedForCrafting = source?.WouldMatchWithoutCraftingMark(plan.Source) == true;
                return null;
            }
            return new Quote(plan, source, state, outputs, SeedFramePart.RetainedChaff(plan), gritGround);
        }
        static bool Emit(Entity owner,string id,Quote quote)
        {
            var plan=quote.Plan;
            var e=GameEvent.New(id);e.SetParameter("Actor",plan.Actor);e.SetParameter("Source",plan.Source);
            e.SetParameter("Station",plan.StationKind!=null?plan.Station:null);
            e.SetParameter("Tool",plan.WorkTool);e.SetParameter("WorkProvider",plan.Station);e.SetParameter("Process",quote.Result);
            e.SetParameter("Inputs",Array.AsReadOnly(new[]{plan.Source}.Concat(plan.ExtraInputs.SelectMany(i=>i.Takes.Select(t=>t.Owner))).ToArray()));
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
            var inputs=new[]{input}.Concat(plan.ExtraInputs.SelectMany(i=>i.Takes.Select(t=>t.Owner))).ToArray();
            var participants=new[]{actor,plan.Station}.Concat(inputs).Concat(plan.Stock==null?Array.Empty<Entity>():new[]{plan.Stock.Box}).ToArray();
            var separateTool = plan.WorkTool != plan.Station ? plan.WorkTool : null;
            if (separateTool != null) participants = participants.Append(separateTool).ToArray();
            if(!tx.TryClaimAction(actor) || inputs.Any(i=>!plan.BeginInput(i,tx,command))
                || !plan.BeginWork(tx,command) || plan.Stock!=null && !Guard(plan.Stock.Box,tx) || !Guard(plan.Station,tx)
                || separateTool != null && !Guard(separateTool, tx) || inputs.Any(i=>!Guard(i,tx)))return Refuse();
            var sourceTemplate=quote.SourceTemplate;var sourceState=quote.SourceState;
            if(sourceState==null)return Refuse();
            int sourceCount=Count(input),units=plan.Definition.InputCount;
            foreach(var owner in participants)if(!Emit(owner,"BeforeMaterialProcess",quote))return Refuse();
            if(!quote.Current())return Refuse();
            var templates=quote.Outputs;var result=quote.Result;
            if(templates.Any(t=>t==null))return Refuse();
            var products=new List<Entity>();var states=new Dictionary<Entity,MaterialProcessItemState>();
            Entity groundedGrit = null;
            string groundedGritId = null;
            bool placedGrit = false;
            var ids=new HashSet<string>(participants.Select(e=>e.ID),StringComparer.Ordinal);
            for(int kind=0;kind<templates.Length;kind++)for(int n=0;n<result.OutputCounts[kind];n++)
            {
                if(!quote.Current())return Refuse();
                var product=plan.Factory.CreateEntity(result.OutputBlueprints[kind]);products.Add(product);
                if (quote.GritGround != null && kind == 1)
                {
                    groundedGrit = product;
                    groundedGritId = product?.ID;
                    tx.Do(null, () => { if (!placedGrit) Detach(product); });
                }
                else tx.Do(null,()=>Detach(product));
                if(product==null || !PreparationActions.Fresh(product,result.OutputBlueprints[kind])
                    || !ids.Add(product.ID) || pack.Objects.Concat(pack.EquippedItems.Values).Concat(zone.GetReadOnlyEntities()).Concat(plan.Stock?.Container.Contents??Enumerable.Empty<Entity>()).Any(e=>e==product||e?.ID==product.ID)
                    || !tx.TryClaim(product,actor,command))return Refuse();
                var state=templates[kind].Snapshot(product);if(state==null)return Refuse();states.Add(product,state);
            }
            if(!quote.Current())return Refuse();
            if (groundedGrit != null && !Guard(groundedGrit, tx)) return Refuse();
            var packProducts = products.Where(product => product != groundedGrit).ToArray();
            var residents=pack.Objects.Where(o=>result.OutputBlueprints.Contains(o.BlueprintName)
                && o.BlueprintName!=groundedGrit?.BlueprintName).ToArray();
            bool ExactGroundedGrit()=>groundedGrit!=null && groundedGrit.ID==groundedGritId
                && groundedGrit.BlueprintName==result.OutputBlueprints[1] && groundedGrit.SpatialZone==zone
                && zone.GetEntityCell(groundedGrit)==quote.GritGround && quote.GritGround.Objects.Contains(groundedGrit)
                && groundedGrit.GetPart<PhysicsPart>() is {InInventory:null,Equipped:null}
                && !pack.Objects.Contains(groundedGrit) && Count(groundedGrit)==1;
            // Size separation has one source and creates ordinary, unmarked clay.
            // Its output may join an existing valid origin stack without touching the stamp.
            bool screening=plan.ExtraInputs.Count==0 && result.Id==SiftableStockPart.ProcessId;
            bool storedScreening = screening && plan.Stock != null;
            var markedResidents=new HashSet<Entity>(residents.Where(o=>screening && o.BlueprintName=="FireClay" && o.HasPart<ConnectedClayOriginPart>()));
            // These recipients may leave the pack in an independent spill. Undo
            // only their added units, wherever the original stack ends up.
            var scopedResidents = groundedGrit == null && !storedScreening ? markedResidents : new HashSet<Entity>(residents);
            foreach(var resident in residents)
            {
                var template=templates[result.OutputBlueprints.ToList().IndexOf(resident.BlueprintName)];
                var state=template.Snapshot(resident,allowClayStamp:markedResidents.Contains(resident));
                if(state==null || !PreparationActions.Carried(actor,resident) || !tx.TryClaim(resident,actor,command))return Refuse();
                states.Add(resident,state);
            }
            int ceiling=pack.MaxWeight<0?int.MaxValue:Math.Max(pack.MaxWeight,pack.GetCarriedWeight());
            var beforeCounts=residents.ToDictionary(e=>e,Count);
            var expected=new Dictionary<Entity,int>(beforeCounts);var received=new HashSet<Entity>();
            var individuallyPaid=new HashSet<Entity>(inputs.Where(plan.IndividuallyPaidClay));
            if (groundedGrit != null) individuallyPaid.Add(input);
            bool CanRetractRecipient(Entity owner) => states[owner].Matches() && Count(owner) == expected[owner];
            bool CanRefundScreening() => (!placedGrit || ExactGroundedGrit())
                && received.Where(scopedResidents.Contains).All(CanRetractRecipient)
                && (!storedScreening || sourceCount > units || plan.Stock.ExcludesPaidItem(input));
            var receipt=InventoryTransferSnapshot.Capture(pack,packProducts);bool restored=false;
            // Only inputs this transaction actually consumed from are detached on undo: Restore re-inserts exactly the changed entries, so detaching an untouched input
            // (reachable once an ingredient can span several stacks and a later take refuses) would lose the player's stack.
            var consumed=new HashSet<Entity>();
            tx.Do(null,()=>
            {
                if(restored)return;restored=true;
                var conflicts=new HashSet<Entity>(consumed.Where(item=>!pack.Objects.Contains(item)
                    && pack.Objects.Any(other=>other!=item && other?.ID==item.ID)));
                var separate=new HashSet<Entity>(individuallyPaid);
                bool retainedOutput=false;
                if (placedGrit)
                {
                    if (ExactGroundedGrit()) zone.RemoveEntity(groundedGrit);
                    else retainedOutput = true;
                }
                foreach(var owner in received.Where(scopedResidents.Contains))
                {
                    separate.Add(owner);
                    if(CanRetractRecipient(owner))
                    {
                        owner.GetPart<StackerPart>().StackCount=beforeCounts[owner];
                        owner.GetPart<PhysicsPart>().InInventory?.GetPart<InventoryPart>()?.RefreshHandlingCarryPenalty();
                    }
                    else retainedOutput=true;
                }
                var returnable=plan.Stock==null && !retainedOutput?consumed:Enumerable.Empty<Entity>();
                foreach(var owner in packProducts.Concat(received).Concat(returnable).Distinct())
                    if(!separate.Contains(owner))Detach(owner);
                var excluded=new HashSet<Entity>(conflicts);
                excluded.UnionWith(separate);
                if(retainedOutput)
                {
                    // The independently changed recipient may still hold output.
                    // Returning its source as well would create a second batch.
                    excluded.UnionWith(consumed);
                }
                receipt.RestoreExcept(excluded);
                if(retainedOutput)
                {
                    CavesOfOoo.Diagnostics.Diag.Record("event","MaterialProcessRollbackLimited",actor:actor,target:input,
                        payload:new{reason="screening-output-changed"});
                    MessageLog.Add("Some screened material could not be recovered safely. One packet was spent.");
                }
                foreach(var item in conflicts)CavesOfOoo.Diagnostics.Diag.Record("event","MaterialProcessRollbackLimited",actor:actor,target:item,
                    payload:new{reason="source-identity-conflict",identity=item.ID});
            });
            if(!receipt.Apply(()=>
            {
                for(int n=0;n<units;n++)
                {
                    bool paid = groundedGrit == null ? plan.Consume(input, tx, storedScreening ? CanRefundScreening : null)
                        : MaterialProcessPayment.Spend(actor, input, actor, zone, tx, sourceState.Matches, refundAllowed: CanRefundScreening);
                    if (!paid) return false;
                    consumed.Add(input);
                }
                foreach(var ingredient in plan.ExtraInputs)
                    foreach(var take in ingredient.Takes)
                        for(int n=0;n<take.Units;n++){if(!plan.Consume(take.Owner,tx))return false;consumed.Add(take.Owner);}
                foreach(var product in packProducts)
                {
                    if(!pack.AddCraftedUnit(product,out var recipient) || recipient==null)return false;
                    received.Add(recipient);expected.TryGetValue(recipient,out int prior);expected[recipient]=prior+1;
                }
                if (groundedGrit != null)
                {
                    if (!quote.GroundCurrent() || !Loose(groundedGrit)) return false;
                    placedGrit = zone.AddEntity(groundedGrit, quote.GritGround.X, quote.GritGround.Y);
                    if (!placedGrit) return false;
                }
                return true;
            }) || !receipt.ClaimChanges(tx,actor,command))return Refuse();
            bool Final()
            {
                bool paymentCurrent = sourceCount > units ? plan.InputAvailable(input)
                    : Loose(input) && !pack.Objects.Contains(input) && (!storedScreening || plan.Stock.ExcludesPaidItem(input));
                if(!plan.ContextCurrent() || !sourceState.Matches() || !quote.OutputsCurrent() || Count(input)!=Math.Max(1,sourceCount-units)
                    || !paymentCurrent
                    || pack.GetCarriedWeight()>ceiling || storedScreening && pack.MaxWeight>=0 && pack.GetCarriedWeight()>pack.MaxWeight
                    || states.Values.Any(s=>!s.Matches()) || plan.ExtraInputs.Any(i=>!i.After(actor)))return false;
                foreach(var pair in expected)if(!PreparationActions.Carried(actor,pair.Key) || Count(pair.Key)!=pair.Value)return false;
                if (groundedGrit != null && (!ExactGroundedGrit()
                    || zone.GetReadOnlyEntities().Any(e => e != groundedGrit && e.ID == groundedGrit.ID)
                    || pack.Objects.Concat(pack.EquippedItems.Values).Any(e => e?.ID == groundedGrit.ID))) return false;
                foreach(var product in packProducts)
                    if(!received.Contains(product) && (!Loose(product)||Count(product)!=0||pack.Objects.Contains(product)))return false;
                return true;
            }
            if(!Final())return Refuse();
            foreach(var owner in products.Concat(received).Concat(inputs).Distinct())if(!Guard(owner,tx))return Refuse();
            tx.BeforeCommit(Final);
            string message = groundedGrit == null
                ? "You complete the quoted " + plan.Definition.Mode.ToLowerInvariant() + " work. The declared inputs are spent; the products are in your pack."
                : "You pack the finer fraction and leave the coarse grit loose at your feet.";
            PreparationActions.Complete(tx,actor,input,command,message);
            foreach(var owner in participants)
            {var captured=owner;tx.AfterCommit(()=>Emit(captured,"MaterialProcessCompleted",quote));}
            return true;
        }
    }
}
