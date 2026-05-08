using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Spellcraft_LeyTap tests. Pins the "drain HP, single-
    /// charge spell-damage buff" mechanic.
    /// </summary>
    public class SpellcraftLeyTapTests
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
        public void LeyTap_Spec_ReturnsExpectedShape()
        {
            var spec = new Spellcraft_LeyTap().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandLeyTap", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, spec.TargetingMode);
        }

        [Test]
        public void LeyTap_DrainsHp()
        {
            var actor = MakeBodied(hp: 100);
            var skill = new Spellcraft_LeyTap();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            int hp = actor.GetStatValue("Hitpoints");
            Assert.Less(hp, 100);
            // 15% of 100 = 15. Allow ±2 rounding.
            Assert.That(hp, Is.InRange(83, 87));
        }

        [Test]
        public void LeyTap_StoresPendingBonus()
        {
            var actor = MakeBodied();
            var skill = new Spellcraft_LeyTap();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            Assert.Greater(skill.PendingBonus, 0,
                "LeyTap must store a pending bonus = drained × multiplier.");
        }

        [Test]
        public void LeyTap_AnyElement_ReturnsBonus()
        {
            var actor = MakeBodied();
            var skill = new Spellcraft_LeyTap();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });
            int bonus = skill.OnGetSpellDamageModifier(actor, actor, "Cold", baseDamage: 5);
            Assert.Greater(bonus, 0, "LeyTap is universal — any element gets the bonus.");
        }

        [Test]
        public void LeyTap_BuffIsSingleCharge_SecondCallReturnsZero()
        {
            var actor = MakeBodied();
            var skill = new Spellcraft_LeyTap();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            int firstBonus = skill.OnGetSpellDamageModifier(actor, actor, "Fire", 5);
            Assert.Greater(firstBonus, 0);
            int secondBonus = skill.OnGetSpellDamageModifier(actor, actor, "Fire", 5);
            Assert.AreEqual(0, secondBonus,
                "LeyTap is single-charge — second call returns 0 (buff consumed).");
        }

        [Test]
        public void LeyTap_NoHpStat_EmitsDiag()
        {
            var actor = new Entity { ID = "actor" };
            actor.AddPart(new RenderPart { DisplayName = "actor" });
            var skill = new Spellcraft_LeyTap();
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_hitpoints", recs[0].PayloadJson);
        }
    }
}
