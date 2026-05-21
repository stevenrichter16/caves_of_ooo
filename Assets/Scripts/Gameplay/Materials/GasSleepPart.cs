namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.8c — sleep gas behavior. Applies <see cref="AsleepByGasEffect"/>
    /// to inhaling creatures via the shared filter chain in
    /// <see cref="IObjectGasBehaviorPart"/>. Same shape as
    /// <see cref="GasStunPart"/> / <see cref="GasConfusionPart"/>
    /// (refresh-on-reapply, no immediate damage), with the new effect
    /// providing the "wake-on-damage" twist.
    /// </summary>
    public class GasSleepPart : IObjectGasBehaviorPart
    {
        public override string Name => "GasSleep";

        /// <summary>Per-level sleep duration in turns. Level 1 → 3 turns,
        /// Level 3 → 9 turns. Longer than stun (sleep is less
        /// "interruptible by environment" than stun's KO).</summary>
        public const int DURATION_PER_LEVEL = 3;

        public const int BASE_INTAKE = 100;

        public override bool ApplyGas(Entity target, Zone zone)
        {
            int intake = RunFilterChain(target, BASE_INTAKE);
            if (intake < 0) return false;

            int duration = BaseGas.Level * DURATION_PER_LEVEL;
            target.GetPart<StatusEffectsPart>()?.RemoveEffect<AsleepByGasEffect>();
            target.ApplyEffect(new AsleepByGasEffect(duration: duration), BaseGas.Creator, zone);

            Diagnostics.Diag.Record("gas", "Applied", BaseGas.Creator, target,
                new { gasId = BaseGas.GasId, gasType = BaseGas.GasType,
                      gasLevel = BaseGas.Level, intake, effectDuration = duration });
            return true;
        }
    }
}
