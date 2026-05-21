using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.8a — stun-gas behavior. Applies the existing
    /// <see cref="StunnedEffect"/> to creatures inhaling the gas, via
    /// the shared filter chain in <see cref="IObjectGasBehaviorPart"/>.
    /// Mirrors Qud <c>XRL.World.Parts.GasStun</c>.
    ///
    /// <para><b>Different from GasPoisonPart in two ways:</b>
    /// <list type="number">
    ///   <item>NO immediate damage — stun is incapacitation, not
    ///         exposure damage. (Qud's GasStun also doesn't tick HP.)</item>
    ///   <item>No lingering after-exit effect — Qud's stun is applied
    ///         per-tick while in the cloud and naturally ticks down via
    ///         StunnedEffect's existing Duration. A creature stepping
    ///         out of the cloud just lets the existing stun expire
    ///         normally.</item>
    /// </list>
    /// Refresh-on-reapply: removes any existing StunnedEffect first
    /// (Qud parity GasPoison.cs:118), so re-entering the cloud each
    /// turn refreshes Duration rather than stacking it.</para>
    /// </summary>
    public class GasStunPart : IObjectGasBehaviorPart
    {
        public override string Name => "GasStun";

        /// <summary>Per-level stun duration in turns. Level 1 → 2 turns,
        /// Level 2 → 4 turns, etc. StunnedEffect's default duration is
        /// also 2 — matches the lower bound.</summary>
        public const int DURATION_PER_LEVEL = 2;

        public const int BASE_INTAKE = 100;

        public override bool ApplyGas(Entity target, Zone zone)
        {
            int intake = RunFilterChain(target, BASE_INTAKE);
            if (intake < 0) return false;

            int duration = BaseGas.Level * DURATION_PER_LEVEL;
            target.GetPart<StatusEffectsPart>()?.RemoveEffect<StunnedEffect>();
            target.ApplyEffect(new StunnedEffect(duration: duration), BaseGas.Creator, zone);

            Diag.Record("gas", "Applied", BaseGas.Creator, target,
                new { gasId = BaseGas.GasId, gasType = BaseGas.GasType,
                      gasLevel = BaseGas.Level, intake, effectDuration = duration });
            return true;
        }
    }
}
