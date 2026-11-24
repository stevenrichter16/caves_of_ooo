using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Builders;
namespace CavesOfOoo.Scenarios.Custom
{
    [Scenario("Cargo sled",category:"Home",description:"Supplied reclaimed timber, cord and spike for a local cargo sled. Ordinary-strength native forging, hauling and same-sled recovery; not ordinary acquisition or balance evidence.")]
    public sealed class CargoSledShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ScenarioLoadout.Give(ctx,new[]{("IronSpikeComponent",1),("SalvagedTimber",2),("KnotflaxCord",2)},"[Sled trial]");
            ctx.PlayerEntity.GetStat("Strength").BaseValue=18;
            ctx.Spawn("TinkersForge").AtPlayerOffset(-1,-1);
            ctx.Spawn("GleanersTimberPallet").AtPlayerOffset(1,0);
            ctx.Log("[Sled trial] Supplied materials, Strength18. Make the sled at the forge northwest. It adds six weight beneath the eastern pallet and halves hauling slowdown. Fit it, use the pallet's ordinary haul command, move away, release and recover the same sled. The moved pallet can then be dismantled for its existing finite timber output.");
        }
    }
}
