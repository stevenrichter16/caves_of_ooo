using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP3.1 — Dispatcher contract tests. Verify that
    /// <see cref="SkillEventDispatcher"/> routes each event to every owned
    /// <see cref="BaseSkillPart"/> on the actor, that null-actor /
    /// no-SkillsPart paths are no-ops, and that the GetSkillHitModifier
    /// aggregator sums contributions linearly.
    ///
    /// <para>Stubs are distinct types so <see cref="SkillsPart.AddSkill"/>'s
    /// dedup-by-Type doesn't reject duplicates — needed for the multi-skill
    /// dispatch + sum tests.</para>
    /// </summary>
    public class SkillEventDispatcherTests
    {
        // ── Test stubs (distinct types so SkillsPart dedup doesn't reject) ──

        private class CountingSkillA : BaseSkillPart
        {
            public int AfterAttackCount;
            public int MissCount;
            public int DefenderMissedCount;
            public int CritCount;
            public int HitBonus;

            public override void OnAttackerAfterAttack(SkillEventContext ctx) => AfterAttackCount++;
            public override void OnAttackerMeleeMiss(SkillEventContext ctx) => MissCount++;
            public override void OnDefenderAfterAttackMissed(SkillEventContext ctx) => DefenderMissedCount++;
            public override void OnWeaponMadeCriticalHit(SkillEventContext ctx) => CritCount++;
            public override int OnGetToHitModifier(Entity actor, MeleeWeaponPart weapon) => HitBonus;
        }

        private class CountingSkillB : BaseSkillPart
        {
            public int AfterAttackCount;
            public int HitBonus;
            public override void OnAttackerAfterAttack(SkillEventContext ctx) => AfterAttackCount++;
            public override int OnGetToHitModifier(Entity actor, MeleeWeaponPart weapon) => HitBonus;
        }

        private class CountingSkillC : BaseSkillPart
        {
            public int HitBonus;
            public override int OnGetToHitModifier(Entity actor, MeleeWeaponPart weapon) => HitBonus;
        }

        private static Entity MakeActorWithSkills(params BaseSkillPart[] skills)
        {
            var e = new Entity { ID = "actor" };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            e.AddPart(new SkillsPart());
            foreach (var skill in skills)
            {
                bool added = e.GetPart<SkillsPart>().AddSkill(skill, source: "test");
                Assert.IsTrue(added,
                    $"AddSkill({skill.GetType().Name}) failed in fixture — likely a " +
                    $"duplicate-Type collision. Use distinct stub classes per skill.");
            }
            return e;
        }

        // ── Tests ────────────────────────────────────────────────────────

        [Test]
        public void AttackerAfterAttack_Routes_ToOwnedSkills()
        {
            var s1 = new CountingSkillA();
            var s2 = new CountingSkillB();
            var actor = MakeActorWithSkills(s1, s2);
            SkillEventDispatcher.AttackerAfterAttack(actor, new SkillEventContext { Attacker = actor });
            Assert.AreEqual(1, s1.AfterAttackCount);
            Assert.AreEqual(1, s2.AfterAttackCount);
        }

        [Test]
        public void AttackerMeleeMiss_Routes_ToOwnedSkills()
        {
            var s = new CountingSkillA();
            var actor = MakeActorWithSkills(s);
            SkillEventDispatcher.AttackerMeleeMiss(actor, new SkillEventContext { Attacker = actor });
            Assert.AreEqual(1, s.MissCount);
        }

        [Test]
        public void DefenderAfterAttackMissed_Routes_ToOwnedSkills()
        {
            var s = new CountingSkillA();
            var defender = MakeActorWithSkills(s);
            SkillEventDispatcher.DefenderAfterAttackMissed(defender, new SkillEventContext { Defender = defender });
            Assert.AreEqual(1, s.DefenderMissedCount);
        }

        [Test]
        public void WeaponMadeCriticalHit_Routes_ToOwnedSkills()
        {
            var s = new CountingSkillA();
            var actor = MakeActorWithSkills(s);
            SkillEventDispatcher.WeaponMadeCriticalHit(actor, new SkillEventContext { Attacker = actor });
            Assert.AreEqual(1, s.CritCount);
        }

        [Test]
        public void GetSkillHitModifier_Sums_AcrossOwnedSkills()
        {
            // Three distinct stub types each returning a different bonus.
            var s1 = new CountingSkillA { HitBonus = 2 };
            var s2 = new CountingSkillB { HitBonus = 1 };
            var s3 = new CountingSkillC { HitBonus = 3 };
            var actor = MakeActorWithSkills(s1, s2, s3);
            int total = SkillEventDispatcher.GetSkillHitModifier(actor, weapon: null);
            Assert.AreEqual(6, total,
                "GetSkillHitModifier should sum OnGetToHitModifier returns across all owned skills.");
        }

        [Test]
        public void NullActor_NoCrash_NoEvents()
        {
            Assert.DoesNotThrow(() =>
                SkillEventDispatcher.AttackerAfterAttack(null, new SkillEventContext()));
            Assert.DoesNotThrow(() =>
                SkillEventDispatcher.AttackerMeleeMiss(null, new SkillEventContext()));
            Assert.DoesNotThrow(() =>
                SkillEventDispatcher.DefenderAfterAttackMissed(null, new SkillEventContext()));
            Assert.DoesNotThrow(() =>
                SkillEventDispatcher.WeaponMadeCriticalHit(null, new SkillEventContext()));
            Assert.AreEqual(0, SkillEventDispatcher.GetSkillHitModifier(null, weapon: null));
        }

        [Test]
        public void ActorWithoutSkillsPart_NoCrash_NoEvents()
        {
            var actor = new Entity { ID = "noskills" };
            actor.AddPart(new RenderPart { DisplayName = "noskills" });
            Assert.DoesNotThrow(() =>
                SkillEventDispatcher.AttackerAfterAttack(actor, new SkillEventContext { Attacker = actor }));
            Assert.AreEqual(0, SkillEventDispatcher.GetSkillHitModifier(actor, weapon: null));
        }
    }
}
