namespace CavesOfOoo.Core
{
    /// <summary>
    /// WSP2.1 — ShatterArmor: armor crumbles for the duration, reducing
    /// the wearer's effective AV. Applied passively by
    /// <see cref="Skills.Cudgel_ShatteringBlows"/> on Cudgel hits.
    ///
    /// <para>Mechanic: while owned, <see cref="CombatSystem.GetAV"/>
    /// subtracts <see cref="AV_REDUCTION"/> from the defender's
    /// effective AV. Multiple stacks add their reductions linearly
    /// (worst case armor goes to 0 — the GetAV hook clamps to non-negative).
    /// Mirrors Qud's ShatterArmor magnitude-based AV decay; CoO uses a
    /// fixed-amount-per-stack since CoO doesn't have Qud's per-armor
    /// shatter-points accumulation system.</para>
    ///
    /// <para>Stacks: <see cref="OnStack"/> extends duration AND
    /// accumulates per-stack reduction via the <see cref="StackCount"/>
    /// counter. <c>GetAV</c> reads <c>AV_REDUCTION * StackCount</c>.</para>
    /// </summary>
    public class ShatterArmorEffect : Effect
    {
        public override string DisplayName => "shattered armor";

        // WSP6.16 — TYPE_NEGATIVE backfill (see AcidicEffect.cs).
        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        /// <summary>AV reduction PER STACK. Fixed CoO value (Qud uses
        /// per-armor magnitude pools).</summary>
        public const int AV_REDUCTION = 2;

        /// <summary>How many ShatterArmor effects have been stacked
        /// onto this one. Drives the AV reduction in GetAV.</summary>
        public int StackCount = 1;

        public ShatterArmorEffect(int duration = 4)
        {
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + "'s armor cracks!");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + "'s armor knits back together.");
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is ShatterArmorEffect shatter)
            {
                Duration += shatter.Duration;
                StackCount += shatter.StackCount;
                MessageLog.Add(Owner.GetDisplayName() + "'s armor cracks further!");
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&K";
    }
}
