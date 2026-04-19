namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M2.2 × M2.3 interaction showcase — pacify a Passive Scribe with
    /// Calm, then kill a nearby hostile. The Scribe now has two goals on
    /// stack: NoFightGoal (from Calm, underneath) + WanderDurationGoal
    /// (from WitnessedEffect, on top). Pins the "shock overrides calm"
    /// stack-layering the M2 review called out as the most subtle
    /// interaction.
    ///
    /// Expected flow when launched:
    /// - Cast Calm eastward — target the SCRIBE (not the Snapjaw).
    ///   Scribe brain gets NoFightGoal on stack.
    ///   Look mode over her now reads "pacified."
    /// - Kill the one-HP Snapjaw further east.
    ///   BroadcastDeathWitnessed sees Scribe in range (clear LOS, Passive,
    ///   within radius), applies WitnessedEffect(20).
    ///   Scribe's OnApply pushes WanderDurationGoal on top of NoFightGoal.
    ///   Look mode STILL reads "pacified" (NoFightGoal is still on the
    ///   stack, just not on top — the look label only checks HasGoal, not
    ///   PeekGoal, so the correct behavior is "pacified" throughout).
    /// - Scribe paces for ~20 turns.
    /// - WanderDurationGoal expires naturally (Finished() on _ticksTaken
    ///   hit), effect OnRemove runs, brain now has only NoFightGoal on top.
    /// - Scribe returns to idle-pacified for the rest of the ~70 turns
    ///   (Calm L3 base duration).
    ///
    /// Good for:
    /// - Verifying stack layering (the review's note #13 in the M2 audit)
    /// - Confirming look-mode "pacified" label survives through the
    ///   shock-then-resume cycle
    /// - Ensuring WanderDurationGoal's OnPop cleanly exposes the
    ///   underlying NoFightGoal (no double-engagement, no stuck state)
    /// </summary>
    [Scenario(
        name: "Calm then Witness (M2.2 × M2.3)",
        category: "AI Behavior",
        description: "Pacify a Scribe then kill a Snapjaw. Scribe paces, then returns to pacified idle.")]
    public class CalmThenWitness : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            ctx.Player.AddMutation("CalmMutation", level: 3);

            // Clear the east row so the Calm projectile reaches the Scribe
            // and the player can reach the Snapjaw. Starting zone otherwise
            // has West compass stone at player+2,0 and chest at player+4,0
            // blocking the line.
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 5; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            ctx.Spawn("Scribe").AtPlayerOffset(3, 0);

            ctx.Spawn("Snapjaw")
               .WithHpAbsolute(1)
               .AsPersonalEnemyOf(ctx.PlayerEntity)
               .AtPlayerOffset(5, 0);

            ctx.Log("Cast Calm on the Scribe first (east, key 8). Then kill the Snapjaw. Scribe paces for ~20 turns, then returns to pacified idle.");
        }
    }
}
