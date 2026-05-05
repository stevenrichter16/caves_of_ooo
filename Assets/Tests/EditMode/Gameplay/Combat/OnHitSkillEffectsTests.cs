using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WS.1+ — skill-driven on-hit effect tests. Pins the contract:
    ///
    ///   No SkillsPart on attacker  → no-op (early-out)
    ///   actualDamage = 0           → no-op (vetoed/resisted hit gate)
    ///   null defender / damage / attacker / rng → no-op (no crash)
    ///
    /// Per-skill behavior tests live below the no-op section and are
    /// added as each WS.2-5 sub-milestone lands its skill branch.
    /// All seed loops are bounded; positive cases assert
    /// "across N seeds, at least one observation" and counter-checks
    /// assert "across N seeds, zero observations" (CLAUDE.md §3.4).
    /// </summary>
    public class OnHitSkillEffectsTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ====================================================================
        // Universal scaffold contract (WS.1) — Apply must be safe to call
        // even when nothing is wired up yet (skills branches fill in WS.2-5).
        // ====================================================================

        [Test]
        public void Apply_NullDefender_NoCrash()
        {
            var attacker = MakeAttacker();
            var damage = new Damage(10);
            damage.AddAttribute("Bludgeoning");

            // Defender == null → early-out. Pre-WS.2 there's nothing else
            // to verify, but this locks the null-safety contract.
            Assert.DoesNotThrow(() =>
                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender: null, attacker: attacker, zone: null,
                    rng: new Random(1)));
        }

        [Test]
        public void Apply_NullAttacker_NoCrash()
        {
            var defender = MakeFighter();
            var damage = new Damage(10);
            damage.AddAttribute("Cudgel");

            // Attacker == null → no SkillsPart to check → early-out.
            // (Some weapons/effects can damage entities without a clear
            // attacker; a falling rock, an environmental hazard, etc.)
            Assert.DoesNotThrow(() =>
                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender: defender, attacker: null, zone: null,
                    rng: new Random(1)));
        }

        [Test]
        public void Apply_AttackerWithoutSkillsPart_NoCrash()
        {
            // Counter-check for the most common live-game path: NPC
            // creatures don't have SkillsPart. Apply must early-out
            // silently and not exception. Without this, every NPC
            // melee swing would NRE post-WS.2.
            var defender = MakeFighter();
            var attacker = new Entity { ID = "npc" };
            attacker.AddPart(new RenderPart { DisplayName = "npc" });
            // Note: deliberately NO AddPart(new SkillsPart()) here.
            var damage = new Damage(10);
            damage.AddAttribute("Cudgel");

            Assert.DoesNotThrow(() =>
                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender: defender, attacker: attacker, zone: null,
                    rng: new Random(1)));

            // And — counter-check — no Stunned applied (since attacker
            // has no SkillsPart, no skill branches can fire).
            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Attacker without SkillsPart must NEVER apply skill-tier effects.");
        }

        [Test]
        public void Apply_ZeroActualDamage_NoEffect()
        {
            // Vetoed / fully-resisted hits gate (matches OnHitClassEffects).
            // Even with the right skill owned, actualDamage<=0 should
            // skip all effect application. Without this, fully-absorbed
            // attacks (Glowmaw vs Fire) would still stun/bleed/confuse.
            var defender = MakeFighter();
            var attacker = MakeAttackerWithSkill("Cudgel_Bludgeon");
            var damage = new Damage(10);
            damage.AddAttribute("Cudgel");

            for (int seed = 0; seed < 100; seed++)
            {
                OnHitSkillEffects.Apply(damage, actualDamage: 0,
                    defender, attacker, zone: null, rng: new Random(seed));
            }
            Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "actualDamage=0 means no skill-tier effects fire (parity " +
                "with OnHitClassEffects.Apply's same gate).");
        }

        // ====================================================================
        // WS.2 (re-tuned WSP.2 + WSP.4b) — Cudgel_Bludgeon: Cudgel-class
        // hit + skill owned → CUDGEL_BLUDGEON_CHANCE_PERCENT (50%) chance
        // to apply Stunned for a random
        // [CUDGEL_BLUDGEON_DURATION_MIN, CUDGEL_BLUDGEON_DURATION_MAX]
        // turn duration (3-4T per Qud Cudgel_Bludgeon.cs:56).
        // ====================================================================

        [Test]
        public void CudgelHit_WithBludgeonOwned_HasChance_ToApplyStunned()
        {
            // Positive: across many seeds, the 50% roll lands at least once.
            // Loop tightly bounded — at 50% chance, P(no observation in 100
            // tries) ≈ 7.9e-31. Test should always observe quickly.
            bool observed = false;
            for (int seed = 0; seed < 100 && !observed; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(Cudgel_Bludgeon));
                var damage = new Damage(10);
                damage.AddAttribute("Cudgel");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                if (defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 100 seeds, at least one Cudgel-attribute hit by an actor with " +
                $"Cudgel_Bludgeon owned should produce Stunned (chance " +
                $"{OnHitSkillEffects.CUDGEL_BLUDGEON_CHANCE_PERCENT}%). " +
                $"None observed — chance gate is broken or always rolls high.");
        }

        [Test]
        public void CudgelHit_WithoutBludgeonOwned_NeverAppliesStunned()
        {
            // Counter-check: same setup as positive, but attacker DOESN'T
            // own Cudgel_Bludgeon. The skill branch must be gated on
            // ownership; otherwise the universal Bludgeoning class hook
            // (which fires on the same swing in CombatSystem) would mask
            // this gate breaking. We isolate by calling OnHitSkillEffects
            // directly — class hook isn't fired here.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttacker();  // no skill added
                var damage = new Damage(10);
                damage.AddAttribute("Cudgel");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    $"Seed {seed}: actor without Cudgel_Bludgeon must never apply " +
                    $"Stunned via this hook — gating on SkillsPart.HasSkill is broken.");
            }
        }

        [Test]
        public void NonCudgelHit_WithBludgeonOwned_NeverAppliesStunned()
        {
            // Counter-check on the attribute side: skill IS owned, but
            // the damage doesn't carry the "Cudgel" sub-class attribute
            // (e.g. plain Bludgeoning, or Cutting). The skill branch must
            // gate on damage.HasAttribute("Cudgel") so a LongSword swing
            // by a Cudgel-trained character doesn't accidentally fire.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(Cudgel_Bludgeon));
                var damage = new Damage(10);
                damage.AddAttribute("Bludgeoning");  // class only, no sub-class

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    $"Seed {seed}: Bludgeoning-only damage (no Cudgel sub-class) " +
                    $"must not trigger Cudgel_Bludgeon — attribute gate is broken.");
            }

            // Counter-check on a different sub-class: Cutting damage with
            // the Cudgel skill owned must also never fire Cudgel_Bludgeon.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(Cudgel_Bludgeon));
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("LongBlades");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    $"Seed {seed}: Cutting/LongBlades damage with Cudgel_Bludgeon owned " +
                    $"must not fire Stunned — skill branches must be attribute-scoped.");
            }
        }

        // ====================================================================
        // WS.3 — Axe_Cleave: Axe-class hit + skill owned →
        // AXE_CLEAVE_CHANCE_PERCENT (30%) chance to deal max(1, actualDamage/2)
        // damage to one Creature entity adjacent to defender (excluding
        // attacker). Picks first creature in direction-iteration order
        // (N → NE → E → SE → S → SW → W → NW).
        // ====================================================================

        [Test]
        public void AxeHit_WithCleaveOwned_HasChance_ToDamageAdjacent()
        {
            // Positive: across many seeds, the 30% roll lands at least once.
            // P(no observation in 100 tries at 30%) ≈ 3.2e-16. Loop tightly bounded.
            bool observed = false;
            for (int seed = 0; seed < 100 && !observed; seed++)
            {
                var (zone, defender, cleaveTarget, attacker) = MakeCleaveScenario();
                int targetHpBefore = cleaveTarget.GetStatValue("Hitpoints");

                var damage = new Damage(20);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("Axe");

                OnHitSkillEffects.Apply(damage, actualDamage: 20,
                    defender, attacker, zone, rng: new Random(seed));

                int targetHpAfter = cleaveTarget.GetStatValue("Hitpoints");
                if (targetHpAfter < targetHpBefore) observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 100 seeds, an Axe-attribute hit by a Cleave-trained " +
                $"attacker should sometimes damage the adjacent Creature " +
                $"(chance {OnHitSkillEffects.AXE_CLEAVE_CHANCE_PERCENT}%). " +
                $"None observed — chance gate or adjacency lookup is broken.");
        }

        [Test]
        public void AxeHit_WithoutCleaveOwned_NeverDamagesAdjacent()
        {
            // Counter-check: same setup, but attacker doesn't own Axe_Cleave.
            // No skill branch should fire; adjacent target should never lose HP.
            for (int seed = 0; seed < 100; seed++)
            {
                var (zone, defender, cleaveTarget, attacker) = MakeCleaveScenario(skipSkill: true);
                int targetHpBefore = cleaveTarget.GetStatValue("Hitpoints");

                var damage = new Damage(20);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("Axe");

                OnHitSkillEffects.Apply(damage, actualDamage: 20,
                    defender, attacker, zone, rng: new Random(seed));

                int targetHpAfter = cleaveTarget.GetStatValue("Hitpoints");
                Assert.AreEqual(targetHpBefore, targetHpAfter,
                    $"Seed {seed}: actor without Axe_Cleave must never damage " +
                    $"adjacent Creature via this hook — gating on SkillsPart.HasSkill is broken.");
            }
        }

        [Test]
        public void NonAxeHit_WithCleaveOwned_NeverDamagesAdjacent()
        {
            // Counter-check on attribute side: Cleave skill owned, but
            // damage doesn't carry "Axe" sub-class (e.g. plain Cutting +
            // LongBlades from a LongSword). Skill must gate on
            // damage.HasAttribute("Axe") so a LongSword swing doesn't cleave.
            for (int seed = 0; seed < 100; seed++)
            {
                var (zone, defender, cleaveTarget, attacker) = MakeCleaveScenario();
                int targetHpBefore = cleaveTarget.GetStatValue("Hitpoints");

                var damage = new Damage(20);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("LongBlades");  // not Axe

                OnHitSkillEffects.Apply(damage, actualDamage: 20,
                    defender, attacker, zone, rng: new Random(seed));

                int targetHpAfter = cleaveTarget.GetStatValue("Hitpoints");
                Assert.AreEqual(targetHpBefore, targetHpAfter,
                    $"Seed {seed}: Cutting/LongBlades damage must not cleave — " +
                    $"Axe_Cleave must gate on the Axe sub-class attribute.");
            }
        }

        [Test]
        public void AxeCleave_WithNoAdjacent_NoOps_StillSucceedsRoll()
        {
            // Edge case: skill owned + Axe attribute + roll passes, but
            // no adjacent Creature exists. The cleave roll succeeds but
            // no damage is applied (silent no-op rather than NRE). Without
            // this, sparse zones would crash on every successful Cleave roll.
            for (int seed = 0; seed < 50; seed++)
            {
                var zone = new Zone();
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(Axe_Cleave));
                zone.AddEntity(attacker, 5, 5);
                zone.AddEntity(defender, 6, 5);
                // Note: NO adjacent Creature to defender.

                var damage = new Damage(20);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("Axe");

                Assert.DoesNotThrow(() =>
                    OnHitSkillEffects.Apply(damage, actualDamage: 20,
                        defender, attacker, zone, rng: new Random(seed)),
                    $"Seed {seed}: cleave with no adjacent target must no-op silently.");
            }
        }

        // ====================================================================
        // WS.4 — LongBlades_Lacerate: LongBlades-attribute hit + skill
        // owned → LONGBLADES_LACERATE_CHANCE_PERCENT (35%) chance to
        // apply BleedingEffect with stronger dice (1d3 vs class hook's 1d2).
        // ====================================================================

        [Test]
        public void LongBladesHit_WithLacerateOwned_HasChance_ToApplyBleeding()
        {
            bool observed = false;
            for (int seed = 0; seed < 100 && !observed; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(LongBlades_Lacerate));
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("LongBlades");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                if (defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 100 seeds, a LongBlades-attribute hit by an actor with " +
                $"LongBlades_Lacerate owned should produce Bleeding (chance " +
                $"{OnHitSkillEffects.LONGBLADES_LACERATE_CHANCE_PERCENT}%).");
        }

        [Test]
        public void LongBladesHit_WithoutLacerateOwned_NeverAppliesBleeding()
        {
            // Counter-check on ownership.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttacker();  // no skill
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("LongBlades");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: actor without LongBlades_Lacerate must not apply Bleeding.");
            }
        }

        [Test]
        public void NonLongBladesHit_WithLacerateOwned_NeverAppliesBleeding()
        {
            // Counter-check on attribute. A pure Cutting + Axe hit
            // (e.g. Battleaxe) by a Lacerate-trained character must not
            // fire LongBlades_Lacerate — the gate is the LongBlades
            // sub-class, not the broader Cutting class.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(LongBlades_Lacerate));
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("Axe");  // not LongBlades

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: Cutting/Axe damage must not fire Lacerate — " +
                    $"skill must gate on LongBlades sub-class attribute.");
            }
        }

        // ====================================================================
        // WSP.3 — ShortBlades_Bloodletter: Piercing-attribute hit + skill
        // owned → SHORTBLADES_BLOODLETTER_CHANCE_PERCENT (50%) chance to
        // apply BleedingEffect with light dice ("1d2"). Stacks with
        // ShortBlades_Jab + ShortBladesSkill crit-Bleed on the same hit.
        // ====================================================================

        [Test]
        public void PiercingHit_WithBloodletterOwned_HasChance_ToApplyBleeding()
        {
            bool observed = false;
            for (int seed = 0; seed < 50 && !observed; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(ShortBlades_Bloodletter));
                var damage = new Damage(10);
                damage.AddAttribute("Piercing");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                if (defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 50 seeds, a Piercing hit by an actor with " +
                $"ShortBlades_Bloodletter owned should produce Bleeding " +
                $"(chance {OnHitSkillEffects.SHORTBLADES_BLOODLETTER_CHANCE_PERCENT}%).");
        }

        [Test]
        public void PiercingHit_WithoutBloodletterOwned_NeverAppliesBleeding()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttacker();  // no skills
                var damage = new Damage(10);
                damage.AddAttribute("Piercing");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: actor without ShortBlades_Bloodletter must not apply Bleeding.");
            }
        }

        [Test]
        public void NonPiercingHit_WithBloodletterOwned_NeverAppliesBleeding()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(ShortBlades_Bloodletter));
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("LongBlades");  // not Piercing

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: Cutting/LongBlades damage must not fire Bloodletter — " +
                    $"skill must gate on the Piercing class.");
            }
        }

        // ====================================================================
        // WSP.2 (re-tightened WSP.4b) — Cudgel_Bludgeon re-tune to
        // Qud-verbatim values: 50% chance per Cudgel hit, random 3-4T
        // duration per Qud Cudgel_Bludgeon.cs:56 (Stat.Random(3, 4)).
        // Pin the constants + the range across many seeds.
        // ====================================================================

        [Test]
        public void CudgelBludgeon_DurationIsRandomWithinRange()
        {
            // Across many seeds, the random 3-4T duration must produce
            // both 3 and 4 as the min/max observations. If the range is
            // accidentally clamped (e.g. only ever 3, regressing to the
            // pre-WSP.2 fixed-3T behavior), this test fails.
            int minObserved = int.MaxValue;
            int maxObserved = int.MinValue;
            int observed = 0;
            for (int seed = 0; seed < 300; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(Cudgel_Bludgeon));
                var damage = new Damage(10);
                damage.AddAttribute("Cudgel");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                var stun = defender.GetPart<StatusEffectsPart>().GetEffect<StunnedEffect>();
                if (stun != null)
                {
                    observed++;
                    if (stun.Duration < minObserved) minObserved = stun.Duration;
                    if (stun.Duration > maxObserved) maxObserved = stun.Duration;
                }
            }
            Assert.GreaterOrEqual(observed, 1,
                $"Across 300 seeds, expected at least one Cudgel_Bludgeon Stun. " +
                $"None observed — chance gate is broken.");
            Assert.LessOrEqual(minObserved, OnHitSkillEffects.CUDGEL_BLUDGEON_DURATION_MIN,
                $"Min Cudgel_Bludgeon Stun duration must reach " +
                $"{OnHitSkillEffects.CUDGEL_BLUDGEON_DURATION_MIN}. Observed: {minObserved}.");
            Assert.GreaterOrEqual(maxObserved, OnHitSkillEffects.CUDGEL_BLUDGEON_DURATION_MAX,
                $"Max Cudgel_Bludgeon Stun duration must reach " +
                $"{OnHitSkillEffects.CUDGEL_BLUDGEON_DURATION_MAX}. Observed: {maxObserved}.");
        }

        // ====================================================================
        // WSP.1 — Tree-root crit behaviors. Each tree-root grants a
        // forced (no chance roll) effect when a critical hit lands with
        // a matching weapon class. Mirrors Qud's WeaponMadeCriticalHit
        // overrides on Cudgel/Axe/LongBlades/ShortBlades tree-roots.
        // ====================================================================

        // ── CudgelSkill on crit: random 1-4T Stunned ─────────────────────────

        [Test]
        public void CudgelCrit_WithCudgelSkillOwned_AlwaysAppliesStunned()
        {
            // Force-apply (no chance roll) — every crit must produce Stunned.
            // Unlike the gated Cudgel_Bludgeon, the tree-root fires
            // unconditionally on Cudgel-attribute Critical hits.
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(CudgelSkill));
                var damage = new Damage(10);
                damage.AddAttribute("Cudgel");
                damage.AddAttribute("Critical");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsTrue(
                    defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    $"Seed {seed}: CudgelSkill on Cudgel-Critical hit must always apply Stunned.");
            }
        }

        [Test]
        public void CudgelCrit_DurationIsRandom1To4()
        {
            // Range check across many seeds: at least one duration of 1
            // and at least one of 4 must be observed (uniform across seeds).
            // Lower bound: 1; upper bound: 4. Per Qud's Stat.Random(1, 4).
            int minObserved = int.MaxValue;
            int maxObserved = int.MinValue;
            for (int seed = 0; seed < 200; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(CudgelSkill));
                var damage = new Damage(10);
                damage.AddAttribute("Cudgel");
                damage.AddAttribute("Critical");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                var stun = defender.GetPart<StatusEffectsPart>().GetEffect<StunnedEffect>();
                if (stun != null)
                {
                    if (stun.Duration < minObserved) minObserved = stun.Duration;
                    if (stun.Duration > maxObserved) maxObserved = stun.Duration;
                }
            }
            Assert.LessOrEqual(minObserved, 1,
                $"Across 200 seeds, the minimum CudgelSkill crit Stun duration " +
                $"should hit 1. Observed minimum: {minObserved}.");
            Assert.GreaterOrEqual(maxObserved, 4,
                $"Across 200 seeds, the maximum should hit 4. Observed maximum: {maxObserved}.");
        }

        [Test]
        public void NonCriticalCudgelHit_WithCudgelSkillOwned_DoesNotForceStun()
        {
            // Counter-check: same setup but no Critical attribute. The
            // gated Cudgel_Bludgeon power isn't owned in this fixture
            // (only the tree-root), so Stunned should be very rare —
            // observe 0 across many seeds.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(CudgelSkill));
                var damage = new Damage(10);
                damage.AddAttribute("Cudgel");
                // NO "Critical" attribute

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(
                    defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    $"Seed {seed}: CudgelSkill (without Cudgel_Bludgeon) on a non-crit " +
                    $"Cudgel hit must not stun — gate is the Critical attribute.");
            }
        }

        // ── AxeSkill on crit: force-cleave at 100% ───────────────────────────

        [Test]
        public void AxeCrit_WithAxeSkillOwned_AlwaysCleavesAdjacent()
        {
            // Force-cleave on Axe Critical. Unlike Axe_Cleave's 30% gated
            // chance, the tree-root tries cleave on every crit.
            for (int seed = 0; seed < 50; seed++)
            {
                var (zone, defender, cleaveTarget, attacker) =
                    MakeCleaveScenario(skipSkill: true);
                attacker.GetPart<SkillsPart>().AddSkill(nameof(AxeSkill), source: "test");
                int targetHpBefore = cleaveTarget.GetStatValue("Hitpoints");

                var damage = new Damage(20);
                damage.AddAttribute("Axe");
                damage.AddAttribute("Cutting");
                damage.AddAttribute("Critical");

                OnHitSkillEffects.Apply(damage, actualDamage: 20,
                    defender, attacker, zone, rng: new Random(seed));

                int targetHpAfter = cleaveTarget.GetStatValue("Hitpoints");
                Assert.Less(targetHpAfter, targetHpBefore,
                    $"Seed {seed}: AxeSkill on Axe-Critical hit must always cleave.");
            }
        }

        [Test]
        public void NonCriticalAxeHit_WithAxeSkillOwnedOnly_DoesNotCleave()
        {
            // Counter-check: same setup, no Critical attribute, Axe_Cleave
            // (the gated power) NOT owned. Must observe 0 cleaves across seeds.
            for (int seed = 0; seed < 50; seed++)
            {
                var (zone, defender, cleaveTarget, attacker) =
                    MakeCleaveScenario(skipSkill: true);
                attacker.GetPart<SkillsPart>().AddSkill(nameof(AxeSkill), source: "test");
                int targetHpBefore = cleaveTarget.GetStatValue("Hitpoints");

                var damage = new Damage(20);
                damage.AddAttribute("Axe");
                damage.AddAttribute("Cutting");
                // NO "Critical"

                OnHitSkillEffects.Apply(damage, actualDamage: 20,
                    defender, attacker, zone, rng: new Random(seed));

                int targetHpAfter = cleaveTarget.GetStatValue("Hitpoints");
                Assert.AreEqual(targetHpBefore, targetHpAfter,
                    $"Seed {seed}: AxeSkill on a non-crit Axe hit (without Axe_Cleave) " +
                    $"must not cleave — tree-root crit gate must hold.");
            }
        }

        // ── LongBladesSkill on crit: force Bleed "1d4" ───────────────────────

        [Test]
        public void LongBladesCrit_WithLongBladesSkillOwned_AlwaysAppliesBleeding()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(LongBladesSkill));
                var damage = new Damage(10);
                damage.AddAttribute("LongBlades");
                damage.AddAttribute("Cutting");
                damage.AddAttribute("Critical");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsTrue(
                    defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: LongBladesSkill on LongBlades-Critical hit must always Bleed.");
            }
        }

        [Test]
        public void NonCriticalLongBladesHit_WithLongBladesSkillOwnedOnly_DoesNotForceBleed()
        {
            // Counter-check on the Critical-attribute gate.
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(LongBladesSkill));
                var damage = new Damage(10);
                damage.AddAttribute("LongBlades");
                damage.AddAttribute("Cutting");
                // NO "Critical"

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(
                    defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: LongBladesSkill (without Lacerate) on a non-crit " +
                    $"LongBlades hit must not Bleed — gate is Critical.");
            }
        }

        // ── ShortBladesSkill on crit: force Bleed "1d2" ──────────────────────

        [Test]
        public void ShortBladesCrit_WithShortBladesSkillOwned_AlwaysAppliesBleeding()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(ShortBladesSkill));
                var damage = new Damage(10);
                damage.AddAttribute("Piercing");
                damage.AddAttribute("Critical");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsTrue(
                    defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: ShortBladesSkill on Piercing-Critical hit must always Bleed.");
            }
        }

        [Test]
        public void NonCriticalPiercingHit_WithShortBladesSkillOwnedOnly_DoesNotForceBleed()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(ShortBladesSkill));
                var damage = new Damage(10);
                damage.AddAttribute("Piercing");
                // NO "Critical"

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(
                    defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: ShortBladesSkill on a non-crit Piercing hit must " +
                    $"not Bleed — Jab is the gated power, ShortBladesSkill needs Critical.");
            }
        }

        // ====================================================================
        // WS.6b cold-eye 🟡 #2 — stacking integration: verify that calling
        // OnHitClassEffects.Apply AND OnHitSkillEffects.Apply in sequence
        // (the order CombatSystem.PerformSingleAttack uses) on the SAME
        // hit can produce a Stunned effect with summed duration. The plan
        // claimed this stacks via StunnedEffect.OnStack += Duration; this
        // test pins the claim end-to-end.
        // ====================================================================

        [Test]
        public void Stacking_ClassHookPlusSkillHook_OnSameMaceHit_CanSumDurations()
        {
            // Mace = "Bludgeoning Cudgel" (both attributes). Class hook fires
            // on Bludgeoning at 15% for 2T; skill hook fires on Cudgel at
            // 50% for 3-4T. StunnedEffect.OnStack does Duration += incoming.Duration,
            // so both rolls landing on the same hit produces Duration = 5-6.
            //
            // Across many seeds, observe at least one case where final
            // Stunned.Duration > 4. That's only achievable when BOTH hooks
            // fired (skill alone caps at 4, class alone caps at 2).
            int maxObservedDuration = 0;
            int observedBothFiredCount = 0;
            for (int seed = 0; seed < 500; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(Cudgel_Bludgeon));
                var damage = new Damage(10);
                damage.AddAttribute("Bludgeoning");  // class hook gate
                damage.AddAttribute("Cudgel");       // skill hook gate

                // Same RNG instance threaded through both Apply calls,
                // mirroring how CombatSystem.PerformSingleAttack does it.
                var rng = new Random(seed);
                OnHitClassEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng);
                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng);

                var stun = defender.GetPart<StatusEffectsPart>().GetEffect<StunnedEffect>();
                if (stun != null)
                {
                    if (stun.Duration > maxObservedDuration)
                        maxObservedDuration = stun.Duration;
                    // Skill alone caps at 4 (Cudgel_Bludgeon's 3-4T max);
                    // class alone caps at 2. Duration > 4 is the
                    // unambiguous "both fired" tracer.
                    if (stun.Duration > 4) observedBothFiredCount++;
                }
            }

            Assert.Greater(maxObservedDuration, 4,
                $"Across 500 seeds, expected at least one case where both class " +
                $"AND skill hooks fired on the same Mace hit (final Duration > 4). " +
                $"Highest Duration observed: {maxObservedDuration}. If this stays " +
                $"≤ 4, either OnHitClassEffects or OnHitSkillEffects didn't fire " +
                $"its branch — stacking is broken.");
            Assert.Greater(observedBothFiredCount, 0,
                $"Observed {observedBothFiredCount} 'both fired' events; expected " +
                $"at least 1 across 500 seeds. P(both fire) ≈ 7.5% per seed.");
        }

        // ====================================================================
        // WSP.4b cold-eye 🧪 #5 — 3-hook stacking integration: Mace crit
        // by an actor owning CudgelSkill (tree-root) + Cudgel_Bludgeon
        // (gated power) runs THREE Stun rolls on the same swing —
        // OnHitClassEffects (15% / 2T), OnHitSkillEffects Cudgel_Bludgeon
        // (50% / 3-4T), OnHitSkillEffects CudgelSkill crit (100% / 1-4T).
        // Worst case: 2 + 4 + 4 = 10T. Plan flagged this as a design risk
        // ("watch worst-case Stun feel"). This test pins the upper bound.
        // ====================================================================

        [Test]
        public void Stacking_AllThreeHooks_OnMaceCrit_ProducesUpToTenTurnStun()
        {
            int maxObservedDuration = 0;
            int observedAllThreeFiredCount = 0;
            for (int seed = 0; seed < 500; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(Cudgel_Bludgeon));
                attacker.GetPart<SkillsPart>().AddSkill(nameof(CudgelSkill), source: "test");

                var damage = new Damage(10);
                damage.AddAttribute("Bludgeoning");  // class hook gate
                damage.AddAttribute("Cudgel");       // skill hook gate
                damage.AddAttribute("Critical");     // tree-root crit gate

                var rng = new Random(seed);
                OnHitClassEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng);
                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng);

                var stun = defender.GetPart<StatusEffectsPart>().GetEffect<StunnedEffect>();
                if (stun != null)
                {
                    if (stun.Duration > maxObservedDuration)
                        maxObservedDuration = stun.Duration;
                    // Tree-root crit always fires (100% on Critical).
                    // Cudgel_Bludgeon at 50%, class at 15%. P(all 3) ≈ 7.5%.
                    // Skill+crit alone caps at 4+4=8; class+crit caps at 2+4=6.
                    // All three caps at 2+4+4=10 — Duration > 8 means all 3 fired.
                    if (stun.Duration > 8) observedAllThreeFiredCount++;
                }
            }

            // Always at least the crit Stun (1-4T). At minimum we observe
            // a Stun every seed since Critical → CudgelSkill always fires.
            Assert.GreaterOrEqual(maxObservedDuration, 4,
                $"Critical CudgelSkill must always apply Stun. " +
                $"Max observed: {maxObservedDuration}.");
            // 3-hook upper bound: across 500 seeds, P(all 3 fire) ≈ 7.5% so
            // ~37 cases expected. Asserting ≥ 1 is conservative.
            Assert.Greater(observedAllThreeFiredCount, 0,
                $"Across 500 seeds, expected at least one all-3-hooks-fire " +
                $"event (Duration > 8). Observed: {observedAllThreeFiredCount}. " +
                $"If 0, OnHitClassEffects/Bludgeon/CudgelSkill-crit aren't all wired.");
            // Hard upper bound: theoretical max is 2+4+4 = 10T. Anything
            // above that means an extra effect snuck in (or OnStack is bugged).
            Assert.LessOrEqual(maxObservedDuration, 10,
                $"Worst-case 3-hook Stun must be ≤ 10T " +
                $"(2T class + 4T Bludgeon-max + 4T crit-max). " +
                $"Observed max: {maxObservedDuration}.");
        }

        // ====================================================================
        // WS.5 — ShortBlades_Jab: Piercing-attribute hit + skill owned →
        // SHORTBLADES_JAB_CHANCE_PERCENT (30%) chance to apply Confused
        // for SHORTBLADES_JAB_DURATION (3) turns. Stacks with class hook.
        // ====================================================================

        [Test]
        public void PiercingHit_WithJabOwned_HasChance_ToApplyConfused()
        {
            bool observed = false;
            for (int seed = 0; seed < 100 && !observed; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(ShortBlades_Jab));
                var damage = new Damage(10);
                damage.AddAttribute("Piercing");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                if (defender.GetPart<StatusEffectsPart>().HasEffect<ConfusedEffect>())
                    observed = true;
            }
            Assert.IsTrue(observed,
                $"Across 100 seeds, a Piercing-attribute hit by a Jab-trained " +
                $"actor should produce Confused (chance " +
                $"{OnHitSkillEffects.SHORTBLADES_JAB_CHANCE_PERCENT}%).");
        }

        [Test]
        public void PiercingHit_WithoutJabOwned_NeverAppliesConfused()
        {
            // Counter-check on ownership.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttacker();  // no skill
                var damage = new Damage(10);
                damage.AddAttribute("Piercing");

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<ConfusedEffect>(),
                    $"Seed {seed}: actor without ShortBlades_Jab must never apply Confused.");
            }
        }

        [Test]
        public void NonPiercingHit_WithJabOwned_NeverAppliesConfused()
        {
            // Counter-check on attribute. A Bludgeoning + Cudgel hit
            // (e.g. Mace) by a Jab-trained character must not fire Jab —
            // the gate is the Piercing damage class.
            for (int seed = 0; seed < 100; seed++)
            {
                var defender = MakeFighter();
                var attacker = MakeAttackerWithSkill(nameof(ShortBlades_Jab));
                var damage = new Damage(10);
                damage.AddAttribute("Bludgeoning");
                damage.AddAttribute("Cudgel");  // not Piercing

                OnHitSkillEffects.Apply(damage, actualDamage: 10,
                    defender, attacker, zone: null, rng: new Random(seed));

                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<ConfusedEffect>(),
                    $"Seed {seed}: Bludgeoning/Cudgel damage must not fire Jab — " +
                    $"skill must gate on Piercing class.");
            }
        }

        // Helper: build a 3-entity scene for cleave tests.
        // Layout (zone coords; player at center cleaves to NE adjacent):
        //   . . .
        //   . D T   ← defender at (6,5), cleave target at (7,5)
        //   . A .   ← attacker at (5,6) — diagonal to defender
        //
        // The cleave-target T is adjacent to defender D in direction E (idx 2).
        // First-found-Creature-in-direction-order picks T deterministically.
        private static (Zone zone, Entity defender, Entity cleaveTarget, Entity attacker)
            MakeCleaveScenario(bool skipSkill = false)
        {
            var zone = new Zone();
            var defender = MakeFighter();
            var cleaveTarget = MakeFighter();
            cleaveTarget.ID = "cleaveTarget";
            var attacker = skipSkill ? MakeAttacker() : MakeAttackerWithSkill(nameof(Axe_Cleave));
            attacker.Tags["Creature"] = "";  // so attacker is excluded from cleave-pick correctly

            zone.AddEntity(attacker, 5, 6);
            zone.AddEntity(defender, 6, 5);
            zone.AddEntity(cleaveTarget, 7, 5);

            return (zone, defender, cleaveTarget, attacker);
        }

        // ─────────────────────────────────────────────────────────────────
        // Test fixtures
        // ─────────────────────────────────────────────────────────────────

        private static Entity MakeFighter()
        {
            var e = new Entity { ID = "fighter" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "fighter" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity MakeAttacker()
        {
            var e = new Entity { ID = "attacker" };
            e.AddPart(new RenderPart { DisplayName = "attacker" });
            e.AddPart(new SkillsPart());
            return e;
        }

        /// <summary>
        /// Helper for per-skill behavior tests (WS.2+). Builds an attacker
        /// with the named skill in their SkillsPart so OnHitSkillEffects
        /// can branch on it. Pre-WS.2 the skill class doesn't exist yet,
        /// so callers using this helper must wait for the matching WS.
        /// Uses the string-class AddSkill overload to dodge the C# class
        /// dependency until the per-skill stubs land.
        /// </summary>
        private static Entity MakeAttackerWithSkill(string skillClass)
        {
            var e = MakeAttacker();
            // Cold-eye 🧪 #5 fix: Assert the AddSkill succeeded so a typo
            // in the test (e.g. nameof misspelled as a literal string) fails
            // loud. Without this, a typo'd test would silently produce an
            // attacker WITHOUT the skill, and counter-check tests would pass
            // for the wrong reason ("no skill owned" rather than "gate
            // upheld").
            bool added = e.GetPart<SkillsPart>().AddSkill(skillClass, source: "test");
            Assert.IsTrue(added,
                $"MakeAttackerWithSkill: AddSkill('{skillClass}') returned false. " +
                $"Likely a typo or the C# class doesn't exist yet (missing WS milestone?).");
            return e;
        }
    }
}
