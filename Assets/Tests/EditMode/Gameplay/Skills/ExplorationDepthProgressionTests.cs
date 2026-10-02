using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Real content and level economy. XP is fixture setup, not an ordinary-play witness.</summary>
    public class ExplorationDepthProgressionTests
    {
        private ScenarioTestHarness harness;
        [OneTimeSetUp] public void Open() => harness = new ScenarioTestHarness();
        [OneTimeTearDown] public void Close() => harness?.Dispose();
        [SetUp] public void Setup() { SkillRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); }
        [TearDown] public void Cleanup() { SkillRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); }

        [TestCase("AcrobaticsSkill", "AcrobaticsDodgePower")]
        [TestCase("AcrobaticsSkill", "Acrobatics_Tumble")]
        [TestCase("AcrobaticsSkill", "Acrobatics_EvasiveRoll")]
        [TestCase("AcrobaticsSkill", "Acrobatics_Vault")]
        [TestCase("PersuasionSkill", "Persuasion_Recruit")]
        [TestCase("PersuasionSkill", "Persuasion_Dismiss")]
        public void TwoActualLevelAwardsBuyRootThenOnePower(string tree, string power)
        {
            var player = harness.Factory.CreateEntity("Player");
            Assert.AreEqual(0, player.GetStatValue("SP"));
            Assert.IsFalse(BuySkillAction.Execute(player, tree).Succeeded);
            Level(player);
            Assert.AreEqual(1, player.GetStatValue("SP"));
            Assert.IsTrue(BuySkillAction.Execute(player, tree).Succeeded, "One actual level buys the tree.");
            Assert.AreEqual(0, player.GetStatValue("SP"));
            Assert.IsFalse(BuySkillAction.Execute(player, power).Succeeded, "No free child power.");
            Level(player);
            Assert.AreEqual(SkillsScreenRowState.Buyable, Row(player, power).State);
            Assert.IsTrue(BuySkillAction.Execute(player, power).Succeeded);
            Assert.AreEqual(0, player.GetStatValue("SP"));
            Assert.AreEqual(SkillsScreenRowState.Owned, Row(player, power).State);
            Assert.IsFalse(BuySkillAction.Execute(player, power).Succeeded, "No duplicate purchase.");
        }

        [TestCase("ShortBladesSkill", "ShortBlades_Disengage")]
        [TestCase("AcrobaticsSkill", "Acrobatics_Vault")]
        [TestCase("PersuasionSkill", "Persuasion_Recruit")]
        [TestCase("SpellcraftSkill", "Spellcraft_WardGleam")]
        public void PurchaseAndScreenRequireExactParent(string tree, string power)
        {
            var player = harness.Factory.CreateEntity("Player");
            player.GetStat("SP").BaseValue = 200;
            var skills = player.GetPart<SkillsPart>();
            skills.AddSkill("CudgelSkill");
            Assert.AreEqual(SkillsScreenRowState.RequirementsNotMet, Row(player, power).State);
            var refused = BuySkillAction.Execute(player, power);
            Assert.IsFalse(refused.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.MissingPrereq, refused.Reason);
            Assert.AreEqual(tree, refused.Detail);
            Assert.AreEqual(200, player.GetStatValue("SP"));
            Assert.IsFalse(skills.HasSkill(power));
            Assert.IsTrue(skills.AddSkill(tree));
            Assert.AreEqual(SkillsScreenRowState.Buyable, Row(player, power).State);
            Assert.IsTrue(BuySkillAction.Execute(player, power).Succeeded);
        }

        [TestCase("AcrobaticsSkill", "Acrobatics_Vault", "Agility", 15)]
        [TestCase("PersuasionSkill", "Persuasion_Recruit", "Ego", 16)]
        public void AffordablePowersRetainTheirStatRequirement(string tree, string power, string stat, int minimum)
        {
            var player = harness.Factory.CreateEntity("Player");
            player.GetStat("SP").BaseValue = 1;
            player.GetPart<SkillsPart>().AddSkill(tree);
            player.GetStat(stat).BaseValue = minimum - 1;
            var refused = BuySkillAction.Execute(player, power);
            Assert.AreEqual(BuySkillAction.FailureReason.StatMinNotMet, refused.Reason);
            Assert.AreEqual(SkillsScreenRowState.RequirementsNotMet, Row(player, power).State);
            Assert.AreEqual(1, player.GetStatValue("SP"));
            player.GetStat(stat).BaseValue = minimum;
            Assert.AreEqual(SkillsScreenRowState.Buyable, Row(player, power).State);
            Assert.IsTrue(BuySkillAction.Execute(player, power).Succeeded);
        }

        [Test]
        public void BookAndStartingGrantsRemainExemptWithoutSpendingPoints()
        {
            var player = harness.Factory.CreateEntity("Player");
            var skills = player.GetPart<SkillsPart>();
            Assert.AreEqual(StartingSpellKit.SpellClasses.Length, StartingSpellKit.GrantAll(player));
            Assert.IsFalse(skills.HasSkill("SpellcraftSkill"));
            var book = harness.Factory.CreateEntity("WardGleamGrimoire");
            var read = GameEvent.New("InventoryAction");
            read.SetParameter("Command", "ReadGrimoire"); read.SetParameter("Actor", (object)player);
            book.FireEvent(read); read.Release();
            Assert.IsTrue(skills.HasSkill("Spellcraft_WardGleam"));
            Assert.IsFalse(skills.HasSkill("SpellcraftSkill"));
            Assert.AreEqual(0, player.GetStatValue("SP"));
            Assert.AreEqual(SkillsScreenRowState.Owned, Row(player, "Spellcraft_WardGleam").State);
            Assert.IsFalse(BuySkillAction.Execute(player, "Spellcraft_WardGleam").Succeeded);
            Assert.AreEqual(0, player.GetStatValue("SP"));
        }

        [Test]
        public void RestoringLearnedChildWithoutRootKeepsItsAbility()
        {
            var player = harness.Factory.CreateEntity("Player");
            var skills = player.GetPart<SkillsPart>();
            Assert.IsTrue(skills.AddSkill("Acrobatics_Vault", "older-save"));
            var ability = player.GetPart<Acrobatics_Vault>().ActivatedAbilityID;
            player = PartRoundTripHelper.RoundTripEntityViaTokenGraph(player);
            skills = player.GetPart<SkillsPart>();
            Assert.IsTrue(skills.HasSkill("Acrobatics_Vault"));
            Assert.IsFalse(skills.HasSkill("AcrobaticsSkill"));
            Assert.AreEqual(ability, player.GetPart<Acrobatics_Vault>().ActivatedAbilityID);
            Assert.IsNotNull(player.GetPart<ActivatedAbilitiesPart>().GetAbility(ability));
            Assert.AreEqual(SkillsScreenRowState.Owned, Row(player, "Acrobatics_Vault").State);
            Assert.AreEqual(BuySkillAction.FailureReason.AlreadyOwned, BuySkillAction.Execute(player, "Acrobatics_Vault").Reason);
        }

        private static void Level(Entity player)
        {
            player.GetStat("Experience").BaseValue = LevelingSystem.XPToNextLevel(player.GetStatValue("Level"));
            LevelingSystem.CheckLevelUp(player, null);
        }
        internal static SkillsScreenRow Row(Entity player, string power) => SkillsScreenStateBuilder.Build(player).Rows.Single(r => r.Class == power);
    }
}
