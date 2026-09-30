using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>An ordinary, saved village door. Scene-owned Morrowfast and
    /// archive barriers retain their separate authorities. No turn is charged here;
    /// successful callers must pay one normal action.</summary>
    public sealed class DoorPart : Part
    {
        public override string Name => "Door";
        public const string OpenCommand="OpenDoor", CloseCommand="CloseDoor";
        public bool IsOpen=true;
        public string OwnerId="";
        /// <summary>Saved quarter-turns around vertical, assigned from the actual generated aperture axis.</summary>
        public int QuarterTurns;
        public bool IsClosed => !IsOpen || ParentEntity?.GetPart<LockPart>()?.IsLocked==true;
        public override void Initialize()=>RefreshGlyph();
        public override void OnAfterLoad(SaveReader reader)=>RefreshGlyph();
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID=="GetInventoryActions")
                e.GetParameter<InventoryActionList>("Actions")?.AddAction("Door",IsClosed?"open door":"close door",IsClosed?OpenCommand:CloseCommand,'o',30);
            else if(e.ID=="InventoryAction")
            {
                string command=e.GetStringParameter("Command");
                if(command!=OpenCommand&&command!=CloseCommand)return true;
                if(TrySetOpen(e.GetParameter<Entity>("Actor"),e.GetParameter<Zone>("Zone"),command==OpenCommand))
                {e.Handled=true;return false;}
            }
            return true;
        }
        /// <summary>Read-only path permission. No key use, movement, action,
        /// diagnostic, or state mutation. Reach is checked only at execution.</summary>
        public bool CanOperate(Entity actor,Zone zone)=>HasAuthority(actor,zone)&&ParentEntity.GetPart<LockPart>()?.IsLocked!=true;
        internal bool CanUnlock(Entity actor,Zone zone)=>HasAuthority(actor,zone)&&SpatialQuery.Distance(zone,actor,ParentEntity)<=1;
        internal void RefreshAfterLockChange(){RefreshGlyph();ParentEntity?.SpatialZone?.MarkDoorOcclusionChanged();ZoneRenderHooks.MarkFullDirty("Door.LockState");}
        internal void RejectUnlock(Entity actor)=>Reject(actor,"invalid-lock-request");
        private bool HasAuthority(Entity actor,Zone zone)
        {
            var owner=ParentEntity;var physics=owner?.GetPart<PhysicsPart>();
            return actor!=null&&zone!=null&&owner!=null&&owner.GetPart<DoorPart>()==this
                &&zone.GetEntityCell(actor)!=null&&zone.GetEntityCell(owner)!=null
                &&!CombatSystem.IsDeathHandled(actor)&&actor.GetStatValue("Hitpoints",1)>0
                &&actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()!=true
                &&(actor.HasTag("Player")||actor.HasTag("CanOpenDoors"))
                &&!owner.HasTag("Creature")&&!owner.HasPart<MorrowfastDoorPart>()&&!owner.HasPart<SealedLibraryBarrierPart>()&&physics!=null&&!physics.Takeable&&physics.InInventory==null&&physics.Equipped==null
                &&(string.IsNullOrEmpty(OwnerId)||actor.ID==OwnerId);
        }
        public bool TrySetOpen(Entity actor,Zone zone,bool open)
        {
            if(!CanOperate(actor,zone))return Reject(actor,"invalid-owner-or-permission");
            if(SpatialQuery.Distance(zone,actor,ParentEntity)>1)return Reject(actor,"out-of-reach");
            if(IsOpen==open)return Reject(actor,"already-in-state");
            if(!open)
                foreach(var cell in zone.GetOccupiedCells(ParentEntity))
                    foreach(var other in cell.Occupants)
                        if(other!=ParentEntity&&!IsBareGround(other))return Reject(actor,"doorway-occupied");
            IsOpen=open;RefreshGlyph();zone.MarkDoorOcclusionChanged();
            ZoneRenderHooks.MarkFullDirty("Door.State");
            MessageLog.Add(actor.HasTag("Player")?(open?"You open the door.":"You close the door.")
                :actor.GetDisplayName()+(open?" opens the door.":" closes the door."));
            Diag.Record("furniture",open?"DoorOpened":"DoorClosed",actor,ParentEntity,new{zoneId=zone.ZoneID,ownerId=OwnerId});return true;
        }
        // Shared by physical closure and late placement: Terrain includes bushes and stairs.
        internal static bool IsBareGround(Entity other)
        {
            var physics=other?.GetPart<PhysicsPart>();
            return other!=null&&other.HasTag("Terrain")&&!other.HasTag("Creature")&&!other.HasTag("Wall")&&!other.HasTag("Solid")&&!other.HasTag("Furniture")
                &&physics?.Solid!=true&&physics?.Takeable!=true&&!other.HasPart<LiquidPoolPart>()&&!other.HasPart<GasPoolPart>()
                &&!other.HasPart<TriggerOnStepPart>()&&!other.HasPart<TileStateSourcePart>()
                &&!other.HasPart<DestructiblePart>()&&!other.HasPart<StairsUpPart>()&&!other.HasPart<StairsDownPart>();
        }
        void RefreshGlyph(){var render=ParentEntity?.GetPart<RenderPart>();if(render!=null)render.RenderString=IsClosed?"+":"/";}
        bool Reject(Entity actor,string reason)
        {
            if(actor?.HasTag("Player")==true)MessageLog.Add(reason=="doorway-occupied"?"The doorway is occupied.":"That door cannot be used right now.");
            Diag.Record("furniture","DoorRejected",actor,ParentEntity,new{reason});return false;
        }
    }
}
