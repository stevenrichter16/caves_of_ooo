using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    internal static class AbilityClarityTestSupport
    {
        internal static object Query(string typeName, string methodName, params object[] args)
        {
            var type = typeof(InputHandler).Assembly.GetType("CavesOfOoo.Rendering." + typeName);
            Assert.NotNull(type, "Missing player clarity query: " + typeName);
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "Missing query method: " + methodName);
            return method.Invoke(null, args);
        }
        internal static Entity Actor(string name = "reader")
        {
            var actor = new Entity { ID = name, BlueprintName = name };
            actor.Tags.Add("Creature", ""); actor.Tags.Add("Player", "");
            actor.AddPart(new RenderPart { DisplayName = name, Visible = true });
            actor.AddPart(new PhysicsPart()); actor.AddPart(new InventoryPart());
            actor.AddPart(new ActivatedAbilitiesPart()); actor.AddPart(new SkillsPart());
            actor.AddPart(new StatusEffectsPart());
            foreach (var stat in new[] { "Hitpoints", "SP", "Speed", "Ego", "Strength", "Agility", "Intelligence", "Willpower", "Toughness" })
                actor.Statistics[stat] = new Stat { Name = stat, BaseValue = 100, Max = 100, Owner = actor };
            return actor;
        }
        internal static string Skill(Entity actor, string name) => (string)Query("AbilityDetailsBuilder", "BuildForSkill", actor, name);
        internal static string Ability(Entity actor, Guid id) => (string)Query("AbilityDetailsBuilder", "BuildForAbility", actor, id);
        internal static void Reset() { SkillRegistry.ResetForTests(); SkillRegistry.EnsureInitialized(); ResonanceSystem.ResetForTests(); ResonanceSystem.EnsureInitialized(); MessageLog.Clear(); }
    }

    public class AbilityDetailsTests
    {
        [SetUp] public void SetUp() => AbilityClarityTestSupport.Reset();
        [TearDown] public void TearDown() { SkillRegistry.ResetForTests(); ResonanceSystem.ResetForTests(); MessageLog.Clear(); }

        [Test] public void UnownedPowerShowsWholeDescriptionActualRangeAndCooldownWithoutLearning()
        {
            var actor = AbilityClarityTestSupport.Actor();
            Assert.True(SkillRegistry.TryGetPowerByClass("Rites_HangingBolt", out var power));
            string prose = power.Description;
            int parts = actor.Parts.Count;
            string text = AbilityClarityTestSupport.Skill(actor, power.Class);
            StringAssert.Contains(prose, text);
            StringAssert.Contains("Range: 6", text); StringAssert.Contains("Cooldown: 30", text);
            StringAssert.Contains("1 ink", text); StringAssert.Contains("2 marks", text);
            StringAssert.Contains("companion", text.ToLowerInvariant());
            StringAssert.Contains("Rites", text);
            Assert.AreEqual(parts, actor.Parts.Count); Assert.False(actor.GetPart<SkillsPart>().HasSkill(power.Class));
            Assert.Zero(actor.GetPart<ActivatedAbilitiesPart>().AbilityList.Count);
            Assert.AreEqual(100, actor.GetStatValue("SP"));
        }
        [Test] public void PassiveHasFullProseWithoutInventedActiveRangeOrCooldown()
        {
            var actor = AbilityClarityTestSupport.Actor();
            string text = AbilityClarityTestSupport.Skill(actor, "Axe_Decapitate");
            Assert.True(SkillRegistry.TryGetPowerByClass("Axe_Decapitate", out var power));
            StringAssert.Contains(power.Description, text); StringAssert.Contains("Passive", text);
            StringAssert.DoesNotContain("Cooldown:", text); StringAssert.DoesNotContain("Range:", text);
        }
        [Test] public void OwnedAbilityDetailsReflectCurrentCooldownAndRetainFullDescription()
        {
            var actor = AbilityClarityTestSupport.Actor(); var rite = new Rites_HangingBolt();
            Assert.True(actor.GetPart<SkillsPart>().AddSkill(rite));
            var ability = actor.GetPart<ActivatedAbilitiesPart>().GetAbility(rite.ActivatedAbilityID);
            ability.CooldownRemaining = 7;
            string text = AbilityClarityTestSupport.Ability(actor, ability.ID);
            StringAssert.Contains("7 remaining", text); StringAssert.Contains("Cooldown: 30", text);
            StringAssert.Contains("No ink", text);
            ability.CooldownRemaining = 0;
            StringAssert.DoesNotContain("7 remaining", AbilityClarityTestSupport.Ability(actor, ability.ID));
        }
        [TestCase(null)] [TestCase("")] [TestCase("NotARegisteredPower")]
        public void UnknownSkillCannotExposeAnInventedDetail(string name)
        { Assert.IsTrue(string.IsNullOrEmpty(AbilityClarityTestSupport.Skill(AbilityClarityTestSupport.Actor(), name))); }
        [Test] public void ForeignAbilityIdDoesNotFallBackToAnotherOwnedAbility()
        {
            var actor = AbilityClarityTestSupport.Actor(); actor.GetPart<SkillsPart>().AddSkill(new Rites_HangingBolt());
            Assert.IsTrue(string.IsNullOrEmpty(AbilityClarityTestSupport.Ability(actor, Guid.NewGuid())));
        }
        [TestCase(true)] [TestCase(false)]
        public void HiddenAndObfuscatedUnownedRowsDoNotLeakProse(bool hidden)
        {
            var actor = AbilityClarityTestSupport.Actor(); SkillRegistry.TryGetPowerByClass("Rites_HangingBolt", out var row);
            row.Description = "SECRET_DETAIL_MARKER";
            if (hidden) row.Hidden = true; else { row.Obfuscated = true; row.Attribute = "Ego"; row.Minimum = "1000"; }
            string text = AbilityClarityTestSupport.Skill(actor, row.Class) ?? "";
            StringAssert.DoesNotContain("SECRET_DETAIL_MARKER", text);
            actor.GetPart<SkillsPart>().AddSkill(new Rites_HangingBolt());
            StringAssert.Contains("SECRET_DETAIL_MARKER", AbilityClarityTestSupport.Skill(actor, row.Class));
        }
        [Test] public void LongDescriptionIsPreservedPastTheOldFooterLimit()
        {
            var actor = AbilityClarityTestSupport.Actor(); SkillRegistry.TryGetPowerByClass("Rites_HangingBolt", out var row);
            row.Description = string.Join(" ", Enumerable.Repeat("Entire paragraph remains readable.", 150)) + " FINAL_SENTINEL";
            StringAssert.Contains(row.Description, AbilityClarityTestSupport.Skill(actor, row.Class));
        }
    }
}
