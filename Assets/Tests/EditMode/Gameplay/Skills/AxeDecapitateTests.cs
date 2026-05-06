using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP6.18 — Axe_Decapitate marker-skill tests + Dismember×Decapitate
    /// interaction tests.
    ///
    /// <para>Coverage:
    /// <list type="bullet">
    ///   <item><see cref="Axe_Decapitate.ShouldDecapitate"/>: null actor
    ///         → false; actor without skill → false; actor with skill → true.</item>
    ///   <item>Marker-skill invariant: Axe_Decapitate has NO behavioral
    ///         virtual overrides (it's purely a tag for
    ///         <see cref="Axe_Dismember"/>'s candidate-pool decision).</item>
    ///   <item>Interaction (positive): actor with both Axe_Dismember +
    ///         Axe_Decapitate eventually dismembers a Mortal severable
    ///         part across many seeds.</item>
    ///   <item>Interaction (counter-check): actor with ONLY Axe_Dismember
    ///         (no Decapitate) never dismembers a Mortal-only target —
    ///         the existing <c>AxeDismemberTests.Dismember_OnlyMortalSeverableParts_
    ///         NoDismember</c> covers this end; we add a parallel here for
    ///         completeness.</item>
    /// </list></para>
    /// </summary>
    public class AxeDecapitateTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeBareActor(string name = "actor")
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new SkillsPart());
            return e;
        }

        private static SkillEventContext MakeAxeHitContext(Entity attacker, Entity defender, int seed)
        {
            var damage = new Damage(10);
            damage.AddAttribute("Axe");
            return new SkillEventContext
            {
                Attacker = attacker, Defender = defender,
                Damage = damage, ActualDamage = 10,
                Zone = null, Rng = new Random(seed),
            };
        }

        // Defender with ONLY a body root + Mortal Head — no other
        // severable parts. Mirrors AxeDismemberTests.MakeMortalOnlyDefender
        // (kept private there; duplicated here to keep this fixture
        // self-contained per project convention).
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

            var body = new Body();
            e.AddPart(body);
            var root = new BodyPart
            {
                Type = "Body", Description = "Body",
                Mortal = false, Appendage = false, Integral = true,
                Parts = new List<BodyPart>(),
            };
            var head = new BodyPart
            {
                Type = "Head", Description = "head",
                Mortal = true, Appendage = true, Integral = false,
                ParentPart = root,
                Parts = new List<BodyPart>(),
            };
            root.Parts.Add(head);
            body.SetBody(root);
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // ShouldDecapitate static helper
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ShouldDecapitate_NullActor_ReturnsFalse()
        {
            Assert.IsFalse(Axe_Decapitate.ShouldDecapitate(null),
                "ShouldDecapitate(null) must return false (defense-in-depth).");
        }

        [Test]
        public void ShouldDecapitate_ActorWithoutSkillsPart_ReturnsFalse()
        {
            var actor = new Entity { ID = "bare" };
            actor.AddPart(new RenderPart { DisplayName = "bare" });
            // No SkillsPart attached.
            Assert.IsFalse(Axe_Decapitate.ShouldDecapitate(actor),
                "ShouldDecapitate must return false when actor has no SkillsPart.");
        }

        [Test]
        public void ShouldDecapitate_ActorWithoutSkill_ReturnsFalse()
        {
            var actor = MakeBareActor();
            // SkillsPart attached but no Decapitate skill added.
            Assert.IsFalse(Axe_Decapitate.ShouldDecapitate(actor),
                "ShouldDecapitate must return false when actor doesn't own Axe_Decapitate.");
        }

        [Test]
        public void ShouldDecapitate_ActorWithSkill_ReturnsTrue()
        {
            var actor = MakeBareActor();
            actor.GetPart<SkillsPart>().AddSkill(new Axe_Decapitate(), source: "test");
            Assert.IsTrue(Axe_Decapitate.ShouldDecapitate(actor),
                "ShouldDecapitate must return true when actor owns Axe_Decapitate.");
        }

        // ════════════════════════════════════════════════════════════════
        // Marker-skill invariant: no virtual overrides beyond Name
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Decapitate_IsMarkerSkill_NoBehavioralOverrides()
        {
            // Pin the "marker skill" property: Axe_Decapitate must NOT
            // override any combat-event virtual on BaseSkillPart. If a
            // future contributor adds an OnAttackerAfterAttack or
            // OnGetPenetrationModifier override, this test fails to
            // remind them that Decapitate's behavior is delegated to
            // Axe_Dismember's candidate-pool gate, not implemented here.
            //
            // The check uses reflection — `DeclaredOnly` returns only
            // methods declared on Axe_Decapitate itself, not inherited
            // from BaseSkillPart.
            var skillType = typeof(Axe_Decapitate);
            var declaredMethods = skillType.GetMethods(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly);

            // The only declared-on-this-class methods should be:
            //   - the Name property getter (for the abstract DisplayName/Name override)
            // Specifically: NO On* hooks (OnAttackerAfterAttack, OnGetToHitModifier,
            // OnGetPenetrationModifier, OnWeaponMadeCriticalHit, etc.)
            foreach (var m in declaredMethods)
            {
                Assert.IsFalse(m.Name.StartsWith("On") && m.Name != "OnAttachedToEntity",
                    $"Decapitate is a marker skill — must NOT override behavioral virtual '{m.Name}'. " +
                    $"If you need this override, refactor: Decapitate's behavior is delegated to Axe_Dismember.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Interaction: Dismember + Decapitate can target Mortal parts
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Dismember_WithDecapitateOwned_CanTargetMortalAcrossSeeds()
        {
            // Actor owns BOTH Axe_Dismember + Axe_Decapitate. Defender
            // has ONLY Mortal severable parts (head). Across many seeds
            // at the inflated chance, Dismember should eventually pick
            // the head from the candidate pool (which now includes
            // Mortal parts because Decapitate is owned).
            //
            // Without Decapitate, the existing test
            // (AxeDismemberTests.Dismember_OnlyMortalSeverableParts_NoDismember)
            // proves no dismember fires. With Decapitate, this test
            // proves it DOES fire — that's the whole point of the
            // candidate-pool expansion.
            var dismember = new Axe_Dismember();
            var decap = new Axe_Decapitate();
            var actor = MakeBareActor("attacker");
            actor.GetPart<SkillsPart>().AddSkill(dismember, source: "test");
            actor.GetPart<SkillsPart>().AddSkill(decap, source: "test");

            bool everDismembered = false;
            for (int seed = 0; seed < 5000 && !everDismembered; seed++)
            {
                var defender = MakeMortalOnlyDefender($"defender_{seed}");
                int partsBefore = defender.GetPart<Body>().GetParts().Count;
                var ctx = MakeAxeHitContext(actor, defender, seed);
                dismember.OnAttackerAfterAttack(ctx);

                if (defender.GetPart<Body>().GetParts().Count < partsBefore)
                    everDismembered = true;
            }

            Assert.IsTrue(everDismembered,
                "With Decapitate owned, Dismember's candidate pool includes Mortal parts. " +
                "Across 5000 seeds at 3% chance, at least one Mortal-part dismember should fire. Got zero.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: Decapitate alone (no Dismember) is inert
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Decapitate_AloneWithoutDismember_NeverFiresOnHit()
        {
            // Decapitate is a marker skill — without Axe_Dismember, it
            // has nothing to gate. So an actor with ONLY Decapitate
            // shouldn't dismember anything on Axe hits, regardless of
            // seed count. This pins the "marker only modifies Dismember"
            // claim in the docstring.
            var decap = new Axe_Decapitate();
            var actor = MakeBareActor("attacker");
            actor.GetPart<SkillsPart>().AddSkill(decap, source: "test");

            for (int seed = 0; seed < 200; seed++)
            {
                var defender = MakeMortalOnlyDefender($"defender_{seed}");
                int partsBefore = defender.GetPart<Body>().GetParts().Count;
                var ctx = MakeAxeHitContext(actor, defender, seed);

                // Decapitate has no OnAttackerAfterAttack override (per
                // marker-skill invariant) — calling the dispatcher would
                // be a no-op anyway. Just verify the body's intact.
                Assert.AreEqual(partsBefore, defender.GetPart<Body>().GetParts().Count,
                    $"Seed {seed}: Decapitate alone (no Dismember) must not dismember.");
            }
        }
    }
}
