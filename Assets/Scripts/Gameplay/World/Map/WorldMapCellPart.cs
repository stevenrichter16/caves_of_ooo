namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marker Part placed on each terrain entity inside the embedded
    /// 20×20 region of the world-map zone. Carries the logical world
    /// coordinates so descent (<c>WorldMapTraversal.Descend</c>) and
    /// fog-of-war updates can look them up from the entity the player
    /// is standing on.
    ///
    /// <para>Created by <see cref="WorldMapZoneBuilder"/>. The entity
    /// it lives on also carries a <see cref="RenderPart"/> with the
    /// biome's glyph + color, and a passable <see cref="PhysicsPart"/>.</para>
    /// </summary>
    public class WorldMapCellPart : Part
    {
        public override string Name => "WorldMapCell";

        /// <summary>Logical world X (0..19).</summary>
        public int WorldX;

        /// <summary>Logical world Y (0..19).</summary>
        public int WorldY;
    }
}
