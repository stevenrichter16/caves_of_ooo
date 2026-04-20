namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M3.2 showcase — PetDog (AIRetriever, AlliesOnly=true) responds
    /// to a thrown item. The player throws a bone east; ItemLandedEvent
    /// broadcasts from ThrowItemCommand; the dog (ally, within NoticeRadius=10)
    /// gets the event, pushes GoFetchGoal(bone, returnHome: false), walks
    /// to the bone, picks it up.
    ///
    /// Expected flow when launched:
    /// - PetDog spawns 2 cells east of the player.
    /// - Player inventory contains a Dagger repurposed as throwable
    ///   (the first throwable the builder has at hand — bones aren't
    ///   a blueprint yet; substituting a small dagger).
    /// - Player throws the dagger east (inventory → Throw action →
    ///   direction east). ItemLandedEvent fires on the PetDog.
    /// - PetDog is on Villagers faction (same as player), so the
    ///   AlliesOnly=true gate passes.
    /// - Dog pushes GoFetchGoal(dagger, returnHome: false) — walks to
    ///   the dagger's landing cell, picks it up.
    /// - Look at the PetDog's inventory afterward — it now has the
    ///   dagger.
    ///
    /// Good for:
    /// - Verifying the Throw → ItemLanded broadcast → AIRetriever →
    ///   GoFetchGoal pipeline end-to-end against real blueprints
    /// - Observing the ally-filter working (the dog fetches the
    ///   player's throw but would ignore an enemy throw)
    /// - Confirming the fetched item ends up in the pet's inventory
    ///
    /// Counter-experiment (enemy throw filter): to observe the
    /// AlliesOnly filter in action, launch the scenario, then use
    /// execute_code in a live session to simulate a Snapjaw throwing.
    /// The dog should NOT react. The AIRetriever_IgnoresEnemyThrow
    /// unit test already pins this semantic.
    /// </summary>
    [Scenario(
        name: "Pet Dog Fetches Thrown Item (M3.2)",
        category: "AI Behavior",
        description: "PetDog runs to fetch a thrown item — ally-filtered, radius-limited.")]
    public class PetDogFetchesBone : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Clear east row of starting-zone hazards so the throw line
            // and pathfinding are clean.
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 7; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // Dog between player and the eventual landing cell.
            ctx.Spawn("PetDog").AtPlayerOffset(2, 0);

            // Equip the player with a throwable. A plain Dagger is light
            // and readily in range; a dedicated Bone blueprint is future
            // content but not required for this scenario.
            ctx.Player.GiveItem("Dagger", 1);

            ctx.Log("Open inventory ('i'), pick the dagger, action menu Throw ('t'), aim east. Dog fetches it.");
        }
    }
}
