using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Scatters the status-holding terrain — brine pools, tar seeps,
    /// copper pipe runs, hot ash, frost vents — so the world can set up a
    /// combination the player did not create.
    ///
    /// <para><b>Clusters, not confetti.</b> A single puddle in an empty
    /// room is scenery. A puddle with a pipe run leaving it is a
    /// decision: shock the water and the charge goes somewhere. So this
    /// places small connected patches and, where the biome allows, pairs
    /// a conductive pool with a conductor that leads away from it. That
    /// pairing is the greenhouse vignette from
    /// Docs/STATUS-SYSTEM-MODULAR-LADDER.md §5, generated rather than
    /// hand-authored.</para>
    ///
    /// <para><b>Priority 3900</b> — after LandmarkBuilder (3790-3860) so
    /// stamps' reserved cells are already claimed, and before
    /// PopulationBuilder (4000) and ContainerBuilder (4100), so creatures
    /// and chests place around the hazards rather than inside them.</para>
    ///
    /// <para>Deliberately sparse. These tiles change how a fight
    /// resolves, and a zone carpeted in them would make every fight the
    /// same fight. Two or three patches per zone is enough for the world
    /// to occasionally hand the player an opening.</para>
    /// </summary>
    public class HazardTerrainBuilder : IZoneBuilder
    {
        public string Name => "HazardTerrainBuilder";
        public int Priority => 3900;

        private readonly BiomeType _biome;
        private readonly bool _underground;

        /// <summary>Patches attempted per zone, before rolls.</summary>
        private const int MinPatches = 1;
        private const int MaxPatches = 3;

        /// <summary>Cells per patch. Small: these are puddles and
        /// seeps, not lakes.</summary>
        private const int MinPatchCells = 2;
        private const int MaxPatchCells = 5;

        /// <summary>Chance a conductive patch gets a conductor run
        /// leading out of it — the "and the arc goes somewhere" half.</summary>
        private const int ConductorRunPercent = 55;
        private const int MinRunLength = 3;
        private const int MaxRunLength = 6;

        public HazardTerrainBuilder(BiomeType biome, bool underground = false)
        {
            _biome = biome;
            _underground = underground;
        }

        /// <summary>
        /// What each biome can grow, and how likely each is. Weights are
        /// relative within the biome.
        /// </summary>
        private struct Entry
        {
            public string Blueprint;
            public int Weight;
            /// <summary>True when a conductor run leading away actually
            /// means something — i.e. the patch itself conducts.</summary>
            public bool Conductive;

            public Entry(string bp, int w, bool conductive)
            {
                Blueprint = bp; Weight = w; Conductive = conductive;
            }
        }

        private static readonly Entry[] CaveEntries =
        {
            new Entry("BrinePool", 30, true),
            new Entry("TarSeep", 25, false),
            new Entry("AshBed", 20, false),
            new Entry("SteamVent", 15, true),
            new Entry("PeatBog", 10, true),
        };

        private static readonly Entry[] DesertEntries =
        {
            new Entry("DryBrush", 45, false),
            new Entry("AshBed", 30, false),
            new Entry("TarSeep", 15, false),
            new Entry("BrinePool", 10, true),
        };

        private static readonly Entry[] JungleEntries =
        {
            new Entry("PeatBog", 40, true),
            new Entry("BrinePool", 30, true),
            new Entry("SteamVent", 20, true),
            new Entry("DryBrush", 10, false),
        };

        private static readonly Entry[] RuinsEntries =
        {
            new Entry("BrinePool", 30, true),   // rain gets in
            new Entry("RustedRailing", 25, true),
            new Entry("TarSeep", 20, false),
            new Entry("SteamVent", 15, true),
            new Entry("AshBed", 10, false),
        };

        private static readonly Entry[] UndergroundEntries =
        {
            new Entry("BrinePool", 25, true),
            new Entry("IceSheet", 20, true),
            new Entry("FrostVent", 20, false),
            new Entry("TarSeep", 20, false),
            new Entry("SteamVent", 15, true),
        };

        /// <summary>The conductor laid in a run out of a conductive
        /// patch. Ruins get pipe; everywhere else gets ice, which is the
        /// natural conductor.</summary>
        private string ConductorFor(BiomeType biome)
            => biome == BiomeType.Ruins ? "CopperPipe"
             : _underground ? "IceSheet"
             : "CopperPipe";

        private Entry[] TableFor(BiomeType biome)
        {
            if (_underground) return UndergroundEntries;
            switch (biome)
            {
                case BiomeType.Desert: return DesertEntries;
                case BiomeType.Jungle: return JungleEntries;
                case BiomeType.Ruins: return RuinsEntries;
                default: return CaveEntries;
            }
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (zone == null || factory == null || rng == null) return false;

            Entry[] table = TableFor(_biome);
            if (table.Length == 0) return true;

            List<Cell> candidates = CollectOpenFloor(zone);
            if (candidates.Count < MinPatchCells) return true;

            int patches = MinPatches + rng.Next(MaxPatches - MinPatches + 1);
            var used = new HashSet<(int, int)>();

            for (int p = 0; p < patches; p++)
            {
                Entry entry = Pick(table, rng);
                Cell origin = candidates[rng.Next(candidates.Count)];
                if (used.Contains((origin.X, origin.Y))) continue;

                int size = MinPatchCells + rng.Next(MaxPatchCells - MinPatchCells + 1);
                GrowPatch(zone, factory, rng, origin, entry.Blueprint, size, used);

                // A conductive pool with somewhere for the charge to go.
                if (entry.Conductive && rng.Next(100) < ConductorRunPercent)
                    LayConductorRun(zone, factory, rng, origin, used);
            }

            return true;
        }

        /// <summary>
        /// Flood a small blob outward from <paramref name="origin"/>,
        /// which reads as a puddle rather than a scatter of unrelated
        /// tiles.
        /// </summary>
        private void GrowPatch(Zone zone, EntityFactory factory, System.Random rng,
            Cell origin, string blueprint, int size, HashSet<(int, int)> used)
        {
            int x = origin.X, y = origin.Y;

            for (int i = 0; i < size; i++)
            {
                if (!TryPlace(zone, factory, x, y, blueprint, used)) break;

                // Random walk, orthogonal — a blob, not a diagonal line.
                switch (rng.Next(4))
                {
                    case 0: x++; break;
                    case 1: x--; break;
                    case 2: y++; break;
                    default: y--; break;
                }
            }
        }

        /// <summary>
        /// Run a conductor in one direction out of the patch, so charge
        /// dropped in the pool travels somewhere the player can see.
        /// </summary>
        private void LayConductorRun(Zone zone, EntityFactory factory, System.Random rng,
            Cell origin, HashSet<(int, int)> used)
        {
            string conductor = ConductorFor(_biome);
            int dx = 0, dy = 0;
            switch (rng.Next(4))
            {
                case 0: dx = 1; break;
                case 1: dx = -1; break;
                case 2: dy = 1; break;
                default: dy = -1; break;
            }

            int length = MinRunLength + rng.Next(MaxRunLength - MinRunLength + 1);
            int x = origin.X, y = origin.Y;

            for (int i = 0; i < length; i++)
            {
                x += dx; y += dy;
                if (!TryPlace(zone, factory, x, y, conductor, used)) break;
            }
        }

        private bool TryPlace(Zone zone, EntityFactory factory, int x, int y,
            string blueprint, HashSet<(int, int)> used)
        {
            if (x < 1 || y < 1 || x >= Zone.Width - 1 || y >= Zone.Height - 1) return false;
            if (used.Contains((x, y))) return false;

            Cell cell = zone.GetCell(x, y);
            if (cell == null || !IsOpenFloor(cell)) return false;
            if (zone.GenReservedCells != null
                && zone.GenReservedCells.Contains((x, y))) return false;

            // Ask only for content that exists. EntityFactory LOGS AN
            // ERROR on an unknown blueprint, and Unity's test framework
            // fails any test that emits one — so a builder that requests
            // optional decor blindly breaks every zone-generation test
            // running against a minimal blueprint fixture. Decor is
            // optional by nature; a content pack without these should
            // generate a plain zone, not a wall of errors.
            if (factory.Blueprints == null
                || !factory.Blueprints.ContainsKey(blueprint)) return false;

            Entity e = factory.CreateEntity(blueprint);
            if (e == null) return false;

            zone.AddEntity(e, x, y);
            used.Add((x, y));
            return true;
        }

        private static List<Cell> CollectOpenFloor(Zone zone)
        {
            var list = new List<Cell>(256);
            for (int x = 2; x < Zone.Width - 2; x++)
            {
                for (int y = 2; y < Zone.Height - 2; y++)
                {
                    Cell cell = zone.GetCell(x, y);
                    if (cell == null || !IsOpenFloor(cell)) continue;
                    if (zone.GenReservedCells != null
                        && zone.GenReservedCells.Contains((x, y))) continue;
                    list.Add(cell);
                }
            }
            return list;
        }

        /// <summary>
        /// Empty walkable floor. Refuses to stack on anything solid, on
        /// stairs, or on an existing pool — two liquid sources on one
        /// cell would both assert their coating every turn and fight.
        /// </summary>
        private static bool IsOpenFloor(Cell cell)
        {
            if (cell.IsSolid()) return false;

            for (int i = 0; i < cell.Objects.Count; i++)
            {
                Entity o = cell.Objects[i];
                if (o == null) continue;
                if (o.HasTag("Solid")) return false;
                if (o.HasPart<LiquidPoolPart>()) return false;
                if (o.HasPart<TileStateSourcePart>()) return false;
                if (o.HasPart<ContainerPart>()) return false;
                if (o.HasTag("Creature")) return false;

                string bp = o.BlueprintName;
                if (bp == "StairsDown" || bp == "StairsUp") return false;

                var phys = o.GetPart<PhysicsPart>();
                if (phys != null && phys.Solid) return false;
            }

            return true;
        }

        private static Entry Pick(Entry[] table, System.Random rng)
        {
            int total = 0;
            for (int i = 0; i < table.Length; i++) total += table[i].Weight;
            if (total <= 0) return table[0];

            int roll = rng.Next(total);
            for (int i = 0; i < table.Length; i++)
            {
                roll -= table[i].Weight;
                if (roll < 0) return table[i];
            }
            return table[table.Length - 1];
        }
    }
}
