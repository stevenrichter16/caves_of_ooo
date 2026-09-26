using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityCombatContentTests
    {
        EntityFactory factory, previousFactory;
        System.Random previousRng;
        [SetUp] public void Setup()
        {
            previousFactory = LoadoutPart.Factory; previousRng = LoadoutPart.Rng;
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LoadoutPart.Factory = factory; LoadoutPart.Rng = new System.Random(812);
        }
        [TearDown] public void TearDown() { LoadoutPart.Factory = previousFactory; LoadoutPart.Rng = previousRng; }

        [TestCase("MarlbackGleaner", "ShortBlades_Shank", .40f)]
        [TestCase("MarlbackTunnelguard", "ShortBlades_Shank", .35f)]
        [TestCase("DesertBandit", "LongBlades_Lunge", .35f)]
        [TestCase("AmbushBandit", "LongBlades_Lunge", .25f)]
        [TestCase("MarlbackWallkeeper", "LongBlades_Lunge", .15f)]
        [TestCase("MarlbackBreacher", "Axe_Berserk", .05f)]
        [TestCase("CaveSlime", "Corrosion_AcidSpray", 0f)]
        [TestCase("IceWight", "Cryomancy_IceLance", 0f)]
        [TestCase("CharredHusk", "Pyromancy_EmberSpit", 0f)]
        [TestCase("RuneCultist", "Cryomancy_IceLance", .40f)]
        public void AuthoredCreatureActuallyOwnsRegisteredSkill(string blueprint, string skillClass, float flee)
        {
            var actor = factory.CreateEntity(blueprint);
            Assert.IsNotNull(actor.GetPart("CombatTactics"), blueprint + " must have an authored usable kit.");
            var skills = actor.GetPart<SkillsPart>();
            Assert.IsTrue(skills != null && skills.HasSkill(skillClass));
            var skill = skills.SkillList.Single(s => s.GetType().Name == skillClass);
            var ability = actor.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID);
            Assert.IsNotNull(ability); Assert.Greater(ability.MaxCooldown, 0);
            Assert.AreEqual(flee, actor.GetPart<BrainPart>().FleeThreshold, .0001f);
        }

        [TestCase("Player")] [TestCase("GlassScorpion")] [TestCase("Shambler")]
        [TestCase("Reedfrog")] [TestCase("MarlbackScrabbler")]
        public void UnauthoredCreaturesKeepTheirExistingBehavior(string blueprint)
            => Assert.IsNull(factory.CreateEntity(blueprint).GetPart("CombatTactics"));

        [Test] public void WarlordActuallyWieldsAnAxeForBerserk()
            => Assert.IsNotNull(SkillCombatHelpers.FindEquippedWeaponOfClass(factory.CreateEntity("MarlbackBreacher"), "Axe"));

        [Test] public void IceWightHasARealDepthGroupSourceWithoutEnteringFirstTier()
        {
            Assert.IsFalse(PopulationTable.UndergroundTier(1).Entries.Any(e => e.BlueprintName == "IceWight"));
            var row = PopulationTable.UndergroundTier(3).Entries.SingleOrDefault(e => e.BlueprintName == "IceWight");
            Assert.IsNotNull(row); Assert.AreEqual("DepthEncounter", row.EncounterGroup);
            Assert.AreEqual(1, row.MinCount); Assert.AreEqual(1, row.MaxCount); Assert.Greater(row.Weight, 0);
        }
    }
}
