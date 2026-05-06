namespace CavesOfOoo.Core
{
    /// <summary>
    /// WSP3.6 — Berserk: applied to the attacker (self-buff) by
    /// <see cref="Skills.Axe_Berserk"/>'s active ability. While
    /// active, the actor hits harder (via <see cref="STR_BONUS"/> on
    /// the Strength stat — feeds damage rolls naturally) and is
    /// easier to hit (via <see cref="DV_PENALTY"/>).
    ///
    /// <para>Stat path: stat-Penalty / stat-Bonus uses the existing
    /// <see cref="Stat.Penalty"/> / <see cref="Stat.Bonus"/> machinery
    /// (mirrors StunnedEffect's DV penalty pattern). No new combat-hook
    /// math needed — the bonus propagates through every existing call
    /// site that reads <c>StatUtils.GetModifier(actor, "Strength")</c>
    /// and <c>GetDV</c>.</para>
    ///
    /// <para>Per Qud's Berserk effect — same gameplay direction
    /// (offense up, defense down). Specific magnitudes simplified for
    /// CoO's tighter stat ranges.</para>
    /// </summary>
    public class BerserkEffect : Effect
    {
        public override string DisplayName => "berserk";

        /// <summary>Strength bonus while berserk. Feeds damage rolls
        /// via the standard StatUtils.GetModifier path.</summary>
        public const int STR_BONUS = 5;

        /// <summary>DV penalty while berserk — actor is easier to hit
        /// while raging. Mirrors Qud's Berserk DV penalty.</summary>
        public const int DV_PENALTY = 2;

        public BerserkEffect(int duration = 5)
        {
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            var str = target.GetStat("Strength");
            if (str != null) str.Bonus += STR_BONUS;
            var dv = target.GetStat("DV");
            if (dv != null) dv.Penalty += DV_PENALTY;
            MessageLog.Add(target.GetDisplayName() + " enters a blood frenzy!");
        }

        public override void OnRemove(Entity target)
        {
            var str = target.GetStat("Strength");
            if (str != null) str.Bonus -= STR_BONUS;
            var dv = target.GetStat("DV");
            if (dv != null) dv.Penalty -= DV_PENALTY;
            MessageLog.Add(target.GetDisplayName() + " calms from the frenzy.");
        }

        public override bool OnStack(Effect incoming)
        {
            // Refresh duration to the longer of the two — re-using
            // Berserk while it's active should reset the timer rather
            // than stack the bonus (mirrors Qud's CanRefreshAbilityEvent
            // gate that prevents re-cast while active).
            if (incoming is BerserkEffect berserk)
            {
                if (berserk.Duration > Duration) Duration = berserk.Duration;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&R";
    }
}
