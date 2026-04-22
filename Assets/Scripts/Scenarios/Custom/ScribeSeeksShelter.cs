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
            // Spawn in the 3–5 ring around the player. AtPlayerOffset(2, 0)
            // was previously used but silently failed in this village's
            // layout (a CompassStone sits solid at player+2). NearPlayer
            // picks a random passable cell in the band — almost always
            // exterior in a village of our size, but we still check below
            // so the scenario fails loud rather than silent.
            var scribe = ctx.Spawn("Scribe").NearPlayer(minRadius: 3, maxRadius: 5);
            if (scribe == null)
            {
                ctx.Log("[ScribeSeeksShelter] FAILED: no passable cell in the player+3..+5 ring. Check console for a spawn-skip warning.");
                return;
            }

            var spawnCell = ctx.Zone.GetEntityCell(scribe);
            if (spawnCell == null)
            {
                ctx.Log("[ScribeSeeksShelter] FAILED: Scribe has no cell after spawn. This should not happen.");
                return;
            }

            var brain = scribe.GetPart<BrainPart>();
            if (brain == null)
            {
                ctx.Log("[ScribeSeeksShelter] FAILED: Scribe has no BrainPart. Goal cannot be pushed.");
                return;
            }

            // If she happened to spawn INSIDE a building, the goal will
            // Finish immediately with no Think() call — confusing for the
            // playtester. Call it out in the log so "nothing happened"
            // isn't mistaken for a bug.
            if (spawnCell.IsInterior)
            {
                ctx.Log($"[ScribeSeeksShelter] Scribe spawned at ({spawnCell.X},{spawnCell.Y}) which is already INTERIOR. MoveToInteriorGoal will pop immediately. Re-run for a different spawn.");
                return;
            }

            // Turn OFF Staying. Scribe blueprints default to Staying=true
            // (Qud-parity home-cell behavior — don't drift from assigned
            // post). Leaving that on here would turn the scenario into a
            // round-trip: MoveToInteriorGoal walks her INTO a building,
            // pops on arrival, BoredGoal then notices she's away from her
            // spawn cell and pushes MoveToGoal BACK to the exterior
            // spawn. The playtester sees "she entered and walked back
            // out" — confusingly indistinguishable from a bug in the M4
            // goal, even though each system is working correctly on its
            // own. Disabling Staying isolates the M4 goal for inspection.
            brain.Staying = false;

            brain.PushGoal(new MoveToInteriorGoal());
            ctx.Log($"[ScribeSeeksShelter] Scribe spawned at ({spawnCell.X},{spawnCell.Y}) (exterior), Staying disabled. MoveToInteriorGoal pushed. Advance turns (.) and watch her walk to the nearest building — she'll stop when she reaches an interior cell. Press 't' for the 'seeking shelter' thought in the inspector.");
        }
    }
}
