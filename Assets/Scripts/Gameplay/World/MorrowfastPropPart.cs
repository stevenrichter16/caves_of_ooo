namespace CavesOfOoo.Core
{
    public sealed class MorrowfastPropPart : Part
    {
        public override string Name=>"MorrowfastProp";
        public const string ClearCommand="ClearMorrowfastProp",RoofCommand="ToggleMorrowfastRoof";
        public string ComponentId="";
        private MorrowfastSceneDefinition.OwnerSpec Spec=>MorrowfastSceneDefinition.Load()?.FindOwner(ComponentId);
        private bool Clearable=>Spec!=null&&Spec.mutable&&Spec.kind!="npc"&&Spec.kind!="creature"&&Spec.kind!="door"&&Spec.kind!="roof"&&Spec.kind!="building-shell";
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID=="GetInventoryActions")
            {
                var a=e.GetParameter<InventoryActionList>("Actions");
                if(Clearable)a?.AddAction("Clear","remove component",ClearCommand,'r',15);
                if(Spec?.kind=="roof")a?.AddAction("Roof","lift / lower roof",RoofCommand,'r',15);
            }
            else if(e.ID=="InventoryAction")
            {
                string cmd=e.GetStringParameter("Command");var actor=e.GetParameter<Entity>("Actor");var zone=e.GetParameter<Zone>("Zone");
                if(cmd==ClearCommand){TryRemove(actor,zone);e.Handled=true;return false;}
                if(cmd==RoofCommand){TrySetRoofLifted(actor,zone,!MorrowfastSceneRuntime.IsRoofLifted(zone,ComponentId));e.Handled=true;return false;}
            }
            return true;
        }
        private bool Authorized(Entity actor,Zone zone)=>MorrowfastSceneRuntime.IsValidActor(actor,zone)&&ParentEntity!=null&&MorrowfastSceneRuntime.FindOwner(zone,ComponentId)==ParentEntity&&MorrowfastSceneRuntime.WithinOwnerReach(actor,ParentEntity,zone);
        public bool TryRemove(Entity actor,Zone zone)
        {
            if(!Clearable||!Authorized(actor,zone))return false;
            var o=Spec;var at=zone.GetEntityPosition(ParentEntity);
            if(at!=(o.anchorX,o.anchorY)&&o.kind!="stool")return false;
            if(ParentEntity.GetPart<ContainerPart>()?.Contents.Count>0){MessageLog.Add("Empty the container first.");return false;}
            if(o.kind=="bridge")foreach(var p in o.bridgeSupport)foreach(var e in zone.GetCell(p.x,p.y).Objects)
                if(e!=ParentEntity&&!e.HasTag(MorrowfastSceneRuntime.TerrainTag)){MessageLog.Add("The crossing must be clear first.");return false;}
            if(!zone.RemoveEntity(ParentEntity))return false;
            MorrowfastSceneRuntime.GetState(zone).RemoveOwner(ComponentId);MorrowfastSceneRuntime.MarkOwnerDirty(zone,ComponentId);
            ZoneRenderHooks.MarkCellDirty(at.x,at.y,"MorrowfastClear");MessageLog.Add("You remove "+ParentEntity.GetDisplayName()+", revealing the surface beneath.");return true;
        }
        public bool TrySetRoofLifted(Entity actor,Zone zone,bool lifted)
        {
            if(Spec?.kind!="roof"||!Authorized(actor,zone)||MorrowfastSceneRuntime.IsRoofLifted(zone,ComponentId)==lifted)return false;
            var room=Spec.roomId;MorrowfastSceneDefinition.BuildingSpec building=null;
            foreach(var b in MorrowfastSceneDefinition.Load().buildings)if(b.id==room)building=b;
            if(lifted&&building!=null&&!MorrowfastSceneRuntime.IsDoorOpen(zone,building.doorId)&&MorrowfastSceneRuntime.GetRoomAt(zone,zone.GetEntityPosition(actor).x,zone.GetEntityPosition(actor).y)!=room)return false;
            MorrowfastSceneRuntime.GetState(zone).SetRoof(ComponentId,lifted);MorrowfastSceneRuntime.MarkOwnerDirty(zone,ComponentId);return true;
        }
    }
}
