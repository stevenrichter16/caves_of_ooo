using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Cudgel_GroundPound active-ability tests. Pins the
    /// "all 8 adjacent take damage + Stunned + knockback" mechanic.
    /// </summary>
    public class CudgelGroundPoundTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
            Diag.ResetAll();
        }

        private static Entity MakeBodiedCreature(string name = "creature",
            int strength = 16, int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat { Owner = e, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.Statistics["DV"] = new Stat { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new ArmorPart());
            e.AddPart(new InventoryPart { MaxWeight = 150 });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            var body = new Body();
            e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        private static Entity MakeWeapon(string name, string dice, string attrs)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            e.AddPart(new MeleeWeaponPart { BaseDamage = dice, PenBonus = 0, Attributes = attrs });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void Equip(Entity actor, Entity weapon)
        {
            var hand = actor.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            actor.GetPart<InventoryPart>().EquipToBodyPart(weapon, hand);
        }

        private static (Entity attacker, Zone zone, Cudgel_GroundPound skill) Fixture(string attrs = "Bludgeoning Cudgel")
        {
            var attacker = MakeBodiedCreature("attacker");
            Equip(attacker, MakeWeapon("mace", "1d8+2", attrs));
            var skill = new Cudgel_GroundPound();
            attacker.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            return (attacker, zone, skill);
        }

        [Test]
        public void GroundPound_Spec_ReturnsExpectedShape()
        {
            var spec = new Cudgel_GroundPound().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandGroundPound", spec.Command);
            Assert.AreEqual(Cudgel_GroundPound.COOLDOWN, spec.Cooldown);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, spec.TargetingMode);
        }

        [Test]
        public void GroundPound_DamagesAdjacentCreature()
        {
            var (atk, zone, skill) = Fixture();
            var def = MakeBodiedCreature("def");
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            int hpBefore = def.GetStatValue("Hitpoints");
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(7) });
            Assert.Less(def.GetStatValue("Hitpoints"), hpBefore);
        }

        [Test]
        public void GroundPound_StunsAdjacentCreature()
        {
            var (atk, zone, skill) = Fixture();
            var def = MakeBodiedCreature("def");
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(7) });
            Assert.IsTrue(def.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "GroundPound must apply Stunned to surviving adjacent targets.");
        }

        [Test]
        public void GroundPound_KnocksbackAdjacentCreature()
        {
            var (atk, zone, skill) = Fixture();
            var def = MakeBodiedCreature("def", hp: 200);
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(7) });
            var pos = zone.GetEntityPosition(def);
            Assert.AreEqual(7, pos.x, "Defender at (6,5) East of attacker should knock to (7,5).");
        }

        [Test]
        public void GroundPound_NoCudgelWeapon_RefusesAndEmitsDiag()
        {
            var (atk, zone, skill) = Fixture(attrs: "Cutting LongBlades");
            var def = MakeBodiedCreature("def");
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            int hpBefore = def.GetStatValue("Hitpoints");
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(7) });
            Assert.AreEqual(hpBefore, def.GetStatValue("Hitpoints"));
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("no_weapon", recs[0].PayloadJson);
        }

        [Test]
        public void GroundPound_NoAdjacent_RefusesAndEmitsDiag()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5); // no defenders
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(7) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("no_target", recs[0].PayloadJson);
        }

        [Test]
        public void GroundPound_NullRng_NoCrash()
        {
            var (atk, zone, skill) = Fixture();
            var def = MakeBodiedCreature("def");
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            int hpBefore = def.GetStatValue("Hitpoints");
            Assert.DoesNotThrow(() =>
                skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = null }));
            Assert.AreEqual(hpBefore, def.GetStatValue("Hitpoints"));
        }

        [Test]
        public void GroundPound_NullZone_NoCrash()
        {
            var (atk, _, skill) = Fixture();
            Assert.DoesNotThrow(() =>
                skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = null, Rng = new Random(7) }));
        }
    }
}
