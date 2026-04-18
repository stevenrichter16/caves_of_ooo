using CavesOfOoo.Scenarios;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Phase 1 demo scenario: surrounds the player with 5 snapjaws in a rough ring.
    /// Useful for rapidly stress-testing combat feel, FleeGoal triggers, and any
    /// AI behavior that depends on multi-threat situations.
    ///
    /// Uses <c>AtPlayerOffset</c> rather than absolute coordinates so the scenario
    /// works regardless of where the player spawns in their starting zone. If any
    /// offset lands on a non-passable cell (wall/furniture), that individual spawn
    /// is silently dropped — the rest still spawn.
    /// </summary>
    [Scenario(
        name: "Five Snapjaw Ambush",
        category: "Combat Stress",
        description: "Player surrounded by 5 snapjaws in a rough ring.")]
    public class FiveSnapjawAmbush : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Rough ring around the player — 5 cardinal-ish positions.
            // Individual spawns gracefully drop if any cell is blocked.
            ctx.Spawn("Snapjaw").AtPlayerOffset(2, 0);
            ctx.Spawn("Snapjaw").AtPlayerOffset(-2, 0);
            ctx.Spawn("Snapjaw").AtPlayerOffset(0, 2);
            ctx.Spawn("Snapjaw").AtPlayerOffset(0, -2);
            ctx.Spawn("Snapjaw").AtPlayerOffset(2, 2);

            ctx.Log("Five Snapjaw Ambush applied.");
        }
    }
}
