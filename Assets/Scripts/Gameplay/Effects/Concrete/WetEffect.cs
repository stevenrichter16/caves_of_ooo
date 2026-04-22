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
            // Porous materials (cloth, sponge) absorb more moisture than their
            // nominal input: scale the incoming moisture by (1 + Porosity).
            var material = target.GetPart<MaterialPart>();
            if (material != null && material.Porosity > 0f)
            {
                Moisture *= (1f + material.Porosity);
                if (Moisture > 1.0f)
                    Moisture = 1.0f;
            }
            MessageLog.Add(target.GetDisplayName() + " is drenched.");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " has dried off.");
        }

        public override void OnTurnEnd(Entity target)
        {
            // Porous materials hold moisture longer. Scale evaporation by
            // (1 - Porosity * 0.5). Porosity 1.0 → half the evap rate.
            var material = target.GetPart<MaterialPart>();
            float porosityScale = 1f;
            if (material != null && material.Porosity > 0f)
                porosityScale = 1f - (material.Porosity * 0.5f);

            // Evaporate based on temperature
            var thermal = target.GetPart<ThermalPart>();
            if (thermal != null && thermal.Temperature > 50f)
            {
                float evapRate = (thermal.Temperature - 50f) * 0.002f * porosityScale;
                Moisture = System.Math.Max(Moisture - evapRate, 0f);
            }
            else
            {
                // Slow natural evaporation
                Moisture = System.Math.Max(Moisture - 0.01f * porosityScale, 0f);
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
