using System.Linq;
using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Hand-play fixture for the machines built after the camp kits (Docs/MACHINE-INVENTED-USES.md 3f-3j), on wild ground: a steeping trough kit with leather and cloth garments, pitchpod resin and
    /// fire clay; a sounding rod; a camp hearth and still with timber, a striker and two flasks of bog-mire (to bake into fire clay); and what Cinderhold's mine pump asks for, so the whole
    /// chain (bake clay, patch the pump, steep a garment) can be tried in one sitting. It grants items only: every verb afterwards is the ordinary one.
    /// </summary>
    [Scenario(
        name: "Camp Machines (wild ground)",
        category: "Home",
        description: "Starts on wild ground with the steeping trough, a sounding rod, a hearth and still with two flasks of mire, garments, and the pump's repair parts, for hand-testing the newer machines.")]
    public class CampMachinesShowcase : IScenario
    {
        /// <summary>What the pack receives, in order. Pinned against the real content by a test.</summary>
        public static readonly (string Blueprint, int Count)[] Loadout =
        {
            ("CampTrough", 1), ("CampHearth", 1), ("CampStill", 1),
            ("LeatherArmor", 1), ("KilnfeltApron", 1),            // two garments the trough accepts
            ("PitchpodResin", 2), ("FireClay", 3),                 // two steepings' resin; clay for one steeping (1) and one clay-wash (2); bake more at the still
            ("SoundingRod", 1),
            ("SalvagedTimber", 6), ("FlintStriker", 1), ("Torch", 1),
            ("LiquidFlask", 2),                                    // filled with bog-mire in Apply: each bakes into one fire clay
            ("IronSpikeComponent", 2), ("OakHaftComponent", 1), ("LeatherBindingComponent", 1),   // Cinderhold's pump, mended for good
        };

        public const string MireLiquid = CampKitsShowcase.MireLiquid;
        public const int MireUnits = 12;

        public void Apply(ScenarioContext ctx)
        {
            foreach (var (blueprint, count) in Loadout) ctx.Player.GiveItem(blueprint, count);
            foreach (var vessel in (ctx.PlayerEntity?.GetPart<InventoryPart>()?.Objects ?? new System.Collections.Generic.List<Entity>()).Where(o => o != null && o.BlueprintName == "LiquidFlask").Select(o => o.GetPart<LiquidVesselPart>()).Where(v => v != null))
            { vessel.LiquidId = MireLiquid; vessel.Volume = MireUnits; }
            ctx.Log("[Machines] You start on wild ground with a steeping trough, a hearth and a still, two flasks of bog-mire, two garments, pitchpod resin, a sounding rod and the parts Cinderhold's pump asks for.");
            ctx.Log("[Machines] 1. Open the pack, pick the hearth and choose 'set down' east, then the still west ('make camp here' lays out every kit at once). 2. Press C toward the hearth: highlight 'stoke' and read what it does, then choose it. 3. Press C toward the still: it shows your mix status and, beside the lit fire, offers 'bake 12 mire to 1 fire clay' for each flask (each uses 8 mire and 10 fuel; settle some mire instead to compare).");
            ctx.Log("[Machines] 4. Set down the trough (pack, 'set down'), press C toward it and choose 'steep leather armor': it takes 1 pitchpod resin and 1 fire clay. Examine the armor to read the steeping, wear it, and check your acid resistance; take it off and the turns that remain are kept. Then try 'clay-wash' on the apron: 2 fire clay, +35 heat resistance for 100 turns of wear (bake more clay at the still beside a lit fire). 4b. Pour one of the mire flasks onto your lit hearth (pack, the flask, 'pour ... east'): the row says what it would do. A full flask puts the fire out and the hearth keeps its fuel; stoke and light it again from a flame.");
            ctx.Log("[Machines] 5. Cinderhold's mine pump: travel to Cinderhold (Overworld.6.6.0, north-west on the world map), find the pump beside the well, press C: its menu shows what each repair needs against what you carry; 'mend for good' uses the two iron spikes, the oak haft and the binding. The pump then reads 'running'.");
            ctx.Log("[Machines] 6. The sounding rod works only in an ordinary deep cave: take any stairs down, step in, open the pack, choose 'sound the ground'. It names open ground, a way through and dense stone you have not seen, as a bearing and a distance, and the strike is loud.");
        }
    }
}
