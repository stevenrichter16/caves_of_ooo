using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// BIOME-OVERHAUL A2 — a small hand-authored structure footprint.
    /// <see cref="Rows"/> is the ASCII shape; <see cref="Legend"/> maps
    /// each char to a marker:
    ///   "BlueprintName"        → place that entity
    ///   "chest:TableName"      → place a stocked Chest (LootStocker)
    ///   "spawn:BlueprintName"  → place a creature
    ///   "" (empty)             → claimed-but-empty (door gaps, floors)
    /// '.' needs no legend entry — existing terrain stays, cell is
    /// still claimed as part of the footprint.
    /// </summary>
    public class StructureStamp
    {
        public string Name;
        public string[] Rows;
        public Dictionary<char, string> Legend = new Dictionary<char, string>();

        /// <summary>Percent chance to attempt this stamp per eligible zone.</summary>
        public int Chance = 100;

        /// <summary>Minimum zone tier (1-3) this stamp appears at.</summary>
        public int MinTier = 1;

        /// <summary>Widest row (rows may be ragged — see ProspectorsCache).</summary>
        public int Width
        {
            get
            {
                if (Rows == null) return 0;
                int w = 0;
                for (int i = 0; i < Rows.Length; i++)
                    if (Rows[i].Length > w) w = Rows[i].Length;
                return w;
            }
        }

        public int Height => Rows?.Length ?? 0;
    }

    /// <summary>
    /// The shipped per-biome stamp catalogs. Phase A2 seeds ONE proving
    /// stamp per biome; the C-F biome passes grow these lists. All
    /// content here is gated by BiomeLandmarkTests'
    /// ShippedCatalogs_ValidateAgainstShippedContent.
    /// </summary>
    public static class StampCatalog
    {
        public static IReadOnlyList<StructureStamp> For(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Cave: return Cave;
                case BiomeType.Desert: return Desert;
                case BiomeType.Jungle: return Jungle;
                case BiomeType.Ruins: return Ruins;
                default: return System.Array.Empty<StructureStamp>();
            }
        }

        private static readonly StructureStamp[] Cave =
        {
            // A dead prospector's camp: supply crate inside, the vein
            // they were working just outside the door.
            new StructureStamp
            {
                Name = "ProspectorsCache",
                Chance = 30,
                MinTier = 1,
                Rows = new[]
                {
                    "######",
                    "#c...#",
                    "#....+.v",
                    "######",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "Wall" },
                    { 'c', "chest:CaveSupplyT1" },
                    { '+', "" },
                    { 'v', "GlowQuartzVein" },
                },
            },
        };

        private static readonly StructureStamp[] Desert =
        {
            // A bandit dugout — ambushers sleeping on their haul.
            new StructureStamp
            {
                Name = "BanditDugout",
                Chance = 25,
                MinTier = 2,
                Rows = new[]
                {
                    "#######",
                    "#b...c#",
                    "#..b..+",
                    "#######",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "SandstoneWall" },
                    { 'c', "chest:BanditCacheT2" },
                    { 'b', "spawn:AmbushBandit" },
                    { '+', "" },
                },
            },
        };

        private static readonly StructureStamp[] Jungle =
        {
            // A hunter's blind, long abandoned — dried stores and a
            // venom-worked blade if you're lucky.
            new StructureStamp
            {
                Name = "HuntersBlind",
                Chance = 30,
                MinTier = 1,
                Rows = new[]
                {
                    "#####",
                    "#c..#",
                    "#...+",
                    "#####",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "VineWall" },
                    { 'c', "chest:HunterCacheT1" },
                    { '+', "" },
                },
            },
        };

        private static readonly StructureStamp[] Ruins =
        {
            // A collapsed library — the ONLY circulation source for the
            // six utility grimoires that shipped with no source at all.
            new StructureStamp
            {
                Name = "CollapsedLibrary",
                Chance = 35,
                MinTier = 1,
                Rows = new[]
                {
                    "#####",
                    "#c.p#",
                    "#...+",
                    "#####",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "StoneWall" },
                    { 'c', "chest:LibraryShelfT1" },
                    { 'p', "Pillar" },
                    { '+', "" },
                },
            },
        };
    }

    /// <summary>
    /// BIOME-OVERHAUL A2 — places 0-<see cref="MaxStructuresPerZone"/>
    /// structure stamps into a wilderness zone. Runs at priority 3800
    /// (after terrain/connectivity, before StartingNeighborhood 3900 and
    /// population 4000). Anchor search mirrors
    /// StartingNeighborhoodBuilder's proven pattern: random anchors,
    /// every footprint cell must be in-bounds (margin 2) and passable.
    /// The footprint is claimed in <see cref="Zone.GenReservedCells"/>.
    /// Stamps whose legend names a blueprint the factory doesn't know
    /// are skipped WHOLE (minimal test fixtures stay safe; the shipped
    /// catalog is gated by its own content-integrity test).
    /// </summary>
    public class LandmarkBuilder : IZoneBuilder
    {
        public string Name => "LandmarkBuilder";
        public int Priority => 3800;

        public const int MaxStructuresPerZone = 2;
        private const int AnchorAttempts = 80;
        private const int EdgeMargin = 2;

        private readonly BiomeType _biome;
        private readonly int _tier;
        private readonly IReadOnlyList<StructureStamp> _catalog;

        public LandmarkBuilder(BiomeType biome, int tier,
            IReadOnlyList<StructureStamp> catalogOverride = null)
        {
            _biome = biome;
            _tier = tier;
            _catalog = catalogOverride ?? StampCatalog.For(biome);
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            int placed = 0;
            foreach (var stamp in _catalog)
            {
                if (placed >= MaxStructuresPerZone) break;
                if (stamp == null || stamp.Height == 0) continue;
                if (_tier < stamp.MinTier) continue;
                if (stamp.Chance < 100 && rng.Next(100) >= stamp.Chance) continue;
                if (!AllBlueprintsKnown(stamp, factory)) continue;

                if (TryPlace(zone, factory, rng, stamp))
                    placed++;
            }
            return true;
        }

        private static bool AllBlueprintsKnown(StructureStamp stamp, EntityFactory factory)
        {
            foreach (var kvp in stamp.Legend)
            {
                string marker = kvp.Value;
                if (string.IsNullOrEmpty(marker)) continue;
                if (marker.StartsWith("chest:"))
                {
                    if (!factory.Blueprints.ContainsKey("Chest")) return false;
                    continue;
                }
                string bp = marker.StartsWith("spawn:") ? marker.Substring(6) : marker;
                if (!factory.Blueprints.ContainsKey(bp)) return false;
            }
            return true;
        }

        private bool TryPlace(Zone zone, EntityFactory factory, System.Random rng, StructureStamp stamp)
        {
            int w = stamp.Width, h = stamp.Height;
            int maxX = Zone.Width - w - EdgeMargin;
            int maxY = Zone.Height - h - EdgeMargin;
            if (maxX <= EdgeMargin || maxY <= EdgeMargin) return false;

            for (int attempt = 0; attempt < AnchorAttempts; attempt++)
            {
                int ax = rng.Next(EdgeMargin, maxX);
                int ay = rng.Next(EdgeMargin, maxY);
                if (!FootprintClear(zone, stamp, ax, ay)) continue;

                Apply(zone, factory, rng, stamp, ax, ay);
                if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("worldgen"))
                {
                    CavesOfOoo.Diagnostics.Diag.Record(
                        category: "worldgen", kind: "StructurePlaced",
                        payload: new { stamp = stamp.Name, zone = zone.ZoneID, ax, ay });
                }
                return true;
            }
            return false;
        }

        private static bool FootprintClear(Zone zone, StructureStamp stamp, int ax, int ay)
        {
            for (int y = 0; y < stamp.Height; y++)
            {
                string row = stamp.Rows[y];
                for (int x = 0; x < row.Length; x++)
                {
                    var cell = zone.GetCell(ax + x, ay + y);
                    if (cell == null || !cell.IsPassable()) return false;
                    if (zone.GenReservedCells.Contains((ax + x, ay + y))) return false;
                }
            }
            return true;
        }

        private static void Apply(Zone zone, EntityFactory factory, System.Random rng,
            StructureStamp stamp, int ax, int ay)
        {
            for (int y = 0; y < stamp.Height; y++)
            {
                string row = stamp.Rows[y];
                for (int x = 0; x < row.Length; x++)
                {
                    int wx = ax + x, wy = ay + y;
                    zone.GenReservedCells.Add((wx, wy));

                    char ch = row[x];
                    if (ch == '.') continue;
                    if (!stamp.Legend.TryGetValue(ch, out string marker)) continue;
                    if (string.IsNullOrEmpty(marker)) continue;

                    if (marker.StartsWith("chest:"))
                    {
                        var chest = factory.CreateEntity("Chest");
                        if (chest != null)
                        {
                            zone.AddEntity(chest, wx, wy);
                            LootStocker.StockContainer(chest, marker.Substring(6), factory, rng);
                        }
                        continue;
                    }

                    string bp = marker.StartsWith("spawn:") ? marker.Substring(6) : marker;
                    var entity = factory.CreateEntity(bp);
                    if (entity != null)
                        zone.AddEntity(entity, wx, wy);
                }
            }
        }
    }
}
