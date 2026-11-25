using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Builders;
namespace CavesOfOoo.Scenarios.Custom
{
    [Scenario("General screening and finite clay spoil",category:"Home",description:"Declared fresh western Morrowfast start with ordinary player stats. Supplies sieve construction materials and a forge, never a sieve or mixed stock. Harvest the finite installed spoil, sort and use its actual products.")]
    public sealed class GeneralScreeningShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ScenarioLoadout.Give(ctx,new[]{("IronSpikeComponent",1),("SalvagedTimber",2),("KnotflaxCord",2)},"[General screening trial]");
            ctx.Spawn("TinkersForge").AdjacentToPlayer();
            ctx.Log("[General screening trial] This guided fresh start requests western Morrowfast, not a discovered route. Construction materials and the nearby forge are supplied; no sieve, spoil or products are granted. Make a sieve, then find the finite dry clay spoil beside the allotment's clay bank. Harvest one packet, carry it away and screen its two size grades. Use the clay to repair a well and the grit on a slippery surface. Ordinary clay-bank harvest remains an alternative. Save and continue to check the spent source stays spent.");
        }
    }
}
