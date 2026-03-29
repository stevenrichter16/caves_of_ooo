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
        // Deprecated for world-space rendering. Retained only for blueprint compatibility.
        public string DetailColor = "";
        // Deprecated for world-space rendering. Retained only for blueprint compatibility.
        public string TileColor = "";
        // Deprecated for world-space rendering. Retained only for blueprint compatibility.
        public string Tile;
        public int RenderLayer;
        public bool Visible = true;

        public override bool HandleEvent(GameEvent e)
        {
            return true;
        }
    }
}
