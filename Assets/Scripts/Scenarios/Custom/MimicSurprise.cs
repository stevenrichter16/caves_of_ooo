using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Places a real Chest and a MimicChest (a Creature that looks like a chest)
    /// side by side, with a HealingTonic on the floor as a lure. Attack the wrong
    /// one and it attacks back.
    ///
    /// Phase 2d showcase: uses <c>ctx.World.PlaceObject</c> for the non-creature
    /// Chest and lure, and <c>ctx.Spawn</c> for the creature Mimic (which needs
    /// full brain/turn wiring). The two builders naturally complement each other.
    ///
    /// BLUEPRINT COMPENSATION: the <c>Chest</c> blueprint renders with
    /// ColorString <c>&amp;w</c> (white) and <c>MimicChest</c> renders with
    /// <c>&amp;Y</c> (bright yellow) — visually distinct, so a vanilla placement
    /// would give the Mimic away at a glance. This scenario overrides the
    /// Mimic's RenderPart.ColorString to match the real Chest, preserving the
    /// "which one is lying?" premise. If/when the MimicChest blueprint is
    /// updated to match Chest's color intrinsically, this override becomes a
    /// harmless no-op and can be removed.
    ///
    /// Note: MimicChest blueprint has <c>SightRadius=0</c> and
    /// <c>Physics.Solid=false</c> — so the mimic can't wake on sight (only on
    /// damage) and the player can walk through its cell without triggering it.
    /// The interaction is entirely attack-driven.
    ///
    /// Plan detail: originally called for a GoldPile lure, but that blueprint
    /// doesn't exist — swapped to HealingTonic per the plan's risk note.
    /// </summary>
    [Scenario(
        name: "Mimic Surprise",
        category: "Content Demo",
        description: "A real chest and a mimic side by side. Both render identically. Attack the wrong one and it bites back.")]
    public class MimicSurprise : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Real chest (decoy) 3 cells east of player.
            ctx.World.PlaceObject("Chest").AtPlayerOffset(3, 0);

            // Mimic disguised as a chest, 5 cells east. Force-match the Chest's
            // render color so the two look identical — blueprint divergence
            // (&w vs &Y) would otherwise give the trick away.
            var mimic = ctx.Spawn("MimicChest").AtPlayerOffset(5, 0);
            if (mimic != null)
            {
                var render = mimic.GetPart<RenderPart>();
                if (render != null)
                    render.ColorString = "&w"; // Match Chest's ColorString.
            }

            // Lure item — HealingTonic on the floor between them.
            ctx.World.PlaceObject("HealingTonic").AtPlayerOffset(4, 0);

            ctx.Log("Mimic Surprise applied. Two chests, one is lying — they render identically now.");
        }
    }
}
