using System.Linq;
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
            ("IronSpikeComponent", 2), ("OakHaftComponent", 1), ("LeatherBindingComponent", 1), // a craftable set for the bench, and a second spike to knap the striker with
            ("MendleafSprig", 2), ("FireMoss", 1),                                  // reagents for the still
            ("FlintStriker", 1),                                                    // a flame where none burns: light the torch or hearth, or spark an oil slick
            ("LiquidFlask", 1),                                                     // filled with bog-mire in Apply, for settling at the still
        };

        /// <summary>A real, lit campfire placed beside the player: the "nearby fire" a torch is lit at.</summary>
        public const string FireBlueprint = "Campfire";

        /// <summary>The flask the loadout hands over is filled with this liquid, so the still's settling can be tried without a trip to a mire. Kept in step with the still's feed by a test.</summary>
        public const string MireLiquid = "bog-mire";
        public const int MireUnits = 12;

        /// <summary>A short-lived oil slick west of the player, for trying the flint striker's spark.</summary>
        public const string SlickLiquid = "oil";
        public const int SlickTurns = 300;

        public void Apply(ScenarioContext ctx)
        {
            foreach (var (blueprint, count) in Loadout) ctx.Player.GiveItem(blueprint, count);
            var vessel = ctx.PlayerEntity?.GetPart<InventoryPart>()?.Objects.FirstOrDefault(o => o != null && o.BlueprintName == "LiquidFlask")?.GetPart<LiquidVesselPart>();
            if (vessel != null) { vessel.LiquidId = MireLiquid; vessel.Volume = MireUnits; }
            ctx.World.PlaceObject(FireBlueprint).AtPlayerOffset(2, 0);
            var at = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            if (at.x >= 0) ctx.Zone.TileState.WriteCoating(at.x - 2, at.y, SlickLiquid, SlickTurns); // an oil slick two cells west: stand beside it and "strike a spark"
            ctx.Log("[CampKits] Four kits, 6 timber, a torch, a flint striker, 2 iron spikes, craft parts, reagents, raw meat, a dagger and a flask of bog-mire are in your pack. A campfire is two cells east and an oil slick two cells west.");
            ctx.Log("[CampKits] 1. Flint: a real fire in reach is always used first, so stay here (the campfire is two cells away, out of reach). Equip the torch, extinguish it if it is burning, then choose 'light torch at nearby fire': it strikes the flint instead. 2. Stand beside the oil slick (one step west), press C and the direction of the slick, and choose 'light it with the flint striker' (a lit torch in hand lights it free). The striker's own pack menu offers 'strike a spark' too.");
            ctx.Log("[CampKits] 3. This opening zone refuses set-down. Open the world map with < and travel to ordinary wild ground (Overworld.10.11.0, one west and one south, is one), then use 'set down' on the hearth, the still and the bench. 4. Hearth: press C toward it, feed 2 timber, relight it from the striker (its glow and flicker come on at once, and its menu always shows its fuel and turns), then rest beside it (it burns about 15 fuel for the night). 5. Still: with the hearth lit within two cells, settle the mire flask to water or oil.");
            ctx.Log("[CampKits] Bench: mark the spike, haft and binding in its picker and craft. The box takes your dagger. Open the world map afterwards to see where you keep things.");
        }
    }
}
