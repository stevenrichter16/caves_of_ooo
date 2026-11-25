using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>One paid stroke relocates finite ground stock. No stored liquid,
    /// elapsed job, irrigation or purification is represented by this tool.</summary>
    public sealed class DirectLiquidPumpPart : Part, IDescribeUse, IDescribeUseRows, IInventoryActionReceiptOwner
    {
        public override string Name=>"DirectLiquidPump";
        public const int StrokeVolume=20;
        public int Order=>40;
        public bool OwnsInventoryReceipt(string command)=>ParentEntity?.GetPart<DirectLiquidPumpPart>()==this
            && command?.StartsWith("PumpLiquid|",StringComparison.Ordinal)==true;
        public override bool HandleEvent(GameEvent e)
        {
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions")RouteWorkMenu.Rows(ParentEntity,actor,zone,e.GetParameter<InventoryActionList>("Actions"),Choices(actor,zone),true);
            if(e.ID=="InventoryAction" && OwnsInventoryReceipt(e.GetStringParameter("Command")) && RouteWorkMenu.Act(ParentEntity,e)){e.Handled=true;return false;}
            return true;
        }
        public void AddUseRows(Entity actor,Zone zone,InventoryActionList rows)=>RouteWorkMenu.Rows(ParentEntity,actor,zone,rows,Choices(actor,zone),false);
        public void DescribeUse(DescribeContext context,UseGuide guide)
        {
            guide.How.Add("Stand between a finite pool and clear floor, each one cardinal step away. Carry this sound pump and choose the source and outlet shown in its action.");
            guide.Needs.Add("visible clear short hoses; one simple unmixed pool; bare outlet or one matching pool");
            guide.Limits.Add("One action moves up to 20 units and keeps the pump. Liquid remains hazardous at its outlet. Emptying the last units removes the source pool. No wells, films, owned fixtures, occupied outlets, irrigation, purification or container filling. No heat history is transferred.");
            guide.WhyNot="carry the pump between an admitted liquid source and a clear visible outlet";
        }
        static readonly (int x,int y)[] Directions={(0,-1),(1,0),(0,1),(-1,0)};
        static bool Clear(Zone zone,Cell cell,Entity pool,Entity actor)
            =>RouteWorkMenu.Visible(cell) && !cell.BlocksMovement(actor)
            && cell.Objects.Any(e=>e!=pool && e.HasTag("Terrain"))
            && cell.Occupants.All(e=>e==pool || e.HasTag("Terrain") && !e.HasTag("Creature")
                && !e.HasPart<LiquidPoolPart>() && !e.HasPart<GasPoolPart>() && !e.HasPart<CropPart>()
                && !e.HasPart<TileStateSourcePart>() && !e.HasPart<WellPart>() && !SecondExplorationActions.Special(e));
        IEnumerable<RouteWorkMenu.Entry> Choices(Entity actor,Zone zone)
        {
            var tool=ParentEntity;var factory=HarvestablePart.Factory;
            if(!RouteWorkMenu.ActorReady(actor,zone) || !WorldResourceActions.Carried(actor,tool)
                || actor.GetPart<InventoryPart>().Objects.Contains(tool)!=true || tool.GetPart<DirectLiquidPumpPart>()!=this
                || tool.HasPart<StackerPart>() || tool.HasEffect<BrokenEffect>() || tool.HasEffect<FrozenEffect>()
                || RepairablePart.BlocksFunction(tool) || factory==null)yield break;
            var template=MaterialProcessTemplate.Capture(factory,tool.BlueprintName);var payload=template?.Snapshot(tool,true);
            var outputTemplate=MaterialProcessTemplate.Capture(factory,LiquidVesselService.PoolBlueprint,true);
            if(payload==null || outputTemplate==null)yield break;
            var pack=actor.GetPart<InventoryPart>();var origin=zone.GetEntityCell(actor);
            bool Context()=>RouteWorkMenu.ActorReady(actor,zone) && actor.GetPart<InventoryPart>()==pack
                && WorldResourceActions.Carried(actor,tool) && pack.Objects.Contains(tool) && zone.GetEntityCell(actor)==origin
                && !zone.GetReadOnlyEntities().Any(e=>e.ID==tool.ID) && payload.Matches() && outputTemplate.Current()
                && HarvestablePart.Factory==factory && !tool.HasEffect<BrokenEffect>() && !tool.HasEffect<FrozenEffect>() && !RepairablePart.BlocksFunction(tool);
            foreach(var d in Directions)
            {
                var from=zone.GetCell(origin.X+d.x,origin.Y+d.y);
                if(!RouteWorkMenu.Visible(from))continue;
                var sources=from.Occupants.Where(e=>e.HasPart<LiquidPoolPart>()).ToArray();if(sources.Length!=1)continue;
                var source=sources[0];
                var stock=PoolReceipt.Capture(source,zone);if(stock==null || stock.Volume<=0 || !Clear(zone,from,source,actor))continue;
                int amount=Math.Min(StrokeVolume,stock.Volume);
                foreach(var direction in Directions)
                {
                    var to=zone.GetCell(origin.X+direction.x,origin.Y+direction.y);
                    if(to==from || !RouteWorkMenu.Visible(to))continue;
                    var pools=to.Occupants.Where(e=>e.HasPart<LiquidPoolPart>()).ToArray();if(pools.Length>1)continue;
                    var destination=pools.SingleOrDefault();var recipient=destination==null?null:PoolReceipt.Capture(destination,zone);
                    if(destination!=null && (recipient==null || recipient.Liquid!=stock.Liquid || (long)recipient.Volume+amount>int.MaxValue))continue;
                    if(!Clear(zone,to,destination,actor) || !LiquidSourceSafety.IsUnmixedCell(zone,to.X,to.Y,stock.Liquid)
                        || new[]{actor.ID,tool.ID,source.ID}.Distinct().Count()!=3
                        || destination!=null && new[]{actor.ID,tool.ID,source.ID,destination.ID}.Distinct().Count()!=4)continue;
                    Entity output=destination;LiquidPoolPart outputPool=recipient?.Pool;PoolReceipt createdReceipt=null;
                    int initial=recipient?.Volume??0;bool credited=false,debited=false;
                    bool Before()=>Context() && stock.Current(stock.Volume) && zone.GetEntityCell(source)==from && Clear(zone,from,source,actor)
                        && Clear(zone,to,destination,actor) && (recipient==null?!to.Occupants.Any(e=>e.HasPart<LiquidPoolPart>()):recipient.Current(initial))
                        && LiquidSourceSafety.IsUnmixedCell(zone,to.X,to.Y,stock.Liquid);
                    bool After()=>Context() && stock.Current(stock.Volume-amount) && zone.GetEntityCell(source)==from && Clear(zone,from,source,actor)
                        && output!=null && (recipient??createdReceipt)?.Current(initial+amount)==true && zone.GetEntityCell(output)==to
                        && Clear(zone,to,output,actor) && LiquidSourceSafety.IsUnmixedCell(zone,to.X,to.Y,stock.Liquid);
                    yield return new RouteWorkMenu.Entry{Actor=actor,Zone=zone,Cell=from,
                        Key="PumpLiquid|"+source.ID+"|"+to.X+"|"+to.Y,
                        Label="pump "+amount+" "+stock.Liquid+" from "+from.X+","+from.Y+" to "+to.X+","+to.Y,
                        Hint="One action; same pump kept. Relocates real stock. Dangerous liquid stays dangerous at the outlet.",
                        Claims=destination==null?new[]{source}:new[]{source,destination},Before=Before,After=After,Apply=tx=>
                        {
                            if(!Before())return false;
                            if(output==null)
                            {
                                output=factory.CreateEntity(LiquidVesselService.PoolBlueprint);outputPool=output?.GetPart<LiquidPoolPart>();
                                if(output==null || outputTemplate.Snapshot(output)==null || !Before() || outputPool?.ParentEntity!=output
                                    || outputPool.Volume!=0 || outputPool.LiquidId!="" || output.SpatialZone!=null
                                    || output.GetPart<PhysicsPart>()?.InInventory!=null || output.GetPart<PhysicsPart>()?.Equipped!=null
                                    || new[]{actor.ID,tool.ID,source.ID,output.ID}.Distinct().Count()!=4
                                    || zone.GetReadOnlyEntities().Any(e=>e.ID==output.ID) || pack.Objects.Any(e=>e.ID==output.ID)
                                    || !tx.TryClaim(output,actor,"PumpLiquid"))return false;
                            }
                            tx.Do(null,()=>
                            {
                                // Only undo actual recoverable credit. A raw callback
                                // consuming it cannot also receive a source refund.
                                int recovered=0;
                                bool refundable=debited && stock.Owner.GetPart<LiquidPoolPart>()==stock.Pool
                                    && stock.Pool.ParentEntity==stock.Owner && stock.Pool.LiquidId==stock.Liquid && stock.Pool.Volume>=0;
                                int room=refundable?int.MaxValue-stock.Pool.Volume:0;
                                if(credited && output.GetPart<LiquidPoolPart>()==outputPool && outputPool.ParentEntity==output && outputPool.LiquidId==stock.Liquid)
                                {recovered=Math.Min(Math.Min(amount,room),Math.Max(0,outputPool.Volume));outputPool.Volume-=recovered;}
                                if(recovered>0)stock.Pool.Volume=WaterQuantityUndo.Restore(stock.Pool.Volume,-recovered);
                                if(credited && recovered<amount)Diag.Record("liquid","PumpUndoCreditRetainedOrConsumed",actor,tool,new{amount,recovered,source=source.ID,outlet=output.ID});
                                if(destination==null && createdReceipt?.Current(0)==true && zone.GetEntityCell(output)==to)zone.RemoveEntity(output);
                            });
                            if(!Before())return false;
                            stock.Pool.Volume-=amount;debited=true;outputPool.Volume+=amount;credited=true;
                            if(destination==null)
                            {
                                outputPool.LiquidId=stock.Liquid;outputPool.Initialize();
                                if(!zone.AddEntity(output,to.X,to.Y))return false;
                                createdReceipt=PoolReceipt.Capture(output,zone);if(createdReceipt==null)return false;
                            }
                            return After();
                        },Publish=()=>
                        {
                            // Zero volume alone does not remove legacy tile coating.
                            // Current rechecks exact owner, Part, cell and stock before
                            // normal removal publishes its projection lifecycle.
                            if(stock.Volume==amount && stock.Current(0) && zone.GetEntityCell(source)==from)zone.RemoveEntity(source);
                            ZoneRenderHooks.MarkCellDirty(from,"HandPump.Source");ZoneRenderHooks.MarkCellDirty(to,"HandPump.Outlet");
                            Diag.Record("liquid","GroundLiquidPumped",actor,tool,new{source=source.ID,outlet=output.ID,liquid=stock.Liquid,amount});
                            MessageLog.Add("You move "+amount+" "+stock.Liquid+" to "+to.X+","+to.Y+". The liquid remains there.");
                        }};
                }
            }
        }
        sealed class PoolReceipt
        {
            internal readonly Entity Owner;internal readonly LiquidPoolPart Pool;internal readonly int Volume;internal readonly string Liquid;
            readonly Zone zone;readonly Cell cell;readonly Func<bool> identity;readonly Part[] passive;
            readonly IMaterialProcessPartPolicy[] policies;readonly object[] values;
            static readonly HashSet<Type> Types=new(){typeof(RenderPart),typeof(PhysicsPart),typeof(ExaminablePart),typeof(LiquidPoolPart),typeof(MaterialPart),typeof(ThermalPart),typeof(StatusEffectsPart)};
            PoolReceipt(Entity owner,Zone zone)
            {
                Owner=owner;Pool=owner.GetPart<LiquidPoolPart>();Volume=Pool.Volume;Liquid=Pool.LiquidId;this.zone=zone;cell=zone.GetEntityCell(owner);identity=RouteWorkMenu.Identity(owner);
                passive=owner.Parts.Where(p=>p!=Pool).ToArray();policies=passive.Select(p=>MaterialProcessPayloadPolicies.Get(p.GetType())).ToArray();
                values=passive.Select((p,i)=>policies[i].Capture(p)).ToArray();
            }
            static bool Plain(Entity owner,Zone zone)
            {
                if(!WorldResourceActions.Ground(owner,zone) || SecondExplorationActions.Special(owner) || !owner.HasTag("Terrain") || owner.HasTag("Creature")
                    || owner.Parts.Any(p=>p==null || p.ParentEntity!=owner || !Types.Contains(p.GetType()))
                    || owner.Parts.GroupBy(p=>p.GetType()).Any(g=>g.Count()!=1) || owner.GetPart<StatusEffectsPart>()?.EffectCount>0
                    || owner.GetPart<PhysicsPart>() is not PhysicsPart physics || physics.Takeable || physics.Solid
                    || owner.GetPart<LiquidPoolPart>() is not LiquidPoolPart pool || pool.Volume<0 || string.IsNullOrEmpty(pool.LiquidId)
                    || !LiquidRegistry.IsInitialized || LiquidRegistry.Get(pool.LiquidId)==null
                    || zone.GetReadOnlyEntities().Count(e=>e==owner || e.ID==owner.ID)!=1)return false;
                var at=zone.GetEntityCell(owner);
                if(at.Occupants.Count(e=>e.HasPart<LiquidPoolPart>())!=1 || !LiquidSourceSafety.IsUnmixedSource(zone,owner,pool.LiquidId))return false;
                return !(pool.LiquidId=="water" || LiquidRegistry.Get(pool.LiquidId).GroundReactionFamily=="water") || LiquidSourcePhase.CanDrawWater(zone,owner);
            }
            internal static PoolReceipt Capture(Entity owner,Zone zone)=>Plain(owner,zone)?new PoolReceipt(owner,zone):null;
            internal bool Current(int expected)=>identity() && Plain(Owner,zone) && zone.GetEntityCell(Owner)==cell
                && Owner.GetPart<LiquidPoolPart>()==Pool && Pool.Volume==expected && Pool.LiquidId==Liquid
                && passive.Select((p,i)=>MaterialProcessPayloadPolicies.Get(p.GetType())==policies[i] && policies[i].Matches(p,values[i])).All(v=>v);
        }
    }
}
