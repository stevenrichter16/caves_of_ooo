namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M1.3 showcase — <c>AIAmbushPart</c> dormant creature. SleepingTroll
    /// starts 10–14 cells from the player, outside its own 8-cell sight
    /// radius. On its first TakeTurn the ambush part pushes
    /// <c>DormantGoal</c>; the troll idles with <c>z</c> sleep particles
    /// (blueprint SleepParticleInterval = 8) until a hostile enters sight.
    ///
    /// Expected flow when launched:
    /// - Turn 1: Troll pushes DormantGoal. Sleep particles visible.
    /// - Player walks east (toward the troll's position).
    /// - When player enters the troll's sight radius: DormantGoal wakes the
    ///   troll, it pushes KillGoal and charges.
    ///
    /// Good for:
    /// - Eyeballing sleep particle cadence (every 8 turns per blueprint)
    /// - Confirming wake-on-hostile-in-sight fires at exactly the sight-radius
    ///   threshold
    /// - Testing KillGoal pathing against a previously-dormant attacker
    ///
    /// Uses <c>NearPlayer(10, 14)</c> rather than <c>AtPlayerOffset(12, 0)</c>
    /// because the latter can land in walls in cramped zones; NearPlayer
    /// picks a passable cell from the Chebyshev band.
    /// </summary>
    [Scenario(
        name: "Sleeping Troll",
        category: "AI Behavior",
        description: "Dormant troll outside sight radius. Walk toward it — ambush wakes when you enter sight.")]
    public class SleepingTroll : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ctx.Spawn("SleepingTroll").NearPlayer(minRadius: 10, maxRadius: 14);

            ctx.Log("Sleeping troll placed 10-14 cells away. Walk toward it; ambush wakes when you enter its 8-cell sight.");
        }
    }
}
