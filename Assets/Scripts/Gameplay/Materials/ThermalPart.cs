namespace CavesOfOoo.Core
{
    /// <summary>
    /// Tracks continuous temperature for an entity.
    /// Handles heat application (radiant and direct modes from Qud)
    /// and triggers ignition when temperature crosses FlameTemperature.
    /// </summary>
    public class ThermalPart : Part
    {
        public override string Name => "Thermal";

        // Blueprint-configurable fields
        public float Temperature = 25f;
        public float FlameTemperature = 400f;
        public float VaporTemperature = 10000f;
        public float FreezeTemperature = 0f;
        public float BrittleTemperature = -100f;
        public float HeatCapacity = 1.0f;
        public float AmbientDecayRate = 0.02f;
        public float AmbientTemperature = 25f;

        public bool IsAflame => Temperature >= FlameTemperature;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ApplyHeat")
                return HandleApplyHeat(e);

            if (e.ID == "EndTurn")
                return HandleEndTurn(e);

            return true;
        }

        private bool HandleApplyHeat(GameEvent e)
        {
            float joules = e.GetParameter<float>("Joules");
            bool radiant = e.GetParameter<bool>("Radiant");

            if (joules == 0f)
                return true;

            // Snapshot pre-change thresholds so we can detect crossings in both directions.
            float previous = Temperature;
            float effectiveFlame = GetEffectiveFlameTemperature();
            bool wasBelowFlame = previous < effectiveFlame;
            bool wasAboveFreeze = previous > FreezeTemperature;
            bool wasBelowVapor = previous < VaporTemperature;

            float delta;
            if (radiant)
            {
                // Qud-style radiant: asymptotic approach
                float factor = 0.035f / (HeatCapacity > 0f ? HeatCapacity : 1f);
                delta = (joules - Temperature) * factor;
            }
            else
            {
                // Direct heat: linear addition scaled by capacity
                delta = joules / (HeatCapacity > 0f ? HeatCapacity : 1f);
            }

            Temperature += delta;

            // Check for ignition threshold crossing (volatility lowers the bar).
            if (wasBelowFlame && Temperature >= effectiveFlame)
                TryIgnite(e);

            // Check for extinguish: cooling dropped temperature below FlameTemperature
            if (!wasBelowFlame && Temperature < effectiveFlame)
                TryExtinguish();

            // Cold crossing: warm → at/below freezing.
            if (wasAboveFreeze && Temperature <= FreezeTemperature)
                TryFreeze(e);

            // Vapor crossing: sub-vapor → at/above VaporTemperature.
            if (wasBelowVapor && Temperature >= VaporTemperature)
                TryVaporize(e);

            // Thermal shock: brittle materials crack under a large single-tick delta.
            if (System.Math.Abs(delta) > 200f)
                TryShatter(e, "ThermalShock");

            return true;
        }

        /// <summary>
        /// Returns the ignition threshold adjusted by MaterialPart.Volatility.
        /// Oil, alcohol, and other volatile materials ignite at a lower temperature
        /// and start with a hotter BurningEffect.
        /// </summary>
        private float GetEffectiveFlameTemperature()
        {
            if (ParentEntity == null)
                return FlameTemperature;
            var material = ParentEntity.GetPart<MaterialPart>();
            if (material == null || material.Volatility <= 0f)
                return FlameTemperature;
            return FlameTemperature - (material.Volatility * 100f);
        }

        private void TryIgnite(GameEvent sourceEvent)
        {
            if (ParentEntity == null)
                return;

            // Check WetEffect suppression
            var wet = ParentEntity.GetEffect<WetEffect>();
            if (wet != null && wet.Moisture > 0.35f)
            {
                // Wet suppresses ignition; evaporate some moisture instead
                wet.Moisture -= 0.1f;
                return;
            }

            // Fire TryIgnite event — MaterialPart can veto if Combustibility == 0
            var tryIgnite = GameEvent.New("TryIgnite");
            tryIgnite.SetParameter("Source", sourceEvent.GetParameter("Source"));
            bool allowed = ParentEntity.FireEvent(tryIgnite);
            bool cancelled = tryIgnite.GetParameter<bool>("Cancelled");
            tryIgnite.Release();

            if (!allowed || cancelled)
                return;

            // Apply BurningEffect if not already burning. Volatile materials start hotter.
            if (!ParentEntity.HasEffect<BurningEffect>())
            {
                Entity source = sourceEvent.GetParameter<Entity>("Source");
                var zone = sourceEvent.GetParameter<Zone>("Zone");
                float startIntensity = 1.0f;
                var material = ParentEntity.GetPart<MaterialPart>();
                if (material != null && material.Volatility > 0f)
                    startIntensity += material.Volatility;
                ParentEntity.ApplyEffect(new BurningEffect(intensity: startIntensity, source: source), source, zone);
            }
        }

        private void TryFreeze(GameEvent sourceEvent)
        {
            if (ParentEntity == null)
                return;

            // Allow other parts to veto the freeze.
            var tryFreeze = GameEvent.New("TryFreeze");
            tryFreeze.SetParameter("Source", sourceEvent.GetParameter("Source"));
            bool allowed = ParentEntity.FireEvent(tryFreeze);
            bool cancelled = tryFreeze.GetParameter<bool>("Cancelled");
            tryFreeze.Release();

            if (!allowed || cancelled)
                return;

            if (!ParentEntity.HasEffect<FrozenEffect>())
            {
                Entity source = sourceEvent.GetParameter<Entity>("Source");
                var zone = sourceEvent.GetParameter<Zone>("Zone");
                ParentEntity.ApplyEffect(new FrozenEffect(cold: 1.0f), source, zone);
            }
        }

        private void TryVaporize(GameEvent sourceEvent)
        {
            if (ParentEntity == null)
                return;

            // Let parts veto vaporization (e.g. inert high-boil materials).
            var tryVaporize = GameEvent.New("TryVaporize");
            tryVaporize.SetParameter("Source", sourceEvent.GetParameter("Source"));
            bool allowed = ParentEntity.FireEvent(tryVaporize);
            bool cancelled = tryVaporize.GetParameter<bool>("Cancelled");
            tryVaporize.Release();

            if (!allowed || cancelled)
                return;

            // Strip moisture — the water has boiled off.
            if (ParentEntity.HasEffect<WetEffect>())
                ParentEntity.RemoveEffect<WetEffect>();

            // Mark the entity for steam handling upstream (material reactions pick this up).
            ParentEntity.FireEvent("Vaporized");
        }

        private void TryShatter(GameEvent sourceEvent, string cause)
        {
            if (ParentEntity == null)
                return;

            var tryShatter = GameEvent.New("TryShatter");
            tryShatter.SetParameter("Cause", cause);
            tryShatter.SetParameter("Source", sourceEvent.GetParameter("Source"));
            ParentEntity.FireEvent(tryShatter);
            tryShatter.Release();
        }

        private void TryExtinguish()
        {
            if (ParentEntity == null)
                return;

            if (ParentEntity.HasEffect<BurningEffect>())
            {
                ParentEntity.RemoveEffect<BurningEffect>();
                ParentEntity.FireEvent("Extinguished");
            }
        }

        private bool HandleEndTurn(GameEvent e)
        {
            // Decay toward ambient temperature
            if (Temperature != AmbientTemperature)
            {
                float diff = Temperature - AmbientTemperature;
                Temperature -= diff * AmbientDecayRate;

                // Snap to ambient if close enough
                if (System.Math.Abs(Temperature - AmbientTemperature) < 0.5f)
                    Temperature = AmbientTemperature;
            }

            // Check if fire has gone out — remove the BurningEffect directly
            if (Temperature < GetEffectiveFlameTemperature() && ParentEntity.HasEffect<BurningEffect>())
            {
                ParentEntity.RemoveEffect<BurningEffect>();
                ParentEntity.FireEvent("Extinguished");
            }

            return true;
        }
    }
}
