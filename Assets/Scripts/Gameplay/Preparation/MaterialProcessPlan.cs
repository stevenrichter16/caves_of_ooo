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
        readonly MaterialProcessInputPart permission;
        readonly string actorId, authorization, stationBlueprint;
        readonly Part[] sourceParts, stationParts;
        readonly Cell actorCell, stationCell;
        readonly int count, weight, maximumStack;

        MaterialProcessPlan(Entity actor, Entity source, Entity station, Zone zone,
            EntityFactory factory, MaterialProcessDefinition definition)
        {
            Actor=actor; Source=source; Station=station; Zone=zone; Factory=factory; Definition=definition;
            SourceId=source.ID; StationId=station.ID; actorId=actor.ID; stationBlueprint=station.BlueprintName;
            pack=actor.GetPart<InventoryPart>(); physics=source.GetPart<PhysicsPart>(); stack=source.GetPart<StackerPart>();
            permission=source.GetPart<MaterialProcessInputPart>(); authorization=permission.ProcessIds;
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
            var station=ForgeWorkReceipt.Resolve(actor,zone);
            if(!VisibleStation(actor,station,zone))return null;
            var plan=new MaterialProcessPlan(actor,source,station,zone,factory,definition);
            return plan.Current()?plan:null;
        }
        static bool Permission(Entity source,MaterialProcessDefinition definition)
            => source?.GetPart<MaterialProcessInputPart>() is MaterialProcessInputPart part && part.Allows(definition)
                && source.Parts.Count(p=>p is MaterialProcessInputPart)==1 && part.ParentEntity==source;
        static bool VisibleStation(Entity actor,Entity station,Zone zone)
            => ForgeWorkReceipt.Near(actor,station,zone) && zone.GetEntityCell(station) is Cell cell && cell.IsVisible && cell.Explored;
        internal bool ContextCurrent()
            => ForgePart.Factory==Factory && MaterialProcessRegistry.Get(Definition.Id)==Definition
                && MaterialProcessRegistry.Validate(Factory).Count==0
                && PreparationActions.CurrentActor(Actor,Zone) && Actor.ID==actorId && Actor.GetPart<InventoryPart>()==pack
                && !CombatSystem.IsDeathHandled(Actor) && (Actor.GetStat("Hitpoints")==null || Actor.GetStatValue("Hitpoints")>0)
                && Zone.GetEntityCell(Actor)==actorCell && Zone.GetEntityCell(Station)==stationCell
                && Station.ID==StationId && Station.BlueprintName==stationBlueprint && Station.Parts.SequenceEqual(stationParts) && VisibleStation(Actor,Station,Zone);
        public bool Current()
            => ContextCurrent() && Source.ID==SourceId && Source.BlueprintName==Definition.SourceBlueprint
                && Source.Parts.SequenceEqual(sourceParts) && Source.GetPart<PhysicsPart>()==physics
                && physics.Weight==weight && Source.GetPart<StackerPart>()==stack
                && (stack?.StackCount??1)==count && (stack?.MaxStack??1)==maximumStack
                && permission.ProcessIds==authorization && Permission(Source,Definition)
                && PreparationActions.Carried(Actor,Source);
    }
}
