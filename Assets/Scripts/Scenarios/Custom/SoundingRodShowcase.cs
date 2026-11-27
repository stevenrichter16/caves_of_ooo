using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Builders;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Hand-play fixture for the sounding rod and its survey log (Docs/MACHINE-INVENTED-USES.md 3g): the fresh game starts one floor down, in an ordinary cave under wild Spread ground
    /// (requested through <c>ScenarioRunner.LaunchWild(BiomeType.Spread, 1)</c>), where the rod can sound, with the rod in the pack. It grants one item only: every verb afterwards is the ordinary one.
    /// </summary>
    [Scenario(
        name: "Sounding Rod (an ordinary cave)",
        category: "Home",
        description: "Starts one floor down in an ordinary cave with a sounding rod: sound the ground, read what it names, walk to check it, then read the survey log.")]
    public class SoundingRodShowcase : IScenario
    {
        public static readonly (string Blueprint, int Count)[] Loadout = { ("SoundingRod", 1) };

        public void Apply(ScenarioContext ctx)
        {
            ScenarioLoadout.Give(ctx, Loadout, "[Sounding]");
            if (WorldMap.GetDepth(ctx.Zone?.ZoneID) <= 0) { ctx.Log("[Sounding] The start did not start you underground (the cave start could not be placed), and the rod refuses on the surface. Take any stairs down into an ordinary cave to use it; the rod is in your pack."); return; }
            ctx.Log("[Sounding] You are in an ordinary cave one floor below wild ground, with a sounding rod in your pack.");
            ctx.Log("[Sounding] 1. Open the pack and choose 'sound the ground'. The row gives the survey reach. The strike can draw nearby creatures that hunt by sound. The rod gives bearings and rough distances to unseen open ground, bores through stone and dense rock. 2. Walk toward a lead to check it. 3. Sound again somewhere else, then choose 'read the survey log' to reread your last eight soundings with where you stood. 4. Climb the stairs and try the rod on the surface: the ground there is too loose to carry a sounding.");
        }
    }
}
