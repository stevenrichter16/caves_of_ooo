using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Scatters the heavy, un-carryable objects the drag mechanic exists for
    /// (<c>Docs/DRAG-AND-HAUL.md</c>).
    ///
    /// <para><b>Why a builder at all.</b> Every rule in D1–D5 was correct and
    /// completely unreachable, because nothing placed a single haulable
    /// object anywhere in the world. A mechanic the player cannot meet is
    /// not shipped.</para>
    ///
    /// <para><b>Deliberately sparse.</b> One or two per zone at most, and
    /// often none. A millstone is a landmark, not scenery — finding one
    /// should be a small event, and a floor littered with anvils would make
    /// the whole thing feel like debris.</para>
    ///
    /// <para>Priority 4200: after population (4000) and stocking (4100) so
    /// the open floor is already known, before dramas (4500).</para>
    /// </summary>
    public sealed class HaulablePropBuilder : IZoneBuilder
    {
        public string Name => "HaulableProps";
        public int Priority => 4200;

        /// <summary>Per-mille chance of placing anything at all in a zone.
        /// 350 = roughly a third of zones have one.</summary>
        public int ChancePerMille = 350;

        private readonly BiomeType _biome;

        public HaulablePropBuilder(BiomeType biome) { _biome = biome; }

        /// <summary>What turns up where. A haulable should tell you
        /// something about the place you found it in.</summary>
        private static readonly Dictionary<BiomeType, string[]> Pools = new()
        {
            // Settled country: the leavings of work.
            [BiomeType.Spread] = new[] { "HaulBarrel", "FallenBeam", "MillStone" },
            [BiomeType.Grovelands] = new[] { "FallenBeam", "HaulBarrel" },
            // Wet country: what the flood carried and dropped.
            [BiomeType.Sodden] = new[] { "FallenBeam", "HaulBarrel" },
            // Salt and sun: what keeps out here, and what holds it.
            [BiomeType.Beating] = new[] { "SaltCuredBody", "StoneCoffer" },
            [BiomeType.Stump] = new[] { "MillStone", "StoneCoffer" },
            // The scraped region. A single anvil in all that emptiness is
            // worth more than a dozen elsewhere.
            [BiomeType.Overwrit] = new[] { "SmithAnvil" },
            [BiomeType.Ruins] = new[] { "StoneCoffer", "SmithAnvil", "FallenBeam" },
            [BiomeType.Cave] = new[] { "FallenBeam", "StoneCoffer" },
            [BiomeType.Desert] = new[] { "SaltCuredBody", "StoneCoffer" },
            [BiomeType.Jungle] = new[] { "FallenBeam", "HaulBarrel" },
        };

        /// <summary>
        /// Always returns true. The pipeline reads false as "this zone
        /// failed to generate, throw it away and retry with a new seed"
        /// (<c>ZoneGenerationPipeline.cs:36-40</c>) — NOT as "I placed
        /// nothing". A decorative pass that declines to act has still
        /// succeeded, and returning false here failed every zone whose roll
        /// came up empty.
        /// </summary>
        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;
            if (!Pools.TryGetValue(_biome, out string[] pool) || pool.Length == 0) return true;
            if (rng.Next(1000) >= ChancePerMille) return true;

            string blueprint = pool[rng.Next(pool.Length)];

            // Try a bounded number of spots rather than scanning the zone:
            // if 40 random cells are all unsuitable the zone is too full to
            // want another solid object in it anyway.
            for (int attempt = 0; attempt < 40; attempt++)
            {
                int x = rng.Next(1, Zone.Width - 1);
                int y = rng.Next(1, Zone.Height - 1);

                var cell = zone.GetCell(x, y);
                if (cell == null || cell.BlocksMovement()) continue;

                // Never wall off a doorway or a corridor: a solid object in
                // a one-cell gap can seal a zone, and the player has no verb
                // that removes it except the drag this places it for — which
                // they may not be strong enough to use.
                if (!HasOpenNeighbourOnBothSides(zone, x, y)) continue;

                if (BuilderSpawn.TryPlace(zone, factory, blueprint, x, y) != null)
                    return true;
            }

            return true;   // no room for one; the zone is still fine
        }

        /// <summary>
        /// True when the cell has open ground on both the horizontal and the
        /// vertical, i.e. it is in the middle of a space rather than plugging
        /// a gap between two walls.
        /// </summary>
        private static bool HasOpenNeighbourOnBothSides(Zone zone, int x, int y)
        {
            bool horizontal = IsOpen(zone, x - 1, y) && IsOpen(zone, x + 1, y);
            bool vertical = IsOpen(zone, x, y - 1) && IsOpen(zone, x, y + 1);
            return horizontal && vertical;
        }

        private static bool IsOpen(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            return cell != null && !cell.BlocksMovement();
        }
    }
}
