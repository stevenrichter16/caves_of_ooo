namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Phase-2-ready scenario that activates automatically once the Calm mutation
    /// ships in M2. Until then, <see cref="Builders.PlayerBuilder.AddMutation"/>'s
    /// fail-soft contract logs a warning and continues — the three Snapjaws still
    /// spawn, they're just not in a Calmable configuration yet.
    ///
    /// M2 readiness checklist:
    /// 1. <c>CalmMutation</c> exists as a <see cref="CavesOfOoo.Core.BaseMutation"/>
    ///    subclass reachable by <c>Type.Name</c> lookup
    /// 2. Registered in the mutation factory / reflection table
    /// 3. On activation, targets one enemy with a debuff that makes them ignore
    ///    the player for N turns
    ///
    /// When M2 ships, the warning disappears automatically on next scenario run.
    /// </summary>
    [Scenario(
        name: "Calm Test Setup",
        category: "Mutations",
        description: "Player with Calm + 3 hostile Snapjaws. Calm the first, fight the rest. Activates after M2.")]
    public class CalmTestSetup : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Will log "unknown class" warning until M2.2 lands; harmless.
            ctx.Player.AddMutation("CalmMutation", level: 3);

            ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);
            ctx.Spawn("Snapjaw").AtPlayerOffset(5, 0);
            ctx.Spawn("Snapjaw").AtPlayerOffset(7, 0);

            ctx.Log("Calm Test ready — cast Calm on one snapjaw, fight the rest.");
        }
    }
}
