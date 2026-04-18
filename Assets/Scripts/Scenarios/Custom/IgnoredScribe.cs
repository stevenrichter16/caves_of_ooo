namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M1.2 showcase — Scribe with <c>BrainPart.Passive=true</c> seeing a
    /// hostile Snapjaw within her 8-cell sight radius. Passive NPCs ignore
    /// hostile sight (no engagement, no flee on proximity) but still
    /// retaliate when attacked.
    ///
    /// Expected flow when launched:
    /// - Turn 1: Scribe sees Snapjaw at distance 3. Passive — no action.
    /// - Turns 2–5: Snapjaw (hostile to Villagers faction) walks toward Scribe.
    /// - Turn 6ish: Snapjaw attacks Scribe adjacent. Scribe retaliates
    ///   (Passive ≠ pacifist).
    /// - Scribe drops below 80% HP → her AISelfPreservation (thresholds
    ///   0.8/0.95) fires and she retreats.
    ///
    /// Good for:
    /// - Verifying Passive suppresses the hostile-on-sight scan
    /// - Confirming retaliation still works (combat isn't broken for passives)
    /// - Observing the passive → retaliate → retreat transition
    /// </summary>
    [Scenario(
        name: "Ignored Scribe",
        category: "AI Behavior",
        description: "Passive Scribe ignores a nearby Snapjaw on sight. Retaliates only when attacked.")]
    public class IgnoredScribe : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ctx.Spawn("Scribe").AtPlayerOffset(5, 0);
            ctx.Spawn("Snapjaw").AtPlayerOffset(8, 0);   // 3 cells east of Scribe

            ctx.Log("Scribe at player+5, Snapjaw at player+8. Scribe should ignore Snapjaw on sight (Passive=true).");
        }
    }
}
