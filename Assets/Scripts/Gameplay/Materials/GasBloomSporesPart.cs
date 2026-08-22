using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W4.4 SM-B (Docs/FELLING-W4-PLAN.md §6) — bloom-spores gas
    /// behavior: exposure can put the Driving Bloom on a body
    /// (<see cref="BloomedEffect"/>).
    ///
    /// <para><b>Why a sibling and not a rider (sweep row 5):</b>
    /// GasFungalSporesPart hard-codes FungalInfectionEffect and its
    /// counter-test pins "this gas applies ONLY FungalInfectionEffect";
    /// the 5-stage infection is a separate, heavily pinned disease arc.
    /// The Bloom SITS BESIDE it: its own gas def ("bloom-spores",
    /// GasType BloomSpores, the Choir's &amp;m), its own behavior part,
    /// mirroring <see cref="GasFungalSporesPart"/>'s shape — shared
    /// filter chain, already-affected bail, Toughness-vs-Level chance —
    /// with the Bloom's own constants so the two tune independently.</para>
    ///
    /// <para><b>Refresh-on-reapply is a no-op</b>: already Bloomed means
    /// already worn; there is no "more worn"
    /// (<see cref="BloomedEffect.OnStack"/> absorbs). The bail here
    /// emits <c>gas/BloomAlreadyPresent</c> so a query can tell
    /// "already taken, no roll" from "rolled and stood".</para>
    /// </summary>
    public class GasBloomSporesPart : IObjectGasBehaviorPart
    {
        public override string Name => "GasBloomSpores";

        /// <summary>Same shape as the fungal chance
        /// (GasFungalSporesPart) but the Bloom's own numbers: taking is
        /// a little harder than infection — the Bloom prefers a body
        /// that stays long enough to be argued with.</summary>
        public const int BASE_TAKE_CHANCE_PERCENT = 25;
        public const int CHANCE_PER_GAS_LEVEL = 10;
        public const int CHANCE_REDUCTION_PER_TOUGHNESS = 2;
        public const int BASE_INTAKE = 100;

        // Test-injected RNG (GasPoisonPart.TestRng pattern).
        public static System.Random TestRng;
        private static readonly System.Random _defaultRng = new System.Random();

        public override bool ApplyGas(Entity target, Zone zone)
        {
            int intake = RunFilterChain(target, BASE_INTAKE);
            if (intake < 0) return false;

            // Already worn — no second taking (BloomedEffect.OnStack
            // would absorb anyway; bailing here gives the specific
            // record a query wants).
            if (target.GetEffect<BloomedEffect>() != null)
            {
                if (Diag.IsChannelEnabled("gas"))
                    Diag.Record("gas", "BloomAlreadyPresent", BaseGas.Creator, target,
                        new { gasId = BaseGas.GasId, gasType = BaseGas.GasType });
                return false;
            }

            int toughness = target.GetStatValue("Toughness", 14);
            int chance = ComputeTakeChance(BaseGas.Level, toughness);
            var rng = TestRng ?? _defaultRng;
            int roll = rng.Next(100);
            bool taken = roll < chance;

            if (Diag.IsChannelEnabled("gas"))
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
                        taken,
                    });

            if (taken)
            {
                target.ApplyEffect(new BloomedEffect(), BaseGas.Creator, zone);
                return true;
            }
            return false;
        }

        /// <summary>Chance (0..100) the Bloom takes a body at this gas
        /// level vs this Toughness. Pure; floor 0 (a hard body can be
        /// beneath the Bloom's notice).</summary>
        public static int ComputeTakeChance(int gasLevel, int targetToughness)
        {
            int chance = BASE_TAKE_CHANCE_PERCENT
                + gasLevel * CHANCE_PER_GAS_LEVEL
                - targetToughness * CHANCE_REDUCTION_PER_TOUGHNESS;
            if (chance < 0) chance = 0;
            if (chance > 100) chance = 100;
            return chance;
        }
    }
}
