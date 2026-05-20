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
            GasSystem.OnTickEnd(SettlementRuntime.ActiveZone);
            return true;
        }
    }
}
