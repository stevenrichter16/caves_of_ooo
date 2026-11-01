using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Which wild chunks gain density structure (Docs/CHUNK-ACTIVITY.md §9). Pure functions of the
    /// world and the zone ID: the formation comes from <see cref="FormationSelector"/>, never from a
    /// generated neighbour or the pipeline's RNG.
    /// </summary>
    public static class ChunkDensityPlan
    {
        /// <summary>The formations whose renders read as wide bare ground. Hedgerow and Fallow are already
        /// structured; the SaltPan is a zero-cover duel floor by design; DuneBelt's crests are its structure
        /// (solid terrain, so every sector is already dressed); WindBarrens has its own outcrops;
        /// BrineLens is water; the Sodden and the Stump read as their formations intend; the Overwrit's
        /// emptiness is canon.</summary>
        public static bool IsSparse(BiomeType biome, Formation formation)
        {
            switch (biome)
            {
                case BiomeType.Spread:
                    return formation == Formation.FieldStrips || formation == Formation.FlowerMeadow
                        || formation == Formation.OldRoad || formation == Formation.RiverMeadow;
                case BiomeType.Beating:
                    return formation == Formation.CaravanRoad || formation == Formation.RuinField;
                default: return false;
            }
        }

        /// <summary>The formation a wild zone is composed with.</summary>
        public static Formation FormationOf(BiomeType biome, string zoneId) => FormationSelector.For(biome, zoneId);

        /// <summary>A waysite-eligible wild zone (curated, opening, situation, POI and exploration-family
        /// zones excluded) in a sparse formation.</summary>
        public static bool IsEligible(OverworldZoneManager manager, string zoneId)
        {
            if (!WaysitePlan.IsEligible(manager, zoneId)) return false;
            var (x, y, _) = WorldMap.FromZoneID(zoneId);
            var biome = WorldMapAuthoring.BiomeAt(x, y);
            return IsSparse(biome, FormationOf(biome, zoneId));
        }
    }

    /// <summary>One density feature: a small coherent shape of approved-art blueprints. No flower patch: the
    /// meadow already has its flowers (the composition's CharmFlowers drifts and the formation's FlowerField,
    /// Urqu's bleed instrument), and another patch would blur which flowers are the instrument.</summary>
    public enum DensityFeature { CopseKnot, FieldTree, HedgeRun, TuftDrift, Outcrop, BrushClump }

    /// <summary>The features each sparse formation favours, and the shapes they make.</summary>
    public static class ChunkDensityCatalog
    {
        /// <summary>Weighted features for the formation; empty when it is not sparse.</summary>
        public static IReadOnlyList<(DensityFeature feature, int weight)> For(BiomeType biome, Formation formation)
        {
            if (!ChunkDensityPlan.IsSparse(biome, formation)) return Array.Empty<(DensityFeature, int)>();
            switch (formation)
            {
                case Formation.FieldStrips: return new[] { (DensityFeature.HedgeRun, 4), (DensityFeature.FieldTree, 4), (DensityFeature.TuftDrift, 2) };
                case Formation.FlowerMeadow: return new[] { (DensityFeature.TuftDrift, 3), (DensityFeature.FieldTree, 3), (DensityFeature.CopseKnot, 1) };
                case Formation.OldRoad: return new[] { (DensityFeature.CopseKnot, 3), (DensityFeature.TuftDrift, 3), (DensityFeature.HedgeRun, 2), (DensityFeature.FieldTree, 2) };
                case Formation.RiverMeadow: return new[] { (DensityFeature.TuftDrift, 4), (DensityFeature.FieldTree, 3), (DensityFeature.CopseKnot, 2) };
                case Formation.CaravanRoad: return new[] { (DensityFeature.Outcrop, 3), (DensityFeature.BrushClump, 2) };
                case Formation.RuinField: return new[] { (DensityFeature.Outcrop, 2), (DensityFeature.BrushClump, 2) };
                default: return Array.Empty<(DensityFeature, int)>();
            }
        }

        /// <summary>The feature's cells as (dx, dy, blueprint) around its anchor. <paramref name="lee"/> is
        /// the zone's downwind side (-1 or 1), where brush gathers behind an outcrop.</summary>
        public static List<(int dx, int dy, string blueprint)> Shape(DensityFeature feature, Random rng, int lee)
        {
            var cells = new List<(int dx, int dy, string blueprint)>();
            var used = new HashSet<(int, int)>();
            void Add(int dx, int dy, string bp) { if (used.Add((dx, dy))) cells.Add((dx, dy, bp)); }
            void Blob(string bp, int count, bool alongX)
            {
                int x = 0, y = 0; Add(0, 0, bp);
                for (int guard = 0; cells.Count < count && guard < 40; guard++)
                {
                    if (alongX) { x += rng.Next(3) - 1; if (rng.Next(3) == 0) y += rng.Next(3) - 1; }
                    else { x += rng.Next(3) - 1; y += rng.Next(3) - 1; }
                    Add(x, y, bp);
                }
            }
            switch (feature)
            {
                case DensityFeature.CopseKnot:
                {
                    Add(0, 0, "Tree");
                    var sides = new List<(int, int)> { (1, 0), (-1, 0), (0, 1), (0, -1) };
                    for (int i = 0, n = 1 + rng.Next(2); i < n; i++) { int k = rng.Next(sides.Count); Add(sides[k].Item1, sides[k].Item2, "Tree"); sides.RemoveAt(k); }
                    var ring = new List<(int, int)>();
                    for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++)
                        if ((Math.Abs(dx) == 2 || Math.Abs(dy) == 2) && !used.Contains((dx, dy))) ring.Add((dx, dy));
                    for (int i = 0, n = 3 + rng.Next(3); i < n && ring.Count > 0; i++) { int k = rng.Next(ring.Count); Add(ring[k].Item1, ring[k].Item2, "Bush"); ring.RemoveAt(k); }
                    break;
                }
                case DensityFeature.FieldTree:
                {
                    Add(0, 0, "Tree");
                    var foot = new List<(int, int)> { (-1, 1), (0, 1), (1, 1) };
                    for (int i = 0, n = 1 + rng.Next(2); i < n; i++) { int k = rng.Next(foot.Count); Add(foot[k].Item1, foot[k].Item2, "Bush"); foot.RemoveAt(k); }
                    break;
                }
                case DensityFeature.HedgeRun:
                {
                    int length = 5 + rng.Next(5); bool vertical = rng.Next(10) < 3;
                    for (int i = 0; i < length; i++)
                    {
                        if (length >= 7 && i == length / 2) continue;   // a gate gap in a long run
                        if (vertical) Add(0, i, "Hedge"); else Add(i, 0, "Hedge");
                    }
                    break;
                }
                case DensityFeature.TuftDrift: Blob("Bush", 5 + rng.Next(4), alongX: true); break;
                case DensityFeature.BrushClump: Blob("DryBrush", 3 + rng.Next(4), alongX: true); break;
                case DensityFeature.Outcrop:
                {
                    Blob("Rock", 3 + rng.Next(3), alongX: false);
                    var lees = new List<(int, int)>();
                    foreach (var (dx, dy, _) in cells.ToArray())
                        if (!used.Contains((dx + lee, dy))) lees.Add((dx + lee, dy));
                    for (int i = 0, n = 2 + rng.Next(2); i < n && lees.Count > 0; i++) { int k = rng.Next(lees.Count); Add(lees[k].Item1, lees[k].Item2, "DryBrush"); lees.RemoveAt(k); }
                    break;
                }
            }
            return cells;
        }
    }
}
