using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityOriginalEnemyTests
    {
        EntityFactory factory, previousFactory;
        System.Random previousRng;
        [SetUp] public void Setup()
        {
            previousFactory = LoadoutPart.Factory; previousRng = LoadoutPart.Rng;
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LoadoutPart.Factory = factory; LoadoutPart.Rng = new System.Random(718);
        }
        [TearDown] public void TearDown() { LoadoutPart.Factory = previousFactory; LoadoutPart.Rng = previousRng; FactionManager.Reset(); }

        [TestCase("MarlbackScrabbler")] [TestCase("MarlbackGleaner")] [TestCase("MarlbackTunnelguard")]
        [TestCase("MarlbackWallkeeper")] [TestCase("MarlbackBreacher")] [TestCase("MarlbackCorpse")]
        [TestCase("GroveLanternMoth")] [TestCase("BreacherCleaver")] [TestCase("SootGremlin")] [TestCase("DirtGnome")]
        public void OriginalIdentityExists(string id) => Assert.IsNotNull(factory.CreateEntity(id), id);

        [TestCase("Snapjaw")] [TestCase("SnapjawScavenger")] [TestCase("SnapjawHunter")]
        [TestCase("SnapjawChieftain")] [TestCase("SnapjawWarlord")] [TestCase("SnapjawCorpse")] [TestCase("GlowMoth")]
        public void RetiredIdentityCannotSpawn(string id) => Assert.IsFalse(factory.Blueprints.ContainsKey(id), id);

        [TestCase("MarlbackScrabbler")] [TestCase("MarlbackGleaner")] [TestCase("MarlbackTunnelguard")]
        [TestCase("MarlbackWallkeeper")] [TestCase("MarlbackBreacher")]
        public void EntireLifeCycleHasOriginalIdentity(string id)
        {
            var actor = factory.CreateEntity(id); Assert.IsNotNull(actor);
            Assert.AreEqual("OutlandRaiders", actor.GetTag("Faction"));
            Assert.AreEqual("MarlbackCorpse", actor.GetPart<CorpsePart>().CorpseBlueprint);
            StringAssert.Contains("marlback", actor.GetPart<RenderPart>().DisplayName);
            StringAssert.Contains("shale", actor.GetPart<ExaminablePart>().Description.ToLowerInvariant());
        }
        [Test] public void MothKeepsItsGentlePaintedLightEcology()
        {
            var moth = factory.CreateEntity("GroveLanternMoth"); Assert.IsNotNull(moth);
            Assert.IsTrue(moth.GetPart<BrainPart>().Passive);
            Assert.IsNull(moth.GetPart<LightSourcePart>());
            Assert.AreEqual(3, moth.GetStatValue("Hitpoints"));
        }
        [TestCase("MarlbackRake", "1d4", 1)] [TestCase("MarlbackGuardRake", "1d6", 2)]
        [TestCase("BreacherCleaver", "2d5", 2)]
        public void OriginalNaturalWeaponRetainsItsCombatProfile(string id, string damage, int pen)
        {
            var weapon = NaturalWeaponFactory.Create(id).GetPart<MeleeWeaponPart>();
            Assert.AreEqual(damage, weapon.BaseDamage); Assert.AreEqual(pen, weapon.PenBonus);
        }
        [TestCase(false)] [TestCase(true)]
        public void CoalitionRenamePreservesHostilityAndUnrelatedAllies(bool json)
        {
            if (json) FactionManager.Initialize(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Data/Factions.json")));
            else FactionManager.Initialize();
            var player = factory.CreateEntity("Player");
            var newEnemy = factory.CreateEntity("MarlbackGleaner"); Assert.IsNotNull(newEnemy);
            Assert.IsTrue(FactionManager.IsHostile(newEnemy, player));
            foreach (var id in new[] { "DesertBandit", "AmbushBandit", "SleepingTroll", "MimicChest" })
            {
                var other = factory.CreateEntity(id);
                Assert.AreEqual("OutlandRaiders", other.GetTag("Faction"));
                Assert.IsTrue(FactionManager.IsHostile(other, player));
                Assert.AreEqual(100, FactionManager.GetFeeling(newEnemy, other));
            }
            Assert.AreEqual(0, FactionManager.GetFeeling(factory.CreateEntity("GlassScorpion"), player));
        }
        [TestCase("SootGremlin")] [TestCase("DirtGnome")]
        public void QuestCreaturesDoNotHideSpeciesIdentityOrRemains(string id)
        {
            var actor = factory.CreateEntity(id); Assert.IsNotNull(actor);
            Assert.AreEqual(id, actor.BlueprintName);
            Assert.AreEqual("CreatureCorpse", actor.GetPart<CorpsePart>().CorpseBlueprint);
            Assert.AreEqual("OutlandRaiders", actor.GetTag("Faction"));
        }

        [TestCase("Snapjaw", "snapjaw", "MarlbackScrabbler", "marlback scrabbler")]
        [TestCase("SnapjawScavenger", "snapjaw scavenger", "MarlbackGleaner", "marlback gleaner")]
        [TestCase("SnapjawHunter", "snapjaw hunter", "MarlbackTunnelguard", "marlback tunnelguard")]
        [TestCase("SnapjawChieftain", "snapjaw chieftain", "MarlbackWallkeeper", "marlback wallkeeper")]
        [TestCase("SnapjawWarlord", "snapjaw warlord", "MarlbackBreacher", "marlback breacher")]
        [TestCase("SnapjawCorpse", "snapjaw corpse", "MarlbackCorpse", "marlback remains")]
        [TestCase("GlowMoth", "glow-moth", "GroveLanternMoth", "grove lantern-moth")]
        public void SavedDefaultIdentityMigratesWithoutRebuildingLiveState(string before, string display, string after, string afterDisplay)
        {
            var actor = LegacyActor(before, display);
            actor.Properties["custom"] = "preserve";
            var loaded = PartRoundTripHelper.RoundTripEntity(actor);
            Assert.AreEqual(after, loaded.BlueprintName); Assert.AreEqual(afterDisplay, loaded.GetPart<RenderPart>().DisplayName);
            Assert.AreEqual("legacy-id", loaded.ID); Assert.AreEqual(7, loaded.GetStatValue("Hitpoints"));
            Assert.AreEqual("preserve", loaded.Properties["custom"]);
            Assert.IsNull(loaded.GetPart<CombatTacticsPart>(), "identity conversion must not grant a current blueprint's skills");
            var twice = PartRoundTripHelper.RoundTripEntity(loaded);
            Assert.AreEqual(after, twice.BlueprintName); Assert.AreEqual(7, twice.GetStatValue("Hitpoints"));
        }
        [TestCase("Snapjaw", "soot gremlin", "SootGremlin")]
        [TestCase("Snapjaw", "dirt gnome", "DirtGnome")]
        [TestCase("Snapjaw", "Old Moss", "MarlbackScrabbler")]
        [TestCase("CustomSnapjawling", "My snapjaw", "CustomSnapjawling")]
        public void SavedCustomNamesAndQuestIdentitiesArePreserved(string before, string name, string after)
        {
            var source = LegacyActor(before, name);
            if (after == "SootGremlin") source.AddPart(new CavesOfOoo.Storylets.SetFactWhenSlain { Fact = "rbg_gremlin_routed", Value = 1 });
            if (after == "DirtGnome") source.AddPart(new CavesOfOoo.Storylets.AddFactWhenSlain { Fact = "warren_gnomes_routed", Amount = 1 });
            var loaded = PartRoundTripHelper.RoundTripEntity(source);
            Assert.AreEqual(after, loaded.BlueprintName); Assert.AreEqual(name, loaded.GetPart<RenderPart>().DisplayName);
        }
        [Test] public void SavedHostileFactionAndDeferredCorpseMigrateTogether()
        {
            var actor = LegacyActor("DesertBandit", "desert bandit");
            actor.Tags["Faction"] = "Snapjaws";
            actor.AddPart(new CorpsePart { CorpseBlueprint = "SnapjawCorpse" });
            var loaded = PartRoundTripHelper.RoundTripEntity(actor);
            Assert.AreEqual("DesertBandit", loaded.BlueprintName);
            Assert.AreEqual("OutlandRaiders", loaded.GetTag("Faction"));
            Assert.AreEqual("MarlbackCorpse", loaded.GetPart<CorpsePart>().CorpseBlueprint);
        }
        [Test] public void SavedReputationUsesNewCoalitionWithoutResettingItsValue()
        {
            PlayerReputation.Set("Snapjaws", 137); PlayerReputation.Set("Villagers", 62);
            using (var stream = new MemoryStream())
            {
                SaveGraphSerializer.SavePlayerReputation(new SaveWriter(stream)); stream.Position = 0;
                SaveGraphSerializer.LoadPlayerReputation(new SaveReader(stream, null));
            }
            Assert.AreEqual(137, PlayerReputation.Get("OutlandRaiders"));
            Assert.AreEqual(62, PlayerReputation.Get("Villagers"));
            Assert.IsFalse(PlayerReputation.GetAll().ContainsKey("Snapjaws"));
        }
        static Entity LegacyActor(string blueprint, string display)
        {
            var entity = new Entity { ID = "legacy-id", BlueprintName = blueprint };
            entity.AddPart(new RenderPart { DisplayName = display });
            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = 7, Min = 0, Max = 20 };
            return entity;
        }
    }
}
