namespace CavesOfOoo.Core
{
    /// <summary>The sender who can book a single marked physical shipment.</summary>
    public sealed class LocalShipmentSponsorPart : Part
    {
        public override string Name => "LocalShipmentSponsor";
        public Entity Ledger;
        public int LastAttemptTick = -50;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != AIBoredEvent.ID || Ledger?.GetPart<LocalPorterLedgerPart>() is not { } order
                || order.OrderID != null || ParentEntity is not { } sender) return true;
            var zone = sender.SpatialZone;
            int tick = WorldClock.CurrentTick;
            if ((long)tick < (long)LastAttemptTick + 50 || !order.SiteCurrent(zone)) return true;
            LastAttemptTick = tick;
            if (!order.TryBookShipment(this, zone)) return true;
            e.Handled = true;
            return false;
        }
    }
}
