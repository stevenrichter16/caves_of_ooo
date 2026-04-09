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

            bool wasBelow = Temperature < FlameTemperature;

            if (radiant)
            {
                // Qud-style radiant: asymptotic approach
                float factor = 0.035f / (HeatCapacity > 0f ? HeatCapacity : 1f);
                Temperature += (joules - Temperature) * factor;
            }
            else
            {
                // Direct heat: linear addition scaled by capacity
                Temperature += joules / (HeatCapacity > 0f ? HeatCapacity : 1f);
            }

            // Check for ignition threshold crossing
            if (wasBelow && Temperature >= FlameTemperature)
                TryIgnite(e);

            // Check for extinguish: cooling dropped temperature below FlameTemperature
            if (!wasBelow && Temperature < FlameTemperature)
                TryExtinguish();

            return true;
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

            // Apply BurningEffect if not already burning
            if (!ParentEntity.HasEffect<BurningEffect>())
            {
                Entity source = sourceEvent.GetParameter<Entity>("Source");
                var zone = sourceEvent.GetParameter<Zone>("Zone");
                ParentEntity.ApplyEffect(new BurningEffect(intensity: 1.0f, source: source), source, zone);
            }
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
            if (Temperature < FlameTemperature && ParentEntity.HasEffect<BurningEffect>())
            {
                ParentEntity.RemoveEffect<BurningEffect>();
                ParentEntity.FireEvent("Extinguished");
            }

            return true;
        }
    }
}
