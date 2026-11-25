using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    internal interface IMaterialProcessPermission
    { bool Allows(MaterialProcessDefinition definition); }
    internal interface IMaterialReclaimPermission : IMaterialProcessPermission { }
    /// <summary>Authored permission only. No material tag implies a salvage yield.</summary>
    public sealed class MaterialProcessInputPart : Part, IPostCommitInventoryActionNotification
    {
        public override string Name => "MaterialProcessInput";
        public string ProcessIds = "";
        internal bool Allows(MaterialProcessDefinition definition) => definition != null
            && ParentEntity?.GetPart<MaterialProcessInputPart>() == this
            && ParentEntity.BlueprintName == definition.SourceBlueprint
            && Array.IndexOf((ProcessIds ?? "").Split(','), definition.Id) >= 0;
        public const string CommandPrefix="MaterialProcess|";
        public static bool IsCommand(string command)=>command?.StartsWith(CommandPrefix,StringComparison.Ordinal)==true;
        // The shared material transaction owns item/quantity undo. A blanket
        // executor snapshot would heal an independent injury on a late veto.
        private MaterialProcessDefinition ReceiptedDefinition(string command)
        {
            if(ParentEntity?.GetPart<MaterialProcessInputPart>()!=this || !IsCommand(command))return null;
            var fields=command.Split('|');
            if(fields.Length!=4)return null;
            var definition=MaterialProcessRegistry.Get(fields[1]);
            return Allows(definition)?definition:null;
        }
        public bool OwnsInventoryReceipt(string command)=>ReceiptedDefinition(command)!=null;
        public bool NotifyAfterCommit(string command)=>ReceiptedDefinition(command)?.WorkKind==SeedFramePart.WorkKind;
        public override bool HandleEvent(GameEvent e)=>MaterialProcessActions.Handle(ParentEntity,e,(ProcessIds??" ").Split(','));
    }
    // One cache per actual owner, never a persisted entity-ID lookup. Both
    // permission adapters use the same gather/dispatch and transaction surface.
    internal static class MaterialProcessActions
    {
        sealed class Menu { internal readonly Dictionary<string,MaterialProcessService.Quote> Quotes=new Dictionary<string,MaterialProcessService.Quote>(); }
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Entity,Menu> Menus=new System.Runtime.CompilerServices.ConditionalWeakTable<Entity,Menu>();
        static string Label(CavesOfOoo.Data.EntityFactory factory,string bp)
            => factory.Blueprints[bp].Parts.TryGetValue("Render",out var p) && p.TryGetValue("DisplayName",out var label)?label:bp;
        internal static bool Handle(Entity owner,GameEvent e,IEnumerable<string> ids,SiftableStockPart sizeStock=null)
        {
            if(owner==null)return true;
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions")
            {
                if(e.GetParameter<Entity>("MaterialProcessGatherOwner")==owner)return true;
                e.SetParameter("MaterialProcessGatherOwner",owner);var menu=Menus.GetValue(owner,_=>new Menu());
                var previous=menu.Quotes.ToArray();menu.Quotes.Clear();
                foreach(var id in ids.Distinct())
                {
                    var plan=sizeStock==null?MaterialProcessPlan.TryCreate(actor,owner,id,zone,ForgePart.Factory)
                        :MaterialProcessPlan.TryCreateScreening(actor,owner,zone,ForgePart.Factory,sizeStock);
                    var quoted=MaterialProcessService.TryQuote(plan);
                    if(quoted==null)
                    {
                        // Not runnable now: explain a near-miss with one disabled row (no quote, no token, never dispatched).
                        if(sizeStock==null&&plan==null&&MaterialProcessMisses.Explain(actor,owner,id,zone,ForgePart.Factory) is MaterialProcessMisses.Miss miss)
                            e.GetParameter<InventoryActionList>("Actions")?.AddDisabled(id,MaterialProcessRegistry.Get(id).ActionText,miss.Code,miss.Reason,'\0',MaterialProcessMisses.RowPriority);
                        else if(sizeStock!=null && sizeStock.Definition(ForgePart.Factory)!=null && PreparationActions.CurrentActor(actor,zone)
                            && PreparationActions.Carried(actor,owner) && MaterialProcessTemplate.Capture(ForgePart.Factory,owner.BlueprintName)?.Snapshot(owner,true)!=null)
                            e.GetParameter<InventoryActionList>("Actions")?.AddDisabled(id,"screen size grades","screening-unavailable","Carry a sound sieve and leave room for both grades.",'\0',19);
                        continue;
                    }
                    var retained=previous.FirstOrDefault(p=>p.Value.Plan.Actor==actor && p.Value.Plan.Source==owner && p.Value.Plan.Zone==zone
                        && p.Value.Plan.Station==plan.Station && p.Value.Plan.SameDefinition(plan) && p.Value.Current());
                    string token=retained.Key??Guid.NewGuid().ToString("N");menu.Quotes[token]=retained.Value??quoted;
                    var d=plan.Definition;
                    string outputs=string.Join(" + ",d.OutputBlueprints.Select((bp,i)=>d.OutputCounts[i]+" "+Label(plan.Factory,bp)));
                    string inputs=d.AdditionalInputBlueprints.Count==0?d.InputCount.ToString():d.InputCount+" "+Label(plan.Factory,d.SourceBlueprint)+" + "+string.Join(" + ",d.AdditionalInputBlueprints.Select((bp,i)=>d.AdditionalInputCounts[i]+" "+Label(plan.Factory,bp)));
                    var row=e.GetParameter<InventoryActionList>("Actions")?.AddAction(id,d.ActionText+": "+inputs+" -> "+outputs+(d.LossText.Length>0?"; "+d.LossText:"")+" (1 action, pack)",
                        MaterialProcessInputPart.CommandPrefix+id+"|"+Uri.EscapeDataString(plan.StationId)+"|"+token,'\0',19,false,ActionTime.OneAction);
                    if(row!=null && d.WorkKind==SeedFramePart.WorkKind)
                    {
                        row.Display=d.ActionText+" "+d.InputCount+" "+Label(plan.Factory,d.SourceBlueprint);
                        row.Hint="Makes "+outputs+". "+d.LossText+". One action. The seed goes into your pack.";
                        row.AtEntity(zone,plan.Station);
                    }
                }
                return true;
            }
            string command=e.GetStringParameter("Command");
            if(e.ID!="InventoryAction" || !MaterialProcessInputPart.IsCommand(command))return true;
            if(e.GetParameter<bool>("MaterialProcessAttempted"))return true;e.SetParameter("MaterialProcessAttempted",true);
            var fields=command.Split('|');if(fields.Length!=4 || !Menus.TryGetValue(owner,out var current) || !current.Quotes.TryGetValue(fields[3],out var selectedQuote))return true;
            current.Quotes.Remove(fields[3]);var selected=selectedQuote.Plan;
            if(selected.Actor!=actor || selected.Source!=owner || selected.Zone!=zone || selected.Definition.Id!=fields[1]
                || Uri.EscapeDataString(selected.StationId)!=fields[2]
                || !MaterialProcessService.TryExecute(selectedQuote,e.GetParameter<InventoryTransaction>("InventoryTransaction"),command))return true;
            e.Handled=true;return false;
        }
    }
}
