using CavesOfOoo.Data;
namespace CavesOfOoo.Core
{
    /// <summary>Optional fresh-world acquisition only. Processing itself has no
    /// site restriction and can use any currently ready, supported forge.</summary>
    internal static class CinderholdReclamation
    {
        internal const string Blueprint="ReleasedExtractionTool";
        internal static Entity CreateFresh(EntityFactory factory)
        {
            if(MaterialProcessRegistry.Validate(factory).Count!=0)return null;
            var source=MaterialProcessTemplate.Capture(factory,Blueprint,true);
            if(source==null)return null;
            foreach(var dependency in new[]{"RecoveredIronBlank","CinderhookAxeHeadComponent","FieldHaftComponent","SalvagedTimber","IronSpikeComponent"})
                if(MaterialProcessTemplate.Capture(factory,dependency,true)==null)return null;
            var item=CinderholdCompositionBuilder.Create(factory,Blueprint);
            return item!=null && PreparationActions.Fresh(item,Blueprint) && source.Matches(item)
                && item.GetPart<PhysicsPart>().Weight==5 && item.GetPart<CommercePart>()?.Value==46
                && item.GetPart<MaterialProcessInputPart>()?.ProcessIds=="dismantle,reclaim"?item:null;
        }
    }
}
