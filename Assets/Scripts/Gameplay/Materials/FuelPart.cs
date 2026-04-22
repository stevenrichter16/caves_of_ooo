namespace CavesOfOoo.Core
{
    /// <summary>
    /// Tracks combustible fuel mass for an entity.
    /// BurningEffect fires "ConsumeFuel" each turn to burn fuel
    /// and determine heat output.
    /// </summary>
    public class FuelPart : Part
    {
        public override string Name => "Fuel";

        // Blueprint-configurable fields
        public float FuelMass = 100f;
        public float MaxFuel = 100f;
        public float BurnRate = 1.0f;
        public float HeatOutput = 1.0f;
        public string ExhaustProduct = "";

        public bool IsExhausted => FuelMass <= 0f;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ConsumeFuel")
                return HandleConsumeFuel(e);

            return true;
        }

        private bool HandleConsumeFuel(GameEvent e)
        {
            float intensity = e.GetParameter<float>("Intensity");
            if (intensity <= 0f) intensity = 1.0f;
            float amount = BurnRate * intensity;

            if (amount > FuelMass)
                amount = FuelMass;

            FuelMass -= amount;
            float heatProduced = amount * HeatOutput;

            e.SetParameter("HeatProduced", (object)heatProduced);
            e.SetParameter("Exhausted", (object)IsExhausted);
            e.SetParameter("ExhaustProduct", ExhaustProduct ?? "");

            return true;
        }
    }
}
