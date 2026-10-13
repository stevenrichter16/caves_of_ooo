namespace CavesOfOoo.Core
{
    public enum MeleeStrikeOutcome { Cancelled, NaturalMiss, FullParry, ArmorStopped, Hit }

    /// <summary>Only this strike's committed damage; Killed also includes its contact chain.
    /// A full parry is not a natural miss and must suppress external contact riders.</summary>
    public readonly struct MeleeStrikeResult
    {
        public readonly MeleeStrikeOutcome Outcome;
        public readonly int ActualDamage;
        public readonly bool Killed;
        public bool AllowsContactRider => Outcome != MeleeStrikeOutcome.FullParry && Outcome != MeleeStrikeOutcome.Cancelled;
        public MeleeStrikeResult(MeleeStrikeOutcome outcome, int damage = 0, bool killed = false)
        { Outcome = outcome; ActualDamage = damage; Killed = killed; }
    }
}
