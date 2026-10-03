namespace CavesOfOoo.Core
{
    /// <summary>
    /// Singleton Part on the world entity that listens to <c>TickEnd</c>
    /// and reconciles elapsed world time for a player boundary. The
    /// presentation loop also reconciles after the scheduler advances;
    /// repeating a clock value cannot create growth. Bare, actorless events
    /// retain the explicit authored-unit fixture contract.
    ///
    /// <para><b>Zone resolution.</b> Uses
    /// <see cref="SettlementRuntime.ActiveZone"/>; null in EditMode
    /// tests without a scene — the system gracefully no-ops.</para>
    /// </summary>
    public sealed class CropSystemPart : Part
    {
        public override string Name => "CropSystem";

        private static readonly int TickEndEventID = GameEvent.GetID("TickEnd");

        public override bool WantEvent(int eventID) => eventID == TickEndEventID;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "TickEnd") return true;
            // NPC turns do not create a second reconciliation boundary.
            // Runtime EndTurn always stamps Actor; only legacy direct
            // drivers use an actorless event as one authored growth unit.
            var actor = e.GetParameter<Entity>("Actor");
            if (actor != null && !actor.HasTag("Player")) return true;
            if (actor == null) CropSystem.OnTickEnd(SettlementRuntime.ActiveZone);
            else CropTime.ReconcileZone(SettlementRuntime.ActiveZone, WorldClock.CurrentTick);
            return true;
        }
    }
}
