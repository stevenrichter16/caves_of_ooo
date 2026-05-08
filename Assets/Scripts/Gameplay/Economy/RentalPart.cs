namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an item as rented from a lessor NPC. Records how many Ink
    /// the renter paid and which lessor (by blueprint name) issued the
    /// rental, so <see cref="RentalSystem.TryReturn"/> can compute the
    /// refund and reject returns at the wrong NPC.
    ///
    /// Lifetime: attached by <see cref="RentalSystem.TryRent"/>; removed
    /// when the item is returned. If the renter dies, the item drops
    /// with the rest of their inventory and the part stays attached
    /// (the rental is effectively lost — there is no auto-recovery in
    /// v1; see Docs/WEAPON-RENTAL-SYSTEM.md §1).
    /// </summary>
    public class RentalPart : Part
    {
        public override string Name => "Rental";

        /// <summary>How many Ink the renter spent to rent this item.</summary>
        public int InkPaid;

        /// <summary>Blueprint name of the lessor NPC. Returns at any
        /// other NPC are refused so a single Quartermaster blueprint
        /// can be shared across multiple zone instances and still
        /// route correctly.</summary>
        public string LessorBlueprintName;

        /// <summary>
        /// Veto sales of rented items. Without this guard a player
        /// could rent a weapon for cheap Ink, sell it to the village
        /// merchant for full Drams, and walk off — keeping the value
        /// of the weapon AND most of the Ink. The
        /// <see cref="TradeSystem.CanBeTraded"/> path fires this event
        /// on the item itself, so the veto is colocated with the
        /// rental flag rather than scattered across TradeSystem.
        /// </summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "CanBeTraded")
                return false;
            return true;
        }
    }
}
