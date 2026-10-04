using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Factory, real dialogue/inventory routes and serialized stock evidence.
    // Filing authority/haul paths and quarantine turns have separate fixtures.
    public sealed class CurationReceivingContentTests
    {
        const string ZoneID = "Overworld.12.12.0";
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        readonly List<Action> restore = new List<Action>();
        HaulingContentScope scope;
        EntityFactory Factory => scope.Factory;
        Zone zone;
        Entity player;
        InventoryPart Inventory => player.GetPart<InventoryPart>();

        void Keep(Type type, string name)
        {
            var field = type.GetField(name, Static); Assert.NotNull(field, type.Name + "." + name);
            var value = field.GetValue(null);
            if (value is IDictionary dictionary)
            {
                var rows = new List<DictionaryEntry>(); foreach (DictionaryEntry row in dictionary) rows.Add(row);
                restore.Add(() => { if (!field.IsInitOnly) field.SetValue(null, value); dictionary.Clear(); foreach (var row in rows) dictionary.Add(row.Key, row.Value); });
            }
            else if (value is HashSet<string> set)
            { var rows = set.ToArray(); restore.Add(() => { field.SetValue(null, value); set.Clear(); foreach (string row in rows) set.Add(row); }); }
            else restore.Add(() => field.SetValue(null, value));
        }

        [SetUp] public void Setup()
        {
            // Borrow before the game-session fixture touches conversation/report state.
            Keep(typeof(SpreadDiscoveryReports), "offers"); Keep(typeof(SpreadDiscoveryReports), "revision");
            foreach (string name in new[] { "_cache", "_loaded" }) Keep(typeof(ConversationLoader), name);
            foreach (string name in new[] { "_factionFeelings", "_registeredFactions", "_factionData" }) Keep(typeof(FactionManager), name);
            Keep(typeof(StoryletRegistry), "_storylets"); Keep(typeof(StoryletRegistry), "_loaded");
            foreach (string name in new[] { "byId", "documents", "issues" }) Keep(typeof(ReadableDocumentCatalog), name);
            scope = new HaulingContentScope(); scope.Seed(64);
            FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Factions.json")));
            foreach (string file in Directory.GetFiles(Path.Combine(Application.dataPath, "Resources/Content/Conversations"), "*.json", SearchOption.AllDirectories))
                ConversationLoader.LoadFromJson(File.ReadAllText(file), Path.GetFileName(file));
            ReadableDocumentCatalog.ResetForTests();
            StoryletPart.Current = new StoryletPart(); NarrativeStatePart.Current = new NarrativeStatePart();
            ConversationActions.Factory = Factory;
            zone = new Zone(ZoneID); player = Factory.CreateEntity("Player");
            Assert.True(zone.AddEntity(player, 5, 5)); SettlementRuntime.ActiveZone = zone;
        }
        [TearDown] public void Cleanup()
        {
            ConversationManager.EndConversation();
            try { scope?.Dispose(); }
            finally { for (int i = restore.Count - 1; i >= 0; i--) restore[i](); restore.Clear(); }
        }
        Entity Create(string blueprint)
        {
            Assert.True(Factory.Blueprints.ContainsKey(blueprint), "Authored receiving-yard blueprint: " + blueprint);
            var entity = Factory.CreateEntity(blueprint); Assert.NotNull(entity); return entity;
        }
        Entity Place(string blueprint, int x = 6, int y = 5)
        { var entity = Create(blueprint); Assert.True(zone.AddEntity(entity, x, y)); return entity; }
        void Act(Entity target, string command)
        {
            var e = GameEvent.New("InventoryAction"); e.SetParameter("Actor", player); e.SetParameter("Zone", zone); e.SetParameter("Command", command);
            target.FireEventAndRelease(e);
        }
        OverworldZoneManager Generate(int seed = 64)
        {
            zone.RemoveEntity(player); var manager = OverworldZoneManager.CreateDetached(Factory, seed, true);
            zone = manager.GetZone(ZoneID); SettlementRuntime.ActiveZone = zone; return manager;
        }
        Entity Owner(string blueprint)
        {
            var all = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == blueprint).ToArray();
            Assert.AreEqual(1, all.Length, "One current generated " + blueprint); return all[0];
        }
        void Beside(Entity target)
        {
            var at = zone.GetEntityCell(target); Assert.NotNull(at);
            var cell = new[] { (at.X - 1, at.Y), (at.X + 1, at.Y), (at.X, at.Y - 1), (at.X, at.Y + 1) }
                .Select(p => zone.GetCell(p.Item1, p.Item2)).FirstOrDefault(c => c != null && zone.CanPlaceFootprint(player, c.X, c.Y));
            Assert.NotNull(cell, "Actual reachable frontage");
            Assert.True(zone.GetEntityCell(player) == null ? zone.AddEntity(player, cell.X, cell.Y) : zone.MoveEntity(player, cell.X, cell.Y));
        }

        [TestCase("CurationIntakeFiler", "CurationIntakeFiler_1")]
        [TestCase("CurationJuniorIndexer", "CurationJuniorIndexer_1")]
        public void OriginalWorkersHaveLocalPublicConversationWithoutRemoteQuestOrFreeRewards(string blueprint, string conversation)
        {
            var worker = Place(blueprint); Assert.AreEqual("PaleCuration", worker.GetTag("Faction"));
            Assert.False(FactionManager.IsHostile(worker, player));
            Assert.AreEqual(conversation, worker.GetPart<ConversationPart>()?.ConversationID);
            var dialogue = ConversationLoader.Get(conversation); Assert.NotNull(dialogue);
            var choices = dialogue.Nodes.SelectMany(n => n.Choices).ToArray();
            Assert.False(choices.SelectMany(c => c.Actions ?? new List<ConversationParam>()).Any(a =>
                new[] { "StartQuest", "CompleteQuest", "GiveDrams", "ChangeFactionFeeling", "GiveItem" }.Contains(a.Key)), "Local conversation must not manufacture rewards or borrow courier progress.");
            Assert.False(choices.SelectMany(c => c.Predicates ?? new List<ConversationParam>()).Any(p => p.Value == "BogBodyCourier"));
            int purse = TradeSystem.GetDrams(player), reputation = PlayerReputation.Get("PaleCuration");
            for (int repeat = 0; repeat < 2; repeat++)
            {
                Act(worker, "Chat"); Assert.True(ConversationManager.IsActive); Assert.AreSame(worker, ConversationManager.Speaker);
                var visible = ConversationManager.VisibleChoices;
                int branch = visible.ToList().FindIndex(c => !string.IsNullOrEmpty(c.Target) && c.Target != "End" && c.Target != "Start"
                    && (c.Actions == null || c.Actions.Count == 0));
                Assert.GreaterOrEqual(branch, 0, "An ordinary explorer can ask about the local work.");
                ConversationManager.SelectChoice(branch); Assert.True(ConversationManager.IsActive);
                Assert.Greater(ConversationManager.CurrentText.Length, 50); ConversationManager.EndConversation();
            }
            Assert.AreEqual(purse, TradeSystem.GetDrams(player)); Assert.AreEqual(reputation, PlayerReputation.Get("PaleCuration"));
            Assert.False(StoryletPart.Current.IsQuestActive("BogBodyCourier")); Assert.False(StoryletPart.Current.IsQuestCompleted("BogBodyCourier"));
        }

        [Test]
        public void OriginalClerkExplainsTheReceivingHallWithoutRequiringCourierPaper()
        {
            var clerk = Place("FilerClerk"); Act(clerk, "Chat"); Assert.True(ConversationManager.IsActive);
            Assert.False(ConversationManager.VisibleChoices.Any(c => c.Text.StartsWith("[Deliver]", StringComparison.Ordinal)));
            int branch = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target != "End" && c.Target != "NoPaper"
                && c.Target != "Start" && !string.IsNullOrEmpty(c.Target));
            Assert.GreaterOrEqual(branch, 0, "No-paper visitors now get useful public context.");
            int purse = TradeSystem.GetDrams(player); ConversationManager.SelectChoice(branch);
            Assert.Greater(ConversationManager.CurrentText.Length, 50); Assert.AreEqual(purse, TradeSystem.GetDrams(player));
            Assert.False(StoryletPart.Current.IsQuestActive("BogBodyCourier"));
        }

        [Test]
        public void OriginalCourierStillConsumesOnlyItsCarriedParcelAndPaysExactlyOnce()
        {
            Generate(); Owner("CurationIntakeIndex"); var clerk = Owner("FilerClerk"); Beside(clerk);
            StoryletRegistry.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Storylets/BogBodyCourier.json")));
            StoryletPart.Current.StartQuest(new QuestState { QuestId = "BogBodyCourier", CurrentStageIndex = 0 });
            Act(clerk, "Chat"); Assert.False(ConversationManager.VisibleChoices.Any(c => c.Text.StartsWith("[Deliver]", StringComparison.Ordinal)));
            ConversationManager.EndConversation(); var parcel = Create("SealedBogTakenBody"); Assert.True(Inventory.AddObject(parcel));
            int purse = TradeSystem.GetDrams(player), reputation = PlayerReputation.Get("PaleCuration");
            Act(clerk, "Chat"); int choice = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Text.StartsWith("[Deliver]", StringComparison.Ordinal));
            Assert.GreaterOrEqual(choice, 0); ConversationManager.SelectChoice(choice); ConversationManager.EndConversation();
            Assert.False(Inventory.Contains(parcel)); Assert.True(StoryletPart.Current.IsQuestCompleted("BogBodyCourier"));
            Assert.AreEqual(purse + 25, TradeSystem.GetDrams(player)); Assert.AreEqual(reputation + 10, PlayerReputation.Get("PaleCuration"));
            Assert.True(Inventory.AddObject(Create("SealedBogTakenBody"))); Act(clerk, "Chat");
            Assert.False(ConversationManager.VisibleChoices.Any(c => c.Text.StartsWith("[Deliver]", StringComparison.Ordinal)));
            Assert.AreEqual(purse + 25, TradeSystem.GetDrams(player));
            Assert.AreEqual(1, Owner("CurationIntakeIndex").GetPart<InventoryPart>().Objects.Count(e => e.BlueprintName == "CurationCounterfoil"), "Courier delivery cannot issue the unrelated local certificate.");
        }

        [TestCase("CurationCounterfoil", "marrowstye-intake-tools")]
        [TestCase("CurationInspectionKey", "marrowstye-quarantine")]
        public void PhysicalAccessItemsHaveDistinctReusableKeys(string blueprint, string keyId)
        {
            var item = Create(blueprint); Assert.AreEqual(keyId, item.GetPart<KeyPart>()?.KeyId);
            Assert.True(item.GetPart<PhysicsPart>().Takeable); Assert.NotNull(item.GetPart<ExaminablePart>());
            Assert.False(item.HasPart<BitLockerPart>()); Assert.False(item.HasTag("Creature"));
        }

        [TestCase(64)] [TestCase(1729)]
        public void ActualLockedCabinetHasOneFiniteWorkKitAndOnlyMatchingCounterfoilAllowsTaking(int seed)
        {
            Generate(seed); var cabinet = Owner("CurationToolCabinet"); Beside(cabinet);
            var container = cabinet.GetPart<ContainerPart>(); Assert.NotNull(container); Assert.False(container.Locked);
            var keyLock = cabinet.GetPart<LockPart>(); Assert.NotNull(keyLock); Assert.True(keyLock.IsLocked);
            Assert.AreEqual("marrowstye-intake-tools", keyLock.KeyId);
            CollectionAssert.AreEquivalent(new[] { "CurationSaltRake", "CurationInspectionKey", "CurationTransferDocket", "CurationDiscrepancyReport" }, container.Contents.Select(e => e.BlueprintName));
            var stock = container.Contents.ToArray(); var wrong = Create("CurationInspectionKey"); Assert.True(Inventory.AddObject(wrong));
            Act(cabinet, "Unlock"); Assert.True(keyLock.IsLocked);
            Assert.False(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cabinet, stock[0]), player, zone).Success);
            // This test exercises key/stock semantics. Earning this exact kind of key is covered by the filing fixture.
            var right = Create("CurationCounterfoil"); Assert.True(Inventory.AddObject(right)); Act(cabinet, "Unlock");
            Assert.False(keyLock.IsLocked); Assert.True(Inventory.Contains(right)); Assert.True(Inventory.Contains(wrong));
            foreach (var item in stock) Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cabinet, item), player, zone).Success);
            Assert.IsEmpty(container.Contents); Assert.AreEqual(0, InventorySystem.TakeAllFromContainer(player, cabinet));
            foreach (var item in stock) { Assert.AreSame(player, item.GetPart<PhysicsPart>().InInventory); Assert.False(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cabinet, item), player, zone).Success); }
        }

        [Test]
        public void DepletedGeneratedCabinetAndPhysicalKeysSurviveSessionAndCachedReentry()
        {
            var manager = Generate(); var cabinet = Owner("CurationToolCabinet"); Beside(cabinet);
            var key = Create("CurationCounterfoil"); Assert.True(Inventory.AddObject(key)); Act(cabinet, "Unlock");
            var stock = cabinet.GetPart<ContainerPart>().Contents.ToArray(); Assert.AreEqual(4, stock.Length);
            foreach (var item in stock) Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cabinet, item), player, zone).Success);
            manager.SetActiveZone(zone); var state = GameSessionState.Capture("curation-stock", "core-content-test", manager, null, player);
            GameSessionState loaded;
            using (var stream = new MemoryStream()) { state.Save(new SaveWriter(stream)); stream.Position = 0; loaded = GameSessionState.Load(new SaveReader(stream, Factory)); }
            var returned = loaded.ZoneManager.GetZone(ZoneID); var restored = returned.GetReadOnlyEntities().Single(e => e.ID == cabinet.ID);
            Assert.False(restored.GetPart<LockPart>().IsLocked); Assert.IsEmpty(restored.GetPart<ContainerPart>().Contents);
            var inventory = loaded.Player.GetPart<InventoryPart>();
            foreach (var item in stock.Concat(new[] { key })) Assert.AreEqual(1, inventory.Objects.Count(e => e.ID == item.ID));
            loaded.ZoneManager.UnloadZone(ZoneID); Assert.AreSame(returned, loaded.ZoneManager.GetZone(ZoneID));
            Assert.IsEmpty(restored.GetPart<ContainerPart>().Contents); Assert.AreEqual(1, returned.GetReadOnlyEntities().Count(e => e.BlueprintName == "CurationToolCabinet"));
        }

        [TestCase("CurationTransferDocket")] [TestCase("CurationDiscrepancyReport")]
        public void LocalDocumentsReadThroughRealActionAndRefuseRemovedOrRemoteOwners(string blueprint)
        {
            var item = Place(blueprint); var part = item.GetPart<ReadableDocumentPart>(); Assert.NotNull(part);
            var entry = ReadableDocumentCatalog.Get(part.DocumentId); Assert.NotNull(entry); Assert.AreEqual(blueprint, entry.Blueprint);
            Assert.False(entry.Source.StartsWith("Lore/Codex/", StringComparison.Ordinal), "Authored local cases must not masquerade as canonical codex transcriptions.");
            int purse = TradeSystem.GetDrams(player); Act(item, "ReadDocument"); Assert.AreEqual(entry.Title + "\n\n" + entry.Text, MessageLog.ConsumeAnnouncement());
            Assert.True(zone.MoveEntity(item, 20, 20)); Assert.False(part.TryRead(player, zone)); Assert.False(MessageLog.HasPendingAnnouncement);
            zone.RemoveEntity(item); Assert.False(part.TryRead(player, zone)); Assert.True(Inventory.AddObject(item));
            Act(item, "ReadDocument"); Assert.AreEqual(entry.Title + "\n\n" + entry.Text, MessageLog.ConsumeAnnouncement());
            Assert.True(Inventory.Contains(item)); Assert.AreEqual(purse, TradeSystem.GetDrams(player)); Assert.False(StoryletPart.Current.IsQuestActive("BogBodyCourier"));
        }

        [Test]
        public void SaltRakeIsAnActualEquippableCudgelAndBenchDoesNotPretendToCureBodies()
        {
            var rake = Create("CurationSaltRake"); Assert.True(rake.HasTag("Cudgel"));
            Assert.NotNull(rake.GetPart<MeleeWeaponPart>()); Assert.NotNull(rake.GetPart<EquippablePart>());
            Assert.True(Inventory.AddObject(rake)); Assert.True(InventorySystem.ExecuteCommand(new EquipCommand(rake), player, zone).Success);
            Assert.True(Inventory.GetAllEquipped().Contains(rake)); Assert.False(rake.HasTag("Natural"));
            var bench = Place("CurationSaltBench"); Assert.NotNull(bench.GetPart<ExaminablePart>());
            Assert.False(WorldInteractionSystem.GatherActions(bench, player).Any(a => a.Command == "CureBody" || a.Command == "Tinker"));
            Assert.False(player.HasPart<BitLockerPart>());
        }

        [TestCase("CurationIntakeFiler")]
        [TestCase("CurationJuniorIndexer")]
        public void OrdinaryVisitorCanAskWorkersAboutActualAnnexRoutesWithoutAcceptingWork(string blueprint)
        {
            var worker = Place(blueprint);
            int purse = TradeSystem.GetDrams(player), reputation = PlayerReputation.Get("PaleCuration");
            Act(worker, "Chat"); Assert.True(ConversationManager.IsActive);
            int choice = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == "Annex");
            Assert.GreaterOrEqual(choice, 0, "The expanded wing must be discoverable through an ordinary public conversation.");
            var offer = ConversationManager.VisibleChoices[choice];
            Assert.IsTrue(offer.Actions == null || offer.Actions.Count == 0, "Information must not complete or accept a job.");
            ConversationManager.SelectChoice(choice);
            StringAssert.Contains("gallery", ConversationManager.CurrentText);
            StringAssert.Contains("holding", ConversationManager.CurrentText);
            StringAssert.Contains("service", ConversationManager.CurrentText);
            Assert.AreEqual(purse, TradeSystem.GetDrams(player));
            Assert.AreEqual(reputation, PlayerReputation.Get("PaleCuration"));
            Assert.False(StoryletPart.Current.IsQuestActive("BogBodyCourier"));
        }

        [Test]
        public void AnnexMaintenancePlacardReadsThroughActualWorldActionWithoutBeingPortable()
        {
            var placard = Place("CurationAnnexPlacard");
            Assert.False(placard.GetPart<PhysicsPart>().Takeable);
            var part = placard.GetPart<ReadableDocumentPart>(); Assert.NotNull(part);
            Assert.AreEqual("curation-annex-maintenance", part.DocumentId);
            var entry = ReadableDocumentCatalog.Get(part.DocumentId); Assert.NotNull(entry);
            Assert.AreEqual(placard.BlueprintName, entry.Blueprint);
            StringAssert.Contains("original Marrowstye", entry.Source);
            Act(placard, "ReadDocument");
            Assert.AreEqual(entry.Title + "\n\n" + entry.Text, MessageLog.ConsumeAnnouncement());
            StringAssert.Contains("service gate", entry.Text);
            StringAssert.Contains("holding chamber", entry.Text);
            Assert.True(zone.MoveEntity(placard, 20, 20));
            Assert.False(part.TryRead(player, zone));
            Assert.False(MessageLog.HasPendingAnnouncement);
        }

        [Test]
        public void HalfSetIsOriginalBloomHostileWithPlainTendrilAndNoDiseasePayload()
        {
            Generate(); var enemy = Owner("CurationHalfSet"); var worker = Owner("CurationIntakeFiler");
            Assert.AreEqual("Beasts", enemy.GetTag("Faction"));
            Assert.Null(FactionManager.GetFactionData("DrivingBloom"), "Bloom is a condition/pressure, not a reputation society.");
            Assert.True(FactionManager.IsHostile(enemy, player)); Assert.True(FactionManager.IsHostile(enemy, worker));
            Assert.True(enemy.GetPart<BrainPart>().IsPersonallyHostileTo(worker), "Only exact local staff are targets of this quarantine case.");
            Assert.False(FactionManager.IsHostile(worker, player));
            Assert.False(enemy.HasPart<ConversationPart>()); Assert.False(enemy.HasTag("CanOpenDoors"));
            Assert.AreEqual("DefaultTendril", enemy.GetProperty("NaturalWeapon"));
            var hands = enemy.GetPart<Body>().GetPartsByType("Hand"); Assert.IsNotEmpty(hands);
            foreach (var hand in hands)
            {
                Assert.NotNull(hand._DefaultBehavior); Assert.AreEqual("DefaultTendril", hand.DefaultBehaviorBlueprint);
                var weapon = hand._DefaultBehavior.GetPart<MeleeWeaponPart>(); Assert.AreEqual("1d3", weapon.BaseDamage);
                Assert.True(string.IsNullOrEmpty(weapon.OnHitEffectsRaw));
            }
            Assert.False(enemy.Parts.Any(p => p.Name.IndexOf("Spore", StringComparison.OrdinalIgnoreCase) >= 0 || p.Name.IndexOf("Disease", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        [Test]
        public void FullyCuredSubjectsRemainTheTwoOriginalInertHaulableOwners()
        {
            Generate(); Owner("CurationIntakeIndex");
            var subjects = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "SaltCuredBody").ToArray(); Assert.AreEqual(2, subjects.Length);
            foreach (var body in subjects)
            {
                Assert.False(body.HasTag("Creature")); Assert.False(body.HasPart<BrainPart>()); Assert.False(body.HasPart<CorpsePart>());
                Assert.False(body.HasPart<ContainerPart>()); Assert.False(body.HasPart<DestructiblePart>());
                Assert.AreEqual(90, body.GetPart<HandlingPart>()?.Weight); Assert.AreEqual(90, body.GetPart<PhysicsPart>().Weight);
                Assert.False(body.GetPart<HandlingPart>().Carryable); Assert.False(body.GetPart<HandlingPart>().Throwable);
            }
        }
    }
}
