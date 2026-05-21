using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.8d.2 — fungal-spores gas behavior. CoO-simplified port of
    /// Qud's <c>XRL.World.Parts.GasFungalSpores</c>
    /// (qud GasFungalSpores.cs:83-188). Applies
    /// <see cref="FungalInfectionEffect"/> to inhaling creatures via
    /// the shared filter chain in
    /// <see cref="IObjectGasBehaviorPart"/>.
    ///
    /// <para><b>Qud divergence:</b> Qud also applies a short-tick
    /// <c>SporeCloudPoison</c> effect alongside the long infection
    /// (so even creatures who save the infection still take some
    /// immediate damage from the cloud). CoO simplification: the
    /// gas's per-turn dose IS the short tick (creatures who fail the
    /// infection still take immediate damage from
    /// <see cref="GasPoisonPart"/>-style exposure). One Effect, not two.
    /// Documented.</para>
    ///
    /// <para><b>Refresh-on-reapply is INTENTIONALLY no-op</b> — unlike
    /// sibling gases. Per <see cref="FungalInfectionEffect.OnStack"/>
    /// the stage clock is preserved on re-exposure. This Part does NOT
    /// call <c>RemoveEffect&lt;FungalInfectionEffect&gt;()</c> before
    /// ApplyEffect (which would defeat the no-reset invariant); instead
    /// it checks for the existing effect and emits a
    /// <c>gas/InfectionAlreadyPresent</c>-style diag.</para>
    ///
    /// <para><b>Infection chance.</b> Qud rolls a Toughness save vs
    /// <c>10 + GasLevel/3</c>. CoO has no save-roll subsystem. We use
    /// a deterministic infection-probability scaled by GasLevel vs
    /// target Toughness: <c>chance = clamp(BASE_CHANCE + GasLevel × 10
    /// - Toughness × 2, 0, 100)</c>. A Level-1 spore cloud vs Toughness
    /// 14 = 30 + 10 - 28 = 12% chance per exposure. Walking through
    /// thicker clouds (higher GasLevel) is much riskier. Tunable.</para>
    /// </summary>
    public class GasFungalSporesPart : IObjectGasBehaviorPart
    {
        public override string Name => "GasFungalSpores";

        /// <summary>Baseline infection chance before Level/Toughness
        /// adjustments. 30% = "low-Toughness creature in Level 1 spore
        /// cloud has ~12-30% chance per exposure depending on Tough."</summary>
        public const int BASE_INFECTION_CHANCE_PERCENT = 30;
        public const int CHANCE_PER_GAS_LEVEL = 10;
        public const int CHANCE_REDUCTION_PER_TOUGHNESS = 2;
        public const int BASE_INTAKE = 100;

        // Test-injected RNG (mirrors GasPoisonPart.TestRng pattern).
        public static System.Random TestRng;
        private static readonly System.Random _defaultRng = new System.Random();

        public override bool ApplyGas(Entity target, Zone zone)
        {
            int intake = RunFilterChain(target, BASE_INTAKE);
            if (intake < 0) return false;

            // Already infected? Refresh-on-reapply is INTENTIONALLY
            // no-op (FungalInfectionEffect.OnStack preserves the stage
            // clock). Don't call RemoveEffect first; just bail.
            // FungalInfectionEffect.OnStack would also handle this if
            // we called ApplyEffect, but the diag here is more specific
            // (it differentiates "already infected, no roll" from
            // "rolled and failed").
            if (target.GetEffect<FungalInfectionEffect>() != null)
            {
                Diag.Record("gas", "InfectionAlreadyPresent", BaseGas.Creator, target,
                    new { gasId = BaseGas.GasId, gasType = BaseGas.GasType });
                return false;
            }

            // Infection chance roll. Toughness-vs-Level math from
            // ComputeInfectionChance is the CoO substitute for Qud's
            // Toughness save (MakeSave at GasFungalSpores.cs:160).
            int toughness = target.GetStatValue("Toughness", 14);
            int chance = ComputeInfectionChance(BaseGas.Level, toughness);
            var rng = TestRng ?? _defaultRng;
            int roll = rng.Next(100);
            bool infected = roll < chance;

            Diag.Record("gas", "Applied", BaseGas.Creator, target,
                new
                {
                    gasId = BaseGas.GasId,
                    gasType = BaseGas.GasType,
                    gasLevel = BaseGas.Level,
                    intake,
                    targetToughness = toughness,
                    chance,
                    roll,
                    infected,
                });

            if (infected)
            {
                target.ApplyEffect(new FungalInfectionEffect(), BaseGas.Creator, zone);
                return true;
            }
            return false;
        }

        /// <summary>Compute infection chance (0..100) for a given gas
        /// level vs target Toughness. Pure function — testable in
        /// isolation. Floor at 0 (a very tough target can be fully
        /// immune to weak spores).</summary>
        public static int ComputeInfectionChance(int gasLevel, int targetToughness)
        {
            int chance = BASE_INFECTION_CHANCE_PERCENT
                + gasLevel * CHANCE_PER_GAS_LEVEL
                - targetToughness * CHANCE_REDUCTION_PER_TOUGHNESS;
            if (chance < 0) chance = 0;
            if (chance > 100) chance = 100;
            return chance;
        }
    }
}
