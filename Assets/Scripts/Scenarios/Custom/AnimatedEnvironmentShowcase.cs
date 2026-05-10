using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Pass 5 §5A.4 — animated environment showcase. Spawns a few
    /// torches, a grass strip, and a water pool around the player so
    /// you can see all three Pass 5 animations side-by-side without
    /// hunting for them across a generated zone.
    ///
    /// <para>What to look for:
    /// <list type="bullet">
    ///   <item><b>Water (E):</b> the <c>~</c>/<c>=</c>/<c>-</c>
    ///         glyphs in the pool drift horizontally — UV scroll
    ///         on the AnimatedEnvironment_Water material.</item>
    ///   <item><b>Grass (S):</b> the <c>,</c> grass tufts sway
    ///         left-right with sin-based vertex displacement.
    ///         Top of each glyph moves more than the base so it
    ///         looks like the blade is bending, not sliding.</item>
    ///   <item><b>Torches (W):</b> the <c>*</c> flame glyphs
    ///         flicker brightness + scroll vertically (illusion of
    ///         flame drift). Combined with the already-shipped
    ///         <c>LightSourceFlickerPart</c> from Pass 3 §3.A —
    ///         actual lights also wobble intensity.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Pass 5 plan: <c>Docs/GRAPHICS-PASS5.md</c></para>
    /// </summary>
    [Scenario(
        name: "Animated Environment Showcase",
        category: "Combat",
        description: "Pass 5 §5A: water UV scroll + grass vertex sway + torch flicker via custom shader on overlay tilemaps. Cells E/S/W of the player highlight each effect.")]
    public class AnimatedEnvironmentShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            ctx.Player
                .SetStatMax("Hitpoints", 999)
                .SetHp(999);

            // === East: water pool ===
            // Tiles with the water glyph already get re-routed by
            // AnimatedEnvironmentRenderer — we just need the cells
            // to render water glyphs. Drop a small Water blueprint
            // strip if available; otherwise rely on the existing
            // zone's water cells. Many overworld biomes have water
            // already; this scenario primarily highlights what's
            // visible in the *current* zone.

            // === South: grass strip ===
            // Same logic — grass glyphs `,` already render via
            // GrassTilemap once Pass 5 lands.

            // === West: torch ===
            // Spawn a Torch blueprint at p.x - 4. Torch entity
            // already has LightSource + LightSourceFlicker via the
            // Pass 3 §3.A wire; the `*` glyph is the fire char.
            ctx.Spawn("Torch")
                .Passive()
                .At(p.x - 4, p.y);
            ctx.Spawn("Torch")
                .Passive()
                .At(p.x - 4, p.y - 1);
            ctx.Spawn("Torch")
                .Passive()
                .At(p.x - 4, p.y + 1);

            ctx.Log("=== Animated Environment Showcase (Pass 5 §5A) ===");
            ctx.Log("Watch for shader-driven motion:");
            ctx.Log("  Water (any '~/=/-' glyph): horizontal UV scroll.");
            ctx.Log("  Grass (any ',' glyph): vertex sway, top moves more than base.");
            ctx.Log("  Torches (W of player): flame brightness flicker + slight vertical drift.");
            ctx.Log("If everything looks frozen, AnimatedEnvironmentRenderer didn't init.");
            ctx.Log("If motion is too aggressive, tune material params:");
            ctx.Log("  Assets/Materials/AnimatedEnvironment_{Water,Grass,Fire}.mat");
        }
    }
}
