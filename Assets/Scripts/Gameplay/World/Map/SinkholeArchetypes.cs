namespace CavesOfOoo.Core
{
    /// <summary>W5.3 — what is at the bottom of a hole. Append-only,
    /// like the Formation enum: canon designs eight, W5 ships three
    /// end-to-end rather than eight halfway.</summary>
    public enum SinkholeArchetype
    {
        DrownedSima,
        StrandedSettlement,
        ChoirCathedral,
    }

    /// <summary>
    /// Which floor a sinkhole has. Canon: "one archetype per sinkhole
    /// (rolled at worldgen, then FIXED — the world persists)".
    ///
    /// <para><b>R3:</b> the answer is a PURE FUNCTION of the sinkhole's
    /// name — never the builder rng (which pipeline retries reseed) and
    /// never a saved field (which a format bump can drop, the way
    /// W4.6a's village profiles were dropped until W4.7 caught it).
    /// Pure means fixed for free, and identical before and after any
    /// save.</para>
    ///
    /// <para><b>Where canon named a place, canon wins.</b> A hole called
    /// the Deepest Cathedral is a cathedral; the hash is the fallback
    /// for holes nobody named.</para>
    /// </summary>
    public static class SinkholeArchetypes
    {
        public static SinkholeArchetype For(string sinkholeName)
        {
            switch (sinkholeName)
            {
                // Authored — the name is the design.
                case "the Deepest Cathedral": return SinkholeArchetype.ChoirCathedral;
                case "Lampwell":              return SinkholeArchetype.DrownedSima;
                case "Spivenor":              return SinkholeArchetype.DrownedSima;
                case "Olderdeep":             return SinkholeArchetype.StrandedSettlement;
            }
            // Unnamed: stable hash, same shape as the formation pools.
            int i = FormationSelector.StableIndex("SinkholeFloor|" + (sinkholeName ?? ""), 3);
            return (SinkholeArchetype)i;
        }
    }
}
