using CavesOfOoo.Core;
namespace CavesOfOoo.Scenarios.Custom
{
    [Scenario("Grit material and ordinary supply",category:"Home",description:"Normal player and fresh merchant stock; no grit granted. A separately supplied finite-duration oil film supports a traction trial after an ordinary purchase.")]
    public sealed class GritMaterialShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var at=ctx.Zone.GetEntityCell(ctx.PlayerEntity);var east=ctx.Zone.GetCell(at.X+1,at.Y);
            if(east!=null && !east.BlocksMovement(ctx.PlayerEntity))ctx.Zone.TileState.WriteCoating(east.X,east.Y,"oil",120);
            else ctx.Log("[Grit trial] The eastern surface is blocked; no oil film was supplied.");
            ctx.Log("[Grit trial] Fresh Morrowfast menders offer finite opening grit; this starting region may have none. Inspect grit, scatter one unit beside the supplied eastern oil film, then walk onto that surface and save. The film lasts120turns and stays oily beneath the grit. This is a supplied hazard trial, not a natural oil-source or general sieve demonstration.");
        }
    }
    [Scenario("Grit material supplied pickup",category:"Home",description:"Separate supplied-input trial: two ground grit units and a finite-duration oil film. Native pickup, scatter, movement and saves; no ordinary acquisition claim.")]
    public sealed class GritMaterialSuppliedShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            new GritMaterialShowcase().Apply(ctx);
            var at=ctx.Zone.GetEntityCell(ctx.PlayerEntity);var grit=ctx.Factory.CreateEntity("CoarseGrit");
            if(grit==null){ctx.Log("[Supplied grit trial] Grit blueprint missing; no input supplied.");return;}
            grit.GetPart<StackerPart>().StackCount=2;
            if(!ctx.Zone.AddEntity(grit,at.X,at.Y)){ctx.Log("[Supplied grit trial] Ground input could not be placed.");return;}
            ctx.Log("[Supplied grit trial] Two grit units are explicitly supplied on the ground here. Pick them up, inspect and scatter beside the supplied oil film. This separate branch demonstrates field use, not the mender's ordinary supply or general screening.");
        }
    }
}
