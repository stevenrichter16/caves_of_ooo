namespace CavesOfOoo.Core
{
    /// <summary>
    /// Controls how an entity appears on the grid.
    /// World rendering is ASCII-first: RenderString, ColorString, and RenderLayer
    /// are the canonical world-space fields.
    /// Tile, TileColor, and DetailColor remain as imported/deprecated data for now,
    /// but the world renderer intentionally ignores them.
    /// </summary>
    public class RenderPart : Part
    {
        public override string Name => "Render";

        public string DisplayName;
        public string RenderString = "?";
        public string ColorString = "&y";
        /// <summary>
        /// Optional background color code (e.g. "^K" for dark gray background).
        /// Uses the same CGA palette as foreground but rendered as a solid
        /// block behind the glyph, darkened for readability.
        /// </summary>
        public string BackgroundColor = "";
        /// <summary>
        /// Optional glyph variation string. Each character is a possible glyph.
        /// When set, the renderer picks one deterministically based on cell position,
        /// giving terrain visual texture without randomness between frames.
        /// Example: ".,'" means the tile can appear as '.', ',', or '\''.
        /// </summary>
        public string GlyphVariants = "";
        // Deprecated for world-space rendering. Retained only for blueprint compatibility.
        public string DetailColor = "";
        // Deprecated for world-space rendering. Retained only for blueprint compatibility.
        public string TileColor = "";
        // Deprecated for world-space rendering. Retained only for blueprint compatibility.
        public string Tile;
        /// <summary>
        /// Optional stable key into the data-driven animated visual catalog. Empty
        /// values fall back to a blueprint mapping, then the static sprite/CP437 chain.
        /// </summary>
        public string VisualID = "";
        /// <summary>
        /// Stable palette/costume variant for authored overrides. The first animated
        /// slice does not consume this yet, but saves preserve it for later art passes.
        /// </summary>
        public string VisualVariant = "";
        /// <summary>
        /// Last meaningful facing. This is game-state metadata, unlike the transient
        /// animation frame, so save/load can restore an actor looking the same way.
        /// </summary>
        public EntityVisualFacing VisualFacing = EntityVisualFacing.South;
        public int RenderLayer;
        public bool Visible = true;

        /// <summary>
        /// Pick a glyph variant based on a cell position hash.
        /// Returns the base RenderString character if no variants are defined.
        /// </summary>
        public char ResolveGlyph(int x, int y)
        {
            if (string.IsNullOrEmpty(GlyphVariants))
                return RenderString != null && RenderString.Length > 0 ? RenderString[0] : '?';

            // Simple hash: mix x and y to get a stable per-cell index
            int hash = x * 374761393 + y * 668265263;
            hash = (hash ^ (hash >> 13)) * 1274126177;
            int index = ((hash >> 16) & 0x7FFF) % GlyphVariants.Length;
            return GlyphVariants[index];
        }

        public override bool HandleEvent(GameEvent e)
        {
            return true;
        }
    }
}
