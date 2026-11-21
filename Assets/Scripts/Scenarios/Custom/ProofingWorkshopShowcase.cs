using System.Linq;
using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Hand-play fixture for proofing (Docs/MACHINE-PROOFING.md): the player starts on wild ground with every machine, every reagent, flasks, fire clay and timber, status tonics, the four elemental spell books and
    /// ten full sets of armor, beside a proving post that calls enemies that burn, freeze, shock or corrode (and sends them away and mends you), with a range of environmental hazards for each element
    /// laid out in four corners so one element is tested at a time. It only grants items and places objects: every verb afterwards is the ordinary one.
    /// </summary>
    [Scenario(
        name: "Proofing Workshop (everything, 10 armor sets, hazards, elemental foes)",
        category: "Home",
        description: "Wild ground with the trough, hearth, still, every reagent, flasks, fire clay, ten full armor sets, a proving post that calls elemental enemies, and heat, cold, acid and shock hazards to test resistances on.")]
    public class ProofingWorkshopShowcase : IScenario
    {
        /// <summary>Ten full outfits, five slots each (body, head, hands, feet, back), from the armor the game has. Pieces repeat across sets, so the pack holds stacks.</summary>
        public static readonly string[][] ArmorSets =
        {
            new[] { "LeatherArmor", "LeatherCap", "LeatherGloves", "LeatherBoots", "Cloak" },
            new[] { "LeatherArmor", "LeatherCap", "LeatherGloves", "LeatherBoots", "WardedCloak" },
            new[] { "LeatherArmor", "FilterHood", "LeatherGloves", "LeatherBoots", "ColdwardCloak" },
            new[] { "ChainMail", "IronHelmet", "LeatherGloves", "IronshodBoots", "Cloak" },
            new[] { "ChainMail", "IronHelmet", "LeatherGloves", "IronshodBoots", "WardedCloak" },
            new[] { "ChainMail", "IronHelmet", "Buckler", "IronshodBoots", "ColdwardCloak" },
            new[] { "PlateArmor", "IronHelmet", "LeatherGloves", "IronshodBoots", "Cloak" },
            new[] { "PlateArmor", "IronHelmet", "IronBuckler", "IronshodBoots", "WardedCloak" },
            new[] { "KilnfeltApron", "LeatherCap", "LeatherGloves", "LeatherBoots", "ColdwardCloak" },
            new[] { "AcidworkerApron", "FilterHood", "GroundwireScreen", "LeatherBoots", "Cloak" },
        };

        /// <summary>The machines, the reagents (all ten, eight of each), the fixings, the status tonics and the four elemental spell books. The armor sets are given from <see cref="ArmorSets"/>.</summary>
        public static readonly (string Blueprint, int Count)[] Loadout =
        {
            ("CampTrough", 1), ("CampSettlingTray", 2), ("HushStrip", 8), ("CampHearth", 1), ("CampStill", 1), ("CampBench", 1), ("CampBox", 1),
            ("FireMoss", 8), ("EmberFruit", 8), ("PitchpodResin", 8), ("FrostLichen", 8), ("GlacierSalt", 8), ("ChillcressTip", 8), ("SparkRoot", 8), ("PrismreedPith", 8), ("GlimmerBrine", 8), ("SourmantleFold", 8),
            ("LiquidFlask", 12), ("FireClay", 24), ("SalvagedTimber", 30), ("FlintStriker", 1), ("Torch", 3),
            ("PitchpodSeed", 4), ("ChillcressSeed", 4), ("PrismreedSeed", 4), ("SourmantleSeed", 4),
            ("FrostTonic", 6), ("LightningTonic", 6),
            ("ArcBoltGrimoire", 1), ("IceLanceGrimoire", 1), ("EmberVeinGrimoire", 1), ("AcidSprayGrimoire", 1),
        };

        public const int MireFlasks = 4, MireUnits = 12;

        /// <summary>Hazards by element: blueprint and offsets from the player. Heat is north-east, cold north-west, acid south-east and shock south-west, at least twelve cells apart from each other.</summary>
        public static readonly (string Blueprint, int Dx, int Dy)[] Range =
        {
            ("SteamVent", 8, -5), ("SteamVent", 10, -5), ("AshBed", 8, -7), ("AshBed", 10, -7), ("FireTrap", 12, -5), ("FireTrap", 12, -7),
            ("FrostVent", -8, -5), ("FrostVent", -10, -5), ("IceSheet", -8, -7), ("IceSheet", -9, -7), ("IceSheet", -10, -7), ("RuneOfFrost", -12, -5), ("RuneOfFrost", -12, -7),
            ("AcidPool", 8, 5), ("AcidPool", 9, 5), ("AcidPool", 8, 6), ("AcidPool", 9, 6), ("MirePool", 11, 5), ("MirePool", 12, 5), ("MirePool", 11, 6), ("MirePool", 12, 6),
            ("BrinePool", -8, 5), ("BrinePool", -9, 5), ("BrinePool", -8, 6), ("BrinePool", -9, 6), ("BrinePool", -11, 5), ("BrinePool", -12, 5),
        };

        public void Apply(ScenarioContext ctx)
        {
            // Room first: a test character strong enough to carry the whole workshop and to survive the range between trials.
            ctx.Player.SetStatMax("Strength", 120).SetStat("Strength", 70).SetStatMax("Hitpoints", 400).SetStat("Hitpoints", 400);
            var pack = ctx.PlayerEntity?.GetPart<InventoryPart>(); if (pack != null && pack.MaxWeight >= 0) pack.MaxWeight = System.Math.Max(pack.MaxWeight, 5000);   // a hard carry cap, where one is set, must not drop part of the workshop
            foreach (var (blueprint, count) in Loadout) ctx.Player.GiveItem(blueprint, count);
            foreach (var set in ArmorSets) foreach (var piece in set) ctx.Player.GiveItem(piece);
            foreach (var vessel in (ctx.PlayerEntity?.GetPart<InventoryPart>()?.Objects ?? new System.Collections.Generic.List<Entity>()).Where(o => o != null && o.BlueprintName == "LiquidFlask").Select(o => o.GetPart<LiquidVesselPart>()).Where(v => v != null).Take(MireFlasks))
            { vessel.LiquidId = CampMachinesShowcase.MireLiquid; vessel.Volume = MireUnits; }
            int missed = 0; foreach (var (blueprint, dx, dy) in Range) if (ctx.Spawn(blueprint).AtPlayerOffset(dx, dy) == null) missed++;
            bool post = ctx.Spawn("ElementalProvingPost").AtPlayerOffset(1, 0) != null;
            if (!post) ctx.Log("[Workshop] The cell beside you was not clear, so no proving post was placed: step 5 is unavailable here.");
            if (missed > 0) ctx.Log("[Workshop] " + missed + " of the " + Range.Length + " hazards could not be placed (the ground was not clear there).");
            ctx.Log("[Workshop] You start on wild ground with every proofing machine and reagent, flasks, fire clay, ten full sets of armor (open the pack), status tonics and the four elemental spell books. A proving post stands beside you; heat hazards lie north-east, cold north-west, acid south-east, shock south-west.");
            ctx.Log("[Workshop] 1. Press reagents into flasks: choose a reagent in the pack, 'press into a flask'. Frost lichen, glacier salt and chillcress make cold; fire moss, ember fruit and pitchpod make heat; spark root and prismreed make shock; sourmantle makes acid; glimmer brine makes shock or acid. 2. Coat: choose a flask of proofing, 'coat <garment>' (+35 for 100 worn turns; works on a stack of identical armor, one unit at a time). 3. For good: set down the proofing trough (pack, 'set down'), put 4 units of one element in your flasks, and 'proof <garment> against <element> for good' (4 units and 2 fire clay for +25; one lasting proofing a garment). 4. Make more clay: set down the hearth and still, stoke the fire and bake the four mire flasks (8 mire and 10 fuel per clay), or plant the seeds for more reagents. 4b. Catch reagent from the hazards for nothing: carry a settling tray to any hazard (a steam vent, ash bed, frost vent, ice sheet, or a pool of brine, acid or mire) and press C on the tray kit in your pack: its set-down rows say what each cell would catch (the cell beside a hazard is refused, and so is the cell in line beyond an ash bed or frost vent, which warm and chill what they touch; a cell a little off the line works). It settles one level-one reagent of that hazard's element every half day (four at most, from the hazard's own supply, which refills a day at a time). Rest a few hours, press C beside it, 'gather'; the catch cannot be sold.");
            ctx.Log("[Workshop] 5. Test: wear a set (open the pack, equip each piece). Press C beside the proving post: call a Marlback cindercaller or charred husk (heat), ice wight or rune cultist (cold), Marlback stormbinder or vault sentinel (shock), Marlback soursprayer or cave slime (acid); 'send away' removes what it called; 'mend yourself' restores health and clears statuses (a coat stays). Walk the hazards: steam vents, ash beds and fire traps for heat; frost vents, ice and frost runes for cold; acid pools and bog mire for acid; brine for shock (zap yourself with a lightning tonic, or cast an arc bolt, while standing in it). Drink a frost or lightning tonic to put a status on yourself. Your resistance is on the inventory screen's status panel.");
        }
    }
}
