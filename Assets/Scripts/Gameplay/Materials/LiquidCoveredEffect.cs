namespace CavesOfOoo.Core
{
    /// <summary>
    /// LQ.4 — the on-creature liquid coating (closes plan gap (b): a
    /// creature stepping into a <see cref="LiquidPoolPart"/> cell now
    /// actually carries the liquid). Mirrors Qud's
    /// <c>XRL.World.Effects.LiquidCovered</c> reduced to CoO's phase-1
    /// single-liquid scalar model.
    ///
    /// <para><b>Divergence #1 (no parts-per-1000 mixing).</b> A second,
    /// different liquid merging via <see cref="OnStack"/> does NOT blend
    /// into a ratio. Instead the larger contact pool wins the surface id
    /// (the dominant liquid is what reacts downstream in LQ.5+); the
    /// scalar amounts add. Same-liquid re-entry just accumulates.</para>
    ///
    /// <para><b>Divergence #3 (water also keeps you wet).</b> A water
    /// coat additionally ensures a <see cref="WetEffect"/> so the
    /// pre-existing wet→electric coupling in
    /// <see cref="ElectrifiedEffect"/> — and the pinned
    /// ElectrifiedEffectDamageTests — keep working untouched. No other
    /// liquid applies WetEffect.</para>
    ///
    /// <para><b>Divergence #5 (once-on-enter + persistent dry-down).</b>
    /// The coat is applied once when the creature steps into the cell
    /// (the <c>EntityEnteredCell</c> dispatch in
    /// <see cref="LiquidPoolPart.HandleEvent"/>); standing still does not
    /// re-coat. It then dries down each end-of-turn by the liquid's
    /// Fluidity+Evaporativity (heat-accelerated, mirroring
    /// <see cref="WetEffect.OnTurnEnd"/>) and removes itself at zero.</para>
    ///
    /// <para><b>Flyweight contract (LQ.3 review finding F3).</b>
    /// <see cref="LiquidRegistry.Get"/> returns the shared mutable
    /// <see cref="LiquidDefinition"/>. This effect only ever reads
    /// scalars out of it — never holds-and-mutates.</para>
    ///
    /// <para>Fields are plain public so the effect round-trips through
    /// the reflection save path with no <c>FormatVersion</c> bump
    /// (plan §A5), and the optional-arg ctor doubles as the
    /// parameterless ctor reflection deserialization needs.</para>
    /// </summary>
    public class LiquidCoveredEffect : Effect
    {
        /// <summary>Registry id of the dominant liquid on the
        /// creature (e.g. "water", "oil", "acid").</summary>
        public string LiquidId;

        /// <summary>Scalar contact amount. Drives dry-down and the
        /// downstream consequence hooks (LQ.5+). Clamped to ≥ 0.</summary>
        public int Amount;

        public LiquidCoveredEffect(string liquidId = "", int amount = 0)
        {
            LiquidId = liquidId ?? "";
            Amount = amount < 0 ? 0 : amount;
            Duration = DURATION_INDEFINITE;
        }

        public override int GetEffectType() => TYPE_CONTACT | TYPE_REMOVABLE;

        /// <summary>
        /// Pulled live from the liquid definition's
        /// <see cref="LiquidDefinition.Adjective"/> so a new liquid only
        /// needs a JSON row. Falls back to a generic label when the
        /// registry is uninitialized or the id is unknown.
        /// </summary>
        public override string DisplayName
        {
            get
            {
                if (LiquidRegistry.IsInitialized)
                {
                    var def = LiquidRegistry.Get(LiquidId);
                    if (def != null && !string.IsNullOrEmpty(def.Adjective))
                        return def.Adjective;
                }
                return "liquid-covered";
            }
        }

        public override void OnApply(Entity target)
        {
            if (target != null)
                MessageLog.Add(target.GetDisplayName() + " is covered in " + DisplayName + ".");
            RefreshWaterCoupling(target);
        }

        public override void OnRemove(Entity target)
        {
            if (target != null)
                MessageLog.Add("The " + DisplayName + " coating wears off " + target.GetDisplayName() + ".");
        }

        /// <summary>
        /// Divergence #1: same-liquid re-entry accumulates; a
        /// different liquid with a larger contact amount takes over the
        /// surface id, and the amounts add either way. Always returns
        /// true so <see cref="StatusEffectsPart"/> never stacks a second
        /// instance (merge-not-stack — pinned by
        /// LiquidCoatingTests.ReEnterPool_Merges_NotStacks).
        /// </summary>
        public override bool OnStack(Effect incoming)
        {
            if (incoming is LiquidCoveredEffect other)
            {
                if (other.LiquidId != LiquidId && other.Amount > Amount)
                    LiquidId = other.LiquidId;
                Amount += other.Amount;
                RefreshWaterCoupling(Owner);
                return true;
            }
            return false;
        }

        public override void OnTurnEnd(Entity target)
        {
            int dry = 1;
            if (LiquidRegistry.IsInitialized)
            {
                var def = LiquidRegistry.Get(LiquidId);
                if (def != null)
                {
                    dry = def.Fluidity + def.Evaporativity;
                    if (dry < 1) dry = 1; // never-zero so a coat always dries
                }
            }

            // Heat accelerates dry-down, mirroring WetEffect's
            // temperature coupling (WetEffect.OnTurnEnd:49-54).
            var thermal = target?.GetPart<ThermalPart>();
            if (thermal != null && thermal.Temperature > 50f)
                dry += (int)((thermal.Temperature - 50f) * 0.05f);

            Amount -= dry;
            if (Amount <= 0)
            {
                Amount = 0;
                Duration = 0; // StatusEffectsPart.HandleEndTurn cleans up
            }
        }

        /// <summary>
        /// Divergence #3: a water coat ALSO ensures a
        /// <see cref="WetEffect"/> (full moisture) so the existing
        /// wet→electric amplification in <see cref="ElectrifiedEffect"/>
        /// keeps working. Non-water liquids never apply WetEffect.
        /// Null-safe: a stack-merge on an orphaned effect (no Owner) is
        /// a no-op rather than a crash.
        /// </summary>
        private void RefreshWaterCoupling(Entity target)
        {
            if (target == null) return;
            if (LiquidId != "water") return;
            target.ApplyEffect(new WetEffect(1.0f), null, null);
        }
    }
}
