using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP3.5 — Activated-ability lifecycle tests. Pins the contract:
    ///   - DeclareActivatedAbility on a skill registers an ActivatedAbility
    ///     on the actor's ActivatedAbilitiesPart at AddSkill time
    ///   - The skill's ActivatedAbilityID is stamped after registration
    ///   - TryRouteSkillCommand dispatches the matching command to
    ///     OnCommand (and applies the cooldown afterwards)
    ///   - Cooldown gate blocks re-invocation while CooldownRemaining > 0
    ///   - RemoveSkill cleans up the ability from ActivatedAbilitiesPart
    /// </summary>
    public class SkillActivatedAbilityTests
    {
        // Test stub — declares an ability, counts OnCommand invocations.
        private class TestActiveSkill : BaseSkillPart
        {
            public override string Name => nameof(TestActiveSkill);
            public int OnCommandCount;
            public Entity LastCommandActor;

            public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
            {
                return new ActivatedAbilitySpec
                {
                    DisplayName = "Test Active",
                    Command = "CommandTestActive",
                    Class = "Skills",
                    TargetingMode = AbilityTargetingMode.SelfCentered,
                    Range = 1,
                    Cooldown = 5,
                };
            }

            public override void OnCommand(Entity actor)
            {
                OnCommandCount++;
                LastCommandActor = actor;
            }
        }

        // Stub for the no-ability default path (passive skill).
        private class TestPassiveSkill : BaseSkillPart
        {
            public override string Name => nameof(TestPassiveSkill);
        }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        private static Entity MakeActor()
        {
            var e = new Entity { ID = "actor" };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        [Test]
        public void AddSkill_WithActivatedAbilitySpec_RegistersAbility()
        {
            var actor = MakeActor();
            var skill = new TestActiveSkill();
            Assert.IsTrue(actor.GetPart<SkillsPart>().AddSkill(skill, source: "test"));

            Assert.AreNotEqual(System.Guid.Empty, skill.ActivatedAbilityID,
                "After successful AddSkill, the skill should have its ActivatedAbilityID stamped.");
            var ability = actor.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID);
            Assert.IsNotNull(ability, "The ActivatedAbilitiesPart must contain the registered ability.");
            Assert.AreEqual("CommandTestActive", ability.Command);
            Assert.AreEqual(5, ability.MaxCooldown);
        }

        [Test]
        public void AddSkill_PassiveSkill_DoesNotRegisterAbility()
        {
            var actor = MakeActor();
            var skill = new TestPassiveSkill();
            Assert.IsTrue(actor.GetPart<SkillsPart>().AddSkill(skill, source: "test"));

            Assert.AreEqual(System.Guid.Empty, skill.ActivatedAbilityID,
                "Passive skills (no ActivatedAbilitySpec) should not register any ability.");
            Assert.AreEqual(0, actor.GetPart<ActivatedAbilitiesPart>().AbilityList.Count,
                "ActivatedAbilitiesPart should remain empty for passive skills.");
        }

        [Test]
        public void TryRouteSkillCommand_DispatchesToOnCommand()
        {
            var actor = MakeActor();
            var skill = new TestActiveSkill();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            bool routed = actor.GetPart<SkillsPart>().TryRouteSkillCommand("CommandTestActive");
            Assert.IsTrue(routed, "TryRouteSkillCommand should return true when a skill consumes the command.");
            Assert.AreEqual(1, skill.OnCommandCount, "OnCommand should fire exactly once.");
            Assert.AreSame(actor, skill.LastCommandActor, "OnCommand should pass the actor entity.");
        }

        [Test]
        public void TryRouteSkillCommand_AppliesCooldown_AfterInvocation()
        {
            var actor = MakeActor();
            var skill = new TestActiveSkill();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            actor.GetPart<SkillsPart>().TryRouteSkillCommand("CommandTestActive");

            var ability = actor.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID);
            Assert.AreEqual(5, ability.CooldownRemaining,
                "After successful command dispatch, CooldownRemaining should be set to MaxCooldown.");
            Assert.IsFalse(ability.IsUsable, "Ability should report not-usable while on cooldown.");
        }

        [Test]
        public void TryRouteSkillCommand_BlocksWhileOnCooldown()
        {
            var actor = MakeActor();
            var skill = new TestActiveSkill();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            actor.GetPart<SkillsPart>().TryRouteSkillCommand("CommandTestActive");
            // Second invocation while cooldown active.
            bool routedAgain = actor.GetPart<SkillsPart>().TryRouteSkillCommand("CommandTestActive");
            Assert.IsFalse(routedAgain,
                "Second TryRouteSkillCommand while on cooldown must return false.");
            Assert.AreEqual(1, skill.OnCommandCount,
                "OnCommand should NOT fire again while ability is on cooldown.");
        }

        [Test]
        public void TryRouteSkillCommand_UnknownCommand_ReturnsFalse()
        {
            var actor = MakeActor();
            actor.GetPart<SkillsPart>().AddSkill(new TestActiveSkill(), source: "test");
            Assert.IsFalse(actor.GetPart<SkillsPart>().TryRouteSkillCommand("CommandUnknown"),
                "Unknown commands should return false (caller can fall through to other dispatchers).");
        }

        [Test]
        public void RemoveSkill_CleansUpRegisteredAbility()
        {
            var actor = MakeActor();
            var skill = new TestActiveSkill();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var idBefore = skill.ActivatedAbilityID;
            Assert.AreNotEqual(System.Guid.Empty, idBefore);

            actor.GetPart<SkillsPart>().RemoveSkill(skill, cause: "test");

            Assert.AreEqual(System.Guid.Empty, skill.ActivatedAbilityID,
                "RemoveSkill should clear the skill's ActivatedAbilityID.");
            Assert.IsNull(actor.GetPart<ActivatedAbilitiesPart>().GetAbility(idBefore),
                "ActivatedAbilitiesPart should no longer contain the removed ability.");
        }

        [Test]
        public void Actor_WithoutActivatedAbilitiesPart_StillAcceptsActiveSkill()
        {
            // No ActivatedAbilitiesPart on the actor (e.g. NPC). AddSkill
            // should still succeed — just skip the ability registration
            // silently. The skill's OnCommand is unreachable but the
            // skill exists and can fire its passive virtuals.
            var actor = new Entity { ID = "noabilities" };
            actor.AddPart(new RenderPart { DisplayName = "noabilities" });
            actor.AddPart(new SkillsPart());
            // Note: deliberately NO ActivatedAbilitiesPart.

            var skill = new TestActiveSkill();
            Assert.IsTrue(actor.GetPart<SkillsPart>().AddSkill(skill, source: "test"),
                "AddSkill should succeed even without ActivatedAbilitiesPart — passive virtuals still work.");
            Assert.AreEqual(System.Guid.Empty, skill.ActivatedAbilityID,
                "Without ActivatedAbilitiesPart, the skill keeps Guid.Empty (no ability registered).");
        }
    }
}
