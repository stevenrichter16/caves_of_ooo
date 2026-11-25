using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Explicit suitable-grit capability. No inferred material yield,
    /// saved job or creation work; only the admitted carried unit is spent.</summary>
    public sealed class GritMaterialPart : Part,IDescribeUse,IDescribeUseRows,IInventoryActionReceiptOwner
    {
        public override string Name=>"GritMaterial";
        public int Order=>40;
        const string Prefix="ScatterGritMaterial|";
        public bool OwnsInventoryReceipt(string command)=>ParentEntity?.GetPart<GritMaterialPart>()==this && command?.StartsWith(Prefix,StringComparison.Ordinal)==true;
        static ZoneTileState.Layer Layer(Zone zone,Cell cell)=>zone.TileState.Get(cell.X,cell.Y)?.Residues
            .FirstOrDefault(r=>r.Id==CombatUtilityActions.GritResidue && string.IsNullOrEmpty(r.ProjectionOwnerId));
        static bool Target(Entity actor,Zone zone,Cell cell)
        {
            if(!RouteWorkMenu.Visible(cell) || cell.ParentZone!=zone || cell.BlocksMovement(actor))return false;
            var layers=zone.TileState.Get(cell.X,cell.Y)?.Residues.Where(r=>r.Id==CombatUtilityActions.GritResidue).ToArray();
            return layers==null || layers.Length==0 || layers.Length==1 && string.IsNullOrEmpty(layers[0].ProjectionOwnerId)
                && layers[0].Turns<CombatUtilityActions.GritTurns;
        }
        IEnumerable<RouteWorkMenu.Entry> Choices(Entity actor,Zone zone)
        {
            var item=ParentEntity;var factory=HarvestablePart.Factory;
            if(!RouteWorkMenu.ActorReady(actor,zone) || !WorldResourceActions.Carried(actor,item,false) || !actor.GetPart<InventoryPart>().Objects.Contains(item) || factory==null
                || item.GetPart<GritMaterialPart>()!=this || item.Parts.Count(p=>p is GritMaterialPart)!=1)yield break;
            var payload=MaterialProcessTemplate.Capture(factory,item.BlueprintName)?.Snapshot(item,true);if(payload==null)yield break;
            var pack=actor.GetPart<InventoryPart>();var physics=item.GetPart<PhysicsPart>();var stack=item.GetPart<StackerPart>();
            int count=stack?.StackCount??1;var origin=zone.GetEntityCell(actor);string actorId=actor.ID,zoneId=zone.ZoneID;
            bool Context()=>RouteWorkMenu.ActorReady(actor,zone) && actor.ID==actorId && zone.ZoneID==zoneId
                && zone.GetEntityCell(actor)==origin && actor.GetPart<InventoryPart>()==pack && HarvestablePart.Factory==factory
                && item.GetPart<GritMaterialPart>()==this && payload.Matches() && !zone.GetReadOnlyEntities().Any(e=>e.ID==item.ID);
            bool Paid()=>Context() && physics.Equipped==null && (count>1?WorldResourceActions.Carried(actor,item,false) && pack.Objects.Contains(item) && stack.StackCount==count-1
                :item.SpatialZone==null && physics.InInventory==null && (stack?.StackCount??1)==1
                    && !pack.Objects.Any(e=>e==item || e?.ID==item.ID) && !pack.EquippedItems.Values.Any(e=>e==item || e?.ID==item.ID)
                    && pack.FindEquippedBodyPart(item)==null);
            foreach(var d in new[]{(x:0,y:0,name:"here"),(x:0,y:-1,name:"north"),(x:1,y:-1,name:"northeast"),(x:1,y:0,name:"east"),
                (x:1,y:1,name:"southeast"),(x:0,y:1,name:"south"),(x:-1,y:1,name:"southwest"),(x:-1,y:0,name:"west"),(x:-1,y:-1,name:"northwest")})
            {
                var at=zone.GetCell(origin.X+d.x,origin.Y+d.y);if(!Target(actor,zone,at))continue;
                ZoneTileState.Layer staged=null;
                bool Before()=>Context() && WorldResourceActions.Carried(actor,item,false) && pack.Objects.Contains(item) && (stack?.StackCount??1)==count && Target(actor,zone,at);
                bool After()=>Paid() && RouteWorkMenu.Visible(at) && !at.BlocksMovement(actor) && staged!=null
                    && ReferenceEquals(Layer(zone,at),staged) && staged.Turns==CombatUtilityActions.GritTurns;
                yield return new RouteWorkMenu.Entry{Actor=actor,Zone=zone,Cell=at,Key=Prefix+at.X+","+at.Y,
                    Label="scatter grit "+d.name+" (1 unit)",
                    Hint="12 turns of shared firm footing. Oil, ice, acid and other contact hazards remain.",Before=Before,After=After,
                    Apply=tx=>
                    {
                        if(!Before() || !MaterialProcessService.ReserveInput(item,tx)
                            || !MaterialProcessPayment.Spend(actor,item,actor,zone,tx,payload.Matches) || !Paid() || !Target(actor,zone,at))return false;
                        var previous=Layer(zone,at);int turns=previous?.Turns??0;
                        tx.Do(null,()=>
                        {
                            var current=Layer(zone,at);
                            if(staged==null || !ReferenceEquals(current,staged) || current.Turns!=CombatUtilityActions.GritTurns)return;
                            if(previous==null)zone.TileState.RemoveResidue(at.X,at.Y,CombatUtilityActions.GritResidue);
                            else {current.Turns=turns;zone.TileState.WriteResidue(at.X,at.Y,CombatUtilityActions.GritResidue,turns);}
                        });
                        // The out receipt is assigned before synchronous tile
                        // observers run, so an independent replacement is not ours.
                        zone.TileState.WriteResidue(at.X,at.Y,CombatUtilityActions.GritResidue,CombatUtilityActions.GritTurns,out staged);
                        return After();
                    },Publish=()=>
                    {
                        ZoneRenderHooks.MarkCellDirty(at,"GritMaterial.Scattered");
                        Diag.Record("event","GritMaterialUsed",actor,item,new{blueprint=item.BlueprintName,x=at.X,y=at.Y,spent=1,turns=CombatUtilityActions.GritTurns});
                        MessageLog.Add("You scatter grit for firm footing. Other ground hazards remain.");
                    }};
            }
        }
        public override bool HandleEvent(GameEvent e)
        {
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions")RouteWorkMenu.Rows(ParentEntity,actor,zone,e.GetParameter<InventoryActionList>("Actions"),Choices(actor,zone),true);
            if(e.ID=="InventoryAction" && OwnsInventoryReceipt(e.GetStringParameter("Command")))
            {
                if(RouteWorkMenu.Act(ParentEntity,e)){e.Handled=true;return false;}
                Diag.Record("event","GritMaterialRejected",actor,ParentEntity,new{command=e.GetStringParameter("Command"),reason="stale-or-unavailable-selection"});
            }
            return true;
        }
        public void AddUseRows(Entity actor,Zone zone,InventoryActionList rows)=>RouteWorkMenu.Rows(ParentEntity,actor,zone,rows,Choices(actor,zone),false);
        public void DescribeUse(DescribeContext context,UseGuide guide)
        {
            guide.How.Add("Carry this suitable grit; scatter one unit here or onto visible adjacent clear ground.");
            guide.Needs.Add("one actual carried unit; an available ground surface");
            guide.Limits.Add("One action gives 12 turns of firm footing to ordinary movers. It prevents slipping, not liquid contact, heat or other hazards. No grit is recovered afterward.");
            guide.WhyNot="carry plain suitable grit beside visible clear ground without a longer grit layer";
        }
    }
}
