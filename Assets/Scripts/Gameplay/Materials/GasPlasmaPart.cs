using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.8e — plasma gas behavior. CoO port of Qud's
    /// <c>XRL.World.Parts.GasPlasma</c> (qud GasPlasma.cs:77-125).
    /// Applies <see cref="CoatedInPlasmaEffect"/> — the gas-as-coat
    /// hybrid — to creatures enveloped by the cloud.
    ///
    /// <para><b>Duration scales with density</b> (Qud parity
    /// GasPlasma.cs:104): <c>Random(density*2/5, density*3/5)</c>.
    /// A density-100 cloud coats for 40-60 turns. The coat outlasts
    /// the cloud — that's the gas-as-coat hybrid distinction.</para>
    ///
    /// <para><b>Refresh-on-reapply is delegated to the Effect's
    /// OnStack</b> (take larger Duration). Unlike sibling Parts, this
    /// Part does NOT call <c>RemoveEffect&lt;CoatedInPlasmaEffect&gt;()</c>
    /// first — doing so would unapply+reapply the resistance stat
    /// shifts (re-capturing the already-shifted values as "prior").
    /// Just calls ApplyEffect; StatusEffectsPart routes to OnStack.</para>
    /// </summary>
    public class GasPlasmaPart : IObjectGasBehaviorPart
    {
        public override string Name => "GasPlasma";

        public const int BASE_INTAKE = 100;

        // Test-injected RNG for deterministic duration rolls.
        public static System.Random TestRng;
        private static readonly System.Random _defaultRng = new System.Random();

        public override bool ApplyGas(Entity target, Zone zone)
        {
            // Shared filter chain (self-guard → Creature tag →
            // CheckGasCanAffect veto → respiratory intake). We don't
            // USE the intake to scale anything (Qud plasma scales the
            // coat by cloud DENSITY, not intake) but we still run the
            // chain so GasImmunity/non-creature/mask gates apply.
            int intake = RunFilterChain(target, BASE_INTAKE);
            if (intake < 0) return false;

            // Duration scales with cloud density (Qud GasPlasma.cs:104).
            var rng = TestRng ?? _defaultRng;
            int duration = ComputeDuration(BaseGas.Density, rng);
            if (duration < 1) return false;

            // NOTE: unlike GasPoison we do NOT RemoveEffect first.
            // Refresh-on-reapply is delegated to CoatedInPlasmaEffect.
            // OnStack (take larger Duration). Removing+re-adding would
            // unapply+reapply the resistance shift, re-capturing the
            // already-shifted values as the new "prior" — corrupting the
            // restore. ApplyEffect routes a re-apply straight to OnStack.
            target.ApplyEffect(new CoatedInPlasmaEffect(duration, target),
                BaseGas.Creator, zone);

            Diag.Record("gas", "Applied", BaseGas.Creator, target,
                new { gasId = BaseGas.GasId, gasType = BaseGas.GasType,
                      density = BaseGas.Density, gasLevel = BaseGas.Level,
                      intake, coatDuration = duration });
            return true;
        }

        /// <summary>Compute coat duration from cloud density. Pure
        /// function for testability. <c>Random(density*2/5,
        /// density*3/5)</c> inclusive, floored at 1.</summary>
        public static int ComputeDuration(int density, System.Random rng)
        {
            if (density <= 0) return 0;
            int min = density * 2 / 5;
            int max = density * 3 / 5;
            if (min < 1) min = 1;
            if (max < min) max = min;
            int d = (min >= max) ? min : rng.Next(min, max + 1);
            return d < 1 ? 1 : d;
        }
    }
}
