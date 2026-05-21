using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.8a — confusion-gas behavior. Applies the existing
    /// <see cref="ConfusedEffect"/> to creatures inhaling the gas, via
    /// the shared filter chain in <see cref="IObjectGasBehaviorPart"/>.
    /// Mirrors Qud <c>XRL.World.Parts.GasConfusion</c>.
    ///
    /// <para>Same shape as <see cref="GasStunPart"/>: no immediate
    /// damage (confusion is a behavior-modifier, not exposure damage),
    /// refresh-on-reapply, Duration scales with GasLevel.</para>
    /// </summary>
    public class GasConfusionPart : IObjectGasBehaviorPart
    {
        public override string Name => "GasConfusion";

        /// <summary>Per-level confusion duration in turns. ConfusedEffect's
        /// default duration is 4; we scale by GasLevel × 4 so a Level 1
        /// gas matches the default.</summary>
        public const int DURATION_PER_LEVEL = 4;

        public const int BASE_INTAKE = 100;

        public override bool ApplyGas(Entity target, Zone zone)
        {
            int intake = RunFilterChain(target, BASE_INTAKE);
            if (intake < 0) return false;

            int duration = BaseGas.Level * DURATION_PER_LEVEL;
            target.GetPart<StatusEffectsPart>()?.RemoveEffect<ConfusedEffect>();
            target.ApplyEffect(new ConfusedEffect(duration: duration), BaseGas.Creator, zone);

            Diag.Record("gas", "Applied", BaseGas.Creator, target,
                new { gasId = BaseGas.GasId, gasType = BaseGas.GasType,
                      gasLevel = BaseGas.Level, intake, effectDuration = duration });
            return true;
        }
    }
}
