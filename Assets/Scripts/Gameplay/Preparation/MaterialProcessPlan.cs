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
        internal readonly MaterialProcessStock Stock;
        internal readonly Zone Zone;
        internal readonly EntityFactory Factory;
        internal string StationKind => KindFor(Definition);
        readonly InventoryPart pack;
        readonly PhysicsPart physics;
        readonly StackerPart stack;
        readonly Part permission;
        internal readonly System.Collections.Generic.IReadOnlyList<MaterialProcessIngredient> ExtraInputs;
        readonly string actorId, authorization, stationBlueprint;
        readonly Part[] sourceParts, stationParts;
        readonly Cell actorCell, stationCell;
        readonly int count, weight, maximumStack;
        readonly IMaterialWorkReceipt work;
        readonly SiftableStockPart sizeStock;
        readonly string sizeManifest;

        MaterialProcessPlan(Entity actor, Entity source, Entity station, Zone zone,
            EntityFactory factory, MaterialProcessDefinition definition, MaterialProcessIngredient[] extraInputs, IMaterialWorkReceipt work=null,MaterialProcessStock stock=null,SiftableStockPart sizeStock=null)
        {
            Actor=actor; Source=source; Station=station; Zone=zone; Factory=factory; Definition=definition;
            this.work=work;Stock=stock;
            this.sizeStock=sizeStock;sizeManifest=sizeStock?.Contents;
            ExtraInputs=Array.AsReadOnly(extraInputs);
            SourceId=source.ID; StationId=station.ID; actorId=actor.ID; stationBlueprint=station.BlueprintName;
            pack=actor.GetPart<InventoryPart>(); physics=source.GetPart<PhysicsPart>(); stack=source.GetPart<StackerPart>();
            permission=PermissionPart(source,definition); authorization=Authorization(permission);
            count=stack?.StackCount??1; maximumStack=stack?.MaxStack??1; weight=physics.Weight;
            sourceParts=source.Parts.ToArray(); stationParts=station.Parts.ToArray();
            actorCell=zone.GetEntityCell(actor); stationCell=zone.GetEntityCell(station);
        }
        public static MaterialProcessPlan TryCreate(Entity actor, Entity source, string id, Zone zone, EntityFactory factory)
            => Create(actor,source,id,zone,factory,null);
        internal static MaterialProcessPlan TryCreateFromStock(Entity actor,Entity source,string id,Zone zone,EntityFactory factory,MaterialProcessStock stock)
            => stock==null?null:Create(actor,source,id,zone,factory,stock);
        internal static MaterialProcessPlan TryCreateScreening(Entity actor,Entity source,Zone zone,EntityFactory factory,
            SiftableStockPart provider, MaterialProcessStock stock = null)
            => provider?.ParentEntity!=source || source?.GetPart<SiftableStockPart>()!=provider?null:
                Create(actor,source,SiftableStockPart.ProcessId,zone,factory,stock,provider);
        static MaterialProcessPlan Create(Entity actor,Entity source,string id,Zone zone,EntityFactory factory,MaterialProcessStock stock,SiftableStockPart sizeStock=null)
        {
            var definition=sizeStock==null?MaterialProcessRegistry.Get(id):sizeStock.Definition(factory);
            if(definition==null || factory==null || ForgePart.Factory!=factory
                || MaterialProcessRegistry.ValidateForWork(factory,definition.WorkKind).Count!=0 || !PreparationActions.CurrentActor(actor,zone)
                || !(stock==null?PreparationActions.Carried(actor,source):stock.Contains(source)) || !Permission(source,definition)
                || (source.GetPart<StackerPart>()?.StackCount??1)<definition.InputCount)return null;
            IMaterialWorkReceipt work=definition.WorkKind switch
            {
                "HandCutting"=>MaterialCuttingWorkReceipt.Resolve(actor,source,zone),
                "Screening"=>MaterialScreeningWorkReceipt.Resolve(actor,source,zone),
                _=>null
            };
            string kind=KindFor(definition);
            var station=kind==null?work?.Tool:StationQuery.Resolve(actor,zone,kind);
            if(station==null || work==null && !VisibleStation(actor,station,zone,kind))return null;
            var extraInputs=new MaterialProcessIngredient[definition.AdditionalInputBlueprints.Count];
            for(int i=0;i<extraInputs.Length;i++)
            {
                extraInputs[i]=MaterialProcessIngredient.Resolve(actor,source,station,definition.AdditionalInputBlueprints[i],definition.AdditionalInputCounts[i],factory,stock);
                if(extraInputs[i]==null)return null;
            }
            var owners=new[]{actor,source,station}.Concat(extraInputs.SelectMany(i=>i.Takes.Select(t=>t.Owner)))
                .Concat(stock==null?Array.Empty<Entity>():new[]{stock.Box}).ToArray();
            if(owners.Distinct().Count()!=owners.Length || owners.Select(e=>e.ID).Distinct(StringComparer.Ordinal).Count()!=owners.Length)return null;
            var plan=new MaterialProcessPlan(actor,source,station,zone,factory,definition,extraInputs,work,stock,sizeStock);
            return plan.Current()?plan:null;
        }
        internal static Part PermissionPart(Entity source,MaterialProcessDefinition definition)
        {
            if(source==null)return null;
            if(definition.SourceRecipeId.Length==0)
            {
                // An explicit permission, including a denial, always wins.
                if(source.HasPart<MaterialProcessInputPart>())
                    return source.GetPart<MaterialProcessInputPart>() is MaterialProcessInputPart explicitPart && explicitPart.Allows(definition)
                        && source.Parts.Count(p=>p is MaterialProcessInputPart)==1 && explicitPart.ParentEntity==source?explicitPart:null;
                var adapters=source.Parts.Where(p=>p is IMaterialProcessPermission).ToArray();
                return adapters.Length==1 && adapters[0].ParentEntity==source
                    && ((IMaterialProcessPermission)adapters[0]).Allows(definition)?adapters[0]:null;
            }
            if(source.HasPart<MaterialProcessInputPart>() || source.Parts.Count(p=>p is PreparationRecipePart)!=1)return null;
            var recipe=PreparationRecipeService.Recipes.FirstOrDefault(r=>r.Id==definition.SourceRecipeId);
            var part=source.GetPart<PreparationRecipePart>();
            return recipe!=null && recipe.Input==definition.SourceBlueprint && part.ParentEntity==source && part.Allows(recipe)?part:null;
        }
        static string Authorization(Part part)=>part is MaterialProcessInputPart p?p.ProcessIds
            :part is IMaterialProcessPermission?part.Name:(part as PreparationRecipePart)?.RecipeIds;
        static bool Permission(Entity source,MaterialProcessDefinition definition)=>PermissionPart(source,definition)!=null;
        static string KindFor(MaterialProcessDefinition definition) => definition.WorkKind switch
        {
            "ForgeCold" => ForgeWorkReceipt.Kind,
            SeedFramePart.WorkKind => SeedFramePart.Kind,
            _ => null
        };
        static bool VisibleStation(Entity actor,Entity station,Zone zone,string kind)
            => StationQuery.Near(actor,station,zone,kind) && zone.GetEntityCell(station) is Cell cell && cell.IsVisible && cell.Explored;
        internal bool ContextCurrent()
            => ForgePart.Factory==Factory && (sizeStock==null?MaterialProcessRegistry.Get(Definition.Id)==Definition
                :sizeStock.ParentEntity==Source && Source.GetPart<SiftableStockPart>()==sizeStock && sizeStock.Contents==sizeManifest
                    && PermissionPart(Source,Definition)==sizeStock)
                && MaterialProcessRegistry.ValidateForWork(Factory,Definition.WorkKind).Count==0
                && PreparationActions.CurrentActor(Actor,Zone) && Actor.ID==actorId && Actor.GetPart<InventoryPart>()==pack
                && (Stock==null || Stock.Current())
                && !CombatSystem.IsDeathHandled(Actor) && (Actor.GetStat("Hitpoints")==null || Actor.GetStatValue("Hitpoints")>0)
                && Zone.GetEntityCell(Actor)==actorCell && Zone.GetEntityCell(Station)==stationCell
                && Station.ID==StationId && Station.BlueprintName==stationBlueprint && Station.Parts.SequenceEqual(stationParts)
                && (work!=null?work.Current():VisibleStation(Actor,Station,Zone,StationKind));
        internal bool SameDefinition(MaterialProcessPlan other)
            => other!=null && (sizeStock==null?other.sizeStock==null && Definition==other.Definition
                :other.sizeStock==sizeStock && other.sizeManifest==sizeManifest && Definition.Id==other.Definition.Id
                    && Definition.SourceBlueprint==other.Definition.SourceBlueprint && Definition.Mode==other.Definition.Mode
                    && Definition.WorkKind==other.Definition.WorkKind && Definition.InputCount==other.Definition.InputCount
                    && Definition.OutputBlueprints.SequenceEqual(other.Definition.OutputBlueprints)
                    && Definition.OutputCounts.SequenceEqual(other.Definition.OutputCounts));
        internal bool BeginWork(CavesOfOoo.Core.Inventory.InventoryTransaction tx,string command)
            => work!=null?work.Begin(tx,command):StationReceipt.Begin(Actor,Zone,tx,command,StationKind,Station)!=null;
        internal bool InputAvailable(Entity input)=>Stock==null?PreparationActions.Carried(Actor,input):Stock.Contains(input);
        internal bool BeginInput(Entity input,CavesOfOoo.Core.Inventory.InventoryTransaction tx,string command)
            => Stock==null?PreparationActions.Begin(Actor,input,Zone,tx,command):Stock.Begin(input,tx,command);
        internal bool IndividuallyPaidClay(Entity input)
            => Stock==null && input!=Source && input.BlueprintName=="FireClay" && input.HasPart<ConnectedClayOriginPart>();
        internal int StoredScreeningWeight => Stock != null && sizeStock != null
            ? SiftableStockPart.Weight(Factory, Definition.SourceBlueprint) : 0;
        internal bool RoomForStoredScreening()
            => Stock == null || sizeStock == null || pack.MaxWeight < 0
                || (long)pack.GetCarriedWeight() + StoredScreeningWeight <= pack.MaxWeight;
        internal bool Consume(Entity input,CavesOfOoo.Core.Inventory.InventoryTransaction tx,Func<bool> refundAllowed=null)
        {
            if (Stock != null) return Stock.Consume(input,tx,input!=Source && input.BlueprintName=="FireClay",refundAllowed);
            if (!IndividuallyPaidClay(input)) return pack.TryConsumeMaterialProcessUnit(input,tx);
            var state=MaterialProcessTemplate.Capture(Factory,input.BlueprintName)?.Snapshot(input,true,allowClayStamp:true);
            return state!=null && MaterialProcessPayment.Spend(Actor,input,Actor,Zone,tx,state.Matches,()=>state.MatchesAfterPayment(1));
        }
        public bool Current()
            => ContextCurrent() && Source.ID==SourceId && Source.BlueprintName==Definition.SourceBlueprint
                && Source.Parts.SequenceEqual(sourceParts) && Source.GetPart<PhysicsPart>()==physics
                && physics.Weight==weight && Source.GetPart<StackerPart>()==stack
                && (stack?.StackCount??1)==count && (stack?.MaxStack??1)==maximumStack
                && PermissionPart(Source,Definition)==permission && Authorization(permission)==authorization
                && ExtraInputs.All(i=>i.Before(Actor))
                && InputAvailable(Source);
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
        readonly MaterialProcessTemplate template;readonly MaterialProcessStock stock;
        MaterialProcessIngredient(Take[] takes,MaterialProcessTemplate template,MaterialProcessStock stock=null){Takes=takes;this.template=template;this.stock=stock;}
        bool Available(Entity actor,Entity item)=>stock==null?PreparationActions.Carried(actor,item):stock.Contains(item);
        static int Count(Entity e)=>e.GetPart<StackerPart>()?.StackCount??1;
        internal static MaterialProcessIngredient Resolve(Entity actor,Entity source,Entity tool,string blueprint,int units,EntityFactory factory,MaterialProcessStock stock=null)
        {
            var template=MaterialProcessTemplate.Capture(factory,blueprint);if(template==null)return null;
            bool allowClayStamp=blueprint=="FireClay";
            var choices=(stock?.Container.Contents??actor.GetPart<InventoryPart>().Objects).Where(e=>e?.BlueprintName==blueprint && e!=source && e!=tool
                && !string.IsNullOrEmpty(e.ID) && Count(e)>=units && (stock==null?PreparationActions.Carried(actor,e):stock.Contains(e)))
                .Select(e=>new {Owner=e,State=template.Snapshot(e,true,allowClayStamp),Plain=template.Snapshot(e,allowClayStamp:allowClayStamp)!=null}).Where(e=>e.State!=null).ToArray();
            // Legacy first and unchanged: any single sufficient stack keeps its exact choice, and the deliberate ambiguity refusal (several sufficient stacks with a non-plain one) stays a refusal.
            if(choices.Length>1 && choices.Any(e=>!e.Plain))return null;
            if(choices.Length>0)
            {
                var chosen=choices.OrderBy(e=>e.Owner.ID,StringComparer.Ordinal).First();
                return new MaterialProcessIngredient(new[]{new Take(chosen.Owner,units,chosen.State)},template,stock);
            }
            return Pool(actor,source,tool,blueprint,units,template,stock);
        }
        /// <summary>
        /// Pools only where no single stack was sufficient: carried, non-equipped, plain template-exact stacks of the one blueprint that all merge with the first by the
        /// ordinary stack rule (so no state is laundered), smallest stack first then ID ordinal, spending whole small stacks before touching a larger one.
        /// </summary>
        static MaterialProcessIngredient Pool(Entity actor,Entity source,Entity tool,string blueprint,int units,MaterialProcessTemplate template,MaterialProcessStock stock=null)
        {
            bool allowClayStamp=blueprint=="FireClay";
            var siblings=(stock?.Container.Contents??actor.GetPart<InventoryPart>().Objects).Where(e=>e?.BlueprintName==blueprint && e!=source && e!=tool && e!=actor
                && !string.IsNullOrEmpty(e.ID) && Count(e)>=1 && (stock==null?PreparationActions.Carried(actor,e):stock.Contains(e)) && !MaterialProcessService.IsProtected(e))
                .Select(e=>new {Owner=e,State=template.Snapshot(e,true,allowClayStamp),Plain=template.Snapshot(e,allowClayStamp:allowClayStamp)!=null}).Where(e=>e.State!=null && e.Plain)
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
            if(allowClayStamp)
                for(int i=0;i<takes.Count;i++)for(int j=i+1;j<takes.Count;j++)
                    if(!ConnectedClayProvenance.CanMerge(takes[i].Owner,takes[j].Owner))return null;
            return remaining==0?new MaterialProcessIngredient(takes.ToArray(),template,stock):null;
        }
        internal bool Before(Entity actor)=>template.Current() && Takes.All(t=>t.State.Matches() && Count(t.Owner)==t.OriginalCount && Available(actor,t.Owner));
        internal bool After(Entity actor)
        {
            if(!template.Current())return false;
            foreach(var t in Takes)
            {
                if(!t.State.MatchesAfterPayment(t.Units) || Count(t.Owner)!=Math.Max(1,t.OriginalCount-t.Units))return false;
                if(t.OriginalCount>t.Units){if(!Available(actor,t.Owner))return false;continue;}
                var p=t.Owner.GetPart<PhysicsPart>();
                if(!(t.Owner.SpatialZone==null && p.InInventory==null && p.Equipped==null && !actor.GetPart<InventoryPart>().Objects.Contains(t.Owner)))return false;
            }
            return true;
        }
    }
}
