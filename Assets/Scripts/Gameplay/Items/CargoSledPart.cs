using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>The same physical sled is either carried or beneath one local
    /// load. References preserve custody; neither mass nor load is reconstructed.</summary>
    public sealed class CargoSledPart : Part, IDescribeUse, IDescribeState,
        IDescribeUseRows, IInventoryActionReceiptOwner
    {
        public override string Name => "CargoSled";
        public Entity Load;
        public int Order => 40;
        public ReaderScope Scope => ReaderScope.Both;
        public string DescribeState(DescribeContext context) => Load == null
            ? "Ready to fit beneath one nearby ordinary haulable load."
            : CargoSledSupport.Paired(Load, ParentEntity)
                ? "Beneath " + Load.GetDisplayName() + "; adds its own weight and halves hauling slowdown while sound."
                : "Separated from its load; no hauling assistance. Recover this same sled from beside it.";
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Carry it beside an ordinary heavy load, fit it underneath, then use the load's usual haul command. Release the haul to recover the same sled.");
            guide.Limits.Add("One action to fit or recover. Adds 6 weight and halves only hauling Speed penalty; no extra strength, blocked-path bypass or general travel between zones. Broken or frozen sleds give no assistance.");
            guide.WhyNot = "carry a sound sled beside an unowned single-cell load you can already haul";
        }
        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<CargoSledPart>() == this
            && command != null && (command.StartsWith("FitCargoSled|", StringComparison.Ordinal)
                || command.StartsWith("TakeCargoSled|", StringComparison.Ordinal));
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var zone = PreparationActions.ZoneFor(e, actor);
            if(e.ID == "GetInventoryActions") RouteWorkMenu.Rows(ParentEntity,actor,zone,
                e.GetParameter<InventoryActionList>("Actions"),Choices(actor,zone),true);
            if(e.ID == "InventoryAction" && OwnsInventoryReceipt(e.GetStringParameter("Command"))
                && RouteWorkMenu.Act(ParentEntity,e)){e.Handled=true;return false;}
            return true;
        }
        public void AddUseRows(Entity actor,Zone zone,InventoryActionList rows)
            => RouteWorkMenu.Rows(ParentEntity,actor,zone,rows,Choices(actor,zone),false);

        IEnumerable<RouteWorkMenu.Entry> Choices(Entity actor,Zone zone)
        {
            var sled=ParentEntity;
            if(!RouteWorkMenu.ActorReady(actor,zone) || !CargoSledSupport.PlainSled(sled))yield break;
            var origin=zone.GetEntityCell(actor);var sameSled=RouteWorkMenu.Identity(sled);
            var pack=actor.GetPart<InventoryPart>();var physics=sled.GetPart<PhysicsPart>();
            bool ActorHere()=>RouteWorkMenu.ActorReady(actor,zone) && actor.GetPart<InventoryPart>()==pack
                && zone.GetEntityCell(actor)==origin && sameSled();
            if(Load==null && WorldResourceActions.Carried(actor,sled) && pack.Objects.Contains(sled) && CargoSledSupport.Sound(sled))
            {
                foreach(var load in zone.GetReadOnlyEntities().ToArray())
                {
                    if(!CargoSledSupport.CanFit(actor,sled,load,zone))continue;
                    var at=zone.GetEntityCell(load);var parts=load.Parts.ToArray();SledLoadPart support=null;
                    bool Before()=>ActorHere() && Load==null && WorldResourceActions.Carried(actor,sled) && pack.Objects.Contains(sled)
                        && load.Parts.SequenceEqual(parts) && zone.GetEntityCell(load)==at && CargoSledSupport.CanFit(actor,sled,load,zone);
                    bool After()=>ActorHere() && CargoSledSupport.Paired(load,sled) && zone.GetEntityCell(load)==at
                        && load.GetPart<SledLoadPart>()==support && load.Parts.SequenceEqual(parts.Concat(new[]{support}))
                        && DragRules.CanDrag(actor,load)==DragVerdict.Ok
                        && !pack.Objects.Any(e=>e==sled || e?.ID==sled.ID);
                    yield return new RouteWorkMenu.Entry{Actor=actor,Zone=zone,Cell=at,Key="FitCargoSled|"+load.ID,
                        Label="fit sled beneath "+load.GetDisplayName()+" (+6 weight; half hauling slowdown)",
                        Hint="One action. The same load and sled remain on the ground; normal capacity and obstacles still apply.",
                        Claims=new[]{load},Before=Before,After=After,Apply=tx=>
                        {
                            support=new SledLoadPart{Sled=sled};
                            tx.Do(null,()=>
                            {
                                if(load.GetPart<SledLoadPart>()==support)load.RemovePart(support);
                                if(Load==load)Load=null;
                                if(Load!=null || sled.GetPart<PhysicsPart>()!=physics || sled.GetPart<CargoSledPart>()!=this)return;
                                var p=physics;
                                // Undo our pickup prohibition without undoing an
                                // independent move or holder's actual custody.
                                p.Takeable=true;
                                if(sled.SpatialZone==zone && zone.GetEntityCell(sled)==at && p.InInventory==null && p.Equipped==null)
                                {
                                    zone.RemoveEntity(sled);p.Takeable=true;
                                    if(!pack.AddRetrievedObject(sled))zone.AddEntity(sled,at.X,at.Y);
                                }
                                else if(sled.SpatialZone==null && p.InInventory==null && p.Equipped==null)
                                {p.Takeable=true;if(!pack.AddRetrievedObject(sled))zone.AddEntity(sled,at.X,at.Y);}
                            });
                            if(!Before() || !pack.RemoveObject(sled))return false;
                            sled.GetPart<PhysicsPart>().Takeable=false;Load=load;load.AddPart(support);
                            return zone.AddEntity(sled,at.X,at.Y) && After();
                        },Publish=()=>{ZoneRenderHooks.MarkCellDirty(at,"CargoSled.Fit");MessageLog.Add("You fit the sled beneath "+load.GetDisplayName()+".");}};
                }
            }
            var current=Load;
            if(current==null || !WorldResourceActions.Nearby(actor,sled,zone) || DragSystem.IsBeingDragged(sled)
                || CargoSledSupport.Paired(current,sled) && DragSystem.IsBeingDragged(current))yield break;
            var ground=zone.GetEntityCell(sled);var oldSupport=current.GetPart<SledLoadPart>();
            bool TakeBefore()=>ActorHere() && Load==current && WorldResourceActions.Nearby(actor,sled,zone)
                && zone.GetEntityCell(sled)==ground && !DragSystem.IsBeingDragged(sled)
                && !(CargoSledSupport.Paired(current,sled) && DragSystem.IsBeingDragged(current));
            bool TakeAfter()=>ActorHere() && Load==null && WorldResourceActions.Carried(actor,sled) && pack.Objects.Contains(sled)
                && (oldSupport==null || current.GetPart<SledLoadPart>()!=oldSupport || oldSupport.Sled!=sled);
            yield return new RouteWorkMenu.Entry{Actor=actor,Zone=zone,Cell=ground,Key="TakeCargoSled|"+sled.ID,
                Label="recover cargo sled",Hint="One action returns the same sled. Release a current haul first; no load is reconstructed.",
                Claims=new[]{current},Before=TakeBefore,After=TakeAfter,Apply=tx=>
                {
                    tx.Do(null,()=>
                    {
                        if(Load!=null || sled.GetPart<PhysicsPart>()!=physics || sled.GetPart<CargoSledPart>()!=this)return;
                        bool carried=WorldResourceActions.Carried(actor,sled) && pack.Objects.Contains(sled);
                        if(!carried && (sled.SpatialZone!=null || physics.InInventory!=null || physics.Equipped!=null))return;
                        if(carried && !pack.RemoveObject(sled))return;
                        if(!zone.AddEntity(sled,ground.X,ground.Y)){pack.AddRetrievedObject(sled);return;}
                        sled.GetPart<PhysicsPart>().Takeable=false;
                        if(oldSupport!=null && oldSupport.Sled==sled && current.GetPart<SledLoadPart>()==null)current.AddPart(oldSupport);
                        Load=current;
                    });
                    if(!TakeBefore() || !zone.RemoveEntity(sled))return false;
                    sled.GetPart<PhysicsPart>().Takeable=true;Load=null;
                    if(oldSupport?.Sled==sled && current.GetPart<SledLoadPart>()==oldSupport)current.RemovePart(oldSupport);
                    return pack.AddRetrievedObject(sled) && TakeAfter();
                },Publish=()=>{ZoneRenderHooks.MarkCellDirty(ground,"CargoSled.Take");MessageLog.Add("You recover the same cargo sled.");}};
        }
    }
    public sealed class SledLoadPart : Part
    {
        public override string Name=>"SledLoad";
        public Entity Sled;
    }
    internal static class CargoSledSupport
    {
        internal static bool PlainSled(Entity sled)=>sled!=null && sled.GetPart<CargoSledPart>()?.ParentEntity==sled
            && sled.Parts.Count(p=>p is CargoSledPart)==1 && sled.GetPart<PhysicsPart>()?.ParentEntity==sled
            && sled.GetPart<PhysicsPart>().Weight==6 && !sled.GetPart<PhysicsPart>().Solid && !sled.HasPart<StackerPart>()
            && !sled.HasPart<HandlingPart>() && !sled.HasPart<ContainerPart>() && !sled.HasTag("Creature")
            && !sled.HasPart<SpatialFootprintPart>() && !sled.HasPart<MultiCellPilotPropPart>();
        internal static bool Sound(Entity sled)=>!sled.HasEffect<BrokenEffect>() && !sled.HasEffect<FrozenEffect>()
            && !RepairablePart.BlocksFunction(sled) && sled.GetPart<DestructiblePart>()?.Gone!=true;
        static bool Link(Entity load,Entity sled)=>load!=null && PlainSled(sled)
            && load.GetPart<SledLoadPart>() is SledLoadPart support && support.ParentEntity==load && support.Sled==sled
            && load.Parts.Count(p=>p is SledLoadPart)==1 && sled.GetPart<CargoSledPart>().Load==load
            && !sled.GetPart<PhysicsPart>().Takeable && !load.HasTag("Creature")
            && !load.HasPart<SpatialFootprintPart>() && !load.HasPart<MultiCellPilotPropPart>();
        internal static bool Paired(Entity load,Entity sled)
        {
            var zone=load?.SpatialZone;
            return zone!=null && Link(load,sled) && WorldResourceActions.Ground(load,zone)
                && WorldResourceActions.Ground(sled,zone) && zone.GetEntityCell(load)==zone.GetEntityCell(sled);
        }
        internal static int Weight(Entity load)=>Paired(load,load?.GetPart<SledLoadPart>()?.Sled)?6:0;
        internal static bool Assists(Entity load)=>Paired(load,load?.GetPart<SledLoadPart>()?.Sled)
            && Sound(load.GetPart<SledLoadPart>().Sled);
        internal static bool CanFit(Entity actor,Entity sled,Entity load,Zone zone)
        {
            if(!RouteWorkMenu.Visible(zone?.GetEntityCell(load)) || !WorldResourceActions.Nearby(actor,load,zone)
                || !PlainSled(sled) || !Sound(sled) || load==sled || actor==load
                || load.ID==sled.ID || load.ID==actor.ID || sled.ID==actor.ID
                || SecondExplorationActions.Special(load) || SecondExplorationActions.Special(sled)
                || load.HasPart<SledLoadPart>() || load.HasPart<StackerPart>() || load.HasPart<SpatialFootprintPart>() || load.HasPart<MultiCellPilotPropPart>()
                || load.HasPart<PackableFixturePart>() || load.HasPart<InstallationMarkerPart>() || load.HasPart<RentalPart>()
                || load.GetPart<DestructiblePart>()?.Gone==true || load.HasPart<InventoryPart>()
                || load.HasPart<ContainerPart>() && !load.HasPart<ContainerLoadPart>()
                || DragSystem.IsDragging(actor) || DragSystem.IsBeingDragged(load) || DragSystem.IsDragging(load)
                || DragRules.CanDrag(actor,load)!=DragVerdict.Ok || (long)HandlingService.GetWeight(load)+6>DragRules.MaxDragWeight(actor))return false;
            var at=zone.GetEntityCell(load);
            return !at.BlocksMovement(load) && !at.Objects.Any(e=>e!=load && (e.HasTag("Creature") || e.HasPart<LiquidPoolPart>()))
                && zone.GetReadOnlyEntities().Count(e=>e==load || e?.ID==load.ID)==1;
        }
        /// <summary>Called only by a successful ordinary hauling follow. A
        /// shove uses the same movement event but must not teleport the sled.</summary>
        internal static void Follow(Entity load,Entity sled,Zone zone,Cell from,Cell destination)
        {
            if(sled==null || !Link(load,sled) || load.SpatialZone!=zone || sled.SpatialZone!=zone
                || zone.GetEntityCell(load)!=destination || zone.GetEntityCell(sled)!=from
                || !WorldResourceActions.Ground(load,zone) || !WorldResourceActions.Ground(sled,zone))return;
            zone.MoveEntity(sled,destination.X,destination.Y);
            ZoneRenderHooks.MarkCellDirty(from,"CargoSled.FollowFrom");ZoneRenderHooks.MarkCellDirty(destination,"CargoSled.FollowTo");
        }
    }
}
