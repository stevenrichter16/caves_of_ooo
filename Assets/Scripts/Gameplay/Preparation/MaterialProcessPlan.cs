using System;
using System.Linq;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>A read-only quote for one exact supervised process. This is not
    /// a durable job, an output reservation or permission to skip final validation.</summary>
    public sealed class MaterialProcessPlan
    {
        public MaterialProcessDefinition Definition { get; }
        public string SourceId { get; }
        public string StationId { get; }
        public int ActionCost => 1;
        internal readonly Entity Actor, Source, Station;
        internal readonly Zone Zone;
        internal readonly EntityFactory Factory;
        readonly InventoryPart pack;
        readonly PhysicsPart physics;
        readonly StackerPart stack;
        readonly Part permission;
        internal readonly System.Collections.Generic.IReadOnlyList<MaterialProcessIngredient> ExtraInputs;
        readonly string actorId, authorization, stationBlueprint;
        readonly Part[] sourceParts, stationParts;
        readonly Cell actorCell, stationCell;
        readonly int count, weight, maximumStack;
        readonly MaterialCuttingWorkReceipt cutting;

        MaterialProcessPlan(Entity actor, Entity source, Entity station, Zone zone,
            EntityFactory factory, MaterialProcessDefinition definition, MaterialProcessIngredient[] extraInputs, MaterialCuttingWorkReceipt cutting=null)
        {
            Actor=actor; Source=source; Station=station; Zone=zone; Factory=factory; Definition=definition;
            this.cutting=cutting;
            ExtraInputs=Array.AsReadOnly(extraInputs);
            SourceId=source.ID; StationId=station.ID; actorId=actor.ID; stationBlueprint=station.BlueprintName;
            pack=actor.GetPart<InventoryPart>(); physics=source.GetPart<PhysicsPart>(); stack=source.GetPart<StackerPart>();
            permission=PermissionPart(source,definition); authorization=Authorization(permission);
            count=stack?.StackCount??1; maximumStack=stack?.MaxStack??1; weight=physics.Weight;
            sourceParts=source.Parts.ToArray(); stationParts=station.Parts.ToArray();
            actorCell=zone.GetEntityCell(actor); stationCell=zone.GetEntityCell(station);
        }
        public static MaterialProcessPlan TryCreate(Entity actor, Entity source, string id, Zone zone, EntityFactory factory)
        {
            var definition=MaterialProcessRegistry.Get(id);
            if(definition==null || factory==null || ForgePart.Factory!=factory
                || MaterialProcessRegistry.Validate(factory).Count!=0 || !PreparationActions.CurrentActor(actor,zone)
                || !PreparationActions.Carried(actor,source) || !Permission(source,definition)
                || (source.GetPart<StackerPart>()?.StackCount??1)<definition.InputCount)return null;
            var cutting=definition.WorkKind=="HandCutting"?MaterialCuttingWorkReceipt.Resolve(actor,source,zone):null;
            var station=definition.WorkKind=="HandCutting"?cutting?.Tool:ForgeWorkReceipt.Resolve(actor,zone);
            if(station==null || cutting==null && !VisibleStation(actor,station,zone))return null;
            var extraInputs=new MaterialProcessIngredient[definition.AdditionalInputBlueprints.Count];
            for(int i=0;i<extraInputs.Length;i++)
            {
                extraInputs[i]=MaterialProcessIngredient.Resolve(actor,source,station,definition.AdditionalInputBlueprints[i],definition.AdditionalInputCounts[i],factory);
                if(extraInputs[i]==null)return null;
            }
            var owners=new[]{actor,source,station}.Concat(extraInputs.SelectMany(i=>i.Takes.Select(t=>t.Owner))).ToArray();
            if(owners.Distinct().Count()!=owners.Length || owners.Select(e=>e.ID).Distinct(StringComparer.Ordinal).Count()!=owners.Length)return null;
            var plan=new MaterialProcessPlan(actor,source,station,zone,factory,definition,extraInputs,cutting);
            return plan.Current()?plan:null;
        }
        static Part PermissionPart(Entity source,MaterialProcessDefinition definition)
        {
            if(source==null)return null;
            if(definition.SourceRecipeId.Length==0)
                return source.GetPart<MaterialProcessInputPart>() is MaterialProcessInputPart explicitPart && explicitPart.Allows(definition)
                    && source.Parts.Count(p=>p is MaterialProcessInputPart)==1 && explicitPart.ParentEntity==source?explicitPart:null;
            if(source.HasPart<MaterialProcessInputPart>() || source.Parts.Count(p=>p is PreparationRecipePart)!=1)return null;
            var recipe=PreparationRecipeService.Recipes.FirstOrDefault(r=>r.Id==definition.SourceRecipeId);
            var part=source.GetPart<PreparationRecipePart>();
            return recipe!=null && recipe.Input==definition.SourceBlueprint && part.ParentEntity==source && part.Allows(recipe)?part:null;
        }
        static string Authorization(Part part)=>part is MaterialProcessInputPart p?p.ProcessIds:(part as PreparationRecipePart)?.RecipeIds;
        static bool Permission(Entity source,MaterialProcessDefinition definition)=>PermissionPart(source,definition)!=null;
        static bool VisibleStation(Entity actor,Entity station,Zone zone)
            => ForgeWorkReceipt.Near(actor,station,zone) && zone.GetEntityCell(station) is Cell cell && cell.IsVisible && cell.Explored;
        internal bool ContextCurrent()
            => ForgePart.Factory==Factory && MaterialProcessRegistry.Get(Definition.Id)==Definition
                && MaterialProcessRegistry.Validate(Factory).Count==0
                && PreparationActions.CurrentActor(Actor,Zone) && Actor.ID==actorId && Actor.GetPart<InventoryPart>()==pack
                && !CombatSystem.IsDeathHandled(Actor) && (Actor.GetStat("Hitpoints")==null || Actor.GetStatValue("Hitpoints")>0)
                && Zone.GetEntityCell(Actor)==actorCell && Zone.GetEntityCell(Station)==stationCell
                && Station.ID==StationId && Station.BlueprintName==stationBlueprint && Station.Parts.SequenceEqual(stationParts)
                && (cutting!=null?cutting.Current():VisibleStation(Actor,Station,Zone));
        internal bool BeginWork(CavesOfOoo.Core.Inventory.InventoryTransaction tx,string command)
            => cutting!=null?cutting.Begin(tx,command):ForgeWorkReceipt.Begin(Actor,Zone,tx,command,Station);
        public bool Current()
            => ContextCurrent() && Source.ID==SourceId && Source.BlueprintName==Definition.SourceBlueprint
                && Source.Parts.SequenceEqual(sourceParts) && Source.GetPart<PhysicsPart>()==physics
                && physics.Weight==weight && Source.GetPart<StackerPart>()==stack
                && (stack?.StackCount??1)==count && (stack?.MaxStack??1)==maximumStack
                && PermissionPart(Source,Definition)==permission && Authorization(permission)==authorization
                && ExtraInputs.All(i=>i.Before(Actor))
                && PreparationActions.Carried(Actor,Source);
    }
    /// <summary>
    /// One extra input of a process: usually one carried stack, or, when no single stack holds enough units, several template-exact stacks of the same blueprint that
    /// the ordinary stack-merge rule admits (spec S12b, Docs/MACHINE-MULTI-STACK-CLAIMS.md). Each <see cref="Take"/> is guarded, checked and consumed on its own.
    /// </summary>
    internal sealed class MaterialProcessIngredient
    {
        internal readonly struct Take
        {
            internal readonly Entity Owner; internal readonly int Units, OriginalCount; internal readonly MaterialProcessItemState State;
            internal Take(Entity owner,int units,MaterialProcessItemState state){Owner=owner;Units=units;OriginalCount=Count(owner);State=state;}
        }
        internal readonly Take[] Takes;
        /// <summary>The first take's owner: the whole ingredient in the common single-stack case.</summary>
        internal Entity Owner=>Takes[0].Owner;
        internal int Units=>Takes.Sum(t=>t.Units);
        readonly MaterialProcessTemplate template;
        MaterialProcessIngredient(Take[] takes,MaterialProcessTemplate template){Takes=takes;this.template=template;}
        static int Count(Entity e)=>e.GetPart<StackerPart>()?.StackCount??1;
        internal static MaterialProcessIngredient Resolve(Entity actor,Entity source,Entity tool,string blueprint,int units,EntityFactory factory)
        {
            var template=MaterialProcessTemplate.Capture(factory,blueprint);if(template==null)return null;
            var choices=actor.GetPart<InventoryPart>().Objects.Where(e=>e?.BlueprintName==blueprint && e!=source && e!=tool
                && !string.IsNullOrEmpty(e.ID) && Count(e)>=units && PreparationActions.Carried(actor,e))
                .Select(e=>new {Owner=e,State=template.Snapshot(e,true),Plain=template.Snapshot(e)!=null}).Where(e=>e.State!=null).ToArray();
            // Legacy first and unchanged: any single sufficient stack keeps its exact choice, and the deliberate ambiguity refusal (several sufficient stacks with a non-plain one) stays a refusal.
            if(choices.Length>1 && choices.Any(e=>!e.Plain))return null;
            if(choices.Length>0)
            {
                var chosen=choices.OrderBy(e=>e.Owner.ID,StringComparer.Ordinal).First();
                return new MaterialProcessIngredient(new[]{new Take(chosen.Owner,units,chosen.State)},template);
            }
            return Pool(actor,source,tool,blueprint,units,template);
        }
        /// <summary>
        /// Pools only where no single stack was sufficient: carried, non-equipped, plain template-exact stacks of the one blueprint that all merge with the first by the
        /// ordinary stack rule (so no state is laundered), smallest stack first then ID ordinal, spending whole small stacks before touching a larger one.
        /// </summary>
        static MaterialProcessIngredient Pool(Entity actor,Entity source,Entity tool,string blueprint,int units,MaterialProcessTemplate template)
        {
            var siblings=actor.GetPart<InventoryPart>().Objects.Where(e=>e?.BlueprintName==blueprint && e!=source && e!=tool && e!=actor
                && !string.IsNullOrEmpty(e.ID) && Count(e)>=1 && PreparationActions.Carried(actor,e) && !MaterialProcessService.IsProtected(e))
                .Select(e=>new {Owner=e,State=template.Snapshot(e,true),Plain=template.Snapshot(e)!=null}).Where(e=>e.State!=null && e.Plain)
                .OrderBy(e=>Count(e.Owner)).ThenBy(e=>e.Owner.ID,StringComparer.Ordinal).ToArray();
            if(siblings.Length<2 || siblings.Sum(e=>Count(e.Owner))<units)return null;
            var anchor=siblings[0].Owner;
            if(siblings.Skip(1).Any(e=>!anchor.GetPart<StackerPart>().CanStackWith(e.Owner)))return null;
            var takes=new System.Collections.Generic.List<Take>();int remaining=units;
            foreach(var e in siblings)
            {
                if(remaining<=0)break;
                int take=Math.Min(remaining,Count(e.Owner));takes.Add(new Take(e.Owner,take,e.State));remaining-=take;
            }
            return remaining==0?new MaterialProcessIngredient(takes.ToArray(),template):null;
        }
        internal bool Before(Entity actor)=>template.Current() && Takes.All(t=>t.State.Matches() && Count(t.Owner)==t.OriginalCount && PreparationActions.Carried(actor,t.Owner));
        internal bool After(Entity actor)
        {
            if(!template.Current())return false;
            foreach(var t in Takes)
            {
                if(!t.State.Matches() || Count(t.Owner)!=Math.Max(1,t.OriginalCount-t.Units))return false;
                if(t.OriginalCount>t.Units){if(!PreparationActions.Carried(actor,t.Owner))return false;continue;}
                var p=t.Owner.GetPart<PhysicsPart>();
                if(!(t.Owner.SpatialZone==null && p.InInventory==null && p.Equipped==null && !actor.GetPart<InventoryPart>().Objects.Contains(t.Owner)))return false;
            }
            return true;
        }
    }
}
