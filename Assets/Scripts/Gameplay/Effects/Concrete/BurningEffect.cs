namespace CavesOfOoo.Core
{
    /// <summary>
    /// Burning: intensity-based fire effect with fuel consumption, heat emission,
    /// and spatial propagation. Replaces the simple duration+damageDice version.
    ///
    /// When the owner has a FuelPart, duration is indefinite (fuel-controlled).
    /// Without FuelPart, falls back to a calculated duration from intensity.
    /// Implements IAuraProvider for the generalized aura system.
    /// </summary>
    public class BurningEffect : Effect, IAuraProvider
    {
        public override string DisplayName => "burning";

        public float Intensity;
        public Entity IgnitionSource;
        public System.Random Rng;

        // Damage tiers by intensity (min damage, max damage)
        private static readonly int[,] DamageTiers =
        {
            { 1, 2 },  // 0.0 - 0.99
            { 1, 4 },  // 1.0 - 1.99
            { 2, 6 },  // 2.0 - 2.99
            { 3, 8 },  // 3.0 - 3.99
            { 4, 10 }, // 4.0 - 4.99
            { 5, 12 }, // 5.0+
        };

        public BurningEffect(float intensity = 1.0f, Entity source = null, System.Random rng = null)
        {
            Intensity = intensity;
            IgnitionSource = source;
            Rng = rng ?? new System.Random();
            // Duration set in OnApply based on FuelPart presence
            Duration = DURATION_INDEFINITE;
        }

        public override void OnApply(Entity target)
        {
            // If no FuelPart, use fallback duration based on intensity
            var fuel = target.GetPart<FuelPart>();
            if (fuel == null)
                Duration = System.Math.Max(1, (int)System.Math.Ceiling(Intensity * 3));

            MessageLog.Add(target.GetDisplayName() + " catches fire!");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is no longer burning.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            Zone zone = context?.GetParameter<Zone>("Zone");

            // 1. Consume fuel if available
            var fuel = target.GetPart<FuelPart>();
            if (fuel != null)
            {
                var consumeFuel = GameEvent.New("ConsumeFuel");
                consumeFuel.SetParameter("Intensity", (object)Intensity);
                target.FireEvent(consumeFuel);

                bool exhausted = consumeFuel.GetParameter<bool>("Exhausted");
                string exhaustProduct = consumeFuel.GetStringParameter("ExhaustProduct");
                consumeFuel.Release();

                if (exhausted)
                {
                    // Fuel is gone — transition to smoldering
                    target.ApplyEffect(new SmolderingEffect(), null, zone);
                    target.ApplyEffect(new CharredEffect(), null, zone);

                    // Notify for exhaust product spawning (e.g., AshPile)
                    if (!string.IsNullOrEmpty(exhaustProduct))
                    {
                        var spawnEvent = GameEvent.New("FuelExhausted");
                        spawnEvent.SetParameter("ExhaustProduct", exhaustProduct);
                        spawnEvent.SetParameter("Zone", (object)zone);
                        target.FireEvent(spawnEvent);
                        spawnEvent.Release();
                    }

                    Duration = 0; // mark for removal
                    return;
                }
            }

            // 2. Deal damage based on intensity tier
            int damage = RollDamage();
            if (damage > 0)
            {
                CombatSystem.ApplyDamage(target, damage, IgnitionSource, zone);
                MessageLog.Add(target.GetDisplayName() + " takes " + damage + " fire damage.");
            }

            // 3. Emit heat to self (small reinforcement keeping temperature up)
            var thermal = target.GetPart<ThermalPart>();
            if (thermal != null)
            {
                var selfHeat = GameEvent.New("ApplyHeat");
                selfHeat.SetParameter("Joules", (object)(Intensity * 20f));
                selfHeat.SetParameter("Radiant", (object)false);
                selfHeat.SetParameter("Source", (object)target);
                target.FireEvent(selfHeat);
                selfHeat.Release();
            }

            // 4. Evaluate material reactions (data-driven bonuses)
            MaterialReactionResolver.EvaluateReactions(target, zone, this);

            // 5. Emit heat to adjacent cells for spatial propagation
            if (zone != null)
                MaterialSimSystem.EmitHeatToAdjacent(target, zone, Intensity * 30f);
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is BurningEffect burn)
            {
                Intensity = System.Math.Min(Intensity + burn.Intensity * 0.5f, 5.0f);
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&R";

        public AsciiFxTheme GetAuraTheme() => AsciiFxTheme.Fire;

        private int RollDamage()
        {
            int tier = (int)Intensity;
            if (tier < 0) tier = 0;
            if (tier >= DamageTiers.GetLength(0)) tier = DamageTiers.GetLength(0) - 1;

            int min = DamageTiers[tier, 0];
            int max = DamageTiers[tier, 1];
            return Rng.Next(min, max + 1);
        }
    }
}
