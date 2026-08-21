namespace CavesOfOoo.Core
{
    /// <summary>
    /// Singleton Part on the world entity that listens to <c>TickEnd</c>
    /// and drives the per-turn crop growth pass via
    /// <see cref="CropSystem.OnTickEnd"/>. Byte-for-byte mirror of
    /// <see cref="GasSystemPart"/> (GasSystemPart.cs:18-32) — bootstrap
    /// creates the world entity, attaches this Part, TurnManager fires
    /// TickEnd, this Part forwards to the static system.
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
            // Wx opt review §0 — same gate as GasSystemPart: TickEnd is
            // per ACTOR, growth is per ROUND. Ungated, crops matured and
            // dried out faster in populated zones. Unstamped events
            // (benches/tests) keep the old contract. Pinned by
            // TickEndActorGateTests.
            var actor = e.GetParameter<Entity>("Actor");
            if (actor != null && !actor.HasTag("Player")) return true;
            CropSystem.OnTickEnd(SettlementRuntime.ActiveZone);
            return true;
        }
    }
}
