namespace CavesOfOoo.Core
{
    /// <summary>
    /// Gas cloud rendering helper. Gives gas the Qud-style "densely packed
    /// dots" look by mapping density to a CP437 shade glyph (░ ▒ ▓) — thin
    /// edges read as a light stipple, dense cores as a solid block — and
    /// keeps the gas entity's <see cref="RenderPart"/> in sync as density
    /// changes, marking the cell dirty so the renderer actually repaints it.
    ///
    /// <para><b>Why this exists.</b> Gas clouds were invisible in-game: the
    /// renderer only repaints cells flagged via
    /// <see cref="ZoneRenderHooks.MarkCellDirty"/>, and nothing in the gas
    /// system ever flagged a gas cell. So a cloud sat in the zone, fully
    /// simulated, but its cell was never redrawn. <see cref="Refresh"/>
    /// fixes both halves at once — sets the density-appropriate glyph AND
    /// marks the cell dirty.</para>
    ///
    /// <para><b>CP437 shade blocks.</b> The tileset is indexed by byte-as-
    /// char, so the shade glyphs are chars 176/177/178 (░ ▒ ▓). Color comes
    /// from the gas's <see cref="GasPoolPart.ColorString"/> (so a poison
    /// cloud is green shades, stun yellow, etc.) — glyph encodes density,
    /// color encodes type.</para>
    /// </summary>
    public static class GasVisuals
    {
        public const char SHADE_LIGHT  = (char)176; // ░
        public const char SHADE_MEDIUM = (char)177; // ▒
        public const char SHADE_DARK   = (char)178; // ▓

        /// <summary>Density ≥ this → medium shade ▒.</summary>
        public const int MEDIUM_THRESHOLD = 30;
        /// <summary>Density ≥ this → dark shade ▓ (dense core).</summary>
        public const int DARK_THRESHOLD = 80;

        /// <summary>Map density to a shade glyph: dense core ▓, mid ▒, thin
        /// edges ░. Inclusive thresholds. Monotonic (denser is never
        /// lighter), so a spreading cloud reads as a dark center fading to
        /// a light fringe.</summary>
        public static char GlyphForDensity(int density)
        {
            if (density >= DARK_THRESHOLD) return SHADE_DARK;
            if (density >= MEDIUM_THRESHOLD) return SHADE_MEDIUM;
            return SHADE_LIGHT;
        }

        /// <summary>Sync the gas entity's glyph to its current density and
        /// mark its cell dirty so the renderer repaints the cloud. Call
        /// after spawn and after any density change. Null-safe; the dirty
        /// mark is a no-op when no renderer is attached (test harness).</summary>
        public static void Refresh(Entity gas, GasPoolPart pool, Zone zone)
        {
            if (gas == null || pool == null) return;
            var render = gas.GetPart<RenderPart>();
            if (render != null)
                render.RenderString = GlyphForDensity(pool.Density).ToString();
            if (zone != null)
            {
                var pos = zone.GetEntityPosition(gas);
                if (pos.x >= 0)
                    ZoneRenderHooks.MarkCellDirty(pos.x, pos.y, "Gas");
            }
        }
    }
}
