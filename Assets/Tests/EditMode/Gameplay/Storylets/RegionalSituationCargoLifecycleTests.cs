using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class RegionalSituationCargoLifecycleTests
    {
        private const int Seed = 64;
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Entity player;
        private Zone playerZone;

        [SetUp]
        public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false);
            playerZone = null;
            CinderholdCompositionTests.LoadLoot();
            factory = GrovelandsCompositionTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, Seed);
            player = factory.CreateEntity("Player");
            Assert.NotNull(player);
            player.GetPart<InventoryPart>().MaxWeight = -1;
            StoryletPart.Current = new StoryletPart();
            StoryletPart.LocalPlayer = player;
            NarrativeStatePart.Current = new NarrativeStatePart();
            MorrowfastContent.EnsureRegistered();
        }

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            scope?.Dispose();
            LootTableRegistry.ResetForTests();
        }


        // A restored source cache may first resolve while its real cargo lies
        // elsewhere. That negative observation must not permanently erase work
        // when the player later recovers the actual same saved owner. The paired
        // removed-owner control forbids regenerating a replacement or a false !.
        [TestCase(false)][TestCase(true)]
        public void RestoredDroppedAwayCargoCanBeRecoveredWithoutStaleCueOrReplacement(bool recover)
        {
            var d=RegionalSituations.Find("sumphold-oil"); var b=Bind(d.Id);
            var source=manager.GetZone(d.SourceZoneId); var cargo=Source(source,d.Id);
            var at=source.GetEntityCell(cargo); MovePlayer(source,at.X,at.Y);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(cargo),player,source).Success);
            var away=manager.GetZone("Overworld.15.7.0");
            var open=Enumerable.Range(0,Zone.Width*Zone.Height).Select(i=>away.GetCell(i%Zone.Width,i/Zone.Width))
                .First(c=>!c.BlocksMovement());
            MovePlayer(away,open.X,open.Y);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new DropCommand(cargo),player,away).Success);
            string cargoId=cargo.ID, playerId=player.ID; StandBy(b.zone,b.recipient);
            RoundTripWorld(playerId);
            b=Bind(d.Id); StandBy(b.zone,b.recipient); away=manager.GetZone("Overworld.15.7.0");
            cargo=away.GetAllEntities().Single(e=>e.ID==cargoId);
            int cached=manager.CachedZoneCount;
            Assert.AreEqual(QuestCueState.None,b.request.GetCueState(player,b.zone));
            Assert.AreEqual(cached,manager.CachedZoneCount,"A cue cannot search by generating chunks.");
            if(!recover)
            {
                Assert.IsTrue(away.RemoveEntity(cargo));
                Assert.AreEqual(QuestCueState.None,b.request.GetCueState(player,b.zone));
                Assert.IsFalse(b.request.CanAct(player,b.zone,"accept"));
                Assert.IsFalse(manager.GetZone(d.SourceZoneId).GetAllEntities().Any(e=>e.ID==cargoId));
                Assert.IsFalse(b.request.Completed); return;
            }
            at=away.GetEntityCell(cargo); MovePlayer(away,at.X,at.Y);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(cargo),player,away).Success);
            StandBy(b.zone,b.recipient);
            Assert.IsTrue(b.request.CanAct(player,b.zone,"accept"),"The native action sees the actual carried consignment.");
            Assert.AreEqual(QuestCueState.Available,b.request.GetCueState(player,b.zone),"Cue and native action must agree after pickup.");
            Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));
            Assert.AreEqual(QuestCueState.Active,b.request.GetCueState(player,b.zone));
            Assert.IsTrue(b.request.TryAct(player,b.zone,"release"));
            Assert.AreEqual(QuestCueState.Available,b.request.GetCueState(player,b.zone));
            // Moving the same owner after release must still change availability.
            MovePlayer(away,open.X,open.Y);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new DropCommand(cargo),player,away).Success);
            StandBy(b.zone,b.recipient);
            Assert.AreEqual(QuestCueState.None,b.request.GetCueState(player,b.zone));
            MovePlayer(away,open.X,open.Y);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(cargo),player,away).Success);
            StandBy(b.zone,b.recipient);
            Assert.AreEqual(QuestCueState.Available,b.request.GetCueState(player,b.zone));
            Assert.IsFalse(b.request.Completed);
        }
        private void RoundTripWorld(string playerId)
        {
            using(var stream=new MemoryStream())
            {
                // The session writes queued entity bodies and runs native load
                // hooks; the manager subrecord alone contains unresolved refs.
                var saved=GameSessionState.Capture("regional-adversarial", "test", manager, null, player);
                saved.Save(new SaveWriter(stream));stream.Position=0;
                var restored=GameSessionState.Load(new SaveReader(stream,factory));
                manager=restored.ZoneManager;
                Assert.AreEqual(playerId,restored.Player.ID);
            }
            playerZone=manager.CachedZones.Values.Single(z=>z.GetAllEntities().Any(e=>e.ID==playerId));
            player=playerZone.GetAllEntities().Single(e=>e.ID==playerId);
            StoryletPart.LocalPlayer=player;manager.SetActiveZone(playerZone);SettlementRuntime.ActiveZone=playerZone;
        }
        private (Zone zone, Entity recipient, RegionalRequestPart request) Bind(string id)
        {
            var d = RegionalSituations.Find(id); Assert.NotNull(d, id);
            var z = manager.GetZone(d.RecipientZoneId);
            var matches = z.GetAllEntities().Where(e => e.GetPart<RegionalRequestPart>()?.DefinitionId == id).ToArray();
            Assert.AreEqual(1, matches.Length, "Native recipient binding " + id);
            return (z, matches[0], matches[0].GetPart<RegionalRequestPart>());
        }

        private static Entity Source(Zone zone, string id)
        {
            string sourceId = RegionalSituations.SourceId(RegionalSituations.Find(id), Seed);
            var owners = zone.GetAllEntities().Where(e => e.ID == sourceId).ToArray();
            Assert.AreEqual(1, owners.Length, "Actual generated primary source for " + id);
            return owners[0];
        }

        private void StandBy(Zone zone, Entity owner)
        {
            var at = zone.GetEntityCell(owner); Assert.NotNull(at);
            var standing = CinderholdCompositionTests.Neighbors(at.X, at.Y)
                .Select(p => zone.GetCell(p.x, p.y)).FirstOrDefault(c => c != null && !c.BlocksMovement());
            Assert.NotNull(standing, "Native recipient needs a standing frontage.");
            MovePlayer(zone, standing.X, standing.Y);
        }

        private void MovePlayer(Zone zone, int x, int y)
        {
            if (playerZone != null) Assert.IsTrue(playerZone.RemoveEntity(player));
            Assert.IsTrue(zone.AddEntity(player, x, y)); playerZone = zone;
            manager.SetActiveZone(zone); SettlementRuntime.ActiveZone = zone;
        }

    }
}
