using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Builders;

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

        /// <summary>Gives the loadout, fills the flask with mire, and lays down the campfire and the oil slick. Shared by the opening-zone scenario and the two wild-ground ones.</summary>
        public static void Supply(ScenarioContext ctx)
        {
            ScenarioLoadout.Give(ctx, Loadout, "[CampKits]");
            var vessel = ctx.PlayerEntity?.GetPart<InventoryPart>()?.Objects.FirstOrDefault(o => o != null && o.BlueprintName == "LiquidFlask")?.GetPart<LiquidVesselPart>();
            if (vessel != null) { vessel.LiquidId = MireLiquid; vessel.Volume = MireUnits; }
            ctx.World.PlaceObject(FireBlueprint).AtPlayerOffset(2, 0);
            var at = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            if (at.x >= 0) ctx.Zone.TileState.WriteCoating(at.x - 2, at.y, SlickLiquid, SlickTurns); // an oil slick two cells west: stand beside it and "strike a spark"
        }

        public void Apply(ScenarioContext ctx)
        {
            Supply(ctx);
            ctx.Log("[CampKits] Four kits, 6 timber, a torch, a flint striker, 2 iron spikes, craft parts, reagents, raw meat, a dagger and a flask of bog-mire are in your pack. A campfire is two cells east and an oil slick two cells west. Highlight any menu row to read what it does.");
            ctx.Log("[CampKits] 1. Flint: a real fire in reach is always used first, so stay here (the campfire is two cells away, out of reach). Equip the torch, extinguish it if it is burning, then choose 'light torch at nearby fire': it strikes the flint instead. 2. Stand beside the oil slick (one step west), press C and the direction of the slick, and choose 'light it with the flint striker' (a lit torch in hand lights it free). The striker's own pack menu offers 'strike a spark' too.");
            ctx.Log("[CampKits] 3. This opening zone refuses set-down. Open the world map with < and travel to ordinary wild ground (or use the 'wild ground' variants of this scenario, which start there), then open the pack, pick a kit and choose 'make camp here' to lay out every kit at once, or 'set down' one at a time. 4. Hearth: press C toward it; 'stoke' feeds the timber and lights it in one action. Its menu always shows its fuel and turns, and its glow comes on at once. Then rest beside it. 5. Still: with the hearth lit within two cells, settle the mire to water or oil. 6. On any standing kit: 'move', 'pack up camp' and 'name this camp'.");
            ctx.Log("[CampKits] Bench: mark the spike, haft and binding in its picker and craft. The box takes your dagger. Open the world map afterwards to see where you keep things and what each camp holds.");
        }
    }

    /// <summary>
    /// The same fixture, but the fresh game starts on ordinary wild ground (the nearest Spread zone the camp-kit rules accept) so every set-down verb works at once. Launch it from the
    /// Home menu: the start is requested through <c>ScenarioRunner.LaunchWild</c>; run any other way it behaves as the opening-zone scenario.
    /// </summary>
    [Scenario(
        name: "Camp Kits (wild ground)",
        category: "Home",
        description: "Starts on ordinary wild ground with the camp kits, timber, a striker and mire, so make camp, stoke, rest, move, pack up and name can be tried without leaving the start.")]
    public class CampKitsWildShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            CampKitsShowcase.Supply(ctx);
            ctx.Log("[CampKits wild] You start on ordinary wild ground: set-down works here. Everything for a camp is in your pack.");
            ctx.Log("[CampKits wild] 1. Open the pack (i), select a camp kit and choose 'make camp here (4 kits)': the hearth goes down first, the still beside it, the bench and box clear of the flames. 2. Press C toward the hearth and highlight 'stoke': the small text says what it does. Choose it: it feeds the timber and lights the fire from the striker in one action. 3. Press C toward the still and read what your mire would settle to; settle it. 4. Press C on any kit and choose 'name this camp' (press it again to change the name). 5. Open the world map: the note names the camp, where it is and what it holds as you left it. 6. 'move' shifts one kit a cell; 'pack up camp' takes everything within four cells back.");
        }
    }

    /// <summary>The wild-ground fixture at night and hurt: the reason to make a camp. The clock starts at the beginning of the Dark band and the player at two fifths health.</summary>
    [Scenario(
        name: "Camp Kits (wild ground, night, hurt)",
        category: "Home",
        description: "Wild ground, the start of the Dark band, two fifths health: make camp, stoke the hearth and rest until dawn.")]
    public class CampKitsWildNightShowcase : IScenario
    {
        public const float HealthFraction = 0.4f;

        /// <summary>The tick the scenario moves the clock to: the first tick of the Dark band, or the current tick when it is already Dark (the clock never runs backwards, so a game past this day's Dark goes to the next day's).</summary>
        public static int DarkStart(int tick)
        {
            if (WorldClock.GetBand(tick) == DayBand.Dark) return tick;
            int dayStart = tick - tick % WorldClock.DayLengthTicks;
            int target = dayStart + (int)DayBand.Dark * WorldClock.BandLengthTicks;
            return target >= tick ? target : target + WorldClock.DayLengthTicks;
        }

        public void Apply(ScenarioContext ctx)
        {
            CampKitsShowcase.Supply(ctx);
            ctx.Player.SetHpFraction(HealthFraction);
            if (ctx.Turns != null) ctx.Turns.AdvanceClock(DarkStart(ctx.Turns.TickCount) - ctx.Turns.TickCount);
            ctx.Log("[CampKits night] It is night and you are hurt. A camp is the way through it.");
            ctx.Log("[CampKits night] 1. Open the pack, pick a camp kit and choose 'make camp here (4 kits)'. 2. Press C toward the hearth and choose 'stoke' (highlight it first to read what it does): it feeds the timber and lights it. 3. Press C on the hearth again: 'rest until Dawn' shows what the night heals and how much fuel it burns; choose it. 4. Wake at dawn: the fire has burned its fuel and your health is restored. 5. Try 'pack up camp' to leave, then the world map to see what you left.");
        }
    }
}
