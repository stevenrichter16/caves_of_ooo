using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    public sealed class ScreeningToolPart : Part, IDescribeUse
    {
        public override string Name=>"ScreeningTool";
        public int Order=>35;
        public void DescribeUse(DescribeContext context,UseGuide guide)
        {
            guide.How.Add("Carry this sieve and examine a known packet to see what screening yields.");
            guide.How.Add("Sort clay spoil for lining repairs and grit for footing. Quarry sweepings can supply pale-salt for weapon infusion and silver sand for trail marks.");
            guide.How.Add("Away from a public seed frame, rub one gathered knotflax bundle over the sieve to keep a seed. The fixed frame can also save chaff in fair weather.");
            guide.How.Add("On a dry floor under cover, use carried bellows and the sieve together to keep both the seed and the chaff.");
            guide.Needs.Add("An identified packet or gathered knotflax bundle and room for what you keep.");
            guide.Limits.Add("One action spends one packet or bundle and keeps the sieve. The mesh passes fine and medium stock and retains coarse stock. Unlike materials of the same size stay mixed.");
            guide.WhyNot=MaterialScreeningWorkReceipt.Usable(context.Viewer,ParentEntity,context.Zone)
                ?"choose a known stock, quarry-sweepings packet or gathered knotflax bundle for its exact preview"
                :"carry one sound, unfrozen sieve as an actual pack item";
        }
    }
    internal sealed class MaterialScreeningWorkReceipt : IMaterialWorkReceipt
    {
        public Entity Tool { get; }
        readonly Entity actor,source;
        readonly Zone zone;
        readonly InventoryPart pack;
        readonly PhysicsPart physics;
        readonly ScreeningToolPart capability;
        readonly Func<bool> identity;
        readonly int weight;
        MaterialScreeningWorkReceipt(Entity actor,Entity source,Entity tool,Zone zone)
        {
            this.actor=actor;this.source=source;Tool=tool;this.zone=zone;
            pack=actor.GetPart<InventoryPart>();physics=tool.GetPart<PhysicsPart>();
            capability=tool.GetPart<ScreeningToolPart>();identity=RouteWorkMenu.Identity(tool);weight=physics.Weight;
        }
        internal static bool IsSoundCarriedTool(Entity actor, Entity source, Entity tool, Zone zone)
        {
            if(!RouteWorkMenu.ActorReady(actor,zone) || tool==null || tool==source || tool==actor
                || tool.ID==actor.ID || tool.ID==source?.ID || !WorldResourceActions.Carried(actor,tool)
                || actor.GetPart<InventoryPart>().Objects.Contains(tool)!=true || !IsSoundTool(tool)) return false;
            return !zone.GetReadOnlyEntities().Any(e=>e.ID==tool.ID);
        }
        internal static bool IsSoundTool(Entity tool)
        {
            if(tool?.GetPart<PhysicsPart>() is not PhysicsPart physics
                || physics.Solid || physics.Weight<0 || !tool.HasTag("Item") || tool.HasTag("Creature") || tool.HasTag("Terrain")
                || tool.HasPart<StackerPart>() || tool.HasPart<SpatialFootprintPart>() || tool.HasPart<MultiCellPilotPropPart>()
                || tool.Parts.Count(p=>p is PhysicsPart)!=1 || tool.HasEffect<BrokenEffect>() || tool.HasEffect<FrozenEffect>()
                || RepairablePart.BlocksFunction(tool) || CombatSystem.IsDeathHandled(tool)
                || tool.GetStat("Hitpoints")!=null && tool.GetStatValue("Hitpoints")<=0
                || tool.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP<=0))return false;
            return true;
        }
        internal static bool HasScreeningCapability(Entity tool)
            => tool?.GetPart<ScreeningToolPart>() is ScreeningToolPart sieve && sieve.ParentEntity == tool
                && tool.Parts.Count(p => p is ScreeningToolPart) == 1;
        static bool Available(Entity actor, Entity source, Entity tool, Zone zone)
            => HasScreeningCapability(tool) && IsSoundCarriedTool(actor, source, tool, zone);
        internal static bool Usable(Entity actor,Entity tool,Zone zone)=>Available(actor,null,tool,zone);
        internal static MaterialScreeningWorkReceipt Resolve(Entity actor, Entity source, Zone zone, Predicate<Entity> accepts = null)
        {
            var tool=actor?.GetPart<InventoryPart>()?.Objects.Where(e=>Available(actor,source,e,zone) && (accepts == null || accepts(e)))
                .OrderBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();
            return tool==null?null:new MaterialScreeningWorkReceipt(actor,source,tool,zone);
        }
        public bool Current()=>actor.GetPart<InventoryPart>()==pack && identity() && Tool.GetPart<PhysicsPart>()==physics
            && physics.Weight==weight && Tool.GetPart<ScreeningToolPart>()==capability && Available(actor,source,Tool,zone);
        public bool Begin(InventoryTransaction transaction,string command)
        {
            if(transaction==null || !Current() || !transaction.TryClaim(Tool,actor,command))return false;
            transaction.BeforeCommit(Current);return true;
        }
    }

    internal sealed class ShelteredWinnowWorkReceipt : IMaterialWorkReceipt
    {
        public Entity Tool => screening.Tool;
        internal Entity AirTool { get; }
        readonly MaterialScreeningWorkReceipt screening;
        readonly Entity actor, source;
        readonly Zone zone;
        readonly Cell cell;
        readonly Func<bool> airIdentity;
        readonly int airWeight;

        ShelteredWinnowWorkReceipt(Entity actor, Entity source, Zone zone, Cell cell,
            MaterialScreeningWorkReceipt screening, Entity airTool)
        {
            this.actor = actor;
            this.source = source;
            this.zone = zone;
            this.cell = cell;
            this.screening = screening;
            AirTool = airTool;
            airIdentity = RouteWorkMenu.Identity(airTool);
            airWeight = airTool.GetPart<PhysicsPart>().Weight;
        }

        static bool Available(Entity actor, Entity source, Entity airTool, Zone zone)
            => airTool != null && airTool.GetPart<DirectedAirPart>()?.ParentEntity == airTool
                && airTool.Parts.Count(part => part is DirectedAirPart) == 1
                && MaterialScreeningWorkReceipt.IsSoundCarriedTool(actor, source, airTool, zone);

        static bool Distinct(Entity sieve, Entity airTool) => airTool != sieve && airTool.ID != sieve.ID;

        internal static ShelteredWinnowWorkReceipt Resolve(Entity actor, Entity source, Zone zone)
        {
            var cell = zone.GetEntityCell(actor);
            if (cell?.IsInterior != true || !SeedFramePart.IsDryWorkCell(zone, cell)) return null;
            var airTools = actor.GetPart<InventoryPart>().Objects
                .Where(item => Available(actor, source, item, zone)).OrderBy(item => item.ID, StringComparer.Ordinal).ToArray();
            if (airTools.Length == 0) return null;
            var screening = MaterialScreeningWorkReceipt.Resolve(actor, source, zone, sieve => airTools.Any(air => Distinct(sieve, air)));
            if (screening == null) return null;
            var airTool = airTools.First(air => Distinct(screening.Tool, air));
            return new ShelteredWinnowWorkReceipt(actor, source, zone, cell, screening, airTool);
        }

        public bool Current()
            => zone.GetEntityCell(actor) == cell && cell.IsInterior && SeedFramePart.IsDryWorkCell(zone, cell)
                && screening.Current() && airIdentity() && Distinct(Tool, AirTool) && Available(actor, source, AirTool, zone)
                && AirTool.GetPart<PhysicsPart>().Weight == airWeight;

        public bool Begin(InventoryTransaction transaction, string command)
        {
            if (transaction == null || !Current() || !screening.Begin(transaction, command)
                || !transaction.TryClaim(AirTool, actor, command)) return false;
            transaction.BeforeCommit(Current);
            return true;
        }
    }
}
