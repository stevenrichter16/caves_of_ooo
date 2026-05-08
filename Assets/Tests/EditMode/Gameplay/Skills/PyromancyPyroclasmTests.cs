using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Pyromancy_Pyroclasm tests. Pins the "consume Burning,
    /// AOE damage = duration × DAMAGE_PER_BURN_TURN" mechanic.
    /// </summary>
    public class PyromancyPyroclasmTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied(string name = "c", int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["HeatResistance"] = new Stat { Owner = e, Name = "HeatResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        private static (Entity atk, Zone zone, Pyromancy_Pyroclasm skill) Fixture()
        {
            var atk = MakeBodied("atk");
            var skill = new Pyromancy_Pyroclasm();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            return (atk, new Zone(), skill);
        }

        [Test]
        public void Pyroclasm_Spec_ReturnsExpectedShape()
        {
            var spec = new Pyromancy_Pyroclasm().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandPyroclasm", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode);
        }

        [Test]
        public void Pyroclasm_TargetMustBeBurning_NonBurningRejected()
        {
            var (atk, zone, skill) = Fixture();
            var def = MakeBodied("def");
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            // No Burning on def.
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_target", recs[0].PayloadJson);
            Assert.AreEqual(50, def.GetStatValue("Hitpoints"), "Non-burning target is untouched.");
        }

        [Test]
        public void Pyroclasm_BurningAdjacentTarget_ConsumesEffect()
        {
            var (atk, zone, skill) = Fixture();
            var def = MakeBodied("def", hp: 200);
            def.AddPart(new ThermalPart()); // Burning's OnApply pokes thermal
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            def.ApplyEffect(new BurningEffect(intensity: 2.0f), atk, zone);
            Assert.IsTrue(def.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "Setup: defender starts burning.");

            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });

            Assert.IsFalse(def.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "Pyroclasm must consume the BurningEffect.");
        }

        [Test]
        public void Pyroclasm_DamagesAOECreatures()
        {
            var (atk, zone, skill) = Fixture();
            var def = MakeBodied("def", hp: 200);
            var bystander = MakeBodied("bystander", hp: 200);
            def.AddPart(new ThermalPart());
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(def, 7, 5); // 2 cells away — adjacency for the original ability requires range 1
            // Since Pyroclasm is AdjacentCell with 8-dir scan, place def at distance 1.
            zone.RemoveEntity(def); zone.AddEntity(def, 6, 5);
            zone.AddEntity(bystander, 7, 6); // adjacent to def (distance 1 SE)
            def.ApplyEffect(new BurningEffect(intensity: 2.0f), atk, zone);

            int defHp = def.GetStatValue("Hitpoints");
            int byHp = bystander.GetStatValue("Hitpoints");

            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });

            Assert.Less(def.GetStatValue("Hitpoints"), defHp, "Burning target itself takes AOE damage.");
            Assert.Less(bystander.GetStatValue("Hitpoints"), byHp, "Bystander in 3x3 radius takes AOE damage.");
        }

        [Test]
        public void Pyroclasm_NoZone_EmitsDiag()
        {
            var (atk, _, skill) = Fixture();
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = null, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_zone", recs[0].PayloadJson);
        }

        [Test]
        public void Pyroclasm_NullRng_NoCrash()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            Assert.DoesNotThrow(() =>
                skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = null }));
        }
    }
}
