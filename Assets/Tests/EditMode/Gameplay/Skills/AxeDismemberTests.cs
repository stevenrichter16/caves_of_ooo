using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP6.12 — Axe_Dismember passive tests.
    ///
    /// <para>Coverage:
    /// <list type="bullet">
    ///   <item>Positive: across many seeds at 3% chance, eventually
    ///         observe a non-Mortal severable body part dismembered AND
    ///         BleedingEffect applied to the defender.</item>
    ///   <item>Counter-checks: non-Axe damage / zero actualDamage /
    ///         no-Body defender / null Rng / null Defender.</item>
    ///   <item>Mortal-skip: defender with only Mortal-severable parts
    ///         (head/heart-only stub) never sees this passive
    ///         dismember anything — that's <c>Axe_Decapitate</c>'s job.</item>
    /// </list></para>
    /// </summary>
    public class AxeDismemberTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers (mirror SkillSystemTier2Tests) ───────────────

        private static Entity MakeBodiedDefender(string name = "defender", int hp = 200)
        {
            // High HP so a wave of dismemberments doesn't kill the defender
            // before we see the proc fire — the seed loop needs a stable
            // target across many iterations.
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat
                { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new StatusEffectsPart());
            var body = new Body();
            e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        private static Entity MakeAttackerWithSkill(BaseSkillPart skill)
        {
            var e = new Entity { ID = "attacker" };
            e.AddPart(new RenderPart { DisplayName = "attacker" });
            e.AddPart(new SkillsPart());
            Assert.IsTrue(e.GetPart<SkillsPart>().AddSkill(skill, source: "test"));
            return e;
        }

        private static SkillEventContext MakeHitContext(Entity attacker, Entity defender,
            int seed, int actualDamage = 10, params string[] damageAttrs)
        {
            var damage = new Damage(actualDamage);
            foreach (var a in damageAttrs) damage.AddAttribute(a);
            return new SkillEventContext
            {
                Attacker = attacker, Defender = defender,
                Damage = damage, ActualDamage = actualDamage,
                Zone = null, Rng = new Random(seed),
            };
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: 3% across many seeds — eventually observe a dismember
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Dismember_OnAxeHit_HasChance_ToDismemberAcrossSeeds()
        {
            // 3% rate across 5000 seeds → expected ~150 procs. Robust
            // floor: at least one observation. Each seed needs a fresh
            // defender (dismember is destructive — once a part is gone,
            // re-applying does nothing on the same body).
            var skill = new Axe_Dismember();
            var actor = MakeAttackerWithSkill(skill);

            bool everDismembered = false;
            bool everBled = false;
            for (int seed = 0; seed < 5000 && (!everDismembered || !everBled); seed++)
            {
                var defender = MakeBodiedDefender($"defender_{seed}");
                int partsBefore = defender.GetPart<Body>().GetParts().Count;
                var ctx = MakeHitContext(actor, defender, seed, actualDamage: 10, "Axe");
                skill.OnAttackerAfterAttack(ctx);

                int partsAfter = defender.GetPart<Body>().GetParts().Count;
                if (partsAfter < partsBefore) everDismembered = true;
                if (defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>())
                    everBled = true;
            }

            Assert.IsTrue(everDismembered,
                "Across 5000 seeds at 3% chance, Dismember should produce at least one " +
                "dismemberment of a non-Mortal severable part. Got zero.");
            Assert.IsTrue(everBled,
                "Every successful Dismember proc must also apply BleedingEffect. " +
                "Across 5000 seeds, no Bleeding observed.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: non-Axe damage doesn't fire (1000 seeds zero)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Dismember_OnNonAxeHit_NeverFires()
        {
            var skill = new Axe_Dismember();
            var actor = MakeAttackerWithSkill(skill);

            for (int seed = 0; seed < 1000; seed++)
            {
                var defender = MakeBodiedDefender($"defender_{seed}");
                int partsBefore = defender.GetPart<Body>().GetParts().Count;
                var ctx = MakeHitContext(actor, defender, seed, actualDamage: 10, "Cutting", "LongBlades");
                skill.OnAttackerAfterAttack(ctx);

                int partsAfter = defender.GetPart<Body>().GetParts().Count;
                Assert.AreEqual(partsBefore, partsAfter,
                    $"Seed {seed}: Cutting/LongBlades hit must NOT trigger Axe_Dismember. " +
                    $"Parts before: {partsBefore}, after: {partsAfter}.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: zero actualDamage (vetoed/fully-resisted hit)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Dismember_OnZeroDamageHit_NeverFires()
        {
            // A hit with the Axe attribute but actualDamage=0 (e.g. fully
            // resisted by Stoneskin or vetoed by BeforeTakeDamage). The
            // proc should bail before rolling — corpses don't bleed and
            // unaffected targets shouldn't lose limbs.
            var skill = new Axe_Dismember();
            var actor = MakeAttackerWithSkill(skill);

            for (int seed = 0; seed < 200; seed++)
            {
                var defender = MakeBodiedDefender($"defender_{seed}");
                int partsBefore = defender.GetPart<Body>().GetParts().Count;
                var ctx = MakeHitContext(actor, defender, seed, actualDamage: 0, "Axe");
                skill.OnAttackerAfterAttack(ctx);

                int partsAfter = defender.GetPart<Body>().GetParts().Count;
                Assert.AreEqual(partsBefore, partsAfter,
                    $"Seed {seed}: zero-damage Axe hit must NOT trigger Axe_Dismember.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Defense-in-depth: defender without a Body part
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Dismember_DefenderWithoutBody_NoOps_NoCrash()
        {
            // Some creatures (e.g. swarms, environmental hazards) might
            // lack a Body Part. Dismember must early-out gracefully
            // instead of NREing on body.GetParts().
            var skill = new Axe_Dismember();
            var actor = MakeAttackerWithSkill(skill);

            for (int seed = 0; seed < 100; seed++)
            {
                var defender = new Entity { ID = "bodyless" };
                defender.Tags["Creature"] = "";
                defender.AddPart(new RenderPart { DisplayName = "bodyless" });
                defender.AddPart(new StatusEffectsPart());
                // NO Body part attached.

                var ctx = MakeHitContext(actor, defender, seed, actualDamage: 10, "Axe");
                Assert.DoesNotThrow(() => skill.OnAttackerAfterAttack(ctx),
                    $"Seed {seed}: Dismember on body-less defender must not throw.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Defense-in-depth: null Defender / null Rng
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Dismember_NullDefender_NoCrash()
        {
            var skill = new Axe_Dismember();
            var actor = MakeAttackerWithSkill(skill);
            var damage = new Damage(10);
            damage.AddAttribute("Axe");
            var ctx = new SkillEventContext
            {
                Attacker = actor, Defender = null,
                Damage = damage, ActualDamage = 10,
                Rng = new Random(0),
            };
            Assert.DoesNotThrow(() => skill.OnAttackerAfterAttack(ctx),
                "Null Defender must not crash Axe_Dismember.");
        }

        [Test]
        public void Dismember_NullRng_NoCrash()
        {
            var skill = new Axe_Dismember();
            var actor = MakeAttackerWithSkill(skill);
            var defender = MakeBodiedDefender();
            var damage = new Damage(10);
            damage.AddAttribute("Axe");
            var ctx = new SkillEventContext
            {
                Attacker = actor, Defender = defender,
                Damage = damage, ActualDamage = 10,
                Rng = null,
            };
            Assert.DoesNotThrow(() => skill.OnAttackerAfterAttack(ctx),
                "Null Rng must not crash Axe_Dismember.");
            // Confirm no dismemberment occurred either.
            int partsAfter = defender.GetPart<Body>().GetParts().Count;
            Assert.Greater(partsAfter, 0,
                "Defender body should still be intact after a null-Rng call.");
        }

        // ════════════════════════════════════════════════════════════════
        // Mortal-skip: defender with only Mortal severable parts
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Dismember_OnlyMortalSeverableParts_NoDismember()
        {
            // Construct a stub defender whose ONLY severable parts are
            // Mortal (Head). Per the skill's contract, those are
            // skipped — that's Axe_Decapitate's job. Across many seeds
            // at the inflated chance, no dismember should fire.
            //
            // Rationale: the "skip Mortal" branch in Axe_Dismember is
            // important because without it a single 3% proc could
            // instakill any humanoid by removing the Head. Decapitate
            // is the gated active-toggle that allows that.
            var skill = new Axe_Dismember();
            var actor = MakeAttackerWithSkill(skill);

            for (int seed = 0; seed < 200; seed++)
            {
                var defender = MakeMortalOnlyDefender($"defender_{seed}");
                int partsBefore = defender.GetPart<Body>().GetParts().Count;
                var ctx = MakeHitContext(actor, defender, seed, actualDamage: 10, "Axe");
                skill.OnAttackerAfterAttack(ctx);

                int partsAfter = defender.GetPart<Body>().GetParts().Count;
                Assert.AreEqual(partsBefore, partsAfter,
                    $"Seed {seed}: defender with only Mortal severable parts must not be dismembered by Axe_Dismember. " +
                    $"(That's Axe_Decapitate's job.) Parts before: {partsBefore}, after: {partsAfter}.");
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: no Bleeding should be applied when no part was actually dismembered.");
            }
        }

        /// <summary>
        /// Build a stub defender with only a body root + one Mortal
        /// severable part (Head). Used by the Mortal-skip counter-check
        /// to verify Axe_Dismember bails when the only candidates would
        /// be Decapitate territory.
        /// </summary>
        private static Entity MakeMortalOnlyDefender(string name)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 200, Min = 0, Max = 200 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat
                { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new StatusEffectsPart());

            // Custom body: root (non-severable) + Head (Mortal, severable).
            // No arms/legs/etc. — so no non-Mortal severable candidates exist.
            var body = new Body();
            e.AddPart(body);
            var root = new BodyPart
            {
                Type = "Body", Description = "Body",
                Mortal = false, Appendage = false, Integral = true,
                Parts = new System.Collections.Generic.List<BodyPart>(),
            };
            var head = new BodyPart
            {
                Type = "Head", Description = "head",
                Mortal = true, Appendage = true, Integral = false,
                ParentPart = root,
                Parts = new System.Collections.Generic.List<BodyPart>(),
            };
            root.Parts.Add(head);
            body.SetBody(root);
            return e;
        }
    }
}
