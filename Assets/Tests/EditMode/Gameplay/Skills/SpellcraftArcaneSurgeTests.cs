using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Spellcraft_ArcaneSurge tests. Pins the "reset all
    /// other ability cooldowns to 0 (skip self)" mechanic.
    /// </summary>
    public class SpellcraftArcaneSurgeTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied()
        {
            var e = new Entity { ID = "actor", BlueprintName = "actor" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        [Test]
        public void ArcaneSurge_Spec_ReturnsExpectedShape()
        {
            var spec = new Spellcraft_ArcaneSurge().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandArcaneSurge", spec.Command);
            Assert.AreEqual(Spellcraft_ArcaneSurge.COOLDOWN, spec.Cooldown);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, spec.TargetingMode);
        }

        [Test]
        public void ArcaneSurge_ResetsOtherAbilityCooldowns()
        {
            var actor = MakeBodied();
            var surge = new Spellcraft_ArcaneSurge();
            actor.GetPart<SkillsPart>().AddSkill(surge, source: "test");

            // Add an unrelated activated ability with a cooldown ticking.
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            var otherID = abilities.AddAbility(displayName: "Test", command: "CommandTest",
                abilityClass: "Skills", targetingMode: AbilityTargetingMode.AdjacentCell,
                range: 1, sourceMutationClass: "");
            var other = abilities.GetAbility(otherID);
            other.MaxCooldown = 50;
            other.CooldownRemaining = 30;

            surge.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            Assert.AreEqual(0, other.CooldownRemaining,
                "ArcaneSurge must reset the other ability's cooldown to 0.");
        }

        [Test]
        public void ArcaneSurge_DoesNotResetOwnCooldown()
        {
            var actor = MakeBodied();
            var surge = new Spellcraft_ArcaneSurge();
            actor.GetPart<SkillsPart>().AddSkill(surge, source: "test");
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            var ownAbility = abilities.GetAbility(surge.ActivatedAbilityID);
            Assert.IsNotNull(ownAbility, "Surge's own ability must be registered.");
            ownAbility.MaxCooldown = Spellcraft_ArcaneSurge.COOLDOWN;
            ownAbility.CooldownRemaining = Spellcraft_ArcaneSurge.COOLDOWN; // pretend mid-cooldown

            surge.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });

            Assert.AreEqual(Spellcraft_ArcaneSurge.COOLDOWN, ownAbility.CooldownRemaining,
                "ArcaneSurge must NOT reset its own cooldown — long cooldown is the gate.");
        }

        [Test]
        public void ArcaneSurge_NoActivatedAbilitiesPart_EmitsDiag()
        {
            var actor = new Entity { ID = "actor", BlueprintName = "actor" };
            actor.Tags["Creature"] = "";
            actor.AddPart(new RenderPart { DisplayName = "actor" });
            // Note: no ActivatedAbilitiesPart.
            var surge = new Spellcraft_ArcaneSurge();
            Diag.ResetAll();
            surge.OnCommand(new SkillEventContext { Attacker = actor, Defender = actor, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_abilities", recs[0].PayloadJson);
        }
    }
}
