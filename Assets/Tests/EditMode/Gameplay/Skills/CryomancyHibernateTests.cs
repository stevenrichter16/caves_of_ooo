using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Cryomancy_Hibernate tests. Pins the "applies
    /// HibernatingEffect to self" mechanic. Effect-mechanic
    /// invariants are pinned by HibernatingEffectTests.
    /// </summary>
    public class CryomancyHibernateTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied()
        {
            var e = new Entity { ID = "actor", BlueprintName = "actor" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 100 };
            e.Statistics["HeatResistance"] = new Stat { Owner = e, Name = "HeatResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["ColdResistance"] = new Stat { Owner = e, Name = "ColdResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        [Test]
        public void Hibernate_Spec_ReturnsExpectedShape()
        {
            var spec = new Cryomancy_Hibernate().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandHibernate", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, spec.TargetingMode);
        }

        [Test]
        public void Hibernate_AppliesHibernatingEffect()
        {
            var actor = MakeBodied();
            var skill = new Cryomancy_Hibernate();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            Assert.IsTrue(actor.GetPart<StatusEffectsPart>().HasEffect<HibernatingEffect>(),
                "Hibernate must apply HibernatingEffect.");
        }

        [Test]
        public void Hibernate_NoWeaponRequired()
        {
            // Hibernate has no weapon-class gate; verify it works even
            // with an actor that has no body/inventory/weapon at all.
            var actor = new Entity { ID = "actor", BlueprintName = "actor" };
            actor.Tags["Creature"] = "";
            actor.Statistics["Hitpoints"] = new Stat { Owner = actor, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 100 };
            actor.AddPart(new RenderPart { DisplayName = "actor" });
            actor.AddPart(new StatusEffectsPart());

            var skill = new Cryomancy_Hibernate();
            Assert.DoesNotThrow(() =>
                skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) }));
        }
    }
}
