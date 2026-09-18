using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Draft only. No DoStart invocation: it owns unrelated presentation setup.
    // The production GenerateStartingZone seam must also be exercised by real N
    // in the native bootstrap gate; reflection alone cannot prove delegation.
    internal sealed class MorrowfastStartFixture : IDisposable
    {
        private readonly List<Action> _restore = new List<Action>();
        public readonly HotbarSaveFixture Runtime;
        public readonly EntityFactory Factory;
        public GameBootstrap Bootstrap => Runtime.Bootstrap;
        public Entity Player => (Entity)HotbarSaveFixture.Get(Bootstrap, "_player");
        public Zone Zone => (Zone)HotbarSaveFixture.Get(Bootstrap, "_zone");
        public OverworldZoneManager Manager => (OverworldZoneManager)HotbarSaveFixture.Get(Bootstrap, "_zoneManager");
        public NewGameSaveFixture Save => (NewGameSaveFixture)typeof(HotbarSaveFixture)
            .GetField("_save", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Runtime);

        public MorrowfastStartFixture()
        {
            // Morrowfast registration adds actual authored faction/dialogue/quest
            // definitions. Restore registry containers and borrowed entry refs.
            foreach (var type in new[] { typeof(ConversationLoader), typeof(ConversationActions),
                typeof(ConversationPredicates), typeof(StoryletRegistry), typeof(FactionManager) })
                SnapshotRegistry(type);
            try
            {
                Runtime = new HotbarSaveFixture(false, false);
                Factory = MorrowfastTestWorld.Factory();
                LoadoutPart.Factory = Factory;
                TraderPart.Factory = Factory;
                HotbarSaveFixture.Set(Bootstrap, "_factory", Factory);
            }
            catch { Dispose(); throw; }
        }
        private void SnapshotRegistry(Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsLiteral) continue;
                object original = field.GetValue(null);
                if (original is IDictionary dictionary)
                {
                    var entries = new List<DictionaryEntry>();
                    foreach (DictionaryEntry entry in dictionary) entries.Add(entry);
                    _restore.Add(() => {
                        if (!field.IsInitOnly) field.SetValue(null, original);
                        dictionary.Clear(); foreach (var entry in entries) dictionary.Add(entry.Key, entry.Value);
                    });
                }
                else if (original is HashSet<string> set)
                {
                    var entries = set.ToArray();
                    _restore.Add(() => { if (!field.IsInitOnly) field.SetValue(null, original); set.Clear(); foreach (var entry in entries) set.Add(entry); });
                }
                else if (!field.IsInitOnly) _restore.Add(() => field.SetValue(null, original));
            }
        }
        public object Invoke(string name)
        {
            var method = typeof(GameBootstrap).GetMethod(name, HotbarSaveFixture.Flags);
            Assert.NotNull(method, "Explicit private bootstrap seam required: " + name);
            return method.Invoke(Bootstrap, null);
        }
        public void Generate()
        {
            Assert.IsTrue((bool)Invoke("GenerateStartingZone"));
            Assert.NotNull(Zone); Assert.AreSame(Zone, Manager.ActiveZone);
            PreparePlayer();
        }
        public void UseActualVillage(int seed = 64)
        {
            var manager = new OverworldZoneManager(Factory, seed);
            var zone = manager.GetZone(MorrowfastSceneRuntime.ZoneID);
            manager.SetActiveZone(zone);
            HotbarSaveFixture.Set(Bootstrap, "_zoneManager", manager);
            HotbarSaveFixture.Set(Bootstrap, "_zone", zone);
            PreparePlayer();
        }
        public void UseBareZone(string id)
        {
            var zone = new Zone(id); var manager = new OverworldZoneManager(Factory, 64);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { id, zone } }, id,
                new Dictionary<string, List<ZoneConnection>>());
            HotbarSaveFixture.Set(Bootstrap, "_zoneManager", manager);
            HotbarSaveFixture.Set(Bootstrap, "_zone", zone);
            PreparePlayer();
        }
        private void PreparePlayer()
        {
            var player = Factory.CreateEntity("Player"); Assert.NotNull(player);
            FarmingAccessGrant.MarkGranted(player); // no legacy kit migration in load-location cases
            var world = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "World" };
            world.SetTag("WorldEntity");
            var narrative = new NarrativeStatePart(); var storylets = new StoryletPart();
            world.AddPart(narrative); world.AddPart(storylets); narrative.RegisterReactor(storylets);
            NarrativeStatePart.Current = narrative; StoryletPart.Current = storylets;
            StoryletPart.LocalPlayer = player; TurnManager.World = world;
            var turns = new TurnManager();
            turns.RestoreSavedState(17, true, player, new List<TurnManager.SavedTurnEntry> {
                new TurnManager.SavedTurnEntry { Entity = player, Energy = 1000 } });
            HotbarSaveFixture.Set(Bootstrap, "_player", player);
            HotbarSaveFixture.Set(Bootstrap, "_world", world);
            HotbarSaveFixture.Set(Bootstrap, "_turnManager", turns);
            HotbarSaveFixture.Set(Bootstrap, "_gameID", Save.NewID);
        }
        public void Place() => Invoke("PlacePlayerInOpenCell");
        public GameSessionState Capture() => Runtime.Capture();
        public GameSessionState RoundTrip()
        {
            using (var stream = new MemoryStream())
            {
                Capture().Save(new SaveWriter(stream)); stream.Position = 0;
                return GameSessionState.Load(new SaveReader(stream, Factory));
            }
        }
        public void RegisterFresh() => SaveGameService.RegisterRuntime(Runtime.Capture, Bootstrap.ApplyLoadedGame, Save.NewID);
        public void Dispose()
        {
            Runtime?.Dispose();
            for (int i = _restore.Count - 1; i >= 0; --i) _restore[i]();
        }
    }

    public sealed class GameAuditMorrowfastStartTests
    {
        [Test] public void ActualFreshZoneGenerationSelectsAuthoredMorrowfastAndItsCachedInstance()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.Generate(); Assert.AreEqual(MorrowfastSceneRuntime.ZoneID, f.Zone.ZoneID);
                Assert.IsTrue(MorrowfastSceneRuntime.IsActive(f.Zone));
                Assert.AreSame(f.Zone, f.Manager.GetZone(MorrowfastSceneRuntime.ZoneID));
                Assert.AreEqual("Morrowfast", f.Manager.WorldMap.GetPOI(3, 6).Profile);
            }
        }
        [TestCase(64)] [TestCase(65)] [TestCase(1729)]
        public void FreshPlacementUsesSouthernMainRoadInsideTheBoundary(int seed)
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseActualVillage(seed); f.Place();
                Assert.AreEqual((40, 23), f.Zone.GetEntityPosition(f.Player));
                Assert.IsFalse(f.Zone.GetEntityCell(f.Player).BlocksMovement(f.Player));
                Assert.IsNull(MorrowfastSceneDefinition.Load().RoomAt(40, 23));
                Assert.AreEqual(1, f.Zone.GetAllEntities().Count(e => ReferenceEquals(e, f.Player)));
            }
        }
        [Test] public void NonMorrowfastPlacementRetainsExistingCenterSearch()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseBareZone("Overworld.10.10.0"); f.Place();
                Assert.AreEqual((40, 12), f.Zone.GetEntityPosition(f.Player));
            }
        }
        [Test] public void SillIdentityStillOwnsItsExistingPopulationAndRepairSiteContracts()
        {
            Assert.AreEqual("Overworld.10.10.0", WorldMap.StartingZoneID);
            Assert.AreEqual(WorldMap.StartingZoneID, VillagePopulationBuilder.StartingVillageZoneId);
            Assert.AreEqual(WorldMap.StartingZoneID, SettlementSiteDefinitions.StartingVillageZoneId);
            Assert.AreNotEqual(WorldMap.StartingZoneID, MorrowfastSceneRuntime.ZoneID);
        }
        [Test] public void BlockedPreferredSpawnSearchesNearbyWithoutRemovingTheBlocker()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseActualVillage(); var obstacle = new Entity { ID = "spawn-blocker", BlueprintName = "SpawnBlocker" };
                obstacle.SetTag("Solid"); Assert.IsTrue(f.Zone.AddEntity(obstacle, 40, 23)); f.Place();
                var at = f.Zone.GetEntityPosition(f.Player);
                Assert.AreEqual(1, Math.Max(Math.Abs(at.x - 40), Math.Abs(at.y - 23)));
                Assert.AreEqual((40, 23), f.Zone.GetEntityPosition(obstacle));
                Assert.IsFalse(f.Zone.GetEntityCell(f.Player).BlocksMovement(f.Player));
            }
        }
        [Test] public void ActualFreshSpawnCanReachBothAuthoredExitsWithoutOpeningOrClearingOwners()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseActualVillage(); f.Place(); var at = f.Zone.GetEntityPosition(f.Player);
                Assert.AreEqual((40, 23), at); var seen = MorrowfastTestWorld.Flood(f.Zone, f.Player, at.x, at.y);
                Assert.IsTrue(seen.Contains((40, 0))); Assert.IsTrue(seen.Contains((40, 24)));
                foreach (var b in MorrowfastSceneDefinition.Load().buildings)
                    Assert.IsFalse(MorrowfastSceneRuntime.IsDoorOpen(f.Zone, b.doorId));
            }
        }
        [Test] public void FarmPlotSetupRetainsEveryAuthoredOwnerAndBothRoadConnections()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseActualVillage(); f.Place();
                var owners = MorrowfastSceneDefinition.Load().owners.ToDictionary(o => o.id, o => MorrowfastSceneRuntime.FindOwner(f.Zone, o.id));
                Assert.IsTrue(owners.Values.All(e => e != null));
                f.Invoke("EnsureFarmPlotAtSpawn");
                foreach (var pair in owners) Assert.AreSame(pair.Value, MorrowfastSceneRuntime.FindOwner(f.Zone, pair.Key), pair.Key);
                var at = f.Zone.GetEntityPosition(f.Player); Assert.AreEqual((40, 23), at);
                var seen = MorrowfastTestWorld.Flood(f.Zone, f.Player, at.x, at.y);
                Assert.IsTrue(seen.Contains((40, 0))); Assert.IsTrue(seen.Contains((40, 24)));
            }
        }
        [Test] public void OrdinaryNorthTransitionStillEntersRealFellingAndReturnsTheSamePlayer()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseActualVillage(); f.Place(); var actor = f.Player; var original = f.Zone;
                // API stimulus after proving the road is connected in the sibling test.
                Assert.IsTrue(original.MoveEntity(actor, 40, 0));
                var north = ZoneTransitionSystem.TransitionPlayer(actor, original, TransitionDirection.North, 40, 0, f.Manager, f.Manager.WorldMap);
                Assert.IsTrue(north.Success, north.ErrorReason); Assert.AreEqual(FellingSiteBuilder.ZoneID, north.NewZone.ZoneID);
                Assert.AreEqual((40, 24), north.NewZone.GetEntityPosition(actor)); Assert.IsNull(original.GetEntityCell(actor));
                var south = ZoneTransitionSystem.TransitionPlayer(actor, north.NewZone, TransitionDirection.South, 40, 24, f.Manager, f.Manager.WorldMap);
                Assert.IsTrue(south.Success, south.ErrorReason); Assert.AreSame(original, south.NewZone);
                Assert.AreEqual((40, 0), south.NewZone.GetEntityPosition(actor));
            }
        }
        [TestCase("Overworld.10.10.0")] [TestCase("Overworld.4.6.1")]
        public void ExistingSerializedLocationIsNotRelocatedByFullBootstrapApply(string zoneId)
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseBareZone(zoneId); Assert.IsTrue(f.Zone.AddEntity(f.Player, 7, 8));
                f.Player.SetIntProperty("MorrowfastRecordConsent", 1); string id = f.Player.ID;
                var loaded = f.RoundTrip(); f.Bootstrap.ApplyLoadedGame(loaded);
                Assert.AreSame(loaded.Player, f.Player); Assert.AreEqual(id, f.Player.ID);
                Assert.AreEqual(zoneId, f.Zone.ZoneID); Assert.AreEqual((7, 8), f.Zone.GetEntityPosition(f.Player));
                Assert.AreEqual(1, f.Player.GetIntProperty("MorrowfastRecordConsent"));
                Assert.AreSame(loaded.Player, loaded.TurnManager.CurrentActor); Assert.AreEqual(17, loaded.TurnManager.TickCount);
            }
        }
        [Test] public void ExistingMorrowfastLocationDoorAndJournalSurviveFullLoadWithoutReset()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseActualVillage(); f.Place(); var b = MorrowfastSceneDefinition.Load().buildings[0];
                var door = MorrowfastSceneRuntime.FindOwner(f.Zone, b.doorId);
                var approach = MorrowfastTestWorld.Approach(f.Zone, f.Player, door);
                Assert.IsTrue(f.Zone.MoveEntity(f.Player, approach.x, approach.y));
                Assert.IsTrue(door.GetPart<MorrowfastDoorPart>().TrySetOpen(f.Player, f.Zone, true));
                StoryletPart.Current.StartQuest(new QuestState { QuestId = MorrowfastQuests.ReturnQuestId, CurrentStageIndex = 0, EnteredStageAtTurn = 11 });
                f.Player.SetIntProperty(MorrowfastQuests.EddenConsent, 1);
                var loaded = f.RoundTrip(); f.Bootstrap.ApplyLoadedGame(loaded);
                Assert.AreEqual(approach, f.Zone.GetEntityPosition(f.Player));
                Assert.IsTrue(MorrowfastSceneRuntime.IsDoorOpen(f.Zone, b.doorId));
                Assert.AreEqual(11, StoryletPart.Current.GetQuestState(MorrowfastQuests.ReturnQuestId).EnteredStageAtTurn);
                Assert.AreEqual(1, f.Player.GetIntProperty(MorrowfastQuests.EddenConsent));
            }
        }
        [Test] public void FreshPlacementDoesNotAcceptOptionalQuestsOrInventConsent()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseActualVillage(); f.Place();
                foreach (var id in new[] { MorrowfastQuests.ReturnQuestId, MorrowfastQuests.BellQuestId, MorrowfastQuests.SupperQuestId })
                    Assert.IsNull(StoryletPart.Current.GetQuestState(id));
                Assert.AreEqual(0, f.Player.GetIntProperty(MorrowfastQuests.EddenConsent));
                Assert.AreEqual(0, f.Player.GetIntProperty("MorrowfastRecordConsent"));
            }
        }
        [Test] public void RealBootNCheckpointsNewVillageGraphAndPreservesPreviousSaveBytes()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.Generate(); f.Place(); string playerId = f.Player.ID; f.RegisterFresh();
                Assert.IsFalse(f.Save.Choose(KeyCode.N).IsActive);
                Assert.AreEqual(f.Save.NewID, f.Save.ActiveID); Assert.IsTrue(SaveGameService.QuickLoad());
                Assert.AreEqual(playerId, f.Player.ID); Assert.AreEqual(MorrowfastSceneRuntime.ZoneID, f.Zone.ZoneID);
                Assert.AreEqual((40, 23), f.Zone.GetEntityPosition(f.Player)); f.Save.OldUnchanged();
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void BootContinueRetainsSerializedOldLocationAndWinsSimultaneousN(bool alsoNew)
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseBareZone("Overworld.10.10.0"); Assert.IsTrue(f.Zone.AddEntity(f.Player, 7, 8));
                string oldPlayerId = f.Player.ID;
                HotbarSaveFixture.Set(f.Bootstrap, "_gameID", f.Save.OldID);
                SaveGameService.RegisterRuntime(f.Runtime.Capture, f.Bootstrap.ApplyLoadedGame, f.Save.OldID);
                SaveGameService.SetActiveGameID(f.Save.OldID); Assert.IsTrue(SaveGameService.QuickSave());
                string oldPath = Path.Combine(f.Save.Root, f.Save.OldID, "Quick.sav.gz");
                byte[] oldBytes = File.ReadAllBytes(oldPath);
                f.Generate(); f.Place(); f.RegisterFresh();
                Assert.IsFalse(f.Save.Choose(alsoNew ? new[] { KeyCode.C, KeyCode.N } : new[] { KeyCode.C }).IsActive);
                Assert.AreEqual(f.Save.OldID, f.Save.ActiveID); Assert.AreEqual(oldPlayerId, f.Player.ID);
                Assert.AreEqual("Overworld.10.10.0", f.Zone.ZoneID); Assert.AreEqual((7, 8), f.Zone.GetEntityPosition(f.Player));
                Assert.IsFalse(Directory.Exists(Path.Combine(f.Save.Root, f.Save.NewID)));
                CollectionAssert.AreEqual(oldBytes, File.ReadAllBytes(oldPath));
            }
        }
        [Test] public void ImmediateCheckpointAndLaterF5F6RetainTheActualMovedPosition()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.Generate(); f.Place(); f.RegisterFresh(); Assert.IsTrue(f.Save.Begin());
                Assert.IsFalse(f.Zone.GetCell(40, 22).BlocksMovement(f.Player));
                Assert.IsTrue(f.Zone.MoveEntity(f.Player, 40, 22));
                Assert.IsTrue(SaveGameService.QuickSave()); Assert.IsTrue(f.Zone.MoveEntity(f.Player, 40, 23));
                Assert.IsTrue(SaveGameService.QuickLoad());
                Assert.AreEqual((40, 22), f.Zone.GetEntityPosition(f.Player));
                Assert.AreSame(f.Player, f.Capture().TurnManager.CurrentActor); f.Save.OldUnchanged();
            }
        }
    }
}
