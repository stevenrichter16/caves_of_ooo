namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Spawns a Warden at 20% HP, 8 cells east of the player. The Warden's
    /// StartingCell is pinned to its spawn cell — observe how <c>AIGuardPart</c>
    /// (wired into the Warden blueprint in Tier 3b) pushes <c>GuardGoal</c>,
    /// which scans for hostiles and returns to post when displaced.
    ///
    /// Good for:
    /// - Verifying AIGuard ↔ GuardGoal ↔ BoredGoal handoff in a live session
    /// - Eyeballing the at-post reactivity fix (hostile appears → Warden engages
    ///   on the very next tick, not two ticks later)
    /// - Watching return-to-post pathing after combat drifts the Warden away
    /// </summary>
    [Scenario(
        name: "Wounded Warden",
        category: "AI Behavior",
        description: "Warden at 20% HP — watch her guard her post and retreat on damage.")]
    public class WoundedWarden : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var playerPos = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            int wardenX = playerPos.x + 8;
            int wardenY = playerPos.y;

            ctx.Spawn("Warden")
               .WithHp(fraction: 0.20f)
               .WithStartingCell(wardenX, wardenY)
               .At(wardenX, wardenY);

            ctx.Log("Wounded Warden spawned at 20% HP — observe AIGuard → GuardGoal flow.");
        }
    }
}
