using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M4 manual-playtest showcase — a passive Scribe stands on an
    /// exterior cell in the starting village; we push MoveToInteriorGoal
    /// on her brain and watch her walk into the nearest building.
    ///
    /// Expected flow when launched:
    /// - Turn 0: Scribe spawned 2 tiles east of the player, exterior (no
    ///   roof). Brain's goal stack has MoveToInteriorGoal at the top.
    /// - Turn 1: MoveToInteriorGoal.TakeAction runs → BFS finds the
    ///   nearest interior cell → PushChildGoal(MoveToGoal). Think signal
    ///   "seeking shelter" appears in the Phase 10 goal-stack inspector.
    /// - Turns 2–N: Scribe walks east/north/wherever the nearest interior
    ///   is, passing through doors as needed.
    /// - Terminal turn: Scribe steps onto a StoneFloor cell (interior).
    ///   Next tick, Finished()=true, the goal pops. If a subsequent goal
    ///   exists (e.g. BoredGoal from BrainPart), the NPC takes that —
    ///   otherwise she just stays idle on the interior tile.
    ///
    /// Good for:
    /// - Visually confirming MoveToInteriorGoal routes around walls via doors
    /// - Observing the "seeking shelter" narrative signal in the goal-stack
    ///   inspector (press 't' during play)
    /// - Sanity-checking the Cell.IsInterior tagging coverage against a
    ///   real-generated village layout
    ///
    /// Does not cover (out of M4 scope):
    /// - Weather-driven trigger (no weather system yet)
    /// - Curfew / dawn triggers
    /// - MoveToExteriorGoal showcase (see the symmetric companion if added)
    /// </summary>
    [Scenario(
        name: "Scribe Seeks Shelter",
        category: "AI Behavior",
        description: "Passive Scribe with MoveToInteriorGoal walks to the nearest building interior.")]
    public class ScribeSeeksShelter : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var scribe = ctx.Spawn("Scribe")
                .AtPlayerOffset(2, 0);

            // Push the goal directly — no event trigger yet in this
            // milestone. When the weather / curfew system ships, this
            // becomes a reactive push instead of an imperative one.
            var brain = scribe.GetPart<BrainPart>();
            if (brain != null)
                brain.PushGoal(new MoveToInteriorGoal());

            ctx.Log("Scribe has MoveToInteriorGoal pushed. Watch her walk to the nearest building interior; open the thought inspector ('t') to see 'seeking shelter'.");
        }
    }
}
