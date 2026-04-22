namespace CavesOfOoo.Scenarios
{
    /// <summary>
    /// A launch-able or assertable scenario. The <see cref="Apply"/> method receives a
    /// fully-wired <see cref="ScenarioContext"/> (live <c>GameBootstrap</c> references)
    /// and uses the fluent builders on it to spawn entities, modify the player, place
    /// objects, etc.
    ///
    /// Conventions:
    /// - Mark the class with <c>[Scenario(name, category)]</c> for menu integration.
    /// - Keep <see cref="Apply"/> idempotent-ish — it runs once per launch, but a
    ///   scenario should not assume clean-slate state (other systems may have populated
    ///   the zone before the scenario runs).
    /// - Use positioning helpers that gracefully degrade when cells are blocked
    ///   (e.g., <c>NearPlayer</c>, <c>InRing</c>) rather than hard-coding <c>.At(x, y)</c>
    ///   unless the scenario specifically needs absolute positions.
    /// </summary>
    public interface IScenario
    {
        /// <summary>
        /// Apply the scenario to the live game. Called exactly once per launch,
        /// after <c>GameBootstrap.Start()</c> has fully populated the zone, player,
        /// factory, and turn manager.
        /// </summary>
        void Apply(ScenarioContext ctx);
    }
}
