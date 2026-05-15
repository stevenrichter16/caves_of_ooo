namespace CavesOfOoo.Core
{
    /// <summary>
    /// Attached to the player; listens for <c>AfterMove</c> events and
    /// advances the global clock by
    /// <see cref="WorldMapStepTurns"/> ticks when the move happened on
    /// the world-map zone. Represents the in-world time a single
    /// long-distance worldmap step takes — mirrors Qud's
    /// per-parasang tick burn (TerrainTravel.cs:222-264).
    ///
    /// <para>The Part is intentionally cheap: a single
    /// <see cref="HandleEvent"/> branch + an event-ID compare + a
    /// zone-id compare. Other zones (ground / underground) get no
    /// extra cost.</para>
    /// </summary>
    public class WorldMapTravelCostPart : Part
    {
        public override string Name => "WorldMapTravelCost";

        /// <summary>
        /// Ticks added to the global clock per cell of worldmap travel.
        /// Placeholder value — tuned in WM.7 playtest.
        /// </summary>
        public int WorldMapStepTurns = 10;

        public override bool HandleEvent(GameEvent e)
        {
            if (e == null || e.ID != "AfterMove") return true;
            // Only the player's own moves count for travel cost.
            var actor = e.GetParameter<Entity>("Actor");
            if (actor != ParentEntity) return true;
            // Only worldmap-zone moves consume travel time.
            var cell = e.GetParameter<Cell>("Cell");
            var zone = cell?.ParentZone;
            if (zone == null || !WorldMap.IsWorldMapZoneID(zone.ZoneID))
                return true;

            // Advance the global clock — no actor-energy ticks.
            TurnManager.Active?.AdvanceClock(WorldMapStepTurns);
            return true;
        }
    }
}
