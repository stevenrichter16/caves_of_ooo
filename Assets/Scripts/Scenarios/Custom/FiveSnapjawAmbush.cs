namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Phase 1 demo scenario: surrounds the player with 5 snapjaws on a
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
        name: "Five Snapjaw Ambush",
        category: "Combat Stress",
        description: "Player surrounded by 5 snapjaws on a radius-2 ring.")]
    public class FiveSnapjawAmbush : IScenario
    {
        private const int Count = 5;
        private const int Radius = 2;

        public void Apply(ScenarioContext ctx)
        {
            int spawned = 0;
            for (int i = 0; i < Count; i++)
            {
                if (ctx.Spawn("Snapjaw").InRing(Radius, i, Count) != null)
                    spawned++;
            }

            if (spawned == Count)
                ctx.Log($"Five Snapjaw Ambush: all {Count} Snapjaws spawned on r={Radius} ring.");
            else
                ctx.Log($"Five Snapjaw Ambush: {spawned}/{Count} Snapjaws spawned (rest blocked by terrain).");
        }
    }
}
