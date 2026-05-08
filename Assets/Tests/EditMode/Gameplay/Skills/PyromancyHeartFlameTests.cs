using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Pyromancy_HeartFlame tests. Pins the "drain HP, charge
    /// 3 fire-spell amplifications" mechanic.
    /// </summary>
    public class PyromancyHeartFlameTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied(int hp = 100)
        {
            var e = new Entity { ID = "actor", BlueprintName = "actor" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        [Test]
        public void HeartFlame_Spec_ReturnsExpectedShape()
        {
            var spec = new Pyromancy_HeartFlame().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandHeartFlame", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, spec.TargetingMode);
        }

        [Test]
        public void HeartFlame_DrainsHp()
        {
            var actor = MakeBodied(hp: 100);
            var skill = new Pyromancy_HeartFlame();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            int hp = actor.GetStatValue("Hitpoints");
            Assert.Less(hp, 100, "HeartFlame must drain HP.");
            // 50% sacrifice from 100 = 50 drained. Allow ±1 for rounding.
            Assert.That(hp, Is.InRange(49, 51), "HeartFlame should drain ~50% of current HP.");
        }

        [Test]
        public void HeartFlame_SetsBuffCharges()
        {
            var actor = MakeBodied();
            var skill = new Pyromancy_HeartFlame();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            Assert.AreEqual(Pyromancy_HeartFlame.BUFF_CHARGES, skill.ChargesRemaining,
                "HeartFlame must set BUFF_CHARGES (3) charges.");
        }

        [Test]
        public void HeartFlame_OnGetSpellDamageModifier_HeatElement_ReturnsBonus()
        {
            var actor = MakeBodied();
            var skill = new Pyromancy_HeartFlame();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            int bonus = skill.OnGetSpellDamageModifier(actor, actor, "Heat", baseDamage: 10);
            Assert.Greater(bonus, 0, "HeartFlame must return a damage bonus when buff is active + element is Heat.");
        }

        [Test]
        public void HeartFlame_OnGetSpellDamageModifier_NonHeatElement_ReturnsZero()
        {
            var actor = MakeBodied();
            var skill = new Pyromancy_HeartFlame();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            int bonus = skill.OnGetSpellDamageModifier(actor, actor, "Cold", baseDamage: 10);
            Assert.AreEqual(0, bonus, "Non-Heat element must NOT receive HeartFlame bonus.");
        }

        [Test]
        public void HeartFlame_BuffConsumesChargePerCast()
        {
            var actor = MakeBodied();
            var skill = new Pyromancy_HeartFlame();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            // Three Heat casts consume all 3 charges.
            for (int i = 0; i < Pyromancy_HeartFlame.BUFF_CHARGES; i++)
            {
                int bonus = skill.OnGetSpellDamageModifier(actor, actor, "Heat", baseDamage: 10);
                Assert.Greater(bonus, 0, "Charge " + i + " must yield a bonus.");
            }
            Assert.AreEqual(0, skill.ChargesRemaining, "All charges should be spent.");
            int afterAllSpent = skill.OnGetSpellDamageModifier(actor, actor, "Heat", baseDamage: 10);
            Assert.AreEqual(0, afterAllSpent, "After charges spent, no bonus.");
        }

        [Test]
        public void HeartFlame_NoHpStat_EmitsDiag()
        {
            var actor = new Entity { ID = "actor", BlueprintName = "actor" };
            actor.AddPart(new RenderPart { DisplayName = "actor" });
            // No Hitpoints stat.
            var skill = new Pyromancy_HeartFlame();
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_hitpoints", recs[0].PayloadJson);
        }
    }
}
