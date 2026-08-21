namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.3 — singleton Part on the world entity that listens to
    /// <c>TickEnd</c> and drives the per-turn gas dispersal pass via
    /// <see cref="GasSystem.OnTickEnd"/>. Mirrors
    /// <see cref="NarrativeStatePart"/>'s shape (NarrativeStatePart.cs:62-77)
    /// so the wiring through <c>GameBootstrap</c> follows the same
    /// pattern: bootstrap creates the world entity, attaches this Part,
    /// TurnManager fires TickEnd, this Part forwards to GasSystem.
    ///
    /// <para><b>Zone resolution.</b> Uses
    /// <see cref="SettlementRuntime.ActiveZone"/> — the static accessor
    /// the rest of the codebase uses (reflect, knockback, well/oven
    /// sites). Null in EditMode tests without a scene; the system
    /// gracefully no-ops in that case.</para>
    /// </summary>
    public sealed class GasSystemPart : Part
    {
        public override string Name => "GasSystem";

        private static readonly int TickEndEventID = GameEvent.GetID("TickEnd");

        public override bool WantEvent(int eventID) => eventID == TickEndEventID;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "TickEnd") return true;
            // Wx opt review §0 — TickEnd fires once per ACTOR
            // (TurnManager.EndTurn), but gas semantics are per ROUND:
            // ungated, cloud lifetime and the per-turn poison dose scaled
            // with zone population (4 actors = 4× the authored dose).
            // ZoneTileStateSystem.cs:25-30 documents this exact trap.
            // Forward only when the round ends — the player's turn end —
            // or when the event is unstamped (benches/tests firing bare
            // TickEnd mean "a round passed"). Pinned by
            // TickEndActorGateTests.
            var actor = e.GetParameter<Entity>("Actor");
            if (actor != null && !actor.HasTag("Player")) return true;
            GasSystem.OnTickEnd(SettlementRuntime.ActiveZone);
            return true;
        }
    }
}
