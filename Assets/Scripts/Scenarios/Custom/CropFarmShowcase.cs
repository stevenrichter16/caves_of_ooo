using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Crops &amp; Watering Grimoire showcase — the manual-playtest half
    /// of <c>Docs/CROPS-WATERING-GRIMOIRE.md</c> (§2.9 honesty bounds):
    /// EditMode tests pin every state transition, diag record, and FX
    /// REQUEST, but they cannot verify how the rain looks in motion,
    /// whether the wet-soil background reads as "wet earth" on screen,
    /// or stage-glyph readability. This scenario exists to eyeball
    /// exactly those things.
    ///
    /// <para><b>Layout:</b> a 5×3 grass plot east of the player. One
    /// row arrives pre-planted mid-growth (dry — paused), so a single
    /// Conjure Rain cast immediately shows: rain drops falling over
    /// multiple tiles, soil darkening under every watered crop, and
    /// growth resuming.</para>
    ///
    /// <para><b>What to verify visually (cannot be script-verified):</b></para>
    /// <list type="number">
    ///   <item>Cast Conjure Rain (hotbar; self-centered, no cursor):
    ///         blue drops fall from ~2 tiles above each crop onto it,
    ///         staggered, followed by a small Water splash.</item>
    ///   <item>Every watered crop tile's background darkens to a wet
    ///         brown block; unwatered grass outside the radius keeps
    ///         its normal look.</item>
    ///   <item>Wait ~40 turns: the wet backgrounds fade (soil dries)
    ///         and dry crops stop growing.</item>
    ///   <item>Keep watering the candy carrots (~40 moist ticks total):
    ///         seed '.' → sprout 't' glyph/color swap, then the crop
    ///         disappears and an orange '%' candy carrot lies there.
    ///         Walk over and pick it up.</item>
    ///   <item>Plant a seed from inventory ("plant" action) on grass —
    ///         works; try it on plain stone floor — rejected with a
    ///         message.</item>
    /// </list>
    /// </summary>
    [Scenario(
        name: "Crop Farm (Watering Grimoire)",
        category: "World Systems",
        description: "Plant crops, cast Conjure Rain from the grimoire, watch the rain fall, the dirt darken, and the crops grow. Pre-planted plot east of spawn; seeds + grimoire in inventory.")]
    public class CropFarmShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Grimoire spell pre-taught so the demo is one keypress in;
            // the grimoire ITSELF is also in inventory so "read → learn"
            // can be shown on a fresh character (it reports already-known
            // here — that's the correct duplicate-guard behavior).
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .AddMutation(nameof(ConjureRainMutation), 1)
                .GiveItem("WateringGrimoire")
                .GiveItem("CandyCarrotSeed", 4)
                .GiveItem("EmberwheatSeed", 2);

            // === The plot: 5×3 grass patch east of the player ===
            for (int dx = 2; dx <= 6; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x = p.x + dx, y = p.y + dy;
                    if (!ctx.Zone.InBounds(x, y)) continue;
                    var cell = ctx.Zone.GetCell(x, y);
                    if (cell != null && !cell.HasObjectWithTag("Terrain"))
                        ctx.Spawn("Grass").At(x, y);
                }
            }

            // === Pre-planted middle row (dry, mid-growth: paused) ===
            // Three candy carrots part-way through the seed stage and one
            // emberwheat, so one rain cast visibly resumes several crops.
            for (int i = 0; i < 3; i++)
            {
                var cropEntity = ctx.Spawn("CandyCarrotCrop").At(p.x + 2 + i, p.y);
                var crop = cropEntity?.GetPart<CropPart>();
                if (crop != null)
                    crop.TicksInStage = 10; // halfway through the 20-tick seed stage
            }
            ctx.Spawn("EmberwheatCrop").At(p.x + 5, p.y);

            // === Walk-through ===
            ctx.Log("=== Crop Farm Showcase ===");
            ctx.Log("A grass plot lies EAST. The middle row is pre-planted (dry).");
            ctx.Log("");
            ctx.Log("Try:");
            ctx.Log("  1) Cast Conjure Rain (hotbar) near the plot:");
            ctx.Log("     rain falls on each crop, the dirt under them darkens.");
            ctx.Log("  2) Wait/move ~turns: crops grow ONLY while the soil is dark.");
            ctx.Log("  3) Candy carrots: seed '.' -> sprout 't' -> '%' produce drops.");
            ctx.Log("  4) Plant your seeds on grass ('plant' in inventory).");
            ctx.Log("  5) Try planting on bare stone -> politely rejected.");
            ctx.Log("  6) Read the grimoire: already-known guard message.");
        }
    }
}
