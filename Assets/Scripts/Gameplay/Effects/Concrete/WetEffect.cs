namespace CavesOfOoo.Core
{
    /// <summary>
    /// Wet: suppresses ignition when Moisture > 0.35.
    /// Moisture evaporates based on owner temperature each turn.
    /// Stacking adds moisture, capped at 1.0.
    /// </summary>
    public class WetEffect : Effect
    {
        public override string DisplayName => "wet";

        public float Moisture;

        public WetEffect(float moisture = 1.0f)
        {
            Moisture = moisture > 1.0f ? 1.0f : moisture;
            Duration = DURATION_INDEFINITE;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is drenched.");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " has dried off.");
        }

        public override void OnTurnEnd(Entity target)
        {
            // Evaporate based on temperature
            var thermal = target.GetPart<ThermalPart>();
            if (thermal != null && thermal.Temperature > 50f)
            {
                float evapRate = (thermal.Temperature - 50f) * 0.002f;
                Moisture = System.Math.Max(Moisture - evapRate, 0f);
            }
            else
            {
                // Slow natural evaporation
                Moisture = System.Math.Max(Moisture - 0.01f, 0f);
            }

            if (Moisture <= 0f)
                Duration = 0; // will be cleaned up by StatusEffectsPart
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is WetEffect wet)
            {
                Moisture += wet.Moisture;
                if (Moisture > 1.0f)
                    Moisture = 1.0f;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&B";
    }
}
