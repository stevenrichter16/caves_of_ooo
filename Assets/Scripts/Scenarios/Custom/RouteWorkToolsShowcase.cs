using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Builders;

namespace CavesOfOoo.Scenarios.Custom
{
    [Scenario("Route work tools",category:"Home",description:"Supplied materials for bellows, finite sand marks and a removable door spike. Ordinary native actions only after setup; not an ordinary acquisition or balance claim.")]
    public sealed class RouteWorkToolsShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ScenarioLoadout.Give(ctx,new[]{("IronSpikeComponent",2),("OakHaftComponent",1),("LeatherBindingComponent",1),("SilverSand",4),("PoisonGasGrenade",2)},"[Route-work trial]");
            var hp=ctx.PlayerEntity.GetStat("Hitpoints");hp.Max=400;hp.BaseValue=400;ctx.PlayerEntity.GetStat("Strength").BaseValue=70;
            ctx.Spawn("TinkersForge").AtPlayerOffset(-1,-1);ctx.Spawn("VillageDoor").AtPlayerOffset(1,0);
            ctx.Log("[Route-work trial] Supplied materials, 400 HP and Strength70. No bellows granted: make them at the forge northwest with one spike, haft and binding. The second spike can brace the ordinary door east: close it first and split the spike stack if needed.");
            ctx.Log("[Route-work trial] Use silver sand on dry bare floor to leave your own route notes. Remove a door brace to recover that same spike. A supplied poison grenade can create ordinary transient gas for the bellows trial: the gas is real and can poison you; bellows only move it, and normal drift remains.");
        }
    }
}
