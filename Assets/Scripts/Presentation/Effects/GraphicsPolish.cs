namespace CavesOfOoo.Presentation.Effects
{
    /// <summary>
    /// Master gate for Pass 1-11 visual polish layers.
    ///
    /// When <see cref="IsEnabled"/> is <c>false</c> (the shipping default),
    /// the game renders pure CP437 glyphs with NONE of the following:
    ///   - URP Volume post-processing (bloom, vignette, color grading,
    ///     chromatic aberration) — Pass 1-3
    ///   - HitStop crit-freeze — Pass 4 §4A
    ///   - CRT phosphor overlay — Pass 4 §4B
    ///   - Animated environment shader (water UV scroll, grass sway) — Pass 5
    ///   - Motion-ghost trails behind moving entities — Pass 6 §6A
    ///   - Biome color grading — Pass 6 §6B
    ///   - Hybrid sprite environment (walls/floors/water/doors → 16×16
    ///     pixel-art tiles + 15 Pass 8 sprites + chest/lantern/bed/corpse
    ///     blueprint-resolved sprites) — Pass 7-11
    ///   - URP 2D Light2D point lights on campfires / shrines / lanterns
    ///     and the player-held torch + biome-dim ambient — Pass 8-10
    ///
    /// Flip this constant to <c>true</c> and recompile to bring back
    /// everything Pass 1-11 added. All sprite PNGs stay on disk; the
    /// gate is purely a wiring-layer skip in the bootstrap path. No
    /// content needs to change.
    /// </summary>
    public static class GraphicsPolish
    {
        /// <summary>
        /// Master enable. <c>false</c> = pure CP437.
        /// </summary>
        public const bool IsEnabled = false;
    }
}
