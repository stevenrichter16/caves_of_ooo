namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Showcase for Phase 2a's <c>.InRing</c> positioning primitive.
    /// Spawns 8 marlbacks evenly distributed on a circle of radius 4 around the
    /// player — a more visually uniform ambush than the hand-placed
    /// <c>FiveMarlbackScrabblerAmbush</c>.
    ///
    /// Use this to stress-test:
    /// - ranged mutation AoE (do all 8 get hit cleanly?)
    /// - retreat/flee pathfinding with threats from every direction
    /// - the AIBored → KillGoal acquisition loop under maximum aggro pressure
    /// </summary>
    [Scenario(
        name: "MarlbackScrabbler Ring Ambush (x8)",
        category: "Combat Stress",
        description: "8 marlbacks evenly distributed on a radius-4 ring around the player.")]
    public class MarlbackScrabblerRingAmbush : IScenario
    {
        private const int Count = 8;
        private const int Radius = 4;

        public void Apply(ScenarioContext ctx)
        {
            for (int i = 0; i < Count; i++)
            {
                ctx.Spawn("MarlbackScrabbler").InRing(Radius, i, Count);
            }
            ctx.Log($"MarlbackScrabbler Ring Ambush applied: {Count} hostiles at r={Radius}.");
        }
    }
}
