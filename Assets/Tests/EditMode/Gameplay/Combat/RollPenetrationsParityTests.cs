using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase A of the Qud-parity port: tests that pin the EXPECTED Qud-parity
    /// behavior of <see cref="CombatSystem.RollPenetrations"/>.
    ///
    /// Reference: <c>XRL.Rules.Stat.RollDamagePenetrations</c> (lines 160-203).
    ///
    /// These tests are written RED-first (before the port) and document the
    /// behaviors that distinguish Qud's algorithm from the pre-port simplified
    /// version. Each test name calls out the specific behavior it pins.
    ///
    /// Algorithm summary (Qud parity):
    ///   • Outer loop: roll sets of 3 dice while the previous set was a clean sweep.
    ///   • Each die: 1d10 − 2 (range −1 to 8). Explodes on raw 10 (post-mod 8):
    ///     adds +8 and re-rolls; explosions chain.
    ///   • Pen counting: a set with ≥1 success awards EXACTLY 1 penetration
    ///     (NOT one per successful roll).
    ///   • Bonus decays by 2 every set unconditionally (not just on streak).
    ///   • Effective bonus per roll: Math.Min(Bonus, MaxBonus).
    /// </summary>
    public class RollPenetrationsParityTests
    {
        // ====================================================================
        // 1. Exploding dice — chained 10s allow penetration of targets that
        //    the OLD algorithm (1d8 + PV vs AV) could never beat.
        // ====================================================================

        [Test]
        public void ExplodingDice_AllowsPenetration_AtImpossibleBaseRange()
        {
            // Target = 12, Bonus = -2, MaxBonus = 0.
            // OLD algo (1d8+PV vs AV): 1d8 + (−2) = −1..6, never > 12. Always 0.
            // QUD algo: per roll, num4 starts at 0. If raw 10 hits, num4=8, re-roll.
            //   Two consecutive raw 10s → num4 = 16 + final num3 (−1..7) = 15..23.
            //   With Math.Min(−2, 0) = −2 added: 13..21. Some > 12 → penetrates.
            //   So: across many seeds, at least ONE produces a penetration.
            int totalPens = 0;
            int trialsWithAnyPen = 0;
            for (int seed = 0; seed < 5000; seed++)
            {
                int pens = CombatSystem.RollPenetrations(
                    targetInclusive: 12, bonus: -2, maxBonus: 0, rng: new Random(seed));
                totalPens += pens;
                if (pens > 0) trialsWithAnyPen++;
            }
            // Expected explosion-driven success rate is small but nonzero.
            // Assertion: at least one penetration occurred across 5000 seeded trials.
            Assert.Greater(trialsWithAnyPen, 0,
                "Exploding dice must enable penetration of a target unreachable by base roll");
        }

        // ====================================================================
        // 2. Per-set pen counting — Qud's set produces AT MOST 1 pen, regardless
        //    of how many of the 3 rolls succeeded. The old algorithm awarded
        //    one pen per success (so 2 successes → 2 pens).
        //
        //    Hard to construct a single-seed proof; instead use a statistical
        //    bound: with success-rate per roll p, the OLD algo's expected pens
        //    grows with p, while Qud's caps at ~1 per non-streak set.
        // ====================================================================

        [Test]
        public void PerSetPenCounting_AveragePensCappedByQudFormula()
        {
            // Bonus = 2, MaxBonus = 2, Target = 4.
            // Per roll: num3 ∈ [−1, 8] (no explosions on rolls < 8 here is wrong;
            //   raw 10 → 8 still explodes. We allow some explosion.).
            // Without explosion: num4 + 2 > 4 needs num4 > 2 → raw 5,6,7,8,9 → 5/10 = 50%.
            //   Raw 10 explodes (8 + ...) so contributes too. Effective P(succ) ≈ 0.55.
            //
            // OLD algo expected per-set pens (no streak): ~1.65 (3 × 0.55).
            // QUD algo expected per-set pens (no streak): ~0.91 (P(at least 1 succ) = 1 - 0.45^3).
            // QUD algo with streak: streak rate ~16.6% → modest extra pens via Bonus−2 sets.
            //
            // Empirical bound for the Qud algorithm: average pens per call should be
            // well under 2 across 1000 trials with these parameters.
            int totalPens = 0;
            const int trials = 1000;
            for (int seed = 0; seed < trials; seed++)
            {
                int pens = CombatSystem.RollPenetrations(4, 2, 2, new Random(seed));
                totalPens += pens;
            }
            double avgPens = totalPens / (double)trials;
            // Tight upper bound: Qud's per-set cap of 1 + occasional streak chains
            // produces empirical avg ~1.0-1.3. The OLD algo would produce ~1.7+
            // (one pen per successful roll, no per-set cap).
            Assert.Less(avgPens, 1.5,
                $"Qud's per-set pen cap should keep avg pens < 1.5; got {avgPens:F2}");
        }

        // ====================================================================
        // 3. Unconditional bonus decay — Qud decays Bonus by 2 every set
        //    whether or not the set was a clean sweep. The old algorithm
        //    only decayed currentPV after a streak (3 of 3).
        //
        //    Test: with Bonus very high relative to Target+MaxBonus cap, every
        //    set should produce a clean sweep — but our Bonus−2 happens
        //    regardless. We can't observe Bonus directly; instead, observe
        //    that a sufficiently "easy" target is eventually beaten less
        //    consistently as Bonus decays below MaxBonus.
        //
        //    Cleaner approach: with MaxBonus high but Bonus moderate, after
        //    many sets the effective bonus (Min(Bonus, MaxBonus)) becomes
        //    Bonus and decays. We can detect bounded pen counts.
        // ====================================================================

        [Test]
        public void BonusDecay_LimitsTotalPensInDeterministicScenario()
        {
            // Bonus = 8, MaxBonus = 100, Target = 0.
            // First set: effective = min(8, 100) = 8. num3 ∈ [−1, 8] + 8 = [7, 16+]. Always > 0. Clean sweep, +1 pen, Bonus → 6.
            // Second set: effective = 6. num3+6 ∈ [5, 14+]. Always > 0. Clean sweep, +1, Bonus → 4.
            // Third: effective = 4. num3+4 ∈ [3, 12+]. Always > 0. +1, Bonus → 2.
            // Fourth: effective = 2. num3+2 ∈ [1, 10+]. Always > 0. +1, Bonus → 0.
            // Fifth: effective = 0. num3+0 ∈ [−1, 8+]. Roll −1 (raw 1) FAILS. Streak likely breaks.
            //
            // Assertion: pens are bounded — definitely between 4 and ~10.
            // Validates that decay limits the runaway streak.
            int totalPens = 0;
            int trials = 100;
            for (int seed = 0; seed < trials; seed++)
            {
                int pens = CombatSystem.RollPenetrations(0, 8, 100, new Random(seed));
                Assert.GreaterOrEqual(pens, 4,
                    $"With Bonus=8 vs Target=0, first 4 sets always sweep → pens >= 4 (seed {seed})");
                Assert.LessOrEqual(pens, 30,
                    $"Bonus decay must bound the streak; runaway loops impossible (seed {seed})");
                totalPens += pens;
            }
            double avgPens = totalPens / (double)trials;
            Assert.Greater(avgPens, 4.0, "Sanity: avg should clearly exceed minimum");
        }

        // ====================================================================
        // 4. MaxBonus cap — when Bonus > MaxBonus, only MaxBonus contributes
        //    to the per-roll comparison.
        // ====================================================================

        [Test]
        public void MaxBonusCap_LimitsBonusContribution()
        {
            // Setup: Bonus=100, MaxBonus=0, Target=5.
            // Effective bonus = Math.Min(100, 0) = 0.
            // Per roll: num3 + 0 = num3. Range [−1, 8] (with explosions higher).
            // num3 > 5 → raw 8, 9, 10 → 3/10 = 30% (without exploding contribution).
            // Crucially, capped behavior != uncapped behavior:
            //   With cap → ~0.3-1 pen per set (no streak).
            //   Without cap → Bonus=100 makes every roll succeed → big pens.
            //
            // So average with cap should be much smaller than without.
            int withCapTotal = 0, withoutCapTotal = 0;
            const int trials = 200;
            for (int seed = 0; seed < trials; seed++)
            {
                withCapTotal += CombatSystem.RollPenetrations(5, 100, 0, new Random(seed));
                withoutCapTotal += CombatSystem.RollPenetrations(5, 100, 100, new Random(seed));
            }
            Assert.Less(withCapTotal * 5, withoutCapTotal,
                $"Cap=0 must dramatically reduce penetrations vs cap=100. " +
                $"With cap: {withCapTotal}, without: {withoutCapTotal}");
        }

        // ====================================================================
        // 5. Determinism — same seed always produces same result.
        // ====================================================================

        [Test]
        public void Deterministic_SameSeedSameResult()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                int a = CombatSystem.RollPenetrations(5, 7, 7, new Random(seed));
                int b = CombatSystem.RollPenetrations(5, 7, 7, new Random(seed));
                Assert.AreEqual(a, b, $"Seed {seed} must produce identical results across calls");
            }
        }

        // ====================================================================
        // 6. Loop termination — even with parameters that maximize streak chains
        //    and explosion potential, the function must return in bounded time.
        // ====================================================================

        [Test]
        public void LoopTerminates_EvenWithExtremeBonuses()
        {
            // Massive Bonus, massive MaxBonus, AV=0. Every set sweeps until Bonus
            // decays into failure range. Even with explosions, must terminate.
            int pens = CombatSystem.RollPenetrations(0, 1000, 1000, new Random(42));
            Assert.Greater(pens, 100, "With Bonus=1000 vs AV=0, many sets should sweep");
            Assert.Less(pens, 100_000, "Loop must terminate; Bonus decay bounds total sets");
        }

        [Test]
        public void LoopTerminates_AtPathologicalNegativeBonus()
        {
            // Negative Bonus immediately forces failures; loop should exit fast.
            int pens = CombatSystem.RollPenetrations(50, -100, -100, new Random(42));
            Assert.AreEqual(0, pens,
                "Massive negative bonus + high target → 0 pens, immediate exit");
        }
    }
}
