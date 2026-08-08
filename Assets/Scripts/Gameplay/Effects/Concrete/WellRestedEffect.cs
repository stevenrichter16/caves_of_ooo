namespace CavesOfOoo.Core
{
    /// <summary>
    /// BIOME-OVERHAUL A4 — Well Rested: +10 Speed after a night in a
    /// real bed (the inn's paid rest, distinguishing it from a free
    /// campfire nap). Stat boost/revert shape mirrors
    /// <see cref="HobbledEffect"/>; re-application refreshes rather
    /// than stacks (two nights of sleep don't make you twice as fast).
    /// </summary>
    public class WellRestedEffect : Effect
    {
        public override string DisplayName => "well rested";

        public override int GetEffectType() => TYPE_GENERAL;

        public const int SPEED_BONUS = 10;

        public WellRestedEffect(int duration = 100)
        {
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            var speed = target.GetStat("Speed");
            if (speed != null)
                speed.Boost += SPEED_BONUS;
            MessageLog.Add(target.GetDisplayName() + " feels well rested.");
        }

        public override void OnRemove(Entity target)
        {
            var speed = target.GetStat("Speed");
            if (speed != null)
                speed.Boost -= SPEED_BONUS;
            MessageLog.Add(target.GetDisplayName() + "'s good night of sleep wears off.");
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is WellRestedEffect fresh)
            {
                // Refresh, don't accumulate.
                if (fresh.Duration > Duration)
                    Duration = fresh.Duration;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&G";
    }
}
