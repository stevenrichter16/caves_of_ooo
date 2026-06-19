namespace CavesOfOoo.Core
{
    /// <summary>
    /// SCAFFOLD for Challenge 3 — "Brew a New Status Effect".
    /// See Docs/PROGRAMMING-CHALLENGES.md §Challenge 3.
    ///
    /// Goal: a debuff that saps Strength for a few turns. Right now the
    /// lifecycle hooks are empty, so WeakenedEffectChallengeTests fails (RED).
    /// Implement the TODOs to turn it GREEN, in this order:
    ///   1. OnApply  -> Apply_LowersStrength
    ///   2. OnRemove -> Remove_RestoresStrength
    ///   3. a "weakened" case in OnHitEffectFactory.Create -> Factory_MapsWeakenedName
    ///   4. OnStack  -> Reapplying_DoesNotDoubleStack
    ///
    /// Model: BerserkEffect.cs is the mirror image — it ADDS to str.Bonus on
    /// apply and subtracts on remove. You do the inverse: ADD to str.Penalty on
    /// apply, subtract it back on remove. (Penalty is reversible and clamps
    /// cleanly; never mutate BaseValue for a temporary debuff.)
    ///
    /// Duration counts down on its own — the base Effect.OnTurnEnd decrements it
    /// — so a fixed-length debuff needs no per-turn code.
    /// </summary>
    public class WeakenedEffect : Effect
    {
        // DisplayName is abstract on Effect, so it MUST be implemented for the
        // class to compile. The rest of the behavior is yours to write.
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
            str.Penalty += StrPenalty;
            MessageLog.Add($"Strength of {target.GetDisplayName()} weakened from {str.BaseValue} to {str.BaseValue - StrPenalty} for {Duration} turns.");
        }

        public override void OnRemove(Entity target)
        {
            var str = target.GetStat("Strength");
            if (str == null) return;
            str.Penalty -= StrPenalty;
            MessageLog.Add($"Strength of {target.GetDisplayName()} reset to  {str.BaseValue}.");
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is WeakenedEffect weakenedEffect)
            {
                if (weakenedEffect.StrPenalty > StrPenalty)
                {
                    StrPenalty = weakenedEffect.StrPenalty;
                }
                return true;
            }
            return false;
        }
        
        public override string GetRenderColorOverride() => "&Y";

        // TODO (Challenge 3):
        //   public override void OnApply(Entity target)  -> target.GetStat("Strength").Penalty += StrPenalty;
        //   public override void OnRemove(Entity target) -> ... -= StrPenalty;  (guard for null)
        //   public override bool OnStack(Effect incoming) -> refresh Duration to the
        //       longer of the two and return true (so the penalty is NOT applied twice).
    }
}
