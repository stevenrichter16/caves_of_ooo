using System;
namespace CavesOfOoo.Core
{
    /// <summary>A current menu opportunity, never an instruction to execute an action.
    /// Value-only and ephemeral: no saved state, event dispatch or hidden contents.</summary>
    public readonly struct WorldAffordance
    {
        internal WorldAffordance(Entity target, Cell cell, int verb, int direction, string harvestLabel)
        { Target=target; Cell=cell; VerbIndex=verb; Direction=direction; HarvestLabel=harvestLabel; }
        internal int VerbIndex { get; }
        internal int Direction { get; }
        internal string HarvestLabel { get; }
        public Entity Target { get; }
        public Cell Cell { get; }
        public string Command => WorldAffordanceQuery.Commands[VerbIndex];
        public string Hint => HarvestLabel==null ? WorldAffordanceQuery.Hints[VerbIndex*10+Direction]
            : WorldAffordanceQuery.MenuPrefixes[Direction]+HarvestLabel;
    }

    /// <summary>Bounded read-only hints for native interaction families.
    /// Checks current prerequisites, not future event vetoes. The existing menu
    /// remains authoritative. No GetInventoryActions, lazy loading or callbacks.</summary>
    public static class WorldAffordanceQuery
    {
        internal static readonly string[] Commands={"Harvest","OpenContainer",DoorPart.OpenCommand,DoorPart.CloseCommand,"Examine",HandlingPart.HaulCommand,HandlingPart.ReleaseCommand,TrapJammingPart.JamCommand};
        internal static readonly string[] MenuPrefixes={"C, Y: menu / ","C, W: menu / ","C, U: menu / ","C, A: menu / ","C, .: menu / ",
            "C, D: menu / ","C, B: menu / ","C, S: menu / ","C, N: menu / ","Enter: menu / "};
        internal static readonly string[] Hints=BuildHints();
        private static string[] BuildHints()
        {
            var verbs=new[]{"harvest","open","open door","close door","read","haul","let go","jam trap"};
            var rows=new string[verbs.Length*10];
            for(int v=0;v<verbs.Length;v++)for(int d=0;d<10;d++)rows[v*10+d]=MenuPrefixes[d]+verbs[v];
            return rows;
        }
        public static WorldAffordance? Find(Entity actor,Zone zone,bool focused,int x,int y)
        {
            if(!ActorCurrent(actor,zone,out var standing))return null;
            if(focused)return At(actor,zone,standing,x,y,true);
            var underfoot=At(actor,zone,standing,standing.X,standing.Y,false);
            if(underfoot.HasValue)return underfoot;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {
                if(dx==0&&dy==0)continue;
                var found=At(actor,zone,standing,standing.X+dx,standing.Y+dy,false);
                if(found.HasValue)return found;
            }
            return null;
        }
        public static bool Current(Entity actor,Zone zone,WorldAffordance cue)
        {
            if(!ActorCurrent(actor,zone,out var standing)||cue.Cell==null)return false;
            var found=At(actor,zone,standing,cue.Cell.X,cue.Cell.Y,cue.Direction==9);
            return found.HasValue&&ReferenceEquals(found.Value.Target,cue.Target)&&ReferenceEquals(found.Value.Cell,cue.Cell)
                &&found.Value.VerbIndex==cue.VerbIndex&&found.Value.Direction==cue.Direction
                &&string.Equals(found.Value.HarvestLabel,cue.HarvestLabel,StringComparison.Ordinal);
        }
        private static bool ActorCurrent(Entity actor,Zone zone,out Cell standing)
        {
            standing=null;
            if(actor==null||zone==null||!actor.HasTag("Player")||actor.SpatialZone!=zone
                ||CombatSystem.IsDeathHandled(actor)||actor.GetStatValue("Hitpoints",1)<=0||KnownActionBlock(actor))return false;
            var manager=WorldLocationContext.For(zone);
            if(manager!=null&&(manager.ActiveZone!=zone||!manager.CachedZones.TryGetValue(zone.ZoneID,out var cached)||cached!=zone))return false;
            standing=zone.GetEntityCell(actor);
            var physics=actor.GetPart<PhysicsPart>();
            return standing!=null&&standing.Objects.Contains(actor)&&physics!=null&&physics.ParentEntity==actor
                &&physics.InInventory==null&&physics.Equipped==null;
        }
        // Mirror current built-in blocking fields without invoking virtual AllowAction.
        // Unknown custom callbacks remain the existing menu/execution's authority.
        private static bool KnownActionBlock(Entity actor)
        {
            var status=actor.GetPart<StatusEffectsPart>();
            if(status==null)return false;
            if(status.ParentEntity!=actor)return true;
            var effects=status.GetAllEffects();
            for(int i=0;i<effects.Count;i++)
            {
                var e=effects[i];
                if(e==null||e.Owner!=actor)return true;
                if(e is StunnedEffect||e is ParalyzedEffect||e is HibernatingEffect||e is AsleepByGasEffect)return true;
                if(e is FrozenEffect ice&&ice.Cold>0)return true;
                if(e is LiquidCoveredEffect coat&&LiquidRegistry.IsInitialized
                    &&LiquidRegistry.Get(coat.LiquidId)?.BlockAction==true)return true;
            }
            return false;
        }
        private static WorldAffordance? At(Entity actor,Zone zone,Cell standing,int x,int y,bool focused)
        {
            if(!zone.InBounds(x,y)||Math.Abs(x-standing.X)>1||Math.Abs(y-standing.Y)>1)return null;
            var cell=zone.GetCell(x,y);
            if(cell==null||!cell.IsVisible||!cell.Explored)return null;
            Entity best=null;int verb=-1,layer=int.MinValue;string harvestLabel=null;
            // CellOccupants is a value enumerator; indexed access allocates no list.
            var occupants=cell.Occupants;
            for(int i=0;i<occupants.Count;i++)
            {
                var owner=occupants[i];
                if(owner==actor||!OwnerCurrent(owner,zone,cell))continue;
                int candidate=Verb(actor,zone,owner,out var label);
                if(candidate<0)continue;
                int ownerLayer=owner.GetPart<RenderPart>().RenderLayer;
                if(best==null||ownerLayer>layer||(ownerLayer==layer&&string.CompareOrdinal(owner.ID,best.ID)<0))
                {best=owner;verb=candidate;layer=ownerLayer;harvestLabel=label;}
            }
            return best==null?(WorldAffordance?)null:new WorldAffordance(best,cell,verb,
                focused?9:(y-standing.Y+1)*3+x-standing.X+1,harvestLabel);
        }
        private static bool OwnerCurrent(Entity owner,Zone zone,Cell cell)
        {
            if(owner==null||owner.SpatialZone!=zone||zone.GetEntityCell(owner)!=cell||!cell.Objects.Contains(owner))return false;
            var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();
            return render!=null&&render.ParentEntity==owner&&render.Visible&&physics!=null&&physics.ParentEntity==owner
                &&physics.InInventory==null&&physics.Equipped==null&&(owner.GetPart<StackerPart>()?.StackCount??1)>0;
        }
        private static int Verb(Entity actor,Zone zone,Entity owner,out string harvestLabel)
        {
            harvestLabel=null;
            var trap=owner.GetPart<TrapJammingPart>();
            if(trap!=null&&trap.CanOfferJam(actor,zone))return 7;
            var field=owner.GetPart<FieldHarvestPart>();
            if(field!=null&&field.ParentEntity==owner&&!field.Harvested&&field.YieldCount>0
                &&!string.IsNullOrEmpty(field.YieldBlueprint)&&HarvestablePart.Factory?.Blueprints.ContainsKey(field.YieldBlueprint)==true)return 0;
            var harvest=owner.GetPart<HarvestablePart>();
            if(harvest!=null&&harvest.GetType()==typeof(HarvestablePart)&&harvest.ParentEntity==owner&&!harvest.Harvested
                &&harvest.YieldMin>=1&&harvest.YieldMax>=harvest.YieldMin&&harvest.YieldMax<=HarvestablePart.MaximumYieldCount
                &&harvest.YieldChance>=0&&harvest.YieldChance<=100&&HarvestablePart.CanCreateYield(HarvestablePart.Factory,harvest.YieldBlueprint))
            {
                if(!string.IsNullOrWhiteSpace(harvest.ActionText)&&harvest.ActionText!="harvest")
                    harvestLabel=harvest.ActionText;
                return 0;
            }
            var container=owner.GetPart<ContainerPart>();
            if(container!=null&&container.GetType()==typeof(ContainerPart)&&container.ParentEntity==owner&&!container.IsLocked)return 1;
            var door=owner.GetPart<DoorPart>();
            if(door!=null&&door.ParentEntity==owner&&!owner.HasTag("Creature")&&!owner.HasPart<MorrowfastDoorPart>()
                &&!owner.HasPart<SealedLibraryBarrierPart>()&&!owner.GetPart<PhysicsPart>().Takeable
                &&!RepairablePart.BlocksFunction(owner)&&owner.GetPart<LockPart>()?.IsLocked!=true&&(string.IsNullOrEmpty(door.OwnerId)||door.OwnerId==actor.ID))
            {
                if(!door.IsOpen)return 2;
                // No closure when another physical owner occupies the aperture.
                var occupied=zone.GetOccupiedCells(owner);
                foreach(var cell in occupied)foreach(var other in cell.Occupants)
                    if(other!=owner&&!DoorPart.IsBareGround(other))return -1;
                return 3;
            }
            var examine=owner.GetPart<ExaminablePart>();var sign=owner.GetPart<RegionalSignpostPart>();
            if(examine!=null&&examine.GetType()==typeof(ExaminablePart)&&examine.ParentEntity==owner
                &&((sign!=null&&sign.ParentEntity==owner)||owner.BlueprintName=="Signpost"||owner.BlueprintName=="GroveSign"))return 4;
            return HaulVerb(actor,owner);
        }
        private static int HaulVerb(Entity actor,Entity owner)
        {
            // Match the sealed menu provider without firing GetInventoryActions.
            // At/OwnerCurrent already prove visible, reachable world ownership.
            var handling=owner.GetPart<HandlingPart>();
            if(handling==null||handling.ParentEntity!=owner)return -1;
            var grip=actor.GetPart<DragPart>();var held=owner.GetPart<DraggedPart>();
            if(grip!=null)
            {
                // Let Go remains available after strength/weight changes, but a
                // stale or one-sided link must never be repaired by this query.
                return grip.ParentEntity==actor&&grip.Dragged==owner
                    &&held!=null&&held.ParentEntity==owner&&held.Dragger==actor?6:-1;
            }
            if(held!=null)return -1;
            return DragRules.CanDrag(actor,owner)==DragVerdict.Ok?5:-1;
        }
    }
}
