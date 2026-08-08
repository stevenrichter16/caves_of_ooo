using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase B3 — the Persuasion skill tree
    /// (Docs/BIOME-OVERHAUL.md §6 B3, log in Docs/BIOME-OVERHAUL-LOG.md).
    /// The COMPLETE follower stack shipped dormant: Persuasion_Recruit /
    /// Persuasion_Dismiss skills, RecruitedEffect, FollowLeaderGoal with
    /// combat assist, party zone-transit, companion limits — all gated
    /// to zero because no Persuasion tree JSON existed in
    /// Content/Data/Skills, so the skills could never be bought.
    /// This authors the tree; the skills were already tested.
    /// </summary>
    [TestFixture]
    public class BiomePersuasionTests
    {
        [SetUp]
        public void Setup() => SkillRegistry.ResetForTests();

        [TearDown]
        public void TearDown() => SkillRegistry.ResetForTests();

        private static string ShippedJson() => File.ReadAllText(Path.Combine(
            Application.dataPath, "Resources/Content/Data/Skills/Persuasion.json"));

        [Test]
        public void PersuasionTree_ShipsAndLoads()
        {
            SkillRegistry.InitializeFromJson(ShippedJson());

            Assert.IsTrue(SkillRegistry.TryGetSkillByName("Persuasion", out var tree));
            Assert.AreEqual("PersuasionSkill", tree.Class,
                "tree-root marker class (AcrobaticsSkill shape)");

            Assert.IsTrue(SkillRegistry.TryGetPowerByClass("Persuasion_Recruit", out var recruit),
                "the recruit power is purchasable content");
            Assert.AreEqual("Ego", recruit.Attribute,
                "persuasion runs on Ego — same stat the recruit roll uses");

            Assert.IsTrue(SkillRegistry.TryGetPowerByClass("Persuasion_Dismiss", out _),
                "dismiss ships alongside recruit");
        }

        [Test]
        public void PersuasionSkill_TreeRootClass_Exists()
        {
            // The tree entry's Class must be a real BaseSkillPart or the
            // purchase flow dead-ends at instantiation.
            var skill = new PersuasionSkill();
            Assert.AreEqual(nameof(PersuasionSkill), skill.Name);
        }

        [Test]
        public void OwningRecruit_RaisesCompanionLimit()
        {
            var actor = new Entity { ID = "leader" };
            actor.AddPart(new RenderPart { DisplayName = "leader" });
            actor.AddPart(new ActivatedAbilitiesPart());
            var skills = new SkillsPart();
            actor.AddPart(skills);

            Assert.AreEqual(0,
                GetCompanionLimitEvent.GetFor(actor, GetCompanionLimitEvent.MEANS_RECRUIT),
                "baseline: no followers without the skill");

            Assert.IsTrue(skills.AddSkill(nameof(Persuasion_Recruit), source: "test"));
            Assert.AreEqual(1,
                GetCompanionLimitEvent.GetFor(actor, GetCompanionLimitEvent.MEANS_RECRUIT),
                "owning Persuasion_Recruit grants one follower slot");
            Assert.AreEqual(0, GetCompanionLimitEvent.GetFor(actor, "other-means"),
                "the bump is channel-scoped — other means stay at their base");
        }
    }
}
