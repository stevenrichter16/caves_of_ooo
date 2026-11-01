using System;

namespace CavesOfOoo.Core
{
    /// <summary>What a trader accepts in exchange: drams, goods (barter), or nothing at all.</summary>
    public enum TenderKind { Drams, Barter, None }

    /// <summary>
    /// EC6 (Docs/ECONOMY-SYSTEM.md §12.2): who takes what. Canon (Lore/History/08_MaterialCulture.md §II):
    /// drams are "the closest thing to universal; the Concord underwrites it", but catacomb folk are
    /// barter-first, Tent-Right is oath-first, the Choir has no economy, and "drams are dead weight in the
    /// deep wasteland (where salt and the oath matter)". In order:
    /// <list type="number">
    /// <item>the Choir: <see cref="TenderKind.None"/> (it gives; see <see cref="ChoirOffering"/>);</item>
    /// <item>the Concord: <see cref="TenderKind.Drams"/>, everywhere;</item>
    /// <item>Tent-Right and catacomb folk: <see cref="TenderKind.Barter"/>, everywhere;</item>
    /// <item>anyone standing in the deep wasteland: <see cref="TenderKind.Barter"/>;</item>
    /// <item>everyone else: <see cref="TenderKind.Drams"/>.</item>
    /// </list>
    /// The deep wasteland is the Beating's interior: surface cells at least <see cref="DeepWastelandDepth"/>
    /// world cells (Chebyshev) from any other biome. The map is a pure function of the authored world,
    /// computed once.
    /// </summary>
    public static class Tender
    {
        public const string ConcordFaction = "SaccharineConcord";
        public const int DeepWastelandDepth = 3;
        static readonly string[] BarterFirst = { "TentRight", "CatacombFolk" };
        static bool[,] deep;

        public static TenderKind For(Entity trader)
        {
            if (trader == null) return TenderKind.Drams;
            if (ChoirOffering.KeepsNoShop(trader)) return TenderKind.None;
            string faction = FactionManager.GetFaction(trader);
            if (faction == ConcordFaction) return TenderKind.Drams;
            if (Array.IndexOf(BarterFirst, faction) >= 0) return TenderKind.Barter;
            return IsDeepWasteland(trader.SpatialZone?.ZoneID) ? TenderKind.Barter : TenderKind.Drams;
        }

        /// <summary>True for a surface zone in the Beating's interior; false off the map, underground, or for
        /// any zone id that names no world cell.</summary>
        public static bool IsDeepWasteland(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || !zoneId.StartsWith("Overworld.", StringComparison.Ordinal)) return false;
            int x, y, z;
            try { (x, y, z) = WorldMap.FromZoneID(zoneId); } catch (Exception) { return false; }
            return z == 0 && WorldMapAuthoring.InBounds(x, y) && Map()[x, y];
        }

        static bool[,] Map()
        {
            if (deep != null) return deep;
            var map = new bool[WorldMap.Width, WorldMap.Height];
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Beating) continue;
                    bool interior = true;
                    for (int dx = -(DeepWastelandDepth - 1); interior && dx < DeepWastelandDepth; dx++)
                        for (int dy = -(DeepWastelandDepth - 1); interior && dy < DeepWastelandDepth; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (WorldMapAuthoring.InBounds(nx, ny) && WorldMapAuthoring.BiomeAt(nx, ny) != BiomeType.Beating) interior = false;
                        }
                    map[x, y] = interior;
                }
            return deep = map;
        }
    }
}
