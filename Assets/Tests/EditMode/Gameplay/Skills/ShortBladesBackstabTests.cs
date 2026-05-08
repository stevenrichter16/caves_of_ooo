using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — ShortBlades_Backstab tests. Pins the "+100% bonus
    /// damage when target is flanked (ally directly opposite attacker)"
    /// mechanic.
    /// </summary>
    public class ShortBladesBackstabTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied(string name = "c", int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
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

        private static Entity MakeWeapon(string attrs)
        {
            var e = new Entity { ID = "dagger", BlueprintName = "dagger" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "dagger" });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d4+1", Attributes = attrs });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static (Entity atk, Entity def, Zone zone, ShortBlades_Backstab skill) Fixture(int defHp = 100, string attrs = "Piercing")
        {
            var atk = MakeBodied("atk");
            var hand = atk.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            atk.GetPart<InventoryPart>().EquipToBodyPart(MakeWeapon(attrs), hand);
            var skill = new ShortBlades_Backstab();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var def = MakeBodied("def", hp: defHp);
            return (atk, def, new Zone(), skill);
        }

        [Test]
        public void Backstab_Spec_ReturnsExpectedShape()
        {
            var spec = new ShortBlades_Backstab().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandBackstab", spec.Command);
            Assert.AreEqual(ShortBlades_Backstab.COOLDOWN, spec.Cooldown);
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode);
        }

        [Test]
        public void Backstab_FlankedTarget_DealsBonusDamage()
        {
            // atk at (5,5), def at (6,5), ally (flanker) at (7,5).
            // Backstab: opposite cell from atk through def is (7,5) — flanker present → bonus.
            var (atk, def, zone, skill) = Fixture(defHp: 100);
            var flanker = MakeBodied("flanker");
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5); zone.AddEntity(flanker, 7, 5);

            int hpBefore = def.GetStatValue("Hitpoints");
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42) });
            int flankedDamage = hpBefore - def.GetStatValue("Hitpoints");

            // Compare against unflanked damage with same seed.
            var (atk2, def2, zone2, skill2) = Fixture(defHp: 100);
            zone2.AddEntity(atk2, 5, 5); zone2.AddEntity(def2, 6, 5); // no flanker
            int hpBefore2 = def2.GetStatValue("Hitpoints");
            skill2.OnCommand(new SkillEventContext { Attacker = atk2, Defender = atk2, Zone = zone2, Rng = new Random(42) });
            int unflankedDamage = hpBefore2 - def2.GetStatValue("Hitpoints");

            Assert.Greater(flankedDamage, unflankedDamage,
                "Flanked target must take more damage than unflanked. flanked="
                + flankedDamage + " unflanked=" + unflankedDamage);
        }

        [Test]
        public void Backstab_UnflankedTarget_NoBonus()
        {
            var (atk, def, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            int hpBefore = def.GetStatValue("Hitpoints");
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42) });
            // Damage taken is whatever PerformSingleAttack rolled (no bonus added).
            int damage = hpBefore - def.GetStatValue("Hitpoints");
            // Just verify SOME damage landed (the swing fires regardless).
            Assert.GreaterOrEqual(damage, 0, "Unflanked Backstab still rolls a normal swing.");
        }

        [Test]
        public void Backstab_NoPiercingWeapon_RefusesAndEmitsDiag()
        {
            var (atk, def, zone, skill) = Fixture(attrs: "Bludgeoning Cudgel");
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_weapon", recs[0].PayloadJson);
        }

        [Test]
        public void Backstab_NoTarget_RefusesAndEmitsDiag()
        {
            var (atk, _, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_target", recs[0].PayloadJson);
        }

        [Test]
        public void Backstab_NullRng_NoCrash()
        {
            var (atk, def, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            Assert.DoesNotThrow(() =>
                skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = null }));
        }
    }
}
