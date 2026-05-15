namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part attached to the player that records where they ascended
    /// from. On <see cref="WorldMapTraversal.Ascend"/> we save the
    /// current ground zone + cell so a later
    /// <see cref="WorldMapTraversal.Descend"/> can drop the player
    /// back at the same exact spot — mirrors Qud's
    /// <c>LastLocationOnSurface</c> (XRLCore.cs:1382).
    ///
    /// <para>The fields default-empty. <see cref="HasSavedSurface"/>
    /// returns true once Ascend has run at least once.</para>
    /// </summary>
    public class WorldMapPart : Part
    {
        public override string Name => "WorldMap";

        /// <summary>The ZoneID of the ground zone the player was last
        /// on when they ascended. Empty means never ascended.</summary>
        public string LastZoneIDOnSurface = "";

        /// <summary>X coordinate inside <see cref="LastZoneIDOnSurface"/>.</summary>
        public int LastZoneX = -1;

        /// <summary>Y coordinate inside <see cref="LastZoneIDOnSurface"/>.</summary>
        public int LastZoneY = -1;

        public bool HasSavedSurface =>
            !string.IsNullOrEmpty(LastZoneIDOnSurface)
            && LastZoneX >= 0 && LastZoneY >= 0;
    }
}
