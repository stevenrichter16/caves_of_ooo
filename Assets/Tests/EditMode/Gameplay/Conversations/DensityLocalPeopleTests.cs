using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DensityLocalPeopleTests
    {
        DensityLootTestScope scope;
        EntityFactory Factory => scope.Factory;
        static readonly string[] Voices = { "Mogu", "Grib", "Nam", "Sien", "Sopp" };
        [SetUp] public void Setup() { scope = new DensityLootTestScope(); ConversationManager.EndConversation(); FactionManager.Initialize(); }
        [TearDown] public void Cleanup() { ConversationManager.EndConversation(); scope.Dispose(); }
        OverworldZoneManager Manager(int seed = 518) => OverworldZoneManager.CreateDetached(Factory, seed);
        static string Label(Entity actor) => actor.GetPart<RenderPart>().DisplayName;
        static IEnumerable<Entity> Residents(Zone zone) => zone.GetAllEntities().Where(e => e.BlueprintName == "Villager" && e.GetPart<ConversationPart>()?.ConversationID == "Villager_1");

        [TestCase("Overworld.10.10.0", "Villagers")]
        [TestCase("Overworld.14.9.0", "Palimpsest")]
        [TestCase("Overworld.10.14.0", "SaccharineConcord")]
        [TestCase("Overworld.12.12.0", "PaleCuration")]
        [TestCase("Overworld.5.9.0", "BowerFolk")]
        [TestCase("Overworld.8.16.0", "TentRight")]
        [TestCase("Overworld.16.1.0", "CatacombFolk")]
        public void ActualSettlementResidentsGainPersonalNamesAndKeepTheirRoles(string zoneId, string culture)
        {
            var manager = Manager(); var zone = manager.GetZone(zoneId); var people = Residents(zone).ToArray();
            Assert.IsNotEmpty(people, "real settlement must supply the tested roles");
            foreach (var actor in people)
            {
                Assert.AreEqual(culture, actor.GetTag("Faction"));
                Assert.That(Label(actor), Does.EndWith(", villager"));
                Assert.IsNotEmpty(actor.GetProperty("LocalPersonalName", ""));
                Assert.AreEqual("Villager", actor.BlueprintName);
                Assert.AreEqual("Villager_1", actor.GetPart<ConversationPart>().ConversationID);
            }
            Assert.AreEqual(people.Length, people.Select(Label).Distinct().Count());
        }

        [Test]
        public void SameSeedReplaysNamesButDifferentSeedCanChooseDifferentPeople()
        {
            string Names(int seed) => string.Join("|", Residents(Manager(seed).GetZone("Overworld.10.10.0")).Select(Label).OrderBy(s => s));
            string first = Names(518); Assert.AreEqual(first, Names(518));
            Assert.AreNotEqual(first, Names(519));
        }

        [Test]
        public void AuthoredQuestOwnersAndNamedCastKeepNamesAndFrozenKeys()
        {
            var zone = Manager().GetZone("Overworld.10.10.0");
            Assert.IsTrue(zone.GetAllEntities().Any(e => Label(e) == "Ellun"));
            Assert.IsTrue(zone.GetAllEntities().Any(e => e.GetPart<ConversationPart>()?.ConversationID == "RootBeerGuy_Quest" && Label(e) == "Hallun"));
            foreach (string name in new[] { "StillleafSearcher", "StillleafIndexer", "CatacombWarden", "CaveHermit", "DesertHermit", "JungleHermit", "RuinsHermit" })
            {
                var actor = Factory.CreateEntity(name); string before = Label(actor), id = actor.ID;
                var manager = Manager(); var fresh = new Zone("Overworld.10.10.0"); fresh.AddEntity(actor, 10, 10); Finish(manager, fresh);
                Assert.AreEqual(before, Label(actor)); Assert.AreEqual(id, actor.ID);
                Assert.IsFalse(actor.Properties.ContainsKey("LocalPersonalName"));
            }
        }

        [Test]
        public void CachedOldResidentsAreNeverRenamedOnAccessOrFullLoad()
        {
            var manager = Manager(); var zone = new Zone("Overworld.10.10.0"); var actor = Factory.CreateEntity("Villager"); zone.AddEntity(actor, 10, 10);
            manager.SetActiveZone(zone); Assert.AreSame(zone, manager.GetZone(zone.ZoneID)); Assert.AreEqual("villager", Label(actor));
            var state = RoundTrip(manager, actor, new Entity { BlueprintName = "World" });
            Assert.AreEqual("villager", Label(state.Player)); Assert.IsFalse(state.Player.Properties.ContainsKey("LocalPersonalName"));
        }

        [TestCase("Mogu")] [TestCase("Grib")] [TestCase("Nam")] [TestCase("Sien")] [TestCase("Sopp")]
        public void SecondFreshGroveCannotCloneAnAlreadyPlacedPerson(string blueprint)
        {
            var manager = Manager(); var first = Grove("Overworld.1.1.0", blueprint); var owner = first.GetAllEntities().Single();
            var carried = Factory.CreateEntity("Dagger"); owner.GetPart<InventoryPart>().AddObject(carried);
            Finish(manager, first); var next = Grove("Overworld.2.1.0", blueprint); var bystander = Factory.CreateEntity("ChoirTendril"); next.AddEntity(bystander, 13, 10);
            Finish(manager, next);
            Assert.IsFalse(next.GetAllEntities().Any(e => e.BlueprintName == blueprint));
            Assert.That(first.GetAllEntities(), Does.Contain(owner)); Assert.That(owner.GetPart<InventoryPart>().Objects, Does.Contain(carried));
            Assert.That(next.GetAllEntities(), Does.Contain(bystander)); Assert.IsFalse(next.GetAllEntities().Any(e => e.HasTag("Item")));
        }

        [TestCase("dead")] [TestCase("removed")] [TestCase("unloaded")]
        public void ClaimedOwnerDoesNotRespawnAfterAbsenceAndFullSaveLoad(string absence)
        {
            var manager = Manager(); var zone = Grove("Overworld.1.1.0", "Mogu"); var actor = zone.GetAllEntities().Single(); Finish(manager, zone);
            if (absence == "dead") CombatSystem.HandleDeath(actor, null, zone);
            if (absence == "removed") zone.RemoveEntity(actor);
            if (absence == "unloaded") manager.UnloadZone(zone.ZoneID);
            var safe = new Zone("Overworld.10.10.0"); var player = Factory.CreateEntity("Player"); safe.AddEntity(player, 10, 10); manager.SetActiveZone(safe);
            var state = RoundTrip(manager, player, new Entity { BlueprintName = "World" });
            var attempt = Grove("Overworld.2.1.0", "Mogu"); Finish(state.ZoneManager, attempt);
            Assert.IsFalse(attempt.GetAllEntities().Any(e => e.BlueprintName == "Mogu"));
        }

        [Test]
        public void SeparateManagersEvenWithSameSeedKeepIndependentClaims()
        {
            var a = Manager(); var b = Manager(); var za = Grove("Overworld.1.1.0", "Mogu"); var zb = Grove("Overworld.1.1.0", "Mogu");
            Finish(a, za); Finish(b, zb);
            Assert.AreEqual(1, za.GetAllEntities().Count(e => e.BlueprintName == "Mogu"));
            Assert.AreEqual(1, zb.GetAllEntities().Count(e => e.BlueprintName == "Mogu"));
        }

        [Test]
        public void SavingLegacyCachedOwnerAdoptsItWithoutRenamingOrRemovingIt()
        {
            var manager = Manager(); var old = Grove("Overworld.1.1.0", "Mogu"); var owner = old.GetAllEntities().Single(); string id = owner.ID;
            manager.SetActiveZone(old); // existing cached graph, deliberately never freshly generated
            var state = RoundTrip(manager, owner, new Entity { BlueprintName = "World" });
            Assert.AreEqual(id, state.Player.ID); Assert.AreEqual("Solm", Label(state.Player));
            var next = Grove("Overworld.2.1.0", "Mogu"); Finish(state.ZoneManager, next);
            Assert.IsFalse(next.GetAllEntities().Any(e => e.BlueprintName == "Mogu"));
        }

        [TestCase("Overworld.10.10.0", "Sill")]
        [TestCase("Overworld.14.9.0", "Quillhold")]
        [TestCase("Overworld.10.14.0", "Tally")]
        [TestCase("Overworld.12.12.0", "Marrowstye")]
        [TestCase("Overworld.5.9.0", "Posy")]
        [TestCase("Overworld.8.16.0", "Wellmeet")]
        [TestCase("Overworld.16.1.0", "the Quiet's Door")]
        public void ActualConversationKnowsItsSettlementWithoutMutatingSharedData(string zoneId, string place)
        {
            var manager = Manager(); var zone = manager.GetZone(zoneId); var speaker = Residents(zone).First(); var player = Factory.CreateEntity("Player"); zone.AddEntity(player, 2, 2);
            var node = ConversationLoader.Get("Villager_1").GetStartNode(); string original = node.Text;
            Assert.IsTrue(ConversationManager.StartConversation(speaker, player));
            Assert.That(RenderedText(), Does.Contain(place)); Assert.AreEqual(original, node.Text);
            var passing = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == "PassingThrough"); Assert.GreaterOrEqual(passing, 0);
            Assert.IsTrue(ConversationManager.SelectChoice(passing));
            var scribe = zone.GetAllEntities().First(e => e.BlueprintName == "Scribe");
            Assert.That(RenderedText(), Does.Contain(Label(scribe)));
        }

        [Test]
        public void LocalLeadStopsNamingAnAbsentOrDeadServiceOwner()
        {
            var manager = Manager(); var zone = manager.GetZone("Overworld.10.10.0"); var speaker = Residents(zone).First(); var player = Factory.CreateEntity("Player"); zone.AddEntity(player, 2, 2);
            Assert.IsTrue(ConversationManager.StartConversation(speaker, player));
            ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == "PassingThrough"));
            var scribe = zone.GetAllEntities().First(e => e.BlueprintName == "Scribe"); string before = Label(scribe);
            Assert.That(RenderedText(), Does.Contain(before)); zone.RemoveEntity(scribe); Assert.That(RenderedText(), Does.Not.Contain(before));
        }

        [Test]
        public void LocalWaterLineTracksSavedRepairStateRatherThanInventingAWorkingWell()
        {
            var manager = Manager(); var zone = manager.GetZone("Overworld.10.10.0"); var speaker = Residents(zone).First(); var player = Factory.CreateEntity("Player"); zone.AddEntity(player, 2, 2);
            Assert.IsTrue(ConversationManager.StartConversation(speaker, player));
            ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == "Dangers"));
            var site = manager.SettlementManager.GetSite(zone.ZoneID, SettlementSiteDefinitions.MainWellSiteId);
            site.Stage = RepairStage.Fouled; string broken = RenderedText(); site.Stage = RepairStage.StableRepair; string repaired = RenderedText();
            Assert.AreNotEqual(broken, repaired); Assert.AreEqual(RepairStage.StableRepair, site.Stage);
        }

        static string RenderedText()
        {
            var p = typeof(ConversationManager).GetProperty("CurrentText", BindingFlags.Public | BindingFlags.Static);
            return p == null ? ConversationManager.CurrentNode.Text : (string)p.GetValue(null);
        }
        Zone Grove(string id, string blueprint) { var z = new Zone(id); z.AddEntity(Factory.CreateEntity(blueprint), 10, 10); return z; }
        static void Finish(OverworldZoneManager manager, Zone zone)
        {
            typeof(OverworldZoneManager).GetMethod("OnZoneGenerated", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, new object[] { zone, zone.ZoneID });
            manager.SetActiveZone(zone);
        }
        GameSessionState RoundTrip(OverworldZoneManager manager, Entity player, Entity world)
        {
            var state = GameSessionState.Capture("local-people", "test", manager, new TurnManager(), player, world: world);
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                return GameSessionState.Load(new SaveReader(stream, Factory));
            }
        }
    }
}
