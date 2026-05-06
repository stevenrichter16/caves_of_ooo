namespace CavesOfOoo.Core
{
    /// <summary>
    /// WSP2.1 — Hobbled: limping/encumbered movement penalty. Applied
    /// passively by <see cref="Skills.ShortBlades_Hobble"/> on Piercing
    /// hits. Mirrors Qud's Hobbled effect.
    ///
    /// <para>Mechanic in CoO: -3 DV for the duration. Reframed from Qud's
    /// "-50% move speed" since CoO has no per-actor move-speed stat
    /// (movement is per-tile-per-turn, gated by AllowAction). The DV
    /// penalty represents "limping makes you easier to hit" — same
    /// gameplay direction (target is more vulnerable) without needing
    /// a new movement-speed system.</para>
    ///
    /// <para>Stacks: <see cref="OnStack"/> extends duration (mirrors
    /// StunnedEffect's pattern). Multi-application from chained Piercing
    /// hits adds up reasonably.</para>
    /// </summary>
    public class HobbledEffect : Effect
    {
        public override string DisplayName => "hobbled";

        // WSP6.16 — TYPE_NEGATIVE backfill (see AcidicEffect.cs).
        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        public const int DV_PENALTY = 3;

        public HobbledEffect(int duration = 8)
        {
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            var dv = target.GetStat("DV");
            if (dv != null)
                dv.Penalty += DV_PENALTY;
            MessageLog.Add(target.GetDisplayName() + " is hobbled!");
        }

        public override void OnRemove(Entity target)
        {
            var dv = target.GetStat("DV");
            if (dv != null)
                dv.Penalty -= DV_PENALTY;
            MessageLog.Add(target.GetDisplayName() + " recovers from being hobbled.");
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is HobbledEffect hobbled)
            {
                Duration += hobbled.Duration;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&y";
    }
}
