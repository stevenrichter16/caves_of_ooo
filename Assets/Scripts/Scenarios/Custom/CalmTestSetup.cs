namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M2.2 Calm stress-test — player with Calm at Level 3 plus three
    /// hostile Snapjaws in a row. Exercises the "pacify one, fight the
    /// rest" workflow: the player's challenge is to cast Calm
    /// selectively and manage the remaining two with normal combat.
    ///
    /// Compared to <see cref="PacifiedWarden"/> (the minimum single-
    /// target test), this scenario:
    /// - Has multiple targets — player must pick which to pacify
    /// - Stresses the idempotent-recast feedback at scale (if you
    ///   accidentally re-target an already-calmed Snapjaw, you see
    ///   "... is already at peace." rather than wasting a cooldown
    ///   silently)
    /// - Forces combat AND pacification in the same encounter —
    ///   NoFightGoal vs. KillGoal interactions on the non-calmed
    ///   Snapjaws are visible
    ///
    /// Good for:
    /// - Stress-testing the M2.2 pipeline across multiple targets
    /// - Verifying selective pacification works as expected
    /// - Observing the look-mode "pacified" label on one Snapjaw
    ///   next to "hostile" on two others — a good sanity check for
    ///   the disposition-label fix (commit 4cc7d3d)
    /// </summary>
    [Scenario(
        name: "Calm Test Setup (multi-target)",
        category: "Mutations",
        description: "Player with Calm + 3 hostile Snapjaws. Calm the first, fight the rest.")]
    public class CalmTestSetup : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ctx.Player.AddMutation("CalmMutation", level: 3);

            // Clear the east row so Calm's projectile can reach each Snapjaw
            // individually and the player can reach any of them in melee
            // without routing around compass stones or the chest.
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 7; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            ctx.Spawn("Snapjaw").AtPlayerOffset(3, 0);
            ctx.Spawn("Snapjaw").AtPlayerOffset(5, 0);
            ctx.Spawn("Snapjaw").AtPlayerOffset(7, 0);

            ctx.Log("Cast Calm on one snapjaw (key 8), fight the rest. Look at the pacified one — should read 'pacified'.");
        }
    }
}
