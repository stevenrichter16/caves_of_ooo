using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Authored permission only. No material tag implies a salvage yield.</summary>
    public sealed class MaterialProcessInputPart : Part
    {
        public override string Name => "MaterialProcessInput";
        public string ProcessIds = "";
        internal bool Allows(MaterialProcessDefinition definition) => definition != null
            && ParentEntity?.GetPart<MaterialProcessInputPart>() == this
            && ParentEntity.BlueprintName == definition.SourceBlueprint
            && Array.IndexOf((ProcessIds ?? "").Split(','), definition.Id) >= 0;
        [NonSerialized] readonly Dictionary<string,MaterialProcessService.Quote> quotes=new Dictionary<string,MaterialProcessService.Quote>();
        public const string CommandPrefix="MaterialProcess|";
        public static bool IsCommand(string command)=>command?.StartsWith(CommandPrefix,StringComparison.Ordinal)==true;
        public override bool HandleEvent(GameEvent e)
        {
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions")
            {
                quotes.Clear();
                foreach(var id in (ProcessIds??"").Split(',').Distinct())
                {
                    var plan=MaterialProcessPlan.TryCreate(actor,ParentEntity,id,zone,ForgePart.Factory);
                    var quote=MaterialProcessService.TryQuote(plan);if(quote==null)continue;
                    string token=Guid.NewGuid().ToString("N");quotes[token]=quote;
                    string outputs=string.Join(" + ",plan.Definition.OutputBlueprints.Select((bp,i)=>plan.Definition.OutputCounts[i]+" "+(ForgePart.Factory.Blueprints[bp].Parts["Render"].TryGetValue("DisplayName",out var label)?label:bp)));
                    e.GetParameter<InventoryActionList>("Actions")?.AddAction(id,
                        plan.Definition.ActionText+": "+plan.Definition.InputCount+" -> "+outputs+(plan.Definition.LossText.Length>0?"; "+plan.Definition.LossText:"")+" (1 action, pack)",
                        CommandPrefix+id+"|"+Uri.EscapeDataString(plan.StationId)+"|"+token,'\0',19);
                }
                return true;
            }
            string command=e.GetStringParameter("Command");
            if(e.ID!="InventoryAction" || !IsCommand(command))return true;
            if(e.GetParameter<bool>("MaterialProcessAttempted"))return true;e.SetParameter("MaterialProcessAttempted",true);
            var fields=command.Split('|');if(fields.Length!=4 || !quotes.TryGetValue(fields[3],out var selectedQuote))return true;
            quotes.Remove(fields[3]);var selected=selectedQuote.Plan;
            if(selected.Actor!=actor || selected.Source!=ParentEntity || selected.Zone!=zone || selected.Definition.Id!=fields[1]
                || Uri.EscapeDataString(selected.StationId)!=fields[2]
                || !MaterialProcessService.TryExecute(selectedQuote,e.GetParameter<InventoryTransaction>("InventoryTransaction"),command))return true;
            e.Handled=true;return false;
        }
    }
}
