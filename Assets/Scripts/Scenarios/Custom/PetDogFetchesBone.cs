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
    /// - PetDog spawns 2 east, 1 north of the player (diagonal). The
    ///   diagonal placement keeps the dog adjacent enough that its
    ///   AIRetriever NoticeRadius=10 sees the broadcast, but OUT of
    ///   the east throw trajectory so the bone doesn't hit the dog
    ///   and the dog doesn't get accidentally damaged by the throw.
    /// - Player inventory contains a Bone (lightweight throwable, newly
    ///   blueprinted — was substituted with Dagger in earlier iterations
    ///   before the Bone blueprint existed).
    /// - Player throws the bone east (inventory → Throw action →
    ///   direction east). ItemLandedEvent fires on the PetDog.
    /// - PetDog is on the Villagers faction (the player is on "Player"
    ///   faction, which FactionManager treats as allied with Villagers),
    ///   so the AlliesOnly=true gate passes.
    /// - Dog pushes GoFetchGoal(bone, returnHome: false) — walks to
    ///   the bone's landing cell, picks it up.
    /// - Look at the PetDog's inventory afterward — it now has the
    ///   bone.
    ///
    /// Note on village NPCs: the starting zone drops the player into
    /// the village, which has Elders/Villagers with their own
    /// inventories who WILL also pick up nearby items. In a laggy or
    /// fair-chance run, a shop NPC may grab the thrown bone before
    /// the dog arrives. To see a clean fetch, throw immediately after
    /// the scenario applies. This is emergent first-come-first-served
    /// behavior from the item-pickup economy, not an M3.2 bug.
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
            // and pathfinding are clean. Also clear the dog's cell
            // (player+2 east, 1 north) so the spawn placement succeeds
            // even if the starting-zone generator dropped a chest/stone there.
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 7; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);
            ctx.World.ClearCell(p.x + 2, p.y - 1);

            // Dog offset diagonally (east + north) so it's NOT in the
            // east throw trajectory — otherwise the thrown bone hits
            // the dog and damages it. Still within the AIRetriever's
            // NoticeRadius of 10, so ItemLanded reaches it.
            ctx.Spawn("PetDog").AtPlayerOffset(2, -1);

            // Equip the player with a throwable Bone (weight 1, takeable,
            // Handling with Throwable=true — see Objects.json Bone blueprint).
            ctx.Player.GiveItem("Bone", 1);

            ctx.Log("Open inventory ('i'), pick the bone, action menu Throw ('t'), aim east. Dog fetches it.");
        }
    }
}
