namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M1.1 showcase — Warden spawned at 25% HP, below her blueprint
    /// <c>AISelfPreservation.RetreatThreshold</c> of 0.3. On her first turn
    /// the self-preservation part should push <c>RetreatGoal</c> and she'll
    /// flee toward safety instead of engaging.
    ///
    /// Also exercises the interaction between <c>AIGuardPart</c> (pushes
    /// GuardGoal on bored) and <c>AISelfPreservationPart</c> (pushes
    /// RetreatGoal on low HP) — RetreatGoal should take precedence.
    ///
    /// Good for:
    /// - Verifying low-HP retreat path in a live zone
    /// - Observing what "safety" resolves to for a wounded Warden
    /// - Seeing how RetreatGoal layers over an existing GuardGoal
    /// </summary>
    [Scenario(
        name: "Cornered Warden",
        category: "AI Behavior",
        description: "Warden at 25% HP — watch AISelfPreservation push RetreatGoal on her first turn.")]
    public class CorneredWarden : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ctx.Spawn("Warden")
               .WithHp(0.25f)
               .AtPlayerOffset(4, 0);

            ctx.Log("Cornered Warden at 25% HP (below RetreatThreshold 0.3). She should retreat immediately.");
        }
    }
}
