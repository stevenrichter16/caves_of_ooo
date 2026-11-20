using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Hand-play fixture for the four set-down kits (Docs/HOME-BASE-IMPLEMENTATION.md): puts the camp box, hearth, bench and still in the pack with what each needs to be
    /// tried, and a campfire two cells east to light the torch at. It grants items only; every verb afterwards is the ordinary one (set down, take up, hand over, feed, light,
    /// craft, brew). Set-down is refused in villages, points of interest and the opening's own zones, so walk to ordinary wild ground first (the world map shows where; the
    /// scenario logs the reminder).
    /// </summary>
    [Scenario(
        name: "Camp Kits",
        category: "Home",
        description: "Gives the camp box, hearth, bench and still with timber, a torch, craft parts, reagents and a campfire to light the torch at, for hand-testing set down, take up and the work each kit allows.")]
    public class CampKitsShowcase : IScenario
    {
        /// <summary>What the pack receives, in order. Pinned against the real content by CampKitShowcaseTests.</summary>
        public static readonly (string Blueprint, int Count)[] Loadout =
        {
            ("CampBox", 1), ("CampHearth", 1), ("CampBench", 1), ("CampStill", 1),
            ("SalvagedTimber", 6),                                                  // hearth fuel: one timber is ten fuel, about forty turns
            ("Torch", 1),                                                           // the carried flame; light it at the campfire, then relight the hearth with it
            ("Dagger", 1), ("RawMeat", 2),                                          // something to store in the box; something to cook at the hearth
            ("IronSpikeComponent", 1), ("OakHaftComponent", 1), ("LeatherBindingComponent", 1), // a craftable set for the bench
            ("MendleafSprig", 2), ("FireMoss", 1),                                  // reagents for the still
            ("FlintStriker", 1),                                                    // a flame where none burns: light the torch or hearth, or spark an oil slick
        };

        /// <summary>A real, lit campfire placed beside the player: the "nearby fire" a torch is lit at.</summary>
        public const string FireBlueprint = "Campfire";

        /// <summary>A short-lived oil slick west of the player, for trying the flint striker's spark.</summary>
        public const string SlickLiquid = "oil";
        public const int SlickTurns = 300;

        public void Apply(ScenarioContext ctx)
        {
            foreach (var (blueprint, count) in Loadout) ctx.Player.GiveItem(blueprint, count);
            ctx.World.PlaceObject(FireBlueprint).AtPlayerOffset(2, 0);
            var at = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            if (at.x >= 0) ctx.Zone.TileState.WriteCoating(at.x - 2, at.y, SlickLiquid, SlickTurns); // an oil slick two cells west: stand beside it and "strike a spark"
            ctx.Log("[CampKits] four kits, 6 timber, a torch, craft parts, reagents and a dagger are in your pack; a campfire is two cells east. "
                + "Light the torch at it. This opening zone refuses set-down, so open the world map and travel to ordinary wild ground (Overworld.10.11.0, south-west, is one; villages and the opening zones refuse), then use 'set down' on each kit from the pack menu. "
                + "Hearth: feed timber, then relight it with the lit torch or the flint striker; with 15 fuel in it you can rest beside it (the fire burns about a quarter fuel for every tick you sleep). Bench: mark the spike, haft and binding in its picker and craft. Still: brew the reagents. "
                + "Open the world map afterwards to see where you keep things. "
                + "Flint striker: a real fire in reach is always used first, so to use it walk away from the campfire, then light the torch or relight the hearth, or stand beside the oil slick two cells west of where you started and choose 'strike a spark'.");
        }
    }
}
