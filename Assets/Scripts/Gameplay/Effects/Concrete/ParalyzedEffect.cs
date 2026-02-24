namespace CavesOfOoo.Core
{
    /// <summary>
    /// Paralysis: prevents action, heavily penalizes DV. Stacking extends duration.
    /// </summary>
    public class ParalyzedEffect : Effect
    {
        public override string DisplayName => "paralyzed";

        private const int DV_PENALTY = 6;

        public ParalyzedEffect(int duration = 3)
        {
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            var dv = target.GetStat("DV");
            if (dv != null)
                dv.Penalty += DV_PENALTY;
            MessageLog.Add(target.GetDisplayName() + " is paralyzed!");
        }

        public override void OnRemove(Entity target)
        {
            var dv = target.GetStat("DV");
            if (dv != null)
                dv.Penalty -= DV_PENALTY;
            MessageLog.Add(target.GetDisplayName() + " is no longer paralyzed.");
        }

        public override bool AllowAction(Entity target) => false;

        public override bool OnStack(Effect incoming)
        {
            if (incoming is ParalyzedEffect para)
            {
                Duration += para.Duration;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&c";
    }
}
