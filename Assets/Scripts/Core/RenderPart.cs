namespace CavesOfOoo.Core
{
    /// <summary>
    /// Controls how an entity appears on the grid.
    /// Mirrors Qud's Render part: display name, glyph, color, tile, and render layer.
    /// This is pure data — the actual drawing is done by a Unity-side renderer.
    /// </summary>
    public class RenderPart : Part
    {
        public override string Name => "Render";

        public string DisplayName;
        public string RenderString = "?";
        public string ColorString = "&y";
        public string DetailColor = "";
        public string TileColor = "";
        public string Tile;
        public int RenderLayer;
        public bool Visible = true;

        public override bool HandleEvent(GameEvent e)
        {
            return true;
        }
    }
}
