namespace CavesOfOoo.Core
{
    /// <summary>Saved identity and bell awareness on a normal living actor.</summary>
    public sealed class MorrowfastResidentPart : Part
    {
        public override string Name => "MorrowfastResident";
        public string ResidentId = "";
        public int LastBellTurn = -1;
        public override void OnAfterLoad(SaveReader reader) { MorrowfastContent.EnsureRegistered(); }
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "MorrowfastBellRung") LastBellTurn = TurnManager.Active?.TickCount ?? 0;
            return true;
        }
    }
}
