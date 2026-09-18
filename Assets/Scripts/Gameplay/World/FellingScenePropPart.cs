using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A real scene owner. Clearing removes only this component and
    /// its collision; permanent terrain and lore positions cannot be cleared.</summary>
    public sealed class FellingScenePropPart : Part
    {
        public override string Name=>"FellingSceneProp";
        public const string ClearCommand="ClearFellingProp";
        public string ComponentId="";
        public bool Mutable;
        public string ClearLabel="clear growth";
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID=="GetInventoryActions"&&Mutable)
                e.GetParameter<InventoryActionList>("Actions")?.AddAction("Clear",ClearLabel,ClearCommand,'r',15);
            else if(e.ID=="InventoryAction"&&e.GetStringParameter("Command")==ClearCommand)
            {TryClear(e.GetParameter<Entity>("Actor"),e.GetParameter<Zone>("Zone"));e.Handled=true;return false;}
            return true;
        }
        /// <summary>Atomic, reward-free removal. The input command owns turn
        /// consumption; direct callers get a success flag and no clock mutation.</summary>
        public bool TryClear(Entity actor,Zone zone)
        {
            string reason=null;
            var authored=FellingSceneDefinition.Load()?.FindLayer(ComponentId);
            if(!Mutable||authored==null||!authored.mutable)reason="Fixed";
            else if(actor==null||!actor.HasTag("Player"))reason="NotPlayer";
            else if(actor.GetStatValue("Hitpoints",0)<=0)reason="Dead";
            else if(!FellingSceneRuntime.IsActive(zone))reason="WrongZone";
            else if(ParentEntity==null||FellingSceneRuntime.FindOwner(zone,ComponentId)!=ParentEntity)reason="StaleOwner";
            else if(zone.GetEntityPosition(ParentEntity)!=(authored.anchorX,authored.anchorY))reason="DisplacedOwner";
            else if(zone.GetEntityCell(actor)==null||!zone.GetEntityCell(actor).Objects.Contains(actor))reason="DetachedActor";
            else if(!DestructionSystem.IsWithinStrikeReach(actor,ParentEntity,zone))reason="OutOfReach";
            if(reason!=null)
            {
                if(Diag.IsChannelEnabled("furniture"))Diag.Record("furniture","FellingClearRejected",actor,ParentEntity,new{component=ComponentId,reason});
                if(actor!=null&&actor.HasTag("Player"))MessageLog.Add(reason=="OutOfReach"?"That is out of reach.":"That part of the site cannot be cleared now.");
                return false;
            }
            var cell=zone.GetEntityCell(ParentEntity);var state=FellingSceneRuntime.GetState(zone);
            if(cell==null||state==null||!zone.RemoveEntity(ParentEntity))return false;
            state.RecordRemoval(ComponentId);
            ZoneRenderHooks.MarkCellDirty(cell.X,cell.Y,"FellingClear");
            MessageLog.Add("You clear "+ParentEntity.GetDisplayName()+", exposing the ground beneath.");
            if(Diag.IsChannelEnabled("furniture"))Diag.Record("furniture","FellingCleared",actor,ParentEntity,new{component=ComponentId,x=cell.X,y=cell.Y});
            return true;
        }
    }
}
