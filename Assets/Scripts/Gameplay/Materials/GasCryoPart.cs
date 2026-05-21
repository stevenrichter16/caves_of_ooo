using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.8b — cryo gas behavior. Direct port of Qud
    /// <c>XRL.World.Parts.GasCryo</c> (GasCryo.cs:1-155). Architecturally
    /// the most divergent gas-type so far: Qud's GasCryo extends
    /// <c>IGasBehavior</c> DIRECTLY (not <c>IObjectGasBehavior</c>)
    /// because it bypasses the "is a creature" gate and the respiratory
    /// pipeline — cryo affects all matter via thermal coupling.
    ///
    /// <para><b>CoO simplification.</b> For the gas system to keep one
    /// shared per-turn dispatcher (<see cref="GasSystem.DispatchPerTurnApply"/>
    /// looks for <see cref="IObjectGasBehaviorPart"/>), GasCryoPart
    /// still inherits IObjectGasBehaviorPart — but its
    /// <see cref="ApplyGas"/> override uses a slimmer filter chain that
    /// SKIPS the Creature + respiratory gates. Anything with a
    /// Hitpoints stat can take cryo damage. Qud-parity divergence
    /// flagged 🟡 in self-review.</para>
    ///
    /// <para><b>Damage attributes.</b> Cold + Gas. The "Cold" tag routes
    /// through any future ColdResistance stat. The "Gas" tag lets
    /// <see cref="GasMaskPart"/> reduce the damage via its
    /// BeforeTakeDamage gate (a sealed hazmat suit dampens cryo
    /// exposure even though intake-reduction isn't a cryo gate).</para>
    ///
    /// <para><b>FrozenEffect.</b> Applies CoO's existing
    /// <see cref="FrozenEffect"/> with <c>Cold = GasLevel × COLD_PER_LEVEL</c>
    /// clamped to [0..1]. Higher-Level cryo gas freezes harder
    /// (FrozenEffect.OnStack adds 50% of incoming Cold).</para>
    /// </summary>
    public class GasCryoPart : IObjectGasBehaviorPart
    {
        public override string Name => "GasCryo";

        /// <summary>Cold-damage per density unit. Density=100 → 20 damage.</summary>
        public const int DAMAGE_PER_FIVE_DENSITY = 1;

        /// <summary>FrozenEffect Cold intensity per GasLevel (clamped 0..1).
        /// Level 1 → 0.30 (mild freeze), Level 3 → 0.90 (deep freeze).</summary>
        public const float COLD_PER_LEVEL = 0.30f;

        public override bool ApplyGas(Entity target, Zone zone)
        {
            if (BaseGas == null) return false;
            if (target == null || target == ParentEntity) return false;
            // Cryo affects any damageable entity (Qud parity: not gated
            // on Creature tag). Hitpoints presence is the minimum bar.
            if (target.GetStat("Hitpoints") == null) return false;

            // Per-type immunity still applies (GasImmunityPart for "Cryo"
            // vetoes via CheckGasCanAffect event).
            if (!CheckCanAffect(target)) return false;

            int coldDamage = BaseGas.Density / 5 * DAMAGE_PER_FIVE_DENSITY;
            if (coldDamage < 1) coldDamage = 1; // exposure floor
            var dmg = new Damage(coldDamage);
            dmg.AddAttribute("Cold");
            dmg.AddAttribute("Gas");
            CombatSystem.ApplyDamage(target, dmg, BaseGas.Creator, zone);

            // FrozenEffect: refresh-on-reapply, Cold scaled by Level.
            float coldIntensity = BaseGas.Level * COLD_PER_LEVEL;
            if (coldIntensity > 1.0f) coldIntensity = 1.0f;
            target.GetPart<StatusEffectsPart>()?.RemoveEffect<FrozenEffect>();
            target.ApplyEffect(new FrozenEffect(cold: coldIntensity), BaseGas.Creator, zone);

            Diag.Record("gas", "Applied", BaseGas.Creator, target,
                new { gasId = BaseGas.GasId, gasType = BaseGas.GasType,
                      gasLevel = BaseGas.Level, density = BaseGas.Density,
                      coldDamage, coldIntensity });
            return true;
        }
    }
}
