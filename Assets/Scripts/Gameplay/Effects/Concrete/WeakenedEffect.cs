namespace CavesOfOoo.Core
{
    /// <summary>
    /// A temporary Strength penalty. Reapplication keeps one effect with the
    /// strongest penalty and longer remaining duration; upgrades apply only the
    /// difference so removal restores exactly this effect's contribution while
    /// the actor retains the same Strength statistic.
    /// StrPenalty and Duration persist alongside the actor's stat modifiers;
    /// loading rebinds Owner without applying the penalty again.
    /// </summary>
    public class WeakenedEffect : Effect
    {
        public override string DisplayName => "weakened";

        /// <summary>How many points of Strength this effect removes while active.</summary>
        public int StrPenalty;

        public WeakenedEffect(int strPenalty = 2, int duration = 3)
        {
            StrPenalty = strPenalty;
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            var str = target.GetStat("Strength");
            if (str == null) return;
            int before = str.Value;
            str.Penalty += StrPenalty;
            string durationText = Duration == DURATION_INDEFINITE ? "until removed" : $"for {Duration} turns";
            MessageLog.Add($"{target.GetDisplayName()} is weakened: Strength {before} to {str.Value} {durationText}.");
        }

        public override void OnRemove(Entity target)
        {
            var str = target.GetStat("Strength");
            if (str == null) return;
            str.Penalty -= StrPenalty;
            MessageLog.Add($"{target.GetDisplayName()} recovers from weakness: Strength {str.Value}.");
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is WeakenedEffect weakenedEffect)
            {
                if (weakenedEffect.StrPenalty > StrPenalty)
                {
                    var strength = Owner?.GetStat("Strength");
                    if (strength != null)
                        strength.Penalty += weakenedEffect.StrPenalty - StrPenalty;
                    StrPenalty = weakenedEffect.StrPenalty;
                }
                if (Duration == DURATION_INDEFINITE || weakenedEffect.Duration == DURATION_INDEFINITE)
                    Duration = DURATION_INDEFINITE;
                else if (weakenedEffect.Duration > Duration)
                    Duration = weakenedEffect.Duration;
                return true;
            }
            return false;
        }
        
        public override string GetRenderColorOverride() => "&Y";

    }
}
