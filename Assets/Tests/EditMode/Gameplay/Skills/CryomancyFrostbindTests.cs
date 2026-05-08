using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Cryomancy_Frostbind tests. Pins the "apply RootedEffect
    /// to adjacent target" mechanic. Movement-blocking semantics are
    /// pinned by the RootedEffect tests; here we just verify the
    /// skill's apply path.
    /// </summary>
    public class CryomancyFrostbindTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied(string name = "c")
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        [Test]
        public void Frostbind_Spec_ReturnsExpectedShape()
        {
            var spec = new Cryomancy_Frostbind().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandFrostbind", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode);
        }

        [Test]
        public void Frostbind_AdjacentTarget_AppliesRooted()
        {
            var atk = MakeBodied("atk");
            var skill = new Cryomancy_Frostbind();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });

            Assert.IsTrue(def.GetPart<StatusEffectsPart>().HasEffect<RootedEffect>(),
                "Frostbind must apply RootedEffect to the adjacent target.");
        }

        [Test]
        public void Frostbind_NoAdjacent_EmitsDiag()
        {
            var atk = MakeBodied("atk");
            var skill = new Cryomancy_Frostbind();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_target", recs[0].PayloadJson);
        }
    }
}
