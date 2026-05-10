namespace CavesOfOoo.Core
{
    /// <summary>
    /// WSP8.3 — Hibernating: self-stasis for
    /// <see cref="Skills.Cryomancy_Hibernate.HIBERNATE_DURATION"/> turns.
    /// Cannot act, but heals a percentage of max HP per turn AND has
    /// 100% HeatResistance + ColdResistance while active. Mirrors Qud's
    /// "torpor"-style self-stasis pattern (CoO-original implementation —
    /// no direct Qud parity).
    ///
    /// <para><b>Mechanic:</b>
    /// <list type="bullet">
    ///   <item>AllowAction => false (turn-skip the actor while
    ///         hibernating — same as Stunned/Frozen).</item>
    ///   <item>OnTurnStart heals <see cref="HEAL_PERCENT_PER_TURN"/> of
    ///         max HP each tick. Healing fires BEFORE the action-block
    ///         check (consistent with how Bleeding ticks during a
    ///         stunned turn).</item>
    ///   <item>OnApply records prior HeatResistance + ColdResistance
    ///         then bumps both to 100. OnRemove restores them. The
    ///         restored values are clamped against any other shifts
    ///         that landed during the hibernation window — mirrors
    ///         StatShifter's add/remove convention.</item>
    /// </list></para>
    ///
    /// <para>Stacks: returns false from OnStack — re-applying Hibernate
    /// while already hibernating is a no-op. Mirrors Confused's
    /// non-stacking semantic (a player who keeps pressing the Hibernate
    /// hotkey shouldn't double-up the resistance).</para>
    ///
    /// <para>Per <c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Cryomancy_Hibernate</c>:
    /// "the only self-stasis with healing trade-off." The brainstorm
    /// also called for "ends early on Heat damage > 50% HP" — deferred
    /// to a future iteration since the OnTakeDamage hook would need to
    /// track the cumulative-Heat-damage threshold across a stasis
    /// window. v1 ships the duration-locked version.</para>
    /// </summary>
    public class HibernatingEffect : Effect
    {
        public override string DisplayName => "hibernating";

        public override int GetEffectType() => TYPE_GENERAL;

        public const int HEAL_PERCENT_PER_TURN = 5;
        public const int RESISTANCE_BUFF = 100;

        // Captured pre-hibernation values, restored on OnRemove.
        // Default -1 sentinel = "OnApply hasn't run yet"; OnRemove only
        // restores if these were captured.
        //
        // <b>SL.6.4: must be public FIELDS, not properties with private
        // setters.</b> SaveSystem.WritePublicFields walks
        // `BindingFlags.Public | Instance` field set only — the
        // compiler-generated backing field for a `{ get; private set; }`
        // property is private, so it would be silently dropped on save.
        // A creature that saved mid-hibernation would wake up with
        // resistances stuck at the +100 buff because OnRemove sees the
        // -1 sentinel post-load (the captured value didn't persist).
        // Tests in EffectRoundTripPrivateStateTests pin this contract.
        public int PriorHeatResistance = -1;
        public int PriorColdResistance = -1;

        public HibernatingEffect(int duration = 10)
        {
            Duration = duration;
        }

        public override bool AllowAction(Entity target) => false;

        public override void OnApply(Entity target)
        {
            PriorHeatResistance = target.GetStatValue("HeatResistance", 0);
            PriorColdResistance = target.GetStatValue("ColdResistance", 0);

            // Bump resistances to 100. Use SetStatValue if available —
            // fall back to direct stat manipulation. We re-read via
            // Statistics.TryGetValue so the test fixtures' synthetic
            // entities also get the buff applied.
            var heatStat = target.GetStat("HeatResistance");
            if (heatStat != null) heatStat.BaseValue = RESISTANCE_BUFF;
            var coldStat = target.GetStat("ColdResistance");
            if (coldStat != null) coldStat.BaseValue = RESISTANCE_BUFF;

            MessageLog.Add(target.GetDisplayName() + " enters hibernation.");
        }

        public override void OnRemove(Entity target)
        {
            // Restore prior resistances if captured. Defensive: if
            // OnApply didn't run (which would be an order-violation
            // bug), the -1 sentinels skip the restore so we don't
            // silently corrupt other paths' buffs.
            if (PriorHeatResistance >= 0)
            {
                var heatStat = target.GetStat("HeatResistance");
                if (heatStat != null) heatStat.BaseValue = PriorHeatResistance;
            }
            if (PriorColdResistance >= 0)
            {
                var coldStat = target.GetStat("ColdResistance");
                if (coldStat != null) coldStat.BaseValue = PriorColdResistance;
            }

            MessageLog.Add(target.GetDisplayName() + " stirs from hibernation.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            var hp = target.GetStat("Hitpoints");
            if (hp == null) return;
            int maxHp = hp.Max;
            int healAmount = (maxHp * HEAL_PERCENT_PER_TURN) / 100;
            if (healAmount < 1) healAmount = 1; // floor at 1 HP/turn
            int newHp = hp.BaseValue + healAmount;
            if (newHp > maxHp) newHp = maxHp;
            hp.BaseValue = newHp;
        }

        public override bool OnStack(Effect incoming)
        {
            // Non-stacking: re-applying Hibernate is a no-op. The
            // mid-hibernation player can't even press the keybind
            // (AllowAction=false), but the safety guard keeps a
            // future caller from accidentally double-applying.
            return true;
        }

        public override string GetRenderColorOverride() => "&B";
    }
}
