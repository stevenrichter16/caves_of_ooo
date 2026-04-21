namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Phase 10 showcase — enables the AI goal-stack inspector toggle and
    /// spawns three creatures with distinct behaviors so the inspector's
    /// rendering has something to show across the major goal types:
    ///
    /// - Snapjaw   (hostile)  → KillGoal targeting the player once in sight.
    /// - Warden    (guard)    → BoredGoal scanning post; may retreat at low HP.
    /// - VillageChild (pet)   → BoredGoal / WanderRandomlyGoal, occasional PetGoal.
    ///
    /// Expected flow when launched:
    /// - AIDebug.AIInspectorEnabled = true is set by Apply().
    /// - Open look mode ('L') and move the cursor onto any creature.
    /// - The sidebar's FOCUS panel grows to show:
    ///     Goals:
    ///       KillGoal: target=player
    ///       BoredGoal
    ///     Thought: attacking player
    /// - Goals/thought update live as the NPC ticks. The Snapjaw's Thought
    ///   cycles through "closing on player" → "attacking player" → etc as
    ///   its KillGoal branches.
    ///
    /// Side effects:
    /// - The inspector toggle STAYS ON across scenario exits (static flag).
    ///   To disable, exit Play mode OR use the editor menu
    ///   "Tools/Caves of Ooo/AI/Toggle Goal Inspector".
    /// </summary>
    [Scenario(
        name: "Inspect AI Goals (Phase 10)",
        category: "Debug",
        description: "Enables AI goal-stack inspector; spawns Snapjaw + Warden + VillageChild. Look-mode ('L') on each creature shows their goal stack + last thought in the sidebar.")]
    public class InspectAIGoals : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Flip the toggle first so even the scenario's own spawn ticks
            // produce visible inspector output as soon as the player opens
            // look mode.
            CavesOfOoo.Diagnostics.AIDebug.AIInspectorEnabled = true;

            // Clear east and north-east rows of starting-zone hazards so the
            // three creatures have clean line-of-sight + open ground for
            // natural AI behavior (compass stones and chests would otherwise
            // block pathfinding and trigger KillGoal failure branches).
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 6; dx++)
            {
                ctx.World.ClearCell(p.x + dx, p.y);
                ctx.World.ClearCell(p.x + dx, p.y - 1);
                ctx.World.ClearCell(p.x + dx, p.y + 1);
            }

            // Snapjaw 6 east — within sight radius, so BoredGoal's hostile
            // scan immediately promotes to KillGoal targeting the player.
            // Great first sample for the inspector (Thought cycles on each
            // approach/attack tick).
            ctx.Spawn("Snapjaw").AtPlayerOffset(6, 0);

            // Warden 3 north-east — AIGuardPart from M1.1 pushes GuardGoal
            // which stays as BoredGoal until hostile appears. Shows the
            // "idle guard post" shape in the inspector. Also demonstrates
            // the inspector working on a goal that DOESN'T override
            // GetDetails (GuardGoal is v2 per the plan) — the inspector
            // renders "GuardGoal" with just the type name.
            ctx.Spawn("Warden").AtPlayerOffset(3, -1);

            // VillageChild 3 south-east — Passive wanderer; inspector shows
            // BoredGoal / WanderRandomlyGoal rotating as the child pathfinds
            // around. Occasionally PetGoal pushes (probability-gated).
            ctx.Spawn("VillageChild").AtPlayerOffset(3, 1);

            ctx.Log(
                "AI inspector enabled. Press 'L' and hover any creature to " +
                "see their goal stack + last thought in the sidebar FOCUS panel.");
        }
    }
}
