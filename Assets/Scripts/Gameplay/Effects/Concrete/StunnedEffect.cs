namespace CavesOfOoo.Core
{
    /// <summary>
    /// Stun: prevents action, penalizes DV. Stacking extends duration.
    /// </summary>
    public class StunnedEffect : Effect
    {
        public override string DisplayName => "stunned";

        private const int DV_PENALTY = 4;

        public StunnedEffect(int duration = 2)
        {
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            var dv = target.GetStat("DV");
            if (dv != null)
                dv.Penalty += DV_PENALTY;
            MessageLog.Add(target.GetDisplayName() + " is stunned!");
        }

        public override void OnRemove(Entity target)
        {
            var dv = target.GetStat("DV");
            if (dv != null)
                dv.Penalty -= DV_PENALTY;
            MessageLog.Add(target.GetDisplayName() + " is no longer stunned.");
        }

        public override bool AllowAction(Entity target) => false;

        public override bool OnStack(Effect incoming)
        {
            if (incoming is StunnedEffect stun)
            {
                Duration += stun.Duration;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&C";
    }
}
