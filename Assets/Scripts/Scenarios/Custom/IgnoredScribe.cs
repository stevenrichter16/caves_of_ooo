namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M1.2 showcase — Scribe with <c>BrainPart.Passive=true</c> seeing a
    /// hostile MarlbackScrabbler within her 8-cell sight radius. Passive NPCs ignore
    /// hostile sight (no engagement, no flee on proximity) but still
    /// retaliate when attacked.
    ///
    /// Expected flow when launched:
    /// - Turn 1: Scribe sees MarlbackScrabbler at distance 3. Passive — no engagement.
    /// - Turns 2–5: MarlbackScrabbler (OutlandRaiders faction, hostile to Villagers) walks
    ///   toward the closer target (Scribe).
    /// - Turn 6ish: MarlbackScrabbler attacks Scribe adjacent. Scribe retaliates
    ///   (Passive ≠ pacifist).
    /// - Scribe's HP drops below 80% → her AISelfPreservation (thresholds
    ///   0.8/0.95) pushes RetreatGoal targeting her StartingCell.
    /// - She walks west past the player toward the pinned safe waypoint.
    ///
    /// IMPORTANT setup detail: the Scribe's StartingCell is explicitly
    /// pinned 3 cells WEST of the player — NOT at her spawn cell. If we
    /// left StartingCell as the spawn-cell default, RetreatGoal would
    /// target "the cell she's already standing on" and short-circuit
    /// straight to the Recover phase — the retreat step of the flow
    /// would be invisible (same gotcha that bit CorneredWarden).
    ///
    /// Good for:
    /// - Verifying Passive suppresses the hostile-on-sight scan
    /// - Confirming retaliation still works (combat isn't broken for passives)
    /// - Observing the full passive → retaliate → visible retreat cycle
    /// </summary>
    [Scenario(
        name: "Ignored Scribe",
        category: "AI Behavior",
        description: "Passive Scribe ignores a nearby MarlbackScrabbler on sight. Retaliates if attacked, then flees west.")]
    public class IgnoredScribe : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var playerPos = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Scribe 5 east of player; her "safe" waypoint is 3 west of player.
            // MarlbackScrabbler spawns 3 cells east of Scribe so Scribe is the closer
            // target (MarlbackScrabbler melee-targets closest hostile).
            int scribeX = playerPos.x + 5;
            int scribeY = playerPos.y;
            int safeX = playerPos.x - 3;
            int safeY = playerPos.y;

            ctx.Spawn("Scribe")
               .WithStartingCell(safeX, safeY)
               .At(scribeX, scribeY);

            ctx.Spawn("MarlbackScrabbler").AtPlayerOffset(8, 0);   // 3 cells east of Scribe

            ctx.Log("Scribe at player+5 (safe post 3 west of player). MarlbackScrabbler at player+8. Passive on sight; retreat west after being hit.");
        }
    }
}
