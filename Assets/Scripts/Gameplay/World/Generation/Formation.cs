namespace CavesOfOoo.Core
{
    /// <summary>
    /// The kind of place a chunk is, within its biome.
    ///
    /// <para><b>This is the anti-sameness machine</b>
    /// (<c>Docs/FELLING-WORLD-DESIGN.md</c> §formations). The old world
    /// generated every chunk of a biome from one recipe with a noise seed,
    /// so walking three screens of the Spread showed you the same field
    /// three times with the grass in different places. A formation changes
    /// the chunk's <i>topology</i> — what is a room, what is a line, what is
    /// open — not just its scatter.</para>
    ///
    /// <para>Append-only: formations are selected by index off a stable
    /// hash, so reordering this enum silently re-rolls the whole world.</para>
    /// </summary>
    public enum Formation
    {
        /// <summary>No formation chosen — the biome's plain recipe.</summary>
        None = 0,

        // ── The Spread ──────────────────────────────────────────
        /// <summary>Hedges cut the open country into rooms.</summary>
        Hedgerow,
        /// <summary>A worked lane running through crop strips.</summary>
        FieldStrips,
        /// <summary>The old road: fast, straight, and watched.</summary>
        OldRoad,
        /// <summary>Flower-charm meadow — the instrument you do not know
        /// you have yet.</summary>
        FlowerMeadow,
        /// <summary>Gone-back ground: scrub reclaiming a field nobody
        /// works any more.</summary>
        Fallow,
        /// <summary>Water-meadow along a river edge.</summary>
        RiverMeadow,
    }

    /// <summary>
    /// Picks a chunk's formation, deterministically and stably.
    ///
    /// <para><b>Keyed on the zone ID, not on the generation RNG.</b> That
    /// matters more than it looks: an RNG-derived choice shifts the moment
    /// any builder earlier in the pipeline consumes a different number of
    /// rolls, so adding one prop scatter silently re-rolls every formation
    /// in the world. Hashing the zone ID means a given chunk is the same
    /// kind of place across code changes, save files, and sessions.</para>
    /// </summary>
    public static class FormationSelector
    {
        /// <summary>The Spread's formations, in the proportions the region
        /// should feel like: mostly worked country, the road and the meadow
        /// rarer and more memorable.</summary>
        private static readonly Formation[] SpreadPool =
        {
            Formation.Hedgerow,
            Formation.Hedgerow,
            Formation.FieldStrips,
            Formation.FieldStrips,
            Formation.Fallow,
            Formation.RiverMeadow,
            Formation.OldRoad,
            Formation.FlowerMeadow,
        };

        public static Formation For(BiomeType biome, string zoneID)
        {
            Formation[] pool = PoolFor(biome);
            if (pool == null || pool.Length == 0) return Formation.None;
            return pool[StableIndex(zoneID, pool.Length)];
        }

        private static Formation[] PoolFor(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Spread: return SpreadPool;
                default: return null;   // W2+ fill these in
            }
        }

        /// <summary>
        /// A stable, non-negative index in [0, count).
        ///
        /// <para>FNV-1a rather than <c>string.GetHashCode</c>, which is
        /// randomised per process on modern .NET — using it here would give
        /// a chunk a different formation every time the game launched.</para>
        /// </summary>
        public static int StableIndex(string key, int count)
        {
            if (count <= 0) return 0;
            unchecked
            {
                const uint offset = 2166136261;
                const uint prime = 16777619;
                uint hash = offset;
                if (key != null)
                {
                    for (int i = 0; i < key.Length; i++)
                    {
                        hash ^= key[i];
                        hash *= prime;
                    }
                }
                return (int)(hash % (uint)count);
            }
        }
    }
}
