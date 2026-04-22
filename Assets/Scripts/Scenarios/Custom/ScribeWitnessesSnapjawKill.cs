namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M2.3 showcase — Passive Scribe watches a nearby death. The base
    /// WitnessedEffect scenario: confirm the post-death broadcast reaches
    /// a Passive NPC within 8 cells + LOS and pushes WanderDurationGoal.
    ///
    /// Expected flow when launched:
    /// - Scribe at player+3, Passive by blueprint.
    /// - One-HP Snapjaw at player+5, personally hostile to player (so the
    ///   Snapjaw's AI attacks first, giving the player an excuse to kill
    ///   it — but the test is indifferent to who throws the first punch).
    /// - Player attacks Snapjaw → Snapjaw HP hits 0 → CombatSystem.HandleDeath
    ///   fires → BroadcastDeathWitnessed applies WitnessedEffect(20) to
    ///   every Passive NPC within 8 cells + LOS.
    /// - Scribe is at Chebyshev distance 2, clear LOS — gets the effect.
    /// - MessageLog: "Scribe looks shaken."
    /// - Scribe's brain now has WanderDurationGoal on top, ticking down
    ///   20 turns. Each tick she picks a random passable neighbor and
    ///   steps — visibly "pacing" in the square.
    /// - After ~20 turns, effect expires, OnRemove tears down the goal,
    ///   Scribe returns to idle.
    ///
    /// Good for:
    /// - Verifying the end-to-end witness broadcast path
    /// - Observing the visible pacing animation (wander step per tick)
    /// - Watching OnRemove cleanly tear down the goal when Duration hits 0
    /// </summary>
    [Scenario(
        name: "Scribe Witnesses Snapjaw Kill (M2.3)",
        category: "AI Behavior",
        description: "Passive Scribe near a one-HP hostile Snapjaw — kill the Snapjaw, watch the Scribe pace.")]
    public class ScribeWitnessesSnapjawKill : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Clear the east row so the Scribe has clear LOS to the kill cell
            // AND the player can reach the Snapjaw without pathing around
            // the starting zone's compass stones and grimoire chest.
            // (West stone at player+2,0; chest at player+4,0 — both Solid.)
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 5; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            ctx.Spawn("Scribe").AtPlayerOffset(3, 0);

            // One-HP hostile so one swing from the player ends it cleanly —
            // no RNG miss-chain to get lucky through before the test resolves.
            ctx.Spawn("Snapjaw")
               .WithHpAbsolute(1)
               .AsPersonalEnemyOf(ctx.PlayerEntity)
               .AtPlayerOffset(5, 0);

            ctx.Log("Kill the Snapjaw east of you. Watch the Scribe start pacing (shaken) for ~20 turns.");
        }
    }
}
