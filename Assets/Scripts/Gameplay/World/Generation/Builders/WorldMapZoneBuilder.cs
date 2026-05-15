using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Builds the world-map zone — the singular Zone (ZoneID =
    /// <see cref="WorldMap.WorldMapZoneID"/>) the player physically
    /// inhabits when they ascend (<c>&lt;</c>) from a ground zone.
    /// Mirrors Qud's pattern: the world map IS a Zone, not a modal
    /// UI overlay. Distinguished by having no dots in its ZoneID.
    ///
    /// <para>Layout:</para>
    /// <list type="bullet">
    ///   <item>Fills 80×25 with Wall entities (impassable border).</item>
    ///   <item>Inside the region <c>[30..49] × [3..22]</c> (offsets
    ///         <see cref="WorldMap.WorldMapXOffset"/> /
    ///         <see cref="WorldMap.WorldMapYOffset"/>), removes walls
    ///         and creates one terrain entity per logical world cell.
    ///         Each terrain entity has a <see cref="WorldMapCellPart"/>
    ///         carrying <c>WorldX</c>/<c>WorldY</c>, a
    ///         <see cref="RenderPart"/> with biome-appropriate glyph
    ///         + color, and a passable <see cref="PhysicsPart"/>.</item>
    ///   <item>Top rows 0-2 reserved for a HUD strip (WM.6 paints it).</item>
    ///   <item>Bottom rows 23-24 reserved for the biome legend
    ///         (WM.6 paints it).</item>
    /// </list>
    ///
    /// <para>This builder is RNG-agnostic: the glyph/color of each
    /// cell is determined by the <see cref="WorldMap"/> data passed
    /// in at construction (so what the player sees on the map mirrors
    /// the per-cell biome assigned by <c>WorldGenerator</c>).</para>
    /// </summary>
    public class WorldMapZoneBuilder : IZoneBuilder
    {
        public string Name => "WorldMapZoneBuilder";

        // EARLY (1000) — same as BorderBuilder. The world-map zone has
        // no further builders today; if WM.8+ adds e.g. POI overlays
        // or random encounters, they should run AFTER (Priority > 1000).
        public int Priority => 1000;

        public string WallBlueprint = "Wall";

        private readonly WorldMap _worldMap;

        public WorldMapZoneBuilder(WorldMap worldMap)
        {
            _worldMap = worldMap;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (_worldMap == null) return false;

            // 1. Fill entire 80×25 with walls.
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    var wall = factory.CreateEntity(WallBlueprint);
                    if (wall != null) zone.AddEntity(wall, x, y);
                }
            }

            // 2. For each logical worldmap cell, replace the wall with a
            //    terrain entity carrying the biome glyph + WorldMapCellPart.
            for (int wy = 0; wy < WorldMap.Height; wy++)
            {
                for (int wx = 0; wx < WorldMap.Width; wx++)
                {
                    var (zoneX, zoneY) = WorldMap.WorldCellToZoneCell(wx, wy);
                    // Remove the wall at the zone cell.
                    ClearCell(zone, zoneX, zoneY);
                    // Create the terrain entity.
                    var terrain = CreateWorldMapTerrain(wx, wy);
                    zone.AddEntity(terrain, zoneX, zoneY);
                }
            }
            return true;
        }

        private static void ClearCell(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
                zone.RemoveEntity(cell.Objects[i]);
        }

        private Entity CreateWorldMapTerrain(int worldX, int worldY)
        {
            var biome = _worldMap.GetBiome(worldX, worldY);
            var (glyph, color, displayName) = GetBiomeRender(biome);

            var e = new Entity { BlueprintName = "WorldMapCell" };
            e.Tags["WorldMapCell"] = "";
            e.AddPart(new RenderPart
            {
                DisplayName = displayName,
                RenderString = glyph,
                ColorString = color,
            });
            e.AddPart(new PhysicsPart
            {
                Solid = false,
                Takeable = false,
                Weight = 0,
            });
            e.AddPart(new WorldMapCellPart
            {
                WorldX = worldX,
                WorldY = worldY,
            });
            return e;
        }

        /// <summary>
        /// Biome → (glyph, color, displayName). Color codes follow
        /// the CGA palette convention used elsewhere in the project
        /// (&amp; prefix = foreground).
        /// </summary>
        public static (string glyph, string color, string displayName) GetBiomeRender(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Cave:   return ("#", "&w", "cave region");
                case BiomeType.Desert: return (".", "&Y", "desert region");
                case BiomeType.Jungle: return ("%", "&g", "jungle region");
                case BiomeType.Ruins:  return ("o", "&y", "ruins region");
                default:               return ("?", "&w", "unknown region");
            }
        }
    }
}
