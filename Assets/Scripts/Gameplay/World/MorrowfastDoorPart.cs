namespace CavesOfOoo.Core
{
    public sealed class MorrowfastDoorPart : Part
    {
        public override string Name=>"MorrowfastDoor";
        public const string OpenCommand="OpenMorrowfastDoor",CloseCommand="CloseMorrowfastDoor";
        public string ComponentId="";
        public bool IsOpen;
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID=="GetInventoryActions")e.GetParameter<InventoryActionList>("Actions")?.AddAction("Door",IsOpen?"close door":"open door",IsOpen?CloseCommand:OpenCommand,'o',30);
            else if(e.ID=="InventoryAction")
            {var c=e.GetStringParameter("Command");if(c==OpenCommand||c==CloseCommand){TrySetOpen(e.GetParameter<Entity>("Actor"),e.GetParameter<Zone>("Zone"),c==OpenCommand);e.Handled=true;return false;}}
            return true;
        }
        public bool TrySetOpen(Entity actor,Zone zone,bool open)
        {
            var o=MorrowfastSceneDefinition.Load()?.FindOwner(ComponentId);
            if(o?.kind!="door"||!MorrowfastSceneRuntime.IsValidActor(actor,zone)||MorrowfastSceneRuntime.FindOwner(zone,ComponentId)!=ParentEntity||!MorrowfastSceneRuntime.WithinOwnerReach(actor,ParentEntity,zone)||zone.GetEntityPosition(ParentEntity)!=(o.anchorX,o.anchorY)||MorrowfastSceneRuntime.IsDoorOpen(zone,ComponentId)==open)return false;
            if(!open)foreach(var p in o.footprint)foreach(var e in zone.GetCell(p.x,p.y).Objects)
                if(e!=ParentEntity&&!e.HasTag(MorrowfastSceneRuntime.TerrainTag)){MessageLog.Add("The doorway is occupied.");return false;}
            IsOpen=open;MorrowfastSceneRuntime.GetState(zone).SetDoor(ComponentId,open);
            ParentEntity.GetPart<RenderPart>().RenderString=open?"/":"+";
            MorrowfastSceneRuntime.MarkOwnerDirty(zone,ComponentId);
            // A stationary door action changes line of sight beyond its footprint.
            ZoneRenderHooks.MarkFullDirty("MorrowfastDoor");MessageLog.Add(open?"You open the door.":"You close the door.");return true;
        }
    }
}
