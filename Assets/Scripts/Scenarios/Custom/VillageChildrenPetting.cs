namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M3.1 showcase — a controlled ambient-life setup: spawn a VillageChild
    /// adjacent to a Villager (as a pet target) in a cleared area so the
    /// child's periodic PetGoal is visibly observable without the village's
    /// procedural noise.
    ///
    /// Expected flow when launched:
    /// - Child and Villager both visible near the player.
    /// - Child wanders randomly one cell per tick (Wanders + WandersRandomly,
    ///   Passive so no combat interest).
    /// - Approx every 20 ticks (Chance=5 per bored tick → ~20-tick mean),
    ///   child pushes PetGoal → walks adjacent to the Villager → emits a
    ///   magenta '*' particle → PetGoal pops. Rinse and repeat.
    /// - Skip turns with '.' (wait) to accelerate observation.
    ///
    /// Good for:
    /// - Verifying the full AIBoredEvent → AIPetterPart → PetGoal path
    ///   lands visibly with a real blueprint
    /// - Confirming the magenta '*' pet particle renders correctly
    /// - Demonstrating Passive behavior — child won't engage if a hostile
    ///   is adjacent (try spawning a snapjaw and watch the child NOT react)
    ///
    /// Side-effect note: PetGoal's TakeAction fires during TurnManager ticks,
    /// which only run when the player isn't mid-action. Skipping turns with
    /// '.' is the fastest way to see the behavior in a session.
    /// </summary>
    [Scenario(
        name: "Village Children Petting (M3.1)",
        category: "AI Behavior",
        description: "VillageChild next to a Villager — watch the child periodically emit a '*' petting particle.")]
    public class VillageChildrenPetting : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Clear the east row — same starting-zone hazard pattern documented
            // in M2 scenarios. Compass stones + chest would block spawn
            // positioning and obscure the observation window.
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 5; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // Villager as the pet target. Villagers faction, so the child
            // (also Villagers) considers her an ally via FactionManager.
            ctx.Spawn("Villager").AtPlayerOffset(3, 0);

            // Child two cells east of the Villager — visible, adjacent-to-
            // but-not-on her cell. Child wanders; eventually bored fires,
            // probability gate passes, PetGoal pushes.
            ctx.Spawn("VillageChild").AtPlayerOffset(5, 0);

            ctx.Log("Child wanders near the Villager. Wait (press '.') and watch for the magenta '*' pet particle every ~20 turns.");
        }
    }
}
