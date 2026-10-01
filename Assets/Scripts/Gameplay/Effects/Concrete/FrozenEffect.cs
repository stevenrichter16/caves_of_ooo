using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Frozen: cold-based counterpart to BurningEffect. Blocks action while any
    /// <see cref="Cold"/> remains, extinguishes any active burning on apply, and
    /// shatters brittle materials under freeze shock.
    ///
    /// <para><b>Thaw follows magnitude</b> (Docs/FREEZE-THAW.md). Cold is the
    /// magnitude of the freeze and it drains at <see cref="THAW_PER_TURN"/> a
    /// turn, so a freeze of 0.5 lasts ~5 turns and 1.0 lasts ~10. The old rule
    /// only thawed while the owner's body was ABOVE freezing, which a cold
    /// dose made false for ~65 turns, turning any cheap chill into a lock.
    /// Warmth still helps and <b>fire thaws</b>: heat doses, ignition and fire
    /// damage each remove Cold (see the <c>THAW_PER_*</c> constants). Applies
    /// to creatures and objects alike.</para>
    /// </summary>
    public class FrozenEffect : Effect
    {
        public override string DisplayName => "frozen";

        // WSP6.16 — TYPE_NEGATIVE backfill (see AcidicEffect.cs).
        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        /// <summary>
        /// 0..1. Any positive value locks the owner out of action. The
        /// field slowly thaws toward 0 in <see cref="OnTurnEnd"/>, and
        /// when it reaches 0 the effect removes itself via
        /// <c>Duration = 0</c>. "Partially frozen" is not a playable
        /// state — the owner is either acting or not.
        /// </summary>
        public float Cold;

        public FrozenEffect(float cold = 1.0f)
        {
            Cold = cold > 1.0f ? 1.0f : (cold < 0f ? 0f : cold);
            Duration = DURATION_INDEFINITE;
        }

        // ── Thaw model (all in Cold units, 0..1) ──────────────────────────

        /// <summary>Cold lost every turn regardless of temperature. 0.10 means
        /// a magnitude-1.0 freeze lasts 10 turns.</summary>
        public const float THAW_PER_TURN = 0.10f;

        /// <summary>Extra Cold lost per turn per degree the owner is ABOVE
        /// freezing (the pre-existing coefficient, now additive).</summary>
        public const float WARMTH_THAW_PER_DEGREE = 0.0002f;

        /// <summary>Cold removed per degree of temperature a heat dose raises
        /// (joules / heat capacity). A 300 J attack on a 1.6-capacity body is
        /// ~187 degrees: ~0.75 of thaw.</summary>
        public const float THAW_PER_DEGREE_OF_HEAT = 0.004f;

        /// <summary>Cold removed per point of Fire/Heat damage taken.</summary>
        public const float THAW_PER_FIRE_DAMAGE = 0.06f;

        /// <summary>Cold removed when a flame takes hold, per point of
        /// BurningEffect intensity. The flame still ignites (see BurningEffect.Apply).</summary>
        public const float THAW_PER_BURN_INTENSITY = 0.5f;

        /// <summary>Smallest freeze a cold dose produces (crossing the freezing
        /// point at all).</summary>
        public const float FREEZE_FLOOR = 0.10f;

        /// <summary>Extra Cold per degree a dose drives the body BELOW
        /// freezing: 1/600, so a -150 J Quench on flesh (~69 degrees under)
        /// is a ~0.21 freeze and a 540-degree plunge saturates at 1.0.</summary>
        public const float FREEZE_PER_DEGREE_BELOW = 1f / 600f;

        /// <summary>Freeze magnitude for a body driven <paramref name="degreesBelowFreezing"/>
        /// past its freezing point. Floored at <see cref="FREEZE_FLOOR"/>, capped at 1.</summary>
        public static float ColdForDepth(float degreesBelowFreezing)
        {
            float depth = degreesBelowFreezing > 0f ? degreesBelowFreezing : 0f;
            float cold = FREEZE_FLOOR + depth * FREEZE_PER_DEGREE_BELOW;
            return cold > 1.0f ? 1.0f : cold;
        }

        /// <summary>Turns until the ice is gone at the base rate (warmth and
        /// fire only shorten it). For the player-facing description.</summary>
        public int TurnsToThaw => Cold <= 0f ? 0 : (int)System.Math.Ceiling(Cold / THAW_PER_TURN - 1e-4f);

        /// <summary>
        /// Remove <paramref name="amount"/> of Cold. At zero the effect is removed
        /// immediately ("X thaws."). Returns the Cold that remains. <paramref name="cause"/>
        /// is "time" for the per-turn drain (not recorded) or what thawed it
        /// ("heat", "fire", "ignition"), which emits an <c>effect/Thawed</c> record.
        /// </summary>
        public float Thaw(float amount, string cause)
        {
            if (amount <= 0f || Cold <= 0f) return Cold;

            float before = Cold;
            Cold -= amount;
            if (Cold < 0.0001f) Cold = 0f;

            if (cause != "time" && Diag.IsChannelEnabled("effect"))
            {
                Diag.Record(
                    category: "effect",
                    kind: "Thawed",
                    target: Owner,
                    payload: new { cause = cause, amount = amount, coldBefore = before, coldAfter = Cold });
            }

            if (Cold <= 0f)
            {
                Duration = 0;
                // Immediate when attached; a time-tick leaves removal to the
                // EndTurn sweep, which also catches Duration == 0.
                if (cause != "time") Owner?.GetPart<StatusEffectsPart>()?.RemoveEffect(this);
            }
            return Cold;
        }

        /// <summary>Moisture above which water deepens the freeze.
        /// Deliberately the same threshold ElectrifiedEffect uses for
        /// its charge doubling — one number for "wet enough to matter",
        /// so the player learns a single rule instead of two.</summary>
        public const float WET_AMPLIFY_THRESHOLD = 0.2f;

        /// <summary>Multiplier applied to <see cref="Cold"/> on a soaked
        /// target. Lower than Electrified's ×2 because Cold is capped at
        /// 1.0 and any positive value already locks the target out — the
        /// gain here is DURATION (it thaws from higher), not a stronger
        /// state.</summary>
        public const float WET_AMPLIFY_FACTOR = 1.5f;

        public override void OnApply(Entity target)
        {
            // SYMMETRY (SPELLCRAFT SM6). ElectrifiedEffect.OnApply
            // doubles its Charge on a wet target, and CryomancySkill's
            // own bonus already keys on Wet — but Frozen itself did not
            // amplify, so soaking made a target better to shock and no
            // better to freeze, for no reason a player could infer.
            // Water freezes. The gain is duration: Cold is a 0..1 field
            // where any positive value already blocks action, so a
            // deeper freeze thaws from higher rather than biting harder.
            var wet = target.GetEffect<WetEffect>();
            if (wet != null && wet.Moisture > WET_AMPLIFY_THRESHOLD)
            {
                Cold *= WET_AMPLIFY_FACTOR;
                if (Cold > 1.0f) Cold = 1.0f;
            }

            MessageLog.Add(target.GetDisplayName() + " is frozen!");

            // Cold defeats fire, symmetric to how fire defeats wet.
            if (target.HasEffect<BurningEffect>())
            {
                target.RemoveEffect<BurningEffect>();
                target.FireEvent("Extinguished");
            }

            // Freeze shock on brittle materials.
            var material = target.GetPart<MaterialPart>();
            var thermal = target.GetPart<ThermalPart>();
            if (material != null && thermal != null
                && material.Brittleness > 0.5f
                && thermal.Temperature <= thermal.BrittleTemperature)
            {
                var shatter = GameEvent.New("TryShatter");
                shatter.SetParameter("Cause", "Freeze");
                target.FireEvent(shatter);
                shatter.Release();
            }
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " thaws.");
        }

        public override void OnTurnEnd(Entity target)
        {
            // Thaw by magnitude: a fixed drain every turn, whatever the body
            // temperature, plus a little more when the body is above freezing.
            float rate = THAW_PER_TURN;
            var thermal = target.GetPart<ThermalPart>();
            if (thermal != null && thermal.Temperature > thermal.FreezeTemperature)
                rate += (thermal.Temperature - thermal.FreezeTemperature) * WARMTH_THAW_PER_DEGREE;

            Thaw(rate, "time");
        }

        /// <summary>Fire and heat damage melt ice in proportion to the damage.
        /// Other damage (a blade, a cold blast) does not.</summary>
        public override void OnTakeDamage(Entity target, GameEvent e)
        {
            var damage = e?.GetParameter<Damage>("Damage");
            if (damage == null || damage.Amount <= 0 || !damage.IsHeatDamage()) return;
            Thaw(damage.Amount * THAW_PER_FIRE_DAMAGE, "fire");
        }

        // Block ALL action while the effect is present (Cold > 0). The
        // old <= 0.5 threshold created a confusing window where the log
        // said "X is frozen and cannot act!" on the skipped turn while
        // ProcessUntilPlayerTurn thawed Cold below 0.5 in the same
        // Unity frame, so by the next input frame the player could move
        // despite the log message. The effect self-expires at Cold == 0
        // (OnTurnEnd sets Duration = 0), so "effect present" === "frozen".
        public override bool AllowAction(Entity target) => Cold <= 0f;

        public override bool OnStack(Effect incoming)
        {
            if (incoming is FrozenEffect frozen)
            {
                Cold += frozen.Cold * 0.5f;
                if (Cold > 1.0f)
                    Cold = 1.0f;

                // Re-run the extinguish side-effect on stack: a target that caught fire
                // after the initial freeze should still be put out by the new cold pulse.
                if (Owner != null && Owner.HasEffect<BurningEffect>())
                {
                    Owner.RemoveEffect<BurningEffect>();
                    Owner.FireEvent("Extinguished");
                }
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&*C"; // HDR — see GRAPHICS.md §3.B.3
    }
}
