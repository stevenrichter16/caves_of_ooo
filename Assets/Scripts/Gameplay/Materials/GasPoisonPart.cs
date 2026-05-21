using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.5 — first concrete gas behavior. Direct port of Qud
    /// <c>XRL.World.Parts.GasPoison</c> (GasPoison.cs:1-125), reduced
    /// to CoO's vocabulary. Applies <see cref="PoisonedByGasEffect"/>
    /// to creatures in the cell + a small immediate damage tick scaled
    /// by respiratory intake.
    ///
    /// <para><b>Filter chain</b> (see
    /// <see cref="IObjectGasBehaviorPart"/> for the shared helpers):
    /// <list type="number">
    ///   <item>target != self</item>
    ///   <item>target has "Creature" tag</item>
    ///   <item><c>CheckGasCanAffect</c> event veto (GasImmunity)</item>
    ///   <item><c>GetRespiratoryPerformance</c> intake calc (GasMask)</item>
    ///   <item>Apply <see cref="PoisonedByGasEffect"/> with Duration =
    ///         <see cref="DURATION_MIN"/>..<see cref="DURATION_MAX"/>
    ///         (Qud uses Stat.Random(1, 10)), Damage =
    ///         <c>GasLevel * <see cref="DAMAGE_PER_LEVEL"/></c></item>
    ///   <item>Deal immediate small damage = ceil((intake+1)/20),
    ///         floor of 1 — even with a strong gas mask, 1 hit of
    ///         "exposure" damage gets through (Qud GasPoison.cs:122).</item>
    /// </list></para>
    /// </summary>
    public class GasPoisonPart : IObjectGasBehaviorPart
    {
        public override string Name => "GasPoison";

        // Qud parity constants. Per-type tunings if a future GasPoison
        // variant wants different numbers — defaulting from Qud's GasPoison.
        public const int DURATION_MIN = 1;          // Qud Stat.Random(1, 10)
        public const int DURATION_MAX = 10;
        public const int DAMAGE_PER_LEVEL = 2;      // Qud GasLevel * 2
        public const int BASE_INTAKE = 100;
        public const int IMMEDIATE_DAMAGE_DIVISOR = 20; // Qud Math.Floor((intake+1)/20)

        // Test-injected RNG for deterministic Duration rolls.
        public static System.Random TestRng;

        public override bool ApplyGas(Entity target, Zone zone)
        {
            // G.8a refactor: filter-chain logic extracted into the
            // shared RunFilterChain helper on IObjectGasBehaviorPart so
            // G.8 sibling Parts (Stun, Confusion, etc.) reuse it.
            int intake = RunFilterChain(target, BASE_INTAKE);
            if (intake < 0) return false;

            // Refresh-on-reapply (Qud GasPoison.cs:118 RemoveEffect first).
            // Avoids tick-stacking — re-entering the cloud each turn just
            // refreshes the Duration, doesn't add to it.
            target.GetPart<StatusEffectsPart>()?.RemoveEffect<PoisonedByGasEffect>();

            var rng = TestRng ?? _defaultRng;
            int duration = rng.Next(DURATION_MIN, DURATION_MAX + 1);
            int damage = BaseGas.Level * DAMAGE_PER_LEVEL;
            int gasTypeId = GasTypeStringToId(BaseGas.GasType);

            var fx = new PoisonedByGasEffect
            {
                Duration = duration,
                DamagePerTurn = damage,
                GasTypeKey = BaseGas.GasType,
                Owner = BaseGas.Creator,
            };
            target.ApplyEffect(fx, BaseGas.Creator, zone);

            // Immediate exposure damage — Qud's "InhaleDanger Poison Gas"
            // hit on entry. Scales with intake / divisor, minimum 1 so
            // gas-mask wearers still feel that they walked through gas.
            // Two damage attributes: "Poison" (the element — routes
            // through any future PoisonResistance) AND "Gas" (the
            // delivery vector — lets GasMaskPart scale Amount via
            // BeforeTakeDamage). G.6 wires the "Gas" gate.
            int immediate = (intake + 1) / IMMEDIATE_DAMAGE_DIVISOR;
            if (immediate < 1) immediate = 1;
            var dmg = new Damage(immediate);
            dmg.AddAttribute("Poison");
            dmg.AddAttribute("Gas");
            CombatSystem.ApplyDamage(target, dmg, BaseGas.Creator, zone);

            Diag.Record("gas", "Applied", BaseGas.Creator, target,
                new { gasId = BaseGas.GasId, gasType = BaseGas.GasType,
                      gasLevel = BaseGas.Level, intake, immediateDamage = immediate,
                      effectDuration = duration, effectDamagePerTurn = damage });
            return true;
        }

        private static int GasTypeStringToId(string gasType)
        {
            // Reserved for future per-GasType effect-key disambiguation
            // (e.g. SporeInfection vs PoisonedByGas tracked separately
            // when both can fire on the same creature). G.8 work.
            return gasType?.GetHashCode() ?? 0;
        }

        private static readonly System.Random _defaultRng = new System.Random();
    }
}
