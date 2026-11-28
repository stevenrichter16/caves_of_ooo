using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
    /// <summary>One paid stroke relocates explicitly admitted finite stock into a ground pool.</summary>
    public sealed class DirectLiquidPumpPart : Part, IDescribeUse, IDescribeUseRows, IInventoryActionReceiptOwner
    {
        public override string Name=>"DirectLiquidPump";
        public const int StrokeVolume=20;
        const string RecoverAtOutlet = "One action. Fill a compatible flask at the outlet while it is liquid.";
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
            guide.How.Add("Stand one cardinal step from both a finite pool or released oil tray and a clear outlet. Carry the sound pump and choose the source and outlet shown.");
            guide.How.Add("To recover what you moved, stand beside the liquid at the outlet, press C toward it and choose Fill. A flask needs room and must be empty or hold the same liquid.");
            guide.Needs.Add("visible short hose ends, an admitted inlet and bare floor or one matching pool at the outlet");
            guide.Limits.Add("One action moves up to 20 units. Liquid remains hazardous at its outlet. Empty pools disappear, while an emptied tray stays in place for clean recovery.");
            guide.Limits.Add("The outlet follows its own temperature and terrain rules.");
            guide.WhyNot="carry the pump between an admitted liquid source and a clear visible outlet";
        }
        static readonly (int x,int y)[] Directions={(0,-1),(1,0),(0,1),(-1,0)};
        static bool Clear(Cell cell,Entity stock,Entity ignoring,bool needsTerrain=true)
            =>RouteWorkMenu.Visible(cell) && !cell.BlocksMovement(ignoring)
            && (!needsTerrain || cell.Objects.Any(e=>e!=stock && e.HasTag("Terrain")))
            && cell.Occupants.All(e=>e==stock || e.HasTag("Terrain") && !e.HasTag("Creature")
                && !e.HasPart<LiquidPoolPart>() && !e.HasPart<ContainedLiquidSourcePart>() && !e.HasPart<GasPoolPart>() && !e.HasPart<CropPart>()
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
                var sources=from.Occupants.Where(e=>e.HasPart<LiquidPoolPart>()
                    || e.HasPart<ContainedLiquidSourcePart>()).ToArray();
                if(sources.Length!=1)continue;
                var source=sources[0];
                StockReceipt stock=source.HasPart<ContainedLiquidSourcePart>()
                    ? TrayReceipt.Capture(source,actor,zone) : PoolReceipt.Capture(source,zone);
                if(stock==null || stock.Volume<=0)continue;
                bool tray=stock is TrayReceipt;
                bool SourceCurrent(int expected)=>stock.Current(expected) && zone.GetEntityCell(source)==from
                    && Clear(from,source,tray?source:actor,needsTerrain:!tray);
                if(!SourceCurrent(stock.Volume))continue;
                int amount=Math.Min(StrokeVolume,stock.Volume);
                foreach(var direction in Directions)
                {
                    var to=zone.GetCell(origin.X+direction.x,origin.Y+direction.y);
                    if(to==from || !RouteWorkMenu.Visible(to))continue;
                    var pools=to.Occupants.Where(e=>e.HasPart<LiquidPoolPart>()).ToArray();if(pools.Length>1)continue;
                    var destination=pools.SingleOrDefault();var recipient=destination==null?null:PoolReceipt.Capture(destination,zone);
                    if(destination!=null && (recipient==null || recipient.Liquid!=stock.Liquid || (long)recipient.Volume+amount>int.MaxValue))continue;
                    bool BareOutlet()=>!tray || recipient!=null
                        || zone.TileState.Get(to.X,to.Y)?.Coatings.Any(coating=>coating.Turns>0)!=true;
                    if(!Clear(to,destination,actor) || !BareOutlet() || !LiquidSourceSafety.IsUnmixedCell(zone,to.X,to.Y,stock.Liquid)
                        || new[]{actor.ID,tool.ID,source.ID}.Distinct().Count()!=3
                        || destination!=null && new[]{actor.ID,tool.ID,source.ID,destination.ID}.Distinct().Count()!=4)continue;
                    Entity output=destination;LiquidPoolPart outputPool=recipient?.Pool;PoolReceipt createdReceipt=null;
                    int initial=recipient?.Volume??0;bool credited=false,debited=false;
                    bool Before()=>Context() && SourceCurrent(stock.Volume)
                        && Clear(to,destination,actor) && BareOutlet()
                        && (recipient==null?!to.Occupants.Any(e=>e.HasPart<LiquidPoolPart>()):recipient.Current(initial))
                        && LiquidSourceSafety.IsUnmixedCell(zone,to.X,to.Y,stock.Liquid);
                    bool After()=>Context() && SourceCurrent(stock.Volume-amount)
                        && output!=null && (recipient??createdReceipt)?.Current(initial+amount)==true && zone.GetEntityCell(output)==to
                        && Clear(to,output,actor) && LiquidSourceSafety.IsUnmixedCell(zone,to.X,to.Y,stock.Liquid);
                    yield return new RouteWorkMenu.Entry{Actor=actor,Zone=zone,Cell=from,
                        Key="PumpLiquid|"+source.ID+"|"+to.X+"|"+to.Y,
                        Label="pump "+amount+" "+stock.Liquid+(tray?" from tray at ":" from ")+from.X+","+from.Y+" to "+to.X+","+to.Y,
                        Hint=RecoverAtOutlet + (tray ? " The tray stays. Oil remains hazardous." : " Outlet hazards remain."),
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
                                int room=debited?stock.RefundRoom():0;
                                if(credited && output.GetPart<LiquidPoolPart>()==outputPool && outputPool.ParentEntity==output && outputPool.LiquidId==stock.Liquid)
                                {recovered=Math.Min(Math.Min(amount,room),Math.Max(initial,outputPool.Volume)-initial);outputPool.Volume-=recovered;}
                                if(recovered>0)stock.Restore(recovered);
                                if(credited && recovered<amount)Diag.Record("liquid","PumpUndoCreditRetainedOrConsumed",actor,tool,new{amount,recovered,source=source.ID,outlet=output.ID});
                                if(destination==null && createdReceipt?.Current(0)==true && zone.GetEntityCell(output)==to)zone.RemoveEntity(output);
                            });
                            if(!Before())return false;
                            stock.Debit(amount);debited=true;outputPool.Volume+=amount;credited=true;
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
                            if(!tray && stock.Volume==amount && stock.Current(0) && zone.GetEntityCell(source)==from)zone.RemoveEntity(source);
                            ZoneRenderHooks.MarkCellDirty(from,"HandPump.Source");ZoneRenderHooks.MarkCellDirty(to,"HandPump.Outlet");
                            Diag.Record("liquid","GroundLiquidPumped",actor,tool,new{source=source.ID,outlet=output.ID,liquid=stock.Liquid,amount});
                            MessageLog.Add("You move "+amount+" "+stock.Liquid+" to "+to.X+","+to.Y+". The liquid remains there.");
                        }};
                }
            }
        }
        abstract class StockReceipt
        {
            internal readonly Entity Owner;
            internal readonly int Volume;
            internal readonly string Liquid;
            protected StockReceipt(Entity owner, int volume, string liquid)
            { Owner = owner; Volume = volume; Liquid = liquid; }
            internal abstract bool Current(int expected);
            internal abstract int RefundRoom();
            internal abstract void Debit(int amount);
            internal abstract void Restore(int amount);
        }

        sealed class TrayReceipt : StockReceipt
        {
            readonly ContainedLiquidSourcePart stock;
            readonly ContainerPart container;
            readonly Entity[] contents;
            readonly Entity actor;
            readonly Zone zone;
            readonly Cell cell;
            readonly Func<bool> identity;

            TrayReceipt(Entity owner, ContainedLiquidSourcePart stock, Entity actor, Zone zone)
                : base(owner, stock.Volume, stock.LiquidId)
            {
                this.stock = stock; this.actor = actor; this.zone = zone;
                cell = zone.GetEntityCell(owner);
                identity = RouteWorkMenu.Identity(owner);
                container = owner.GetPart<ContainerPart>();
                contents = container.Contents.ToArray();
            }

            static bool Released(Entity owner) => CinderholdOilRecovery.Payload(owner)
                && !SecondExplorationActions.Special(owner);

            internal static TrayReceipt Capture(Entity owner, Entity actor, Zone zone)
            {
                var stock = owner.GetPart<ContainedLiquidSourcePart>();
                return Released(owner) && stock?.Context(actor,zone)==true
                    ? new TrayReceipt(owner,stock,actor,zone) : null;
            }

            internal override bool Current(int expected) => identity() && Released(Owner) && stock.Context(actor,zone)
                && Owner.GetPart<ContainedLiquidSourcePart>()==stock && zone.GetEntityCell(Owner)==cell
                && stock.Volume==expected && stock.LiquidId==Liquid && container.Contents.SequenceEqual(contents);

            internal override int RefundRoom() => identity() && Released(Owner) && zone.GetEntityCell(Owner)==cell
                && Owner.GetPart<ContainedLiquidSourcePart>()==stock && stock.LiquidId==Liquid
                ? stock.Capacity-stock.Volume : 0;

            internal override void Debit(int amount) => stock.Volume-=amount;
            internal override void Restore(int amount) => stock.Volume=WaterQuantityUndo.Restore(stock.Volume,-amount,stock.Capacity);
        }

        sealed class PoolReceipt : StockReceipt
        {
            internal readonly LiquidPoolPart Pool;
            readonly Zone zone;readonly Cell cell;readonly Func<bool> identity;readonly Part[] passive;
            readonly IMaterialProcessPartPolicy[] policies;readonly object[] values;
            static readonly HashSet<Type> Types=new(){typeof(RenderPart),typeof(PhysicsPart),typeof(ExaminablePart),typeof(LiquidPoolPart),typeof(MaterialPart),typeof(ThermalPart),typeof(StatusEffectsPart)};
            PoolReceipt(Entity owner,Zone zone) : base(owner,owner.GetPart<LiquidPoolPart>().Volume,owner.GetPart<LiquidPoolPart>().LiquidId)
            {
                Pool=owner.GetPart<LiquidPoolPart>();this.zone=zone;cell=zone.GetEntityCell(owner);identity=RouteWorkMenu.Identity(owner);
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
            internal override bool Current(int expected)=>identity() && Plain(Owner,zone) && zone.GetEntityCell(Owner)==cell
                && Owner.GetPart<LiquidPoolPart>()==Pool && Pool.Volume==expected && Pool.LiquidId==Liquid
                && passive.Select((p,i)=>MaterialProcessPayloadPolicies.Get(p.GetType())==policies[i] && policies[i].Matches(p,values[i])).All(v=>v);
            internal override int RefundRoom() => Owner.GetPart<LiquidPoolPart>()==Pool && Pool.ParentEntity==Owner
                && Pool.LiquidId==Liquid && Pool.Volume>=0 ? int.MaxValue-Pool.Volume : 0;
            internal override void Debit(int amount) => Pool.Volume-=amount;
            internal override void Restore(int amount) => Pool.Volume=WaterQuantityUndo.Restore(Pool.Volume,-amount);
        }
    }
}
