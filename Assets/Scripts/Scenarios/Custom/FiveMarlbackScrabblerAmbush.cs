namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Phase 1 demo scenario: surrounds the player with 5 marlbacks on a
    /// radius-2 ring (evenly distributed — 72° angle step). Good for rapidly
    /// stress-testing combat feel, FleeGoal triggers, and any AI behavior
    /// that depends on multi-threat situations.
    ///
    /// Originally hand-placed at (2,0)/(-2,0)/(0,2)/(0,-2)/(2,2) — four
    /// cardinals + a redundant diagonal that broke symmetry. Modernized to
    /// use Phase 2a's <c>InRing(radius, i, totalOfN)</c> for true symmetry.
    ///
    /// Partial-spawn reporting: if any ring position lands on a non-passable
    /// cell (wall/furniture), <c>EntityBuilder</c> silently skips that spawn.
    /// This scenario tracks successes and reports the count so a partial
    /// ambush is visible rather than mysterious.
    /// </summary>
    [Scenario(
        name: "Five MarlbackScrabbler Ambush",
        category: "Combat Stress",
        description: "Player surrounded by 5 marlbacks on a radius-2 ring.")]
    public class FiveMarlbackScrabblerAmbush : IScenario
    {
        private const int Count = 5;
        private const int Radius = 2;

        public void Apply(ScenarioContext ctx)
        {
            int spawned = 0;
            for (int i = 0; i < Count; i++)
            {
                if (ctx.Spawn("MarlbackScrabbler").InRing(Radius, i, Count) != null)
                    spawned++;
            }

            if (spawned == Count)
                ctx.Log($"Five MarlbackScrabbler Ambush: all {Count} OutlandRaiders spawned on r={Radius} ring.");
            else
                ctx.Log($"Five MarlbackScrabbler Ambush: {spawned}/{Count} OutlandRaiders spawned (rest blocked by terrain).");
        }
    }
}
