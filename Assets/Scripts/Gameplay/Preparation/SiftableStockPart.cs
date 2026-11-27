using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Finite known constituents per packet. The fixed hand mesh sorts
    /// size only; overlapping identities refuse rather than becoming pure goods.</summary>
    public sealed class SiftableStockPart : Part,IMaterialProcessPermission,IInventoryActionReceiptOwner,IDescribeUse,IDescribeUseRows,IDescribeState
    {
        public override string Name=>"SiftableStock";
        public string Contents="";
        internal const string ProcessId="screen_size_grades";
        internal const string LeaveGritId="screen_size_grades_leave_grit";
        public int Order=>36;
        public ReaderScope Scope => ReaderScope.Both;
        public string DescribeState(DescribeContext context) => context?.Owner != null && context.Owner == ParentEntity
            ? MaterialProcessActions.DescribeScreening(ParentEntity, this, Definition(ForgePart.Factory)) : null;
        void Remember()=>MaterialProcessRegistry.RememberPayload(ParentEntity?.BlueprintName);
        public override void OnAfterLoad(SaveReader reader)=>Remember();
        public bool OwnsInventoryReceipt(string command)=>ParentEntity?.GetPart<SiftableStockPart>()==this
            && (command?.StartsWith(MaterialProcessInputPart.CommandPrefix+ProcessId+"|",StringComparison.Ordinal)==true
                || command?.StartsWith(MaterialProcessInputPart.CommandPrefix+LeaveGritId+"|",StringComparison.Ordinal)==true);
        bool IMaterialProcessPermission.Allows(MaterialProcessDefinition d)=>d!=null && d.Id==ProcessId && d.Mode=="Process"
            && d.WorkKind=="Screening" && d.InputCount==1 && d.SourceBlueprint==ParentEntity?.BlueprintName
            && ParentEntity.GetPart<SiftableStockPart>()==this && ParentEntity.Parts.Count(p=>p is SiftableStockPart)==1;
        public override bool HandleEvent(GameEvent e)
        {
            // Bookkeeping remembers protection after permission removal/revocation;
            // it grants no process, item, currency or world-state output.
            if(e.ID=="ObjectCreated"){Remember();return true;}
            return MaterialProcessActions.Handle(ParentEntity,e,new[]{ProcessId},this);
        }
        internal MaterialProcessDefinition Definition(EntityFactory factory)
        {
            var owner=ParentEntity;
            if(owner?.GetPart<SiftableStockPart>()!=this || owner.Parts.Count(p=>p is SiftableStockPart)!=1
                || string.IsNullOrEmpty(Contents) || Contents.Length>512 || !MaterialProcessRegistry.Portable(factory,owner.BlueprintName))return null;
            var terms=Contents.Split(';');if(terms.Length<2 || terms.Length>4)return null;
            var passing=new Dictionary<string,int>(StringComparer.Ordinal);var retained=new Dictionary<string,int>(StringComparer.Ordinal);
            int units=0;long mass=0;
            foreach(string term in terms)
            {
                var fields=term.Split(':');
                if(fields.Length!=3 || !int.TryParse(fields[1],NumberStyles.None,CultureInfo.InvariantCulture,out int count)
                    || !int.TryParse(fields[2],NumberStyles.None,CultureInfo.InvariantCulture,out int band)
                    || count<1 || count>64 || band<0 || band>2 || (units+=count)>64)return null;
                string bp=fields[0];
                if(bp==owner.BlueprintName || !MaterialProcessRegistry.Portable(factory,bp)
                    || factory.Blueprints[bp].Parts.ContainsKey("SiftableStock")
                    || MaterialProcessTemplate.Capture(factory,bp,true)==null)return null;
                int weight=Weight(factory,bp);if(weight<0)return null;mass+=(long)weight*count;
                var group=band<=1?passing:retained;group.TryGetValue(bp,out int previous);group[bp]=previous+count;
            }
            if(passing.Count!=1 || retained.Count!=1 || passing.Keys.Single()==retained.Keys.Single()
                || mass!=Weight(factory,owner.BlueprintName))return null;
            var row=new MaterialProcessRow{Id=ProcessId,SourceBlueprint=owner.BlueprintName,Mode="Process",WorkKind="Screening",InputCount=1,
                OutputBlueprints=new[]{passing.Keys.Single(),retained.Keys.Single()},OutputCounts=new[]{passing.Values.Single(),retained.Values.Single()},
                ActionText="screen size grades",LossText="keep all material"};
            return MaterialProcessRegistry.Valid(row)?new MaterialProcessDefinition(row):null;
        }
        internal static int Weight(EntityFactory factory,string name)
        {
            if(!factory.Blueprints.TryGetValue(name,out var bp) || !bp.Parts.TryGetValue("Physics",out var p)
                || !p.TryGetValue("Weight",out string raw) || !int.TryParse(raw,NumberStyles.Integer,CultureInfo.InvariantCulture,out int weight))return -1;
            // Match HandlingService's shell rule for these audited plain packets
            // and commodities. A positive handling override is actual carried mass.
            if(bp.Parts.TryGetValue("Handling",out var handling) && handling.TryGetValue("Weight",out var overrideRaw))
            {if(!int.TryParse(overrideRaw,NumberStyles.Integer,CultureInfo.InvariantCulture,out int overrideWeight))return -1;if(overrideWeight>0)weight=overrideWeight;}
            return weight;
        }
        internal static int SingleCoarseGritWeight(MaterialProcessPlan plan)
        {
            var result = plan.Definition;
            if (result.Id != ProcessId || result.OutputBlueprints.Count != 2 || result.OutputCounts[1] != 1
                || !plan.Factory.Blueprints.TryGetValue(result.OutputBlueprints[1], out var coarse)
                || !coarse.Parts.ContainsKey("GritMaterial")) return 0;
            return Math.Max(0, Weight(plan.Factory, coarse.Name));
        }

        internal static Cell GritDestination(MaterialProcessPlan plan)
        {
            if (SingleCoarseGritWeight(plan) == 0) return null;
            var cell = plan.Zone.GetEntityCell(plan.Actor);
            if (!RouteWorkMenu.Visible(cell) || cell.ParentZone != plan.Zone || cell.BlocksMovement(plan.Actor)
                || cell.Objects.Any(item => item.GetPart<LiquidPoolPart>() is { Volume: > 0 })
                || plan.Zone.TileState.Get(cell.X, cell.Y)?.Coatings.Any(layer => layer.Turns > 0) == true)
                return null;
            return cell;
        }

        // The guide describes the packet without opening its executable menu.
        void IDescribeUseRows.AddUseRows(Entity actor, Zone zone, InventoryActionList rows) { }

        public void DescribeUse(DescribeContext context,UseGuide guide)
        {
            guide.How.Add("Carry this known mixed stock and a sound hand sieve; choose screen size grades.");
            if (ParentEntity?.BlueprintName == RainClaySeamSite.Packet)
                guide.How.Add("Check Orrit's rope shop in Morrowfast for a sieve, or make one at a ready forge from an iron spike, two salvaged timber and two knotflax cord.");
            guide.Needs.Add("One packet, a sound carried sieve and pack space for the material you keep.");
            guide.Limits.Add("One action keeps the same sieve and separates fine/medium passing stock from coarse retained stock. Quantities and material identities stay the same. Equal-sized unlike materials refuse; screening does not identify, grind, dry or purify anything.");
            if (Definition(ForgePart.Factory) is {} result && result.OutputCounts[1] == 1
                && ForgePart.Factory.Blueprints[result.OutputBlueprints[1]].Parts.ContainsKey("GritMaterial"))
                guide.How.Add("On a clear surface you can leave the coarse grit at your feet for later pickup and carry the finer material.");
            guide.WhyNot="check the packet's action menu for tool, load and ground options";
        }
    }
}
