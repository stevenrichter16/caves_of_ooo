namespace CavesOfOoo.Core
{
    /// <summary>
    /// ALPHA-READINESS item 7 SM3 — periodic trader dram restock. Once
    /// the player sold a village out of drams, the trader could NEVER
    /// buy again: blueprint Drams were seeded at spawn and only ever
    /// drained. On each zone entry (InputHandler.HandleZoneTransition),
    /// villager-faction entities that carry a Drams property and whose
    /// last restock is at least <see cref="RestockIntervalTurns"/> old
    /// are topped back up to <see cref="DramsFloor"/>.
    ///
    /// <para>Save-safe: the per-trader stamp lives in the entity's int
    /// properties (round-trip through the save graph, like Drams
    /// itself). Deliberately a FLOOR top-up, not a blueprint-value
    /// restore — resolving each trader's original blueprint value would
    /// need factory plumbing through the input layer; the alpha need is
    /// "traders can buy again", and rich traders are unaffected
    /// (top-up only applies below the floor).</para>
    /// </summary>
    public static class TraderRestockSystem
    {
        public const int RestockIntervalTurns = 300;
        public const int DramsFloor = 100;
        public const string LastRestockProp = "LastRestockTurn";

        /// <summary>Restock eligible traders in the zone. Returns the
        /// number of traders whose drams were topped up.</summary>
        public static int RestockZone(Zone zone, int currentTurn)
        {
            if (zone == null) return 0;

            int restocked = 0;
            var creatures = zone.GetEntitiesWithTag("Creature");
            for (int i = 0; i < creatures.Count; i++)
            {
                var e = creatures[i];
                string faction;
                if (!e.Tags.TryGetValue("Faction", out faction) || faction != "Villagers")
                    continue;
                // "Is a trader" = carries the Drams property at all
                // (blueprint-seeded); ordinary villagers have none.
                int drams = e.GetIntProperty(TradeSystem.CURRENCY_PROP, -1);
                if (drams < 0)
                    continue;

                int last = e.GetIntProperty(LastRestockProp, int.MinValue);
                if (last != int.MinValue && currentTurn - last <= RestockIntervalTurns)
                    continue;

                if (drams < DramsFloor)
                {
                    TradeSystem.SetDrams(e, DramsFloor);
                    restocked++;
                }
                e.SetIntProperty(LastRestockProp, currentTurn);
            }
            return restocked;
        }
    }
}
