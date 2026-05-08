using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Galvanism_Overload tests. Pins the "chain through
    /// Wet/Electrified targets, broken by non-conductors" mechanic.
    /// </summary>
    public class GalvanismOverloadTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied(string name = "c", int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["LightningResistance"] = new Stat { Owner = e, Name = "LightningResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        private static (Entity atk, Zone zone, Galvanism_Overload skill) Fixture()
        {
            var atk = MakeBodied("atk");
            var skill = new Galvanism_Overload();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            return (atk, new Zone(), skill);
        }

        [Test]
        public void Overload_Spec_ReturnsExpectedShape()
        {
            var spec = new Galvanism_Overload().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandOverload", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode);
            Assert.AreEqual(Galvanism_Overload.OVERLOAD_RANGE, spec.Range);
        }

        [Test]
        public void Overload_WetTarget_TakesDamage()
        {
            var (atk, zone, skill) = Fixture();
            var def = MakeBodied("def", hp: 200);
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            def.ApplyEffect(new WetEffect(), atk, zone);

            int hpBefore = def.GetStatValue("Hitpoints");
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            });
            Assert.Less(def.GetStatValue("Hitpoints"), hpBefore, "Wet target takes Electric damage.");
        }

        [Test]
        public void Overload_DryTargetBreaksChain_FurtherWetTargetsUntouched()
        {
            var (atk, zone, skill) = Fixture();
            var dry = MakeBodied("dry", hp: 200);
            var wet = MakeBodied("wet", hp: 200);
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(dry, 6, 5);
            zone.AddEntity(wet, 7, 5);
            wet.ApplyEffect(new WetEffect(), atk, zone);

            int dryHp = dry.GetStatValue("Hitpoints");
            int wetHp = wet.GetStatValue("Hitpoints");

            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            });

            Assert.AreEqual(dryHp, dry.GetStatValue("Hitpoints"),
                "Dry target breaks the chain — must not take damage.");
            Assert.AreEqual(wetHp, wet.GetStatValue("Hitpoints"),
                "Wet target BEHIND the dry one is untouched (chain broke).");
        }

        [Test]
        public void Overload_NoTargets_EmitsDiag()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1);
            StringAssert.Contains("no_target", recs[0].PayloadJson);
        }

        [Test]
        public void Overload_NoDirection_EmitsDiag()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0),
                DirectionX = 0, DirectionY = 0,
            });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_direction", recs[0].PayloadJson);
        }

        [Test]
        public void Overload_NullRng_NoCrash()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            Assert.DoesNotThrow(() =>
                skill.OnCommand(new SkillEventContext
                {
                    Attacker = atk, Defender = atk, Zone = zone, Rng = null,
                    DirectionX = 1, DirectionY = 0,
                }));
        }
    }
}
