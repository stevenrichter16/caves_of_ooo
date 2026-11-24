using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Builders;
namespace CavesOfOoo.Scenarios.Custom
{
    [Scenario("Hand sieve",category:"Home",description:"Supplied construction materials, no sieve or sweepings granted. Look for an actual newly stocked ordinary ore cache; native pickup, screening and use after setup.")]
    public sealed class HandSieveShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ScenarioLoadout.Give(ctx,new[]{("IronSpikeComponent",1),("SalvagedTimber",2),("KnotflaxCord",2)},"[Sieve trial]");
            ctx.Spawn("TinkersForge").AdjacentToPlayer();
            ctx.Log("[Sieve trial] Supplied construction materials, no finished sieve or sweepings. Make a sieve at the nearby forge. Newly stocked ordinary tier-one ore caches can contain a finite packet; existing saved caches are not refilled. Find and take a real packet, screen its two fractions, then use them through ordinary preparations or trail work.");
        }
    }

    [Scenario("Hand sieve supplied packet",category:"Home",description:"Separate supplied-input trial: construction materials and two finite mixed packets, no finished sieve. This does not demonstrate ordinary acquisition.")]
    public sealed class HandSieveSuppliedShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            new HandSieveShowcase().Apply(ctx);
            ScenarioLoadout.Give(ctx,new[]{("QuarrySweepings",2)},"[Supplied screening trial]");
            ctx.Log("[Supplied screening trial] Two finite quarry-sweepings packets are explicitly supplied. Forge the sieve, walk away from the forge, inspect and cancel a preparation, screen one packet and use its silver sand for a trail mark. Keep the second packet to demonstrate reuse. This is a separate supplied branch, not an ordinary-find demonstration.");
        }
    }
}
