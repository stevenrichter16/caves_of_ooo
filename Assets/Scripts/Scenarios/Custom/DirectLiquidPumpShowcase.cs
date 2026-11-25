using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Builders;
namespace CavesOfOoo.Scenarios.Custom
{
    [Scenario("Direct liquid pump",category:"Home",description:"Supplied construction stock and one finite60-unit acid pool. No finished pump. Forge, inspect and relocate stock; verify the former hazard clears while the outlet stays acid.")]
    public sealed class DirectLiquidPumpShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ScenarioLoadout.Give(ctx,new[]{("IronSpikeComponent",1),("FireClay",2),("LeatherBindingComponent",2)},"[Pump trial]");
            ctx.Spawn("TinkersForge").AtPlayerOffset(0,-1);
            ctx.Spawn("AcidPool").AtPlayerOffset(1,0);
            ctx.Log("[Pump trial] Supplied materials and a60-unit acid pool east, no pump. Make the pump at the northern forge. Stand here to move three20-unit strokes from east to clear western floor. This relocates the acid: the emptied eastern site should become safe to step on, while the west remains hazardous. Readers, cancellation, movement and saves use ordinary native actions. This is supplied setup, not ordinary acquisition or a flooded-room demonstration.");
        }
    }
}
