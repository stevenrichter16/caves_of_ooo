using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M3.3 showcase — wounded Scribe with both AIFleeToShrine AND
    /// AISelfPreservation parts attached. A Shrine is placed AWAY from
    /// her starting cell so the two retreat destinations are
    /// visibly distinct. Demonstrates that AIFleeToShrine wins on
    /// blueprint order + event consumption: the Scribe walks to the
    /// shrine, NOT to her starting cell.
    ///
    /// Expected flow when launched:
    /// - Scribe at player+6,0, HP 5/20 (25%, below her FleeThreshold of 0.8).
    /// - Her StartingCell is pinned at player+6,0 (where she was spawned),
    ///   so AISelfPreservation's fallback RetreatGoal would send her
    ///   RIGHT BACK to where she is — zero visible movement if shrine
    ///   logic weren't working. That makes this a sharp test of shrine
    ///   wins.
    /// - Shrine at player+3,0 — three cells WEST of the Scribe,
    ///   between her and the player. Chebyshev distance 3 → quick walk.
    /// - No hostile in sight (Scribe's AIBored fires cleanly on turn 1).
    /// - Turn 1: AIBoredEvent fires → AIFleeToShrine runs first (blueprint
    ///   order), sees HP 25% &lt; 0.8 threshold, finds the shrine, pushes
    ///   FleeLocationGoal(player+3, 0). Event CONSUMED — AISelfPreservation
    ///   does not fire.
    /// - Turns 2–4: Scribe walks west to the shrine cell. MoveToGoal
    ///   drives the pathfinding.
    /// - On arrival: FleeLocationGoal.Finished() returns true, goal pops.
    ///   Scribe stands at the shrine cell (shrine is non-solid, she can
    ///   share the tile).
    ///
    /// Good for:
    /// - Verifying the end-to-end AIBored → AIFleeToShrine → FleeLocationGoal
    ///   path against a real Scribe blueprint
    /// - Confirming the blueprint-order priority: Scribe goes to the shrine,
    ///   NOT to her starting cell. If AISelfPreservation fired first, she'd
    ///   stand still (StartingCell = current cell)
    /// - Observing a Scribe share a cell with the shrine (non-solid shrine)
    ///
    /// Counter-experiment (via unit test, not this scenario): the
    /// AIFleeToShrine_DoesNotFire_WhenNoShrineInZone test pins the
    /// graceful fallback — when no shrine exists, AISelfPreservation
    /// runs normally.
    /// </summary>
    [Scenario(
        name: "Wounded Scribe Flees to Shrine (M3.3)",
        category: "AI Behavior",
        description: "Wounded Scribe chooses Shrine over her home post — demonstrates blueprint-order priority.")]
    public class WoundedScribeFleesToShrine : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Clear east row of starting-zone hazards (compass stones + chest)
            // so the Shrine and Scribe both spawn cleanly AND the Scribe's
            // westbound path is unobstructed.
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 7; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // Shrine 3 cells east of player — between the player and the Scribe.
            ctx.World.PlaceObject("Shrine").AtPlayerOffset(3, 0);

            // Wounded Scribe at 25% HP, spawned east of the shrine. Her
            // StartingCell auto-sets to her spawn cell (player+6,0), so
            // AISelfPreservation's fallback RetreatGoal would target
            // "stand still" — making any visible westbound walk proof that
            // AIFleeToShrine won over AISelfPreservation on the bored tick.
            ctx.Spawn("Scribe")
                .WithHp(0.25f)
                .AtPlayerOffset(6, 0);

            ctx.Log("Wait a few turns ('.' key). Scribe walks WEST to the shrine at player+3 — not to her starting cell at player+6.");
        }
    }
}
