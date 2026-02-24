namespace CavesOfOoo.Core
{
    /// <summary>
    /// Confused: penalizes DV and Agility. NPCs may move randomly.
    /// Does not stack (rejects duplicate).
    /// </summary>
    public class ConfusedEffect : Effect
    {
        public override string DisplayName => "confused";

        private const int DV_PENALTY = 2;
        private const int AGI_PENALTY = 2;

        public ConfusedEffect(int duration = 4)
        {
            Duration = duration;
        }

        public override bool CanApply(Entity target)
        {
            // Cannot stack confusion
            var sep = target.GetPart<StatusEffectsPart>();
            if (sep != null && sep.HasEffect<ConfusedEffect>())
                return false;
            return true;
        }

        public override void OnApply(Entity target)
        {
            var dv = target.GetStat("DV");
            if (dv != null)
                dv.Penalty += DV_PENALTY;
            var agi = target.GetStat("Agility");
            if (agi != null)
                agi.Penalty += AGI_PENALTY;
            MessageLog.Add(target.GetDisplayName() + " is confused!");
        }

        public override void OnRemove(Entity target)
        {
            var dv = target.GetStat("DV");
            if (dv != null)
                dv.Penalty -= DV_PENALTY;
            var agi = target.GetStat("Agility");
            if (agi != null)
                agi.Penalty -= AGI_PENALTY;
            MessageLog.Add(target.GetDisplayName() + " is no longer confused.");
        }

        public override string GetRenderColorOverride() => "&W";
    }
}
