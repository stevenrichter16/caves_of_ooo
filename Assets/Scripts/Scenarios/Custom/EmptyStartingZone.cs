namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Strips every creature from the current zone, leaving only the player and
    /// terrain/furniture behind. Baseline for controlled tests where you want to
    /// stage exactly what the player interacts with.
    ///
    /// <see cref="Builders.ZoneBuilder.RemoveEntitiesWithTag"/> preserves the player
    /// implicitly, so there's no re-add dance needed.
    /// </summary>
    [Scenario(
        name: "Empty Starting Zone",
        category: "Baseline",
        description: "Remove all creatures from the current zone. Player is preserved.")]
    public class EmptyStartingZone : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ctx.World.RemoveEntitiesWithTag("Creature");
            ctx.Log("Zone cleared of creatures. Baseline state for controlled testing.");
        }
    }
}
