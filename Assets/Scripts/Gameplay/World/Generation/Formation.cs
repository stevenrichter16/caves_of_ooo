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

        // ── The Beating (W2, Docs/FELLING-W1-W2-PLAN.md §7.6) ────
        /// <summary>Polygon-cracked crust, zero cover — a duel floor
        /// with salt outcrops.</summary>
        SaltPan,
        /// <summary>The pre-Felling street grid standing waist-high out
        /// of the pan. The old world, walkable.</summary>
        RuinField,
        /// <summary>Soft banded dunes: slow going, occluded lines.</summary>
        DuneBelt,
        /// <summary>The well-to-well lane. The road is life; leaving it
        /// is a decision.</summary>
        CaravanRoad,
        /// <summary>Rock and dry brush. Scorpion country; the brush
        /// burns.</summary>
        WindBarrens,
        /// <summary>Thin crust over deep brine — standing water in the
        /// driest place in the world.</summary>
        BrineLens,

        // ── The Sodden (W3, Docs/FELLING-W3-PLAN.md §3) ──────────
        /// <summary>Archipelago — tussock paths over deep mire.
        /// Route-picking, cell by cell.</summary>
        OpenMire,
        /// <summary>Comb — harvested trenches, water-filled, worked
        /// banks. The Bog-Taken show in the cut faces.</summary>
        PeatCuts,
        /// <summary>Braid — one-cell channels through tall reeds.
        /// Ambush country.</summary>
        ReedMaze,
        /// <summary>Dead trees standing in shallow water. Open but
        /// obstructed.</summary>
        DrownedCopse,
        /// <summary>ONE duckboard line across everything — the safe
        /// line, and everyone knows it, including what hunts.</summary>
        Causeway,
        /// <summary>Edge — a tall cut showing strata. Readable
        /// stratigraphy; bodies at every depth.</summary>
        BogFace,

        // ── The Grovelands (W4, Docs/FELLING-W4-PLAN.md §3) ──────
        /// <summary>Radial — glowing columns ringing a clean seep, the
        /// grown sign at the east entry. The centre is the reason you
        /// came; the rules apply.</summary>
        Grove,
        /// <summary>Braid — tendrils tracing old water-veins. The
        /// Choir's fingertips, and they talk.</summary>
        TendrilFen,
        /// <summary>Rubble — short dense lines of vertical growth.
        /// Harvest country, climbing spores.</summary>
        FruitingWall,
        /// <summary>Ordered rows of the half-taken-back. Loot and
        /// horror in one pile; everything here is somebody.</summary>
        CompostingField,

        // ── The Stump (W6, Docs/FELLING-W6-PLAN.md §3) ───────────
        /// <summary>Parallel stone ridges — the wood grain of a bole a
        /// mile wide, one compass direction across every slope chunk,
        /// so the mountain reads as one object.</summary>
        Grainfield,
        /// <summary>Terrace shelves and spray pools where the
        /// blackwater creeks fall — the biodiversity hotspot.</summary>
        CascadeGorge,
        /// <summary>Petrified roots radiating downhill like walls of
        /// grain-marked stone. (Ships W6.2b.)</summary>
        ButtressRidge,
        /// <summary>Bromeliad scrub on stone domes; the tanks hold
        /// drinkable rain. (Ships W6.2b.)</summary>
        SummitScrub,
        /// <summary>The green crack — humid dwarf-forest at the rim,
        /// cloud passing through. (Ships W6.2b.)</summary>
        RimForest,
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

        /// <summary>The Beating: mostly open pans and barrens (the
        /// exposure IS the biome), the ruins and the brine rarer and
        /// more memorable.</summary>
        private static readonly Formation[] BeatingPool =
        {
            Formation.SaltPan,
            Formation.SaltPan,
            Formation.WindBarrens,
            Formation.WindBarrens,
            Formation.DuneBelt,
            Formation.CaravanRoad,
            Formation.RuinField,
            Formation.BrineLens,
        };

        /// <summary>The Sodden: mostly mire and reeds (the bog IS the
        /// place), the causeway and the bog-face rarer and more
        /// memorable.</summary>
        private static readonly Formation[] SoddenPool =
        {
            Formation.OpenMire,
            Formation.OpenMire,
            Formation.ReedMaze,
            Formation.ReedMaze,
            Formation.PeatCuts,
            Formation.DrownedCopse,
            Formation.Causeway,
            Formation.BogFace,
        };

        /// <summary>The Grovelands: groves ARE the biome — the ring and
        /// the seep in nearly half the chunks; the fen (where the
        /// tendrils talk), the walls, and the composting field rarer.</summary>
        private static readonly Formation[] GrovelandsPool =
        {
            Formation.Grove,
            Formation.Grove,
            Formation.Grove,
            Formation.TendrilFen,
            Formation.TendrilFen,
            Formation.FruitingWall,
            Formation.FruitingWall,
            Formation.CompostingField,
        };

        /// <summary>The Stump chooses by BAND, not by biome alone —
        /// elevation is the content key (W6.1). Pools carry only
        /// SHIPPED formations: W6.2a ships the Grainfield and the
        /// gorge; W6.2b widens each band's pool. An empty pool (the
        /// summit, this milestone) is plain stump ground, not an
        /// error.</summary>
        public static Formation ForStump(StumpBand band, string zoneID)
        {
            Formation[] pool = StumpPoolFor(band);
            if (pool == null || pool.Length == 0) return Formation.None;
            return pool[StableIndex(zoneID, pool.Length)];
        }

        private static readonly Formation[] StumpFoothillsPool =
        {
            Formation.CascadeGorge,
        };

        /// <summary>Mostly grain (the signature), the buttress rarer.</summary>
        private static readonly Formation[] StumpSlopesPool =
        {
            Formation.Grainfield,
            Formation.Grainfield,
            Formation.ButtressRidge,
        };

        /// <summary>Mostly scrub-and-domes; the green crack rarer.</summary>
        private static readonly Formation[] StumpSummitPool =
        {
            Formation.SummitScrub,
            Formation.SummitScrub,
            Formation.RimForest,
        };

        private static Formation[] StumpPoolFor(StumpBand band)
        {
            switch (band)
            {
                case StumpBand.Foothills: return StumpFoothillsPool;
                case StumpBand.Slopes:    return StumpSlopesPool;
                case StumpBand.Summit:    return StumpSummitPool;
                default:                  return null;
            }
        }

        private static Formation[] PoolFor(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Spread: return SpreadPool;
                case BiomeType.Beating: return BeatingPool;
                case BiomeType.Sodden: return SoddenPool;
                case BiomeType.Grovelands: return GrovelandsPool;
                default: return null;   // W5+ fill these in
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
