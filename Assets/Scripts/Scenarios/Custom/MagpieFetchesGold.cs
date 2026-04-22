namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// M3.2 showcase — Magpie (AIHoarder with TargetTag="Shiny") + a
    /// GoldCoin on the ground. The magpie periodically scans on bored
    /// ticks (15% per tick), finds the coin, pushes
    /// GoFetchGoal(coin, returnHome: true), walks to the coin, picks
    /// it up, and returns to its spawn cell.
    ///
    /// Expected flow when launched:
    /// - Magpie wanders east of the player; GoldCoin two cells past it.
    /// - Every ~7 bored ticks on average (15% rate), Magpie scans,
    ///   finds the Shiny-tagged coin, pushes GoFetchGoal.
    /// - GoFetchGoal walks the Magpie to the coin, picks it up
    ///   (InventorySystem.Pickup), then walks back to the magpie's
    ///   starting cell — visible round trip.
    /// - After pickup the Magpie's StartingCell is where she was
    ///   spawned, so she returns there. The coin is now in her
    ///   Inventory (she "hoarded" it).
    /// - Skip turns with '.' to accelerate observation.
    ///
    /// Counter-experiment (for "ignore untagged" sanity): the plan's
    /// unit test AIHoarder_IgnoresItemsWithoutTargetTag pins this
    /// already. A manual demonstration would require swapping the
    /// GoldCoin for a Torch mid-session, which Scenarios can't do
    /// without re-running.
    ///
    /// Good for:
    /// - Verifying the AIBored → AIHoarder → GoFetchGoal(returnHome)
    ///   pipeline against a real Magpie blueprint
    /// - Observing the "walk to item, pick up, walk home" motion
    /// - Confirming the coin is actually stowed (look at the Magpie's
    ///   inventory after fetch)
    /// </summary>
    [Scenario(
        name: "Magpie Fetches Gold (M3.2)",
        category: "AI Behavior",
        description: "Magpie periodically swoops in on a Shiny GoldCoin and carries it home.")]
    public class MagpieFetchesGold : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Clear the east row of starting-zone hazards (compass stones +
            // chest) so the Magpie's fetch path is unobstructed.
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            for (int dx = 1; dx <= 7; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // GoldCoin 5 east — the target the Magpie will fetch.
            ctx.World.PlaceObject("GoldCoin").AtPlayerOffset(5, 0);

            // Magpie 3 east — close enough that its AIHoarder scan finds
            // the coin on the first bored tick that passes the Chance gate.
            ctx.Spawn("Magpie").AtPlayerOffset(3, 0);

            ctx.Log("Wait with '.'. Magpie eventually swoops on the GoldCoin and carries it back to its starting cell.");
        }
    }
}
