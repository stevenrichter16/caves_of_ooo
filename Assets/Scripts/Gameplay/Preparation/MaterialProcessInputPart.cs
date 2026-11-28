using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    internal interface IMaterialProcessPermission
    { bool Allows(MaterialProcessDefinition definition); }
    internal interface IMaterialReclaimPermission : IMaterialProcessPermission { }
    /// <summary>Authored permission only. No material tag implies a salvage yield.</summary>
    public sealed class MaterialProcessInputPart : Part, IPostCommitInventoryActionNotification, IDescribeState
    {
        public override string Name => "MaterialProcessInput";
        public string ProcessIds = "";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 36;
        public string DescribeState(DescribeContext context)
        {
            if (context?.Owner == null || context.Owner != ParentEntity) return null;
            var definitions = MaterialProcessRegistry.ForSource(ParentEntity);
            var summaries = definitions
                .Where(definition => definition.WorkKind == "Screening")
                .Select(definition => MaterialProcessActions.DescribeScreening(ParentEntity, this, definition))
                .Where(summary => summary != null);
            if (definitions.Any(definition => SeedFramePart.IsKnotflaxWork(definition)
                && MaterialProcessPlan.PermissionPart(ParentEntity, definition) == this))
                summaries = summaries.Append("Use a public seed frame or rub the heads over a sound hand sieve to keep one seed. Use a sieve and bellows on a dry floor under cover to keep the chaff too.");
            if (definitions.Any(definition => AnvilPointWorkReceipt.Supports(definition) && Allows(definition)))
                summaries = summaries.Append("A sound warhammer or assembled peat-packing mallet can dress this blank beside an anvil. Keep the hammer.");
            return string.Join("\n", summaries);
        }
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
        internal static string Label(CavesOfOoo.Data.EntityFactory factory,string bp)
            => !string.IsNullOrEmpty(bp) && factory?.Blueprints.TryGetValue(bp, out var blueprint) == true
                && blueprint.Parts.TryGetValue("Render",out var p) && p.TryGetValue("DisplayName",out var label) ? label : bp;
        internal static string Outputs(CavesOfOoo.Data.EntityFactory factory, MaterialProcessDefinition definition)
            => string.Join(" + ", definition.OutputBlueprints.Select((bp, i) =>
                definition.OutputCounts[i].ToString(CultureInfo.InvariantCulture) + " " + Label(factory, bp)));

        private static string OutputNames(CavesOfOoo.Data.EntityFactory factory, MaterialProcessDefinition definition)
            => string.Join(" + ", definition.OutputBlueprints.Select(bp => Label(factory, bp)));

        private static InventoryAction AsRecipe(InventoryAction action, CavesOfOoo.Data.EntityFactory factory, MaterialProcessDefinition definition)
        {
            if (action == null || definition == null) return action;
            var category = definition.Mode == "Reclaim" || definition.Mode == "Dismantle"
                ? PackRecipeCategory.Reclamation
                : definition.WorkKind switch
                {
                    SeedFramePart.WorkKind => PackRecipeCategory.FieldSupplies,
                    "ForgeCold" => PackRecipeCategory.ToolsAndGear,
                    "HandCutting" => PackRecipeCategory.WeaponParts,
                    _ => AnvilPointWorkReceipt.Supports(definition) ? PackRecipeCategory.WeaponParts : PackRecipeCategory.Materials
                };
            var products = definition.OutputBlueprints;
            if (products.Count == 1)
            {
                var preview = PackRecipeEffects.FromBlueprint(factory, products[0]);
                return action.AsRecipe(category, OutputNames(factory, definition), Outputs(factory, definition),
                    preview.Details, highlights: preview.Highlights);
            }
            var previews = products.Select(bp => (Name: Label(factory, bp), Preview: PackRecipeEffects.FromBlueprint(factory, bp))).ToArray();
            var details = previews.Where(product => !string.IsNullOrEmpty(product.Preview.Details))
                .Select(product => product.Name + "\n" + product.Preview.Details);
            var highlights = previews.Where(product => product.Preview.Highlights != null)
                .SelectMany(product => product.Preview.Highlights.Select(highlight => product.Name + ": " + highlight)).ToArray();
            return action.AsRecipe(category, OutputNames(factory, definition), Outputs(factory, definition),
                string.Join("\n\n", details), highlights: highlights);
        }

        // This describes material potential. The action checks the actual tool,
        // destination and capacity without the reader gathering any action rows.
        internal static string DescribeScreening(Entity owner, Part provider, MaterialProcessDefinition definition)
        {
            var factory = ForgePart.Factory;
            if (factory == null || string.IsNullOrEmpty(owner?.ID) || definition?.WorkKind != "Screening"
                || definition.Mode != "Process" || definition.InputCount != 1 || definition.AdditionalInputBlueprints.Count != 0
                || !ReferenceEquals(MaterialProcessPlan.PermissionPart(owner, definition), provider)
                || owner.GetPart<StackerPart>() is StackerPart stack && (stack.StackCount <= 0 || stack.StackCount > stack.MaxStack)
                || MaterialProcessRegistry.ValidateForWork(factory, definition.WorkKind).Count != 0
                || MaterialProcessTemplate.Capture(factory, owner.BlueprintName)?.Matches(owner, allowInstances: true) != true
                || definition.OutputBlueprints.Any(bp => MaterialProcessTemplate.Capture(factory, bp, true) == null)) return null;

            var loss = definition.LossText;
            return "Screen one packet with a hand sieve to get " + Outputs(factory, definition) + "."
                + (loss.Length == 0 ? "" : " " + char.ToUpperInvariant(loss[0]) + loss.Substring(1) + ".");
        }

        internal static bool Handle(Entity owner,GameEvent e,IEnumerable<string> ids,SiftableStockPart sizeStock=null)
        {
            if(owner==null)return true;
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions")
            {
                if(e.GetParameter<Entity>("MaterialProcessGatherOwner")==owner)return true;
                e.SetParameter("MaterialProcessGatherOwner",owner);var menu=Menus.GetValue(owner,_=>new Menu());
                var previous=menu.Quotes.ToArray();menu.Quotes.Clear();
                var actions = e.GetParameter<InventoryActionList>("Actions");
                string Command(MaterialProcessService.Quote choice)
                {
                    var selected = choice.Plan;
                    var retained = previous.FirstOrDefault(p => p.Value.Plan.Actor == actor && p.Value.Plan.Source == owner
                        && p.Value.Plan.Zone == zone && p.Value.Plan.Station == selected.Station && p.Value.Plan.SameDefinition(selected)
                        && p.Value.Plan.WorkTool == selected.WorkTool && p.Value.Plan.IsShelteredWinnow == selected.IsShelteredWinnow
                        && p.Value.GritGround == choice.GritGround && p.Value.Current());
                    string token = retained.Key ?? Guid.NewGuid().ToString("N");
                    menu.Quotes[token] = retained.Value ?? choice;
                    return MaterialProcessInputPart.CommandPrefix + choice.ActionId + "|" + Uri.EscapeDataString(selected.StationId) + "|" + token;
                }
                foreach(var id in ids.Distinct())
                {
                    var definition = sizeStock == null ? MaterialProcessRegistry.Get(id) : sizeStock.Definition(ForgePart.Factory);
                    var plan=sizeStock==null?MaterialProcessPlan.TryCreate(actor,owner,id,zone,ForgePart.Factory)
                        :MaterialProcessPlan.TryCreateScreening(actor,owner,zone,ForgePart.Factory,sizeStock);
                    if (actions != null && sizeStock == null && SeedFramePart.IsKnotflaxWork(definition)
                        && MaterialProcessService.TryQuote(MaterialProcessPlan.TryCreateShelteredWinnow(actor, owner, id, zone, ForgePart.Factory)) is {} sheltered)
                    {
                        var kept = Outputs(sheltered.Plan.Factory, sheltered.Result);
                        AsRecipe(actions.AddActionWithHint("winnow_with_bellows", "winnow with bellows (keep chaff)", Command(sheltered),
                            "Use a sieve and bellows on one bundle. Makes " + kept + ". Keep both tools. One action.",
                            '\0', 19, false, ActionTime.OneAction), sheltered.Plan.Factory, sheltered.Result);
                    }
                    var quoted=MaterialProcessService.TryQuote(plan, out bool reservedForCrafting);
                    if(quoted==null)
                    {
                        InventoryAction unavailable = null;
                        // Not runnable now: explain a near-miss with one disabled row (no quote, no token, never dispatched).
                        if (sizeStock == null && reservedForCrafting)
                            unavailable = actions?.AddDisabled(id, plan.Definition.ActionText,
                                "crafting-reserved", "This item is set aside for crafting. Choose Take back (crafting) to make it available for material work.",
                                '\0', MaterialProcessMisses.RowPriority);
                        else if(sizeStock==null&&plan==null&&MaterialProcessMisses.Explain(actor,owner,id,zone,ForgePart.Factory) is MaterialProcessMisses.Miss miss)
                            unavailable = actions?.AddDisabled(id,definition.ActionText,miss.Code,miss.Reason,'\0',MaterialProcessMisses.RowPriority);
                        else if(sizeStock!=null && definition!=null && PreparationActions.CurrentActor(actor,zone)
                            && PreparationActions.Carried(actor,owner) && MaterialProcessTemplate.Capture(ForgePart.Factory,owner.BlueprintName)?.Snapshot(owner,true)!=null)
                            unavailable = actions?.AddDisabled(id,"screen size grades","screening-unavailable","Carry a sound sieve and leave room for both grades.",'\0',19);
                        AsRecipe(unavailable, ForgePart.Factory, definition);
                        continue;
                    }
                    var d=quoted.Result;
                    string outputs = Outputs(plan.Factory, d);
                    string inputs=d.AdditionalInputBlueprints.Count==0?d.InputCount.ToString():d.InputCount+" "+Label(plan.Factory,d.SourceBlueprint)+" + "+string.Join(" + ",d.AdditionalInputBlueprints.Select((bp,i)=>d.AdditionalInputCounts[i]+" "+Label(plan.Factory,bp)));
                    var row=AsRecipe(actions?.AddAction(id,d.ActionText+": "+inputs+" -> "+outputs+(d.LossText.Length>0?"; "+d.LossText:"")+" (1 action, pack)",
                        Command(quoted),'\0',19,false,ActionTime.OneAction), plan.Factory, d);
                    if (row != null && plan.StationKind == AnvilWorkPart.Kind)
                    {
                        var hammer = plan.WorkTool.GetPart<RenderPart>()?.DisplayName ?? plan.WorkTool.BlueprintName;
                        row.Display = "dress iron point at anvil";
                        row.Hint = $"Use your {hammer} to dress one recovered iron blank. Makes {outputs} in your pack. One action. Keep the hammer.";
                        row.AtEntity(zone, plan.Station);
                    }
                    if(row!=null && d.WorkKind==SeedFramePart.WorkKind)
                    {
                        if (plan.WorkTool != null)
                        {
                            row.Display = "rub and sift " + d.InputCount + " " + Label(plan.Factory, d.SourceBlueprint);
                            row.Hint = "Makes " + outputs + ". Rub the heads over the sieve and discard dry chaff. Keep the sieve. One action.";
                        }
                        else
                        {
                            row.Display=d.ActionText+" "+d.InputCount+" "+Label(plan.Factory,d.SourceBlueprint);
                            row.Hint="Makes "+outputs+". "+d.LossText+". One action. Products go into your pack.";
                            row.AtEntity(zone,plan.Station);
                        }
                    }
                    if (sizeStock != null && SiftableStockPart.GritDestination(plan) is Cell ground
                        && MaterialProcessService.TryQuote(plan, ground) is {} leave)
                    {
                        string label = d.SourceBlueprint == "DryClaySpoil" ? "screen clay and leave grit" : "screen size grades and leave grit";
                        int fine = SiftableStockPart.Weight(plan.Factory, d.OutputBlueprints[0]) * d.OutputCounts[0];
                        int coarse = SiftableStockPart.Weight(plan.Factory, d.OutputBlueprints[1]);
                        if (row != null)
                        {
                            row.Display = $"screen and pack both ({fine + coarse} lb, 1 action)";
                            row.Hint = "Makes " + outputs + ". Both grades go into your pack. Keep the sieve.";
                        }
                        actions?.AddActionWithHint(leave.ActionId,
                            $"{label} (carry {fine} lb, leave {coarse} lb, 1 action)", Command(leave),
                            $"One action. {fine} lb of {Label(plan.Factory, d.OutputBlueprints[0])} into your pack. "
                            + $"{coarse} lb of {Label(plan.Factory, d.OutputBlueprints[1])} at your feet. Keep the sieve.",
                            '\0',18,false,ActionTime.OneAction).AtCell(ground.X, ground.Y)
                            .AsRecipe(PackRecipeCategory.Materials, OutputNames(plan.Factory, d), outputs);
                    }
                }
                return true;
            }
            string command=e.GetStringParameter("Command");
            if(e.ID!="InventoryAction" || !MaterialProcessInputPart.IsCommand(command))return true;
            if(e.GetParameter<bool>("MaterialProcessAttempted"))return true;e.SetParameter("MaterialProcessAttempted",true);
            var fields=command.Split('|');if(fields.Length!=4 || !Menus.TryGetValue(owner,out var current) || !current.Quotes.TryGetValue(fields[3],out var selectedQuote))return true;
            current.Quotes.Remove(fields[3]);var selected=selectedQuote.Plan;
            if(selected.Actor!=actor || selected.Source!=owner || selected.Zone!=zone || selectedQuote.ActionId!=fields[1]
                || Uri.EscapeDataString(selected.StationId)!=fields[2]
                || !MaterialProcessService.TryExecute(selectedQuote,e.GetParameter<InventoryTransaction>("InventoryTransaction"),command))return true;
            e.Handled=true;return false;
        }
    }
}
