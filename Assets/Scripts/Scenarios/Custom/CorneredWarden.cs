namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M1.1 showcase — Warden at 25% HP, displaced from her guard post.
    /// Pushes <c>RetreatGoal</c> on her first turn (HP 0.25 &lt;
    /// RetreatThreshold 0.3) and the goal's MoveToGoal child walks her
    /// back to the post — visible retreat flight across several cells.
    ///
    /// IMPORTANT setup detail — the Warden MUST be displaced from her
    /// StartingCell for the retreat to be visible. <c>RetreatGoal.TravelToWaypoint</c>
    /// short-circuits to the Recover phase when the NPC is already at the
    /// waypoint, so a Warden spawned AT her StartingCell would retreat
    /// "to where she is" — she'd just stand still and heal. This scenario
    /// explicitly pins StartingCell west of the spawn cell so the retreat
    /// walk is observable.
    ///
    /// Good for:
    /// - Verifying low-HP retreat path in a live zone
    /// - Observing how RetreatGoal.TravelToWaypoint + MoveToGoal pathfind
    /// - Seeing Phase.Travel → Phase.Recover transition + self-heal (1 HP/tick)
    /// - Watching the interaction between AIGuard and AISelfPreservation —
    ///   both handle AIBoredEvent; retreat takes priority (pushed last)
    /// </summary>
    [Scenario(
        name: "Cornered Warden",
        category: "AI Behavior",
        description: "Wounded Warden displaced from her post — watch her retreat back while healing.")]
    public class CorneredWarden : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var playerPos = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Warden's "post" is 6 cells east of the player; she's currently
            // displaced 3 cells further east at player+9. RetreatGoal will
            // walk her back west toward the post.
            int postX = playerPos.x + 6;
            int postY = playerPos.y;
            int spawnX = playerPos.x + 9;
            int spawnY = playerPos.y;

            ctx.Spawn("Warden")
               .WithHp(0.25f)
               .WithStartingCell(postX, postY)
               .At(spawnX, spawnY);

            ctx.Log($"Wounded Warden at player+9, post at player+6. She should walk west back to post, then heal.");
        }
    }
}
