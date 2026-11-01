namespace CavesOfOoo.Core
{
    /// <summary>
    /// The economy's upkeep when the player enters a zone (called by <c>InputHandler.HandleZoneTransition</c>):
    /// traders there restock (SM3; EC6 salt purses for barter traders), and the player's Concord account is
    /// reviewed, so a debt past its day is found in default when the player travels (EC7,
    /// Docs/ECONOMY-SYSTEM.md §12.4). Nothing here runs per turn.
    /// </summary>
    public static class EconomyUpkeep
    {
        /// <summary>Returns the restock count from <see cref="TraderRestockSystem.RestockZone"/>.</summary>
        public static int OnZoneEntered(Entity player, Zone zone, int tick)
        {
            int restocked = TraderRestockSystem.RestockZone(zone, tick);
            ConcordAccount.Review(player, tick);
            return restocked;
        }
    }
}
