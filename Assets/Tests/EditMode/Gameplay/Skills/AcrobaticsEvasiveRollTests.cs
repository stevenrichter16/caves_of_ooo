using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Acrobatics_EvasiveRoll tests. Pins the "remove one
    /// negative effect, priority-ordered" mechanic.
    /// </summary>
    public class AcrobaticsEvasiveRollTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied()
        {
            var e = new Entity { ID = "actor", BlueprintName = "actor" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            e.Statistics["DV"] = new Stat { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.Statistics["Agility"] = new Stat { Owner = e, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        [Test]
        public void EvasiveRoll_Spec_ReturnsExpectedShape()
        {
            var spec = new Acrobatics_EvasiveRoll().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandEvasiveRoll", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, spec.TargetingMode);
        }

        [Test]
        public void EvasiveRoll_RemovesStunnedFirst()
        {
            var actor = MakeBodied();
            var skill = new Acrobatics_EvasiveRoll();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            actor.ApplyEffect(new StunnedEffect(2), actor, null);
            actor.ApplyEffect(new BleedingEffect(15, "1d2"), actor, null);

            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            Assert.IsFalse(actor.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "Priority order: Stunned removed first.");
            Assert.IsTrue(actor.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                "Bleeding stays — only one effect removed per cast.");
        }

        [Test]
        public void EvasiveRoll_NoNegativeEffects_EmitsDiag()
        {
            var actor = MakeBodied();
            var skill = new Acrobatics_EvasiveRoll();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_negative_effect", recs[0].PayloadJson);
        }

        [Test]
        public void EvasiveRoll_RemovesBleedingWhenStunnedAbsent()
        {
            var actor = MakeBodied();
            var skill = new Acrobatics_EvasiveRoll();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            actor.ApplyEffect(new BleedingEffect(15, "1d2"), actor, null);
            // No Stunned/Frozen/Paralyzed present.

            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });
            Assert.IsFalse(actor.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                "When Stunned is absent, EvasiveRoll falls through to Bleeding.");
        }

        [Test]
        public void EvasiveRoll_NoStatusEffectsPart_EmitsDiag()
        {
            var actor = new Entity { ID = "actor", BlueprintName = "actor" };
            actor.AddPart(new RenderPart { DisplayName = "actor" });
            // No StatusEffectsPart added.
            var skill = new Acrobatics_EvasiveRoll();
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_status_part", recs[0].PayloadJson);
        }
    }
}
