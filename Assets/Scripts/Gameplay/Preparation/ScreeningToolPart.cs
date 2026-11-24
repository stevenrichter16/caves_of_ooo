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
            guide.How.Add("Carry this sound hand sieve. Choose a packet of salt-bearing quarry sweepings in your pack, then screen it.");
            guide.Needs.Add("one actual mixed packet; room for its declared fractions");
            guide.Limits.Add("One action spends the packet and keeps this same sieve. Recover one pale-salt and two silver sand; two weight of unusable rock dust is discarded. No yield from an ordinary ore-cache description, ash or reeds.");
            guide.WhyNot=MaterialScreeningWorkReceipt.Usable(context.Viewer,ParentEntity,context.Zone)
                ?"choose a quarry-sweepings packet in your pack to see the exact preparation"
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
        static bool Available(Entity actor,Entity source,Entity tool,Zone zone)
        {
            if(!RouteWorkMenu.ActorReady(actor,zone) || tool==null || tool==source || tool==actor
                || tool.ID==actor.ID || tool.ID==source?.ID || !WorldResourceActions.Carried(actor,tool)
                || actor.GetPart<InventoryPart>().Objects.Contains(tool)!=true || tool.GetPart<PhysicsPart>() is not PhysicsPart physics
                || physics.Solid || physics.Weight<0 || !tool.HasTag("Item") || tool.HasTag("Creature") || tool.HasTag("Terrain")
                || tool.HasPart<StackerPart>() || tool.HasPart<SpatialFootprintPart>() || tool.HasPart<MultiCellPilotPropPart>()
                || tool.GetPart<ScreeningToolPart>()?.ParentEntity!=tool || tool.Parts.Count(p=>p is ScreeningToolPart)!=1
                || tool.Parts.Count(p=>p is PhysicsPart)!=1 || tool.HasEffect<BrokenEffect>() || tool.HasEffect<FrozenEffect>()
                || RepairablePart.BlocksFunction(tool) || CombatSystem.IsDeathHandled(tool)
                || tool.GetStat("Hitpoints")!=null && tool.GetStatValue("Hitpoints")<=0
                || tool.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP<=0))return false;
            return !zone.GetReadOnlyEntities().Any(e=>e.ID==tool.ID);
        }
        internal static bool Usable(Entity actor,Entity tool,Zone zone)=>Available(actor,null,tool,zone);
        internal static MaterialScreeningWorkReceipt Resolve(Entity actor,Entity source,Zone zone)
        {
            var tool=actor?.GetPart<InventoryPart>()?.Objects.Where(e=>Available(actor,source,e,zone))
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
}
