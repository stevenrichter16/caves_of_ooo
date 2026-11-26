using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    // Local action promises, not saved state. Parts retain their own rules and
    // exact inputs; the shared adapter only claims, revalidates and publishes.
    internal static class RouteWorkMenu
    {
        internal sealed class Entry
        {
            internal Entity Actor; internal Zone Zone; internal Cell Cell;
            internal string Key,Label,Hint,Command; internal Entity[] Claims=Array.Empty<Entity>();
            internal ActionTime Time = ActionTime.OneAction;
            internal Func<bool> Before,After; internal Func<InventoryTransaction,bool> Apply; internal Action Publish;
        }
        static readonly ConditionalWeakTable<Entity,Dictionary<string,Entry>> Menus=new ConditionalWeakTable<Entity,Dictionary<string,Entry>>();
        internal static bool Visible(Cell cell)=>cell!=null && cell.IsVisible && cell.Explored;
        internal static bool ActorReady(Entity actor,Zone zone)=>WorldResourceActions.ActorCurrent(actor,zone)
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()!=true;
        internal static Func<bool> Identity(Entity e)
        {string id=e.ID,bp=e.BlueprintName;var parts=e.Parts.ToArray();return ()=>e.ID==id && e.BlueprintName==bp && e.Parts.SequenceEqual(parts) && parts.All(p=>p.ParentEntity==e);}
        internal static void Rows(Entity owner,Entity actor,Zone zone,InventoryActionList rows,IEnumerable<Entry> entries,bool retain)
        {
            if(rows==null)return;var cache=retain?Menus.GetValue(owner,_=>new Dictionary<string,Entry>()):null;
            var old=cache?.Values.ToArray()??Array.Empty<Entry>();cache?.Clear();
            foreach(var next in entries)
            {
                if(!next.Before())continue;
                var q=retain?old.FirstOrDefault(e=>e.Actor==actor && e.Zone==zone && e.Key==next.Key && e.Cell==next.Cell
                    && e.Label==next.Label && e.Hint==next.Hint && e.Time==next.Time && e.Claims.SequenceEqual(next.Claims) && e.Before())??next:next;
                if(q.Command==null)q.Command=q.Key+"|"+Guid.NewGuid().ToString("N");
                if(retain)cache[q.Command]=q;
                rows.AddActionWithHint("RouteWork",q.Label,q.Command,q.Hint,'\0',19,false,q.Time).AtCell(q.Cell.X,q.Cell.Y);
            }
        }
        internal static bool Act(Entity owner,GameEvent e)
        {
            string command=e.GetStringParameter("Command");
            if(command==null || !Menus.TryGetValue(owner,out var rows) || !rows.TryGetValue(command,out var q))return false;
            rows.Remove(command);var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            var tx=e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if(tx==null || q.Actor!=actor || q.Zone!=zone || !q.Before() || !tx.TryClaimAction(actor)
                || !tx.TryClaim(actor,actor,command) || !tx.TryClaim(owner,actor,command))return false;
            foreach(var claim in q.Claims.Distinct())if(claim!=actor && claim!=owner && !tx.TryClaim(claim,actor,command))return false;
            if(!q.Before() || !q.Apply(tx) || !q.After())return false;
            tx.BeforeCommit(q.After);tx.AfterCommit(()=>{if(q.After())q.Publish?.Invoke();else Diag.Record("event","RouteWorkCommittedContextChanged",actor,owner,new{command});});
            return true;
        }
    }
    internal static class BareCaveFloor
    {
        internal static string Resolve(Zone zone, Cell cell, Entity actor, Entity placedMark = null)
        {
            if (zone == null || actor == null || !RouteWorkMenu.Visible(cell) || !cell.IsInterior
                || zone.GetEntityCell(actor) != cell || cell.BlocksMovement(actor)
                || zone.TileState.Get(cell.X, cell.Y)?.Coatings.Any(coating => coating.Turns > 0) == true
                || cell.Occupants.Any(owner => owner != actor && owner != placedMark))
                return null;

            // Strata may carve an ordinary floor without placing a terrain object.
            var manager = WorldLocationContext.For(zone);
            if (manager?.ActiveZone != zone || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || current != zone || !BiomeCropPlan.IsOrdinaryCave(manager, zone.ZoneID))
                return null;
            var (x, y, _) = WorldMap.FromZoneID(zone.ZoneID);
            var plan = UndergroundLayoutPlan.Select(manager.WorldSeed, zone.ZoneID,
                manager.WorldMap.GetBiome(x, y), manager.WorldMap.GetPOI(x, y), manager.Factory);
            return plan.IsOrdinaryColumn && !plan.IsRoomed ? plan.FloorBlueprint : null;
        }
    }
    public sealed class DirectedAirPart : Part,IDescribeUse,IDescribeUseRows,IInventoryActionReceiptOwner
    {
        public override string Name=>"DirectedAir";
        public const int MaximumDensity=40;
        public int Order=>40;
        public bool OwnsInventoryReceipt(string command)=>ParentEntity?.GetPart<DirectedAirPart>()==this && command?.StartsWith("PushGas|",StringComparison.Ordinal)==true;
        IEnumerable<RouteWorkMenu.Entry> Choices(Entity actor,Zone zone)
        {
            var tool=ParentEntity;
            if(!RouteWorkMenu.ActorReady(actor,zone) || !WorldResourceActions.Carried(actor,tool) || !actor.GetPart<InventoryPart>().Objects.Contains(tool) || tool.GetPart<DirectedAirPart>()!=this
                || tool.HasPart<StackerPart>() || tool.HasEffect<BrokenEffect>() || tool.HasEffect<FrozenEffect>() || RepairablePart.BlocksFunction(tool))yield break;
            var origin=zone.GetEntityCell(actor);var pack=actor.GetPart<InventoryPart>();var sameTool=RouteWorkMenu.Identity(tool);
            bool Context()=>RouteWorkMenu.ActorReady(actor,zone) && sameTool() && WorldResourceActions.Carried(actor,tool) && pack.Objects.Contains(tool)
                && actor.GetPart<InventoryPart>()==pack && zone.GetEntityCell(actor)==origin
                && !tool.HasEffect<BrokenEffect>() && !tool.HasEffect<FrozenEffect>() && !RepairablePart.BlocksFunction(tool);
            foreach(var d in new[]{(x:0,y:-1),(x:1,y:0),(x:0,y:1),(x:-1,y:0)})
            {
                var from=zone.GetCell(origin.X+d.x,origin.Y+d.y);var to=zone.GetCell(origin.X+2*d.x,origin.Y+2*d.y);
                bool Path()=>RouteWorkMenu.Visible(from) && RouteWorkMenu.Visible(to) && !from.BlocksMovement() && !to.BlocksMovement();
                if(!Path())continue;
                foreach(var gas in from.Objects.ToArray())
                {
                    var pool=gas.GetPart<GasPoolPart>();var physics=gas.GetPart<PhysicsPart>();
                    if(pool?.ParentEntity!=gas || physics?.ParentEntity!=gas || !gas.HasTag("Gas") || pool.Stable || pool.Density<1 || pool.Density>MaximumDensity
                        || gas.HasPart<SpatialFootprintPart>() || gas.HasPart<MultiCellPilotPropPart>() || gas.HasTag("Creature")
                        || !WorldResourceActions.Ground(gas,zone) || physics.Takeable)continue;
                    var sameGas=RouteWorkMenu.Identity(gas);int density=pool.Density,level=pool.Level;bool seeping=pool.Seeping;
                    string id=pool.GasId,type=pool.GasType,color=pool.ColorString;var creator=pool.Creator;
                    bool Current()=>Context() && Path() && sameGas() && zone.GetEntityCell(gas)==from && WorldResourceActions.Ground(gas,zone)
                        && gas.GetPart<GasPoolPart>()==pool && pool.Density==density && pool.Level==level && !pool.Stable && pool.Seeping==seeping
                        && pool.GasId==id && pool.GasType==type && pool.ColorString==color && pool.Creator==creator && gas.GetPart<PhysicsPart>()==physics && !physics.Takeable;
                    yield return new RouteWorkMenu.Entry{Actor=actor,Zone=zone,Cell=from,Claims=new[]{gas},Key="PushGas|"+gas.ID,
                        Label="push "+gas.GetDisplayName()+" toward "+to.X+","+to.Y+" ("+density+" density)",
                        Hint="Moves this thin cloud one cell farther away. Gas stays dangerous and normal drift can bring it back.",Before=Current,After=Current,Apply=tx=>true,
                        Publish=()=>{if(zone.MoveEntity(gas,to.X,to.Y)){ZoneRenderHooks.MarkCellDirty(from,"Bellows.From");ZoneRenderHooks.MarkCellDirty(to,"Bellows.To");
                            Diag.Record("gas","DirectedCloudMoved",actor,gas,new{density,fromX=from.X,fromY=from.Y,x=to.X,y=to.Y});MessageLog.Add("You push the cloud aside. The gas is still there.");}}};
                }
            }
        }
        public override bool HandleEvent(GameEvent e)
        {
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions")RouteWorkMenu.Rows(ParentEntity,actor,zone,e.GetParameter<InventoryActionList>("Actions"),Choices(actor,zone),true);
            if(e.ID=="InventoryAction" && OwnsInventoryReceipt(e.GetStringParameter("Command")) && RouteWorkMenu.Act(ParentEntity,e)){e.Handled=true;return false;}return true;
        }
        public void AddUseRows(Entity actor,Zone zone,InventoryActionList rows)=>RouteWorkMenu.Rows(ParentEntity,actor,zone,rows,Choices(actor,zone),false);
        public void DescribeUse(DescribeContext context,UseGuide guide)
        {guide.How.Add("Carry the bellows; choose an adjacent thin cloud from the pack or its cell's C menu.");guide.Needs.Add("a visible clear cardinal path; one transient cloud of at most "+MaximumDensity+" density");guide.Limits.Add("One action moves the same cloud one cell farther away, conserving its density. It can drift back. Stable clouds refuse; existing gas turns still expose creatures. A lampvein fan can thin denser clouds first.");guide.WhyNot="carry sound bellows beside a thin cloud with a clear visible cell beyond it";}
    }
    public sealed class TrailMarkingPart : Part,IDescribeUse,IDescribeUseRows,IInventoryActionReceiptOwner
    {
        public override string Name=>"TrailMarking"; public int Order=>40;
        static readonly string[] Notes={"north","east","south","west","exit","water","danger"};
        public bool OwnsInventoryReceipt(string command)=>ParentEntity?.GetPart<TrailMarkingPart>()==this && command?.StartsWith("MakeTrailMark|",StringComparison.Ordinal)==true;
        static bool Floor(Zone zone, Cell at, Entity actor, Entity placedMark = null)
        {
            if (!RouteWorkMenu.Visible(at) || at.BlocksMovement(actor)
                || zone.TileState.Get(at.X, at.Y)?.Coatings.Any(coating => coating.Turns > 0) == true)
                return false;

            bool hasTerrain = false;
            foreach (var occupant in at.Occupants)
            {
                if (occupant == placedMark) continue;
                bool terrain = occupant.HasTag("Terrain");
                if ((!terrain && !occupant.HasTag("Creature"))
                    || occupant.HasPart<LiquidPoolPart>() || occupant.HasPart<GasPoolPart>())
                    return false;
                hasTerrain |= terrain && at.Objects.Contains(occupant);
            }
            return hasTerrain || BareCaveFloor.Resolve(zone, at, actor, placedMark) != null;
        }
        IEnumerable<RouteWorkMenu.Entry> Choices(Entity actor,Zone zone)
        {
            var sand=ParentEntity;var factory=HarvestablePart.Factory;
            if(!RouteWorkMenu.ActorReady(actor,zone) || !WorldResourceActions.Carried(actor,sand,false) || sand.BlueprintName!="SilverSand" || factory==null)yield break;
            var origin=zone.GetEntityCell(actor);if(!Floor(zone,origin,actor))yield break;
            var markTemplate=MaterialProcessTemplate.Capture(factory,"TrailMark",true);if(markTemplate==null)yield break;
            var template=MaterialProcessTemplate.Capture(factory,sand.BlueprintName);var payload=template?.Snapshot(sand,true);if(payload==null)yield break;
            var pack=actor.GetPart<InventoryPart>();var physics=sand.GetPart<PhysicsPart>();var stack=sand.GetPart<StackerPart>();int count=stack?.StackCount??1;
            bool Context()=>RouteWorkMenu.ActorReady(actor,zone) && zone.GetEntityCell(actor)==origin && actor.GetPart<InventoryPart>()==pack
                && HarvestablePart.Factory==factory && payload.Matches() && markTemplate.Current();
            bool Paid()=>Context() && physics.Equipped==null && (count>1?WorldResourceActions.Carried(actor,sand,false) && stack.StackCount==count-1
                :sand.SpatialZone==null && physics.InInventory==null && (stack?.StackCount??1)==1 && !pack.Objects.Any(e=>e==sand || e?.ID==sand.ID)
                    && !pack.EquippedItems.Values.Any(e=>e==sand || e?.ID==sand.ID) && pack.FindEquippedBodyPart(sand)==null);
            foreach(string note in Notes)
            {
                Entity mark=null;TrailMarkPart part=null;PhysicsPart outputPhysics=null;Func<bool> sameMark=null;
                bool Before()=>Context() && WorldResourceActions.Carried(actor,sand,false) && (stack?.StackCount??1)==count && Floor(zone,origin,actor);
                bool After()=>Paid() && mark?.SpatialZone==zone && zone.GetEntityCell(mark)==origin && mark.GetPart<TrailMarkPart>()==part && part.Note==note && sameMark() && mark.GetPart<PhysicsPart>()==outputPhysics
                    && outputPhysics.ParentEntity==mark && !outputPhysics.Takeable && !outputPhysics.Solid && outputPhysics.InInventory==null && outputPhysics.Equipped==null
                    && !pack.Objects.Any(e=>e==mark || e?.ID==mark.ID) && !pack.EquippedItems.Values.Any(e=>e==mark || e?.ID==mark.ID)
                    && Floor(zone,origin,actor,mark);
                yield return new RouteWorkMenu.Entry{Actor=actor,Zone=zone,Cell=origin,Key="MakeTrailMark|"+note,Label="mark "+note+" here (1 silver sand)",
                    Hint="Leaves your own navigation note. It verifies no exit, water or danger; brushing it away returns no sand.",Before=Before,After=After,Apply=tx=>
                    {
                        mark=factory.CreateEntity("TrailMark");part=mark?.GetPart<TrailMarkPart>();outputPhysics=mark?.GetPart<PhysicsPart>();
                        sameMark=mark==null?null:RouteWorkMenu.Identity(mark);
                        if(mark==null || markTemplate.Snapshot(mark)==null || string.IsNullOrEmpty(mark.ID) || part?.ParentEntity!=mark || mark.Parts.Count(p=>p is TrailMarkPart)!=1 || mark.SpatialZone!=null
                            || outputPhysics?.ParentEntity!=mark || outputPhysics.InInventory!=null || outputPhysics.Equipped!=null
                            || pack.Objects.Concat(pack.EquippedItems.Values).Any(e=>e==mark || e?.ID==mark.ID)
                            || mark.GetPart<PhysicsPart>()?.Takeable!=false
                            || mark.GetPart<PhysicsPart>().Solid || mark.HasPart<LightSourcePart>() || zone.GetReadOnlyEntities().Any(e=>e.ID==mark.ID)
                            || !Before() || !MaterialProcessService.ReserveInput(sand,tx) || !MaterialProcessPayment.Spend(actor,sand,actor,zone,tx,()=>payload.Matches() && mark.SpatialZone==null && outputPhysics.InInventory==null && outputPhysics.Equipped==null))return false;
                        part.Note=note;part.Refresh();
                        tx.Do(null,()=>{if(mark.SpatialZone==zone && zone.GetEntityCell(mark)==origin && mark.GetPart<TrailMarkPart>()==part && part.Note==note)zone.RemoveEntity(mark);});
                        return Paid() && Floor(zone,origin,actor) && zone.AddEntity(mark,origin.X,origin.Y);
                    },Publish=()=>{ZoneRenderHooks.MarkCellDirty(origin,"TrailMark.Placed");MessageLog.Add("You leave a sand mark: "+note+". It is your note, not a surveyed fact.");}};
            }
        }
        public override bool HandleEvent(GameEvent e)
        {var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);if(e.ID=="GetInventoryActions")RouteWorkMenu.Rows(ParentEntity,actor,zone,e.GetParameter<InventoryActionList>("Actions"),Choices(actor,zone),true);
            if(e.ID=="InventoryAction" && OwnsInventoryReceipt(e.GetStringParameter("Command")) && RouteWorkMenu.Act(ParentEntity,e)){e.Handled=true;return false;}return true;}
        public void AddUseRows(Entity actor,Zone zone,InventoryActionList rows)=>RouteWorkMenu.Rows(ParentEntity,actor,zone,rows,Choices(actor,zone),false);
        public void DescribeUse(DescribeContext context,UseGuide guide)
        {guide.How.Add("Use carried silver sand to mark your dry floor with an arrow, exit, water or danger note.");guide.Limits.Add("One sand and one action per mark. Notes persist until brushed away; they do not reveal or verify anything. One per cell; no light or sand refund.");guide.WhyNot="stand on visible dry bare floor with silver sand";}
    }
    public sealed class TrailMarkPart : Part,IDescribeUse,IDescribeState,IInventoryActionReceiptOwner
    {
        public override string Name=>"TrailMark";public string Note="";public int Order=>40;public ReaderScope Scope=>ReaderScope.Both;
        internal void Refresh(){var r=ParentEntity?.GetPart<RenderPart>();if(r==null)return;r.DisplayName="sand mark: "+Note;r.RenderString=Note switch{"north"=>"^","east"=>">","south"=>"v","west"=>"<","exit"=>"E","water"=>"W",_=>"!"};}
        public override void OnAfterLoad(SaveReader reader)=>Refresh();
        public string DescribeState(DescribeContext context)=>"A traveller's mark: "+Note+". It records a choice, not a verified direction or resource.";
        public void DescribeUse(DescribeContext context,UseGuide guide){guide.How.Add("Read it as a local note, or brush it away from beside it.");guide.Limits.Add("Brushing takes one action and returns no sand. No light or map reveal.");}
        public bool OwnsInventoryReceipt(string command)=>command?.StartsWith("ClearTrailMark|",StringComparison.Ordinal)==true;
        public override bool HandleEvent(GameEvent e)
        {
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);var mark=ParentEntity;
            if(e.ID=="GetInventoryActions" && RouteWorkMenu.ActorReady(actor,zone) && WorldResourceActions.Nearby(actor,mark,zone))
            {
                var origin=zone.GetEntityCell(actor);var at=zone.GetEntityCell(mark);string note=Note;var same=RouteWorkMenu.Identity(mark);
                bool Before()=>same() && Note==note && RouteWorkMenu.ActorReady(actor,zone) && zone.GetEntityCell(actor)==origin && zone.GetEntityCell(mark)==at;
                bool After()=>same() && Note==note && RouteWorkMenu.ActorReady(actor,zone) && zone.GetEntityCell(actor)==origin && mark.SpatialZone==null;
                var q=new RouteWorkMenu.Entry{Actor=actor,Zone=zone,Cell=at,Key="ClearTrailMark|"+mark.ID,Label="brush away sand mark",Hint="One action; no sand is recovered.",Before=Before,After=After,
                    Apply=tx=>{tx.Do(null,()=>{if(mark.SpatialZone==null && same())zone.AddEntity(mark,at.X,at.Y);});return zone.RemoveEntity(mark);},Publish=()=>{ZoneRenderHooks.MarkCellDirty(at,"TrailMark.Cleared");MessageLog.Add("You brush the mark away.");}};
                RouteWorkMenu.Rows(mark,actor,zone,e.GetParameter<InventoryActionList>("Actions"),new[]{q},true);
            }
            if(e.ID=="InventoryAction" && OwnsInventoryReceipt(e.GetStringParameter("Command")) && RouteWorkMenu.Act(mark,e)){e.Handled=true;return false;}return true;
        }
    }
    public sealed class LodgedSpikePart : Part,IDescribeState,IDescribeUse,IInventoryActionReceiptOwner
    {
        public override string Name=>"LodgedSpike";public Entity Door;
        public ReaderScope Scope=>ReaderScope.Both;public int Order=>40;
        public string DescribeState(DescribeContext context)=>"Lodged as a door brace. The same iron spike can be taken back, even if the door is gone.";
        public void DescribeUse(DescribeContext context,UseGuide guide){guide.How.Add("Stand beside this spike and remove the brace.");guide.Limits.Add("One action returns this same spike; the doorway can open again. No extra door strength.");}
        public bool OwnsInventoryReceipt(string command)=>DoorBraceActions.IsCommand(command);
        public override bool HandleEvent(GameEvent e)=>DoorBraceActions.Handle(ParentEntity,e);
    }
    internal static class DoorBraceActions
    {
        internal static bool IsCommand(string command)=>command!=null && (command.StartsWith("BraceDoor|",StringComparison.Ordinal) || command.StartsWith("RemoveDoorBrace|",StringComparison.Ordinal));
        internal static bool Blocked(Entity door,Zone zone)=>zone?.GetEntityCell(door) is Cell cell && cell.Objects.Any(e=>e.GetPart<LodgedSpikePart>() is LodgedSpikePart b
            && b.ParentEntity==e && b.Door==door && e.BlueprintName=="IronSpikeComponent" && WorldResourceActions.Ground(e,zone)
            && e.GetPart<PhysicsPart>()?.Takeable==false && (e.GetPart<StackerPart>()?.StackCount??1)==1);
        static bool DoorReady(Entity actor,Entity door,Zone zone)=>WorldResourceActions.Nearby(actor,door,zone) && RouteWorkMenu.Visible(zone.GetEntityCell(door))
            && door.GetPart<DoorPart>() is DoorPart p && p.IsClosed && p.CanOperate(actor,zone) && !door.HasPart<LockPart>()
            && !door.HasPart<MorrowfastDoorPart>() && !door.HasPart<SealedLibraryBarrierPart>() && !Blocked(door,zone)
            && door.GetPart<DestructiblePart>()?.Gone!=true;
        internal static IEnumerable<RouteWorkMenu.Entry> Choices(Entity spike,Entity actor,Zone zone)
        {
            if(!RouteWorkMenu.ActorReady(actor,zone) || spike?.BlueprintName!="IronSpikeComponent")yield break;
            var origin=zone.GetEntityCell(actor);var pack=actor.GetPart<InventoryPart>();var physics=spike.GetPart<PhysicsPart>();
            var brace=spike.GetPart<LodgedSpikePart>();
            if(brace!=null)
            {
                if(!WorldResourceActions.Nearby(actor,spike,zone) || physics?.Takeable!=false || pack.MaxWeight>=0 && (long)pack.GetCarriedWeight()+HandlingService.GetWeight(spike)>pack.MaxWeight)yield break;
                var at=zone.GetEntityCell(spike);var door=brace.Door;string id=spike.ID;var oldParts=spike.Parts.ToArray();
                bool Before()=>RouteWorkMenu.ActorReady(actor,zone) && zone.GetEntityCell(actor)==origin && actor.GetPart<InventoryPart>()==pack
                    && WorldResourceActions.Ground(spike,zone) && zone.GetEntityCell(spike)==at && spike.ID==id && spike.GetPart<LodgedSpikePart>()==brace
                    && brace.Door==door && spike.Parts.SequenceEqual(oldParts) && spike.GetPart<PhysicsPart>()==physics && !physics.Takeable && (spike.GetPart<StackerPart>()?.StackCount??1)==1;
                bool After()=>RouteWorkMenu.ActorReady(actor,zone) && zone.GetEntityCell(actor)==origin && actor.GetPart<InventoryPart>()==pack && spike.ID==id
                    && WorldResourceActions.Carried(actor,spike) && spike.GetPart<PhysicsPart>()==physics && physics.Takeable && spike.GetPart<LodgedSpikePart>()==null;
                yield return new RouteWorkMenu.Entry{Actor=actor,Zone=zone,Cell=at,Key="RemoveDoorBrace|"+id,Label="remove door brace (take this spike)",Hint="One action. Returns this same spike and allows the door to open.",Before=Before,After=After,Apply=tx=>
                {
                    tx.Do(null,()=>{if(spike.ID!=id || spike.GetPart<PhysicsPart>()!=physics || spike.GetPart<LodgedSpikePart>()!=null)return;
                        if(physics.InInventory==actor && pack.Objects.Count(e=>e==spike)==1 && physics.Equipped==null && spike.SpatialZone==null)pack.RemoveObject(spike);
                        if(physics.InInventory!=null || physics.Equipped!=null || spike.SpatialZone!=null)return;
                        physics.Takeable=false;spike.AddPart(brace);zone.AddEntity(spike,at.X,at.Y);});
                    if(!zone.RemoveEntity(spike))return false;spike.RemovePart(brace);physics.Takeable=true;
                    return pack.AddRetrievedObject(spike);
                },Publish=()=>{ZoneRenderHooks.MarkCellDirty(at,"DoorBrace.Removed");MessageLog.Add("You pull out the iron spike. The door is no longer braced.");}};yield break;
            }
            if(!actor.HasTag("Player") || !WorldResourceActions.Carried(actor,spike) || (spike.GetPart<StackerPart>()?.StackCount??1)!=1)yield break;
            var template=MaterialProcessTemplate.Capture(ForgePart.Factory??HarvestablePart.Factory,spike.BlueprintName);
            if(template?.Snapshot(spike,true)==null)yield break;
            foreach(var door in zone.GetReadOnlyEntities().Where(d=>DoorReady(actor,d,zone)))
            {
                var part=door.GetPart<DoorPart>();var doorCell=zone.GetEntityCell(door);var doorIdentity=RouteWorkMenu.Identity(door);
                var oldParts=spike.Parts.ToArray();var original=oldParts.Where(p=>p is not PhysicsPart).Select(p=>(part:p,policy:MaterialProcessPayloadPolicies.Get(p.GetType()))).ToArray();
                if(original.Any(p=>p.policy==null))continue;var values=original.Select(p=>p.policy.Capture(p.part)).ToArray();
                string id=spike.ID,owner=part.OwnerId;int weight=physics.Weight;float throwBase=physics.ThrowBase,throwHeight=physics.ThrowHeight;int intercept=physics.ThrowIntercept;string category=physics.Category;bool solid=physics.Solid;
                LodgedSpikePart installed=null;int oldIndex=pack.Objects.IndexOf(spike);
                bool Payload(bool fitted)=>spike.ID==id && spike.BlueprintName=="IronSpikeComponent" && spike.GetPart<PhysicsPart>()==physics && physics.Weight==weight && physics.Solid==solid
                    && physics.ThrowBase==throwBase && physics.ThrowHeight==throwHeight && physics.ThrowIntercept==intercept && physics.Category==category
                    && (spike.GetPart<StackerPart>()?.StackCount??1)==1 && spike.Parts.Where(p=>p!=installed).SequenceEqual(oldParts)
                    && original.Select((p,i)=>p.part.ParentEntity==spike && p.policy.Matches(p.part,values[i])).All(v=>v);
                bool Context()=>RouteWorkMenu.ActorReady(actor,zone) && actor.HasTag("Player") && zone.GetEntityCell(actor)==origin && actor.GetPart<InventoryPart>()==pack
                    && doorIdentity() && zone.GetEntityCell(door)==doorCell && part.IsClosed && part.OwnerId==owner && !door.HasPart<LockPart>() && door.GetPart<DestructiblePart>()?.Gone!=true;
                bool Before()=>Context() && Payload(false) && WorldResourceActions.Carried(actor,spike) && DoorReady(actor,door,zone);
                bool After()=>Context() && Payload(true) && WorldResourceActions.Ground(spike,zone) && zone.GetEntityCell(spike)==doorCell && !physics.Takeable
                    && spike.GetPart<LodgedSpikePart>()==installed && installed?.Door==door && !pack.Objects.Any(e=>e==spike || e?.ID==id) && !pack.EquippedItems.Values.Any(e=>e==spike || e?.ID==id);
                yield return new RouteWorkMenu.Entry{Actor=actor,Zone=zone,Cell=doorCell,Claims=new[]{door},Key="BraceDoor|"+door.ID,
                    Label="brace closed door with this spike",Hint="One action. Blocks ordinary opening from both sides; remove this same spike to reopen. Adds no door strength.",Before=Before,After=After,Apply=tx=>
                    {
                        installed=new LodgedSpikePart{Door=door};
                        tx.Do(null,()=>{if(spike.ID!=id || spike.GetPart<PhysicsPart>()!=physics || spike.GetPart<LodgedSpikePart>()!=installed)return;
                            if(spike.SpatialZone==zone && zone.GetEntityCell(spike)==doorCell)zone.RemoveEntity(spike);
                            if(spike.SpatialZone!=null || physics.InInventory!=null || physics.Equipped!=null)return;
                            spike.RemovePart(installed);physics.Takeable=true;
                            if(actor.GetPart<InventoryPart>()==pack && !pack.Objects.Any(e=>e==spike || e?.ID==id)){pack.Objects.Insert(Math.Min(oldIndex,pack.Objects.Count),spike);physics.InInventory=actor;pack.RefreshHandlingCarryPenalty();}
                            else zone.AddEntity(spike,doorCell.X,doorCell.Y);});
                        if(!pack.RemoveObject(spike))return false;physics.Takeable=false;spike.AddPart(installed);return zone.AddEntity(spike,doorCell.X,doorCell.Y);
                    },Publish=()=>{ZoneRenderHooks.MarkCellDirty(doorCell,"DoorBrace.Fitted");MessageLog.Add("You wedge the iron spike into the door. Remove it to open the door again.");}};
            }
        }
        internal static void AddUseRows(Entity spike,Entity actor,Zone zone,InventoryActionList rows)=>RouteWorkMenu.Rows(spike,actor,zone,rows,Choices(spike,actor,zone),false);
        internal static bool Handle(Entity spike,GameEvent e)
        {
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions")RouteWorkMenu.Rows(spike,actor,zone,e.GetParameter<InventoryActionList>("Actions"),Choices(spike,actor,zone),true);
            if(e.ID=="InventoryAction" && IsCommand(e.GetStringParameter("Command")) && RouteWorkMenu.Act(spike,e)){e.Handled=true;return false;}return true;
        }
    }
}
