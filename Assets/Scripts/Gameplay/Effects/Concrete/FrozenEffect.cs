namespace CavesOfOoo.Core
{
    /// <summary>
    /// Frozen: cold-based counterpart to BurningEffect. Blocks action when deeply frozen,
    /// extinguishes any active burning on apply, and shatters brittle materials under
    /// freeze shock. Thaws over time based on the owner's ThermalPart.Temperature.
    /// </summary>
    public class FrozenEffect : Effect
    {
        public override string DisplayName => "frozen";

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

        public override void OnApply(Entity target)
        {
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
            // Thaw based on ambient warmth. Above freezing the ice retreats;
            // below freezing it holds.
            var thermal = target.GetPart<ThermalPart>();
            if (thermal != null && thermal.Temperature > thermal.FreezeTemperature)
            {
                float thawRate = (thermal.Temperature - thermal.FreezeTemperature) * 0.0002f + 0.02f;
                Cold = System.Math.Max(Cold - thawRate, 0f);
            }

            if (Cold <= 0f)
                Duration = 0;
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

        public override string GetRenderColorOverride() => "&C";
    }
}
