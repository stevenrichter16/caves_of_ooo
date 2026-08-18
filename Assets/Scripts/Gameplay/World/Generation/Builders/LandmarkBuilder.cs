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

        /// <summary>
        /// BIOME-OVERHAUL B1 fix — when true, the anchor search accepts
        /// cells blocked by NON-WALL solids (trees, rocks, stalagmites)
        /// and Apply() clears them from the footprint before building.
        /// GUARANTEED structures (the merchant camp) need this: a strict
        /// all-passable 5x6 rect is a coin-flip in tree-scattered jungle
        /// CA, and a POI camp that fails to place is a broken promise.
        /// Ambient stamps leave it false — they're optional flavor and
        /// shouldn't bulldoze the landscape.
        /// </summary>
        public bool ClearsVegetation = false;

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
                case BiomeType.Spread: return Spread;
                case BiomeType.Sodden: return For(BiomeType.Jungle);
                case BiomeType.Beating: return For(BiomeType.Desert);
                case BiomeType.Grovelands: return For(BiomeType.Jungle);
                case BiomeType.Overwrit: return For(BiomeType.Ruins);
                case BiomeType.Stump: return For(BiomeType.Cave);
                default: return System.Array.Empty<StructureStamp>();
            }
        }

        /// <summary>
        /// STARTING TOWN — the five guaranteed shop stamps for the
        /// starting village (Docs/STARTING-TOWN.md). Each is a real
        /// building with its keeper (shop: marker = spawn + themed
        /// stock + restock table memory), a MarketStall counter, and —
        /// where it fits the trade — the crafting station under the
        /// same roof: the smith owns the forge, the apothecary the
        /// still. Placed at priority 3860 (after the river) with
        /// maxStructures 5.
        /// </summary>
        public static IReadOnlyList<StructureStamp> Town()
        {
            return TownStamps;
        }

        private static StructureStamp ShopStamp(string name, string keeperMarker,
            string stationOrNull)
        {
            var legend = new Dictionary<char, string>
            {
                { '#', "StoneWall" },
                { 'K', keeperMarker },
                { 'm', "MarketStall" },
                { '+', "" },
            };
            string[] rows;
            if (stationOrNull != null)
            {
                legend['S'] = stationOrNull;
                rows = new[]
                {
                    "######",
                    "#K..S#",
                    "#m...+",
                    "######",
                };
            }
            else
            {
                rows = new[]
                {
                    "#####",
                    "#K.m#",
                    "#...+",
                    "#####",
                };
            }
            return new StructureStamp
            {
                Name = name,
                Chance = 100,
                MinTier = 1,
                Rows = rows,
                Legend = legend,
            };
        }

        private static readonly StructureStamp[] TownStamps =
        {
            ShopStamp("TheSmithy", "shop:Weaponsmith:WeaponsmithStock", "TinkersForge"),
            ShopStamp("TheBulwark", "shop:Armorer:ArmorerStock", null),
            ShopStamp("TheAlembic", "shop:Apothecary:ApothecaryStock", "AlchemyStill"),
            ShopStamp("TheInkwell", "shop:Arcanist:ArcanistStock", null),
            ShopStamp("TheLarder", "shop:Provisioner:ProvisionerStock", null),
        };

        /// <summary>
        /// BIOME-OVERHAUL G — the underground landmark catalog by depth
        /// band. MinTier here maps to the underground zone tier
        /// (depth/3 + 1, capped 8): mine galleries from the first
        /// shafts, the Pale Curation's lit gallery in the shale band
        /// (the deep's only rest stop — PaleCurator's 22-node tree and
        /// mineral-buying wallet finally placed), sentinel reliquaries
        /// in the quartzite sharing the surface vaults' treasure table.
        /// </summary>
        public static IReadOnlyList<StructureStamp> Underground(int depth)
        {
            return UndergroundStamps;
        }

        private static readonly StructureStamp[] UndergroundStamps =
        {
            new StructureStamp
            {
                Name = "MineGallery",
                Chance = 20,
                MinTier = 1,
                Rows = new[]
                {
                    "#####",
                    "#c.v#",
                    "#.v.+",
                    "#####",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "Wall" },
                    { 'c', "chest:DeepSupplyT2" },
                    { 'v', "GlowQuartzVein" },
                    { '+', "" },
                },
            },
            new StructureStamp
            {
                Name = "CurationGallery",
                Chance = 25,
                MinTier = 3,
                Rows = new[]
                {
                    "######",
                    "#u..c#",
                    "#.f..+",
                    "######",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "Wall" },
                    { 'u', "spawn:PaleCurator" },
                    { 'c', "chest:DeepSupplyT2" },
                    { 'f', "Campfire" },
                    { '+', "" },
                },
            },
            new StructureStamp
            {
                Name = "Reliquary",
                Chance = 20,
                MinTier = 4,
                Rows = new[]
                {
                    "######",
                    "#L..V#",
                    "#..k.+",
                    "######",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "Wall" },
                    { 'V', "spawn:VaultSentinel" },
                    { 'L', "lockedchest:SealedVaultT3" },
                    { 'k', "IronKey" },
                    { '+', "" },
                },
            },
        };

        /// <summary>
        /// BIOME-OVERHAUL B1 — the guaranteed camp stamp for a
        /// MerchantCamp POI zone (NOT part of the ambient wilderness
        /// catalogs above — POI routing places it via a catalogOverride).
        /// Shared layout, per-biome walls and name: tent, campfire
        /// (restable — the wilderness reprieve node), a Merchant and a
        /// Warden guard (Villagers faction → auto-stocked by
        /// TradeStockBuilder, wallets from A6), and a supply chest.
        /// </summary>
        public static StructureStamp MerchantCamp(BiomeType biome)
        {
            string wall, name;
            switch (biome)
            {
                case BiomeType.Desert: wall = "SandstoneWall"; name = "Caravanserai"; break;
                case BiomeType.Jungle: wall = "VineWall"; name = "TrapperCamp"; break;
                case BiomeType.Ruins: wall = "StoneWall"; name = "SalvageCamp"; break;
                case BiomeType.Spread: wall = "VineWall"; name = "TrapperCamp"; break;
                case BiomeType.Sodden: wall = "VineWall"; name = "TrapperCamp"; break;
                case BiomeType.Beating: wall = "SandstoneWall"; name = "Caravanserai"; break;
                case BiomeType.Grovelands: wall = "VineWall"; name = "TrapperCamp"; break;
                case BiomeType.Overwrit: wall = "StoneWall"; name = "SalvageCamp"; break;
                case BiomeType.Stump: wall = "Wall"; name = "ProspectorCamp"; break;
                case BiomeType.Cave:
                default: wall = "Wall"; name = "ProspectorCamp"; break;
            }
            return new StructureStamp
            {
                Name = name,
                Chance = 100,
                MinTier = 1,
                ClearsVegetation = true,
                Rows = new[]
                {
                    "##.##",
                    "#M.c#",
                    "#...#",
                    "##.##",
                    ".W...",
                    "..f..",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', wall },
                    { 'M', "spawn:Merchant" },
                    { 'W', "spawn:Warden" },
                    { 'c', "chest:CampGoodsT1" },
                    { 'f', "Campfire" },
                },
            };
        }

        /// <summary>
        /// BIOME-OVERHAUL B2 — the hermit hut shared by all four biome
        /// catalogs (per-biome wall + resident). The hermit offers paid
        /// rest, cures (Herbalist), rumors, and trade; the campfire by
        /// the door is the free fallback.
        /// </summary>
        private static StructureStamp HermitHut(string wall, string hermit)
        {
            return new StructureStamp
            {
                Name = hermit + "Hut",
                Chance = 20,
                MinTier = 1,
                Rows = new[]
                {
                    "#####",
                    "#h..#",
                    "#...+.f",
                    "#####",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', wall },
                    { 'h', "spawn:" + hermit },
                    { '+', "" },
                    { 'f', "Campfire" },
                },
            };
        }

        private static readonly StructureStamp[] Cave =
        {
            HermitHut("Wall", "CaveHermit"),
            // Phase C: a warband camp — the game's first world-placed
            // LockedChest. The key lies somewhere in camp (always
            // obtainable); the warlord and his raiders are the real lock.
            new StructureStamp
            {
                Name = "WarbandCamp",
                Chance = 25,
                MinTier = 2,
                Rows = new[]
                {
                    "#######",
                    "#s...L#",
                    "#..X..#",
                    "#s..k.+",
                    "#######",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "Wall" },
                    { 'X', "spawn:SnapjawWarlord" },
                    { 's', "spawn:Snapjaw" },
                    { 'L', "lockedchest:WarbandLootT2" },
                    { 'k', "IronKey" },
                    { '+', "" },
                },
            },
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
            HermitHut("SandstoneWall", "DesertHermit"),
            // Phase D: the tomb — PlateArmor's first source, behind the
            // dead and a lock.
            new StructureStamp
            {
                Name = "SandstoneTomb",
                Chance = 25,
                MinTier = 2,
                Rows = new[]
                {
                    "######",
                    "#e..L#",
                    "#.k..#",
                    "#e...+",
                    "######",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "SandstoneWall" },
                    { 'e', "spawn:SkeletalSentry" },
                    { 'L', "lockedchest:TombVaultT2" },
                    { 'k', "IronKey" },
                    { '+', "" },
                },
            },
            // Phase D: the Glassblown Remnant's obelisk — the Drifter's
            // 25-node conversation tree goes live here.
            new StructureStamp
            {
                Name = "GlassblownObelisk",
                Chance = 20,
                MinTier = 1,
                Rows = new[]
                {
                    ".I.",
                    ".g.",
                    "...",
                },
                Legend = new Dictionary<char, string>
                {
                    { 'I', "Pillar" },
                    { 'g', "spawn:GlassblownDrifter" },
                },
            },
            // Phase D: a Saccharine Concord waystation — envoy, fire,
            // and a place to breathe between dunes.
            new StructureStamp
            {
                Name = "ConcordWaystation",
                Chance = 20,
                MinTier = 1,
                Rows = new[]
                {
                    "##.##",
                    "#v..#",
                    "#..f+",
                    "#####",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "SandstoneWall" },
                    { 'v', "spawn:SaccharineEnvoy" },
                    { 'f', "Campfire" },
                    { '+', "" },
                },
            },
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
            HermitHut("VineWall", "JungleHermit"),
            // Phase E: the vine-choked ziggurat — jungle uniques behind
            // an 80-HP tendril and a lock.
            new StructureStamp
            {
                Name = "Ziggurat",
                Chance = 25,
                MinTier = 2,
                Rows = new[]
                {
                    "######",
                    "#T..L#",
                    "#..k.#",
                    "###+##",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "VineWall" },
                    { 'T', "spawn:ChoirTendril" },
                    { 'L', "lockedchest:ZigguratVaultT2" },
                    { 'k', "IronKey" },
                    { '+', "" },
                },
            },
            // Phase E: the Rot Choir's grove — the five NAMED choir
            // NPCs (authored dialogue, never placed until now) gathered
            // around their congregation fire.
            new StructureStamp
            {
                Name = "GroveShrine",
                Chance = 20,
                MinTier = 1,
                Rows = new[]
                {
                    ".m.g.",
                    ".nfo.",
                    "..i..",
                },
                Legend = new Dictionary<char, string>
                {
                    { 'm', "spawn:Mogu" },
                    { 'g', "spawn:Grib" },
                    { 'n', "spawn:Nam" },
                    { 'i', "spawn:Sien" },
                    { 'o', "spawn:Sopp" },
                    { 'f', "Campfire" },
                },
            },
            // Phase E: a wild mendleaf patch — the healing herb gets a
            // harvest source beyond trade.
            new StructureStamp
            {
                Name = "MendleafGarden",
                Chance = 15,
                MinTier = 1,
                Rows = new[]
                {
                    "pp.p",
                    ".pp.",
                },
                Legend = new Dictionary<char, string>
                {
                    { 'p', "MendleafPlant" },
                },
            },
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

        /// <summary>
        /// The Spread's own catalog (Docs/FELLING-W1-W2-PLAN.md SM4) —
        /// before this, <c>For(BiomeType.Spread)</c> delegated to
        /// <see cref="Jungle"/> wholesale, so a Ziggurat or a Rot-Choir
        /// GroveShrine could turn up in a hedgerow. Three stamps, each
        /// "a lived-in place" rather than a threat
        /// (Docs/FELLING-WORLD-DESIGN.md §3.1): a folk river-shrine, a
        /// farmstead, and the conjured festival meadow.
        /// </summary>
        private static readonly StructureStamp[] Spread =
        {
            // The material frame of disbelief in miniature: a worn stone
            // by the water, carved tokens, no god's name spoken.
            new StructureStamp
            {
                Name = "RiverShrine",
                Chance = 20,
                MinTier = 1,
                Rows = new[]
                {
                    ".r.",
                    "rSr",
                    ".r.",
                },
                Legend = new Dictionary<char, string>
                {
                    { 'r', "Reeds" },
                    { 'S', "RiverShrine" },
                },
            },
            // "One big stamp... a lived-in place; owners; doors."
            // Reuses the same walled-footprint idiom every other camp
            // stamp already uses (door gap in the top row).
            new StructureStamp
            {
                Name = "MillStead",
                Chance = 20,
                MinTier = 1,
                Rows = new[]
                {
                    "##.##",
                    "#...#",
                    "#.F.#",
                    "#..c#",
                    "#####",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "Wall" },
                    { 'F', "spawn:Farmer" },
                    { 'c', "chest:CampGoodsT1" },
                },
            },
            // A denser conjured bloom than the wilderness FlowerMeadow
            // formation rolls, with a signpost — the festival gathering
            // spot. Deliberately no contract/quest content (the design
            // doc's "festival contracts" has no canon source,
            // Docs/FELLING-W1-W2-PLAN.md §1.2); purely atmospheric,
            // "gone by morning" (Lore/History/09_Magic.md:42).
            new StructureStamp
            {
                Name = "FestivalField",
                Chance = 15,
                MinTier = 1,
                Rows = new[]
                {
                    ".f.f.",
                    "f.p.f",
                    ".f.f.",
                },
                Legend = new Dictionary<char, string>
                {
                    { 'f', "FlowerField" },
                    { 'p', "Signpost" },
                },
            },
        };

        private static readonly StructureStamp[] Ruins =
        {
            HermitHut("StoneWall", "RuinsHermit"),
            // Phase F: the sealed vault — the game's treasure room.
            // First placed LockedDoor; the sentinel carries no key
            // because the key lies in the antechamber — the FIGHT is
            // the lock.
            new StructureStamp
            {
                Name = "SealedVault",
                Chance = 25,
                MinTier = 2,
                Rows = new[]
                {
                    "#######",
                    "#L...V#",
                    "###D###",
                    "#.k...+",
                    "#######",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "StoneWall" },
                    { 'V', "spawn:VaultSentinel" },
                    { 'L', "lockedchest:SealedVaultT3" },
                    { 'D', "LockedDoor" },
                    { 'k', "IronKey" },
                    { '+', "" },
                },
            },
            // Phase F: a clockwork workshop — the three dead schematics
            // (and with them four unlearnable tinker mods) circulate.
            new StructureStamp
            {
                Name = "ClockworkWorkshop",
                Chance = 25,
                MinTier = 2,
                Rows = new[]
                {
                    "######",
                    "#B..c#",
                    "#..B.+",
                    "######",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "StoneWall" },
                    { 'B', "spawn:BrassHusk" },
                    { 'c', "chest:WorkshopCacheT2" },
                    { '+', "" },
                },
            },
            // Phase F: a rune-cult dig site — RuneCultists arrive with
            // their complete AILayRune AI, so the world's first live
            // trap-laying happens here.
            new StructureStamp
            {
                Name = "RuneCultSite",
                Chance = 20,
                MinTier = 2,
                Rows = new[]
                {
                    ".u.u.",
                    "u.c.u",
                    ".u.u.",
                },
                Legend = new Dictionary<char, string>
                {
                    { 'u', "spawn:RuneCultist" },
                    { 'c', "chest:CultCacheT2" },
                },
            },
            // Phase F: the Palimpsest Archive — the Recension's Echo
            // (the game's largest authored conversation, 42 nodes)
            // finally gets a place to stand, with a scholar's fire.
            new StructureStamp
            {
                Name = "PalimpsestArchive",
                Chance = 20,
                MinTier = 1,
                Rows = new[]
                {
                    "######",
                    "#p.IE#",
                    "#..f.+",
                    "######",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "StoneWall" },
                    { 'E', "spawn:PalimpsestEcho" },
                    { 'p', "Pillar" },
                    { 'I', "Pillar" },
                    { 'f', "Campfire" },
                    { '+', "" },
                },
            },
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
        public int Priority => _priority;

        public const int MaxStructuresPerZone = 2;
        private const int AnchorAttempts = 80;
        private const int EdgeMargin = 2;

        private readonly BiomeType _biome;
        private readonly int _tier;
        private readonly IReadOnlyList<StructureStamp> _catalog;
        private readonly int _priority;

        /// <param name="priority">
        /// Defaults to 3800 (ambient wilderness stamps). GUARANTEED
        /// placements (the B1 merchant camp) pass a LOWER value so they
        /// claim open space BEFORE the optional ambient stamps — in
        /// cramped biomes (ruins rooms, jungle pockets) an ambient hut
        /// grabbing the one large clearing first can leave the
        /// must-place structure with no valid anchor (caught by the B2
        /// regression on the B1 end-to-end test).
        /// </param>
        private readonly int _maxStructures;

        public LandmarkBuilder(BiomeType biome, int tier,
            IReadOnlyList<StructureStamp> catalogOverride = null, int priority = 3800,
            int maxStructures = MaxStructuresPerZone)
        {
            _biome = biome;
            _tier = tier;
            _catalog = catalogOverride ?? StampCatalog.For(biome);
            _priority = priority;
            _maxStructures = maxStructures;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            int placed = 0;
            foreach (var stamp in _catalog)
            {
                if (placed >= _maxStructures) break;
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
                if (marker.StartsWith("lockedchest:"))
                {
                    if (!factory.Blueprints.ContainsKey("LockedChest")) return false;
                    continue;
                }
                if (marker.StartsWith("shop:"))
                {
                    var parts = marker.Split(':');
                    if (parts.Length < 3 || !factory.Blueprints.ContainsKey(parts[1])) return false;
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
                    if (cell == null) return false;
                    if (zone.GenReservedCells.Contains((ax + x, ay + y))) return false;
                    if (HasStairs(cell)) return false; // never bury a stairway
                    if (HasLiquid(cell)) return false; // never build in the river
                    if (cell.IsPassable()) continue;
                    // Blocked cell: acceptable only for vegetation-
                    // clearing stamps, and only when nothing wall-like
                    // stands there.
                    if (!stamp.ClearsVegetation || cell.IsWall()) return false;
                }
            }
            return true;
        }

        private static bool HasStairs(Cell cell)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (cell.Objects[i].GetPart<StairsDownPart>() != null
                    || cell.Objects[i].GetPart<StairsUpPart>() != null)
                    return true;
            }
            return false;
        }

        // STARTING TOWN fix (pre-existing exposure): a stamp footprint
        // could straddle a river — water cells are passable, so nothing
        // vetoed them. Liquid now rejects the anchor for ALL stamps.
        private static bool HasLiquid(Cell cell)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                if (cell.Objects[i].GetPart<LiquidPoolPart>() != null)
                    return true;
            }
            return false;
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

                    if (stamp.ClearsVegetation)
                        ClearSolidNonWalls(zone, wx, wy);

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
                    if (marker.StartsWith("lockedchest:"))
                    {
                        var locked = factory.CreateEntity("LockedChest");
                        if (locked != null)
                        {
                            zone.AddEntity(locked, wx, wy);
                            LootStocker.StockContainer(locked, marker.Substring(12), factory, rng);
                        }
                        continue;
                    }
                    // STARTING TOWN: shop:Blueprint:Table — spawn the
                    // keeper, roll their themed stock into inventory,
                    // and remember the table so TraderRestockSystem can
                    // refill a shelf that runs low.
                    if (marker.StartsWith("shop:"))
                    {
                        var parts = marker.Split(':');
                        if (parts.Length >= 3)
                        {
                            var shopkeeper = factory.CreateEntity(parts[1]);
                            if (shopkeeper != null)
                            {
                                zone.AddEntity(shopkeeper, wx, wy);
                                shopkeeper.Properties["ShopStockTable"] = parts[2];
                                var inv = shopkeeper.GetPart<InventoryPart>();
                                // Only stock a BARE shelf. CreateEntity
                                // above fires ObjectCreated, so a keeper
                                // carrying a TraderPart has already
                                // rolled — and the markers name the very
                                // same table (shop:Weaponsmith:Weapon-
                                // smithStock), so re-rolling here would
                                // silently double every town shop's
                                // opening stock. Keepers without a
                                // TraderPart still get filled here.
                                if (inv != null && inv.Objects.Count == 0)
                                {
                                    foreach (var bpName in LootTableRegistry.Roll(parts[2], rng))
                                    {
                                        if (!factory.Blueprints.ContainsKey(bpName)) continue;
                                        var item = factory.CreateEntity(bpName);
                                        if (item != null) inv.AddObject(item);
                                    }
                                }
                            }
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

        /// <summary>Fell the trees, roll away the rocks — clear every
        /// solid non-wall entity from a footprint cell (see
        /// <see cref="StructureStamp.ClearsVegetation"/>).</summary>
        private static void ClearSolidNonWalls(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.IsWall()) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                var e = cell.Objects[i];
                bool solid = e.HasTag("Solid") || (e.GetPart<PhysicsPart>()?.Solid ?? false);
                if (solid)
                    zone.RemoveEntity(e);
            }
        }
    }
}
