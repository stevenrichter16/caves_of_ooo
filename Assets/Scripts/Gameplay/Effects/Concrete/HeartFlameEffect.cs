namespace CavesOfOoo.Core
{
    /// <summary>Saved fire-cast charges; ordinary effect duration is owner actions.</summary>
    public sealed class HeartFlameEffect : Effect
    {
        public override string DisplayName => "heart flame";
        public int ChargesRemaining;

        public HeartFlameEffect(int charges = 3, int duration = 5)
        { ChargesRemaining = System.Math.Max(0, charges); Duration = duration; }

        public override bool OnStack(Effect incoming)
        {
            if (!(incoming is HeartFlameEffect heart)) return false;
            ChargesRemaining = heart.ChargesRemaining; Duration = heart.Duration;
            JustApplied = TurnManager.Active?.CurrentActor == Owner;
            return true;
        }
    }
}
