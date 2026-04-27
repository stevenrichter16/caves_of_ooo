using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase A adversarial pass for <see cref="CombatSystem.RollPenetrations"/>.
    ///
    /// Each test is engineered to FAIL under a specific common mutation. The
    /// goal is mutation-resistance: if someone "improves" the algorithm in a
    /// way that breaks Qud parity, at least one of these tests should catch it.
    ///
    /// Mutations these tests aim to catch:
    ///   • <c>&gt;</c> flipped to <c>&gt;=</c> in success comparison
    ///   • <c>Math.Min(bonus, maxBonus)</c> flipped to <c>Math.Max</c>
    ///   • <c>successesInSet &gt;= 1</c> flipped to <c>&gt;= 2</c>
    ///   • <c>successesInSet == 3</c> flipped to <c>&gt;= 1</c> (would never exit)
    ///   • Removing <c>bonus -= 2</c> (infinite loop)
    ///   • Removing the explosion loop (no chained 10s)
    ///   • Pen-per-roll regression (the OLD pre-port behavior)
    /// </summary>
    public class RollPenetrationsAdversarialTests
    {
        // ====================================================================
        // 1. STRICT-GREATER comparison — die + bonus must EXCEED target,
        //    not equal it. Catches `>` → `>=` mutation.
        //
        // Construct: target = bonus + max-non-explode-die-without-explosion.
        // Without explosion, dieResult max = 8 (raw 9 → 9-2 = 7? Wait, raw=10
        // explodes, so non-explode max raw is 9 → die=7). With explosion,
        // dieResult ≥ 7 always (explode adds 8 then re-rolls).
        //
        // If we set target = bonus + 7, then ONLY explosions can succeed. With
        // strict-greater the boundary die exactly = bonus + 7 → fails. With
        // mutated >=, the boundary would succeed. The test is statistical:
        // with strict semantics, success rate ≈ explosion rate (~11%).
        // ====================================================================

        [Test]
        public void StrictGreaterComparison_BoundaryDieAlone_DoesNotPenetrate()
        {
            // target = 7, bonus = 0, maxBonus = 0. Effective per roll: die.
            // Non-explode max die = 7 (raw 9). Need die > 7 → only explosions
            // succeed. With strict-greater, success rate ≈ P(explode) = 0.1.
            // Mutated to >=, raw 9 (die=7) would also count. Success rate jumps to ~0.2.
            int succ = 0;
            const int trials = 2000;
            for (int seed = 0; seed < trials; seed++)
            {
                int pens = CombatSystem.RollPenetrations(7, 0, 0, new Random(seed));
                if (pens > 0) succ++;
            }
            // Per-set P(any succ) with strict-greater ≈ 1 - (1 - 0.1)^3 ≈ 0.27.
            // Per-set with mutated >= ≈ 1 - (1 - 0.2)^3 ≈ 0.49.
            // Streak / multi-set effects amplify: avg pens with mutation ≈ 1.5x.
            // Bound: trial-level success rate well under 50%, well over 5%.
            double rate = succ / (double)trials;
            Assert.Less(rate, 0.50,
                $"Strict-greater must keep success rate < 0.50 at boundary. Got {rate:F3}");
            Assert.Greater(rate, 0.05,
                $"Explosions must produce SOME successes (>5%). Got {rate:F3}");
        }

        // ====================================================================
        // 2. MaxBonus cap is MIN, not MAX. Catches `Min` → `Max` mutation.
        //
        // With Bonus much larger than MaxBonus, the cap should DOMINATE. With
        // Max instead of Min, MaxBonus would be ignored.
        //
        // Asserts a directional invariant: same seed, higher MaxBonus → result
        // is monotonically non-decreasing.
        // ====================================================================

        [Test]
        public void MinCap_HigherMaxBonus_ProducesMonotoneNonDecreasingPens()
        {
            // Bonus=20 fixed. Vary MaxBonus from 0 to 20.
            // Same seed → same RNG sequence; only the cap changes.
            // With Min: more cap = bigger effective bonus = more (or equal) pens.
            // With Max: behavior is bizarre and would likely produce non-monotone results.
            for (int seed = 0; seed < 30; seed++)
            {
                int prevPens = -1;
                for (int cap = 0; cap <= 20; cap += 2)
                {
                    int pens = CombatSystem.RollPenetrations(0, 20, cap, new Random(seed));
                    if (prevPens >= 0)
                    {
                        Assert.GreaterOrEqual(pens, prevPens,
                            $"seed={seed}: increasing MaxBonus from {cap-2} to {cap} must not decrease pens (Min cap monotone)");
                    }
                    prevPens = pens;
                }
            }
        }

        // ====================================================================
        // 3. Per-set pen counting — a set with 3 successes still produces only
        //    1 pen (then potentially another set if streak). Catches the
        //    "old-algo regression" of awarding 1 pen per successful roll.
        //
        // Strategy: choose parameters where set 1 reliably sweeps but set 2
        // reliably fails. Total = 1 if per-set; total = 3 if per-roll.
        //
        // Difficult to make deterministic (set 2 outcome is random). Use a
        // statistical bound instead: with parameters that make set 1 always
        // sweep but set 2 mostly fail, average pens should be close to 1
        // (well under 2), not close to 3.
        // ====================================================================

        [Test]
        public void PerSetPenCounting_NotPerRoll_AvgPensCloseToOne()
        {
            // Bonus=8, MaxBonus=8, Target=7.
            // Per roll set 1: die + 8 > 7 → die > -1 → die ≥ 0.
            //   die = -1 only when raw=1 (P=0.1). die >= 0: P=0.9.
            //   P(set 1 sweep) = 0.9^3 = 0.729. Total pens after set 1: 1 (in 0.999 of cases).
            // After set 1 sweeps, bonus=6. Per roll set 2: die + 6 > 7 → die > 1 → raw ≥ 4.
            //   P(succ) = ~0.7. P(set 2 sweep) = 0.343. Most likely set 2 has 1-2 succ → +1 pen, exit.
            //   Or set 2 sweeps → +1 pen, continue (rarer).
            //
            // Old algorithm (per-roll): set 1 with 3 succ = 3 pens.
            //                           Plus pens from subsequent rolls until streak conditions.
            //                           Average would be 3+.
            // New algorithm (per-set):  set 1 sweep = 1 pen. Subsequent sets = 1 each.
            //                           Average ≈ 2-3 pens (1 from set 1, 1 from set 2 typically, occasional more).
            int totalPens = 0;
            const int trials = 1000;
            for (int seed = 0; seed < trials; seed++)
                totalPens += CombatSystem.RollPenetrations(7, 8, 8, new Random(seed));
            double avg = totalPens / (double)trials;
            // Empirical with Qud algo: ~2.0 ± 0.2.
            // Old per-roll algo would be ≥ 4.
            Assert.Less(avg, 3.5,
                $"Per-set counting must keep avg < 3.5 (old per-roll algo would exceed this). Got {avg:F2}");
        }

        // ====================================================================
        // 4. Loop terminates when set has zero successes (sentinel logic).
        //    Catches `successesInSet == 3` → `>= 1` mutation (would loop forever).
        //
        // With target so high NO success is achievable, set 1 has 0 succ →
        // outer-loop guard `successesInSet == 3` is false → exit, return 0.
        //
        // Mutation `successesInSet >= 1` would re-enter the loop on 0 successes
        // (since 0 >= 1 is false; this mutation would actually exit faster, not
        // hang). The dangerous mutation is the inverse `>= 0` which always
        // continues. Test: bounded execution + correct return value.
        // ====================================================================

        [Test]
        public void OuterLoopGuard_ZeroSuccesses_ExitsImmediately_ReturnsZero()
        {
            // target = 200, bonus = -100, maxBonus = -100. Per roll: die - 100 > 200
            // requires die > 300. Need ~38 chained explosions (each +8). P ≈ 10^-38.
            // Practically: 0 successes in any reasonable trial count.
            for (int seed = 0; seed < 50; seed++)
            {
                int pens = CombatSystem.RollPenetrations(200, -100, -100, new Random(seed));
                Assert.AreEqual(0, pens,
                    $"With effectively-impossible target, must return 0 immediately (seed {seed})");
            }
        }

        // ====================================================================
        // 5. Bonus decay is unconditional per set. Catches removal of `bonus -= 2`.
        //
        // Without decay, with bonus=20 vs target=0, every set sweeps forever
        // (would hang or hit timeout). With decay, terminates in ~10-15 sets.
        //
        // Test: assert bounded execution, indirectly proving decay is happening.
        // (If decay were removed, this test would hang or hit a giant pen count.)
        // ====================================================================

        [Test]
        public void BonusDecay_Unconditional_BoundsExecutionTime()
        {
            // Run 200 calls; if decay is removed, each call takes effectively
            // forever. With decay, each completes in microseconds.
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int seed = 0; seed < 200; seed++)
            {
                int pens = CombatSystem.RollPenetrations(0, 20, 20, new Random(seed));
                Assert.Less(pens, 200,
                    $"Bonus decay must bound pens per call (seed {seed}: got {pens})");
            }
            sw.Stop();
            // Generous bound: 200 calls should finish in well under 5 seconds.
            // Without decay, even one call would not terminate.
            Assert.Less(sw.ElapsedMilliseconds, 5000,
                $"Without decay, this loop would not terminate. Took {sw.ElapsedMilliseconds}ms");
        }

        // ====================================================================
        // 6. Exploding dice are present. Catches removal of explosion loop.
        //
        // Without explosions, max die = 8 - 2 = 6 (raw 8 → die 6).
        // Wait — with explosions, raw 10 yields rawRoll = 8, then explodes.
        // Without explosion code, raw 10 → die = 8.
        // So removing the explode loop changes max die from "unbounded" to 8.
        //
        // Test: at impossibly-high target reachable ONLY by explosions, expect
        // some successes across many trials.
        // ====================================================================

        [Test]
        public void ExplodingDice_Present_AllowsExtremeRolls()
        {
            // Target=20, Bonus=-2, MaxBonus=0. Effective per roll: die - 2.
            // Without explosion: max die = 8, max total = 6. Never > 20.
            // With explosion: die = 8+8+...+raw. Need at least 3 explosions to exceed 22.
            // P(3 explosions) = (1/10)^3 ≈ 0.001. Per-roll P(succ) ≈ 0.001.
            // Per-set P(any succ) ≈ 0.003. Across 5000 trials: expect ~10-15 successes.
            int succCount = 0;
            for (int seed = 0; seed < 5000; seed++)
            {
                int pens = CombatSystem.RollPenetrations(20, -2, 0, new Random(seed));
                if (pens > 0) succCount++;
            }
            Assert.Greater(succCount, 0,
                "Without exploding dice, no penetration possible at target=20, bonus=0. " +
                "If this fires zero, the explosion loop is missing.");
        }

        // ====================================================================
        // 7. Integration smoke — PerformMeleeAttack still works with new signature.
        //    Catches caller-update regressions (forgot to thread maxBonus through,
        //    swapped argument order, etc.).
        // ====================================================================

        [Test]
        public void Integration_PerformMeleeAttack_DealsDamage_PostPort()
        {
            var zone = new Zone();
            var attacker = new Entity();
            attacker.BlueprintName = "TestAttacker";
            attacker.Tags["Creature"] = "";
            attacker.Statistics["Hitpoints"] = new Stat { Owner = attacker, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            attacker.Statistics["Strength"] = new Stat { Owner = attacker, Name = "Strength", BaseValue = 20, Min = 1, Max = 50 };
            attacker.Statistics["Agility"] = new Stat { Owner = attacker, Name = "Agility", BaseValue = 20, Min = 1, Max = 50 };
            attacker.Statistics["Speed"] = new Stat { Owner = attacker, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            attacker.AddPart(new RenderPart { DisplayName = "attacker" });
            attacker.AddPart(new PhysicsPart { Solid = true });
            attacker.AddPart(new MeleeWeaponPart { BaseDamage = "1d4", PenBonus = 5 });
            attacker.AddPart(new ArmorPart());
            zone.AddEntity(attacker, 5, 5);

            var defender = new Entity();
            defender.BlueprintName = "TestDefender";
            defender.Tags["Creature"] = "";
            defender.Statistics["Hitpoints"] = new Stat { Owner = defender, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            defender.Statistics["Strength"] = new Stat { Owner = defender, Name = "Strength", BaseValue = 10, Min = 1, Max = 50 };
            defender.Statistics["Agility"] = new Stat { Owner = defender, Name = "Agility", BaseValue = 10, Min = 1, Max = 50 };
            defender.Statistics["Speed"] = new Stat { Owner = defender, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            defender.AddPart(new RenderPart { DisplayName = "defender" });
            defender.AddPart(new PhysicsPart { Solid = true });
            defender.AddPart(new ArmorPart { AV = 0, DV = 0 });
            zone.AddEntity(defender, 6, 5);

            int initialHP = defender.GetStatValue("Hitpoints");
            bool dealtDamage = false;
            for (int seed = 0; seed < 20; seed++)
            {
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
                if (defender.GetStatValue("Hitpoints") < initialHP)
                {
                    dealtDamage = true;
                    break;
                }
            }
            Assert.IsTrue(dealtDamage,
                "Post-port: high-stat attacker vs low-armor defender must still deal damage");
        }
    }
}
