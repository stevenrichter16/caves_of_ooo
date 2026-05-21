namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.8e — gas-as-coat hybrid. CoO port of Qud's
    /// <c>XRL.World.Effects.CoatedInPlasma</c> (qud CoatedInPlasma.cs).
    /// Applied by <see cref="GasPlasmaPart"/> when a creature inhales/
    /// is enveloped by plasma gas; the coat OUTLASTS the cloud (the
    /// "gas-as-coat" pattern — like a liquid coat but delivered by a
    /// gas). Duration scales with the cloud's density at application.
    ///
    /// <para><b>Mechanic (Qud parity):</b>
    /// <list type="bullet">
    ///   <item>-100 HeatResistance / ColdResistance / ElectricResistance
    ///         while coated — you take roughly double elemental damage
    ///         (CoO resistance math: <c>(100 - resistance)/100</c>, so
    ///         -100 → 200% damage).</item>
    ///   <item>Burns off any liquid coat on apply (plasma vaporizes it).
    ///         Qud: <c>RemoveAllEffects&lt;LiquidCovered&gt;</c>.</item>
    ///   <item>Refresh-on-reapply takes the LARGER Duration (Qud
    ///         CoatedInPlasma.cs:71-73) — OPPOSITE of FungalInfection's
    ///         preserve-clock semantic. Walking into a denser plasma
    ///         cloud extends your coat.</item>
    /// </list></para>
    ///
    /// <para><b>Qud-divergence:</b> Qud also blocks temperature from
    /// returning to ambient (you stay hot) and quarters firefighting
    /// effectiveness. CoO has neither system; deferred. The portable
    /// feature is the triple-resistance penalty + liquid burn-off.</para>
    ///
    /// <para><b>Stat-shift capture pattern:</b> uses a
    /// <see cref="StatsApplied"/> bool flag (not a -1 sentinel like
    /// HibernatingEffect) because resistances can legitimately be
    /// negative — a sentinel would misfire on a creature with -1
    /// resistance. Public fields for save round-trip.</para>
    /// </summary>
    public class CoatedInPlasmaEffect : Effect
    {
        public override string DisplayName => "coated in plasma";

        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        public const int RESISTANCE_PENALTY = 100;

        public Entity Owner;

        // Capture-flag (NOT sentinel) so a creature with a genuinely
        // negative resistance still round-trips. Public for save.
        public bool StatsApplied;
        public int PriorHeatResistance;
        public int PriorColdResistance;
        public int PriorElectricResistance;

        public CoatedInPlasmaEffect(int duration = 1, Entity owner = null)
        {
            Duration = duration;
            Owner = owner;
        }

        public override void OnApply(Entity target)
        {
            if (target == null) return;

            // Plasma vaporizes any liquid coat (Qud:
            // RemoveAllEffects<LiquidCovered> in CoatedInPlasma.Apply).
            // RemoveEffect uses an indexed loop and we're inside Applied()
            // (the new effect is already in _effects), so removing a
            // DIFFERENT effect type here is iterator-safe.
            target.RemoveEffect<LiquidCoveredEffect>();

            ApplyStats(target);
            MessageLog.Add(target.GetDisplayName() + " is coated in searing plasma.");
        }

        public override void OnRemove(Entity target)
        {
            UnapplyStats(target);
            MessageLog.Add(target.GetDisplayName() + " sheds the plasma coat.");
        }

        public override bool OnStack(Effect incoming)
        {
            // Refresh-on-reapply: the LARGER Duration wins (Qud
            // CoatedInPlasma.cs:71-73) — walking into a denser plasma
            // cloud EXTENDS the coat. Crucially we do NOT re-apply the
            // resistance shift: the existing instance keeps its single
            // -100; only the clock updates. (StatusEffectsPart routes the
            // incoming here and discards it without calling its OnApply,
            // so the shift can never double to -200.)
            if (incoming != null && incoming.Duration > Duration)
                Duration = incoming.Duration;
            return true;
        }

        // Capture-then-shift. Uses the StatsApplied bool guard (NOT a -1
        // sentinel like HibernatingEffect) so a creature with a genuinely
        // negative prior resistance still round-trips on removal.
        private void ApplyStats(Entity target)
        {
            if (StatsApplied) return;
            var heat = target.GetStat("HeatResistance");
            var cold = target.GetStat("ColdResistance");
            var elec = target.GetStat("ElectricResistance");
            if (heat != null) { PriorHeatResistance = heat.BaseValue; heat.BaseValue -= RESISTANCE_PENALTY; }
            if (cold != null) { PriorColdResistance = cold.BaseValue; cold.BaseValue -= RESISTANCE_PENALTY; }
            if (elec != null) { PriorElectricResistance = elec.BaseValue; elec.BaseValue -= RESISTANCE_PENALTY; }
            StatsApplied = true;
        }

        private void UnapplyStats(Entity target)
        {
            if (!StatsApplied) return;
            if (target != null)
            {
                var heat = target.GetStat("HeatResistance");
                var cold = target.GetStat("ColdResistance");
                var elec = target.GetStat("ElectricResistance");
                if (heat != null) heat.BaseValue = PriorHeatResistance;
                if (cold != null) cold.BaseValue = PriorColdResistance;
                if (elec != null) elec.BaseValue = PriorElectricResistance;
            }
            StatsApplied = false;
        }
    }
}
