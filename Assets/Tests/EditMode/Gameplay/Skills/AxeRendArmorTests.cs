using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Axe_RendArmor tests. Pins the "applies 3 stacks of
    /// ShatterArmorEffect directly, no damage, no chance roll" mechanic.
    /// </summary>
    public class AxeRendArmorTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied(string name = "c", int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
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
            var body = new Body(); e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        private static Entity MakeWeapon(string name, string attrs)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d8", Attributes = attrs });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void Equip(Entity actor, Entity w)
        {
            var hand = actor.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            actor.GetPart<InventoryPart>().EquipToBodyPart(w, hand);
        }

        private static (Entity atk, Entity def, Zone zone, Axe_RendArmor skill) Fixture(string attrs = "Cutting Axe")
        {
            var atk = MakeBodied("atk");
            Equip(atk, MakeWeapon("axe", attrs));
            var skill = new Axe_RendArmor();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def");
            return (atk, def, new Zone(), skill);
        }

        [Test]
        public void RendArmor_Spec_ReturnsExpectedShape()
        {
            var spec = new Axe_RendArmor().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandRendArmor", spec.Command);
            Assert.AreEqual(Axe_RendArmor.COOLDOWN, spec.Cooldown);
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode);
        }

        [Test]
        public void RendArmor_AppliesShatterArmorEffect()
        {
            var (atk, def, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });
            Assert.IsTrue(def.GetPart<StatusEffectsPart>().HasEffect<ShatterArmorEffect>());
        }

        [Test]
        public void RendArmor_AppliesExpectedStackCount()
        {
            var (atk, def, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });
            var sa = def.GetPart<StatusEffectsPart>().GetEffect<ShatterArmorEffect>();
            Assert.IsNotNull(sa);
            Assert.AreEqual(Axe_RendArmor.REND_STACKS, sa.StackCount,
                "RendArmor must apply REND_STACKS (3) at once for a 3 × AV_REDUCTION drop.");
        }

        [Test]
        public void RendArmor_DealsNoDamage()
        {
            var (atk, def, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            int hpBefore = def.GetStatValue("Hitpoints");
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });
            Assert.AreEqual(hpBefore, def.GetStatValue("Hitpoints"),
                "RendArmor is armor-shred, not damage. HP must not change.");
        }

        [Test]
        public void RendArmor_NoAxeWeapon_RefusesAndEmitsDiag()
        {
            var (atk, def, zone, skill) = Fixture(attrs: "Bludgeoning Cudgel");
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });
            Assert.IsFalse(def.GetPart<StatusEffectsPart>().HasEffect<ShatterArmorEffect>());
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_weapon", recs[0].PayloadJson);
        }

        [Test]
        public void RendArmor_NoTarget_RefusesAndEmitsDiag()
        {
            var (atk, _, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5); // no defender
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_target", recs[0].PayloadJson);
        }

        [Test]
        public void RendArmor_NullRng_NoCrash()
        {
            var (atk, def, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            Assert.DoesNotThrow(() =>
                skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = null }));
        }
    }
}
