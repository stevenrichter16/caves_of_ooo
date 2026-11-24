using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Builders;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Explicit supplied trial; never applied to continued saves.</summary>
    [Scenario("Field work tools",category:"Home",description:"Forge three reusable tools, direct finite hearth heat, discharge across a gap and catch finite vent water. Supplied materials and durable trial player; not an ordinary acquisition claim.")]
    public sealed class FieldWorkToolsShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var at=ctx.Zone.GetEntityCell(ctx.PlayerEntity);
            if(at==null)return;
            ScenarioLoadout.Give(ctx,new[]{("IronSpikeComponent",3),("FireClay",3),("KnotflaxCord",1),
                ("CampHearth",1),("SalvagedTimber",2),("FlintStriker",1),("SparkRoot",2),("LiquidFlask",3)},"[Field-work trial]");
            var hp=ctx.PlayerEntity.GetStat("Hitpoints");hp.Max=400;hp.BaseValue=400;
            ctx.PlayerEntity.GetStat("Strength").BaseValue=70;
            var forge=ctx.Spawn("TinkersForge").AtPlayerOffset(-1,-1);
            var pipe=ctx.Spawn("CopperPipe").AtPlayerOffset(2,0);
            var vent=ctx.Spawn("SteamVent").AtPlayerOffset(3,2);
            ctx.Zone.TileState.WriteCoating(at.X+1,at.Y-1,"ice",200);
            ctx.Log("[Field-work trial] Supplied materials, 400 HP and Strength 70 for a controlled trial. No tools granted: make all three from your iron spikes at the forge northwest. Ordinary acquisition and balance remain untested.");
            ctx.Log("[Field-work trial] Set down the hearth west, stoke it, then use the nozzle on the ice northeast. Load one spark-root into the rod; discharge toward the copper pipe two cells east. The steam vent is three east, two south; walk beside it, split an empty flask if stacked, and catch one water with the condenser.");
            if(forge==null || pipe==null || vent==null)ctx.Log("[Field-work trial] A trial fixture failed to spawn; inspect the actual scene before attempting its step.");
        }
    }
}
