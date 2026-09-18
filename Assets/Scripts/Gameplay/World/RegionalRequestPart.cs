using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Core
{
    /// <summary>Owner-bound optional work. Public scalar fields use native Part
    /// persistence. All mutation joins the inventory command's transaction,
    /// including completion, notes, split stacks, stock, reward and currency.</summary>
    public sealed class RegionalRequestPart : Part
    {
        public override string Name => "RegionalRequest";
        public string DefinitionId, InstanceId, RecipientId;
        public bool Accepted, Completed;
        [NonSerialized] private string cachedDefinitionId, expectedInstance, expectedSourceId, completionKey, noteKey;
        [NonSerialized] private int cachedSeed;
        [NonSerialized] private RegionalSituationDefinition definition;

        private bool Context(Entity actor,Zone zone,bool adjacent,out OverworldZoneManager manager)
        {
            manager=WorldLocationContext.For(zone);
            if(manager==null||zone==null||!manager.CachedZones.TryGetValue(zone.ZoneID,out var live)||!ReferenceEquals(live,zone)
                ||actor==null||actor!=StoryletPart.LocalPlayer||!actor.HasTag("Player")
                ||ParentEntity==null||ParentEntity==actor||!ParentEntity.HasTag("Creature")
                ||actor.GetStatValue("Hitpoints")<=0||ParentEntity.GetStatValue("Hitpoints")<=0
                ||CombatSystem.IsDeathHandled(actor)||CombatSystem.IsDeathHandled(ParentEntity)
                ||ParentEntity.GetPart<RenderPart>()?.Visible!=true||FactionManager.IsHostile(ParentEntity,actor))return false;
            if(definition==null||cachedDefinitionId!=DefinitionId||cachedSeed!=manager.WorldSeed)
            {
                definition=RegionalSituations.Find(DefinitionId);cachedDefinitionId=DefinitionId;cachedSeed=manager.WorldSeed;
                expectedInstance=RegionalSituations.InstanceId(definition,cachedSeed);
                expectedSourceId=expectedInstance+":source";
                completionKey=RegionalSituationNotes.CompletionKey(expectedInstance);noteKey=RegionalSituationNotes.NoteKey(expectedInstance);
            }
            if(definition==null||zone.ZoneID!=definition.RecipientZoneId||InstanceId!=expectedInstance||RecipientId!=ParentEntity.ID
                ||!RegionalSituations.RecipientMapAllowed(definition,manager)
                ||!ReferenceEquals(RegionalSituations.Recipient(zone,definition),ParentEntity)
                ||!ParentEntity.HasPart<InventoryPart>()||!ParentEntity.HasPart<TraderPart>()||!ParentEntity.HasPart<ConversationPart>()
                ||definition.ResidentId==null&&ParentEntity.BlueprintName!=definition.RecipientBlueprint)return false;
            if(definition.ResidentId!=null&&!ReferenceEquals(MorrowfastSceneRuntime.FindOwner(zone,definition.ResidentId),ParentEntity))return false;
            var person=zone.GetEntityCell(ParentEntity);var visitor=zone.GetEntityCell(actor);
            return person!=null&&visitor!=null&&person.Objects.Contains(ParentEntity)&&visitor.Objects.Contains(actor)
                &&(!adjacent||Math.Abs(person.X-visitor.X)<=1&&Math.Abs(person.Y-visitor.Y)<=1);
        }
        private bool Finished(Entity actor)=>Completed||actor.GetIntProperty(completionKey)==1;

        /// <summary>Distance-safe cue query. No source generation, inventory
        /// scanning or JSON parsing. Identity strings are cached per world and
        /// definition; the restored native owner index is built once per zone.</summary>
        public QuestCueState GetCueState(Entity actor,Zone zone)
        {
            if(!Context(actor,zone,false,out var manager)||Finished(actor))return QuestCueState.None;
            if(definition.Kind==RegionalSituationKind.Recovery)
            {
                if(!RegionalSituations.SourceAllowed(definition,manager))return QuestCueState.None;
                if(!Accepted&&!RegionalSituations.KnownCargoAvailable(manager,actor,definition,expectedInstance,expectedSourceId))return QuestCueState.None;
            }
            return Accepted?QuestCueState.Active:QuestCueState.Available;
        }

        /// <summary>Pure action eligibility. Quantities are checked on delivery
        /// execution as well; these rows never promise a guaranteed reward.</summary>
        public bool CanAct(Entity actor,Zone zone,string action)
        {
            if(!Context(actor,zone,true,out var manager)||Finished(actor))return false;
            if(action=="release")return Accepted;
            if(action=="deliver")return Accepted;
            if(action!="read")return false;
            if(definition.Kind==RegionalSituationKind.Supply)return true;
            if(!RegionalSituations.SourceAllowed(definition,manager))return false;
            if(!manager.CachedZones.TryGetValue(definition.SourceZoneId,out var source))return true;
            return RegionalSituations.IsCargo(RegionalSituations.FindOwner(source,expectedInstance+":source"),definition,expectedInstance)
                ||RegionalSituations.CarriedCargo(actor,definition,expectedInstance)!=null;
        }

        public bool TryAct(Entity actor,Zone zone,string action)
        {
            if(!CanAct(actor,zone,action))return Reject(actor,action,"unavailable");
            return InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(ParentEntity,"RegionalRequest:"+action),actor,zone).Success;
        }
        public override bool HandleEvent(GameEvent e)
        {
            var zone=e.GetParameter<Zone>("Zone")??SettlementRuntime.ActiveZone;
            var actor=e.GetParameter<Entity>("Actor");
            if(e.ID=="GetInventoryActions")
            {
                var actions=e.GetParameter<InventoryActionList>("Actions");
                if(CanAct(actor,zone,"read"))actions?.AddAction("RegionalRequestRead","read the regional request","RegionalRequest:read",'q',9);
                if(CanAct(actor,zone,"deliver"))actions?.AddAction("RegionalRequestDeliver","deliver the requested goods","RegionalRequest:deliver",'g',9);
                if(CanAct(actor,zone,"release"))actions?.AddAction("RegionalRequestRelease","release this request","RegionalRequest:release",'u',8);
                return true;
            }
            if(e.ID!="InventoryAction")return true;
            string command=e.GetStringParameter("Command");
            if(command==null||!command.StartsWith("RegionalRequest:",StringComparison.Ordinal))return true;
            string action=command.Substring("RegionalRequest:".Length);
            var transaction=e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if(transaction==null||!CanAct(actor,zone,action)||!transaction.TryClaim(ParentEntity,actor,command))
            {Reject(actor,action,"unavailable-or-busy");return true;}
            var manager=WorldLocationContext.For(zone);
            bool oldAccepted=Accepted,oldCompleted=Completed;
            bool hadNote=actor.Properties.TryGetValue(noteKey,out string oldNote);
            bool hadCompletion=actor.IntProperties.TryGetValue(completionKey,out int oldCompletion);
            transaction.Do(null,()=>
            {
                Accepted=oldAccepted;Completed=oldCompleted;
                if(hadNote)actor.Properties[noteKey]=oldNote;else actor.Properties.Remove(noteKey);
                if(hadCompletion)actor.IntProperties[completionKey]=oldCompletion;else actor.IntProperties.Remove(completionKey);
            });
            if(action=="read")
            {
                Zone source=null;
                if(RegionalSituations.SourceAllowed(definition,manager))source=manager.GetZone(definition.SourceZoneId);
                if(definition.Kind==RegionalSituationKind.Recovery
                    &&!RegionalSituations.IsCargo(RegionalSituations.FindOwner(source,expectedInstance+":source"),definition,expectedInstance)
                    &&RegionalSituations.CarriedCargo(actor,definition,expectedInstance)==null)
                {Reject(actor,action,"source-unavailable");return true;}
                Accepted=true;
                string note=RecordNote(actor,manager,"accepted");
                transaction.AfterCommit(()=>MessageLog.Add(note+" Recorded in [Q], [Tab] Field Notes."));
            }
            else if(action=="release")
            {
                Accepted=false;RecordNote(actor,manager,"released");
                string releaseMessage="You release "+definition.Title+". No goods or payment change hands.";
                transaction.AfterCommit(()=>MessageLog.Add(releaseMessage));
            }
            else if(action=="deliver")
            {
                if(!Deliver(actor,zone,manager,transaction))return true;
            }
            else return true;
            e.Handled=true;
            var recipient=ParentEntity;
            var receipt=new{definition=DefinitionId,instance=InstanceId,action};
            transaction.AfterCommit(()=>Diag.Record("quest","RegionalRequestApplied",actor:actor,target:recipient,payload:receipt));
            return false;
        }

        private bool Deliver(Entity actor,Zone zone,OverworldZoneManager manager,InventoryTransaction tx)
        {
            var inventory=actor.GetPart<InventoryPart>();var stock=ParentEntity.GetPart<InventoryPart>();
            if(inventory==null||stock==null)return Reject(actor,"deliver","missing-inventory");
            var reward=RegionalSituations.CreateItem(manager.Factory,definition.RewardBlueprint);
            if(reward==null)return Reject(actor,"deliver","missing-reward");
            var payments=new List<(Entity item,int count)>();var outputs=new List<Entity>();
            Entity cargo=null;int preservation=0;
            if(definition.Kind==RegionalSituationKind.Supply)
            {
                int remaining=definition.ItemCount;
                foreach(var item in inventory.Objects)
                {
                    if(item.BlueprintName!=definition.ItemBlueprint||!RegionalSituations.OwnedPayment(actor,inventory,item))continue;
                    int count=Math.Min(remaining,item.GetPart<StackerPart>()?.StackCount??1);
                    payments.Add((item,count));remaining-=count;if(remaining==0)break;
                }
                if(remaining!=0)return Reject(actor,"deliver","missing-supply");
            }
            else
            {
                cargo=RegionalSituations.CarriedCargo(actor,definition,expectedInstance);
                if(cargo==null)return Reject(actor,"deliver","wrong-cargo");
                payments.Add((cargo,1));
                for(int i=0;i<definition.ItemCount;i++)
                {var output=RegionalSituations.CreateItem(manager.Factory,definition.ItemBlueprint);if(output==null)return Reject(actor,"deliver","missing-stock");outputs.Add(output);}
                if(manager.CachedZones.TryGetValue(definition.SourceZoneId,out var source))
                    preservation=RegionalSituations.PreservationBonus(cargo,source,expectedInstance);
            }
            // Lock the recipient and every possible destination merge stack
            // before callbacks from splitting or packing can re-enter trade.
            foreach(var e in stock.Objects)if(!tx.TryClaim(e,actor,"RegionalDelivery"))return Reject(actor,"deliver","stock-busy");
            foreach(var p in payments)if(!tx.TryClaim(p.item,actor,"RegionalDelivery"))return Reject(actor,"deliver","payment-busy");
            foreach(var p in payments)
            {
                Entity transferred=null;var receipt=InventoryTransferSnapshot.Capture(inventory,p.item);
                // Do executes apply before registering undo, so pre-register the
                // receipt. SplitStack changes count BEFORE calling clone hooks.
                tx.Do(null,receipt.Restore);
                bool removed=receipt.Apply(()=>
                {
                    if(!RegionalSituations.OwnedPayment(actor,inventory,p.item))return false;
                    var stack=p.item.GetPart<StackerPart>();int count=stack?.StackCount??1;
                    if(count<p.count)return false;
                    if(count>p.count)
                    {
                        transferred=stack.SplitStack(p.count);
                        if(transferred==null)return false;
                        var physics=transferred.GetPart<PhysicsPart>();if(physics!=null){physics.InInventory=null;physics.Equipped=null;}
                        return true;
                    }
                    transferred=p.item;return inventory.RemoveObject(p.item);
                });
                if(!removed)throw new InvalidOperationException("The requested payment could not be transferred.");
                if(cargo==null)outputs.Add(transferred);
            }
            foreach(var output in outputs)
            {
                if(!tx.TryClaim(output,actor,"RegionalDelivery"))throw new InvalidOperationException("The outgoing stock is already in use.");
                var receipt=InventoryTransferSnapshot.Capture(stock,output);tx.Do(null,receipt.Restore);
                if(!receipt.Apply(()=>stock.AddObject(output))||!receipt.ClaimChanges(tx,actor,"RegionalDelivery")
                    ||stock.MaxWeight>=0&&stock.GetCarriedWeight()>stock.MaxWeight)
                    throw new InvalidOperationException("The recipient cannot carry this delivery.");
            }
            var rewardReceipt=InventoryTransferSnapshot.Capture(inventory,reward);tx.Do(null,rewardReceipt.Restore);
            if(!rewardReceipt.Apply(()=>inventory.AddObject(reward)))
            {
                rewardReceipt.Restore();var at=zone.GetEntityCell(actor);
                tx.Do(null,()=>{if(zone.GetEntityCell(reward)!=null)zone.RemoveEntity(reward);});
                if(at==null||!zone.AddEntity(reward,at.X,at.Y))throw new InvalidOperationException("The reward cannot be set down.");
                ZoneRenderHooks.MarkCellDirty(at.X,at.Y,"RegionalRewardOverflow");
            }
            else if(!rewardReceipt.ClaimChanges(tx,actor,"RegionalReward"))throw new InvalidOperationException("The reward stack is already in use.");
            // Publish completion before deferred currency notifications. The
            // transaction rolls these exact fields/properties back if any later
            // native AfterInventoryAction callback or currency check refuses.
            Accepted=false;Completed=true;actor.IntProperties[completionKey]=1;
            RecordNote(actor,manager,"completed");
            tx.DeferCurrencyCredit(actor,definition.RewardDrams+preservation);
            var cell=zone.GetEntityCell(ParentEntity);if(cell!=null)ZoneRenderHooks.MarkCellDirty(cell.X,cell.Y,"RegionalRequestCompleted");
            string deliveryMessage="Delivered: "+definition.Title+". The goods join the recipient's trade stock."
                +(preservation>0?" The marked wetland bank remained intact; three extra drams are included.":"");
            tx.AfterCommit(()=>MessageLog.Add(deliveryMessage));
            return true;
        }
        // This is an interaction-time snapshot, never a cue/frame-loop query.
        // Native mineral harvesting removes the owner; crop harvesting retains a
        // spent row. Neither case may continue promising an available local source.
        private string RecordNote(Entity actor,OverworldZoneManager manager,string state)
        {
            bool available=true;
            if(definition.Kind==RegionalSituationKind.Supply)
            {
                available=false;
                if(RegionalSituations.SourceAllowed(definition,manager)
                    &&manager.CachedZones.TryGetValue(definition.SourceZoneId,out var source))
                {
                    int units=0;
                    for(int i=0;i<definition.ItemCount;i++)
                    {
                        string id=i==0?expectedSourceId:expectedSourceId+":"+i;
                        var owner=RegionalSituations.FindOwner(source,id);
                        var damage=owner?.GetPart<DestructiblePart>();
                        if(owner==null||damage!=null&&(damage.Gone||damage.HP<=0))continue;
                        var row=owner.GetPart<FieldHarvestPart>();
                        var vein=owner.GetPart<HarvestablePart>();
                        if(row!=null&&!row.Harvested&&row.YieldBlueprint==definition.ItemBlueprint)units+=Math.Max(0,row.YieldCount);
                        else if(vein!=null&&vein.YieldBlueprint==definition.ItemBlueprint&&vein.YieldChance>0)units+=Math.Max(0,vein.YieldMin);
                    }
                    available=units>=definition.ItemCount;
                }
            }
            return RegionalSituationNotes.Record(actor,definition,expectedInstance,state,ParentEntity,manager.Factory,available);
        }
        private bool Reject(Entity actor,string action,string reason)
        {
            Diag.Record("quest","RegionalRequestRejected",actor:actor,target:ParentEntity,payload:new{definition=DefinitionId,instance=InstanceId,action,reason});
            if(actor==StoryletPart.LocalPlayer)
            {
                var manager=WorldLocationContext.For(SettlementRuntime.ActiveZone);
                string message;
                if(reason=="missing-supply"&&definition!=null)
                    message="This delivery needs "+definition.ItemCount+" x "+RegionalSituationNotes.ItemName(manager?.Factory,definition.ItemBlueprint)
                        +" in your carried inventory. Bought or already carried goods are accepted; no goods were taken.";
                else if(reason=="wrong-cargo"&&definition!=null)
                    message="Bring the marked sealed consignment intact from ("+definition.SourceX+","+definition.SourceY
                        +"). Ordinary sacks and loose goods do not replace that cargo; no goods were taken.";
                else if(reason=="source-unavailable")
                    message="The marked consignment is no longer available. This request has not been accepted.";
                else if(reason=="missing-reward"||reason=="missing-stock")
                    message="The recipient cannot fulfill this delivery just now. No goods or payment changed hands.";
                else if(reason=="stock-busy"||reason=="payment-busy"||reason=="unavailable-or-busy")
                    message="Those goods or that recipient are already involved in another exchange. No delivery was made.";
                else message="This request action is unavailable here. No goods or payment changed hands.";
                MessageLog.Add(message);
            }
            return false;
        }
    }
}
