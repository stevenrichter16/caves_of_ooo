namespace CavesOfOoo.Core
{
    /// <summary>
    /// Stun: prevents action, penalizes DV. Stacking extends duration and
    /// keeps the harder save.
    ///
    /// Saving-throw recovery (user-directed, 2026-07-19; mirrors
    /// BleedingEffect): when <see cref="SaveTarget"/> &gt; 0, each end of
    /// the victim's turn rolls 1d20 + Toughness modifier vs the target —
    /// success shakes the stun off early (cause save_succeeded), and the
    /// target eases by 1 per failed turn. Duration stays the hard ceiling.
    /// SaveTarget 0 (the default) disables the save entirely, so plain
    /// countdown stuns remain deterministic.
    /// </summary>
    public class StunnedEffect : Effect
    {
        public override string DisplayName => "stunned";

        // WSP6.16 — TYPE_NEGATIVE backfill (see AcidicEffect.cs).
        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        private const int DV_PENALTY = 4;

        /// <summary>Save DC; 0 disables the save (pure countdown).</summary>
        public int SaveTarget;

        public System.Random Rng;

        public StunnedEffect(int duration = 2, int saveTarget = 0, System.Random rng = null)
        {
            Duration = duration;
            SaveTarget = saveTarget;
            Rng = rng ?? new System.Random();
        }

        public override void OnTurnEnd(Entity target)
        {
            if (SaveTarget > 0)
            {
                int toughMod = StatUtils.GetModifier(target, "Toughness");
                int roll = DiceRoller.Roll(20, Rng) + toughMod;
                if (roll >= SaveTarget)
                {
                    LastRemovalCause = CAUSE_SAVE_SUCCEEDED;
                    Duration = 0; // cleaned up by HandleEndTurn
                    return;
                }

                // Easing, mirroring Bleeding: escape gets likelier each turn.
                if (SaveTarget > 1)
                    SaveTarget--;
            }

            base.OnTurnEnd(target);
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
                if (stun.SaveTarget > SaveTarget)
                    SaveTarget = stun.SaveTarget;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&C";
    }
}
